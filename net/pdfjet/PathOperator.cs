namespace PDFjet.NET {
/// <summary>The operators that paint a path, for example in Page.DrawPath.</summary>
public enum PathOperator {
    /// <summary>Strokes the path.</summary>
    Stroke,
    /// <summary>Closes and strokes the path.</summary>
    CloseAndStroke,
    /// <summary>Closes and fills the path.</summary>
    Fill,
    /// <summary>Closes, fills and strokes the path.</summary>
    FillAndStroke,
    /// <summary>Fills the path using the even-odd rule.</summary>
    FillUsingEvenOddRule,
    /// <summary>Closes, fills using the even-odd rule and strokes the path.</summary>
    FillUsingEvenOddRuleAndStroke
}

internal static class PathOperatorExtensions {
    // The operator written to the content stream.
    internal static string ToOperator(this PathOperator pathOperator) {
        switch (pathOperator) {
            case PathOperator.Stroke: return "S";
            case PathOperator.CloseAndStroke: return "s";
            case PathOperator.Fill: return "f";
            case PathOperator.FillAndStroke: return "b";
            case PathOperator.FillUsingEvenOddRule: return "f*";
            case PathOperator.FillUsingEvenOddRuleAndStroke: return "b*";
            default: throw new System.ArgumentException("Invalid path operator: " + pathOperator);
        }
    }
}
}
