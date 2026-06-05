/**
 * TextLine.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 * Used to create text line objects.
 */
public class TextLine : IDrawable {
    internal float x;
    internal float y;
    internal Font font;
    internal Font fallbackFont;
    internal float fontSize;
    internal String text;
    internal bool isLastToken = false;  // We need this for underline and strikeout to work properly!
    internal float xOffset = 0f;        // The horizontal offset (from the X coordinate)
    internal bool underline = false;
    internal bool strikeout = false;

    private int degrees = 0;
    private float[] textColor = new float[] {0f, 0f, 0f};
    private float[] lineColor = new float[] {0f, 0f, 0f};
    private Dictionary<String, int> colorMap = null;
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
        this.fontSize = font.GetSize();
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
        this.fontSize = font.GetSize();
        this.text = text;
        this.altDescription = text;
    }

    /**
     * Sets the text.
     *
     * @param text the text.
     * @return this TextLine.
     */
    public TextLine SetText(String text) {
        this.text = text;
        this.altDescription = text;
        return this;
    }

    /**
     * Returns the text.
     *
     * @return the text.
     */
    public String GetText() {
        return text;
    }

    /**
     * Sets the position where this text line will be drawn on the page.
     *
     * @param x the x coordinate of the text line.
     * @param y the y coordinate of the text line.
     * @return this TextLine.
     */
    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    /**
     * Sets the position where this text line will be drawn on the page.
     *
     * @param x the x coordinate of the text line.
     * @param y the y coordinate of the text line.
     * @return this TextLine.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public TextLine SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     * Sets the location where this text line will be drawn on the page.
     *
     * @param x the x coordinate of the text line.
     * @param y the y coordinate of the text line.
     * @return this TextLine.
     */
    public TextLine SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the font to use for this text line.
     *
     * @param font the font to use.
     * @return this TextLine.
     */
    public TextLine SetFont(Font font) {
        this.font = font;
        return this;
    }

    /**
     * Gets the font to use for this text line.
     *
     * @return font the font to use.
     */
    public Font GetFont() {
        return font;
    }

    /**
     * Sets the font size to use for this text line.
     *
     * @param fontSize the fontSize to use.
     * @return this TextLine.
     */
    public TextLine SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    public float GetFontSize() {
        return this.fontSize;
    }

    /**
     * Sets the fallback font.
     *
     * @param fallbackFont the fallback font.
     * @return this TextLine.
     */
    public TextLine SetFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /**
     * Returns the fallback font.
     *
     * @return the fallback font.
     */
    public Font GetFallbackFont() {
        return this.fallbackFont;
    }

    [Obsolete]
    public TextLine SetColor(int color) {
        return SetTextColor(color);
    }

    public TextLine SetTextColor(int color) {
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

    public TextLine SetTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
        return this;
    }

    public TextLine SetTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    public float[] GetTextColor() {
        return textColor;
    }

    public TextLine SetLineColor(int color) {
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

//    public TextLine SetLineColor(float r, float g, float b) {
//        this.lineColor = new float[] {r, g, b};
//        return this;
//    }

    public TextLine SetLineColor(float[] rgbColor) {
        this.lineColor = rgbColor;
        return this;
    }

    public float[] GetLineColor() {
        return lineColor;
    }

    public TextLine SetColorMap(Dictionary<String, int> colorMap) {
        this.colorMap = colorMap;
        return this;
    }

    public Dictionary<String, int> GetColorMap() {
        return this.colorMap;
    }

    /**
     * Returns the x coordinate of the destination.
     *
     * @return the x coordinate of the destination.
     */
    public float GetDestinationX() {
        return x;
    }

    /**
     * Returns the y coordinate of the destination.
     *
     * @return the y coordinate of the destination.
     */
    public float GetDestinationY() {
        return y - this.fontSize;
    }

    /**
     * Returns the width of this TextLine.
     *
     * @return the width.
     */
    public float GetWidth() {
        return font.StringWidth(fallbackFont, this.fontSize, text);
    }

    /**
     * Returns the string width of the specified string.
     *
     * @return the width.
     */
    public float GetStringWidth(String text) {      // TODO: Check TextFrame.cs
        return font.StringWidth(fallbackFont, text);
    }

    /**
     * Returns the height of this TextLine.
     *
     * @return the height.
     */
    public float GetHeight() {
        return font.GetBodyHeight(this.fontSize);
    }

    /**
     * Sets the URI for the "click text line" action.
     *
     * @param uri the URI
     * @return this TextLine.
     */
    public TextLine SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Returns the action URI.
     *
     * @return the action URI.
     */
    public String GetURIAction() {
        return this.uri;
    }

    /**
     * Sets the destination key for the action.
     *
     * @param key the destination name.
     * @return this TextLine.
     */
    public TextLine SetGoToAction(String key) {
        this.key = key;
        return this;
    }

    /**
     * Returns the GoTo action string.
     *
     * @return the GoTo action string.
     */
    public String GetGoToAction() {
        return this.key;
    }

    /**
     * Sets the underline variable.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline flag.
     * @return this TextLine.
     */
    public TextLine SetUnderline(bool underline) {
        this.underline = underline;
        return this;
    }

    /**
     * Returns the underline flag.
     *
     * @return the underline flag.
     */
    public bool GetUnderline() {
        return this.underline;
    }

    /**
     * Sets the strike variable.
     * If the value of the strike variable is 'true' - a strike line is drawn through the text.
     *
     * @param strike the strike value.
     * @return this TextLine.
     */
    public TextLine SetStrikeout(bool strike) {
        this.strikeout = strike;
        return this;
    }

    /**
     * Returns the strikeout flag.
     *
     * @return the strikeout flag.
     */
    public bool GetStrikeout() {
        return this.strikeout;
    }

    /**
     * Sets the direction in which to draw the text.
     *
     * @param degrees the number of degrees.
     * @return this TextLine.
     */
    public TextLine SetTextDirection(int degrees) {
        this.degrees = degrees;
        return this;
    }

    /**
     * Returns the text direction.
     *
     * @return the text direction.
     */
    public int GetTextDirection() {
        return degrees;
    }

    /**
     * Sets the text effect.
     *
     * @param textEffect Effect.NORMAL, Effect.SUBSCRIPT or Effect.SUPERSCRIPT.
     * @return this TextLine.
     */
    public TextLine SetTextEffect(int textEffect) {
        this.textEffect = textEffect;
        if (textEffect == Effect.NORMAL) {
            verticalOffset = 0f;
        } else if (textEffect == Effect.SUPERSCRIPT) {
            verticalOffset = -font.GetBodyHeight(this.fontSize)/2f;
        } else if (textEffect == Effect.SUBSCRIPT) {
            verticalOffset = font.GetBodyHeight(this.fontSize)/3f;
        }
        return this;
    }

    /**
     * Returns the text effect.
     *
     * @return the text effect.
     */
    public int GetTextEffect() {
        return textEffect;
    }

    /**
     * Sets the vertical offset of the text.
     *
     * @param verticalOffset the vertical offset.
     * @return this TextLine.
     */
    public TextLine SetVerticalOffset(float verticalOffset) {
        this.verticalOffset = verticalOffset;
        return this;
    }

    /**
     * Returns the vertical text offset.
     *
     * @return the vertical text offset.
     */
    public float GetVerticalOffset() {
        return verticalOffset;
    }

    public TextLine SetLanguage(String language) {
        this.language = language;
        return this;
    }

    public String GetLanguage() {
        return this.language;
    }

    /**
     * Sets the alternate description of this text line.
     *
     * @param altDescription the alternate description of the text line.
     * @return this TextLine.
     */
    public TextLine SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    public String GetAltDescription() {
        return altDescription;
    }

    public TextLine SetURILanguage(String uriLanguage) {
        this.uriLanguage = uriLanguage;
        return this;
    }

    public TextLine SetURIAltDescription(String uriAltDescription) {
        this.uriAltDescription = uriAltDescription;
        return this;
    }

    public TextLine SetURIActualText(String uriActualText) {
        this.uriActualText = uriActualText;
        return this;
    }

    public TextLine SetStructureType(String structureType) {
        this.structureType = structureType;
        return this;
    }

    /**
     * Places this text line in the specified box at position (0.0, 0.0).
     *
     * @param box the specified box.
     * @return this TextLine.
     */
    public TextLine PlaceIn(Box box) {
        PlaceIn(box, 0.0, 0.0);
        return this;
    }

    /**
     * Places this text line in the box at the specified offset.
     *
     * @param box the specified box.
     * @param xOffset the x offset from the top left corner of the box.
     * @param yOffset the y offset from the top left corner of the box.
     * @return this TextLine.
     */
    public TextLine PlaceIn(
            Box box,
            double xOffset,
            double yOffset) {
        return PlaceIn(box, (float) xOffset, (float) yOffset);
    }

    public float Advance(float leading) {
        this.y += leading;
        return this.y;
    }

    /**
     * Draws this text line on the specified page if is not null.
     *
     * @param page the page to draw this text line on.
     * @param draw if draw is false - no action is performed.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (page == null || text == null || text.Equals("")) {
            return new float[] {x, y};
        }

        page.SetTextDirection(degrees);
        page.SetBrushColor(textColor);
        page.AddBMC(structureType, language, text, altDescription);
        page.DrawString(font, fallbackFont, fontSize, text, x, y, textColor, colorMap);
        page.AddEMC();

        double radians = Math.PI * degrees / 180.0;
        if (underline) {
            page.SetPenWidth(font.GetUnderlineThickness(fontSize));
            page.SetPenColor(lineColor);
            double lineLength = font.StringWidth(fallbackFont, fontSize, text);
            if (this.isLastToken) {
                lineLength -= font.StringWidth(fallbackFont, fontSize, Single.space);
            }
            double xAdjust = font.GetUnderlinePosition(fontSize) * Math.Sin(radians);
            double yAdjust = font.GetUnderlinePosition(fontSize) * Math.Cos(radians);
            double x2 = x + lineLength * Math.Cos(radians);
            double y2 = y - lineLength * Math.Sin(radians);
            page.AddBMC(structureType, language, text, "Underlined text: " + text);
            page.MoveTo(x + xAdjust, y + yAdjust);
            page.LineTo(x2 + xAdjust, y2 + yAdjust);
            page.StrokePath();
            page.AddEMC();
        }

        if (strikeout) {
            page.SetPenWidth(font.GetUnderlineThickness(fontSize));
            page.SetPenColor(lineColor);
            double lineLength = font.StringWidth(fallbackFont, fontSize, text);
            if (this.isLastToken) {
                lineLength -= font.StringWidth(fallbackFont, fontSize, Single.space);
            }
            double xAdjust = (font.GetBodyHeight(fontSize) / 4f) * Math.Sin(radians);
            double yAdjust = (font.GetBodyHeight(fontSize) / 4f) * Math.Cos(radians);
            double x2 = x + lineLength * Math.Cos(radians);
            double y2 = y - lineLength * Math.Sin(radians);
            page.AddBMC(structureType, language, text, "Strikethrough text: " + text);
            page.MoveTo(x - xAdjust, y - yAdjust);
            page.LineTo(x2 - xAdjust, y2 - yAdjust);
            page.StrokePath();
            page.AddEMC();
        }

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x,
                    y - font.GetAscent(fontSize),
                    x + font.StringWidth(fallbackFont, fontSize, text),
                    y + font.GetDescent(fontSize),
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
        page.SetTextDirection(0);

        float len = font.StringWidth(fallbackFont, fontSize, text);
        double xMax = Math.Max((double) x, x + len*Math.Cos(radians));
        double yMax = Math.Max((double) y, y - len*Math.Sin(radians));

        return new float[] {(float) xMax, (float) yMax};
    }
}   // End of TextLine.cs
}   // End of namespace PDFjet.NET
