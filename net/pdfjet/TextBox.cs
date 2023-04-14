/**
 *  TextBox.cs
 *
Copyright 2023 Innovatics Inc.

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
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 *  A box containing line-wrapped text.
 *
 *  <p>Defaults:<br />
 *  x = 0f<br />
 *  y = 0f<br />
 *  width = 300f<br />
 *  height = 0f<br />
 *  alignment = Align.LEFT<br />
 *  valign = Align.TOP<br />
 *  spacing = 0f<br />
 *  margin = 0f<br />
 *  </p>
 *
 *  This class was originally developed by Ronald Bourret.
 *  It was completely rewritten in 2013 by Eugene Dragoev.
 */
public class TextBox : IDrawable {

    private Font font;
    private Font fallbackFont;
    private String text;

    private float x;
    private float y;

    private float width = 300f;
    private float height = 0f;
    private float spacing = 0f;
    private float margin = 0f;
    private float lineWidth;

    private int background = Color.transparent;
    private int pen = Color.black;
    private int brush = Color.black;
    private int valign = Align.TOP;
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
    private int properties = 0x000F0001;

    /**
     *  Creates a text box and sets the font.
     *
     *  @param font the font.
     */
    public TextBox(Font font) {
        this.font = font;
    }

    /**
     *  Creates a text box and sets the font.
     *
     *  @param text the text.
     *  @param font the font.
     */
    public TextBox(Font font, String text) {
        this.font = font;
        this.text = text;
    }

    /**
     *  Creates a text box and sets the font and the text.
     *
     *  @param font the font.
     *  @param text the text.
     *  @param width the width.
     *  @param height the height.
     */
    public TextBox(Font font, String text, double width, double height) :
        this(font, text, (float) width, (float) height) {
    }

    /**
     *  Creates a text box and sets the font and the text.
     *
     *  @param font the font.
     *  @param text the text.
     *  @param width the width.
     *  @param height the height.
     */
    public TextBox(Font font, String text, float width, float height) {
        this.font = font;
        this.text = text;
        this.width = width;
        this.height = height;
    }

    /**
     *  Sets the font for this text box.
     *
     *  @param font the font.
     */
    public void SetFont(Font font) {
        this.font = font;
    }

    /**
     *  Returns the font used by this text box.
     *
     *  @return the font.
     */
    public Font GetFont() {
        return font;
    }

    /**
     *  Sets the text box text.
     *
     *  @param text the text box text.
     */
    public void SetText(String text) {
        this.text = text;
    }

    /**
     *  Returns the text box text.
     *
     *  @return the text box text.
     */
    public String GetText() {
        return text;
    }

    /**
     *  Sets the position where this text box will be drawn on the page.
     *
     *  @param x the x coordinate of the top left corner of the text box.
     *  @param y the y coordinate of the top left corner of the text box.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     *  Sets the position where this text box will be drawn on the page.
     *
     *  @param x the x coordinate of the top left corner of the text box.
     *  @param y the y coordinate of the top left corner of the text box.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }

    /**
     *  Sets the location where this text box will be drawn on the page.
     *
     *  @param x the x coordinate of the top left corner of the text box.
     *  @param y the y coordinate of the top left corner of the text box.
     */
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /**
     * Gets the location where this text box will be drawn on the page.
     *
     * @return the float array of of x and y.
     */
    public float[] GetLocation() {
        return new float[] {this.x, this.y };
    }

    /**
     *  Sets the width of this text box.
     *
     *  @param width the specified width.
     */
    public void SetWidth(double width) {
        this.width = (float) width;
    }

    /**
     *  Sets the width of this text box.
     *
     *  @param width the specified width.
     */
    public void SetWidth(float width) {
        this.width = width;
    }

    /**
     *  Returns the text box width.
     *
     *  @return the text box width.
     */
    public float GetWidth() {
        return width;
    }

    /**
     *  Sets the height of this text box.
     *
     *  @param height the specified height.
     */
    public void SetHeight(double height) {
        this.height = (float) height;
    }

    /**
     *  Sets the height of this text box.
     *
     *  @param height the specified height.
     */
    public void SetHeight(float height) {
        this.height = height;
    }

    /**
     *  Returns the text box height.
     *
     *  @return the text box height.
     */
    public float GetHeight() {
        return height;
    }

    /**
     *  Sets the margin of this text box.
     *
     *  @param margin the margin between the text and the box
     */
    public void SetMargin(double margin) {
        this.margin = (float) margin;
    }

