/*
 * TextLine.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.Map;

/**
 * Used to create text line objects.
 */
public class TextLine implements Drawable {
    protected float x;
    protected float y;
    protected Font font;
    protected Font fallbackFont;
    protected float fontSize;
    protected String text;
    protected boolean isLastToken = false;  // We need this for underline and strikeout to work properly!
    protected float xOffset;                // The horizontal offset (from the X coordinate)
    protected boolean underline = false;
    protected boolean strikeout = false;

    private int degrees = 0;
    private float[] textColor = new float[] {0f, 0f, 0f};
    private float[] lineColor = new float[] {0f, 0f, 0f};
    private Map<String, Integer> colorMap = null;
    private int textEffect = Effect.NORMAL;
    private float verticalOffset = 0f;

    private String uri;
    private String key;
    private String language = null;
    private String altDescription = null;
    private String uriLanguage = null;
    private String uriActualText = null;
    private String uriAltDescription = null;

    private String structureType = StructElem.P;


    /**
     * Constructor for creating text line objects.
     *
     * @param font the font to use.
     */
    public TextLine(Font font) {
        this.font = font;
        this.fallbackFont = font;
        this.fontSize = font.getSize();
    }

    /**
     * Constructor for creating text line objects.
     *
     * @param font the font to use.
     * @param text the text.
     */
    public TextLine(Font font, String text) {
        this.font = font;
        this.fallbackFont = font;
        this.fontSize = font.getSize();
        this.text = text;
        this.altDescription = text;
    }

    /**
     * Sets the text.
     *
     * @param text the text.
     * @return this TextLine.
     */
    public TextLine setText(String text) {
        this.text = text;
        this.altDescription = text;
        return this;
    }

    /**
     * Returns the text.
     *
     * @return the text.
     */
    public String getText() {
        return text;
    }

    /**
     * Sets the position where this text line will be drawn on the page.
     *
     * @param x the x coordinate of the text line.
     * @param y the y coordinate of the text line.
     */
    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    /**
     * Sets the position where this text line will be drawn on the page.
     *
     * @param x the x coordinate of the text line.
     * @param y the y coordinate of the text line.
     */
    public void setPosition(double x, double y) {
        setLocation(x, y);
    }

    /**
     * Sets the location where this text line will be drawn on the page.
     *
     * @param x the x coordinate of the text line.
     * @param y the y coordinate of the text line.
     * @return this TextLine.
     */
    public TextLine setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public TextLine setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    public float advance(float leading) {
        this.y += leading;
        return this.y;
    }

    /**
     * Sets the font to use for this text line.
     *
     * @param font the font to use.
     * @return this TextLine.
     */
    public TextLine setFont(Font font) {
        this.font = font;
        return this;
    }

    /**
     * Gets the font to use for this text line.
     *
     * @return font the font to use.
     */
    public Font getFont() {
        return font;
    }

    /**
     * Sets the font size to use for this text line.
     *
     * @param fontSize the fontSize to use.
     * @return this TextLine.
     */
    public TextLine setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    public float getFontSize() {
        return this.fontSize;
    }

