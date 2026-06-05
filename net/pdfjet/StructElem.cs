/**
 * StructElem.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

/**
 * Defines the StructElem types.
 */
namespace PDFjet.NET {
    public class StructElem {
        // Document structure
        public const string DOCUMENT = "Document";
        public const string PART = "Part";
        public const string DIV = "Div";
        public const string SECT = "Sect";

        // Headings
        public const string H1 = "H1";
        public const string H2 = "H2";
        public const string H3 = "H3";
        public const string H4 = "H4";
        public const string H5 = "H5";
        public const string H6 = "H6";

        // Paragraphs and text
        public const string P = "P";
        public const string TITLE = "Title";
        public const string LBL = "Lbl";

        // Inline text
        public const string SPAN = "Span";
        public const string EM = "Em";
        public const string STRONG = "Strong";

        // Links and annotations
        public const string LINK = "Link";
        public const string ANNOT = "Annot";

        // Lists
        public const string L = "L";
        public const string LI = "LI";

        // Tables
        public const string TABLE = "Table";
        public const string TR = "TR";
        public const string TH = "TH";
        public const string TD = "TD";
        public const string THEAD = "THead";
        public const string TBODY = "TBody";
        public const string TFOOT = "TFoot";
        public const string CAPTION = "Caption";

        // Figures
        public const string FIGURE = "Figure";
        public const string ARTIFACT = "Artifact";

        internal int objNumber;
        internal String structure = null;
        internal int pageObjNumber;
        internal int mcid = 0;
        internal String language = "en-US";
        internal String actualText = null;
        internal String altDescription = null;
        internal Annotation annotation = null;
    }
}   // End of namespace PDFjet.NET
