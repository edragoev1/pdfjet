/**
 * Page.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

/**
 * Used to create PDF page objects.
 *
 * Please note:
 * <pre>
 * The coordinate (0f, 0f) is the top left corner of the page.
 * The size of the pages are represented in points.
 * 1 point is 1/72 inches.
 * </pre>
 */
final public class Page {
    protected PDF pdf;
    protected PDFobj pageObj;
    protected int objNumber;
    protected ByteArrayOutputStream buf;

    protected int renderingMode = 0;
    protected float width;
    protected float height;

    protected float[] cropBox = null;
    protected float[] bleedBox = null;
    protected float[] trimBox = null;
    protected float[] artBox = null;

    private float[] brushColor = {0f, 0f, 0f};
    private float[] penColor = {0f, 0f, 0f};

    private float[] tmx = new float[] {1f, 0f, 0f, 1f};
    private byte[] tm0;   // Used for caching tm values
    private byte[] tm1;
    private byte[] tm2;
    private byte[] tm3;

    private float penWidth = 0.5f;

    private CapStyle lineCapStyle = CapStyle.BUTT;
    private JoinStyle lineJoinStyle = JoinStyle.MITER;
    private String strokeDashPattern = "[] 0";

    protected float rotateDegrees = 0f;

    protected final List<Integer> contents = new ArrayList<Integer>();
    protected final List<Annotation> annots = new ArrayList<Annotation>();
    protected final List<Destination> destinations= new ArrayList<Destination>();

    private int mcid = 0;

    /*
     * From Android's Matrix object:
     */
    public static final int MSCALE_X = 0;
    public static final int MSKEW_X  = 1;
    public static final int MTRANS_X = 2;
    public static final int MSKEW_Y  = 3;
    public static final int MSCALE_Y = 4;
    public static final int MTRANS_Y = 5;

    public static final boolean DETACHED = false;

    /**
     *  Creates page object and add it to the PDF document.
     *
     *  Please note:
     *  <pre>
     *  The coordinate (0f, 0f) is the top left corner of the page.
     *  The size of the pages are represented in points.
     *  1 point is 1/72 inches.
     *  </pre>
     *
     *  @param pdf the pdf object.
     *  @param pageSize the page size of this page.
     *  @throws Exception  If an input or output
     *                     exception occurred
     */
    public Page(PDF pdf, float[] pageSize) throws Exception {
        this(pdf, pageSize, true);
    }

    /**
     *  Creates page object and add it to the PDF document.
     *
     *  Please note:
     *  <pre>
     *  The coordinate (0f, 0f) is the top left corner of the page.
     *  The size of the pages are represented in points.
     *  1 point is 1/72 inches.
     *  </pre>
     *
     *  @param pdf the pdf object.
     *  @param pageSize the page size of this page.
     *  @param addPageToPDF boolean flag.
     *  @throws Exception  If an input or output exception occurred
     */
    public Page(PDF pdf, float[] pageSize, boolean addPageToPDF) throws Exception {
        this.pdf = pdf;
        width = pageSize[0];
        height = pageSize[1];
        buf = new ByteArrayOutputStream(8192);
        tm0 = FastFloat.toByteArray(tmx[0]);
        tm1 = FastFloat.toByteArray(tmx[1]);
        tm2 = FastFloat.toByteArray(tmx[2]);
        tm3 = FastFloat.toByteArray(tmx[3]);
        if (addPageToPDF) {
            pdf.addPage(this);
        }
    }

    public Page(PDF pdf, PDFobj pageObj) {
        this.pdf = pdf;
        this.pageObj = removeComments(pageObj);
        width = pageObj.getPageSize()[0];
        height = pageObj.getPageSize()[1];
        buf = new ByteArrayOutputStream(8192);
        tm0 = FastFloat.toByteArray(tmx[0]);
        tm1 = FastFloat.toByteArray(tmx[1]);
        tm2 = FastFloat.toByteArray(tmx[2]);
        tm3 = FastFloat.toByteArray(tmx[3]);
        this.saveGraphicsState();
        if (pageObj.gsNumber != -1) {
            append("/GS");
            append(pageObj.gsNumber + 1);
            append(" gs\n");
        }
    }

    private PDFobj removeComments(PDFobj obj) {
        List<String> list = new ArrayList<String>();
        boolean comment = false;
        for (String token : obj.dict) {
            if (token.equals("%")) {
                comment = true;
            } else {
                if (token.startsWith("/")) {
                    comment = false;
                    list.add(token);
                } else {
                    if (!comment) {
                        list.add(token);
                    }
                }
            }
        }
        obj.dict = list;
        return obj;
    }

    /**
     * Adds core font resource to the page.
     *
     * @param coreFont the core font.
     * @param objects the objects list.
     * @return the font object.
     */
    public Font addResource(int coreFont, List<PDFobj> objects) {
        return pageObj.addResource(coreFont, objects);
    }

    /**
     * Adds image resource to the page.
     *
     * @param image the image.
     * @param objects the page objects.
     */
    public void addResource(Image image, List<PDFobj> objects) {
        pageObj.addResource(image, objects);
    }

    /**
     * Adds resource to the page.
     *
     * @param font the font.
     * @param objects the objects list.
     */
    public void addResource(Font font, List<PDFobj> objects) {
        pageObj.addResource(font, objects);
    }

    /**
     * Completes the page.
     *
     * @param objects list of the page objects.
     */
    public void complete(List<PDFobj> objects) {
        restoreGraphicsState();
        pageObj.addContent(getContent(), objects);
    }

    /**
     * Returns the page content.
     *
     * @return the page content.
     */
    public byte[] getContent() {
        return buf.toByteArray();
    }

    /**
     *  Adds destination to this page.
     *
     *  @param name The destination name.
     *  @param xPosition The horizontal position of the destination on this page.
     *  @param yPosition The vertical position of the destination on this page.
     *
     *  @return the destination.
     */
    public Destination addDestination(String name, float xPosition, float yPosition) {
        Destination dest = new Destination(name, xPosition, height - yPosition);
        destinations.add(dest);
        return dest;
    }

    /**
     *  Adds destination to this page.
     *
     *  @param name The destination name.
     *  @param yPosition The vertical position of the destination on this page.
     *
     *  @return the destination.
     */
    public Destination addDestination(String name, float yPosition) {
        Destination dest = new Destination(name, 0f, height - yPosition);
        destinations.add(dest);
        return dest;
    }

    /**
     *  Returns the width of this page.
     *
     *  @return the width of the page.
     */
    public float getWidth() {
        return width;
    }

    /**
     *  Returns the height of this page.
     *
     *  @return the height of the page.
     */
    public float getHeight() {
        return height;
    }

    /**
     *  Draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
     *
     *  @param x1 the first point's x coordinate.
     *  @param y1 the first point's y coordinate.
     *  @param x2 the second point's x coordinate.
     *  @param y2 the second point's y coordinate.
     */
    public void drawLine(
            double x1,
            double y1,
            double x2,
            double y2) {
        drawLine((float) x1, (float) y1, (float) x2, (float) y2);
    }

    /**
     *  Draws a line on the page, using the current color, between the points (x1, y1) and (x2, y2).
     *
     *  @param x1 the first point's x coordinate.
     *  @param y1 the first point's y coordinate.
     *  @param x2 the second point's x coordinate.
     *  @param y2 the second point's y coordinate.
     */
    public void drawLine(
            float x1,
            float y1,
            float x2,
            float y2) {
        moveTo(x1, y1);
        lineTo(x2, y2);
        strokePath();
    }

