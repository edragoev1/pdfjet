/**
 * Barcode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 * Used to create one dimensional barcodes - EAN-13, UPC-A, Code 39 and Code 128.
 *
 * Please see Example_11.
 */
public class Barcode : IDrawable {
    /** Specifies EAN13 barcode */
    public static readonly int EAN_13 = 0;
    /** Specifies UPC barcode */
    public static readonly int UPC_A = 1;
    /** Specifies CODE128 barcode */
    public static readonly int CODE_128 = 2;
    /** Specifies CODE39 barcode */
    public static readonly int CODE_39 = 3;

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

    private String[] lCode = {
        "3211","2221","2122","1411","1132",
        "1231","1114","1312","1213","3112"};
    private String[] gCode = new String[10];
    private String[] lgMap = {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"};

    private Dictionary<Char, String> tableB = new Dictionary<Char, String>();

    /**
     * The constructor.
     *
     * @param barcodeType the type of the barcode.
     * @param text the content string of the barcode.
     */
    public Barcode(int barcodeType, String text) {
        this.barcodeType = barcodeType;
        this.text = text;

        if (barcodeType == Barcode.UPC_A && text.Length > 11) {
            throw new Exception("UPC-A barcodes can have maximum of 11 digits!");
        } else if (barcodeType == Barcode.EAN_13 && text.Length > 12) {
            throw new Exception("EAN-13 barcodes can have maximum of 12 digits!");
        }

        for (int i = 0; i < 10; i++) {
            char[] chars = lCode[i].ToCharArray();
            Array.Reverse(chars);
            gCode[i] = new String(chars);
        }

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
     * Sets the position where this barcode will be drawn on the page.
     *
     * @param x1 the x coordinate of the top left corner of the barcode.
     * @param y1 the y coordinate of the top left corner of the barcode.
     */
    public void SetPosition(double x1, double y1) {
        SetPosition((float) x1, (float) y1);
    }

    /**
     * Sets the position where this barcode will be drawn on the page.
     *
     * @param x1 the x coordinate of the top left corner of the barcode.
     * @param y1 the y coordinate of the top left corner of the barcode.
     */
    public void SetPosition(float x1, float y1) {
        SetLocation(x1, y1);
    }

    /**
     * Sets the location where this barcode will be drawn on the page.
     *
     * @param x1 the x coordinate of the top left corner of the barcode.
     * @param y1 the y coordinate of the top left corner of the barcode.
     */
    public Barcode SetLocation(float x1, float y1) {
        this.x1 = x1;
        this.y1 = y1;
        return (PDFjet.NET.Barcode) this;
    }

    /**
     * Sets the location where this barcode will be drawn on the page.
     *
     * @param x1 the x coordinate of the top left corner of the barcode.
     * @param y1 the y coordinate of the top left corner of the barcode.
     */
    public Barcode SetLocation(double x1, double y1) {
        return SetLocation((float) x1, (float) y1);
    }

    /**
     * Sets the module length of this barcode.
     * The default value is 0.75
     *
     * @param moduleLength the specified module length.
     */
    public void SetModuleLength(double moduleLength) {
        this.m1 = (float) moduleLength;
    }

    /**
     * Sets the module length of this barcode.
     * The default value is 0.75f
     *
     * @param moduleLength the specified module length.
     */
    public void SetModuleLength(float moduleLength) {
        this.m1 = moduleLength;
    }

    /**
     * Sets the bar height factor.
     * The height of the bars is the moduleLength * barHeightFactor
     * The default value is 50.0
     *
     * @param barHeightFactor the specified bar height factor.
     */
    public void SetBarHeightFactor(double barHeightFactor) {
        this.barHeightFactor = (float) barHeightFactor;
    }

    /**
     * Sets the bar height factor.
     * The height of the bars is the moduleLength * barHeightFactor
     * The default value is 50.0
     *
     * @param barHeightFactor the specified bar height factor.
     */
    public void SetBarHeightFactor(float barHeightFactor) {
        this.barHeightFactor = barHeightFactor;
    }

    /**
     * Sets the drawing direction for this font.
     *
     * @param direction the specified direction.
     */
    public void SetDirection(int direction) {
        this.direction = direction;
    }

    /**
     * Sets the font to be used with this barcode.
     *
     * @param font the specified font.
     */
    public void SetFont(Font font) {
        this.font = font;
    }

    /**
     * Draws this barcode on the specified page.
     *
     * @param page the page to draw on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception
     */
    public float[] DrawOn(Page page) {
        if (barcodeType == Barcode.EAN_13) {
            return DrawCodeEAN13(page, x1, y1);
        } else if (barcodeType == Barcode.UPC_A) {
            return DrawCodeUPC(page, x1, y1);
        } else if (barcodeType == Barcode.CODE_128) {
            return DrawCode128(page, x1, y1);
        } else if (barcodeType == Barcode.CODE_39) {
            return DrawCode39(page, x1, y1);
        } else {
            throw new Exception("Unsupported Barcode Type.");
        }
    }

    internal float[] DrawOnPageAtLocation(Page page, float x1, float y1) {
        if (barcodeType == Barcode.EAN_13) {
            return DrawCodeEAN13(page, x1, y1);
        } else if (barcodeType == Barcode.UPC_A) {
            return DrawCodeUPC(page, x1, y1);
        } else if (barcodeType == Barcode.CODE_128) {
            return DrawCode128(page, x1, y1);
        } else if (barcodeType == Barcode.CODE_39) {
            return DrawCode39(page, x1, y1);
        } else {
            throw new Exception("Unsupported Barcode Type.");
        }
    }

    private float[] DrawCodeUPC(Page page, float x1, float y1) {
        float x = x1;
        float y = y1;
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        int sum = 0;
        for (int i = 0; i < 11; i += 2) {   // even digits
            sum += (text[i] - '0') * 3;
        }
        for (int i = 1; i < 11; i += 2) {   // odd digits
            sum += (text[i] - '0');
        }
        int checkDigit = 0;
        int remainder = sum % 10;
        if (remainder > 0) {
            checkDigit = (10 - remainder);
        }
        // Use a local variable instead of mutating the text field - DrawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = text + checkDigit.ToString();

        x = DrawEGuard(page, x, y, m1, h + 8);
        for (int i = 0; i < 6; i++) {
            int digit = fullText[i] - '0';
            String str = lCode[digit];
            for (int j = 0; j < 4; j++) {
                int n = str[j] - '0';
                if (j%2 != 0) {
                    DrawVertBar(page, x, y, n*m1, h);
                }
                x += n*m1;
            }
        }
        x = DrawMGuard(page, x, y, m1, h + 8);
        for (int i = 6; i < 12; i++) {
            int digit = fullText[i] - '0';
            String str = lCode[digit];
            for (int j = 0; j < 4; j++) {
                int n = str[j] - '0';
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
                    fullText[0] +
                    "  " +
                    fullText[1] +
                    fullText[2] +
                    fullText[3] +
                    fullText[4] +
                    fullText[5] +
                    "   " +
                    fullText[6] +
                    fullText[7] +
                    fullText[8] +
                    fullText[9] +
                    fullText[10] +
                    "  " +
                    fullText[11];
            float fontSize = font.GetSize();
            font.SetSize(10f);

            TextLine textLine = new TextLine(font, label);
            textLine.SetLocation(
                    x1 + ((x - x1) - font.StringWidth(label))/2,
                    y1 + h + font.GetBodyHeight(font.GetSize()));
            xy = textLine.DrawOn(page);
            xy[0] = Math.Max(x, xy[0]);
            xy[1] = Math.Max(y, xy[1]);

            font.SetSize(fontSize);
            return new float[] {xy[0], xy[1] + font.GetDescent(font.GetSize())};
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
            page.AddArtifactBMC();
            DrawBar(page, x + (0.5f * m1), y, m1, h);
            DrawBar(page, x + (2.5f * m1), y, m1, h);
            page.AddEMC();
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
            page.AddArtifactBMC();
            DrawBar(page, x + (1.5f * m1), y, m1, h);
            DrawBar(page, x + (3.5f * m1), y, m1, h);
            page.AddEMC();
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
            // Some characters need two codewords (SHIFT/FNC_4 + value), so
            // checking list.Count == 48 only *after* adding them could skip
            // right over 48 (e.g. 47 -> 49) and never trip again, silently
            // encoding an unbounded number of characters past the documented
            // limit. Check before adding instead, so the cap always holds.
            int codewordsNeeded = (symchar < 32 || (symchar >= 128 && symchar < 256)) ? 2 : 1;
            if (list.Count + codewordsNeeded > 48) {
                // Maximum number of data characters is 48
                break;
            }
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
                        y1 + h + font.GetBodyHeight(font.GetSize()));
                xy = textLine.DrawOn(page);
                xy[0] = Math.Max(x, xy[0]);
                return new float[] {xy[0], xy[1] + font.GetDescent(font.GetSize())};
            } else if (direction == TOP_TO_BOTTOM) {
                TextLine textLine = new TextLine(font, text);
                textLine.SetLocation(
                        x + w + font.GetBodyHeight(font.GetSize()),
                        y - ((y - y1) - font.StringWidth(text))/2);
                textLine.SetTextDirection(90);
                xy = textLine.DrawOn(page);
                xy[1] = Math.Max(y, xy[1]);
            }
        }

        return xy;
    }

    private float[] DrawCode39(Page page, float x1, float y1) {
        // Use a local variable instead of mutating the text field - DrawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = "*" + text + "*";

        float x = x1;
        float y = y1;
        float w = m1 * barHeightFactor; // Barcode width when drawn vertically
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        float[] xy = new float[] {0f, 0f};

        if (direction == LEFT_TO_RIGHT) {
            for (int i = 0; i < fullText.Length; i++) {
                String code = tableB[fullText[i]];
                if ( code == null ) {
                    throw new Exception("The input string '" + fullText +
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
                TextLine textLine = new TextLine(font, fullText);
                textLine.SetLocation(
                        x1 + ((x - x1) - font.StringWidth(fullText))/2,
                        y1 + h + font.GetBodyHeight(font.GetSize()));
                xy = textLine.DrawOn(page);
                xy[0] = Math.Max(x, xy[0]);
            }
        } else if (direction == TOP_TO_BOTTOM) {
            for (int i = 0; i < fullText.Length; i++) {
                String code = tableB[fullText[i]];
                if ( code == null ) {
                    throw new Exception("The input string '" + fullText +
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
                TextLine textLine = new TextLine(font, fullText);
                textLine.SetLocation(
                        x - font.GetBodyHeight(font.GetSize()),
                        y1 + ((y - y1) - font.StringWidth(fullText))/2);
                textLine.SetTextDirection(270);
                xy = textLine.DrawOn(page);
                xy[0] = Math.Max(x, xy[0]) + w;
                xy[1] = Math.Max(y, xy[1]);
            }

        } else if (direction == BOTTOM_TO_TOP) {
            float height = 0.0f;

            for (int i = 0; i < fullText.Length; i++) {
                String code = tableB[fullText[i]];
                if ( code == null ) {
                    throw new Exception("The input string '" + fullText +
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
            for (int i = 0; i < fullText.Length; i++) {
                String code = tableB[fullText[i]];

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

                TextLine textLine = new TextLine(font, fullText);
                textLine.SetLocation(
                        x + w + font.GetBodyHeight(font.GetSize()),
                        y - ((y - y1) - font.StringWidth(fullText))/2);
                textLine.SetTextDirection(90);
                xy = textLine.DrawOn(page);
                xy[1] = Math.Max(y, xy[1]);
                return new float[] {xy[0], xy[1] + font.GetDescent()};
            }
        }

        return new float[] {xy[0], xy[1]};
    }

    private float[] DrawCodeEAN13(Page page, float x1, float y1) {
        float x = x1;
        float y = y1;
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        int sum = 0;
        for (int i = 0; i < 12; i += 2) {
            sum += (text[i] - 0x30);
        }
        for (int i = 1; i < 12; i += 2) {
            sum += (text[i] - 0x30) * 3;
        }
        int checkDigit = 0;
        int remainder = sum % 10;
        if (remainder > 0) {
            checkDigit = (10 - remainder);
        }
        // Use a local variable instead of mutating the text field - DrawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = text + checkDigit.ToString();

        x = DrawEGuard(page, x, y, m1, h + 8);
        String group1 = lgMap[fullText[0] - '0'];
        for (int i = 1; i < 7; i++) {
            int digit = fullText[i] - '0';
            String str = gCode[digit];
            if (group1[i - 1] == 'L') {
                str = lCode[digit];
            }
            int n = str[0] - '0';
            x += n*m1;
            n = str[1] - '0';
            DrawVertBar(page, x, y, n*m1, h);
            x += n*m1;
            n = str[2] - '0';
            x += n*m1;
            n = str[3] - '0';
            DrawVertBar(page, x, y, n*m1, h);
            x += n*m1;
        }
        x = DrawMGuard(page, x, y, m1, h + 8);
        for (int i = 7; i < 13; i++) {
            int digit = fullText[i] - '0';
            String str = lCode[digit];
            int n = str[0] - '0';
            DrawVertBar(page, x, y, n*m1, h);
            x += n*m1;
            n = str[1] - '0';
            x += n*m1;
            n = str[2] - '0';
            DrawVertBar(page, x, y, n*m1, h);
            x += n*m1;
            n = str[3] - '0';
            x += n*m1;
        }
        x = DrawEGuard(page, x, y, m1, h + 8);

        float[] xy = new float[] {x, y};

        if (font != null) {     // TODO:
            String label =
                    fullText[0] +
                    " " +
                    fullText[1] +
                    fullText[2] +
                    fullText[3] +
                    fullText[4] +
                    fullText[5] +
                    fullText[6] +
                    "    " +
                    fullText[7] +
                    fullText[8] +
                    fullText[9] +
                    fullText[10] +
                    fullText[11] +
                    fullText[12];
            float fontSize = font.GetSize();
            font.SetSize(10f);

            TextLine textLine = new TextLine(font, label);
            textLine.SetLocation(
                    x1 + ((x - x1) - font.StringWidth(label))/2,
                    y1 + h + font.GetBodyHeight(font.GetSize()));
            xy = textLine.DrawOn(page);
            xy[0] = Math.Max(x, xy[0]);
            xy[1] = Math.Max(y, xy[1]);

            font.SetSize(fontSize);

            return new float[] {xy[0], xy[1] + font.GetDescent(font.GetSize())};
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
            page.AddArtifactBMC();
            page.SetPenWidth(m1);
            page.MoveTo(x + m1/2, y);
            page.LineTo(x + m1/2, y + h);
            page.StrokePath();
            page.AddEMC();
        }
    }

    private void DrawHorzBar(
            Page page,
            float x,
            float y,
            float m1,   // Module length
            float w) {
        if (page != null) {
            page.AddArtifactBMC();
            page.SetPenWidth(m1);
            page.MoveTo(x, y + m1/2);
            page.LineTo(x + w, y + m1/2);
            page.StrokePath();
            page.AddEMC();
        }
    }

    private void DrawHorzBar2(
            Page page,
            float x,
            float y,
            float m1,   // Module length
            float w) {
        if (page != null) {
            page.AddArtifactBMC();
            page.SetPenWidth(m1);
            page.MoveTo(x, y - m1/2);
            page.LineTo(x + w, y - m1/2);
            page.StrokePath();
            page.AddEMC();
        }
    }

    /**
     * Returns the height of this barcode.
     * @return the height of this barcode.
     */
    public float GetHeight() {
        if (font == null) {
            return m1 * barHeightFactor;
        }
        return m1 * barHeightFactor + font.GetBodyHeight(font.GetSize());
    }
}   // End of Barcode.cs
}   // End of namespace PDFjet.NET
