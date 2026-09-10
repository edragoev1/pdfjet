/*
 * TextBox.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// A box containing line-wrapped text.
///
/// <para>Defaults:<br/>
/// x = 0f<br/>
/// y = 0f<br/>
/// width = 300f<br/>
/// height = 0f<br/>
/// alignment = Align.LEFT<br/>
/// valign = Align.TOP<br/>
/// spacing = 0f<br/>
/// margin = 0f<br/>
/// </para>
///
/// This class was originally developed by Ronald Bourret.
/// It was completely rewritten in 2013 by Evgeni Dragoev.
/// </summary>
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
    private String altDescription = "";
    private String uri = null;
    private String key = null;
    private String uriLanguage = null;
    private String uriActualText = null;
    private String uriAltDescription = null;
    private Direction textDirection = Direction.LEFT_TO_RIGHT;

    /// <summary>
    /// Creates a text box and sets the font.
    /// </summary>
    /// <param name="font">the font.</param>
    public TextBox(Font font) {
        this.font = font;
        this.fontSize = font.size;
    }

    /// <summary>
    /// Creates a text box and sets the font.
    /// </summary>
    /// <param name="text">the text.</param>
    /// <param name="font">the font.</param>
    public TextBox(Font font, String text) {
        this.font = font;
        this.fontSize = font.size;
        this.text = text;
    }

    /// <summary>
    /// Creates a text box and sets the font and the text.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <param name="text">the text.</param>
    /// <param name="width">the width.</param>
    /// <param name="height">the height.</param>
    public TextBox(Font font, String text, double width, double height) :
        this(font, text, (float) width, (float) height) {
    }

    /// <summary>
    /// Creates a text box and sets the font and the text.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <param name="text">the text.</param>
    /// <param name="width">the width.</param>
    /// <param name="height">the height.</param>
    public TextBox(Font font, String text, float width, float height) {
        this.font = font;
        this.fontSize = font.size;
        this.text = text;
        this.width = width;
        this.height = height;
    }

    /// <summary>
    /// Sets the font for this text box.
    /// </summary>
    /// <param name="font">the font.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetFont(Font font) {
        this.font = font;
        return this;
    }

    /// <summary>
    /// Returns the font used by this text box.
    /// </summary>
    /// <returns>the font.</returns>
    public Font GetFont() {
        return font;
    }

    public TextBox SetFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /// <summary>
    /// Sets the text box text.
    /// </summary>
    /// <param name="text">the text box text.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetText(String text) {
        this.text = text;
        return this;
    }

    /// <summary>
    /// Returns the text box text.
    /// </summary>
    /// <returns>the text box text.</returns>
    public String GetText() {
        return text;
    }

    /// <summary>
    /// Sets the position where this text box will be drawn on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of the text box.</param>
    /// <param name="y">the y coordinate of the top left corner of the text box.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location where this text box will be drawn on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of the text box.</param>
    /// <param name="y">the y coordinate of the top left corner of the text box.</param>
    public TextBox SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>
    /// Sets the size of text box.
    /// </summary>
    /// <param name="w">the width of the text box.</param>
    /// <param name="h">the height of the text box.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetSize(float w, float h) {
        this.width = w;
        this.height = h;
        return this;
    }

    /// <summary>
    /// Gets the location where this text box will be drawn on the page.
    /// </summary>
    /// <returns>the float array of of x and y.</returns>
    public float[] GetLocation() {
        return new float[] { this.x, this.y };
    }

    /// <summary>
    /// Sets the width of this text box.
    /// </summary>
    /// <param name="width">the specified width.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetWidth(double width) {
        this.width = (float) width;
        return this;
    }

    /// <summary>
    /// Sets the width of this text box.
    /// </summary>
    /// <param name="width">the specified width.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetWidth(float width) {
        this.width = width;
        return this;
    }

    /// <summary>
    /// Returns the text box width.
    /// </summary>
    /// <returns>the text box width.</returns>
    public float GetWidth() {
        return width;
    }

    /// <summary>
    /// Sets the height of this text box.
    /// </summary>
    /// <param name="height">the specified height.</param>
    public TextBox SetHeight(double height) {
        this.height = (float) height;
        return this;
    }

    /// <summary>
    /// Sets the height of this text box.
    /// </summary>
    /// <param name="height">the specified height.</param>
    public TextBox SetHeight(float height) {
        this.height = height;
        return this;
    }

    /// <summary>
    /// Returns the text box height.
    /// </summary>
    /// <returns>the text box height.</returns>
    public float GetHeight() {
        return height;
    }

    /// <summary>
    /// Sets the margin of this text box.
    /// </summary>
    /// <param name="margin">the margin between the text and the box</param>
    public TextBox SetMargin(double margin) {
        this.margin = (float) margin;
        return this;
    }

    /// <summary>
    /// Sets the margin of this text box.
    /// </summary>
    /// <param name="margin">the margin between the text and the box</param>
    public TextBox SetMargin(float margin) {
        this.margin = margin;
        return this;
    }

    /// <summary>
    /// Returns the text box margin.
    /// </summary>
    /// <returns>the margin between the text and the box</returns>
    public float GetMargin() {
        return margin;
    }

    /// <summary>
    /// Sets the border line width.
    /// </summary>
    /// <param name="lineWidth">float</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetLineWidth(double lineWidth) {
        this.lineWidth = (float) lineWidth;
        return this;
    }

    /// <summary>
    /// Sets the border line width.
    /// </summary>
    /// <param name="lineWidth">float</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
        return this;
    }

    /// <summary>
    /// Returns the border line width.
    /// </summary>
    /// <returns>float the line width.</returns>
    public float GetLineWidth() {
        return lineWidth;
    }

    /// <summary>
    /// Sets the spacing between lines of text.
    /// </summary>
    /// <param name="spacing">the spacing</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetSpacing(double spacing) {
        this.spacing = (float) spacing;
        return this;
    }

    /// <summary>
    /// Sets the spacing between lines of text.
    /// </summary>
    /// <param name="spacing">the spacing</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetSpacing(float spacing) {
        this.spacing = spacing;
        return this;
    }

    /// <summary>
    /// Returns the spacing between lines of text.
    /// </summary>
    /// <returns>the spacing.</returns>
    public float GetSpacing() {
        return spacing;
    }

    public TextBox SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    public TextBox SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    public TextBox SetBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    public TextBox SetBackgroundColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    public TextBox SetTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
        return this;
    }

    public TextBox SetTextColor(float r, float g, float b) {
        this.textColor = new float[] {r, g, b};
        return this;
    }

    public TextBox SetTextColor(float[] rgbColor) {
        this.textColor = rgbColor;
        return this;
    }

    public float[] GetTextColor() {
        return textColor;
    }

    public TextBox SetLanguage(String language) {
        this.language = language;
        return this;
    }

    public String GetLanguage() {
        return this.language;
    }

    public TextBox SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    public String GetAltDescription() {
        return altDescription;
    }

    public TextBox SetStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    public TextBox SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public TextBox SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    public TextBox SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    public float[] GetStrokeColor() {
        return strokeColor;
    }

    /// <summary>
    /// Sets the TextBox border properties.
    /// </summary>
    /// <param name="border">the border properties.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetBorder(uint border) {
        this.properties |= border;
        return this;
    }

    /// <summary>
    /// Returns the text box specific border value.
    /// </summary>
    /// <param name="border">the border property.</param>
    /// <returns>boolean the specific border value.</returns>
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

    /// <summary>
    /// Sets the TextBox borders on and off.
    /// </summary>
    /// <param name="borders">the borders flag.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetBorders(bool borders) {
        if (borders) {
            SetBorder(Border.ALL);
        } else {
            SetBorder(Border.NONE);
        }
        return this;
    }

    /// <summary>
    /// Sets the cell text alignment.
    /// </summary>
    /// <param name="alignment">the alignment code.
    /// Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.</param>
    public TextBox SetTextAlignment(uint alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
        return this;
    }

    /// <summary>
    /// Returns the text alignment.
    /// </summary>
    /// <returns>alignment the alignment code. Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.</returns>
    public uint GetTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /// <summary>
    /// Sets the underline variable.
    /// If the value of the underline variable is 'true' - the text is underlined.
    /// </summary>
    /// <param name="underline">the underline flag.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetUnderline(bool underline) {
        if (underline) {
            this.properties |= 0x00400000;
        } else {
            this.properties &= 0x00BFFFFF;
        }
        return this;
    }

    /// <summary>
    /// Whether the text will be underlined.
    /// </summary>
    /// <returns>whether the text will be underlined</returns>
    public bool GetUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /// <summary>
    /// Sets the strikeout flag.
    /// In the flag is true - draw strikeout line through the text.
    /// </summary>
    /// <param name="strikeout">the strikeout flag.</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetStrikeout(bool strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        } else {
            this.properties &= 0x007FFFFF;
        }
        return this;
    }

    /// <summary>
    /// Returns the strikeout flag.
    /// </summary>
    /// <returns>boolean the strikeout flag.</returns>
    public bool GetStrikeout() {
        return (properties & 0x00800000) != 0;
    }

    public TextBox SetFallbackFont(Font font) {
        this.fallbackFont = font;
        return this;
    }

    public Font GetFallbackFont() {
        return this.fallbackFont;
    }

    /// <summary>
    /// Sets the vertical alignment of the text in this TextBox.
    /// </summary>
    /// <param name="valign">- valid values are Align.TOP, Align.BOTTOM and Align.CENTER</param>
    /// <returns>this TextBox object.</returns>
    public TextBox SetVerticalAlignment(uint valign) {
        this.valign = valign;
        return this;
    }

    public uint GetVerticalAlignment() {
        return this.valign;
    }

    public TextBox SetTextColors(Dictionary<String, Int32> colors) {
        this.colors = colors;
        return this;
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
        foreach (char ch in str) {
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
        // Font.StringWidth(fallbackFont, fontSize, str) is an exact per-character sum
        // whenever the primary font is not a core font - only the core fonts
        // apply kerning between adjacent characters, so their width is not
        // simply the sum of their parts. For the common case (an embedded
        // TrueType/CJK font) this lets the loops below track the wrapped
        // line's width incrementally - adding one token/character's width at
        // a time - instead of re-measuring the whole accumulated line on
        // every token, which made wrapping a long paragraph an O(n^2)
        // operation. Core fonts keep the original exact re-measurement so
        // kerning is still accounted for correctly.
        bool additive = !font.isCoreFont;
        String[] lines = text.Split(new String[] {"\r\n", "\n"}, StringSplitOptions.None);
        foreach (String line in lines) {
            if (font.StringWidth(fallbackFont, fontSize, line) <= textAreaWidth) {
                list.Add(line);
            } else {
                if (textIsCJK(line)) {
                    StringBuilder sb = new StringBuilder();
                    float sbWidth = 0f;
                    foreach (char ch in line.ToCharArray()) {
                        float chWidth = additive ? font.StringWidth(fallbackFont, fontSize, ch.ToString()) : 0f;
                        float width = additive ? sbWidth + chWidth : font.StringWidth(fallbackFont, fontSize, sb.ToString() + ch);
                        if (width <= textAreaWidth) {
                            sb.Append(ch);
                            sbWidth = width;
                        } else {
                            if (sb.Length > 0) {  // Don't emit an empty line
                                list.Add(sb.ToString());
                            }
                            sb.Length = 0;
                            sb.Append(ch);
                            sbWidth = chWidth;
                        }
                    }
                    if (sb.Length > 0) {
                        list.Add(sb.ToString());
                    }
                } else {
                    StringBuilder sb = new StringBuilder();
                    float sbWidth = 0f;
                    float spaceWidth = additive ? font.StringWidth(fallbackFont, fontSize, " ") : 0f;
                    String[] tokens = System.Text.RegularExpressions.Regex.Split(line, @"\s+");
                    foreach (String token in tokens) {
                        float tokenWidth = additive ? font.StringWidth(fallbackFont, fontSize, token) : 0f;
                        float width;
                        if (additive) {
                            width = sb.Length == 0 ? tokenWidth : sbWidth + spaceWidth + tokenWidth;
                        } else {
                            width = font.StringWidth(fallbackFont, fontSize, sb.ToString() + token);
                        }
                        if (width <= textAreaWidth) {
                            sb.Append(token + " ");
                            sbWidth = width;
                        } else {
                            if (sb.Length > 0) {  // Don't emit an empty line
                                list.Add(sb.ToString().Trim());
                            }
                            sb.Length = 0;
                            sb.Append(token + " ");
                            sbWidth = tokenWidth;
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

    /// <summary>
    /// Draws this text box on the specified page.
    /// </summary>
    /// <param name="page">the Page where the TextBox is to be drawn.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    /// <exception cref="System.Exception"/>
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
                    page.AddArtifactBMC();
                    page.FillRect(x, y, width, height);
                    page.AddEMC();
                }
                page.SetPenColor(this.strokeColor);
                page.SetBrushColor(this.fillColor);
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
                        xText = (x + width) - (font.StringWidth(fallbackFont, fontSize, line) + margin);
                    } else if (GetTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.StringWidth(fallbackFont, fontSize, line))/2;
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
                    page.AddArtifactBMC();
                    page.FillRect(x, y, width, (lines.Length * leading - spacing) + 2*margin);
                    page.AddEMC();
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
                        xText = (x + width) - (font.StringWidth(fallbackFont, fontSize, line) + margin);
                    } else if (GetTextAlignment() == Align.CENTER) {
                        xText = x + (width - font.StringWidth(fallbackFont, fontSize, line))/2;
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
            float lineLength = font.StringWidth(fallbackFont, fontSize, text);
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

    /// <summary>
    /// Sets the URI for the "click text line" action.
    /// </summary>
    /// <param name="uri">the URI</param>
    /// <returns>this TextBox.</returns>
    public TextBox SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    public TextBox SetTextDirection(Direction textDirection) {
        this.textDirection = textDirection;
        return this;
    }
}   // End of TextBox.cs
}   // End of namespace PDFjet.NET
