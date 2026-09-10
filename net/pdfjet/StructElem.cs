/*
 * StructElem.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// Defines the StructElem types.
    /// </summary>
    public class StructElem {
        // Document structure
        /// <summary>The whole document.</summary>
        public const string DOCUMENT = "Document";
        /// <summary>A large division of the document.</summary>
        public const string PART = "Part";
        /// <summary>A generic block-level element.</summary>
        public const string DIV = "Div";
        /// <summary>A section.</summary>
        public const string SECT = "Sect";

        // Headings
        /// <summary>A level 1 heading.</summary>
        public const string H1 = "H1";
        /// <summary>A level 2 heading.</summary>
        public const string H2 = "H2";
        /// <summary>A level 3 heading.</summary>
        public const string H3 = "H3";
        /// <summary>A level 4 heading.</summary>
        public const string H4 = "H4";
        /// <summary>A level 5 heading.</summary>
        public const string H5 = "H5";
        /// <summary>A level 6 heading.</summary>
        public const string H6 = "H6";

        // Paragraphs and text
        /// <summary>A paragraph.</summary>
        public const string P = "P";
        /// <summary>A title.</summary>
        public const string TITLE = "Title";
        /// <summary>A label, such as the bullet or number of a list item.</summary>
        public const string LBL = "Lbl";

        // Inline text
        /// <summary>A generic inline element.</summary>
        public const string SPAN = "Span";
        /// <summary>Emphasized text.</summary>
        public const string EM = "Em";
        /// <summary>Strongly emphasized text.</summary>
        public const string STRONG = "Strong";

        // Links and annotations
        /// <summary>A link.</summary>
        public const string LINK = "Link";
        /// <summary>An annotation.</summary>
        public const string ANNOT = "Annot";

        // Lists
        /// <summary>A list.</summary>
        public const string L = "L";
        /// <summary>A list item.</summary>
        public const string LI = "LI";

        // Tables
        /// <summary>A table.</summary>
        public const string TABLE = "Table";
        /// <summary>A table row.</summary>
        public const string TR = "TR";
        /// <summary>A table header cell.</summary>
        public const string TH = "TH";
        /// <summary>A table data cell.</summary>
        public const string TD = "TD";
        /// <summary>A group of table header rows.</summary>
        public const string THEAD = "THead";
        /// <summary>A group of table body rows.</summary>
        public const string TBODY = "TBody";
        /// <summary>A group of table footer rows.</summary>
        public const string TFOOT = "TFoot";
        /// <summary>A table caption.</summary>
        public const string CAPTION = "Caption";

        // Figures
        /// <summary>A figure.</summary>
        public const string FIGURE = "Figure";
        /// <summary>Content that is not part of the document structure, such as page numbers.</summary>
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
