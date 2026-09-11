/*
 * TextLine.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create text line objects.
/// </summary>
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

    /// <summary>
    /// Constructor for creating text line objects.
    /// </summary>
    /// <param name="font">the font to use.</param>
    public TextLine(Font font) {
        this.font = font;
        this.fallbackFont = font;
        this.fontSize = font.GetSize();
    }

    /// <summary>
    /// Constructor for creating text line objects.
    /// </summary>
    /// <param name="font">the font to use.</param>
    /// <param name="text">the text.</param>
    public TextLine(Font font, String text) {
        this.font = font;
        this.fallbackFont = font;
        this.fontSize = font.GetSize();
        this.text = text;
        this.altDescription = text;
    }

    /// <summary>
    /// Sets the text.
    /// </summary>
    /// <param name="text">the text.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetText(String text) {
        this.text = text;
        this.altDescription = text;
        return this;
    }

    /// <summary>
    /// Returns the text.
    /// </summary>
    /// <returns>the text.</returns>
    public String GetText() {
        return text;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location where this text line will be drawn on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the text line.</param>
    /// <param name="y">the y coordinate of the text line.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location where this text line is drawn on the page.</summary>
    public TextLine SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /// <summary>
    /// Sets the font to use for this text line.
    /// </summary>
    /// <param name="font">the font to use.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetFont(Font font) {
        this.font = font;
        return this;
    }

    /// <summary>
    /// Gets the font to use for this text line.
    /// </summary>
    /// <returns>font the font to use.</returns>
    public Font GetFont() {
        return font;
    }

    /// <summary>
    /// Sets the font size to use for this text line.
    /// </summary>
    /// <param name="fontSize">the fontSize to use.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /// <summary>Returns the font size.</summary>
    public float GetFontSize() {
        return this.fontSize;
    }

    /// <summary>
    /// Sets the fallback font.
    /// </summary>
    /// <param name="fallbackFont">the fallback font.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /// <summary>
    /// Returns the fallback font.
    /// </summary>
    /// <returns>the fallback font.</returns>
    public Font GetFallbackFont() {
        return this.fallbackFont;
    }

    /// <summary>Sets the text color as a 0xRRGGBB value. Color.transparent clears it.</summary>
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

    /// <summary>Sets the text color from red, green and blue values between 0.0 and 1.0.</summary>
    public TextLine SetTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the text color from an array of red, green and blue values.</summary>
    public TextLine SetTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    /// <summary>Returns the text color.</summary>
    public float[] GetTextColor() {
        return textColor;
    }

    /// <summary>Sets the color of the underline and strikeout lines as a 0xRRGGBB value. Color.transparent leaves it unchanged.</summary>
    public TextLine SetLineColor(int color) {
        if (color == Color.transparent) {
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

    /// <summary>Sets the color of the underline and strikeout lines from an array of red, green and blue values.</summary>
    public TextLine SetLineColor(float[] rgbColor) {
        this.lineColor = rgbColor;
        return this;
    }

    /// <summary>Returns the color of the underline and strikeout lines.</summary>
    public float[] GetLineColor() {
        return lineColor;
    }

    /// <summary>Sets the colors used to highlight words in the text.</summary>
    public TextLine SetColorMap(Dictionary<String, int> colorMap) {
        this.colorMap = colorMap;
        return this;
    }

    /// <summary>Returns the colors used to highlight words in the text.</summary>
    public Dictionary<String, int> GetColorMap() {
        return this.colorMap;
    }

    /// <summary>
    /// Returns the x coordinate of the destination.
    /// </summary>
    /// <returns>the x coordinate of the destination.</returns>
    public float GetDestinationX() {
        return x;
    }

    /// <summary>
    /// Returns the y coordinate of the destination.
    /// </summary>
    /// <returns>the y coordinate of the destination.</returns>
    public float GetDestinationY() {
        return y - this.fontSize;
    }

    /// <summary>
    /// Returns the width of this TextLine.
    /// </summary>
    /// <returns>the width.</returns>
    public float GetWidth() {
        return font.StringWidth(fallbackFont, this.fontSize, text);
    }

    /// <summary>
    /// Returns the string width of the specified string.
    /// </summary>
    /// <returns>the width.</returns>
    public float GetStringWidth(String text) {
        return font.StringWidth(fallbackFont, this.fontSize, text);
    }

    /// <summary>
    /// Returns the height of this TextLine.
    /// </summary>
    /// <returns>the height.</returns>
    public float GetHeight() {
        return font.GetBodyHeight(this.fontSize);
    }

    /// <summary>
    /// Sets the URI for the "click text line" action.
    /// </summary>
    /// <param name="uri">the URI</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>
    /// Returns the action URI.
    /// </summary>
    /// <returns>the action URI.</returns>
    public String GetURIAction() {
        return this.uri;
    }

    /// <summary>
    /// Sets the destination key for the action.
    /// </summary>
    /// <param name="key">the destination name.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetGoToAction(String key) {
        this.key = key;
        return this;
    }

    /// <summary>
    /// Returns the GoTo action string.
    /// </summary>
    /// <returns>the GoTo action string.</returns>
    public String GetGoToAction() {
        return this.key;
    }

    /// <summary>
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    /// </summary>
    /// <param name="underline">the underline flag.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetUnderline(bool underline) {
        this.underline = underline;
        return this;
    }

    /// <summary>
    /// Returns the underline flag.
    /// </summary>
    /// <returns>the underline flag.</returns>
    public bool GetUnderline() {
        return this.underline;
    }

    /// <summary>
    /// Sets the strike variable.
    /// If the value of the strike variable is 'true' - a strike line is drawn through the text.
    /// </summary>
    /// <param name="strike">the strike value.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetStrikeout(bool strike) {
        this.strikeout = strike;
        return this;
    }

    /// <summary>
    /// Returns the strikeout flag.
    /// </summary>
    /// <returns>the strikeout flag.</returns>
    public bool GetStrikeout() {
        return this.strikeout;
    }

    /// <summary>
    /// Sets the direction in which to draw the text.
    /// </summary>
    /// <param name="degrees">the number of degrees.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetTextDirection(int degrees) {
        this.degrees = degrees;
        return this;
    }

    /// <summary>
    /// Returns the text direction.
    /// </summary>
    /// <returns>the text direction.</returns>
    public int GetTextDirection() {
        return degrees;
    }

    /// <summary>
    /// Sets the text effect.
    /// </summary>
    /// <param name="textEffect">Effect.NORMAL, Effect.SUBSCRIPT or Effect.SUPERSCRIPT.</param>
    /// <returns>this TextLine.</returns>
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

    /// <summary>
    /// Returns the text effect.
    /// </summary>
    /// <returns>the text effect.</returns>
    public int GetTextEffect() {
        return textEffect;
    }

    /// <summary>
    /// Sets the vertical offset of the text.
    /// </summary>
    /// <param name="verticalOffset">the vertical offset.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetVerticalOffset(float verticalOffset) {
        this.verticalOffset = verticalOffset;
        return this;
    }

    /// <summary>
    /// Returns the vertical text offset.
    /// </summary>
    /// <returns>the vertical text offset.</returns>
    public float GetVerticalOffset() {
        return verticalOffset;
    }

    /// <summary>Sets the language of the text, for example "en-US".</summary>
    public TextLine SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>Returns the language of the text.</summary>
    public String GetLanguage() {
        return this.language;
    }

    /// <summary>
    /// Sets the alternate description of this text line.
    /// </summary>
    /// <param name="altDescription">the alternate description of the text line.</param>
    /// <returns>this TextLine.</returns>
    public TextLine SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>Returns the alternate description of this text line.</summary>
    public String GetAltDescription() {
        return altDescription;
    }

    /// <summary>Sets the language of the link annotation.</summary>
    public TextLine SetURILanguage(String uriLanguage) {
        this.uriLanguage = uriLanguage;
        return this;
    }

    /// <summary>Sets the alternate description of the link annotation.</summary>
    public TextLine SetURIAltDescription(String uriAltDescription) {
        this.uriAltDescription = uriAltDescription;
        return this;
    }

    /// <summary>Sets the actual text of the link annotation.</summary>
    public TextLine SetURIActualText(String uriActualText) {
        this.uriActualText = uriActualText;
        return this;
    }

    /// <summary>Sets the structure element type, for example StructElem.P or StructElem.H1.</summary>
    public TextLine SetStructureType(String structureType) {
        this.structureType = structureType;
        return this;
    }

    /// <summary>Moves this text line down by the leading and returns the new y coordinate.</summary>
    public float Advance(float leading) {
        this.y += leading;
        return this.y;
    }

    /// <summary>
    /// Draws this text line on the specified page if is not null.
    /// </summary>
    /// <param name="page">the page to draw this text line on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        if (page == null || text == null || text.Equals("")) {
            return new float[] {x, y};
        }

        page.SetTextDirection(degrees);
        page.SetBrushColor(textColor);
        // The text is drawn, so it is not given again as actual text, or as its
        // own alternate description: right to left text is drawn in visual
        // order, and would be read backwards.
        String alt = text.Equals(altDescription) ? null : altDescription;
        page.AddBMC(structureType, language, null, alt);
        page.DrawString(font, fallbackFont, fontSize, text, x, y + verticalOffset, textColor, colorMap);
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
            double yAdjust = font.GetUnderlinePosition(fontSize) * Math.Cos(radians) + verticalOffset;
            double x2 = x + lineLength * Math.Cos(radians);
            double y2 = y - lineLength * Math.Sin(radians);
            page.AddBMC(structureType, language, null, "Underlined text: " + text);
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
            double yAdjust = (font.GetBodyHeight(fontSize) / 4f) * Math.Cos(radians) + verticalOffset;
            double x2 = x + lineLength * Math.Cos(radians);
            double y2 = y - lineLength * Math.Sin(radians);
            page.AddBMC(structureType, language, null, "Strikethrough text: " + text);
            page.MoveTo(x - xAdjust, y - yAdjust);
            page.LineTo(x2 - xAdjust, y2 - yAdjust);
            page.StrokePath();
            page.AddEMC();
        }

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x,
                    (y + verticalOffset) - font.GetAscent(fontSize),
                    x + font.StringWidth(fallbackFont, fontSize, text),
                    (y + verticalOffset) + font.GetDescent(fontSize),
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
        double yMax = Math.Max((double) (y + verticalOffset), (y + verticalOffset) - len*Math.Sin(radians));

        return new float[] {(float) xMax, (float) yMax};
    }
}   // End of TextLine.cs
}   // End of namespace PDFjet.NET
