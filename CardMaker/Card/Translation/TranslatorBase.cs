﻿////////////////////////////////////////////////////////////////////////////////
// The MIT License (MIT)
//
// Copyright (c) 2024 Tim Stair
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using CardMaker.Card.FormattedText;
using CardMaker.Data;
using CardMaker.Events.Managers;
using CardMaker.XML;
using Support.IO;
using Support.UI;
using Support.Util;

namespace CardMaker.Card.Translation
{
    public abstract class TranslatorBase
    {
        public const string FIELD_READ_DISALLOWED = "FIELD_READ_DISALLOWED";
        public const string INVALID_FIELD_READ = "INVALID_FIELD_READ";


        // spreadsheet based overrides
        public Dictionary<string, string> DictionaryDefines { get; private set; }
        public List<string> ListColumnNames { get; private set; }

        protected readonly Dictionary<string, ElementString> m_dictionaryElementStringCache = new Dictionary<string, ElementString>();
        protected readonly Dictionary<string, FormattedTextDataCache> m_dictionaryMarkupCache = new Dictionary<string, FormattedTextDataCache>();

        private static readonly HashSet<string> s_setDisallowedOverrideFields = new HashSet<string>()
        {
            "name",
            "variable"
        };
        public static bool IsDisallowedOverrideField(string sField)
        {
            return s_setDisallowedOverrideFields.Contains(sField);
        }

        private static readonly HashSet<string> s_setDisallowedReadFields = new HashSet<string>()
        {
            "variable"
        };
        public static bool IsDisallowedReadField(string sField)
        {
            return s_setDisallowedReadFields.Contains(sField);
        }

        protected TranslatorBase(Dictionary<string, string> dictionaryDefines, List<string> listColumnNames)
        {
            DictionaryDefines = dictionaryDefines;
            ListColumnNames = listColumnNames;
        }

        public ElementString TranslateString(Deck zDeck, string sRawString, int nCardIndex, DeckLine zDeckLine, ProjectLayoutElement zElement, string sCacheSuffix = "", bool bSkipCache = false)
        {
            var sCacheKey = zElement.name + sCacheSuffix;

            // pull from translated string cache
            if (!bSkipCache && m_dictionaryElementStringCache.TryGetValue(sCacheKey, out var zCached))
            {
                return zCached;
            }

            var zElementString = TranslateToElementString(zDeck, sRawString, nCardIndex, zDeckLine, zElement);

            if (zElementString.String.Contains("#nodraw"))
            {
                zElementString.DrawElement = false;
            }
            if (zElementString.String.Contains("#noexport"))
            {
                zElementString.DrawCard = false;
            }

            // all translators perform this post replacement operation
            switch ((ElementType)Enum.Parse(typeof(ElementType), zElement.type))
            {
                case ElementType.Text:
                    zElementString.String = zElementString.String.Replace("\\n", Environment.NewLine);
                    zElementString.String = zElementString.String.Replace("\\q", "\"");
                    zElementString.String = zElementString.String.Replace("\\c", ",");
                    zElementString.String = zElementString.String.Replace("&gt;", ">");
                    zElementString.String = zElementString.String.Replace("&lt;", "<");
                    break;
                case ElementType.FormattedText:
                    // NOTE: never convert \n => <br> here. This will affect file paths that include '\n' (ie. c:\newfile.png)
                    zElementString.String = zElementString.String.Replace("<c>", ",");
                    zElementString.String = zElementString.String.Replace("<q>", "\"");
                    zElementString.String = zElementString.String.Replace("&gt;", ">");
                    zElementString.String = zElementString.String.Replace("&lt;", "<");
                    break;
            }

            AddStringToTranslationCache(sCacheKey, zElementString);

            return zElementString;
        }

        protected abstract ElementString TranslateToElementString(Deck zDeck, string sRawString, int nCardIndex, DeckLine zDeckLine,
            ProjectLayoutElement zElement);

        public ProjectLayoutElement GetOverrideElement(Deck zDeck, ProjectLayoutElement zElement, int nCardIndex, Dictionary<string, Dictionary<string, string>> dictionaryElementToOverrideFieldsToValues, DeckLine zDeckLine)
        {
            dictionaryElementToOverrideFieldsToValues.TryGetValue(
                zElement.name.ToLower(), 
                out var dictionaryOverrideFieldsToValues);

            zElement = ProjectManager.Instance.LookupElementReference(zElement);

            var zOverrideElement = new ProjectLayoutElement();
            zOverrideElement.DeepCopy(zElement, false);
            zOverrideElement.name = zElement.name;

            if (null != dictionaryOverrideFieldsToValues)
            {
                foreach (var sKey in dictionaryOverrideFieldsToValues.Keys)
                {
                    var zProperty = typeof(ProjectLayoutElement).GetProperty(sKey);
                    if (null != zProperty && zProperty.CanWrite)
                    {
                        var sValue = dictionaryOverrideFieldsToValues[sKey].Trim();

                        // Note: TranslateString maintains an element name based cache, the key is critical to make this translation unique
                        sValue = TranslateString(zDeck, sValue, nCardIndex, zDeckLine, zOverrideElement, sKey).String;

                        UpdateElementField(zOverrideElement, zProperty, sValue);
                    }
                    else
                    {
                        IssueManager.Instance.FireAddIssueEvent(
                            "Unrecognized data-based override: [{0}] for element: [{1}]".FormatString(sKey,
                                zElement.name));
                    }
                }
            }

            return zOverrideElement;
        }

