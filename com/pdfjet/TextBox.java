/*
 * TextBox.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * A box containing line-wrapped text.
 *
 * <p>
 * Defaults:
 * x = 0f
 * y = 0f
 * width = 300f
 * height = 0f
 * alignment = Align.LEFT
 * valign = Align.TOP
 * spacing = 0f
 * margin = 0f
 * </p>
 *
 * This class was originally developed by Ronald Bourret.
 * It was completely rewritten in 2013 by Evgeni Dragoev.
 */
public class TextBox implements Drawable {
    /** The font of the text. */
    protected Font font;
    /** The font used for characters that the primary font does not have. */
    protected Font fallbackFont;
    /** The font size. */
    protected float fontSize = 12f;
    /** The text. */
    protected String text;
    /** The x coordinate of the top left corner. */
    protected float x;
    /** The y coordinate of the top left corner. */
    protected float y;
    /** The width. */
    protected float width = 300f;
    /** The height. */
    protected float height = 0f;
    /** The spacing between lines of text. */
    protected float spacing = 0f;
    /** The margin of this text box. */
    protected float margin = 0f;
    /** The width of the border lines. */
    protected float lineWidth = 0f;

    private float[] fillColor;  // The background fill color
    private float[] textColor = new float[] {0f, 0f, 0f};
    private float strokeWidth = 0.5f;
    private float[] strokeColor;

    private int valign = Align.TOP;
    private Map<String, Integer> colors = null;
    // TextBox properties
    // Future use:
    // bits 0 to 15
    // Border:
    // bit 16 - top
    // bit 17 - bottom
    // bit 18 - left
    // bit 19 - right
    // Text Alignment:
    // bit 20
    // bit 21
    // Text Decoration:
    // bit 22 - underline
    // bit 23 - strikeout
    // Future use:
    // bits 24 to 31
    private int properties = 0x00000001;
    private String language = "en-US";
    private String altDescription = "";
    private String uri = null;
    private String key = null;
    private String uriLanguage = null;
    private String uriActualText = null;
    private String uriAltDescription = null;
    private Direction textDirection = Direction.LEFT_TO_RIGHT;

    /**
     * Creates a text box and sets the font.
     *
     * @param font the font.
     */
    public TextBox(Font font) {
        this.font = font;
        this.fontSize = font.size;
    }

    /**
     * Creates a text box and sets the font.
     *
     * @param text the text.
     * @param font the font.
     */
    public TextBox(Font font, String text) {
        this.font = font;
        this.fontSize = font.size;
        this.text = text;
    }

    /**
     * Creates a text box and sets the font and the text.
     *
     * @param font   the font.
     * @param text   the text.
     * @param width  the width.
     * @param height the height.
     */
    public TextBox(Font font, String text, double width, double height) {
        this(font, text, (float) width, (float) height);
    }

    /**
     * Creates a text box and sets the font and the text.
     *
     * @param font   the font.
     * @param text   the text.
     * @param width  the width.
     * @param height the height.
     */
    public TextBox(Font font, String text, float width, float height) {
        this.font = font;
        this.fontSize = font.size;
        this.text = text;
        this.width = width;
        this.height = height;
    }

    /**
     * Sets the font for this text box.
     *
     * @param font the font.
     * @return this TextBox object.
     */
    public TextBox setFont(Font font) {
        this.font = font;
        return this;
    }

    /**
     * Returns the font used by this text box.
     *
     * @return the font.
     */
    public Font getFont() {
        return font;
    }

    /**
     * Sets the font size of the text.
     *
     * @param fontSize the font size.
     * @return this TextBox object.
     */
    public TextBox setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     * Sets the text box text.
     *
     * @param text the text box text.
     * @return this TextBox object.
     */
    public TextBox setText(String text) {
        this.text = text;
        return this;
    }

    /**
     * Returns the text box text.
     *
     * @return the text box text.
     */
    public String getText() {
        return text;
    }

    /**
     * Sets the size of text box.
     *
     * @param w the width of the text box.
     * @param h the height of the text box.
     * @return this TextBox object.
     */
    public TextBox setSize(float w, float h) {
        this.width = w;
        this.height = h;
        return this;
    }

