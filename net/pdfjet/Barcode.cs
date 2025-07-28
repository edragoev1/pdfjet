/**
 *  Barcode.cs
 *
©2025 PDFjet Software

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
 *  Used to create one dimentional barcodes - UPC, Code 39 and Code 128.
 *
 *  Please see Example_11.
 */
public class Barcode : IDrawable {
    public static readonly int UPC = 0;
    public static readonly int CODE128 = 1;
    public static readonly int CODE39 = 2;

    public static readonly int LEFT_TO_RIGHT = 0;
    public static readonly int TOP_TO_BOTTOM = 1;
    public static readonly int BOTTOM_TO_TOP = 2;

    private int barcodeType = 0;
    private String text = null;
    private float x1 = 0.0f;
    private float y1 = 0.0f;
    private float m1 = 0.75f;   // Module length
    private float barHeightFactor = 50.0f;
    private int direction = LEFT_TO_RIGHT;
    private Font font = null;

    private int[] tableA = {3211,2221,2122,1411,1132,1231,1114,1312,1213,3112};
    private Dictionary<Char, String> tableB = new Dictionary<Char, String>();

    /**
     *  The constructor.
     *
     *  @param type the type of the barcode.
     *  @param text the content string of the barcode.
     */
    public Barcode(int barcodeType, String text) {
        this.barcodeType = barcodeType;
        this.text = text;

        tableB.Add( '*', "bWbwBwBwb" );
        tableB.Add( '-', "bWbwbwBwB" );
        tableB.Add( '$', "bWbWbWbwb" );
        tableB.Add( '%', "bwbWbWbWb" );
        tableB.Add( ' ', "bWBwbwBwb" );
        tableB.Add( '.', "BWbwbwBwb" );
        tableB.Add( '/', "bWbWbwbWb" );
        tableB.Add( '+', "bWbwbWbWb" );
        tableB.Add( '0', "bwbWBwBwb" );
        tableB.Add( '1', "BwbWbwbwB" );
        tableB.Add( '2', "bwBWbwbwB" );
        tableB.Add( '3', "BwBWbwbwb" );
        tableB.Add( '4', "bwbWBwbwB" );
        tableB.Add( '5', "BwbWBwbwb" );
        tableB.Add( '6', "bwBWBwbwb" );
        tableB.Add( '7', "bwbWbwBwB" );
        tableB.Add( '8', "BwbWbwBwb" );
        tableB.Add( '9', "bwBWbwBwb" );
        tableB.Add( 'A', "BwbwbWbwB" );
        tableB.Add( 'B', "bwBwbWbwB" );
        tableB.Add( 'C', "BwBwbWbwb" );
        tableB.Add( 'D', "bwbwBWbwB" );
        tableB.Add( 'E', "BwbwBWbwb" );
        tableB.Add( 'F', "bwBwBWbwb" );
        tableB.Add( 'G', "bwbwbWBwB" );
        tableB.Add( 'H', "BwbwbWBwb" );
        tableB.Add( 'I', "bwBwbWBwb" );
        tableB.Add( 'J', "bwbwBWBwb" );
        tableB.Add( 'K', "BwbwbwbWB" );
        tableB.Add( 'L', "bwBwbwbWB" );
        tableB.Add( 'M', "BwBwbwbWb" );
        tableB.Add( 'N', "bwbwBwbWB" );
        tableB.Add( 'O', "BwbwBwbWb" );
        tableB.Add( 'P', "bwBwBwbWb" );
        tableB.Add( 'Q', "bwbwbwBWB" );
        tableB.Add( 'R', "BwbwbwBWb" );
        tableB.Add( 'S', "bwBwbwBWb" );
        tableB.Add( 'T', "bwbwBwBWb" );
        tableB.Add( 'U', "BWbwbwbwB" );
        tableB.Add( 'V', "bWBwbwbwB" );
        tableB.Add( 'W', "BWBwbwbwb" );
        tableB.Add( 'X', "bWbwBwbwB" );
        tableB.Add( 'Y', "BWbwBwbwb" );
        tableB.Add( 'Z', "bWBwBwbwb" );
    }

