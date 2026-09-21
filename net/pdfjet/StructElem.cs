/*
 * StructElem.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
namespace PDFjet.NET {
/// <summary>
/// The structure element types of a tagged (PDF/UA) document, as in ISO 32000-1
/// section 14.8.4. A TextLine is a P by default; see TextLine.SetStructureType.
/// </summary>
public enum StructElem {
    /// <summary>The Document structure element.</summary>
    DOCUMENT,
    /// <summary>The Part structure element.</summary>
    PART,
    /// <summary>The Div structure element.</summary>
    DIV,
    /// <summary>The Sect structure element.</summary>
    SECT,
    /// <summary>The H1 structure element.</summary>
    H1,
    /// <summary>The H2 structure element.</summary>
    H2,
    /// <summary>The H3 structure element.</summary>
    H3,
    /// <summary>The H4 structure element.</summary>
    H4,
    /// <summary>The H5 structure element.</summary>
    H5,
    /// <summary>The H6 structure element.</summary>
    H6,
    /// <summary>The P structure element.</summary>
    P,
    /// <summary>The Title structure element.</summary>
    TITLE,
    /// <summary>The Lbl structure element.</summary>
    LBL,
    /// <summary>The Span structure element.</summary>
    SPAN,
    /// <summary>The Em structure element.</summary>
    EM,
    /// <summary>The Strong structure element.</summary>
    STRONG,
    /// <summary>The Link structure element.</summary>
    LINK,
    /// <summary>The Annot structure element.</summary>
    ANNOT,
    /// <summary>The L structure element.</summary>
    L,
    /// <summary>The LI structure element.</summary>
    LI,
    /// <summary>The LBody structure element, the body of a list item.</summary>
    LBODY,
    /// <summary>The Table structure element.</summary>
    TABLE,
    /// <summary>The TR structure element.</summary>
    TR,
    /// <summary>The TH structure element.</summary>
    TH,
    /// <summary>The TD structure element.</summary>
    TD,
    /// <summary>The THead structure element.</summary>
    THEAD,
    /// <summary>The TBody structure element.</summary>
    TBODY,
    /// <summary>The TFoot structure element.</summary>
    TFOOT,
    /// <summary>The Caption structure element.</summary>
    CAPTION,
    /// <summary>The Figure structure element.</summary>
    FIGURE,
    /// <summary>The Artifact structure element.</summary>
    ARTIFACT
}

internal static class StructElemExtensions {
    private static readonly string[] names = {
        "Document",
        "Part",
        "Div",
        "Sect",
        "H1",
        "H2",
        "H3",
        "H4",
        "H5",
        "H6",
        "P",
        "Title",
        "Lbl",
        "Span",
        "Em",
        "Strong",
        "Link",
        "Annot",
        "L",
        "LI",
        "LBody",
        "Table",
        "TR",
        "TH",
        "TD",
        "THead",
        "TBody",
        "TFoot",
        "Caption",
        "Figure",
        "Artifact",
    };

    // The name of the structure element type in the PDF.
    internal static string Type(this StructElem structElem) {
        return names[(int) structElem];
    }
}
}   // End of namespace PDFjet.NET
