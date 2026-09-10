package com.pdfjet;

import com.pdfjet.corefonts.*;

/**
 * The metrics of the 14 standard PDF fonts.
 */
public class CoreFont {
    /** Courier. */
    public static final int COURIER = 1;
    /** Courier Bold. */
    public static final int COURIER_BOLD = 2;
    /** Courier Oblique. */
    public static final int COURIER_OBLIQUE = 3;
    /** Courier Bold Oblique. */
    public static final int COURIER_BOLD_OBLIQUE = 4;
    /** Helvetica. */
    public static final int HELVETICA = 5;
    /** Helvetica Bold. */
    public static final int HELVETICA_BOLD = 6;
    /** Helvetica Oblique. */
    public static final int HELVETICA_OBLIQUE = 7;
    /** Helvetica Bold Oblique. */
    public static final int HELVETICA_BOLD_OBLIQUE = 8;
    /** Times Roman. */
    public static final int TIMES_ROMAN = 9;
    /** Times Bold. */
    public static final int TIMES_BOLD = 10;
    /** Times Italic. */
    public static final int TIMES_ITALIC = 11;
    /** Times Bold Italic. */
    public static final int TIMES_BOLD_ITALIC = 12;
    /** Symbol. */
    public static final int SYMBOL = 13;
    /** Zapf Dingbats. */
    public static final int ZAPF_DINGBATS = 14;

    /** The PostScript name of the font. */
    protected String name;
    /** The x coordinate of the lower left corner of the font bounding box. */
    protected int bBoxLLx;
    /** The y coordinate of the lower left corner of the font bounding box. */
    protected int bBoxLLy;
    /** The x coordinate of the upper right corner of the font bounding box. */
    protected int bBoxURx;
    /** The y coordinate of the upper right corner of the font bounding box. */
    protected int bBoxURy;
    /** The position of the underline relative to the baseline. */
    protected int underlinePosition;
    /** The thickness of the underline. */
    protected int underlineThickness;
    /** The character widths and kerning pairs. */
    protected int[][] metrics;

