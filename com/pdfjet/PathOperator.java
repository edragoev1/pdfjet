package com.pdfjet;

/**
 * The operators that paint a path, for example in Page.drawPath.
 */
public class PathOperator {
    /** The default constructor */
    public PathOperator() {
    }

    /** Strokes the path. */
    public static final String STROKE = "S";
    /** Closes and then strokes the path. */
    public static final String CLOSE_AND_STROKE = "s";
    /** Closes and fills the path. */
    public static final String FILL = "f";
    /** Closes, fills and then strokes the path. */
    public static final String FILL_AND_STROKE = "b";
    /** Like FILL, but uses the even-odd rule. */
    public static final String FILL_USING_EVEN_ODD_RULE = "f*";
    /** Like FILL_AND_STROKE, but uses the even-odd rule. */
    public static final String FILL_USING_EVEN_ODD_RULE_AND_STROKE = "b*";
}