    /**
     * Sets the fallback font.
     *
     * @param fallbackFont the fallback font.
     * @return this TextLine.
     */
    public TextLine setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /**
     * Sets the fallback font size to use for this text line.
     *
     * @param fallbackFontSize the fallback font size.
     * @return this TextLine.
     */
    public TextLine setFallbackFontSize(float fallbackFontSize) {
        this.fallbackFont.setSize(fallbackFontSize);
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
     * @deprecated Use {@link #setTextColor(int color)} instead.
     * @param color the color value.
     * @return the text line.
     */
    @Deprecated
    public TextLine setColor(int color) {
        return setTextColor(color);
    }

    public TextLine setTextColor(int color) {
        if (color == Color.transparent) {
            this.textColor = null;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
        return this;
    }

    public TextLine setTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
        return this;
    }

    public TextLine setTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    public float[] getTextColor() {
        return textColor;
    }

    public TextLine setLineColor(int color) {
        if (color == Color.transparent) {
            this.textColor = null;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.lineColor = new float[] {r, g, b};
        return this;
    }

    public TextLine setLineColor(float r, float g, float b) {
        this.lineColor = new float[] {r, g, b};
        return this;
    }

    public TextLine setLineColor(float[] rgbColor) {
        this.lineColor = rgbColor;
        return this;
    }

    public float[] getLineColor() {
        return lineColor;
    }

    public TextLine setColorMap(Map<String, Integer> colorMap) {
        this.colorMap = colorMap;
        return this;
    }

    public Map<String, Integer> getColorMap() {
        return this.colorMap;
    }

    /**
     * Returns the x coordinate of the destination.
     *
     * @return the x coordinate of the destination.
     */
    public float getDestinationX() {
        return x;
    }

    /**
     * Returns the y coordinate of the destination.
     *
     * @return the y coordinate of the destination.
     */
    public float getDestinationY() {
        return y - this.fontSize;
    }

    /**
     * Returns the width of this TextLine.
     *
     * @return the width.
     */
    public float getWidth() {
        return font.stringWidth(fallbackFont, this.fontSize, text);
    }

    /**
     * Returns the string width of the specified string.
     *
     * @param text the text string.
     * @return the width.
     */
    public float getStringWidth(String text) {
        return font.stringWidth(fallbackFont, text);
    }

    /**
     * Returns the height of this TextLine.
     *
     * @return the height.
     */
    public float getHeight() {
        return font.getBodyHeight(this.fontSize);
    }

    /**
     * Sets the URI for the "click text line" action.
     *
     * @param uri the URI
     * @return this TextLine.
     */
    public TextLine setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Returns the action URI.
     *
     * @return the action URI.
     */
    public String getURIAction() {
        return this.uri;
    }

    /**
     * Sets the destination key for the action.
     *
     * @param key the destination name.
     * @return this TextLine.
     */
    public TextLine setGoToAction(String key) {
        this.key = key;
        return this;
    }

    /**
     * Returns the GoTo action string.
     *
     * @return the GoTo action string.
     */
    public String getGoToAction() {
        return this.key;
    }

    /**
     * Sets the underline variable.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline flag.
     * @return this TextLine.
     */
    public TextLine setUnderline(boolean underline) {
        this.underline = underline;
        return this;
    }

    /**
     * Returns the underline flag.
     *
     * @return the underline flag.
     */
    public boolean getUnderline() {
        return this.underline;
    }

    /**
     * Sets the strike variable.
     * If the value of the strike variable is 'true' - a strike line is drawn through the text.
     *
     * @param strikeout the strikeout flag.
     * @return this TextLine.
     */
    public TextLine setStrikeout(boolean strikeout) {
        this.strikeout = strikeout;
        return this;
    }

    /**
     * Returns the strikeout flag.
     *
     * @return the strikeout flag.
     */
    public boolean getStrikeout() {
        return this.strikeout;
    }

    /**
     * Sets the direction in which to draw the text.
     *
     * @param degrees the number of degrees.
     * @return this TextLine.
     */
    public TextLine setTextDirection(int degrees) {
        this.degrees = degrees;
        return this;
    }

    /**
     * Returns the text direction.
     *
     * @return the text direction.
     */
    public int getTextDirection() {
        return degrees;
    }

    /**
     * Sets the text effect.
     *
     * @param textEffect Effect.NORMAL, Effect.SUBSCRIPT or Effect.SUPERSCRIPT.
     * @return this TextLine.
     */
    public TextLine setTextEffect(int textEffect) {
        this.textEffect = textEffect;
        if (textEffect == Effect.NORMAL) {
            verticalOffset = 0f;
        } else if (textEffect == Effect.SUPERSCRIPT) {
            verticalOffset = -font.getBodyHeight(this.fontSize)/2f;
        } else if (textEffect == Effect.SUBSCRIPT) {
            verticalOffset = font.getBodyHeight(this.fontSize)/3f;
        }
        return this;
    }

    /**
     * Returns the text effect.
     *
     * @return the text effect.
     */
    public int getTextEffect() {
        return textEffect;
    }

    /**
     * Sets the vertical offset of the text.
     *
     * @param verticalOffset the vertical offset.
     * @return this TextLine.
     */
    public TextLine setVerticalOffset(float verticalOffset) {
        this.verticalOffset = verticalOffset;
        return this;
    }

    /**
     * Returns the vertical text offset.
     *
     * @return the vertical text offset.
     */
    public float getVerticalOffset() {
        return verticalOffset;
    }

    public TextLine setLanguage(String language) {
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
     * @return this TextLine.
     */
    public TextLine setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    public String getAltDescription() {
        return altDescription;
    }

    public TextLine setURILanguage(String uriLanguage) {
        this.uriLanguage = uriLanguage;
        return this;
    }

    public TextLine setURIAltDescription(String uriAltDescription) {
        this.uriAltDescription = uriAltDescription;
        return this;
    }

    public TextLine setURIActualText(String uriActualText) {
        this.uriActualText = uriActualText;
        return this;
    }

    public TextLine setStructureType(String structureType) {
        this.structureType = structureType;
        return this;
    }

    /**
     * Draws this text line on the specified page.
     *
     * @param page the page to draw this text line on.
     * @return float[] with the coordinates of the bottom right corner.
     * @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        if (page == null || text == null || text.equals("")) {
            return new float[] {x, y};
        }

        page.setTextDirection(degrees);
        page.setBrushColor(textColor);
        page.addBMC(structureType, language, text, altDescription);
        page.drawString(font, fallbackFont, fontSize, text, x, y + verticalOffset, textColor, colorMap);
        page.addEMC();

        double radians = Math.PI * degrees / 180.0;
        if (underline) {
            page.setPenWidth(font.getUnderlineThickness(fontSize));
            page.setPenColor(lineColor);
            double lineLength = font.stringWidth(fallbackFont, fontSize, text);
            if (this.isLastToken) {
                lineLength -= font.stringWidth(fallbackFont, fontSize, Single.space);
            }
            double xAdjust = font.getUnderlinePosition(fontSize) * Math.sin(radians);
            double yAdjust = font.getUnderlinePosition(fontSize) * Math.cos(radians) + verticalOffset;
            double x2 = x + (lineLength * Math.cos(radians));
            double y2 = y - (lineLength * Math.sin(radians));
            page.addBMC(structureType, language, text, "Underlined text: " + text);
            page.moveTo(x + xAdjust, y + yAdjust);
            page.lineTo(x2 + xAdjust, y2 + yAdjust);
            page.strokePath();
            page.addEMC();
        }

        if (strikeout) {
            page.setPenWidth(font.getUnderlineThickness(fontSize));
            page.setPenColor(lineColor);
            double lineLength = font.stringWidth(fallbackFont, fontSize, text);
            if (this.isLastToken) {
                lineLength -= font.stringWidth(fallbackFont, fontSize, Single.space);
            }
            double xAdjust = (font.getBodyHeight(fontSize) / 4.0) * Math.sin(radians);
            double yAdjust = (font.getBodyHeight(fontSize) / 4.0) * Math.cos(radians) + verticalOffset;
            double x2 = x + lineLength * Math.cos(radians);
            double y2 = y - lineLength * Math.sin(radians);
            page.addBMC(structureType, language, text, "Strikethrough text: " + text);
            page.moveTo(x - xAdjust, y - yAdjust);
            page.lineTo(x2 - xAdjust, y2 - yAdjust);
            page.strokePath();
            page.addEMC();
        }

        if (uri != null || key != null) {
            page.addAnnotation(new Annotation(              // TODO: Check this code!
                    Annotation.Link,
                    x,
                    (y + verticalOffset) - font.getAscent(),
                    x + font.stringWidth(fallbackFont, fontSize, text),
                    (y + verticalOffset) + font.getDescent(),
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

        float len = font.stringWidth(fallbackFont, text);   // TODO: Check this code!
        double xMax = Math.max(x, x + len*Math.cos(radians));
        double yMax = Math.max(y + verticalOffset, ((y + verticalOffset) - len) * Math.sin(radians));

        return new float[] {(float) xMax, (float) yMax};
    }
}   // End of TextLine.java