    /**
     *  Sets the position where this barcode will be drawn on the page.
     *
     *  @param x1 the x coordinate of the top left corner of the barcode.
     *  @param y1 the y coordinate of the top left corner of the barcode.
     */
    public void SetPosition(double x1, double y1) {
        SetPosition((float) x1, (float) y1);
    }

    /**
     *  Sets the position where this barcode will be drawn on the page.
     *
     *  @param x1 the x coordinate of the top left corner of the barcode.
     *  @param y1 the y coordinate of the top left corner of the barcode.
     */
    public void SetPosition(float x1, float y1) {
        SetLocation(x1, y1);
    }

    /**
     *  Sets the location where this barcode will be drawn on the page.
     *
     *  @param x1 the x coordinate of the top left corner of the barcode.
     *  @param y1 the y coordinate of the top left corner of the barcode.
     */
    public Barcode SetLocation(double x1, double y1) {
        return SetLocation((float) x1, (float) y1);
    }

    /**
     *  Sets the location where this barcode will be drawn on the page.
     *
     *  @param x1 the x coordinate of the top left corner of the barcode.
     *  @param y1 the y coordinate of the top left corner of the barcode.
     */
    public Barcode SetLocation(float x1, float y1) {
        this.x1 = x1;
        this.y1 = y1;
        return (PDFjet.NET.Barcode) this;
    }

    /**
     *  Sets the module length of this barcode.
     *  The default value is 0.75
     *
     *  @param moduleLength the specified module length.
     */
    public void SetModuleLength(double moduleLength) {
        this.m1 = (float) moduleLength;
    }

    /**
     *  Sets the module length of this barcode.
     *  The default value is 0.75f
     *
     *  @param moduleLength the specified module length.
     */
    public void SetModuleLength(float moduleLength) {
        this.m1 = moduleLength;
    }

    /**
     *  Sets the bar height factor.
     *  The height of the bars is the moduleLength * barHeightFactor
     *  The default value is 50.0
     *
     *  @param barHeightFactor the specified bar height factor.
     */
    public void SetBarHeightFactor(double barHeightFactor) {
        this.barHeightFactor = (float) barHeightFactor;
    }

    /**
     *  Sets the bar height factor.
     *  The height of the bars is the moduleLength * barHeightFactor
     *  The default value is 50.0
     *
     *  @param barHeightFactor the specified bar height factor.
     */
    public void SetBarHeightFactor(float barHeightFactor) {
        this.barHeightFactor = barHeightFactor;
    }

    /**
     *  Sets the drawing direction for this font.
     *
     *  @param direction the specified direction.
     */
    public void SetDirection(int direction) {
        this.direction = direction;
    }

    /**
     *  Sets the font to be used with this barcode.
     *
     *  @param font the specified font.
     */
    public void SetFont(Font font) {
        this.font = font;
    }

