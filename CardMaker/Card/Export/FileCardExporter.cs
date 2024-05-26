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
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using CardMaker.Data;
using Support.IO;

namespace CardMaker.Card.Export
{
    public class FileCardExporter : CardExportBase
    {
        private readonly string m_sExportFolder;
        private readonly string m_sOverrideStringFormat;
        private readonly FileCardExporterFactory.CardMakerExportImageFormat m_eImageFormat;
        private readonly int m_nSkipStitchIndex;
        public int[] ExportCardIndices { get; set; }

        private Bitmap m_zSingleCardBuffer;

        ~FileCardExporter()
        {
            m_zSingleCardBuffer?.Dispose();
        }

        public FileCardExporter(int nLayoutStartIndex, int nLayoutEndIdx, string sExportFolder, string sOverrideStringFormat, int nSkipStitchIndex, FileCardExporterFactory.CardMakerExportImageFormat eImageFormat) 
            : this(Enumerable.Range(nLayoutStartIndex, (nLayoutEndIdx - nLayoutStartIndex) + 1).ToArray(), sExportFolder, sOverrideStringFormat, nSkipStitchIndex, eImageFormat)
        {

        }

        public FileCardExporter(int[] arrayExportLayoutIndices, string sExportFolder, string sOverrideStringFormat, int
            nSkipStitchIndex, FileCardExporterFactory.CardMakerExportImageFormat eImageFormat)
            : base(arrayExportLayoutIndices)
        {
            if (!sExportFolder.EndsWith(Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture)))
            {
                sExportFolder += Path.DirectorySeparatorChar;
            }

            m_sExportFolder = sExportFolder;
            m_sOverrideStringFormat = sOverrideStringFormat;

            m_nSkipStitchIndex = nSkipStitchIndex;
            m_eImageFormat = eImageFormat;
            CurrentDeck.ExportContext = new ExportContext(m_eImageFormat);
        }