        /// <summary>
        /// Updates the passed in element with the provided field override dictionary
        /// </summary>
        /// <param name="zElement">Element to update with the override values</param>
        /// <param name="dictionaryOverrideFieldToValue">The dictionary of fields/values to update with</param>
        /// <returns>The updated Element</returns>
        public ProjectLayoutElement GetVariableOverrideElement(ProjectLayoutElement zElement, Dictionary<string, string> dictionaryOverrideFieldToValue)
        {
            if (null != dictionaryOverrideFieldToValue)
            {
                foreach (var zKvp in dictionaryOverrideFieldToValue)
                {
                    // This apparently isn't slow... otherwise cache the lookup etc.
                    var zProperty = typeof(ProjectLayoutElement).GetProperty(zKvp.Key);
                    if (null != zProperty && zProperty.CanWrite)
                    {
                        UpdateElementField(zElement, zProperty, zKvp.Value);
                    }
                    else
                    {
                        IssueManager.Instance.FireAddIssueEvent(
                            "Unrecognized definition/variable override: [{0}] for element: [{1}]".FormatString(zKvp.Key,
                                zElement.name));
                    }
                }
            }
            return zElement;
        }

        private static void UpdateElementField(ProjectLayoutElement zElement, PropertyInfo zPropertyInfo, string sValue)
        {
            var zMethod = zPropertyInfo.GetSetMethod();

            if (!string.IsNullOrEmpty(sValue))
            {
                if (zPropertyInfo.PropertyType == typeof(string))
                {
                    zMethod.Invoke(zElement, new object[] { sValue });
                }
                else if (zPropertyInfo.PropertyType == typeof(float))
                {
                    float fValue;
                    if (ParseUtil.ParseFloat(sValue, out fValue))
                    {
                        zMethod.Invoke(zElement, new object[] { fValue });
                    }
                }
                else if (zPropertyInfo.PropertyType == typeof(bool))
                {
                    bool bValue;
                    if (bool.TryParse(sValue, out bValue))
                    {
                        zMethod.Invoke(zElement, new object[] { bValue });
                    }
                }
                else if (zPropertyInfo.PropertyType == typeof(Int32))
                {
                    int nValue;
                    if (int.TryParse(sValue, out nValue) 
                        || int.TryParse(sValue.Replace("0x", string.Empty), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out nValue))
                    {
                        zMethod.Invoke(zElement, new object[] { nValue });
                    }
                }
            }
        }

#region Translation Cache

        public void ResetTranslationCache(ProjectLayoutElement zElement)
        {
            if (m_dictionaryElementStringCache.ContainsKey(zElement.name))
            {
                m_dictionaryElementStringCache.Remove(zElement.name);
            }
        }

        public void ResetTranslationCache()
        {
            m_dictionaryElementStringCache.Clear();
        }

        protected void AddStringToTranslationCache(string sKey, ElementString zElementString)
        {
            if (m_dictionaryElementStringCache.ContainsKey(sKey))
            {
                m_dictionaryElementStringCache.Remove(sKey);
                //Logger.AddLogLine("String Cache: Replace?!");
            }
            m_dictionaryElementStringCache.Add(sKey, zElementString);
        }

        public ElementString GetStringFromTranslationCache(string sKey)
        {
            if (m_dictionaryElementStringCache.ContainsKey(sKey))
                return m_dictionaryElementStringCache[sKey];
            return null;
        }

        #endregion

#region Markup Cache

        public void AddCachedMarkup(string sElementName, FormattedTextDataCache zFormattedData)
        {
            m_dictionaryMarkupCache.Add(sElementName, zFormattedData);
        }

        public void ResetMarkupCache()
        {
            m_dictionaryMarkupCache.Clear();
        }

        public void ResetMarkupCache(string sElementName)
        {
            m_dictionaryMarkupCache.Remove(sElementName);
        }

        public FormattedTextDataCache GetCachedMarkup(string sElementName)
        {
            if (m_dictionaryMarkupCache.TryGetValue(sElementName, out var zCached))
            {
                return zCached;
            }
            return null;
        }
        #endregion

    }
}
