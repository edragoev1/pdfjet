/**
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
    protected Font font;
    protected Font fallbackFont;
    protected float fontSize = 12f;
    protected String text;
    protected float x;
    protected float y;
    protected float width = 300f;
    protected float height = 0f;
    protected float spacing = 0f;
    protected float margin = 0f;
    protected float lineWidth = 0f;

    private float[] fillColor;  // The background fill color
    private float[] textColor = new float[] {0f, 0f, 0f};
    private float strokeWidth = 1f;
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
     */
    public void setFont(Font font) {
        this.font = font;
    }

    /**
     * Returns the font used by this text box.
     *
     * @return the font.
     */
    public Font getFont() {
        return font;
    }

    public void setFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    /**
     * Sets the text box text.
     *
     * @param text the text box text.
     */
    public void setText(String text) {
        this.text = text;
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
     * Sets the position where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    /**
     * Sets the size of text box.
     *
     * @param w the width of the text box.
     * @param h the height of the text box.
     */
    public void setSize(float w, float h) {
        this.width = w;
        this.height = h;
    }

    /**
     * Sets the position where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public void setPosition(double x, double y) {
        setLocation(x, y);
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
     * Gets the location where this text box will be drawn on the page.
     *
     * @return the float array of of x and y.
     */
    public float[] getLocation() {
        return new float[] {this.x, this.y};
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
     * Sets the width of this text box.
     *
     * @param width the specified width.
     */
    public void setWidth(double width) {
        this.width = (float) width;
    }

    /**
     * Sets the width of this text box.
     *
     * @param width the specified width.
     */
    public void setWidth(float width) {
        this.width = width;
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
     */
    public void setHeight(double height) {
        this.height = (float) height;
    }

    /**
     * Sets the height of this text box.
     *
     * @param height the specified height.
     */
    public void setHeight(float height) {
        this.height = height;
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
     */
    public void setMargin(double margin) {
        this.margin = (float) margin;
    }

    /**
     * Sets the margin of this text box.
     *
     * @param margin the margin between the text and the box
     */
    public void setMargin(float margin) {
        this.margin = margin;
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
     */
    public void setLineWidth(double lineWidth) {
        this.lineWidth = (float) lineWidth;
    }

    /**
     * Sets the border line width.
     *
     * @param lineWidth float
     */
    public void setLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
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
     */
    public void setSpacing(double spacing) {
        this.spacing = (float) spacing;
    }

    /**
     * Sets the spacing between lines of text.
     *
     * @param spacing the spacing
     */
    public void setSpacing(float spacing) {
        this.spacing = spacing;
    }

    /**
     * Returns the spacing between lines of text.
     *
     * @return float the spacing.
     */
    public float getSpacing() {
        return spacing;
    }

    public void setFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void setFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void setBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void setBackgroundColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void setTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
    }

    public void setTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
    }

    public void setTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
    }

    public float[] getTextColor() {
        return textColor;
    }

    public void setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
    }

    public void setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
    }

    public void setStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
    }

    public void setStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
    }

    public float[] getStrokeColor() {
        return strokeColor;
    }

    /**
     * Sets the TextBox border properties.
     *
     * @param border the border properties.
     */
    public void setBorder(int border) {
        this.properties |= border;
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
     */
    public void setBorders(boolean borders) {
        if (borders) {
            setBorder(Border.ALL);
        } else {
            setBorder(Border.NONE);
        }
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment code.
     *                  Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public void setTextAlignment(int alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
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
     */
    public void setUnderline(boolean underline) {
        if (underline) {
            this.properties |= 0x00400000;
        } else {
            this.properties &= 0x00BFFFFF;
        }
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
     */
    public void setStrikeout(boolean strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        } else {
            this.properties &= 0x007FFFFF;
        }
    }

    /**
     * Returns the strikeout flag.
     *
     * @return boolean the strikeout flag.
     */
    public boolean getStrikeout() {
        return (properties & 0x00800000) != 0;
    }

    public void setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
    }

    public Font getFallbackFont() {
        return this.fallbackFont;
    }

    /**
     * Sets the vertical alignment of the text in this TextBox.
     *
     * @param valign - valid values are Align.TOP, Align.BOTTOM and Align.CENTER
     */
    public void setVerticalAlignment(int valign) {
        this.valign = valign;
    }

    public int getVerticalAlignment() {
        return this.valign;
    }

    public void setTextColors(Map<String, Integer> colors) {
        this.colors = colors;
    }

    public Map<String, Integer> getTextColors() {
        return this.colors;
    }

    public TextBox setLanguage(String language) {
        this.language = language;
        return this;
    }

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
        String[] lines = text.split("\\r?\\n", -1);
        for (String line : lines) {
            if (font.stringWidth(fallbackFont, line) <= textAreaWidth) {
                list.add(line);
            } else {
                if (textIsCJK(line)) {
                    StringBuilder sb = new StringBuilder();
                    for (char ch : line.toCharArray()) {
                        if (font.stringWidth(fallbackFont, sb.toString() + ch) <= textAreaWidth) {
                            sb.append(ch);
                        } else {
                            list.add(sb.toString());
                            sb.setLength(0);
                            sb.append(ch);
                        }
                    }
                    if (sb.length() > 0) {
                        list.add(sb.toString());
                    }
                } else {
                    StringBuilder sb = new StringBuilder();
                    String[] tokens = line.split("\\s+");
                    for (String token : tokens) {
                        if (font.stringWidth(fallbackFont, sb.toString() + token) <= textAreaWidth) {
                            sb.append(token + " ");
                        } else {
                            list.add(sb.toString().trim());
                            sb.setLength(0);
                            sb.append(token + " ");
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
                        xText = (x + width) - (font.stringWidth(fallbackFont, line) + margin);
                    } else if (getTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.stringWidth(fallbackFont, line))/2;
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
                        xText = (x + width) - (font.stringWidth(fallbackFont, line) + margin);
                    } else if (getTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.stringWidth(fallbackFont, line))/2;
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
            float lineLength = font.stringWidth(fallbackFont, text);
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
     */
    public void setURIAction(String uri) {
        this.uri = uri;
    }

    public void setTextDirection(Direction textDirection) {
        this.textDirection = textDirection;
    }
} // End of TextBox.java
