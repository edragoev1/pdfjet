/// The operators that paint a path, for example in Page.drawPath.
public enum PathOperator: String {
    case stroke = "S"                         // Stroke the path
    case closeAndStroke = "s"                 // Close and then stroke the path
    case fill = "f"                           // Close and fill the path
    case fillAndStroke = "b"                  // Close, fill and then stroke the path
    case fillUsingEvenOddRule = "f*"          // Like 'f' but using even odd rule
    case fillUsingEvenOddRuleAndStroke = "b*" // Like 'b' but using even odd rule
}
