/**
 * TextBox.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 * A box containing line-wrapped text.
 *
 * <p>Defaults:<br />
 * x = 0f<br />
 * y = 0f<br />
 * width = 300f<br />
 * height = 0f<br />
 * alignment = Align.LEFT<br />
 * valign = Align.TOP<br />
 * spacing = 0f<br />
 * margin = 0f<br />
 * </p>
 *
 * This class was originally developed by Ronald Bourret.
 * It was completely rewritten in 2013 by Evgeni Dragoev.
 */
public class TextBox : IDrawable {
    internal Font font;
    internal Font fallbackFont;
    internal float fontSize = 12f;
    internal String text;
    internal float x;
    internal float y;
    internal float width = 300f;
    internal float height = 0f;
    internal float spacing = 0f;
    internal float margin = 0f;
    internal float lineWidth = 0f;

    private float[] fillColor;  // The background fill color
    private float[] textColor = new float[] {0f, 0f, 0f};
    private float strokeWidth = 0.5f;
    private float[] strokeColor;

    private uint valign = Align.TOP;
    private Dictionary<String, Int32> colors = null;
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
    private uint properties = 0x00000001;
    private String language = "en-US";
    private String altDescription = null;
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
     * @param font the font.
     * @param text the text.
     * @param width the width.
     * @param height the height.
     */
    public TextBox(Font font, String text, double width, double height) :
        this(font, text, (float) width, (float) height) {
    }

    /**
     * Creates a text box and sets the font and the text.
     *
     * @param font the font.
     * @param text the text.
     * @param width the width.
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
    public void SetFont(Font font) {
        this.font = font;
    }

    /**
     * Returns the font used by this text box.
     *
     * @return the font.
     */
    public Font GetFont() {
        return font;
    }

    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    /**
     * Sets the text box text.
     *
     * @param text the text box text.
     */
    public void SetText(String text) {
        this.text = text;
    }

    /**
     * Returns the text box text.
     *
     * @return the text box text.
     */
    public String GetText() {
        return text;
    }

    /**
     * Sets the position where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     * Sets the position where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public void SetPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     * Sets the location where this text box will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the text box.
     * @param y the y coordinate of the top left corner of the text box.
     */
    public TextBox SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the size of text box.
     *
     * @param w the width of the text box.
     * @param h the height of the text box.
     */
    public void SetSize(float w, float h) {
        this.width = w;
        this.height = h;
    }

    /**
     * Gets the location where this text box will be drawn on the page.
     *
     * @return the float array of of x and y.
     */
    public float[] GetLocation() {
        return new float[] { this.x, this.y };
    }

    /**
     * Sets the width of this text box.
     *
     * @param width the specified width.
     */
    public void SetWidth(double width) {
        this.width = (float) width;
    }

    /**
     * Sets the width of this text box.
     *
     * @param width the specified width.
     */
    public void SetWidth(float width) {
        this.width = width;
    }

    /**
     * Returns the text box width.
     *
     * @return the text box width.
     */
    public float GetWidth() {
        return width;
    }

    /**
     * Sets the height of this text box.
     *
     * @param height the specified height.
     */
    public TextBox SetHeight(double height) {
        this.height = (float) height;
        return this;
    }

    /**
     * Sets the height of this text box.
     *
     * @param height the specified height.
     */
    public TextBox SetHeight(float height) {
        this.height = height;
        return this;
    }

    /**
     * Returns the text box height.
     *
     * @return the text box height.
     */
    public float GetHeight() {
        return height;
    }

    /**
     * Sets the margin of this text box.
     *
     * @param margin the margin between the text and the box
     */
    public TextBox SetMargin(double margin) {
        this.margin = (float) margin;
        return this;
    }

    /**
     * Sets the margin of this text box.
     *
     * @param margin the margin between the text and the box
     */
    public TextBox SetMargin(float margin) {
        this.margin = margin;
        return this;
    }

    /**
     * Returns the text box margin.
     *
     * @return the margin between the text and the box
     */
    public float GetMargin() {
        return margin;
    }

    /**
     * Sets the border line width.
     *
     * @param lineWidth float
     */
    public void SetLineWidth(double lineWidth) {
        this.lineWidth = (float) lineWidth;
    }

