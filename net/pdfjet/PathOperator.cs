/*
 * PathOperator.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
namespace PDFjet.NET {
/// <summary>The operators that paint a path, for example in Page.DrawPath.</summary>
public enum PathOperator {
    /// <summary>Strokes the path.</summary>
    STROKE,
    /// <summary>Closes and strokes the path.</summary>
    CLOSE_AND_STROKE,
    /// <summary>Closes and fills the path.</summary>
    FILL,
    /// <summary>Closes, fills and strokes the path.</summary>
    FILL_AND_STROKE,
    /// <summary>Fills the path using the even-odd rule.</summary>
    FILL_USING_EVEN_ODD_RULE,
    /// <summary>Closes, fills using the even-odd rule and strokes the path.</summary>
    FILL_USING_EVEN_ODD_RULE_AND_STROKE
}

internal static class PathOperatorExtensions {
    // The operator written to the content stream.
    internal static string ToOperator(this PathOperator pathOperator) {
        switch (pathOperator) {
            case PathOperator.STROKE: return "S";
            case PathOperator.CLOSE_AND_STROKE: return "s";
            case PathOperator.FILL: return "f";
            case PathOperator.FILL_AND_STROKE: return "b";
            case PathOperator.FILL_USING_EVEN_ODD_RULE: return "f*";
            case PathOperator.FILL_USING_EVEN_ODD_RULE_AND_STROKE: return "b*";
            default: throw new System.ArgumentException("Invalid path operator: " + pathOperator);
        }
    }
}
}
