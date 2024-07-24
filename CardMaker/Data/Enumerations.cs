////////////////////////////////////////////////////////////////////////////////
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

using System.Collections.Generic;

namespace CardMaker.Data
{
    public static class EnumUtil
    {
        private static readonly Dictionary<string, ElementType> s_dictionaryStringElementType = new Dictionary<string, ElementType>();

        static EnumUtil()
        {
            for (var nIdx = 0; nIdx < (int)ElementType.End; nIdx++)
            {
                s_dictionaryStringElementType.Add(((ElementType)nIdx).ToString(), (ElementType)nIdx);
            }
        }

        public static ElementType GetElementType(string sType)
        {
            ElementType eType;
            if (sType != null && s_dictionaryStringElementType.TryGetValue(sType, out eType))
            {
                return eType;
            }
            return ElementType.End;
        }
    }

    public enum ElementType
    {
        Text,
        FormattedText,
        Graphic,
        Shape,
        SubLayout,
        End
    }

    public enum MirrorType
    {
        None = 0,
        Horizontal,
        Vertical,
        End
    }

    public enum ElementColorType
    {
        Add = 0, // default
        Multiply,
        Matrix,
        End
    }

    public enum MeasurementUnit
    {
        Inch,
        Millimeter,
        Centimeter
    }

    public enum GradientStyle
    {
        lefttoright,
        points,
        pointsnormalized,
        None,
    }

    public enum IniSettings
    {
        PreviousProjects,
        ReplacementChars,
        ProjectManagerRoot,
        PrintPageMeasurementUnit,
        PrintPageWidth,
        PrintPageHeight,
        PrintPageVerticalMargin,
        PrintPageHorizontalMargin,
        PrintAutoCenterLayout,
        AutoSaveEnabled,
        AutoSaveIntervalMinutes,
        LastImageExportFormat,
        ExportWebPLossless,
        ExportWebPQuality,
        CompositingQualityGammaCorrected,
        PixelOffsetModeHighQuality,
        PrintLayoutsOnNewPage,
        EnableGoogleCache,
        DefaultTranslator,
        ExportSkipStitchIndex,
        DefineTranslatePrimitiveCharacters,
        LogInceptTranslation,
        ShowCanvasXY,
        FormattedTextMergeTextMarkups,
        StringMeasureMethod,
        EnableTranslateOnDrag,
    }

    public enum ExportType
    {
        PDFSharp,
        Image,
        SingleImage,
        SingleImageClipboard
    }

    public enum TranslatorType
    {
        Incept = 0,
        JavaScript
    }

    public enum ReferenceType
    {
        CSV = 0,
        Google = 1,
        Excel = 2
    }

    public enum ElementInsertDestination
    {
        Above = 0,
        Below,
        Top,
        Bottom
    }
}
