/*
 * Barcode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Text;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Used to create one dimensional barcodes - EAN-13, UPC-A, Code 39 and Code 128.
///
/// Please see Example_11.
/// </summary>
public class Barcode : IDrawable {
    /// <summary>
    ///  Specifies EAN13 barcode
    /// </summary>
    public static readonly int EAN_13 = 0;
    /// <summary>
    ///  Specifies UPC barcode
    /// </summary>
    public static readonly int UPC_A = 1;
    /// <summary>
    ///  Specifies CODE128 barcode
    /// </summary>
    public static readonly int CODE_128 = 2;
    /// <summary>
    ///  Specifies CODE39 barcode
    /// </summary>
    public static readonly int CODE_39 = 3;

    /// <summary>
    /// Specifies GS1-128 barcode. Its text is GS1 data written as people read
    /// it, each Application Identifier in parentheses and its data after it,
    /// such as "(01)09506000134352(10)ABC123", and is drawn so under its bars.
    /// </summary>
    public static readonly int GS1_128 = 4;

    private int barcodeType = 0;
    private String text = null;
    private float x1 = 0.0f;
    private float y1 = 0.0f;
    private float m1 = 0.75f;   // Module length
    private float barHeightFactor = 50.0f;
    private Direction direction = Direction.LEFT_TO_RIGHT;
    private Font font = null;

    private String[] lCode = {
        "3211","2221","2122","1411","1132",
        "1231","1114","1312","1213","3112"};
    private String[] gCode = new String[10];
    private String[] lgMap = {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"};

    private Dictionary<Char, String> tableB = new Dictionary<Char, String>();

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="barcodeType">the type of the barcode.</param>
    /// <param name="text">the content string of the barcode.</param>
    public Barcode(int barcodeType, String text) {
        this.barcodeType = barcodeType;
        this.text = text;

        if (barcodeType == Barcode.UPC_A && (text.Length != 11 || !HasOnlyDigits(text))) {
            throw new Exception("UPC-A barcodes must have exactly 11 digits!");
        } else if (barcodeType == Barcode.EAN_13 && (text.Length != 12 || !HasOnlyDigits(text))) {
            throw new Exception("EAN-13 barcodes must have exactly 12 digits!");
        } else if (barcodeType == Barcode.CODE_128) {
            Code128Codewords(text);     // Throws for a text that a barcode cannot hold
        } else if (barcodeType == Barcode.GS1_128) {
            GS1128Codewords(text);      // Throws for data that is not GS1, or too long
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

    IDrawable IDrawable.SetLocation(float x1, float y1) {
        return SetLocation(x1, y1);
    }

    /// <summary>
    /// Sets the location where this barcode will be drawn on the page.
    /// </summary>
    /// <param name="x1">the x coordinate of the top left corner of the barcode.</param>
    /// <param name="y1">the y coordinate of the top left corner of the barcode.</param>
    public Barcode SetLocation(float x1, float y1) {
        this.x1 = x1;
        this.y1 = y1;
        return (PDFjet.NET.Barcode) this;
    }

    /// <summary>
    /// Sets the module length of this barcode.
    /// The default value is 0.75f
    /// </summary>
    /// <param name="moduleLength">the specified module length.</param>
    /// <returns>this Barcode object.</returns>
    public Barcode SetModuleLength(float moduleLength) {
        this.m1 = moduleLength;
        return this;
    }

    /// <summary>
    /// Sets the bar height factor.
    /// The height of the bars is the moduleLength * barHeightFactor
    /// The default value is 50.0
    /// </summary>
    /// <param name="barHeightFactor">the specified bar height factor.</param>
    /// <returns>this Barcode object.</returns>
    public Barcode SetBarHeightFactor(float barHeightFactor) {
        this.barHeightFactor = barHeightFactor;
        return this;
    }

    /// <summary>
    /// Sets the direction in which this barcode is drawn.
    /// </summary>
    /// <param name="direction">the specified direction.</param>
    /// <returns>this Barcode object.</returns>
    public Barcode SetDirection(Direction direction) {
        this.direction = direction;
        return this;
    }

    /// <summary>
    /// Sets the font to be used with this barcode.
    /// </summary>
    /// <param name="font">the specified font.</param>
    /// <returns>this Barcode object.</returns>
    public Barcode SetFont(Font font) {
        this.font = font;
        return this;
    }

    private static bool HasOnlyDigits(String text) {
        foreach (char ch in text) {
            if (ch < '0' || ch > '9') {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Draws this barcode on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        if (page != null) {
            // The bars are black whatever pen color the page was left with
            page.SaveGraphicsState();
            page.SetPenColor(Color.black);
        }
        try {
            if (barcodeType == Barcode.EAN_13) {
                return DrawCodeEAN13(page, x1, y1);
            } else if (barcodeType == Barcode.UPC_A) {
                return DrawCodeUPC(page, x1, y1);
            } else if (barcodeType == Barcode.CODE_128 || barcodeType == Barcode.GS1_128) {
                return DrawCode128(page, x1, y1);
            } else if (barcodeType == Barcode.CODE_39) {
                return DrawCode39(page, x1, y1);
            } else {
                throw new Exception("Unsupported Barcode Type.");
            }
        } finally {
            if (page != null) {
                page.RestoreGraphicsState();
            }
        }
    }

    private float[] DrawCodeUPC(Page page, float x1, float y1) {
        float x = x1;
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
        Bars bars = new Bars(x1, y1, 95f * m1, h + 8f, direction);  // 95 modules

        x = DrawEGuard(page, bars, x, h + 8);
        float xGroup1Start = x;
        for (int i = 0; i < 6; i++) {
            int digit = fullText[i] - '0';
            String str = lCode[digit];
            for (int j = 0; j < 4; j++) {
                int n = str[j] - '0';
                if (j%2 != 0) {
                    DrawBar(page, bars, x, n*m1, h);
                }
                x += n*m1;
            }
            if (i == 0) {
                xGroup1Start = x;   // Start of the 2nd-6th digit bars (digit 0 is drawn outside)
            }
        }
        float xLeftGroupEnd = x;
        x = DrawMGuard(page, bars, x, h + 8);
        float xRightGroupStart = x;
        float xGroup2End = 0f;
        for (int i = 6; i < 12; i++) {
            if (i == 11) {
                xGroup2End = x;     // End of the 7th-11th digit bars (digit 11 is drawn outside)
            }
            int digit = fullText[i] - '0';
            String str = lCode[digit];
            for (int j = 0; j < 4; j++) {
                int n = str[j] - '0';
                if (j%2 == 0) {
                    DrawBar(page, bars, x, n*m1, h);
                }
                x += n*m1;
            }
        }
        x = DrawEGuard(page, bars, x, h + 8);

        float left = x1;
        float right = x;
        float bottom = y1 + h + 8;
        if (font != null) {
            // Standard UPC-A layout: the leading (number system) digit and
            // the trailing check digit are printed in the quiet zones
            // outside the guard bars, not centered under them together with
            // the rest of the label. The two groups of 5 digits are each
            // centered under their own bar section.
            String firstDigit = fullText.Substring(0, 1);
            String group1 = fullText.Substring(1, 5);
            String group2 = fullText.Substring(6, 5);
            String lastDigit = fullText.Substring(11, 1);

            float fontSize = font.GetSize();
            font.SetSize(10f);
            float yText = y1 + h + font.GetBodyHeight(font.GetSize());
            float gap = font.StringWidth(" ");

            left = x1 - gap - font.StringWidth(firstDigit);
            DrawText(page, bars, firstDigit, left, yText);
            DrawText(page, bars, group1,
                    xGroup1Start + ((xLeftGroupEnd - xGroup1Start) - font.StringWidth(group1))/2,
                    yText);
            DrawText(page, bars, group2,
                    xRightGroupStart + ((xGroup2End - xRightGroupStart) - font.StringWidth(group2))/2,
                    yText);
            float[] xy = DrawText(page, bars, lastDigit, x + gap, yText);
            right = xy[0];
            bottom = Math.Max(bottom, xy[1]);

            font.SetSize(fontSize);
        }

        return bars.GetBottomRight(left, right, bottom);
    }

    private float DrawEGuard(Page page, Bars bars, float x, float h) {
        if (page != null) {
            // 101
            page.AddArtifactBMC();
            StrokeBar(page, bars, x + (0.5f * m1), m1, h);
            StrokeBar(page, bars, x + (2.5f * m1), m1, h);
            page.AddEMC();
        }
        return (x + (3.0f * m1));
    }

    private float DrawMGuard(Page page, Bars bars, float x, float h) {
        if (page != null) {
            // 01010
            page.AddArtifactBMC();
            StrokeBar(page, bars, x + (1.5f * m1), m1, h);
            StrokeBar(page, bars, x + (3.5f * m1), m1, h);
            page.AddEMC();
        }
        return (x + (5.0f * m1));
    }

    // Draws the bar of width w and height h that starts at x.
    private void DrawBar(Page page, Bars bars, float x, float w, float h) {
        if (page != null) {
            page.AddArtifactBMC();
            StrokeBar(page, bars, x + w/2, w, h);
            page.AddEMC();
        }
    }

    // Strokes the bar of width w and height h centered on x.
    private void StrokeBar(Page page, Bars bars, float x, float w, float h) {
        if (page != null) {
            float[] top = bars.Turn(x, bars.y1);
            float[] bottom = bars.Turn(x, bars.y1 + h);
            page.SetPenWidth(w);
            page.MoveTo(top[0], top[1]);
            page.LineTo(bottom[0], bottom[1]);
            page.StrokePath();
        }
    }

    // Draws the text with its baseline starting at (x, y). Returns the end of
    // the baseline and the bottom of the text, before the turn.
    private float[] DrawText(Page page, Bars bars, String str, float x, float y) {
        TextLine textLine = new TextLine(font, str);
        float[] xy = bars.Turn(x, y);
        textLine.SetLocation(xy[0], xy[1]);
        if (direction == Direction.TOP_TO_BOTTOM) {
            textLine.SetTextRotation(-270);
        } else if (direction == Direction.BOTTOM_TO_TOP) {
            textLine.SetTextRotation(-90);
        }
        textLine.DrawOn(page);
        return new float[] {x + font.StringWidth(str), y + font.GetDescent(font.GetSize())};
    }

    // Returns the start and the codewords of the text: runs of four digits or
    // more in code set C, two digits to a codeword, and the rest in code set B,
    // where a character from 32 to 127 takes one codeword, and one below 32 or
    // from 128 to 255 two, SHIFT or FNC 4 and the character. It throws for a
    // character above 255, which the code sets cannot hold, and for a text of
    // more than 48 codewords, the most a barcode holds, rather than draw a
    // barcode of another text.
    private static List<Int32> Code128Codewords(String text) {
        List<Int32> items = new List<Int32>();
        foreach (char symchar in text) {
            if (symchar > 255) {
                throw new Exception("Code 128 barcodes can only hold characters up to U+00FF!");
            }
            items.Add(symchar);
        }
        List<Int32> list = Code128Encode(items);
        if (list.Count - 1 > 48) {
            throw new Exception(
                    "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!");
        }
        return list;
    }

    // Returns the start and the codewords of the GS1 data written as people
    // read it: FNC1, then the fields, each followed by FNC1 when it is of no
    // set length and another follows, encoded as Code128Encode encodes them.
    // It throws for data that is not GS1, see GS1.Parse, or longer than the 48
    // characters a GS1-128 barcode holds, not counting the separators.
    private static List<Int32> GS1128Codewords(String text) {
        List<Int32> items = new List<Int32>();
        int characters = 0;
        foreach (GS1.Field field in GS1.Parse(text)) {
            String s = field.ai + field.data;
            foreach (char c in s) {
                items.Add(c);
            }
            characters += s.Length;
            if (field.separator) {
                items.Add(SEPARATOR);
            }
        }
        if (characters > 48) {
            throw new ArgumentException(
                    "GS1-128 barcodes hold at most 48 characters, not counting the separators!");
        }
        List<Int32> list = Code128Encode(items);
        list.Insert(1, Code128Table.FNC_1);
        return list;
    }

    // The item of Code128Encode that is FNC1.
    private const int SEPARATOR = -1;

    // Returns the start and the codewords of the characters, up to 255, and
    // SEPARATOR for FNC1. It starts in code set C if the characters start with
    // four digits or more, or are two digits, and in code set B if not. In code
    // set C it takes the digits two to a codeword, and changes to code set B at
    // anything else; in code set B it changes to code set C at an even run of
    // four digits or more, and so takes the first digit of an odd run in code
    // set B.
    private static List<Int32> Code128Encode(List<Int32> items) {
        bool inC = Digits(items, 0) >= 4 || (Digits(items, 0) == 2 && items.Count == 2);
        List<Int32> list = new List<Int32>();
        list.Add(inC ? Code128Table.START_C : Code128Table.START_B);
        int i = 0;
        while (i < items.Count) {
            int c = items[i];
            if (c == SEPARATOR) {
                list.Add(Code128Table.FNC_1);
                i++;
            } else if (inC && Digits(items, i) >= 2) {
                list.Add(10*(c - '0') + (items[i + 1] - '0'));
                i += 2;
            } else if (inC) {
                list.Add(Code128Table.CODE_B);
                inC = false;
            } else if (Digits(items, i) >= 4 && Digits(items, i) % 2 == 0) {
                list.Add(Code128Table.CODE_C);
                inC = true;
            } else if (c < 32) {
                list.Add(Code128Table.SHIFT);
                list.Add(c + 64);
                i++;
            } else if (c < 128) {
                list.Add(c - 32);
                i++;
            } else {
                list.Add(Code128Table.FNC_4);
                list.Add(c - 160);      // 128 + 32
                i++;
            }
        }
        return list;
    }

    // Returns how many digits there are in a row from i.
    private static int Digits(List<Int32> items, int i) {
        int n = 0;
        while (i + n < items.Count && items[i + n] >= '0' && items[i + n] <= '9') {
            n++;
        }
        return n;
    }

    private float[] DrawCode128(Page page, float x1, float y1) {
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        List<Int32> list = (barcodeType == Barcode.GS1_128) ? GS1128Codewords(text) : Code128Codewords(text);
        int start = list[0];
        list.RemoveAt(0);

        StringBuilder buf = new StringBuilder();
        int checkDigit = start;
        buf.Append((char) checkDigit);
        for (int i = 0; i < list.Count; i++) {
            int codeword = list[i];
            buf.Append((char) codeword);
            checkDigit += codeword * (i + 1);
        }
        checkDigit %= Code128Table.START_A;
        buf.Append((char) checkDigit);
        buf.Append((char) Code128Table.STOP);

        float length = 0f;
        for (int i = 0; i < buf.Length; i++) {
            String symbol = Code128Table.TABLE[buf[i]].ToString();
            for (int j = 0; j < symbol.Length; j++) {
                length += (symbol[j] - 0x30) * m1;
            }
        }

        Bars bars = new Bars(x1, y1, length, h, direction);
        float x = x1;
        for (int i = 0; i < buf.Length; i++) {
            int si = buf[i];
            String symbol = Code128Table.TABLE[si].ToString();
            for (int j = 0; j < symbol.Length; j++) {
                int n = symbol[j] - 0x30;
                if (j%2 == 0) {
                    DrawBar(page, bars, x, m1 * n, h);
                }
                x += n * m1;
            }
        }

        float right = x;
        float bottom = y1 + h;
        if (font != null) {
            float[] xy = DrawText(page, bars, text,
                    x1 + ((x - x1) - font.StringWidth(text))/2,
                    y1 + h + font.GetBodyHeight(font.GetSize()));
            right = Math.Max(right, xy[0]);
            bottom = xy[1];
        }

        return bars.GetBottomRight(x1, right, bottom);
    }

    private float[] DrawCode39(Page page, float x1, float y1) {
        // Use a local variable instead of mutating the text field - DrawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = "*" + text + "*";
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        float length = 0f;
        foreach (char symbol in fullText) {
            if (!tableB.TryGetValue(symbol, out String code)) {
                throw new Exception("The input string '" + fullText +
                        "' contains characters that are invalid in a Code39 barcode.");
            }
            foreach (char ch in code) {
                length += (ch == 'W' || ch == 'B') ? 3 * m1 : m1;
            }
            length += m1;
        }
        length -= m1;   // There is no gap after the last character

        Bars bars = new Bars(x1, y1, length, h, direction);
        float x = x1;
        foreach (char symbol in fullText) {
            String code = tableB[symbol];
            for (int j = 0; j < 9; j++) {
                char ch = code[j];
                if (ch == 'w') {
                    x += m1;
                } else if (ch == 'W') {
                    x += m1 * 3;
                } else if (ch == 'b') {
                    DrawBar(page, bars, x, m1, h);
                    x += m1;
                } else if (ch == 'B') {
                    DrawBar(page, bars, x, m1 * 3, h);
                    x += m1 * 3;
                }
            }
            x += m1;
        }

        float right = x1 + length;
        float bottom = y1 + h;
        if (font != null) {
            float[] xy = DrawText(page, bars, fullText,
                    x1 + (length - font.StringWidth(fullText))/2,
                    y1 + h + font.GetBodyHeight(font.GetSize()));
            right = Math.Max(right, xy[0]);
            bottom = xy[1];
        }

        return bars.GetBottomRight(x1, right, bottom);
    }

    private float[] DrawCodeEAN13(Page page, float x1, float y1) {
        float x = x1;
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
        Bars bars = new Bars(x1, y1, 95f * m1, h + 8f, direction);  // 95 modules

        x = DrawEGuard(page, bars, x, h + 8);
        float xLeftGroupStart = x;
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
            DrawBar(page, bars, x, n*m1, h);
            x += n*m1;
            n = str[2] - '0';
            x += n*m1;
            n = str[3] - '0';
            DrawBar(page, bars, x, n*m1, h);
            x += n*m1;
        }
        float xLeftGroupEnd = x;
        x = DrawMGuard(page, bars, x, h + 8);
        float xRightGroupStart = x;
        for (int i = 7; i < 13; i++) {
            int digit = fullText[i] - '0';
            String str = lCode[digit];
            int n = str[0] - '0';
            DrawBar(page, bars, x, n*m1, h);
            x += n*m1;
            n = str[1] - '0';
            x += n*m1;
            n = str[2] - '0';
            DrawBar(page, bars, x, n*m1, h);
            x += n*m1;
            n = str[3] - '0';
            x += n*m1;
        }
        float xRightGroupEnd = x;
        x = DrawEGuard(page, bars, x, h + 8);

        float left = x1;
        float right = x;
        float bottom = y1 + h + 8;

        if (font != null) {
            // Standard EAN-13 layout: the leading (number system) digit sits
            // in the quiet zone to the left of the start guard bars, not
            // centered under them together with the rest of the label. The
            // two groups of 6 digits are each centered under their own bar
            // section (left group / right group), not under the barcode as
            // a whole.
            String firstDigit = fullText.Substring(0, 1);
            String leftGroup = fullText.Substring(1, 6);
            String rightGroup = fullText.Substring(7, 6);

            float fontSize = font.GetSize();
            font.SetSize(10f);
            float yText = y1 + h + font.GetBodyHeight(font.GetSize());
            float gap = font.StringWidth(" ");

            left = x1 - gap - font.StringWidth(firstDigit);
            DrawText(page, bars, firstDigit, left, yText);
            DrawText(page, bars, leftGroup,
                    xLeftGroupStart + ((xLeftGroupEnd - xLeftGroupStart) - font.StringWidth(leftGroup))/2,
                    yText);
            float[] xy = DrawText(page, bars, rightGroup,
                    xRightGroupStart + ((xRightGroupEnd - xRightGroupStart) - font.StringWidth(rightGroup))/2,
                    yText);
            right = Math.Max(right, xy[0]);
            bottom = Math.Max(bottom, xy[1]);

            font.SetSize(fontSize);
        }

        return bars.GetBottomRight(left, right, bottom);
    }

    // The bars of a barcode, length long and height high, drawn left to right from
    // (x1, y1) and turned to the direction of the barcode: top to bottom is a quarter
    // turn clockwise and bottom to top a quarter turn counter-clockwise, and the bars
    // stay right of x1 and below y1. The draw methods take the coordinates of the
    // barcode drawn left to right.
    private sealed class Bars {
        internal readonly float x1;
        internal readonly float y1;
        private readonly float length;
        private readonly float height;
        private readonly Direction direction;

        internal Bars(float x1, float y1, float length, float height, Direction direction) {
            this.x1 = x1;
            this.y1 = y1;
            this.length = length;
            this.height = height;
            this.direction = direction;
        }

        // Returns the point (x, y) turned to the direction of the barcode.
        internal float[] Turn(float x, float y) {
            if (direction == Direction.TOP_TO_BOTTOM) {
                return new float[] {x1 + height - (y - y1), y1 + (x - x1)};
            } else if (direction == Direction.BOTTOM_TO_TOP) {
                return new float[] {x1 + (y - y1), y1 + length - (x - x1)};
            }
            return new float[] {x, y};
        }

        // Returns the bottom right corner, turned to the direction of the barcode, of
        // a barcode that spans from left to right and from y1 to bottom.
        internal float[] GetBottomRight(float left, float right, float bottom) {
            if (direction == Direction.TOP_TO_BOTTOM) {
                return new float[] {x1 + height, y1 + (right - x1)};
            } else if (direction == Direction.BOTTOM_TO_TOP) {
                return new float[] {x1 + (bottom - y1), y1 + length + (x1 - left)};
            }
            return new float[] {right, bottom};
        }
    }

    /// <summary>
    /// Returns the height of this barcode as it is drawn: from its location to the bottom of the
    /// bars, or of the text under them, in the direction the barcode is drawn, so a barcode drawn
    /// top to bottom or bottom to top is as tall as it is long.
    /// </summary>
    /// <returns>the height of this barcode.</returns>
    public float GetHeight() {
        return DrawOn(null)[1] - y1;
    }
}   // End of Barcode.cs
}   // End of namespace PDFjet.NET
