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

using System.Collections.Generic;
using CardMaker.XML;

namespace CardMaker.Events.Args
{
    public delegate void ElementSelected(object sender, ElementEventArgs args);
    
    public delegate void ElementBoundsUpdated(object sender, ElementEventArgs args);

    public delegate void ElementRenamed(object sender, ElementRenamedEventArgs args);

    public delegate void ElementsAdded(object sender, ElementEventArgs args);

    public delegate void ElementsRemoved(object sender, ElementsRemovedEventArgs args);

    public class ElementEventArgs
    {
        public List<ProjectLayoutElement> Elements { get; private set; }

        public ElementEventArgs(List<ProjectLayoutElement> listElements)
        {
            Elements = listElements;
        }

        public ElementEventArgs(ProjectLayoutElement zElement) :
            this(new List<ProjectLayoutElement>(new ProjectLayoutElement[] { zElement }))
        {
        }
    }

    public class ElementRenamedEventArgs
    {
        public ProjectLayout Layout { get; private set; }
        public ProjectLayoutElement Element { get; private set; }
        public string OldName { get; private set; }

        public ElementRenamedEventArgs(ProjectLayout zLayout, ProjectLayoutElement zElement, string oldName)
        {
            Layout = zLayout;
            Element = zElement;
            OldName = oldName;
        }
    }

    public class ElementsRemovedEventArgs
    {
        public ProjectLayout Layout { get; private set; }
        public List<ProjectLayoutElement> Elements { get; private set; }

        public ElementsRemovedEventArgs(ProjectLayout zLayout, List<ProjectLayoutElement> listElements)
        {
            Layout = zLayout;
            Elements = listElements;
        }
    }
}
