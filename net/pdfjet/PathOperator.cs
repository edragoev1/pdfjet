namespace PDFjet.NET {
/// <summary>The operators that paint a path, for example in Page.DrawPath.</summary>
public static class PathOperator {
    /// <summary>Strokes the path.</summary>
    public static readonly string Stroke = "S";                         // Stroke the path
    /// <summary>Closes and strokes the path.</summary>
    public static readonly string CloseAndStroke = "s";                 // Close and then stroke the path
    /// <summary>Closes and fills the path.</summary>
    public static readonly string Fill = "f";                           // Close and fill the path
    /// <summary>Closes, fills and strokes the path.</summary>
    public static readonly string FillAndStroke = "b";                  // Close, fill and then stroke the path
    /// <summary>Fills the path using the even-odd rule.</summary>
    public static readonly string FillUsingEvenOddRule = "f*";          // Like 'f' but using even odd rule
    /// <summary>Closes, fills using the even-odd rule and strokes the path.</summary>
    public static readonly string FillUsingEvenOddRuleAndStroke = "b*"; // Like 'b' but using even odd rule
}
}