    /**
     *  Sets the margin of this text box.
     *
     *  @param margin the margin between the text and the box
     */
    public void SetMargin(float margin) {
        this.margin = margin;
    }

    /**
     *  Returns the text box margin.
     *
     *  @return the margin between the text and the box
     */
    public float GetMargin() {
        return margin;
    }

    /**
     *  Sets the border line width.
     *
     *  @param lineWidth float
     */
    public void SetLineWidth(double lineWidth) {
        this.lineWidth = (float) lineWidth;
    }

    /**
     *  Sets the border line width.
     *
     *  @param lineWidth float
     */
    public void SetLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
    }

    /**
     *  Returns the border line width.
     *
     *  @return float the line width.
     */
    public float GetLineWidth() {
        return lineWidth;
    }

    /**
     *  Sets the spacing between lines of text.
     *
     *  @param spacing the spacing
     */
    public void SetSpacing(double spacing) {
        this.spacing = (float) spacing;
    }

    /**
     *  Sets the spacing between lines of text.
     *
     *  @param spacing the spacing
     */
    public void SetSpacing(float spacing) {
        this.spacing = spacing;
    }

    /**
     *  Returns the spacing between lines of text.
     *
     *  @return the spacing.
     */
    public float GetSpacing() {
        return spacing;
    }

    /**
     *  Sets the background to the specified color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void SetBgColor(int color) {
        this.background = color;
    }

    /**
     *  Sets the background to the specified color.
     *
     *  @param color the color specified as array of integer values from 0x00 to 0xFF.
     */
    public void SetBgColor(int[] color) {
        this.background = color[0] << 16 | color[1] << 8 | color[2];
    }

    /**
     *  Sets the background to the specified color.
     *
     *  @param color the color specified as array of double values from 0.0 to 1.0.
     */
    public void SetBgColor(double[] color) {
        SetBgColor(new int[] { (int) color[0], (int) color[1], (int) color[2] });
    }

    /**
     *  Returns the background color.
     *
     * @return int the color as 0xRRGGBB integer.
     */
    public int GetBgColor() {
        return this.background;
    }

    /**
     *  Sets the pen and brush colors to the specified color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void SetFgColor(int color) {
        this.pen = color;
        this.brush = color;
    }

    /**
     *  Sets the pen and brush colors to the specified color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void SetFgColor(int[] color) {
        this.pen = color[0] << 16 | color[1] << 8 | color[2];
        this.brush = pen;
    }

    /**
     *  Sets the foreground pen and brush colors to the specified color.
     *
     *  @param color the color specified as an array of double values from 0.0 to 1.0.
     */
    public void SetFgColor(double[] color) {
        SetPenColor(new int[] { (int) color[0], (int) color[1], (int) color[2] });
        SetBrushColor(pen);
    }

    /**
     *  Sets the pen color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void SetPenColor(int color) {
        this.pen = color;
    }

    /**
     *  Sets the pen color.
     *
     *  @param color the color specified as an array of int values from 0x00 to 0xFF.
     */
    public void SetPenColor(int[] color) {
        this.pen = color[0] << 16 | color[1] << 8 | color[2];
    }

    /**
     *  Sets the pen color.
     *
     *  @param color the color specified as an array of double values from 0.0 to 1.0.
     */
    public void SetPenColor(double[] color) {
        SetPenColor(new int[] { (int) color[0], (int) color[1], (int) color[2] });
    }

    /**
     *  Returns the pen color as 0xRRGGBB integer.
     *
     * @return int the pen color.
     */
    public int GetPenColor() {
        return this.pen;
    }

    /**
     *  Sets the brush color.
     *
     *  @param color the color specified as 0xRRGGBB integer.
     */
    public void SetBrushColor(int color) {
        this.brush = color;
    }

    /**
     *  Sets the brush color.
     *
     *  @param color the color specified as an array of int values from 0x00 to 0xFF.
     */
    public void SetBrushColor(int[] color) {
        this.brush = color[0] << 16 | color[1] << 8 | color[2];
    }

    /**
     *  Sets the brush color.
     *
     *  @param color the color specified as an array of double values from 0.0 to 1.0.
     */
    public void SetBrushColor(double[] color) {
        SetBrushColor(new int [] { (int) color[0], (int) color[1], (int) color[2] });
    }

    /**
     * Returns the brush color.
     *
     * @return int the brush color specified as 0xRRGGBB integer.
     */
    public int GetBrushColor() {
        return this.brush;
    }

    /**
     *  Sets the TextBox border object.
     *
     *  @param border the border object.
     */
    public void SetBorder(int border, bool visible) {
        if (visible) {
            this.properties |= border;
        }
        else {
            this.properties &= (~border & 0x00FFFFFF);
        }
    }

    /**
     *  Returns the text box border.
     *
     *  @return boolean the text border object.
     */
    public bool GetBorder(int border) {
        return (this.properties & border) != 0;
    }

    /**
     *  Sets all borders to be invisible.
     *  This cell will have no borders when drawn on the page.
     */
    public void SetNoBorders() {
        this.properties &= 0x00F0FFFF;
    }

    /**
     *  Sets the cell text alignment.
     *
     *  @param alignment the alignment code.
     *  Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public void SetTextAlignment(int alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
    }

    /**
     *  Returns the text alignment.
     *
     *  @return alignment the alignment code. Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
     */
    public int GetTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /**
     *  Sets the underline variable.
     *  If the value of the underline variable is 'true' - the text is underlined.
     *
     *  @param underline the underline flag.
     */
    public void SetUnderline(bool underline) {
        if (underline) {
            this.properties |= 0x00400000;
        }
        else {
            this.properties &= 0x00BFFFFF;
        }
    }

    /**
     *  Whether the text will be underlined.
     *
     *  @return whether the text will be underlined
     */
    public bool GetUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /**
     *  Sets the srikeout flag.
     *  In the flag is true - draw strikeout line through the text.
     *
     *  @param strikeout the strikeout flag.
     */
    public void SetStrikeout(bool strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        }
        else {
            this.properties &= 0x007FFFFF;
        }
    }

    /**
     *  Returns the strikeout flag.
     *
     *  @return boolean the strikeout flag.
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
     *  Sets the vertical alignment of the text in this TextBox.
     *
     *  @param alignment - valid values are Align.TOP, Align.BOTTOM and Align.CENTER
     */
    public void SetVerticalAlignment(int alignment) {
        this.valign = alignment;
    }

    public int GetVerticalAlignment() {
        return this.valign;
    }

    public void SetTextColors(Dictionary<String, Int32> colors) {
        this.colors = colors;
    }

    public Dictionary<String, Int32> GetTextColors() {
        return this.colors;
    }

    private void DrawBorders(Page page) {
        page.SetPenColor(pen);
        page.SetPenWidth(lineWidth);

        if (GetBorder(Border.TOP) &&
                GetBorder(Border.BOTTOM) &&
                GetBorder(Border.LEFT) &&
                GetBorder(Border.RIGHT)) {
            page.DrawRect(x, y, width, height);
        }
        else {
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
    }

    // Preserves the leading spaces and tabs
    private StringBuilder GetStringBuilder(String line) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < line.Length; i++) {
            char ch = line[i];
            if (ch == ' ') {
                buf.Append(ch);
            } else if (ch == '\t') {
                buf.Append("    ");
            } else {
                break;
            }
        }
        return buf;
    }

    private String[] getTextLines() {
        List<String> list = new List<String>();
        float textAreaWidth = width - 2*margin;
        String[] lines = text.Split(new String[] {"\r\n", "\n"}, StringSplitOptions.None);
        foreach (String line in lines) {
            if (font.StringWidth(fallbackFont, line) <= textAreaWidth) {
                list.Add(line);
            } else {
                StringBuilder buf1 = GetStringBuilder(line);    // Preserves the indentation
                String[] tokens = line.Trim().Split(' ');       // Do not remove the Trim()!
                foreach (String token in tokens) {
                    if (font.StringWidth(fallbackFont, token) > textAreaWidth) {
                        // We have very long token, so we have to split it
                        StringBuilder buf2 = new StringBuilder();
                        for (int i = 0; i < token.Length; i++) {
                            char ch = token[i];
                            if (font.StringWidth(fallbackFont, buf2.ToString() + ch) > textAreaWidth) {
                                list.Add(buf2.ToString());
                                buf2.Length = 0;
                            }
                            buf2.Append(ch);
                        }
                        if (buf2.Length > 0) {
                            buf1.Append(buf2.ToString() + " ");
                        }
                    } else {
                        if (font.StringWidth(fallbackFont, buf1.ToString() + token) > textAreaWidth) {
                            list.Add(buf1.ToString());
                            buf1.Length = 0;
                        }
                        buf1.Append(token + " ");
                    }
                }
                if (buf1.Length > 0) {
                    String str = buf1.ToString().Trim();
                    if (font.StringWidth(fallbackFont, str) <= textAreaWidth) {
                        list.Add(str);
                    } else {
                        // We have very long token, so we have to split it
                        StringBuilder buf2 = new StringBuilder();
                        for (int i = 0; i < str.Length; i++) {
                            char ch = str[i];
                            if (font.StringWidth(fallbackFont, buf2.ToString() + ch) > textAreaWidth) {
                                list.Add(buf2.ToString());
                                buf2.Length = 0;
                            }
                            buf2.Append(ch);
                        }
                        if (buf2.Length > 0) {
                            list.Add(buf2.ToString());
                        }
                    }
                }
            }
        }
        int index = list.Count - 1;
        if (list.Count > 0 && list[index].Trim().Length == 0) {
            list.RemoveAt(index); // Remove the last line if it is blank
        }
        return list.ToArray();
    }

    /**
     *  Draws this text box on the specified page.
     *
     *  @param page the Page where the TextBox is to be drawn.
     *  @param draw flag specifying if this component should actually be drawn on the page.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        String[] lines = getTextLines();
        float leading = font.ascent + font.descent + spacing;

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
                    lastLine = lastLine.Substring(0, lastLine.Length - 3);
                    list[list.Count - 1] = lastLine + "...";
                    lines = list.ToArray();
                }
            }
            if (page != null) {
                if (GetBgColor() != Color.transparent) {
                    page.SetBrushColor(background);
                    page.FillRect(x, y, width, height);
                }
                page.SetPenColor(this.pen);
                page.SetBrushColor(this.brush);
                page.SetPenWidth(this.font.underlineThickness);
            }
            float xText = x + margin;
            float yText = y + margin + font.ascent;
            if (valign == Align.TOP) {
                yText = y + margin + font.ascent;
            } else if (valign == Align.BOTTOM) {
                yText = (y + height) - (((float) lines.Length)*leading + margin);
                yText += font.ascent;
            } else if (valign == Align.CENTER) {
                yText = y + (height - ((float) lines.Length)*leading)/2;
                yText += font.ascent;
            }
            foreach (String line in lines) {
                if (GetTextAlignment() == Align.LEFT) {
                    xText = x + margin;
                } else if (GetTextAlignment() == Align.RIGHT) {
                    xText = (x + width) - (font.StringWidth(fallbackFont, line) + margin);
                } else if (GetTextAlignment() == Align.CENTER) {
                    xText = x + (width - font.StringWidth(fallbackFont, line))/2;
                }
                if (yText + font.descent <= y + height) {
                    if (page != null) {
                        DrawText(page, font, fallbackFont, line, xText, yText, colors);
                    }
                    yText += leading;
                }
            }
        } else {            // TextBox that expands to fit the content
            if (page != null) {
                if (GetBgColor() != Color.transparent) {
                    page.SetBrushColor(background);
                    page.FillRect(x, y, width, (lines.Length * leading - spacing) + 2*margin);
                }
                page.SetPenColor(this.pen);
                page.SetBrushColor(this.brush);
                page.SetPenWidth(this.font.underlineThickness);
            }
            float xText = x + margin;
            float yText = y + margin + font.ascent;
            foreach (String line in lines) {
                if (GetTextAlignment() == Align.LEFT) {
                    xText = x + margin;
                } else if (GetTextAlignment() == Align.RIGHT) {
                    xText = (x + width) - (font.StringWidth(fallbackFont, line) + margin);
                } else if (GetTextAlignment() == Align.CENTER) {
                    xText = x + (width - font.StringWidth(fallbackFont, line))/2;
                }
                if (page != null) {
                    DrawText(page, font, fallbackFont, line, xText, yText, colors);
                }
                yText += leading;
            }
            height = ((yText - y) - (font.ascent + spacing)) + margin;
        }
        if (page != null) {
            DrawBorders(page);
        }

        return new float[] {x + width, y + height};
    }

    private void DrawText(
            Page page,
            Font font,
            Font fallbackFont,
            String text,
            float xText,
            float yText,
            Dictionary<String, Int32> colors) {
        page.DrawString(font, fallbackFont, text, xText, yText, colors);
        float lineLength = font.StringWidth(text);
        if (GetUnderline()) {
            float yAdjust = font.underlinePosition;
            page.MoveTo(xText, yText + yAdjust);
            page.LineTo(xText + lineLength, yText + yAdjust);
            page.StrokePath();
        }
        if (GetStrikeout()) {
            float yAdjust = font.bodyHeight/4;
            page.MoveTo(xText, yText - yAdjust);
            page.LineTo(xText + lineLength, yText - yAdjust);
            page.StrokePath();
        }
    }
}   // End of TextBox.cs
}   // End of namespace PDFjet.NET