    /**
     * Sets the border line width.
     *
     * @param lineWidth float
     */
    public void SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
    }

    /**
     * Returns the border line width.
     *
     * @return float the line width.
     */
    public float GetLineWidth() {
        return lineWidth;
    }

    /**
     * Sets the spacing between lines of text.
     *
     * @param spacing the spacing
     */
    public void SetSpacing(double spacing) {
        this.spacing = (float) spacing;
    }

    /**
     * Sets the spacing between lines of text.
     *
     * @param spacing the spacing
     */
    public void SetSpacing(float spacing) {
        this.spacing = spacing;
    }

    /**
     * Returns the spacing between lines of text.
     *
     * @return the spacing.
     */
    public float GetSpacing() {
        return spacing;
    }

    public void SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void SetBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
    }

    public void SetBackgroundColor(float[] rgbColor) {
        this.fillColor = rgbColor;
    }

    public void SetTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
    }

    public void SetTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
    }

    public void SetTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
    }

    public float[] GetTextColor() {
        return textColor;
    }

    public void SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
    }

    public void SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
    }

    public void SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
    }

    public void SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
    }

    public float[] GetStrokeColor() {
        return strokeColor;
    }

    /**
     * Sets the TextBox border properties.
     *
     * @param border the border properties.
     */
    public void SetBorder(uint border) {
        this.properties |= border;
    }

    /**
     * Returns the text box specific border value.
     *
     * @param border the border property.
     * @return boolean the specific border value.
     */
    public bool GetBorder(uint border) {
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
    public void SetBorders(bool borders) {
        if (borders) {
            SetBorder(Border.ALL);
        } else {
            SetBorder(Border.NONE);
        }
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public TextBox SetTextAlignment(uint alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
        return this;
    }

    /**
     * Returns the text alignment.
     *
     * @return alignment the alignment code. Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public uint GetTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /**
     * Sets the underline variable.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline flag.
     */
    public void SetUnderline(bool underline) {
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
    public bool GetUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /**
     * Sets the strikeout flag.
     * In the flag is true - draw strikeout line through the text.
     *
     * @param strikeout the strikeout flag.
     */
    public void SetStrikeout(bool strikeout) {
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
    public bool GetStrikeout() {
        return (properties & 0x00800000) != 0;
    }

    public void SetFallbackFont(Font font) {
        this.fallbackFont = font;
    }

    public Font GetFallbackFont() {
        return this.fallbackFont;
    }

    /**
     * Sets the vertical alignment of the text in this TextBox.
     *
     * @param valign - valid values are Align.TOP, Align.BOTTOM and Align.CENTER
     */
    public void SetVerticalAlignment(uint valign) {
        this.valign = valign;
    }

    public uint GetVerticalAlignment() {
        return this.valign;
    }

    public void SetTextColors(Dictionary<String, Int32> colors) {
        this.colors = colors;
    }

    public Dictionary<String, Int32> GetTextColors() {
        return this.colors;
    }

    private void DrawBorders(Page page) {
        if (page == null) {
            return;
        }
        page.AddArtifactBMC();
        page.SetPenColor(strokeColor);
        page.SetPenWidth(strokeWidth);
        if (GetBorder(Border.ALL)) {
            page.DrawRect(x, y, width, height);
        } else {
            if (GetBorder(Border.TOP)) {
                page.MoveTo(x, y);
                page.LineTo(x + width, y);
                page.StrokePath();
            }
            if (GetBorder(Border.BOTTOM)) {
                page.MoveTo(x, y + height);
                page.LineTo(x + width, y + height);
                page.StrokePath();
            }
            if (GetBorder(Border.LEFT)) {
                page.MoveTo(x, y);
                page.LineTo(x, y + height);
                page.StrokePath();
            }
            if (GetBorder(Border.RIGHT)) {
                page.MoveTo(x + width, y);
                page.LineTo(x + width, y + height);
                page.StrokePath();
            }
        }
        page.AddEMC();
    }

    private bool textIsCJK(String str) {
        // CJK Unified Ideographs Range: 4E00–9FD5
        // Hiragana Range: 3040–309F
        // Katakana Range: 30A0–30FF
        // Hangul Jamo Range: 1100–11FF
        int numOfCJK = 0;
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if ((ch >= 0x4E00 && ch <= 0x9FD5) ||
                    (ch >= 0x3040 && ch <= 0x309F) ||
                    (ch >= 0x30A0 && ch <= 0x30FF) ||
                    (ch >= 0x1100 && ch <= 0x11FF)) {
                numOfCJK += 1;
            }
        }
        return (numOfCJK > (str.Length / 2));
    }

    private String[] getTextLines() {
        List<String> list = new List<String>();

        float textAreaWidth;
        if (textDirection == Direction.LEFT_TO_RIGHT) {
            textAreaWidth = width - 2*margin;
        } else {
            textAreaWidth = height - 2*margin;
        }
        String[] lines = text.Split(new String[] {"\r\n", "\n"}, StringSplitOptions.None);
        foreach (String line in lines) {
            if (font.StringWidth(fallbackFont, line) <= textAreaWidth) {
                list.Add(line);
            } else {
                if (textIsCJK(line)) {
                    StringBuilder sb = new StringBuilder();
                    foreach (char ch in line.ToCharArray()) {
                        if (font.StringWidth(fallbackFont, sb.ToString() + ch) <= textAreaWidth) {
                            sb.Append(ch);
                        } else {
                            list.Add(sb.ToString());
                            sb.Length = 0;
                            sb.Append(ch);
                        }
                    }
                    if (sb.Length > 0) {
                        list.Add(sb.ToString());
                    }
                } else {
                    StringBuilder sb = new StringBuilder();
                    String[] tokens = System.Text.RegularExpressions.Regex.Split(line, @"\s+");
                    foreach (String token in tokens) {
                        if (font.StringWidth(fallbackFont, sb.ToString() + token) <= textAreaWidth) {
                            sb.Append(token + " ");
                        } else {
                            list.Add(sb.ToString().Trim());
                            sb.Length = 0;
                            sb.Append(token + " ");
                        }
                    }
                    if (sb.ToString().Trim().Length > 0) {
                        list.Add(sb.ToString().Trim());
                    }
                }
            }
        }

        return list.ToArray();
    }

    /**
     * Draws this text box on the specified page.
     *
     * @param page the Page where the TextBox is to be drawn.
     * @param draw flag specifying if this component should actually be drawn on the page.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    public float[] DrawOn(Page page) {
        String[] lines = getTextLines();
        float leading = font.GetAscent(fontSize) + font.GetDescent(fontSize) + spacing;
        if (height > 0f) {  // TextBox with fixed height
            if ((lines.Length*leading - spacing) > (height - 2*margin)) {
                List<String> list = new List<String>();
                for (int i = 0; i < lines.Length; i++) {
                    String line = lines[i];
                    if (((i + 1)*leading - spacing) > (height - 2*margin)) {
                        break;
                    }
                    list.Add(line);
                }
                if (list.Count > 0) {
                    String lastLine = list[list.Count - 1];
                    if (lastLine.Length > 3) {
                        lastLine = lastLine.Substring(0, lastLine.Length - 3);
                    }
                    list[list.Count - 1] = lastLine + "...";
                    lines = list.ToArray();
                }
            }
            if (page != null) {
                if (fillColor != null) {
                    page.SetBrushColor(fillColor);
                    page.FillRect(x, y, width, height);
                }
                page.SetPenColor(this.strokeColor);
                page.SetBrushColor(this.textColor);
                page.SetPenWidth(this.font.GetUnderlineThickness(fontSize));
            }
            float xText = x + margin;
            float yText = y + margin + font.GetAscent(fontSize);
            if (textDirection == Direction.LEFT_TO_RIGHT) {
                if (valign == Align.TOP) {
                    yText = y + margin + font.GetAscent(fontSize);
                } else if (valign == Align.BOTTOM) {
                    yText = (y + height) - (((float) lines.Length)*leading + margin);
                    yText += font.GetAscent(fontSize);
                } else if (valign == Align.CENTER) {
                    yText = y + (height - ((float) lines.Length)*leading)/2;
                    yText += font.GetAscent(fontSize);
                }
            } else {
                yText = x + margin + font.GetAscent(fontSize);
            }
            foreach (String line in lines) {
                if (textDirection == Direction.LEFT_TO_RIGHT) {
                    if (GetTextAlignment() == Align.LEFT) {
                        xText = x + margin;
                    } else if (GetTextAlignment() == Align.RIGHT) {
                        xText = (x + width) - (font.StringWidth(fallbackFont, line) + margin);
                    } else if (GetTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.StringWidth(fallbackFont, line))/2;
                    }
                } else {
                    xText = y + margin;
                }
                if (page != null) {
                    DrawTextLine(page, font, fallbackFont, line, xText, yText, textColor, colors);
                }
                if (textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP) {
                    yText += leading;
                } else {
                    yText -= leading;
                }
            }
        } else {            // TextBox that expands to fit the content
            if (page != null) {
                if (fillColor != null) {
                    page.SetBrushColor(fillColor);
                    page.FillRect(x, y, width, (lines.Length * leading - spacing) + 2*margin);
                }
                page.SetBrushColor(this.textColor);
                page.SetPenColor(this.strokeColor);
                page.SetPenWidth(this.font.GetUnderlineThickness(fontSize));
            }
            float xText = x + margin;
            float yText = y + margin + font.GetAscent(fontSize);
            foreach (String line in lines) {
                if (textDirection == Direction.LEFT_TO_RIGHT) {
                    if (GetTextAlignment() == Align.LEFT) {
                        xText = x + margin;
                    } else if (GetTextAlignment() == Align.RIGHT) {
                        xText = (x + width) - (font.StringWidth(fallbackFont, line) + margin);
                    } else if (GetTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.StringWidth(fallbackFont, line))/2;
                    }
                } else {
                    xText = x + margin;
                }
                if (page != null) {
                    DrawTextLine(page, font, fallbackFont, line, xText, yText, textColor, colors);
                }
                if (textDirection == Direction.LEFT_TO_RIGHT ||
                        textDirection == Direction.BOTTOM_TO_TOP) {
                    yText += leading;
                } else {
                    yText -= leading;
                }
            }
            height = ((yText - y) - (font.GetAscent(fontSize) + spacing)) + margin;
        }
        if (page != null) {
            DrawBorders(page);
            if (textDirection == Direction.LEFT_TO_RIGHT && (uri != null || key != null)) {
                page.AddAnnotation(new Annotation(
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
            page.SetTextDirection(0);
        }
        return new float[] {x + width, y + height};
    }

    private void DrawTextLine(
            Page page,
            Font font,
            Font fallbackFont,
            String text,
            float xText,
            float yText,
            float[] color,
            Dictionary<String, Int32> colors) {
        if (altDescription != null) {
            page.AddBMC(StructElem.P, language, text, altDescription);
        }

        if (textDirection == Direction.LEFT_TO_RIGHT) {
            page.DrawString(font, fallbackFont, fontSize, text, xText, yText, color, colors);
        } else if (textDirection == Direction.BOTTOM_TO_TOP) {
            page.SetTextDirection(90);
            page.DrawString(font, fallbackFont, fontSize, text, yText, xText + height, color, colors);
        } else if (textDirection == Direction.TOP_TO_BOTTOM) {
            page.SetTextDirection(270);
            page.DrawString(font, fallbackFont, fontSize, text,
                    (yText + width) - (margin + 2*font.GetAscent(fontSize)), xText, color, colors);
        }

        if (altDescription != null) {
            page.AddEMC();
        }

        if (textDirection == Direction.LEFT_TO_RIGHT) {
            float lineLength = font.StringWidth(fallbackFont, text);
            if (GetUnderline()) {
                page.AddArtifactBMC();
                page.MoveTo(xText, yText + font.GetUnderlinePosition(fontSize));
                page.LineTo(xText + lineLength, yText + font.GetUnderlinePosition(fontSize));
                page.StrokePath();
                page.AddEMC();
            }
            if (GetStrikeout()) {
                page.AddArtifactBMC();
                page.MoveTo(xText, yText - (font.GetBodyHeight(fontSize)/4));
                page.LineTo(xText + lineLength, yText - (font.GetBodyHeight(fontSize)/4));
                page.StrokePath();
                page.AddEMC();
            }
        }
    }

    /**
     * Sets the URI for the "click text line" action.
     *
     * @param uri the URI
     * @return this TextBox.
     */
    public TextBox SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    public void SetTextDirection(Direction textDirection) {
        this.textDirection = textDirection;
    }
}   // End of TextBox.cs
}   // End of namespace PDFjet.NET