    /**
     * Sets the location where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     * @return this TextBox object.
     */
    public TextBox setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     * @return this TextBox object.
     */
    public TextBox setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Gets the location where this text box will be drawn on the page.
     *
     * @return the float array of x and y.
     */
    public float[] getLocation() {
        return new float[] {this.x, this.y};
    }

    /**
     * Sets the width of this text box.
     *
     * @param width the specified width.
     * @return this TextBox object.
     */
    public TextBox setWidth(double width) {
        this.width = (float) width;
        return this;
    }

    /**
     * Sets the width of this text box.
     *
     * @param width the specified width.
     * @return this TextBox object.
     */
    public TextBox setWidth(float width) {
        this.width = width;
        return this;
    }

    /**
     * Returns the text box width.
     *
     * @return the text box width.
     */
    public float getWidth() {
        return width;
    }

    /**
     * Sets the height of this text box.
     *
     * @param height the specified height.
     * @return this TextBox object.
     */
    public TextBox setHeight(double height) {
        this.height = (float) height;
        return this;
    }

    /**
     * Sets the height of this text box.
     *
     * @param height the specified height.
     * @return this TextBox object.
     */
    public TextBox setHeight(float height) {
        this.height = height;
        return this;
    }

    /**
     * Returns the text box height.
     *
     * @return the text box height.
     */
    public float getHeight() {
        return height;
    }

    /**
     * Sets the margin of this text box.
     *
     * @param margin the margin between the text and the box
     * @return this TextBox object.
     */
    public TextBox setMargin(double margin) {
        this.margin = (float) margin;
        return this;
    }

    /**
     * Sets the margin of this text box.
     *
     * @param margin the margin between the text and the box
     * @return this TextBox object.
     */
    public TextBox setMargin(float margin) {
        this.margin = margin;
        return this;
    }

    /**
     * Returns the text box margin.
     *
     * @return the margin between the text and the box
     */
    public float getMargin() {
        return margin;
    }

    /**
     * Sets the border line width.
     *
     * @param lineWidth double
     * @return this TextBox object.
     */
    public TextBox setLineWidth(double lineWidth) {
        this.lineWidth = (float) lineWidth;
        return this;
    }