        public override void ExportThread()
        {
            var progressLayoutIdx = ProgressReporter.GetProgressIndex(ProgressName.LAYOUT);
            var progressCardIdx = ProgressReporter.GetProgressIndex(ProgressName.CARD);

#warning this other exporters will need this (TODO: test the other exporters)
            CurrentDeck.SubLayoutExportContext = SubLayoutExportContext;

            // Exports may put multiple cards into a single exported image (referred to as a container below)

            ProgressReporter.ProgressReset(progressLayoutIdx, 0, ExportLayoutIndices.Length, 0);
            foreach (var nIdx in ExportLayoutIndices)
            {
                ChangeExportLayoutIndex(nIdx);
                if (CurrentDeck.EmptyReference)
                {
                    // empty reference layouts are not exported
                    ProgressReporter.ProgressStep(progressLayoutIdx);
                    continue;
                }
                var nCardCountPadSize = CurrentDeck.CardCount.ToString(CultureInfo.InvariantCulture).Length;
                ProgressReporter.ProgressReset(progressCardIdx, 0, CurrentDeck.CardCount, 0);

                var exportContainerWidth = CurrentDeck.CardLayout.exportWidth == 0
                    ? CurrentDeck.CardLayout.width : CurrentDeck.CardLayout.exportWidth;

                var exportContainerHeight = CurrentDeck.CardLayout.exportHeight == 0
                    ? CurrentDeck.CardLayout.height : CurrentDeck.CardLayout.exportHeight;

                if (CurrentDeck.CardLayout.width > exportContainerWidth ||
                    CurrentDeck.CardLayout.height > exportContainerHeight)
                {
                    Logger.AddLogLine(
                        $"ERROR: Layout: [{CurrentDeck.CardLayout.Name}] exportWidth and/or exportHeight too small! (Skipping export)");
                    continue;
                }

                // swap width/height if necessary for rotation
                var currentCardWidth = CurrentDeck.CardLayout.width;
                var currentCardHeight = CurrentDeck.CardLayout.height;
                if (Math.Abs(CurrentDeck.CardLayout.exportRotation) == 90)
                {
                    (exportContainerWidth, exportContainerHeight) = (exportContainerHeight, exportContainerWidth);
                    (currentCardWidth, currentCardHeight) = (currentCardHeight, currentCardWidth);
                }

                UpdateBufferBitmap(exportContainerWidth, exportContainerHeight);
                // The graphics must be initialized BEFORE the resolution of the bitmap is set (graphics will be the same DPI as the application/card)
                var zContainerGraphics = Graphics.FromImage(m_zExportCardBuffer);
                var arrayCardIndices = GetCardIndicesArray(CurrentDeck, ExportCardIndices);
                for(var nCardArrayIdx = 0; nCardArrayIdx < arrayCardIndices.Length; nCardArrayIdx++)
                {
                    var nX = 0;
                    var nY = 0;
                    var nCardsExportedInImage = 0;
                    ClearGraphics(zContainerGraphics, m_zExportCardBuffer);
                    do
                    {
                        CurrentDeck.ResetDeckCache();
                        CurrentDeck.CardIndex = arrayCardIndices[nCardArrayIdx];

                        ProcessSubLayoutExports(m_sExportFolder);

                        var bitmapSingleCard = CreateSingleCardBufferBitmap(CurrentDeck.CardLayout.width, CurrentDeck.CardLayout.height);
                        var zSingleCardGraphics = Graphics.FromImage(bitmapSingleCard);
                        ClearGraphics(zSingleCardGraphics, bitmapSingleCard);
                        var bExportCard = CardRenderer.DrawExportLineToGraphics(new GraphicsContext(zSingleCardGraphics, bitmapSingleCard), 0, 0, !CurrentDeck.CardLayout.exportTransparentBackground);
                        if (bExportCard)
                        {
                            ProcessRotateExport(bitmapSingleCard, CurrentDeck.CardLayout, false);
                            zContainerGraphics.DrawImage(bitmapSingleCard, nX, nY);

                            ProgressReporter.ProgressStep(progressCardIdx);

                            nCardsExportedInImage++;

                            var nMoveCount = 1;
                            if (m_nSkipStitchIndex > 0)
                            {
                                var x = ((nCardsExportedInImage + 1) % m_nSkipStitchIndex);
                                if (x == 0)
                                {
                                    // shift forward an extra spot to ignore the dummy index
                                    nMoveCount = 2;
                                }
                            }

                            var bOutOfSpace = false;

                            for (var nShift = 0; nShift < nMoveCount; nShift++)
                            {
#warning this is messed up for rotated layouts
                                nX += currentCardWidth + CurrentDeck.CardLayout.buffer;
                                if (nX + currentCardWidth > exportContainerWidth)
                                {
                                    nX = 0;
                                    nY += currentCardHeight + CurrentDeck.CardLayout.buffer;
                                }

                                if (nY + currentCardHeight > exportContainerHeight)
                                {
                                    // no more space
                                    bOutOfSpace = true;
                                    break;
                                }
                            }

                            if (bOutOfSpace)
                            {
                                break;
                            }
                        }

                        // increment and continue to add cards to this buffer
                        nCardArrayIdx++;
                    } while (nCardArrayIdx < arrayCardIndices.Length);

                    if (nCardsExportedInImage > 0)
                    {
                        string sFileName;
                        if (!string.IsNullOrEmpty(m_sOverrideStringFormat))
                        {
                            // check for the super override
                            sFileName = CurrentDeck.TranslateFileNameString(m_sOverrideStringFormat, CurrentDeck.CardNumber, nCardCountPadSize);
                        }
                        else if (!string.IsNullOrEmpty(CurrentDeck.CardLayout.exportNameFormat))
                        {
                            // check for the per layout override
                            sFileName = CurrentDeck.TranslateFileNameString(CurrentDeck.CardLayout.exportNameFormat, CurrentDeck.CardNumber, nCardCountPadSize);
                        }
                        else // default
                        {
                            sFileName = CurrentDeck.CardLayout.Name + "_" + (CurrentDeck.CardNumber).ToString(CultureInfo.InvariantCulture).PadLeft(nCardCountPadSize, '0');
                        }
                        try
                        {
                            Save(m_zExportCardBuffer,
                                m_sExportFolder + sFileName + "." + m_eImageFormat.ToString().ToLower(),
                                m_eImageFormat,
                                CurrentDeck.CardLayout.dpi);
                        }
                        catch (Exception ex)
                        {
                            ProgressReporter.AddIssue(
                                "Invalid Filename or IO error: " + sFileName + " :: " + ex.Message);
                            ProgressReporter.ThreadSuccess = false;
                            ProgressReporter.Shutdown();
                            return;
                        }
                    }
                }
                ProgressReporter.ProgressStep(progressLayoutIdx);
            }

            ProgressReporter.ThreadSuccess = true;
            ProgressReporter.Shutdown();
        }

        protected virtual Bitmap CreateSingleCardBufferBitmap(int nWidth, int nHeight)
        {
            if (null == m_zSingleCardBuffer ||
                nWidth != m_zSingleCardBuffer.Width ||
                nHeight != m_zSingleCardBuffer.Height)
            {
                m_zSingleCardBuffer?.Dispose();
                m_zSingleCardBuffer = new Bitmap(nWidth, nHeight, PixelFormat.Format32bppArgb);
            }

            return m_zSingleCardBuffer;
        }
    }
}
