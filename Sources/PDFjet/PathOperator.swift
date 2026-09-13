/// The operators that paint a path, for example in Page.drawPath.
public enum PathOperator: String {
    case STROKE = "S"                         // Stroke the path
    case CLOSE_AND_STROKE = "s"                 // Close and then stroke the path
    case FILL = "f"                           // Close and fill the path
    case FILL_AND_STROKE = "b"                  // Close, fill and then stroke the path
    case FILL_USING_EVEN_ODD_RULE = "f*"          // Like 'f' but using even odd rule
    case FILL_USING_EVEN_ODD_RULE_AND_STROKE = "b*" // Like 'b' but using even odd rule
}