    /**
     * Loads the metrics of the specified standard font.
     *
     * @param coreFont one of the font constants in this class, for example CoreFont.HELVETICA.
     */
    public CoreFont(int coreFont) {
        switch (coreFont) {
            case 1: // CoreFont.COURIER
            this.name = Courier.name;
            this.bBoxLLx = Courier.bBoxLLx;
            this.bBoxLLy = Courier.bBoxLLy;
            this.bBoxURx = Courier.bBoxURx;
            this.bBoxURy = Courier.bBoxURy;
            this.underlinePosition = Courier.underlinePosition;
            this.underlineThickness = Courier.underlineThickness;
            this.metrics = Courier.metrics;
            break;

            case 2: // CoreFont.COURIER_BOLD
            this.name = Courier_Bold.name;
            this.bBoxLLx = Courier_Bold.bBoxLLx;
            this.bBoxLLy = Courier_Bold.bBoxLLy;
            this.bBoxURx = Courier_Bold.bBoxURx;
            this.bBoxURy = Courier_Bold.bBoxURy;
            this.underlinePosition = Courier_Bold.underlinePosition;
            this.underlineThickness = Courier_Bold.underlineThickness;
            this.metrics = Courier_Bold.metrics;
            break;

            case 3: // CoreFont.COURIER_OBLIQUE
            this.name = Courier_Oblique.name;
            this.bBoxLLx = Courier_Oblique.bBoxLLx;
            this.bBoxLLy = Courier_Oblique.bBoxLLy;
            this.bBoxURx = Courier_Oblique.bBoxURx;
            this.bBoxURy = Courier_Oblique.bBoxURy;
            this.underlinePosition = Courier_Oblique.underlinePosition;
            this.underlineThickness = Courier_Oblique.underlineThickness;
            this.metrics = Courier_Oblique.metrics;
            break;

            case 4: // CoreFont.COURIER_BOLD_OBLIQUE
            this.name = Courier_BoldOblique.name;
            this.bBoxLLx = Courier_BoldOblique.bBoxLLx;
            this.bBoxLLy = Courier_BoldOblique.bBoxLLy;
            this.bBoxURx = Courier_BoldOblique.bBoxURx;
            this.bBoxURy = Courier_BoldOblique.bBoxURy;
            this.underlinePosition = Courier_BoldOblique.underlinePosition;
            this.underlineThickness = Courier_BoldOblique.underlineThickness;
            this.metrics = Courier_BoldOblique.metrics;
            break;

            case 5: // case CoreFont.HELVETICA:
            this.name = Helvetica.name;
            this.bBoxLLx = Helvetica.bBoxLLx;
            this.bBoxLLy = Helvetica.bBoxLLy;
            this.bBoxURx = Helvetica.bBoxURx;
            this.bBoxURy = Helvetica.bBoxURy;
            this.underlinePosition = Helvetica.underlinePosition;
            this.underlineThickness = Helvetica.underlineThickness;
            this.metrics = Helvetica.metrics;
            break;

            case 6: // CoreFont.HELVETICA_BOLD:
            this.name = Helvetica_Bold.name;
            this.bBoxLLx = Helvetica_Bold.bBoxLLx;
            this.bBoxLLy = Helvetica_Bold.bBoxLLy;
            this.bBoxURx = Helvetica_Bold.bBoxURx;
            this.bBoxURy = Helvetica_Bold.bBoxURy;
            this.underlinePosition = Helvetica_Bold.underlinePosition;
            this.underlineThickness = Helvetica_Bold.underlineThickness;
            this.metrics = Helvetica_Bold.metrics;
            break;

            case 7: // CoreFont.HELVETICA_OBLIQUE:
            this.name = Helvetica_Oblique.name;
            this.bBoxLLx = Helvetica_Oblique.bBoxLLx;
            this.bBoxLLy = Helvetica_Oblique.bBoxLLy;
            this.bBoxURx = Helvetica_Oblique.bBoxURx;
            this.bBoxURy = Helvetica_Oblique.bBoxURy;
            this.underlinePosition = Helvetica_Oblique.underlinePosition;
            this.underlineThickness = Helvetica_Oblique.underlineThickness;
            this.metrics = Helvetica_Oblique.metrics;
            break;

            case 8: // CoreFont.HELVETICA_BOLD_OBLIQUE:
            this.name = Helvetica_BoldOblique.name;
            this.bBoxLLx = Helvetica_BoldOblique.bBoxLLx;
            this.bBoxLLy = Helvetica_BoldOblique.bBoxLLy;
            this.bBoxURx = Helvetica_BoldOblique.bBoxURx;
            this.bBoxURy = Helvetica_BoldOblique.bBoxURy;
            this.underlinePosition = Helvetica_BoldOblique.underlinePosition;
            this.underlineThickness = Helvetica_BoldOblique.underlineThickness;
            this.metrics = Helvetica_BoldOblique.metrics;
            break;

            case 9: // CoreFont.TIMES_ROMAN:
            this.name = Times_Roman.name;
            this.bBoxLLx = Times_Roman.bBoxLLx;
            this.bBoxLLy = Times_Roman.bBoxLLy;
            this.bBoxURx = Times_Roman.bBoxURx;
            this.bBoxURy = Times_Roman.bBoxURy;
            this.underlinePosition = Times_Roman.underlinePosition;
            this.underlineThickness = Times_Roman.underlineThickness;
            this.metrics = Times_Roman.metrics;
            break;

            case 10: // CoreFont.TIMES_BOLD:
            this.name = Times_Bold.name;
            this.bBoxLLx = Times_Bold.bBoxLLx;
            this.bBoxLLy = Times_Bold.bBoxLLy;
            this.bBoxURx = Times_Bold.bBoxURx;
            this.bBoxURy = Times_Bold.bBoxURy;
            this.underlinePosition = Times_Bold.underlinePosition;
            this.underlineThickness = Times_Bold.underlineThickness;
            this.metrics = Times_Bold.metrics;
            break;

            case 11: // CoreFont.TIMES_ITALIC:
            this.name = Times_Italic.name;
            this.bBoxLLx = Times_Italic.bBoxLLx;
            this.bBoxLLy = Times_Italic.bBoxLLy;
            this.bBoxURx = Times_Italic.bBoxURx;
            this.bBoxURy = Times_Italic.bBoxURy;
            this.underlinePosition = Times_Italic.underlinePosition;
            this.underlineThickness = Times_Italic.underlineThickness;
            this.metrics = Times_Italic.metrics;
            break;

            case 12: // CoreFont.TIMES_BOLD_ITALIC:
            this.name = Times_BoldItalic.name;
            this.bBoxLLx = Times_BoldItalic.bBoxLLx;
            this.bBoxLLy = Times_BoldItalic.bBoxLLy;
            this.bBoxURx = Times_BoldItalic.bBoxURx;
            this.bBoxURy = Times_BoldItalic.bBoxURy;
            this.underlinePosition = Times_BoldItalic.underlinePosition;
            this.underlineThickness = Times_BoldItalic.underlineThickness;
            this.metrics = Times_BoldItalic.metrics;
            break;

            case 13: // CoreFont.SYMBOL:
            this.name = Symbol.name;
            this.bBoxLLx = Symbol.bBoxLLx;
            this.bBoxLLy = Symbol.bBoxLLy;
            this.bBoxURx = Symbol.bBoxURx;
            this.bBoxURy = Symbol.bBoxURy;
            this.underlinePosition = Symbol.underlinePosition;
            this.underlineThickness = Symbol.underlineThickness;
            this.metrics = Symbol.metrics;
            break;

            case 14: // CoreFont.ZAPF_DINGBATS:
            this.name = ZapfDingbats.name;
            this.bBoxLLx = ZapfDingbats.bBoxLLx;
            this.bBoxLLy = ZapfDingbats.bBoxLLy;
            this.bBoxURx = ZapfDingbats.bBoxURx;
            this.bBoxURy = ZapfDingbats.bBoxURy;
            this.underlinePosition = ZapfDingbats.underlinePosition;
            this.underlineThickness = ZapfDingbats.underlineThickness;
            this.metrics = ZapfDingbats.metrics;
            break;
        }
    }
}