    /**
     * Sets the border line width.
     *
     * @param lineWidth float
     * @return this TextBox object.
     */
    public TextBox setLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
        return this;
    }

    /**
     * Returns the border line width.
     *
     * @return float the line width.
     */
    public float getLineWidth() {
        return lineWidth;
    }

    /**
     * Sets the spacing between lines of text.
     *
     * @param spacing the spacing
     * @return this TextBox object.
     */
    public TextBox setSpacing(double spacing) {
        this.spacing = (float) spacing;
        return this;
    }

    /**
     * Sets the spacing between lines of text.
     *
     * @param spacing the spacing
     * @return this TextBox object.
     */
    public TextBox setSpacing(float spacing) {
        this.spacing = spacing;
        return this;
    }

    /**
     * Returns the spacing between lines of text.
     *
     * @return float the spacing.
     */
    public float getSpacing() {
        return spacing;
    }

    /**
     * Sets the background color.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBox object.
     */
    public TextBox setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the background color.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBox object.
     */
    public TextBox setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /**
     * Sets the background color.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBox object.
     */
    public TextBox setBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the background color.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBox object.
     */
    public TextBox setBackgroundColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /**
     * Sets the text color.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBox object.
     */
    public TextBox setTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the text color.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this TextBox object.
     */
    public TextBox setTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the text color.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBox object.
     */
    public TextBox setTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    /**
     * Returns the text color.
     *
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] getTextColor() {
        return textColor;
    }

    /**
     * Sets the stroke width of the borders.
     *
     * @param strokeWidth the stroke width.
     * @return this TextBox object.
     */
    public TextBox setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /**
     * Sets the stroke color of the borders.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this TextBox object.
     */
    public TextBox setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke color of the borders.
     *
     * @param r the red component, from 0.0 to 1.0.
     * @param g the green component, from 0.0 to 1.0.
     * @param b the blue component, from 0.0 to 1.0.
     * @return this TextBox object.
     */
    public TextBox setStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke color of the borders.
     *
     * @param rgbColor the red, green and blue components, from 0.0 to 1.0.
     * @return this TextBox object.
     */
    public TextBox setStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    /**
     * Returns the stroke color of the borders.
     *
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] getStrokeColor() {
        return strokeColor;
    }

    /**
     * Sets the TextBox border properties.
     *
     * @param border the border properties.
     * @return this TextBox object.
     */
    public TextBox setBorder(int border) {
        this.properties |= border;
        return this;
    }

    /**
     * Returns the text box specific border value.
     *
     * @param border the border property.
     * @return boolean the specific border value.
     */
    public boolean getBorder(int border) {
        if (border == Border.NONE) {
            if (((properties >> 16) & 0xF) == 0x0) {
                return true;
            }
        } else if (border == Border.TOP) {
            if (((properties >> 16) & 0x1) == 0x1) {
                return true;
            }
        } else if (border == Border.BOTTOM) {
            if (((properties >> 16) & 0x2) == 0x2) {
                return true;
            }
        } else if (border == Border.LEFT) {
            if (((properties >> 16) & 0x4) == 0x4) {
                return true;
            }
        } else if (border == Border.RIGHT) {
            if (((properties >> 16) & 0x8) == 0x8) {
                return true;
            }
        } else if (border == Border.ALL) {
            if (((properties >> 16) & 0xF) == 0xF) {
                return true;
            }
        }
        return false;
    }

    /**
     * Sets the TextBox borders on and off.
     *
     * @param borders the borders flag.
     * @return this TextBox object.
     */
    public TextBox setBorders(boolean borders) {
        if (borders) {
            setBorder(Border.ALL);
        } else {
            setBorder(Border.NONE);
        }
        return this;
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment code.
     *                  Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     * @return this TextBox object.
     */
    public TextBox setTextAlignment(int alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
        return this;
    }

    /**
     * Returns the text alignment.
     *
     * @return alignment the alignment code. Supported values: Align.LEFT,
     *         Align.RIGHT and Align.CENTER.
     */
    public int getTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /**
     * Sets the underline variable.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline flag.
     * @return this TextBox object.
     */
    public TextBox setUnderline(boolean underline) {
        if (underline) {
            this.properties |= 0x00400000;
        } else {
            this.properties &= 0x00BFFFFF;
        }
        return this;
    }

    /**
     * Whether the text will be underlined.
     *
     * @return whether the text will be underlined
     */
    public boolean getUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /**
     * Sets the strikeout flag.
     * In the flag is true - draw strikeout line through the text.
     *
     * @param strikeout the strikeout flag.
     * @return this TextBox object.
     */
    public TextBox setStrikeout(boolean strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        } else {
            this.properties &= 0x007FFFFF;
        }
        return this;
    }

    /**
     * Returns the strikeout flag.
     *
     * @return boolean the strikeout flag.
     */
    public boolean getStrikeout() {
        return (properties & 0x00800000) != 0;
    }

    /**
     * Sets the font used for the characters the main font does not have.
     *
     * @param fallbackFont the fallback font.
     * @return this TextBox object.
     */
    public TextBox setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /**
     * Returns the fallback font.
     *
     * @return the fallback font.
     */
    public Font getFallbackFont() {
        return this.fallbackFont;
    }

    /**
     * Sets the vertical alignment of the text in this TextBox.
     *
     * @param valign - valid values are Align.TOP, Align.BOTTOM and Align.CENTER
     * @return this TextBox object.
     */
    public TextBox setVerticalAlignment(int valign) {
        this.valign = valign;
        return this;
    }

    /**
     * Returns the vertical alignment of the text.
     *
     * @return the vertical alignment.
     */
    public int getVerticalAlignment() {
        return this.valign;
    }

    /**
     * Sets the colors used to highlight words in the text.
     *
     * @param colors the words and their 0xRRGGBB colors.
     * @return this TextBox object.
     */
    public TextBox setTextColors(Map<String, Integer> colors) {
        this.colors = colors;
        return this;
    }

    /**
     * Returns the colors used to highlight words in the text.
     *
     * @return the words and their colors.
     */
    public Map<String, Integer> getTextColors() {
        return this.colors;
    }

    /**
     * Sets the language of the text, for example "en-US".
     *
     * @param language the language.
     * @return this TextBox object.
     */
    public TextBox setLanguage(String language) {
        this.language = language;
        return this;
    }

    /**
     * Returns the language of the text.
     *
     * @return the language.
     */
    public String getLanguage() {
        return this.language;
    }

    /**
     * Sets the alternate description of this text line.
     *
     * @param altDescription the alternate description of the text line.
     * @return this TextBox.
     */
    public TextBox setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Returns the alternate description of this text box.
     *
     * @return the alternate description.
     */
    public String getAltDescription() {
        return altDescription;
    }

    private void drawBorders(Page page) {
        if (page == null) {
            return;
        }
        page.addArtifactBMC();
        page.setPenColor(strokeColor);
        page.setPenWidth(strokeWidth);
        if (getBorder(Border.ALL)) {
            page.drawRect(x, y, width, height);
        } else {
            if (getBorder(Border.TOP)) {
                page.moveTo(x, y);
                page.lineTo(x + width, y);
                page.strokePath();
            }
            if (getBorder(Border.BOTTOM)) {
                page.moveTo(x, y + height);
                page.lineTo(x + width, y + height);
                page.strokePath();
            }
            if (getBorder(Border.LEFT)) {
                page.moveTo(x, y);
                page.lineTo(x, y + height);
                page.strokePath();
            }
            if (getBorder(Border.RIGHT)) {
                page.moveTo(x + width, y);
                page.lineTo(x + width, y + height);
                page.strokePath();
            }
        }
        page.addEMC();
    }

    private boolean textIsCJK(String str) {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        int numOfCJK = 0;
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK += 1;
            }
        }
        return (numOfCJK > (str.length() / 2));
    }

    private String[] getTextLines() {
        List<String> list = new ArrayList<String>();

        float textAreaWidth;
        if (textDirection == Direction.LEFT_TO_RIGHT) {
            textAreaWidth = width - 2*margin;
        } else {
            textAreaWidth = height - 2*margin;
        }
        // Font.stringWidth(fallbackFont, fontSize, str) is an exact per-character sum
        // whenever the primary font is not a core font - only the core fonts
        // apply kerning between adjacent characters, so their width is not
        // simply the sum of their parts. For the common case (an embedded
        // TrueType/CJK font) this lets the loops below track the wrapped
        // line's width incrementally - adding one token/character's width at
        // a time - instead of re-measuring the whole accumulated line on
        // every token, which made wrapping a long paragraph an O(n^2)
        // operation. Core fonts keep the original exact re-measurement so
        // kerning is still accounted for correctly.
        boolean additive = !font.isCoreFont;
        String[] lines = text.split("\\r?\\n", -1);
        for (String line : lines) {
            if (font.stringWidth(fallbackFont, fontSize, line) <= textAreaWidth) {
                list.add(line);
            } else {
                if (textIsCJK(line)) {
                    StringBuilder sb = new StringBuilder();
                    float sbWidth = 0f;
                    for (char ch : line.toCharArray()) {
                        float chWidth = additive ? font.stringWidth(fallbackFont, fontSize, String.valueOf(ch)) : 0f;
                        float width = additive ? sbWidth + chWidth : font.stringWidth(fallbackFont, fontSize, sb.toString() + ch);
                        if (width <= textAreaWidth) {
                            sb.append(ch);
                            sbWidth = width;
                        } else {
                            if (sb.length() > 0) {  // Don't emit an empty line
                                list.add(sb.toString());
                            }
                            sb.setLength(0);
                            sb.append(ch);
                            sbWidth = chWidth;
                        }
                    }
                    if (sb.length() > 0) {
                        list.add(sb.toString());
                    }
                } else {
                    StringBuilder sb = new StringBuilder();
                    float sbWidth = 0f;
                    float spaceWidth = additive ? font.stringWidth(fallbackFont, fontSize, " ") : 0f;
                    String[] tokens = line.split("\\s+");
                    for (String token : tokens) {
                        float tokenWidth = additive ? font.stringWidth(fallbackFont, fontSize, token) : 0f;
                        float width;
                        if (additive) {
                            width = sb.length() == 0 ? tokenWidth : sbWidth + spaceWidth + tokenWidth;
                        } else {
                            width = font.stringWidth(fallbackFont, fontSize, sb.toString() + token);
                        }
                        if (width <= textAreaWidth) {
                            sb.append(token + " ");
                            sbWidth = width;
                        } else {
                            if (sb.length() > 0) {  // Don't emit an empty line
                                list.add(sb.toString().trim());
                            }
                            sb.setLength(0);
                            sb.append(token + " ");
                            sbWidth = tokenWidth;
                        }
                    }
                    if (sb.toString().trim().length() > 0) {
                        list.add(sb.toString().trim());
                    }
                }
            }
        }

        return list.toArray(new String[] {});
    }

    /**
     * Draws this text box on the specified page.
     *
     * @param page the Page where the TextBox is to be drawn.
     * @return x and y coordinates of the bottom right corner of this component.
     */
    public float[] drawOn(Page page) {
        String[] lines = getTextLines();
        float leading = font.getAscent(fontSize) + font.getDescent(fontSize) + spacing;
        if (height > 0f) { // TextBox with fixed height
            if ((lines.length*leading - spacing) > (height - 2*margin)) {
                List<String> list = new ArrayList<String>();
                for (int i = 0; i < lines.length; i++) {
                    String line = lines[i];
                    if (((i + 1)*leading - spacing) > (height - 2*margin)) {
                        break;
                    }
                    list.add(line);
                }
                if (list.size() > 0) {  // At least one line must fit in the text box
                    String lastLine = list.get(list.size() - 1);
                    if (lastLine.length() > 3) {
                        lastLine = lastLine.substring(0, lastLine.length() - 3);
                    }
                    list.set(list.size() - 1, lastLine + "...");
                    lines = list.toArray(new String[] {});
                }
            }
            if (page != null) {
                if (fillColor != null) {
                    page.setBrushColor(fillColor);
                    page.addArtifactBMC();
                    page.fillRect(x, y, width, height);
                    page.addEMC();
                }
                page.setPenColor(this.strokeColor);
                page.setBrushColor(this.fillColor);
                page.setPenWidth(this.font.getUnderlineThickness(fontSize));
            }
            float xText = x + margin;
            float yText = y + margin + font.getAscent(fontSize);
            if (textDirection == Direction.LEFT_TO_RIGHT) {
                if (valign == Align.TOP) {
                    yText = y + margin + font.getAscent(fontSize);
                } else if (valign == Align.BOTTOM) {
                    yText = (y + height) - (Float.valueOf(lines.length)*leading + margin);
                    yText += font.getAscent(fontSize);
                } else if (valign == Align.CENTER) {
                    yText = y + (height - Float.valueOf(lines.length)*leading)/2;
                    yText += font.getAscent(fontSize);
                }
            } else {
                yText = x + margin + font.getAscent(fontSize);
            }
            for (String line : lines) {
                if (textDirection == Direction.LEFT_TO_RIGHT) {
                    if (getTextAlignment() == Align.LEFT) {
                        xText = x + margin;
                    } else if (getTextAlignment() == Align.RIGHT) {
                        xText = (x + width) - (font.stringWidth(fallbackFont, fontSize, line) + margin);
                    } else if (getTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.stringWidth(fallbackFont, fontSize, line))/2;
                    }
                } else {
                    xText = y + margin;
                }
                if (page != null) {
                    drawTextLine(page, font, fallbackFont, line, xText, yText, textColor, colors);
                }
                if (textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP) {
                    yText += leading;
                } else {
                    yText -= leading;
                }
            }
        } else { // TextBox that expands to fit the content
            if (page != null) {
                if (fillColor!= null) {
                    page.setBrushColor(fillColor);
                    page.addArtifactBMC();
                    page.fillRect(x, y, width, (lines.length * leading - spacing) + 2*margin);
                    page.addEMC();
                }
                page.setBrushColor(this.textColor);
                page.setPenColor(this.strokeColor);
                page.setPenWidth(this.font.getUnderlineThickness(fontSize));
            }
            float xText = x + margin;
            float yText = y + margin + font.getAscent(fontSize);
            for (String line : lines) {
                if (textDirection == Direction.LEFT_TO_RIGHT) {
                    if (getTextAlignment() == Align.LEFT) {
                        xText = x + margin;
                    } else if (getTextAlignment() == Align.RIGHT) {
                        xText = (x + width) - (font.stringWidth(fallbackFont, fontSize, line) + margin);
                    } else if (getTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.stringWidth(fallbackFont, fontSize, line))/2;
                    }
                } else {
                    xText = x + margin;
                }
                if (page != null) {
                    drawTextLine(page, font, fallbackFont, line, xText, yText, textColor, colors);
                }
                if (textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP) {
                    yText += leading;
                } else {
                    yText -= leading;
                }
            }
            height = ((yText - y) - (font.getAscent(fontSize) + spacing)) + margin;
        }
        if (page != null) {
            drawBorders(page);
            if (textDirection == Direction.LEFT_TO_RIGHT && (uri != null || key != null)) {
                page.addAnnotation(new Annotation(
                        Annotation.Link,
                        x,
                        y,
                        x + width,
                        y + height,
                        null,   // Vertices
                        null,   // Fill Color
                        0f,     // Transparency
                        null,   // Title
                        null,   // Contents
                        uri,
                        key,    // The destination name
                        uriLanguage,
                        uriActualText,
                        uriAltDescription));
            }
            page.setTextDirection(0);
        }
        return new float[] { x + width, y + height };
    }

    private void drawTextLine(
            Page page,
            Font font,
            Font fallbackFont,
            String text,
            float xText,
            float yText,
            float[] color,
            Map<String, Integer> colors) {
        if (altDescription != null) {
            page.addBMC(StructElem.P, language, text, altDescription);
        }

        if (textDirection == Direction.LEFT_TO_RIGHT) {
            page.drawString(font, fallbackFont, fontSize, text, xText, yText, color, colors);
        } else if (textDirection == Direction.BOTTOM_TO_TOP) {
            page.setTextDirection(90);
            page.drawString(font, fallbackFont, fontSize, text, yText, xText + height, color, colors);
        } else if (textDirection == Direction.TOP_TO_BOTTOM) {
            page.setTextDirection(270);
            page.drawString(font, fallbackFont, fontSize, text,
                    (yText + width) - (margin + 2*font.getAscent(fontSize)), xText, color, colors);
        }

        if (altDescription != null) {
            page.addEMC();
        }

        if (textDirection == Direction.LEFT_TO_RIGHT) {
            float lineLength = font.stringWidth(fallbackFont, fontSize, text);
            if (getUnderline()) {
                page.addArtifactBMC();
                page.moveTo(xText, yText + font.getUnderlinePosition(fontSize));
                page.lineTo(xText + lineLength, yText + font.getUnderlinePosition(fontSize));
                page.strokePath();
                page.addEMC();
            }
            if (getStrikeout()) {
                page.addArtifactBMC();
                page.moveTo(xText, yText - (font.getBodyHeight(fontSize)/4));
                page.lineTo(xText + lineLength, yText - (font.getBodyHeight(fontSize)/4));
                page.strokePath();
                page.addEMC();
            }
        }
    }

    /**
     * Sets the URI for the "click text line" action.
     *
     * @param uri the URI
     * @return this TextBox object.
     */
    public TextBox setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Sets the direction of the text.
     *
     * @param textDirection the text direction.
     * @return this TextBox object.
     */
    public TextBox setTextDirection(Direction textDirection) {
        this.textDirection = textDirection;
        return this;
    }
} // End of TextBox.java