    /**
     *  Draws this barcode on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (barcodeType == Barcode.UPC) {
            return DrawCodeUPC(page, x1, y1);
        } else if (barcodeType == Barcode.CODE128) {
            return DrawCode128(page, x1, y1);
        } else if (barcodeType == Barcode.CODE39) {
            return DrawCode39(page, x1, y1);
        } else {
            throw new Exception("Unsupported Barcode Type.");
        }
    }

    internal float[] DrawOnPageAtLocation(Page page, float x1, float y1) {
        if (barcodeType == Barcode.UPC) {
            return DrawCodeUPC(page, x1, y1);
        } else if (barcodeType == Barcode.CODE128) {
            return DrawCode128(page, x1, y1);
        } else if (barcodeType == Barcode.CODE39) {
            return DrawCode39(page, x1, y1);
        } else {
            throw new Exception("Unsupported Barcode Type.");
        }
    }

    private float[] DrawCodeUPC(Page page, float x1, float y1) {
        float x = x1;
        float y = y1;
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        // Calculate the check digit:
        // 1. Add the digits in the odd-numbered positions (first, third, fifth, etc.)
        // together and multiply by three.
        // 2. Add the digits in the even-numbered positions (second, fourth, sixth, etc.)
        // to the result.
        // 3. Subtract the result modulo 10 from ten.
        // 4. The answer modulo 10 is the check digit.
        int sum = 0;
        for (int i = 0; i < 11; i += 2) {
            sum += text[i] - 48;
        }
        sum *= 3;
        for (int i = 1; i < 11; i += 2) {
            sum += text[i] - 48;
        }
        int reminder = sum % 10;
        int checkDigit = (10 - reminder) % 10;
        text += checkDigit.ToString();

        x = DrawEGuard(page, x, y, m1, h + 8);
        for (int i = 0; i < 6; i++) {
            int digit = text[i] - 0x30;
            // page.DrawString(digit.ToString(), x + 1, y + h + 12);
            String symbol = tableA[digit].ToString();
            for (int j = 0; j < symbol.Length; j++) {
                int n = symbol[j] - 0x30;
                if (j%2 != 0) {
                    DrawVertBar(page, x, y, n*m1, h);
                }
                x += n*m1;
            }
        }
        x = DrawMGuard(page, x, y, m1, h + 8);
        for (int i = 6; i < 12; i++) {
            int digit = text[i] - 0x30;
            // page.DrawString(digit.ToString(), x + 1, y + h + 12);
            String symbol = tableA[digit].ToString();
            for (int j = 0; j < symbol.Length; j++) {
                int n = symbol[j] - 0x30;
                if (j%2 == 0) {
                    DrawVertBar(page, x, y, n*m1, h);
                }
                x += n*m1;
            }
        }
        x = DrawEGuard(page, x, y, m1, h + 8);

        float[] xy = new float[] {x, y};
        if (font != null) {
            String label =
                    text[0] +
                    "  " +
                    text[1] +
                    text[2] +
                    text[3] +
                    text[4] +
                    text[5] +
                    "   " +
                    text[6] +
                    text[7] +
                    text[8] +
                    text[9] +
                    text[10] +
                    "  " +
                    text[11];
            float fontSize = font.GetSize();
            font.SetSize(10f);

            TextLine textLine = new TextLine(font, label);
            textLine.SetLocation(
                    x1 + ((x - x1) - font.StringWidth(label))/2,
                    y1 + h + font.bodyHeight);
            xy = textLine.DrawOn(page);
            xy[0] = Math.Max(x, xy[0]);
            xy[1] = Math.Max(y, xy[1]);

            font.SetSize(fontSize);
            return new float[] {xy[0], xy[1] + font.descent};
        }

        return new float[] {xy[0], xy[1]};
    }

    private float DrawEGuard(
            Page page,
            float x,
            float y,
            float m1,
            float h) {
        if (page != null) {
            // 101
            DrawBar(page, x + (0.5f * m1), y, m1, h);
            DrawBar(page, x + (2.5f * m1), y, m1, h);
        }
        return (x + (3.0f * m1));
    }

    private float DrawMGuard(
            Page page,
            float x,
            float y,
            float m1,
            float h) {
        if (page != null) {
            // 01010
            DrawBar(page, x + (1.5f * m1), y, m1, h);
            DrawBar(page, x + (3.5f * m1), y, m1, h);
        }
        return (x + (5.0f * m1));
    }

    private void DrawBar(
            Page page,
            float x,
            float y,
            float m1,   // Single bar width
            float h) {
        if (page != null) {
            page.SetPenWidth(m1);
            page.MoveTo(x, y);
            page.LineTo(x, y + h);
            page.StrokePath();
        }
    }

    private float[] DrawCode128(Page page, float x1, float y1) {
        float x = x1;
        float y = y1;
        float w = m1;
        float h = m1;

        if (direction == TOP_TO_BOTTOM) {
            w *= barHeightFactor;
        } else if (direction == LEFT_TO_RIGHT) {
            h *= barHeightFactor;
        }

        List<Int32> list = new List<Int32>();
        for (int i = 0; i < text.Length; i++) {
            char symchar = text[i];
            if (symchar < 32) {
                list.Add(GS1_128.SHIFT);
                list.Add(symchar + 64);
            } else if (symchar < 128) {
                list.Add(symchar - 32);
            } else if (symchar < 256) {
                list.Add(GS1_128.FNC_4);
                list.Add(symchar - 160);    // 128 + 32
            } else {
                // list.Add(31);            // '?'
                list.Add(256);              // This will generate an exception.
            }
            if (list.Count == 48) {
                // Maximum number of data characters is 48
                break;
            }
        }

        StringBuilder buf = new StringBuilder();
        int checkDigit = GS1_128.START_B;
        buf.Append((char) checkDigit);
        for (int i = 0; i < list.Count; i++) {
            int codeword = list[i];
            buf.Append((char) codeword);
            checkDigit += codeword * (i + 1);
        }
        checkDigit %= GS1_128.START_A;
        buf.Append((char) checkDigit);
        buf.Append((char) GS1_128.STOP);

        for (int i = 0; i < buf.Length; i++) {
            int si = buf[i];
            String symbol = GS1_128.TABLE[si].ToString();
            for (int j = 0; j < symbol.Length; j++) {
                int n = symbol[j] - 0x30;
                if (j%2 == 0) {
                    if (direction == LEFT_TO_RIGHT) {
                        DrawVertBar(page, x, y, m1 * n, h);
                    } else if (direction == TOP_TO_BOTTOM) {
                        DrawHorzBar(page, x, y, m1 * n, w);
                    }
                }
                if (direction == LEFT_TO_RIGHT) {
                    x += n * m1;
                } else if (direction == TOP_TO_BOTTOM) {
                    y += n * m1;
                }
            }
        }

        float[] xy = new float[] {x, y};
        if (font != null) {
            if (direction == LEFT_TO_RIGHT) {
                TextLine textLine = new TextLine(font, text);
                textLine.SetLocation(
                        x1 + ((x - x1) - font.StringWidth(text))/2,
                        y1 + h + font.bodyHeight);
                xy = textLine.DrawOn(page);
                xy[0] = Math.Max(x, xy[0]);
                return new float[] {xy[0], xy[1] + font.descent};
            } else if (direction == TOP_TO_BOTTOM) {
                TextLine textLine = new TextLine(font, text);
                textLine.SetLocation(
                        x + w + font.bodyHeight,
                        y - ((y - y1) - font.StringWidth(text))/2);
                textLine.SetTextDirection(90);
                xy = textLine.DrawOn(page);
                xy[1] = Math.Max(y, xy[1]);
            }
        }

        return xy;
    }

    private float[] DrawCode39(Page page, float x1, float y1) {
        text = "*" + text + "*";

        float x = x1;
        float y = y1;
        float w = m1 * barHeightFactor; // Barcode width when drawn vertically
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        float[] xy = new float[] {0f, 0f};

        if (direction == LEFT_TO_RIGHT) {
            for (int i = 0; i < text.Length; i++) {
                String code = tableB[text[i]];
                if ( code == null ) {
                    throw new Exception("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.");
                }

                for (int j = 0; j < 9; j++) {
                    char ch = code[j];
                    if (ch == 'w') {
                        x += m1;
                    } else if (ch == 'W') {
                        x += m1 * 3;
                    } else if (ch == 'b') {
                        DrawVertBar(page, x, y, m1, h);
                        x += m1;
                    } else if (ch == 'B') {
                        DrawVertBar(page, x, y, m1 * 3, h);
                        x += m1 * 3;
                    }
                }

                x += m1;
            }

            if (font != null) {
                TextLine textLine = new TextLine(font, text);
                textLine.SetLocation(
                        x1 + ((x - x1) - font.StringWidth(text))/2,
                        y1 + h + font.bodyHeight);
                xy = textLine.DrawOn(page);
                xy[0] = Math.Max(x, xy[0]);
            }
        } else if (direction == TOP_TO_BOTTOM) {
            for (int i = 0; i < text.Length; i++) {
                String code = tableB[text[i]];
                if ( code == null ) {
                    throw new Exception("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.");
                }

                for (int j = 0; j < 9; j++) {
                    char ch = code[j];
                    if (ch == 'w') {
                        y += m1;
                    } else if (ch == 'W') {
                        y += 3 * m1;
                    } else if (ch == 'b') {
                        DrawHorzBar(page, x, y, m1, h);
                        y += m1;
                    } else if (ch == 'B') {
                        DrawHorzBar(page, x, y, 3 * m1, h);
                        y += 3 * m1;
                    }
                }
                y += m1;
            }

            if (font != null) {
                TextLine textLine = new TextLine(font, text);
                textLine.SetLocation(
                        x - font.bodyHeight,
                        y1 + ((y - y1) - font.StringWidth(text))/2);
                textLine.SetTextDirection(270);
                xy = textLine.DrawOn(page);
                xy[0] = Math.Max(x, xy[0]) + w;
                xy[1] = Math.Max(y, xy[1]);
            }

        } else if (direction == BOTTOM_TO_TOP) {
            float height = 0.0f;

            for (int i = 0; i < text.Length; i++) {
                String code = tableB[text[i]];
                if ( code == null ) {
                    throw new Exception("The input string '" + text +
                            "' contains characters that are invalid in a Code39 barcode.");
                }

                for (int j = 0; j < 9; j++) {
                    char ch = code[j];
                    if (ch == 'w' || ch == 'b') {
                        height += m1;
                    } else if (ch == 'W' || ch == 'B') {
                        height += 3 * m1;
                    }
                }
                height += m1;
            }

            y += height - m1;
            for (int i = 0; i < text.Length; i++) {
                String code = tableB[text[i]];

                for (int j = 0; j < 9; j++) {
                    char ch = code[j];
                    if (ch == 'w') {
                        y -= m1;
                    } else if (ch == 'W') {
                        y -= 3 * m1;
                    } else if (ch == 'b') {
                        DrawHorzBar2(page, x, y, m1, h);
                        y -= m1;
                    } else if (ch == 'B') {
                        DrawHorzBar2(page, x, y, 3 * m1, h);
                        y -= 3 * m1;
                    }
                }

                y -= m1;
            }

            if (font != null) {
                y = y1 + ( height - m1);

                TextLine textLine = new TextLine(font, text);
                textLine.SetLocation(
                        x + w + font.bodyHeight,
                        y - ((y - y1) - font.StringWidth(text))/2);
                textLine.SetTextDirection(90);
                xy = textLine.DrawOn(page);
                xy[1] = Math.Max(y, xy[1]);
                return new float[] {xy[0], xy[1] + font.descent};
            }
        }

        return new float[] {xy[0], xy[1]};
    }

    private void DrawVertBar(
            Page page,
            float x,
            float y,
            float m1,   // Module length
            float h) {
        if (page != null) {
            page.SetPenWidth(m1);
            page.MoveTo(x + m1/2, y);
            page.LineTo(x + m1/2, y + h);
            page.StrokePath();
        }
    }

    private void DrawHorzBar(
            Page page,
            float x,
            float y,
            float m1,   // Module length
            float w) {
        if (page != null) {
            page.SetPenWidth(m1);
            page.MoveTo(x, y + m1/2);
            page.LineTo(x + w, y + m1/2);
            page.StrokePath();
        }
    }

    private void DrawHorzBar2(
            Page page,
            float x,
            float y,
            float m1,   // Module length
            float w) {
        if (page != null) {
            page.SetPenWidth(m1);
            page.MoveTo(x, y - m1/2);
            page.LineTo(x + w, y - m1/2);
            page.StrokePath();
        }
    }

    public float GetHeight() {
        if (font == null) {
            return m1 * barHeightFactor;
        }
        return m1 * barHeightFactor + font.GetHeight();
    }
}   // End of Barcode.cs
}   // End of namespace PDFjet.NET
