/**
 *  TextLine.cs
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;


namespace PDFjet.NET {
/**
 *  Used to create text line objects.
 */
public class TextLine : IDrawable {

    internal float x;
    internal float y;

    internal Font font;
    internal Font fallbackFont;
    internal String text;
    internal bool trailingSpace = true;

    private String uri;
    private String key;

    private bool underline = false;
    private bool strikeout = false;

    private int degrees = 0;
    private int color = Color.black;

    private float xBox;
    private float yBox;

    private int textEffect = Effect.NORMAL;
    private float verticalOffset = 0f;

    private String language = null;
    private String altDescription = null;

    private String uriLanguage = null;
    private String uriActualText = null;
    private String uriAltDescription = null;


    /**
     *  Constructor for creating text line objects.
     *
     *  @param font the font to use.
     */
    public TextLine(Font font) {
        this.font = font;
    }


    /**
     *  Constructor for creating text line objects.
     *
     *  @param font the font to use.
     *  @param text the text.
     */
    public TextLine(Font font, String text) {
        this.font = font;
        this.text = text;
        this.altDescription = text;
    }


    /**
     *  Sets the text.
     *
     *  @param text the text.
     *  @return this TextLine.
     */
    public TextLine SetText(String text) {
        this.text = text;
        this.altDescription = text;
        return this;
    }


    /**
     *  Returns the text.
     *
     *  @return the text.
     */
    public String GetText() {
        return text;
    }