    /**
     *  Draws string on the page using the specified fonts and coordinates.
     *
     *  @param font the primary font.
     *  @param fallbackFont the fallback font.
     *  @param fontSize the font size.
     *  @param str the string.
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     */
    public void drawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y) {
        drawString(font, fallbackFont, fontSize, str, x, y, new float[] {0f, 0f, 0f}, null);
    }

    public void drawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y,
            Integer color,
            Map<String, Integer> colors) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        drawString(font, fallbackFont, fontSize, str, x, y, new float[] {r, g, b}, colors);
    }

    /**
     *  Draws the text given by the specified string,
     *  using the specified main font and the current brush color.
     *  If the main font is missing some glyphs - the fallback font is used.
     *  The baseline of the leftmost character is at position (x, y) on the page.
     *
     *  @param font the main font.
     *  @param fallbackFont the fallback font.
     *  @param fontSize the font size.
     *  @param str the string to be drawn.
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     *  @param textColor the text color.
     *  @param highlightColors map used to highlight specific words.
     */
    public void drawString(
            Font font,
            Font fallbackFont,
            float fontSize,
            String str,
            float x,
            float y,
            float[] textColor,
            Map<String, Integer> highlightColors) {
        if (font.isCoreFont ||
                font.isCJK ||
                fallbackFont == null ||
                fallbackFont.isCoreFont ||
                fallbackFont.isCJK) {
            drawString(font, fontSize, str, x, y, textColor, highlightColors);
        } else {
            Font activeFont = font;
            StringBuilder buf = new StringBuilder();
            for (int i = 0; i < str.length(); i++) {
                int ch = str.charAt(i);
                if (activeFont.unicodeToGID[ch] == 0) {
                    drawString(activeFont, fontSize, buf.toString(), x, y, textColor, highlightColors);
                    x += activeFont.stringWidth(fontSize, buf.toString());
                    buf.setLength(0);
                    // Switch the active font
                    if (activeFont == font) {
                        activeFont = fallbackFont;
                    } else {
                        activeFont = font;
                    }
                }
                buf.append((char) ch);
            }
            drawString(activeFont, fontSize, buf.toString(), x, y, textColor, highlightColors);
        }
    }

    /**
     *  Draws the text given by the specified string,
     *  using the specified font and the current brush color.
     *  The baseline of the leftmost character is at position (x, y) on the page.
     *
     *  @param font the font.
     *  @param fontSize the font size.
     *  @param str the string.
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     */
    public void drawString(
            Font font,
            double fontSize,
            String str,
            double x,
            double y) {
        drawString(font, (float) fontSize, str, (float) x, (float) y);
    }

    public void drawString(
            Font font,
            float fontSize,
            String str,
            float x,
            float y) {
        drawString(font, fontSize, str, x, y, new float[] {0f, 0f, 0f}, null);
    }

    /**
     *  Draws the text given by the specified string,
     *  using the specified font and the current brush color.
     *  The baseline of the leftmost character is at position (x, y) on the page.
     *
     *  @param font the font to use.
     *  @param fontSize the font size.
     *  @param str the string to be drawn.
     *  @param x the x coordinate.
     *  @param y the y coordinate.
     *  @param textColor the text color.
     *  @param highlightColors map used to highlight specific words.
     */
    public void drawString(
            Font font,
            float fontSize,
            String str,
            float x,
            float y,
            float[] textColor,
            Map<String, Integer> highlightColors) {
        if (str == null || str.isEmpty()) {
            return;
        }

        append("BT\n");
        setTextFont(font, fontSize);
        if (renderingMode != 0) {
            append(renderingMode);
            append(" Tr\n");
        }

        if (font.skew15 &&
                tmx[0] == 1f &&
                tmx[1] == 0f &&
                tmx[2] == 0f &&
                tmx[3] == 1f) {
            float skew = 0.26f;
            append(tmx[0]);
            append(' ');
            append(tmx[1]);
            append(' ');
            append(tmx[2] + skew);
            append(' ');
            append(tmx[3]);
        } else {
            append(tm0);
            append(' ');
            append(tm1);
            append(' ');
            append(tm2);
            append(' ');
            append(tm3);
        }
        append(' ');
        append(x);
        append(' ');
        append(height - y);
        append(" Tm\n");

        if (highlightColors == null) {
            setBrushColor(textColor);
            if (font.isCoreFont) {
                append("[<");
                drawASCIIString(font, str);
                append(">] TJ\n");  // Need TJ for kerning
            } else {
                append("<");
                drawUnicodeString(font, str);
                append("> Tj\n");
            }

        } else {
            drawColoredString(font, str, textColor, highlightColors);
        }
        append("ET\n");
    }

    private void drawASCIIString(Font font, String str) {
        for (int i = 0; i < str.length(); i++) {
            int c1 = str.charAt(i);
            if (c1 < font.firstChar || c1 > font.lastChar) {
                append(String.format("%02X", 0x20));
                continue;
            }
            append(String.format("%02X", c1));
            if (font.isCoreFont && font.kernPairs && i < (str.length() - 1)) {
                c1 -= 32;
                int c2 = str.charAt(i + 1);
                if (c2 < font.firstChar || c2 > font.lastChar) {
                    c2 = 32;
                }
                for (int j = 2; j < font.metrics[c1].length; j += 2) {
                    if (font.metrics[c1][j] == c2) {
                        append(">");
                        append(-font.metrics[c1][j + 1]);
                        append("<");
                        break;
                    }
                }
            }
        }
    }

    public void drawUnicodeString(Font font, String str) {
        if (str == null || str.isEmpty()) {
            return;
        }
        if (font.isCJK) {
            str.codePoints().forEach(cp -> {
                if (cp != 0xFEFF) {     // BOM
                    if (cp < font.firstChar || cp > font.lastChar) {
                        appendCodePointAsHex(0x0020);
                    } else {
                        appendCodePointAsHex(cp);
                    }
                }
            });
        } else {
            str.codePoints().forEach(cp -> {
                if (cp != 0xFEFF) {     //BOM
                    if (cp < font.firstChar || cp > font.lastChar) {
                        appendCodePointAsHex(font.unicodeToGID[0x0020]);
                    } else {
                        appendCodePointAsHex(font.unicodeToGID[cp]);
                    }
                }
            });
        }
    }

