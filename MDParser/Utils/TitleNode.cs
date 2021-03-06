﻿using System.Collections.Generic;

namespace MDParser.Utils
{
    public class TitleNode
    {
        public string Text { get; set; }
        public List<TitleNode> ChildNodes { get; set; } = new List<TitleNode>();

        public int Level { get; set; } = 0;
        public int RealLevel { get; set; } = 0;

        public TitleNode ParentNode = null;
        public string FileName { get; set; }

    }
}