    /**
     *  Sets the position where this text line will be drawn on the page.
     *
     *  @param x the x coordinate of the text line.
     *  @param y the y coordinate of the text line.
     *  @return this TextLine.
     */
    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }


    /**
     *  Sets the position where this text line will be drawn on the page.
     *
     *  @param x the x coordinate of the text line.
     *  @param y the y coordinate of the text line.
     *  @return this TextLine.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public TextLine SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }


    /**
     *  Sets the location where this text line will be drawn on the page.
     *
     *  @param x the x coordinate of the text line.
     *  @param y the y coordinate of the text line.
     *  @return this TextLine.
     */
    public TextLine SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }


    /**
     *  Sets the font to use for this text line.
     *
     *  @param font the font to use.
     *  @return this TextLine.
     */
    public TextLine SetFont(Font font) {
        this.font = font;
        return this;
    }


    /**
     *  Gets the font to use for this text line.
     *
     *  @return font the font to use.
     */
    public Font GetFont() {
        return font;
    }


    /**
     *  Sets the font size to use for this text line.
     *
     *  @param fontSize the fontSize to use.
     *  @return this TextLine.
     */
    public TextLine SetFontSize(float fontSize) {
        this.font.SetSize(fontSize);
        return this;
    }


    /**
     *  Sets the fallback font.
     *
     *  @param fallbackFont the fallback font.
     *  @return this TextLine.
     */
    public TextLine SetFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }


    /**
     *  Returns the fallback font.
     *
     *  @return the fallback font.
     */
    public Font GetFallbackFont() {
        return this.fallbackFont;
    }


    /**
     *  Sets the color for this text line.
     *
     *  @param color the color specified as an integer.
     *  @return this TextLine.
     */
    public TextLine SetColor(int color) {
        this.color = color;
        return this;
    }


    /**
     *  Returns the text line color.
     *
     *  @return the text line color.
     */
    public int GetColor() {
        return color;
    }


    /**
     * Returns the y coordinate of the destination.
     *
     * @return the y coordinate of the destination.
     */
    public float GetDestinationY() {
        return y - font.GetSize();
    }


    /**
     *  Returns the width of this TextLine.
     *
     *  @return the width.
     */
    public float GetWidth() {
        return font.StringWidth(fallbackFont, text);
    }


    public float GetStringWidth(String text) {
        return font.StringWidth(fallbackFont, text);
    }


    /**
     *  Returns the height of this TextLine.
     *
     *  @return the height.
     */
    public double GetHeight() {
        return font.GetHeight();
    }


    /**
     *  Sets the URI for the "click text line" action.
     *
     *  @param uri the URI
     *  @return this TextLine.
     */
    public TextLine SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }


    /**
     *  Returns the action URI.
     *
     *  @return the action URI.
     */
    public String GetURIAction() {
        return this.uri;
    }


    /**
     *  Sets the destination key for the action.
     *
     *  @param key the destination name.
     *  @return this TextLine.
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
     *  Sets the underline variable.
     *  If the value of the underline variable is 'true' - the text is underlined.
     *
     *  @param underline the underline flag.
     *  @return this TextLine.
     */
    public TextLine SetUnderline(bool underline) {
        this.underline = underline;
        return this;
    }


    /**
     *  Returns the underline flag.
     *
     *  @return the underline flag.
     */
    public bool GetUnderline() {
        return this.underline;
    }


    /**
     *  Sets the strike variable.
     *  If the value of the strike variable is 'true' - a strike line is drawn through the text.
     *
     *  @param strike the strike value.
     *  @return this TextLine.
     */
    public TextLine SetStrikeout(bool strike) {
        this.strikeout = strike;
        return this;
    }


    /**
     *  Returns the strikeout flag.
     *
     *  @return the strikeout flag.
     */
    public bool GetStrikeout() {
        return this.strikeout;
    }


    /**
     *  Sets the direction in which to draw the text.
     *
     *  @param degrees the number of degrees.
     *  @return this TextLine.
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
     *  Sets the text effect.
     *
     *  @param textEffect Effect.NORMAL, Effect.SUBSCRIPT or Effect.SUPERSCRIPT.
     *  @return this TextLine.
     */
    public TextLine SetTextEffect(int textEffect) {
        this.textEffect = textEffect;
        return this;
    }


    /**
     *  Returns the text effect.
     *
     *  @return the text effect.
     */
    public int GetTextEffect() {
        return textEffect;
    }


    /**
     *  Sets the vertical offset of the text.
     *
     *  @param verticalOffset the vertical offset.
     *  @return this TextLine.
     */
    public TextLine SetVerticalOffset(float verticalOffset) {
        this.verticalOffset = verticalOffset;
        return this;
    }


    /**
     *  Returns the vertical text offset.
     *
     *  @return the vertical text offset.
     */
    public float GetVerticalOffset() {
        return verticalOffset;
    }


    /**
     *  Sets the trailing space after this text line when used in paragraph.
     *
     *  @param trailingSpace the trailing space.
     *  @return this TextLine.
     */
    public TextLine SetTrailingSpace(bool trailingSpace) {
        this.trailingSpace = trailingSpace;
        return this;
    }


    /**
     *  Returns the trailing space.
     *
     *  @return the trailing space.
     */
    public bool GetTrailingSpace() {
        return trailingSpace;
    }


    public TextLine SetLanguage(String language) {
        this.language = language;
        return this;
    }


    public String GetLanguage() {
        return this.language;
    }


    /**
     *  Sets the alternate description of this text line.
     *
     *  @param altDescription the alternate description of the text line.
     *  @return this TextLine.
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


    /**
     *  Places this text line in the specified box at position (0.0, 0.0).
     *
     *  @param box the specified box.
     *  @return this TextLine.
     */
    public TextLine PlaceIn(Box box) {
        PlaceIn(box, 0.0, 0.0);
        return this;
    }


    /**
     *  Places this text line in the box at the specified offset.
     *
     *  @param box the specified box.
     *  @param xOffset the x offset from the top left corner of the box.
     *  @param yOffset the y offset from the top left corner of the box.
     *  @return this TextLine.
     */
    public TextLine PlaceIn(
            Box box,
            double xOffset,
            double yOffset) {
        return PlaceIn(box, (float) xOffset, (float) yOffset);
    }


    /**
     *  Places this text line in the box at the specified offset.
     *
     *  @param box the specified box.
     *  @param xOffset the x offset from the top left corner of the box.
     *  @param yOffset the y offset from the top left corner of the box.
     *  @return this TextLine.
     */
    public TextLine PlaceIn(
            Box box,
            float xOffset,
            float yOffset) {
        xBox = box.x + xOffset;
        yBox = box.y + yOffset;
        return this;
    }


    public float Advance(float leading) {
        this.y += leading;
        return this.y;
    }


    /**
     *  Draws this text line on the specified page if is not null.
     *
     *  @param page the page to draw this text line on.
     *  @param draw if draw is false - no action is performed.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (page == null || text == null || text.Equals("")) {
            return new float[] {x, y};
        }

        page.SetTextDirection(degrees);

        x += xBox;
        y += yBox;

        page.SetBrushColor(color);
        page.AddBMC(StructElem.P, language, text, altDescription);
        page.DrawString(font, fallbackFont, text, x, y);
        page.AddEMC();

        double radians = Math.PI * degrees / 180.0;
        if (underline) {
            page.SetPenWidth(font.underlineThickness);
            page.SetPenColor(color);
            double lineLength = font.StringWidth(fallbackFont, text);
            double xAdjust = font.underlinePosition * Math.Sin(radians) + verticalOffset;
            double yAdjust = font.underlinePosition * Math.Cos(radians) + verticalOffset;
            double x2 = x + lineLength * Math.Cos(radians);
            double y2 = y - lineLength * Math.Sin(radians);
            page.AddBMC(StructElem.P, language, text, "Underlined text: " + text);
            page.MoveTo(x + xAdjust, y + yAdjust);
            page.LineTo(x2 + xAdjust, y2 + yAdjust);
            page.StrokePath();
            page.AddEMC();
        }

        if (strikeout) {
            page.SetPenWidth(font.underlineThickness);
            page.SetPenColor(color);
            double lineLength = font.StringWidth(fallbackFont, text);
            double xAdjust = ( font.bodyHeight / 4.0 ) * Math.Sin(radians);
            double yAdjust = ( font.bodyHeight / 4.0 ) * Math.Cos(radians);
            double x2 = x + lineLength * Math.Cos(radians);
            double y2 = y - lineLength * Math.Sin(radians);
            page.AddBMC(StructElem.P, language, text, "Strikethrough text: " + text);
            page.MoveTo(x - xAdjust, y - yAdjust);
            page.LineTo(x2 - xAdjust, y2 - yAdjust);
            page.StrokePath();
            page.AddEMC();
        }

        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y - font.ascent,
                    x + font.StringWidth(fallbackFont, text),
                    y + font.descent,
                    uriLanguage,
                    uriActualText,
                    uriAltDescription));
        }
        page.SetTextDirection(0);

        float len = font.StringWidth(fallbackFont, text);
        double xMax = Math.Max((double) x, x + len*Math.Cos(radians));
        double yMax = Math.Max((double) y, y - len*Math.Sin(radians));

        return new float[] {(float) xMax, (float) yMax};
    }

}   // End of TextLine.cs
}   // End of namespace PDFjet.NET