//     /**
//      * Sets the color for stroking operations.
//      * The pen color is used when drawing lines and splines.
//      *
//      * @param r the red component is float value from 0.0 to 1.0.
//      * @param g the green component is float value from 0.0 to 1.0.
//      * @param b the blue component is float value from 0.0 to 1.0.
//      */
//     public void setPenColor(double r, double g, double b) {
//         setPenColor(new float[] {(float)r, (float)g, (float)b});
//     }
//
//     /**
//      * Sets the color for stroking operations.
//      * The pen color is used when drawing lines and splines.
//      *
//      * @param r the red component is float value from 0.0f to 1.0f.
//      * @param g the green component is float value from 0.0f to 1.0f.
//      * @param b the blue component is float value from 0.0f to 1.0f.
//      */
//     public void setPenColor(float r, float g, float b) {
//         setPenColor(new float[] {r, g, b});
//     }
//
//     /**
//      * Sets the color for brush operations.
//      * This is the color used when drawing regular text and filling shapes.
//      *
//      * @param r the red component is float value from 0.0 to 1.0.
//      * @param g the green component is float value from 0.0 to 1.0.
//      * @param b the blue component is float value from 0.0 to 1.0.
//      */
//     public void setBrushColor(double r, double g, double b) {
//         setBrushColor(new float[] {(float)r, (float)g, (float)b});
//     }
//
//     /**
//      * Sets the color for brush operations.
//      * This is the color used when drawing regular text and filling shapes.
//      *
//      * @param r the red component is float value from 0.0f to 1.0f.
//      * @param g the green component is float value from 0.0f to 1.0f.
//      * @param b the blue component is float value from 0.0f to 1.0f.
//      */
//     public void setBrushColor(float r, float g, float b) {
//         setBrushColor(new float[] {r, g, b});
//     }

    /**
     * Sets the brush color.
     *
     * @param color the color. See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
     */
    public void setBrushColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setBrushColor(new float[] {r, g, b});
    }

    /**
     * Sets the brush color using an RGB color array.
     *
     * @param rgbColor An array of three floats representing the red, green, and blue components of the color.
     *                 Each value should be between 0.0f and 1.0f. If the array is null or values are out of range,
     *                 the method logs a warning and does not change the brush color.
     */
    public void setBrushColor(float[] rgbColor) {
        if (rgbColor == null) {
            return; // Early exit if null
        }

        if (rgbColor[0] < 0f || rgbColor[0] > 1f ||
            rgbColor[1] < 0f || rgbColor[1] > 1f ||
            rgbColor[2] < 0f || rgbColor[2] > 1f) {
            PDF.LOG.warning("Warning: RGB color values must be between 0f and 1f. Ignoring request.");
            return; // Early exit if out of range
        }

        // Now set the brush color
        brushColor = rgbColor;

        // Proceed with setting the color (example)
        append(rgbColor[0]);
        append(Token.SPACE);
        append(rgbColor[1]);
        append(Token.SPACE);
        append(rgbColor[2]);
        append(" rg\n");
    }

    /**
     * Gets the current brush color.
     *
     * @return An array of three floats representing the RGB components of the brush color,
     *         with each value in the range 0.0f to 1.0f.
     */
    public float[] getBrushColor() {
        return brushColor;
    }

    /**
     * Sets the pen color using a packed RGB integer.
     * The integer should be in the format 0xRRGGBB.
     *
     * @param color A packed RGB integer where:
     *              - The 16 most significant bits represent the red component,
     *              - The next 8 bits represent the green component,
     *              - The least significant 8 bits represent the blue component.
     */
     public void setPenColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        setPenColor(new float[] {r, g, b});
    }

    /**
     * Sets the pen color using an RGB array.
     * The array must contain three float values, representing red, green, and blue components.
     * Each component should be in the range of 0.0f to 1.0f.
     *
     * @param rgbColor An array of three float values for red, green, and blue (0.0f to 1.0f).
     */
     public void setPenColor(float[] rgbColor) {
        if (rgbColor == null) {
            return; // Early exit if null
        }

        if (rgbColor[0] < 0f || rgbColor[0] > 1f ||
            rgbColor[1] < 0f || rgbColor[1] > 1f ||
            rgbColor[2] < 0f || rgbColor[2] > 1f) {
            PDF.LOG.warning("Warning: RGB color values must be between 0f and 1f. Ignoring request.");
            return; // Early exit if out of range
        }

        // Now set the pen color
        penColor = rgbColor;

        // Proceed with setting the color (example)
        append(rgbColor[0]);
        append(Token.SPACE);
        append(rgbColor[1]);
        append(Token.SPACE);
        append(rgbColor[2]);
        append(" RG\n");
    }

    /**
     * Retrieves the current pen color.
     *
     * @return An array of three floats representing the RGB components of the pen color.
     *         Each value is in the range of 0.0f to 1.0f.
     */
    public float[] getPenColor() {
        return penColor;
    }

    /**
     * Sets the brush color using CMYK components.
     * This color is applied when drawing regular text and filling shapes.
     *
     * @param c Cyan component (0.0f to 1.0f).
     * @param m Magenta component (0.0f to 1.0f).
     * @param y Yellow component (0.0f to 1.0f).
     * @param k Black component (0.0f to 1.0f).
     */
    public void setBrushColorCMYK(float c, float m, float y, float k) {
        append(c);
        append(' ');
        append(m);
        append(' ');
        append(y);
        append(' ');
        append(k);
        append(" k\n");
    }

    /**
     * Sets the pen color using CMYK components.
     * This color is used for stroking operations such as drawing lines and splines.
     *
     * @param c Cyan component (0.0f to 1.0f).
     * @param m Magenta component (0.0f to 1.0f).
     * @param y Yellow component (0.0f to 1.0f).
     * @param k Black component (0.0f to 1.0f).
     */
     public void setPenColorCMYK(float c, float m, float y, float k) {
        append(c);
        append(' ');
        append(m);
        append(' ');
        append(y);
        append(' ');
        append(k);
        append(" K\n");
    }

    /**
     * Sets the line width to the default.
     * The default is the finest line width.
     */
    public void setDefaultLineWidth() {
        append(0f);
        append(" w\n");
    }

    /**
     * The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
     * It is specified by a dash array and a dash phase.
     * The elements of the dash array are positive numbers that specify the lengths of
     * alternating dashes and gaps.
     * The dash phase specifies the distance into the dash pattern at which to start the dash.
     * The elements of both the dash array and the dash phase are expressed in user space units.
     * <pre>
     * Examples of line dash patterns:
     *
     *     "[Array] Phase"     Appearance          Description
     *     _______________     _________________   ____________________________________
     *     "[] 0"              -----------------   Solid line
     *     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
     *     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
     *     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
     *     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
     *     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
     * </pre>
     *
     * @param strokeDashPattern the line dash pattern.
     */
    public void setStrokeDashPattern(String strokeDashPattern) {
        this.strokeDashPattern = strokeDashPattern;
        append(strokeDashPattern);
        append(" d\n");
    }

    /**
     * Sets the default line dash pattern - solid line.
     */
    public void setDefaultLinePattern() {
        append("[] 0");
        append(" d\n");
    }

    /**
     * Sets the pen width that will be used to draw lines and splines on this page.
     *
     * @param width the pen width.
     */
    public void setPenWidth(double width) {
        setPenWidth((float) width);
    }

    /**
     * Sets the pen width that will be used to draw lines and splines on this page.
     *
     * @param width the pen width.
     */
    public void setPenWidth(float width) {
        this.penWidth = width;
        append(width);
        append(" w\n");
    }

    public float getPenWidth() {
        return penWidth;
    }

    /**
     * Sets the current line cap style.
     *
     * @param style the cap style of the current line.
     * Supported values: CapStyle.BUTT, CapStyle.ROUND and CapStyle.PROJECTING_SQUARE
     */
    public void setLineCapStyle(CapStyle style) {
        append(lineCapStyle.ordinal());
        append(" J\n");
    }

    /**
     * Sets the line join style.
     *
     * @param style the line join style code. Supported values: JoinStyle.MITER, JoinStyle.ROUND and JoinStyle.BEVEL
     */
    public void setLineJoinStyle(JoinStyle style) {
        append(lineJoinStyle.ordinal());
        append(" j\n");
    }

    /**
     * Moves the pen to the point with coordinates (x, y) on the page.
     *
     * @param x the x coordinate of new pen position.
     * @param y the y coordinate of new pen position.
     */
    public void moveTo(double x, double y) {
        moveTo((float) x, (float) y);
    }

    /**
     * Moves the pen to the point with coordinates (x, y) on the page.
     *
     * @param x the x coordinate of new pen position.
     * @param y the y coordinate of new pen position.
     */
    public void moveTo(float x, float y) {
        append(x);
        append(' ');
        append(height - y);
        append(" m\n");
    }

    /**
     * Draws a line from the current pen position to the point with coordinates (x, y),
     * using the current pen width and stroke color.
     * Make sure you call strokePath(), closePath() or fillPath() after the last call to this method.
     * @param x the x coordinate of the new pen position.
     * @param y the y coordinate of the new pen position.
     */
    public void lineTo(double x, double y) {
        lineTo((float) x, (float) y);
    }

    /**
     * Draws a line from the current pen position to the point with coordinates (x, y),
     * using the current pen width and stroke color.
     * Make sure you call strokePath(), closePath() or fillPath() after the last call to this method.
     * @param x the x coordinate of the new pen position.
     * @param y the y coordinate of the new pen position.
     */
    public void lineTo(float x, float y) {
        append(x);
        append(' ');
        append(height - y);
        append(" l\n");
    }

    /**
     * Draws the path using the current pen color.
     */
    public void strokePath() {
        append("S\n");
    }

    /**
     * Closes the path and draws it using the current pen color.
     */
    public void closePath() {
        append("s\n");
    }

    /**
     * Closes and fills the path with the current brush color.
     */
    public void fillPath() {
        append("f\n");
    }

    /**
     * Draws the outline of the specified rectangle on the page.
     * The left and right edges of the rectangle are at x and x + w.
     * The top and bottom edges are at y and y + h.
     * The rectangle is drawn using the current pen color.
     *
     * @param x the x coordinate of the rectangle to be drawn.
     * @param y the y coordinate of the rectangle to be drawn.
     * @param w the width of the rectangle to be drawn.
     * @param h the height of the rectangle to be drawn.
     */
    public void drawRect(double x, double y, double w, double h) {
        drawRect((float) x, (float) y, (float) w, (float) h);
    }

    /**
     * Draws the outline of the specified rectangle on the page.
     * The left and right edges of the rectangle are at x and x + w.
     * The top and bottom edges are at y and y + h.
     * The rectangle is drawn using the current pen color.
     *
     * @param x the x coordinate of the rectangle to be drawn.
     * @param y the y coordinate of the rectangle to be drawn.
     * @param w the width of the rectangle to be drawn.
     * @param h the height of the rectangle to be drawn.
     */
    public void drawRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x+w, y);
        lineTo(x+w, y+h);
        lineTo(x, y+h);
        closePath();
    }

    /**
     * Fills the specified rectangle on the page.
     * The left and right edges of the rectangle are at x and x + w.
     * The top and bottom edges are at y and y + h.
     * The rectangle is drawn using the current pen color.
     *
     * @param x the x coordinate of the rectangle to be drawn.
     * @param y the y coordinate of the rectangle to be drawn.
     * @param w the width of the rectangle to be drawn.
     * @param h the height of the rectangle to be drawn.
     */
    public void fillRect(double x, double y, double w, double h) {
        fillRect((float) x, (float) y, (float) w, (float) h);
    }

    /**
     * Fills the specified rectangle on the page.
     * The left and right edges of the rectangle are at x and x + w.
     * The top and bottom edges are at y and y + h.
     * The rectangle is drawn using the current brush color.
     *
     * @param x the x coordinate of the rectangle to be drawn.
     * @param y the y coordinate of the rectangle to be drawn.
     * @param w the width of the rectangle to be drawn.
     * @param h the height of the rectangle to be drawn.
     */
    public void fillRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x+w, y);
        lineTo(x+w, y+h);
        lineTo(x, y+h);
        fillPath();
    }

    /**
     * Draws a path consisting of multiple points using the specified path operator.
     * The path can include straight line segments and Bézier curve segments controlled by control points.
     *
     * @param path the list of points defining the path. Must contain at least 2 points.
     * @param pathOperator the path painting operator to apply (e.g., PathOperator.STROKE, PathOperator.FILL)
     * @throws Exception if the path contains fewer than 2 points
     *
     * <pre>
     * {@code
     * List<Point> path = new ArrayList<>();
     * path.add(new Point(100, 100));
     * path.add(new Point(200, 100));
     * path.add(new Point(200, 200));
     * path.add(new Point(100, 200));
     *
     * drawPath(path, PathOperator.FILL_AND_STROKE);
     * }
     * </pre>
     *
     * The method handles both straight line segments and Bézier curves:
     * - Straight lines: consecutive points without control points
     * - Bézier curves: points with control points ('C' for cubic, 'Q' for quadratic, etc.)
     *
     * @see Point
     * @see PathOperator
     */
     public void drawPath(List<Point> path, String pathOperator) throws Exception {
        if (path.size() < 2) {
            throw new Exception("The Path object must contain at least 2 points");
        }
        Point point = path.get(0);
        moveTo(point.x, point.y);
        char controlPoint = '\0';
        for (int i = 1; i < path.size(); i++) {
            point = path.get(i);
            if (point.controlPoint != '\0') {
                controlPoint = point.controlPoint;
                append(point);
            } else {
                if (controlPoint != '\0') {
                    append(point);
                    append(controlPoint);
                    append('\n');
                    controlPoint = '\0';
                } else {
                    lineTo(point.x, point.y);
                }
            }
        }
        append(pathOperator);
        append('\n');
    }

    /**
     * Draws a circle on the page.
     * The outline of the circle is drawn using the current pen color.
     *
     * @param x the x coordinate of the center of the circle to be drawn.
     * @param y the y coordinate of the center of the circle to be drawn.
     * @param r the radius of the circle to be drawn.
     */
    public void drawCircle(
            double x,
            double y,
            double r) {
        drawEllipse((float) x, (float) y, (float) r, (float) r, PathOperator.STROKE);
    }

    /**
     * Draws a circle on the page.
     * The outline of the circle is drawn using the current pen color.
     *
     * @param x the x coordinate of the center of the circle to be drawn.
     * @param y the y coordinate of the center of the circle to be drawn.
     * @param r the radius of the circle to be drawn.
     */
    public void drawCircle(
            float x,
            float y,
            float r) {
        drawEllipse(x, y, r, r, PathOperator.STROKE);
    }

    /**
     * Draws the specified circle on the page and fills it with the current brush color.
     *
     * @param x the x coordinate of the center of the circle to be drawn.
     * @param y the y coordinate of the center of the circle to be drawn.
     * @param r the radius of the circle to be drawn.
     * @param pathOperator must be PathOperator.STROKE, PathOperator.CLOSE_AND_STROKE or PathOperator.FILL.
     */
    public void drawCircle(
            double x,
            double y,
            double r,
            String pathOperator) {
        drawEllipse((float) x, (float) y, (float) r, (float) r, pathOperator);
    }

    /**
     * Draws the specified circle on the page and fills it with the current brush color.
     *
     * @param x the x coordinate of the center of the circle to be drawn.
     * @param y the y coordinate of the center of the circle to be drawn.
     * @param r the radius of the circle to be drawn.
     * @param pathOperator must be PathOperator.STROKE, PathOperator.CLOSE_AND_STROKE or PathOperator.FILL.
     */
    public void drawCircle(
            float x,
            float y,
            float r,
            String pathOperator) {
        drawEllipse(x, y, r, r, pathOperator);
    }

    /**
     * Draws an ellipse on the page using the current pen color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param r1 the horizontal radius of the ellipse to be drawn.
     * @param r2 the vertical radius of the ellipse to be drawn.
     */
    public void drawEllipse(
            double x,
            double y,
            double r1,
            double r2) {
        drawEllipse((float) x, (float) y, (float) r1, (float) r2, PathOperator.STROKE);
    }

    /**
     * Draws an ellipse on the page using the current pen color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param r1 the horizontal radius of the ellipse to be drawn.
     * @param r2 the vertical radius of the ellipse to be drawn.
     */
    public void drawEllipse(
            float x,
            float y,
            float r1,
            float r2) {
        drawEllipse(x, y, r1, r2, PathOperator.STROKE);
    }

    /**
     * Fills an ellipse on the page using the current pen color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param r1 the horizontal radius of the ellipse to be drawn.
     * @param r2 the vertical radius of the ellipse to be drawn.
     */
    public void fillEllipse(
            double x,
            double y,
            double r1,
            double r2) {
        drawEllipse((float) x, (float) y, (float) r1, (float) r2, PathOperator.FILL);
    }

    /**
     * Fills an ellipse on the page using the current pen color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param r1 the horizontal radius of the ellipse to be drawn.
     * @param r2 the vertical radius of the ellipse to be drawn.
     */
    public void fillEllipse(
            float x,
            float y,
            float r1,
            float r2) {
        drawEllipse(x, y, r1, r2, PathOperator.FILL);
    }

    /**
     * Draws an ellipse on the page and fills it using the current brush color.
     *
     * @param x the x coordinate of the center of the ellipse to be drawn.
     * @param y the y coordinate of the center of the ellipse to be drawn.
     * @param r1 the horizontal radius of the ellipse to be drawn.
     * @param r2 the vertical radius of the ellipse to be drawn.
     * @param operation the operation.
     */
    private void drawEllipse(
            float x,
            float y,
            float r1,
            float r2,
            String pathOperator) {
        // The best 4-spline magic number
        float m4 = 0.55228f;
        // Starting point
        moveTo(x, y - r2);

        appendPointXY(x + m4*r1, y - r2);
        appendPointXY(x + r1, y - m4*r2);
        appendPointXY(x + r1, y);
        append("c\n");

        appendPointXY(x + r1, y + m4*r2);
        appendPointXY(x + m4*r1, y + r2);
        appendPointXY(x, y + r2);
        append("c\n");

        appendPointXY(x - m4*r1, y + r2);
        appendPointXY(x - r1, y + m4*r2);
        appendPointXY(x - r1, y);
        append("c\n");

        appendPointXY(x - r1, y - m4*r2);
        appendPointXY(x - m4*r1, y - r2);
        appendPointXY(x, y - r2);
        append("c\n");

        append(pathOperator);
        append('\n');
    }

    /**
     * Draws a point on the page using the current pen color.
     *
     * @param p the point.
     * @throws Exception  If an input or output exception occurred
     */
    public void drawPoint(Point p) throws Exception {
        if (p.shape != Point.INVISIBLE) {
            List<Point> list;
            if (p.shape == Point.CIRCLE) {
                drawCircle(p.x, p.y, p.r, p.getPathOperator());
            } else if (p.shape == Point.DIAMOND) {
                list = new ArrayList<Point>();
                list.add(new Point(p.x, p.y - p.r*1.2));
                list.add(new Point(p.x + p.r*1.2, p.y));
                list.add(new Point(p.x, p.y + p.r*1.2));
                list.add(new Point(p.x - p.r*1.2, p.y));
                drawPath(list, p.getPathOperator());
            } else if (p.shape == Point.BOX) {
                list = new ArrayList<Point>();
                list.add(new Point(p.x - p.r*0.886, p.y - p.r*0.886));
                list.add(new Point(p.x + p.r*0.886, p.y - p.r*0.886));
                list.add(new Point(p.x + p.r*0.886, p.y + p.r*0.886));
                list.add(new Point(p.x - p.r*0.886, p.y + p.r*0.886));
                drawPath(list, p.getPathOperator());
            } else if (p.shape == Point.PLUS) {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y);
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Point.UP_ARROW) {
                list = new ArrayList<Point>();
                list.add(new Point(p.x, p.y - p.r));
                list.add(new Point(p.x + p.r, p.y + p.r));
                list.add(new Point(p.x - p.r, p.y + p.r));
                list.add(new Point(p.x, p.y - p.r));
                drawPath(list, p.getPathOperator());
            } else if (p.shape == Point.DOWN_ARROW) {
                list = new ArrayList<Point>();
                list.add(new Point(p.x - p.r, p.y - p.r));
                list.add(new Point(p.x + p.r, p.y - p.r));
                list.add(new Point(p.x, p.y + p.r));
                list.add(new Point(p.x - p.r, p.y - p.r));
                drawPath(list, p.getPathOperator());
            } else if (p.shape == Point.LEFT_ARROW) {
                list = new ArrayList<Point>();
                list.add(new Point(p.x + p.r, p.y + p.r));
                list.add(new Point(p.x - p.r, p.y));
                list.add(new Point(p.x + p.r, p.y - p.r));
                list.add(new Point(p.x + p.r, p.y + p.r));
                drawPath(list, p.getPathOperator());
            } else if (p.shape == Point.RIGHT_ARROW) {
                list = new ArrayList<Point>();
                list.add(new Point(p.x - p.r, p.y - p.r));
                list.add(new Point(p.x + p.r, p.y));
                list.add(new Point(p.x - p.r, p.y + p.r));
                list.add(new Point(p.x - p.r, p.y - p.r));
                drawPath(list, p.getPathOperator());
            } else if (p.shape == Point.H_DASH) {
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y);
            } else if (p.shape == Point.V_DASH) {
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Point.X_MARK) {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r);
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r);
            } else if (p.shape == Point.MULTIPLY) {
                drawLine(p.x - p.r, p.y - p.r, p.x + p.r, p.y + p.r);
                drawLine(p.x - p.r, p.y + p.r, p.x + p.r, p.y - p.r);
                drawLine(p.x - p.r, p.y, p.x + p.r, p.y);
                drawLine(p.x, p.y - p.r, p.x, p.y + p.r);
            } else if (p.shape == Point.STAR) {
                list = new ArrayList<Point>();
                for (int i = 0; i < 10; i++) {
                    double theta = i * 36 * (Math.PI / 180.0);
                    double radius = (i % 2 == 0) ? p.r*1.147f : p.r*0.38196f*1.147f;
                    double x = p.x + radius * Math.sin(theta);
                    double y = p.y - radius * Math.cos(theta);  // minus because y grows down
                    list.add(new Point(x, y));
                }
                drawPath(list, p.getPathOperator());
            }
        }
    }

    /**
     * Sets the text rendering mode.
     *
     * @param mode the rendering mode.
     * @throws Exception  If an input or output exception occurred
     */
    public void setTextRenderingMode(int mode) throws Exception {
        if (mode >= 0 && mode <= 7) {
            this.renderingMode = mode;
        } else {
            throw new Exception("Invalid text rendering mode: " + mode);
        }
    }

    /**
     *  Sets the text direction.
     *
     *  @param degrees the angle.
     */
    public void setTextDirection(int degrees) {
        if (degrees > 360) degrees %= 360;
        if (degrees == 0) {
            tmx = new float[] { 1f,  0f,  0f,  1f};
        } else if (degrees == 90) {
            tmx = new float[] { 0f,  1f, -1f,  0f};
        } else if (degrees == 180) {
            tmx = new float[] {-1f,  0f,  0f, -1f};
        } else if (degrees == 270) {
            tmx = new float[] { 0f, -1f,  1f,  0f};
        } else if (degrees == 360) {
            tmx = new float[] { 1f,  0f,  0f,  1f};
        } else {
            float sinOfAngle = (float) Math.sin(degrees * (Math.PI / 180));
            float cosOfAngle = (float) Math.cos(degrees * (Math.PI / 180));
            tmx = new float[] {cosOfAngle, sinOfAngle, -sinOfAngle, cosOfAngle};
        }
        tm0 = FastFloat.toByteArray(tmx[0]);
        tm1 = FastFloat.toByteArray(tmx[1]);
        tm2 = FastFloat.toByteArray(tmx[2]);
        tm3 = FastFloat.toByteArray(tmx[3]);
    }

    /**
     * Draws a cubic Bézier curve starting from the current point to the end point p3
     *
     * @param x1 first control point x
     * @param y1 first control point y
     * @param x2 second control point x
     * @param y2 second control point y
     * @param x3 end point x
     * @param y3 end point y
     */
    public void curveTo(
        float x1, float y1, float x2, float y2, float x3, float y3) {
        append(x1);
        append(' ');
        append(height - y1);
        append(' ');
        append(x2);
        append(' ');
        append(height - y2);
        append(' ');
        append(x3);
        append(' ');
        append(height - y3);
        append(" c\n");
    }

    public float[] drawCircularArc(
            float x, float y, float r, float startAngle, float sweepDegrees) {
        return drawArc(x, y, r, r, startAngle, sweepDegrees);
    }

    public float[] drawArc(
            float x,
            float y,
            float rx,
            float ry,
            float startAngle,
            float sweepDegrees) {
        float x1 = 0f;
        float y1 = 0f;
        float x2 = 0f;
        float y2 = 0f;
        float x3 = 0f;
        float y3 = 0f;

        int numSegments = (int)Math.ceil(Math.abs(sweepDegrees) / 90.0);
        double angleRad = startAngle * Math.PI / 180.0;
        double deltaPerSeg = (sweepDegrees / numSegments) * Math.PI / 180.0;
        for (int i = 0; i < numSegments; i++) {
            double segStart = angleRad;
            double segEnd   = angleRad + deltaPerSeg;
            double deltaRad = segEnd - segStart; // guaranteed ≤ ±π/2

            // Calculate safe κ
            float k = (float)(4.0 / 3.0 * Math.tan(deltaRad / 4.0));

            float cosStart = (float)Math.cos(segStart);
            float sinStart = (float)Math.sin(segStart);
            float cosEnd   = (float)Math.cos(segEnd);
            float sinEnd   = (float)Math.sin(segEnd);

            // End points
            float x0 = x + rx * cosStart;
            float y0 = y + ry * sinStart;
            x3 = x + rx * cosEnd;
            y3 = y + ry * sinEnd;

            // Control points
            x1 = x0 - (k * rx * sinStart);
            y1 = y0 + (k * ry * cosStart);
            x2 = x3 + (k * rx * sinEnd);
            y2 = y3 - (k * ry * cosEnd);

            if (i == 0) {
                moveTo(x0, y0);
            }
            curveTo(x1, y1, x2, y2, x3, y3);

            angleRad = segEnd;
        }

        return new float[] { x1, y1, x2, y2, x3, y3 };
    }

    /**
     * Draws a Bézier curve starting from the current point.
     * <strong>Please note:</strong> You must call the fillPath,
     * closePath or strokePath method after the last bezierCurveTo call.
     * <p><i>Author:</i> <strong>Pieter Libin</strong>, pieter@emweb.be</p>
     *
     * @param p1 first control point
     * @param p2 second control point
     * @param p3 end point
     */
    public void bezierCurveTo(Point p1, Point p2, Point p3) {
        append(p1);
        append(p2);
        append(p3);
        append("c\n");
    }

    protected void setTextFont(Font font, float fontSize) {
        if (font.fontID != null) {
            append('/');
            append(font.fontID);
        } else {
            append("/F");
            append(font.objNumber);
        }
        append(Token.SPACE);
        append(fontSize);
        append(" Tf\n");
    }

    // Code provided by:
    // Dominique Andre Gunia <contact@dgunia.de>
    // <<
    public void drawRectRoundCorners(
            float x, float y, float w, float h, float r1, float r2, String pathOperator)
        throws Exception {
        // The best 4-spline magic number
        float m4 = 0.55228f;
        List<Point> list = new ArrayList<Point>();

        // Starting point
        list.add(new Point(x + w - r1, y));
        list.add(new Point(x + w - r1 + m4*r1, y, Point.CONTROL_POINT));
        list.add(new Point(x + w, y + r2 - m4*r2, Point.CONTROL_POINT));
        list.add(new Point(x + w, y + r2));

        list.add(new Point(x + w, y + h - r2));
        list.add(new Point(x + w, y + h - r2 + m4*r2, Point.CONTROL_POINT));
        list.add(new Point(x + w - m4*r1, y + h, Point.CONTROL_POINT));
        list.add(new Point(x + w - r1, y + h));

        list.add(new Point(x + r1, y + h));
        list.add(new Point(x + r1 - m4*r1, y + h, Point.CONTROL_POINT));
        list.add(new Point(x, y + h - m4*r2, Point.CONTROL_POINT));
        list.add(new Point(x, y + h - r2));

        list.add(new Point(x, y + r2));
        list.add(new Point(x, y + r2 - m4*r2, Point.CONTROL_POINT));
        list.add(new Point(x + m4*r1, y, Point.CONTROL_POINT));
        list.add(new Point(x + r1, y));
        list.add(new Point(x + w - r1, y));

        drawPath(list, pathOperator);
    }

    /**
     * Clips the path.
     */
    public void clipPath() {
        append("W\n");
        append("n\n");  // Close the path without painting it.
    }

    public void clipRect(float x, float y, float w, float h) {
        moveTo(x, y);
        lineTo(x + w, y);
        lineTo(x + w, y + h);
        lineTo(x, y + h);
        clipPath();
    }

    /**
     * Saves the graphics state. Please see Example_31.
     */
    public void saveGraphicsState() {
        append("q\n");
    }

    /**
     * Sets the graphics state. Please see Example_31.
     *
     * @param gs the graphics state to use.
     */
    public void setGraphicsState(GraphicsState gs) {
        StringBuilder sb = new StringBuilder();
        sb.append("/CA ");
        sb.append(gs.getAlphaStroking());
        sb.append(" ");
        sb.append("/ca ");
        sb.append(gs.getAlphaNonStroking());
        String state = sb.toString();
        Integer n;
        if (pdf.states.containsKey(state)) {
            n = pdf.states.get(state);
        } else {
            n = pdf.states.size() + 1;
            pdf.states.put(state, n);
        }
        append("/GS");
        append(n);
        append(" gs\n");
    }

    /**
     * Restores the graphics state. Please see Example_31.
     */
    public void restoreGraphicsState() {
        append("Q\n");
    }

    /**
     * Sets the page CropBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the CropBox.
     * @param upperLeftY the top left Y coordinate of the CropBox.
     * @param lowerRightX the bottom right X coordinate of the CropBox.
     * @param lowerRightY the bottom right Y coordinate of the CropBox.
     */
    public void setCropBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.cropBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    /**
     * Sets the page BleedBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the BleedBox.
     * @param upperLeftY the top left Y coordinate of the BleedBox.
     * @param lowerRightX the bottom right X coordinate of the BleedBox.
     * @param lowerRightY the bottom right Y coordinate of the BleedBox.
     */
    public void setBleedBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.bleedBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    /**
     * Sets the page TrimBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the TrimBox.
     * @param upperLeftY the top left Y coordinate of the TrimBox.
     * @param lowerRightX the bottom right X coordinate of the TrimBox.
     * @param lowerRightY the bottom right Y coordinate of the TrimBox.
     */
    public void setTrimBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.trimBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    /**
     * Sets the page ArtBox.
     * See page 77 of the PDF32000_2008.pdf specification.
     *
     * @param upperLeftX the top left X coordinate of the ArtBox.
     * @param upperLeftY the top left Y coordinate of the ArtBox.
     * @param lowerRightX the bottom right X coordinate of the ArtBox.
     * @param lowerRightY the bottom right Y coordinate of the ArtBox.
     */
    public void setArtBox(
            float upperLeftX, float upperLeftY, float lowerRightX, float lowerRightY) {
        this.artBox = new float[] {upperLeftX, upperLeftY, lowerRightX, lowerRightY};
    }

    private void appendPointXY(float x, float y) {
        append(x);
        append(' ');
        append(height - y);
        append(' ');
    }

    private void append(Point point) {
        append(point.x);
        append(' ');
        append(height - point.y);
        append(' ');
    }

    void append(String str) {
        try {
            buf.write(str.getBytes(StandardCharsets.UTF_8));
        } catch (IOException e) {
        }
    }

    void append(int n) {
        append(Integer.toString(n));
    }

    void append(float f) {
        append(FastFloat.toByteArray(f));
    }

    void append(char ch) {
        buf.write(ch);
    }

    private void append(byte b) {
        buf.write(b);
    }

    /**
     * Appends the specified array of bytes to the page.
     * @param buffer the array of bytes that is appended.
     */
    public void append(byte[] buffer) {
        try {
            buf.write(buffer);
        } catch (IOException e) {
        }
    }

    protected static final byte[] HEX = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };
    private void appendCodePointAsHex(int codePoint) {
        if (codePoint <= 0xFFFF) {
            // Basic Multilingual Plane (BMP) character
            buf.write(HEX[(codePoint >> 12) & 0xF]);
            buf.write(HEX[(codePoint >> 8)  & 0xF]);
            buf.write(HEX[(codePoint >> 4)  & 0xF]);
            buf.write(HEX[(codePoint)       & 0xF]);
        } else {
            // Supplementary character (needs surrogate pair in UTF-16)
            // Write as 6 hex digits (max Unicode code point is 0x10FFFF)
            buf.write(HEX[(codePoint >> 20) & 0xF]);
            buf.write(HEX[(codePoint >> 16) & 0xF]);
            buf.write(HEX[(codePoint >> 12) & 0xF]);
            buf.write(HEX[(codePoint >> 8)  & 0xF]);
            buf.write(HEX[(codePoint >> 4)  & 0xF]);
            buf.write(HEX[(codePoint)       & 0xF]);
        }
    }

    private void drawWord(
            Font font, StringBuilder buf, float[] color, Map<String, Integer> highlightColors) {
        if (buf.length() > 0) {
            String str = buf.toString();
            if (highlightColors.containsKey(str)) {
                setBrushColor(highlightColors.get(str));
            } else {
                setBrushColor(color);
            }

            if (font.isCoreFont) {
                append("[<");
                drawASCIIString(font, str);
                append(">] TJ\n");
            } else {
                append("<");
                drawUnicodeString(font, str);
                append("> Tj\n");
            }

            buf.setLength(0);
        }
    }

    private void drawColoredString(
            Font font,
            String str,
            float[] color,
            Map<String, Integer> highlightColors) {
        StringBuilder buf1 = new StringBuilder();
        StringBuilder buf2 = new StringBuilder();
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (Character.isLetterOrDigit(ch)) {
                drawWord(font, buf2, color, highlightColors);
                buf1.append(ch);
            } else {
                drawWord(font, buf1, color, highlightColors);
                buf2.append(ch);
            }
        }
        drawWord(font, buf1, color, highlightColors);
        drawWord(font, buf2, color, highlightColors);
    }

    void setStructElementsPageObjNumber(int pageObjNumber) {
        for (StructElem element : pdf.structElements) {
            element.pageObjNumber = pageObjNumber;
        }
    }

    /**
     * Adds BMC marker.
     *
     * @param structure the structure.
     * @param actualText the actual text.
     * @param altDescription the alternative description.
     */
    public void addBMC(
            String structure,
            String actualText,
            String altDescription) {
        addBMC(structure, null, actualText, altDescription);
    }

    /**
     * Adds BMC marker.
     *
     * @param structure the structure.
     * @param language the language.
     * @param actualText the actual text.
     * @param altDescription the alternative description.
     */
    public void addBMC(
            String structure,
            String language,
            String actualText,
            String altDescription) {
        if (pdf.compliance == Compliance.PDF_UA_1) {
            StructElem element = new StructElem();
            element.structure = structure;
            element.mcid = this.mcid;
            element.language = language;
            element.actualText = actualText;
            element.altDescription = altDescription;
            pdf.structElements.add(element);

            append("/");
            append(structure);
            append(" <</MCID ");
            append(mcid++);
            append(">>\n");
            append("BDC\n");
        }
    }

    public void addArtifactBMC() {
        if (pdf.compliance == Compliance.PDF_UA_1) {
            append("/Artifact BMC\n");
        }
    }

    public void addEMC() {
        if (pdf.compliance == Compliance.PDF_UA_1) {
            append("EMC\n");
        }
    }

    void addAnnotation(Annotation annotation) {
        annotation.y1 = this.height - annotation.y1;
        annotation.y2 = this.height - annotation.y2;
        annots.add(annotation);
        if (pdf.compliance == Compliance.PDF_UA_1) {
            StructElem element = new StructElem();
            element.structure = StructElem.LINK;
            element.language = annotation.language;
            element.actualText = annotation.actualText;
            element.altDescription = annotation.altDescription;
            element.annotation = annotation;
            pdf.structElements.add(element);
        }
    }

    private void beginTransform(
            float x, float y, float xScale, float yScale) {
        saveGraphicsState();

        append(xScale);
        append(" 0 0 ");
        append(yScale);
        append(' ');
        append(x);
        append(' ');
        append(y);
        append(" cm\n");

        append(xScale);
        append(" 0 0 ");
        append(yScale);
        append(' ');
        append(x);
        append(' ');
        append(y);
        append(" Tm\n");
    }

    private void endTransform() {
        restoreGraphicsState();
    }

    public void drawContents(
            byte[] content,
            float h,    // The height of the graphics object in points.
            float x,
            float y,
            float xScale,
            float yScale) throws Exception {
        beginTransform(x, (this.height - yScale * h) - y, xScale, yScale);
        append(content);
        endTransform();
    }

    public void drawString(
            Font font, float fontSize, String str, float x, float y, float dx) {
        float x1 = x;
        for (int i = 0; i < str.length(); i++) {
            drawString(font, fontSize, str.substring(i, i + 1), x1, y);
            x1 += dx;
        }
    }

    public void addWatermark(
            Font font, String text) throws Exception {
        float hypotenuse = (float)
                Math.sqrt(this.height * this.height + this.width * this.width);
        float stringWidth = font.stringWidth(text);
        float offset = (hypotenuse - stringWidth) / 2f;
        double angle = Math.atan(this.height / this.width);
        TextLine watermark = new TextLine(font);
        watermark.setTextColor(Color.lightgrey);
        watermark.setText(text);
        watermark.setLocation(
                (float) (offset * Math.cos(angle)),
                (this.height - (float) (offset * Math.sin(angle))));
        watermark.setTextDirection((int) (angle * (180.0 / Math.PI)));
        watermark.drawOn(this);
    }

    public void rotateBy(double rotateDegrees) {
        if (rotateDegrees == 0 ||
            rotateDegrees == 90 ||
            rotateDegrees == 180 ||
            rotateDegrees == 270) {
            this.rotateDegrees = (float) rotateDegrees;
        }
    }

    public void invertYAxis() {
        append("1 0 0 -1 0 ");
        append(this.height);
        append(" cm\n");
    }

    /**
     * Transformation matrix.
     * Use save before, restore afterwards!
     * 9 value array like generated by androids Matrix.getValues()
     * @param values the 9 value array generated by Matrix.getValues()
     */
    public void transform(float[] values) {
        float scalex = values[MSCALE_X];
        float scaley = values[MSCALE_Y];
        float transx = values[MTRANS_X];
        float transy = values[MTRANS_Y];

        append(scalex);
        append(Token.SPACE);
        append(values[MSKEW_X]);
        append(Token.SPACE);
        append(values[MSKEW_Y]);
        append(Token.SPACE);
        append(scaley);
        append(Token.SPACE);

        if (Math.asin(values[MSKEW_Y]) != 0f) {
            transx -= values[MSKEW_Y] * height / scaley;
        }

        append(transx);
        append(Token.SPACE);
        append(-transy);
        append(" cm\n");

        // Weil mit der Hoehe immer die Y-Koordinate im PDF-Koordinatensystem berechnet wird:
        height = height / scaley;
    }

    public float[] addHeader(TextLine textLine) throws Exception {
        return addHeader(textLine, 1.5f*textLine.font.ascent);
    }

    public float[] addHeader(TextLine textLine, float offset) throws Exception {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, offset);
        float[] xy = textLine.drawOn(this);
        xy[1] += textLine.font.descent;
        return xy;
    }

    public float[] addFooter(TextLine textLine) throws Exception {
        return addFooter(textLine, textLine.font.ascent);
    }

    public float[] addFooter(TextLine textLine, float offset) throws Exception {
        textLine.setLocation((getWidth() - textLine.getWidth())/2, getHeight() - offset);
        return textLine.drawOn(this);
    }

    /**
     * Sets the text location.
     *
     * @param x the x coordinate of new text location.
     * @param y the y coordinate of new text location.
     */
    private void setTextLocation(float x, float y) {
        append(x);
        append(Token.SPACE);
        append(height - y);
        append(" Td\n");
    }

    /**
     * Sets the text leading.
     * @param leading the leading.
     */
    private void setTextLeading(float leading) {
        append(leading);
        append(" TL\n");
    }

    /**
     * Advance to the next line.
     */
    private void nextLine() {
        append("T*\n");
    }

    private void setTextScaling(float scaling) {
        append(scaling);
        append(" Tz\n");
    }

    private void setTextRise(float rise) {
        append(rise);
        append(" Ts\n");
    }

    /**
     * Draws a string at the correct location.
     * @param str the string.
     */
    protected void drawTextLine(Font font, String str, float x, float y) {
        append("BT\n");
        setTextLocation(x, y);
        setTextFont(font, font.size);
        if (font.isCoreFont) {
            append("[<");
            drawASCIIString(font, str);
            append(">] TJ\n");
        } else {
            append("<");
            drawUnicodeString(font, str);
            append("> Tj\n");
        }
        append("ET\n");
    }

    void scaleAndRotate(float x, float y, float w, float h, float degrees) {
        // PDF transformations apply LAST-TO-FIRST (like a stack: last command = first applied)

        // [FINAL POSITIONING - Applied First]
        // Moves rotated/scaled image to target (x,y) on page
        append("1 0 0 1 ");
        append(x + w/2);
        append(" ");
        append((height - y) - h/2);
        append(" cm\n");

        // [ROTATION - Applied Second]
        // Rotates around current origin (0,0) by 'degrees'
        double radians = degrees * (Math.PI / 180);
        float cos = (float)Math.cos(radians);
        float sin = (float)Math.sin(radians);
        append(FastFloat.toByteArray(cos));
        append(" ");
        append(FastFloat.toByteArray(sin));
        append(" ");
        append(FastFloat.toByteArray(-sin));
        append(" ");
        append(FastFloat.toByteArray(cos));
        append(" 0 0 cm\n");

        // [ORIGIN SETUP - Applied Last]
        // Centers image at (0,0) and sets scale
        append(w);
        append(" 0 0 ");
        append(h);
        append(" ");
        append(-w/2);
        append(" ");
        append(-h/2);
        append(" cm\n");
    }

    void rotateAroundCenter(float centerX, float centerY, float degrees) {
        append("1 0 0 1 ");
        append(centerX);
        append(" ");
        append(centerY);
        append(" cm\n");

        double radians = degrees * Math.PI / 180;
        float cos = (float)Math.cos(radians);
        float sin = (float)Math.sin(radians);
        append(FastFloat.toByteArray(cos));
        append(" ");
        append(FastFloat.toByteArray(sin));
        append(" ");
        append(FastFloat.toByteArray(-sin));
        append(" ");
        append(FastFloat.toByteArray(cos));
        append(" 0 0 cm\n");

        append("1 0 0 1 ");
        append(-centerX);
        append(" ");
        append(-centerY);
        append(" cm\n");
    }

    protected void drawTextBlock(
            Font font,
            float fontSize,
            TextLine[] textLines,
            float x,
            float y,
            float leading,
            float[] color,
            Map<String, Integer> highlightColors) {
        if (textLines == null || textLines.length == 0) {
            return;
        }

        append("BT\n");
        setBrushColor(color);
        setTextFont(font, fontSize);
        float yText = y;
        for (TextLine textLine : textLines) {
            append("1 0 0 1 ");
            append(x + textLine.xOffset);
            append(' ');
            append(height - (yText + font.getAscent(fontSize)));
            append(" Tm\n");
            if (highlightColors == null) {
                if (font.isCoreFont) {
                    append("[<");
                    drawASCIIString(font, textLine.text);
                    append(">] TJ\n");
                } else {
                    append("<");
                    drawUnicodeString(font, textLine.text);
                    append("> Tj\n");
                }
            } else {
                drawColoredString(font, textLine.text, color, highlightColors);
            }
            yText += leading;
        }
        append("ET\n");

        float yLine = y + font.getBodyHeight(fontSize);
        for (TextLine textLine : textLines) {
            if (textLine.underline) {
                moveTo(x + textLine.xOffset, yLine);
                lineTo(x + textLine.xOffset + font.stringWidth(fontSize, textLine.text), yLine);
                strokePath();
            }
            yLine += leading;
        }
    }
}   // End of Page.java
