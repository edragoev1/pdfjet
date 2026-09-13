package com.pdfjet;

/**
 * The operators that paint a path, for example in Page.drawPath.
 */
public enum PathOperator {
    /** Strokes the path. */
    STROKE("S"),
    /** Closes and then strokes the path. */
    CLOSE_AND_STROKE("s"),
    /** Closes and fills the path. */
    FILL("f"),
    /** Closes, fills and then strokes the path. */
    FILL_AND_STROKE("b"),
    /** Like FILL, but uses the even-odd rule. */
    FILL_USING_EVEN_ODD_RULE("f*"),
    /** Like FILL_AND_STROKE, but uses the even-odd rule. */
    FILL_USING_EVEN_ODD_RULE_AND_STROKE("b*");

    /** The operator written to the content stream. */
    final String operator;

    PathOperator(String operator) {
        this.operator = operator;
    }
}
