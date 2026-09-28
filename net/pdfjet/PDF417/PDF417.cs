/*
 * PDF417.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
///  Used to create PDF417 2D barcodes.
///
///  The bars are drawn from the location set with SetLocation. ISO/IEC 15438
///  asks for a quiet zone of at least two modules (twice the module width) on
///  all four sides of the symbol, so leave that much space around it; scanners
///  reject symbols with less.
///
///  Please see Example_12.
/// </summary>
public class PDF417 : IDrawable {
    private const int ALPHA = 0x08;
    private const int LOWER = 0x04;
    private const int MIXED = 0x02;
    private const int PUNCT = 0x01;

    private const int LATCH_TO_LOWER = 27;
    private const int SHIFT_TO_ALPHA = 27;
    private const int LATCH_TO_MIXED = 28;
    private const int LATCH_TO_ALPHA = 28;
    private const int SHIFT_TO_PUNCT = 29;
    // The codeword that shifts from text compaction to byte compaction for the
    // one codeword after it.
    private const int BYTE_SHIFT = 913;
    private float x1 = 0f;
    private float y1 = 0f;

    // Critical defaults!
    private float w1 = 0.75f;
    private float h1 = 0f;
    private int rows;
    private int cols = 18;
    private int[] codewords = null;
    private String str = null;
    private String altDescription;

    /// <summary>
    ///  Constructor for 2D barcodes.
    ///  The symbol has 18 columns and as many rows as the string needs, up to the
    ///  928 codewords a PDF417 symbol can hold: 864 data codewords, or about 1,300
    ///  characters of mixed text, with the error correction level 5 used here.
    ///  The string is ASCII, and a control character other than HT, LF and CR
    ///  takes a codeword of its own and one more, in byte compaction.
    ///  Throws an exception if there are unencodable characters or the string does not fit in a symbol.
    /// </summary>
    /// <param name="str">the specified string.</param>
    public PDF417(String str) {
        this.str = str;
        this.h1 = 3 * w1;

        foreach (char ch in str) {
            if (ch > 126) {
                throw new Exception("The string contains unencodable characters.");
            }
        }

        // The data codewords, after the symbol length descriptor
        List<Int32> list = DataCodewords();
        int dataCodewords = 1 + list.Count;
        rows = (dataCodewords + L5ECC.Table.Length + cols - 1) / cols;
        if (rows < 3) {
            rows = 3;
        }
        if (rows * cols > 928) {
            throw new Exception("The string is too long for a PDF417 barcode.");
        }
        this.codewords = new int[rows * (cols + 2)];

        int[] lfBuffer = new int[rows];
        int[] lrBuffer = new int[rows];
        int[] buffer = new int[rows * cols];

        // Left and right row indicators - see page 34 of the ISO specification
        int compression = 5;    // Compression Level
        int k = 1;
        for (int i = 0; i < rows; i++) {
            int lf = 0;
            int lr = 0;
            int cf = 30 * (i/3);
            if (k == 1) {
                lf = cf + (rows - 1) / 3;
                lr = cf + (cols - 1);
            } else if (k == 2) {
                lf = cf + 3*compression + (rows - 1) % 3;
                lr = cf + (rows - 1) / 3;
            } else if (k == 3) {
                lf = cf + (cols - 1);
                lr = cf + 3*compression + (rows - 1) % 3;
            }
            lfBuffer[i] = lf;
            lrBuffer[i] = lr;
            k++;
            if (k == 4) k = 1;
        }

        int dataLen = (rows * cols) - L5ECC.Table.Length;
        for (int i = 0; i < dataLen; i++) {
            buffer[i] = 900;    // The default pad codeword
        }
        buffer[0] = dataLen;
        list.CopyTo(buffer, 1);

        addECC(buffer);

        for (int i = 0; i < rows; i++) {
            int index = (cols + 2) * i;
            codewords[index] = lfBuffer[i];
            for (int j = 0; j < cols; j++) {
                codewords[index + j + 1] = buffer[cols*i + j];
            }
            codewords[index + cols + 1] = lrBuffer[i];
        }
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    ///  Sets the location of this barcode on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of the barcode.</param>
    /// <param name="y">the y coordinate of the top left corner of the barcode.</param>
    public PDF417 SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /// <summary>
    ///  Sets the module length of this barcode, the width of its narrowest bar.
    ///  This changes the barcode size while preserving the aspect.
    ///  Use value between 0.5f and 0.75f.
    ///  If the value is too small some scanners may have difficulty reading the barcode.
    /// </summary>
    /// <param name="moduleLength">the module length of the barcode.</param>
    /// <returns>this PDF417 object.</returns>
    public PDF417 SetModuleLength(float moduleLength) {
        this.w1 = moduleLength;
        this.h1 = 3 * w1;
        return this;
    }

    /// <summary>
    ///  Sets what the barcode says for a screen reader: a tagged document, PDF/UA
    ///  or a PDF/A of level A, then has the barcode as a figure of that
    ///  description. Without one, its bars are decoration, which a screen reader
    ///  skips.
    /// </summary>
    /// <param name="altDescription">the description.</param>
    /// <returns>this PDF417 object.</returns>
    public PDF417 SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    ///  Draws this barcode on the specified page. The bars are black, and the
    ///  pen of the page is as it was after it.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        if (page != null) {
            // Described, the barcode is a figure of a tagged document; not
            // described, its bars, which carry no text, are decoration.
            if (!String.IsNullOrEmpty(altDescription)) {
                page.AddBDC(StructElem.FIGURE, null, null, altDescription);
            } else {
                page.AddArtifactBMC();
            }
            page.SaveGraphicsState();
            page.SetPenColor(Color.black);
        }
        float[] xy = DrawPdf417(page);
        if (page != null) {
            page.RestoreGraphicsState();
            if (!String.IsNullOrEmpty(altDescription)) {
                page.SetFigureBoundingBox(x1, y1, xy[0] - x1, xy[1] - y1);
            }
            page.AddEMC();
        }
        return xy;
    }

    private List<Int32> textToArrayOfIntegers() {
        List<Int32> list = new List<Int32>();

        int currentMode = ALPHA;
        foreach (int ch in str) {
            if (ch == 0x20) {
                list.Add(26);   // The codeword for space
                continue;
            }
            if (IsByteShifted(ch)) {
                list.Add(-1 - ch);  // Below 0, see DataCodewords
                continue;
            }
            int value = TextCompact.Table[ch,1];
            int mode = TextCompact.Table[ch,2];
            if (mode == currentMode) {
                list.Add(value);
            } else {
                if (mode == ALPHA && currentMode == LOWER) {
                    list.Add(SHIFT_TO_ALPHA);
                    list.Add(value);
                } else if (mode == ALPHA && currentMode == MIXED) {
                    list.Add(LATCH_TO_ALPHA);
                    list.Add(value);
                    currentMode = mode;
                } else if (mode == LOWER && currentMode == ALPHA) {
                    list.Add(LATCH_TO_LOWER);
                    list.Add(value);
                    currentMode = mode;
                } else if (mode == LOWER && currentMode == MIXED) {
                    list.Add(LATCH_TO_LOWER);
                    list.Add(value);
                    currentMode = mode;
                } else if (mode == MIXED && currentMode == ALPHA) {
                    list.Add(LATCH_TO_MIXED);
                    list.Add(value);
                    currentMode = mode;
                } else if (mode == MIXED && currentMode == LOWER) {
                    list.Add(LATCH_TO_MIXED);
                    list.Add(value);
                    currentMode = mode;
                } else if (mode == PUNCT && currentMode == ALPHA) {
                    list.Add(SHIFT_TO_PUNCT);
                    list.Add(value);
                } else if (mode == PUNCT && currentMode == LOWER) {
                    list.Add(SHIFT_TO_PUNCT);
                    list.Add(value);
                } else if (mode == PUNCT && currentMode == MIXED) {
                    list.Add(SHIFT_TO_PUNCT);
                    list.Add(value);
                }
            }
        }

        return list;
    }

    // Tells if the character is a control character that text compaction has
    // no value for, and that is so encoded in byte compaction.
    private static bool IsByteShifted(int ch) {
        return ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r';
    }

    // Returns the data codewords of the string in text compaction, two values
    // to a codeword; a control character other than HT, LF and CR is the byte
    // compaction shift and its byte, which starts a codeword, so the value
    // before it may be padded, and after which text compaction goes on in the
    // submode it was in.
    internal List<Int32> DataCodewords() {
        List<Int32> list = textToArrayOfIntegers();
        List<Int32> codewords = new List<Int32>();
        int hi = -1;    // The first value of a codeword, if any
        foreach (int value in list) {
            if (value < 0) {
                if (hi != -1) {
                    codewords.Add(30*hi + SHIFT_TO_PUNCT);  // Pad
                    hi = -1;
                }
                codewords.Add(BYTE_SHIFT);
                codewords.Add(-1 - value);
            } else if (hi == -1) {
                hi = value;
            } else {
                codewords.Add(30*hi + value);
                hi = -1;
            }
        }
        if (hi != -1) {
            codewords.Add(30*hi + SHIFT_TO_PUNCT);  // Pad
        }
        return codewords;
    }

    private void addECC(int[] buf) {
        int[] ecc = new int[L5ECC.Table.Length];
        int t1 = 0;
        int t2 = 0;
        int t3 = 0;

        int dataLen = buf.Length - ecc.Length;
        for (int i = 0; i < dataLen; i++) {
            t1 = (buf[i] + ecc[ecc.Length - 1]) % 929;
            for (int j = ecc.Length - 1; j > 0; j--) {
                t2 = (t1 * L5ECC.Table[j]) % 929;
                t3 = 929 - t2;
                ecc[j] = (ecc[j - 1] + t3) % 929;
            }
            t2 = (t1 * L5ECC.Table[0]) % 929;
            t3 = 929 - t2;
            ecc[0] = t3 % 929;
        }

        for (int i = 0; i < ecc.Length; i++) {
            if (ecc[i] != 0) {
                buf[(buf.Length - 1) - i] = 929 - ecc[i];
            }
        }
    }

    private float[] DrawPdf417(Page page) {
        float x = x1;
        float y = y1;

        int[] startSymbol = {8, 1, 1, 1, 1, 1, 1, 3};
        for (int i = 0; i < startSymbol.Length; i++) {
            int n = startSymbol[i];
            if (i%2 == 0) {
                DrawBar(page, x, y, n * w1, rows * h1);
            }
            x += n * w1;
        }
        float x0 = x;   // Where the codewords of each row start

        int k = 1;  // Cluster index
        for (int i = 0; i < codewords.Length; i++) {
            int row = codewords[i];
            String symbol = Pattern.Table[row,k].ToString(CultureInfo.InvariantCulture);
            for (int j = 0; j < 8; j++) {
                int n = symbol[j] - 0x30;
                if (j%2 == 0) {
                    DrawBar(page, x, y, n * w1, h1);
                }
                x += n * w1;
            }
            if (i == codewords.Length - 1) break;
            if ((i + 1) % (cols + 2) == 0) {
                x = x0;
                y += h1;
                k++;
                if (k == 4) k = 1;
            }
        }

        y = y1;
        int[] endSymbol =   {7, 1, 1, 3, 1, 1, 1, 2, 1};
        for (int i = 0; i < endSymbol.Length; i++) {
            int n = endSymbol[i];
            if (i%2 == 0) {
                DrawBar(page, x, y, n * w1, rows * h1);
            }
            x += n * w1;
        }

        return new float[] {x, y + h1*rows};
    }

    private void DrawBar(
            Page page,
            float x,
            float y,
            float w,    // Bar width
            float h) {
        if (page == null) {
            return;     // Measured, not drawn
        }
        page.SetPenWidth(w);
        page.MoveTo(x + w/2, y);
        page.LineTo(x + w/2, y + h);
        page.StrokePath();
    }
}   // End of PDF417.cs
}   // End of namespace PDFjet.NET
