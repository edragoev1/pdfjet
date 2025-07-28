/**
 *  Barcode2D.cs
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
using System.Collections.Generic;

namespace PDFjet.NET {
/**
 *  Used to create PDF417 2D barcodes.
 *
 *  Please see Example_12.
 */
public class Barcode2D : IDrawable {
    private const int ALPHA = 0x08;
    private const int LOWER = 0x04;
    private const int MIXED = 0x02;
    private const int PUNCT = 0x01;

    private const int LATCH_TO_LOWER = 27;
    private const int SHIFT_TO_ALPHA = 27;
    private const int LATCH_TO_MIXED = 28;
    private const int LATCH_TO_ALPHA = 28;
    private const int SHIFT_TO_PUNCT = 29;
    private float x1 = 0f;
    private float y1 = 0f;

    // Critical defaults!
    private float w1 = 0.75f;
    private float h1 = 0f;
    private int rows = 50;
    private int cols = 18;
    private int[] codewords = null;
    private String str = null;

    /**
     *  Constructor for 2D barcodes.
     *
     *  @param str the specified string.
     */
    public Barcode2D(String str) {
        this.str = str;
        this.h1 = 3 * w1;
        this.codewords = new int[rows * (cols + 2)];

        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (ch > 126) {
                throw new Exception("The string contains unencodable characters.");
            }
        }

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

        int dataLen = (rows * cols) - ECC_L5.table.Length;
        for (int i = 0; i < dataLen; i++) {
            buffer[i] = 900;    // The default pad codeword
        }
        buffer[0] = dataLen;

        addData(buffer, dataLen);
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

    /**
     *  Sets the position of this barcode on the page.
     *
     *  @param x the x coordinate of the top left corner of the barcode.
     *  @param y the y coordinate of the top left corner of the barcode.
     */
    public void SetPosition(double x, double y) {
        SetPosition((float) x, (float) y);
    }

    /**
     *  Sets the position of this barcode on the page.
     *
     *  @param x the x coordinate of the top left corner of the barcode.
     *  @param y the y coordinate of the top left corner of the barcode.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public void SetXY(float x, float y) {
        SetLocation(x, y);
    }

    /**
     *  Sets the location of this barcode on the page.
     *
     *  @param x the x coordinate of the top left corner of the barcode.
     *  @param y the y coordinate of the top left corner of the barcode.
     */
    public Barcode2D SetLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     *  Sets the module width for this barcode.
     *  This changes the barcode size while preserving the aspect.
     *  Use value between 0.5f and 0.75f.
     *  If the value is too small some scanners may have difficulty reading the barcode.
     *
     *  @param width the module width of the barcode.
     */
    public void SetModuleWidth(float width) {
        this.w1 = width;
        this.h1 = 3 * w1;
    }

    /**
     *  Draws this barcode on the specified page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        return DrawPdf417(page);
    }

    private List<Int32> textToArrayOfIntegers() {
        List<Int32> list = new List<Int32>();

        int currentMode = ALPHA;
        int ch = 0;
        for (int i = 0; i < str.Length; i++) {
            ch = str[i];
            if (ch == 0x20) {
                list.Add(26);   // The codeword for space
                continue;
            }
            int value = TextCompact.TABLE[ch,1];
            int mode = TextCompact.TABLE[ch,2];
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

    private void addData(int[] buffer, int dataLen) {
        List<Int32> list = textToArrayOfIntegers();
        int bi = 1; // buffer index = 1 to skip the Symbol Length Descriptor
        int hi = 0;
        int lo = 0;
        for (int i = 0; i < list.Count; i += 2) {
            hi = list[i];
            if (i + 1 == list.Count) {
                lo = SHIFT_TO_PUNCT;    // Pad
            } else {
                lo = list[i + 1];
            }

            bi++;
            if (bi == dataLen) break;
            buffer[bi] = 30*hi + lo;
        }
    }

    private void addECC(int[] buf) {
        int[] ecc = new int[ECC_L5.table.Length];
        int t1 = 0;
        int t2 = 0;
        int t3 = 0;

        int dataLen = buf.Length - ecc.Length;
        for (int i = 0; i < dataLen; i++) {
            t1 = (buf[i] + ecc[ecc.Length - 1]) % 929;
            for (int j = ecc.Length - 1; j > 0; j--) {
                t2 = (t1 * ECC_L5.table[j]) % 929;
                t3 = 929 - t2;
                ecc[j] = (ecc[j - 1] + t3) % 929;
            }
            t2 = (t1 * ECC_L5.table[0]) % 929;
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
        x1 = x;

        int k = 1;  // Cluster index
        for (int i = 0; i < codewords.Length; i++) {
            int row = codewords[i];
            String symbol = PDF417.TABLE[row,k].ToString();
            for (int j = 0; j < 8; j++) {
                int n = symbol[j] - 0x30;
                if (j%2 == 0) {
                    DrawBar(page, x, y, n * w1, h1);
                }
                x += n * w1;
            }
            if (i == codewords.Length - 1) break;
            if ((i + 1) % (cols + 2) == 0) {
                x = x1;
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
        page.SetPenWidth(w);
        page.MoveTo(x + w/2, y);
        page.LineTo(x + w/2, y + h);
        page.StrokePath();
    }
}   // End of Barcode2D.cs
}   // End of namespace PDFjet.NET
