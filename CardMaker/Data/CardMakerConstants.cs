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

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using Google.Apis.Sheets.v4;

namespace CardMaker.Data
{
    public static class CardMakerConstants
    {
        public const string APPLICATION_NAME = "CardMaker";
        public static readonly Encoding XML_ENCODING = Encoding.UTF8;
        public const string VISIBLE_SETTING = ".visible";
        public const char CHAR_FILE_SPLIT = '|';
        public const int MAX_RECENT_PROJECTS = 10;
        public static readonly Color NoColor = Color.FromArgb(0, 0, 0, 0);
        public const string ALLOWED_LAYOUT_COLUMN = "allowed_layout";
        public const string FORMATTED_TEXT_PARAM_SEPARATOR = ";";
        public static readonly string[] FORMATTED_TEXT_PARAM_SEPARATOR_ARRAY = {FORMATTED_TEXT_PARAM_SEPARATOR};
        public const char ALLOWED_COLUMN_SEPARATOR = ';';
        public const string GRADIENT_PARAM_SEPARATOR = ";";
        public const string OVERRIDE_COLUMN = "override:";

        // Google connectivity constants
        public const string GOOGLE_CLIENT_ID = "455195524701-cmdvv6fl5ru9uftin99kjmhojt36mnm9.apps.googleusercontent.com";

        public static readonly string[] GOOGLE_SCOPES = new string[]
            {SheetsService.Scope.SpreadsheetsReadonly};

        public static readonly Bitmap GridBackground;
        public static readonly TextureBrush GridBackgroundBrush;

        static CardMakerConstants()
        {
            GridBackground = new Bitmap(32, 32);
            var zGraphics = Graphics.FromImage(GridBackground);
            Brush zBlack = new SolidBrush(Color.LightGray);
            Brush zWhite = new SolidBrush(Color.White);
            zGraphics.FillRectangle(zBlack, 0, 0, 16, 16);
            zGraphics.FillRectangle(zWhite, 16, 0, 16, 16);
            zGraphics.FillRectangle(zBlack, 16, 16, 16, 16);
            zGraphics.FillRectangle(zWhite, 0, 16, 16, 16);
            GridBackgroundBrush = new TextureBrush(GridBackground, WrapMode.Tile);
        }

    }

    public static class ProgressName
    {
        public const string REFERENCE_DATA = "Reference Data";
        public const string LAYOUT = "Layout";
        public const string CARD = "Card";
    }
}
