/*
 * PDF417.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.pdf417;

import com.pdfjet.*;

import java.util.*;

/**
 * Used to create PDF417 barcodes.
 *
 * The bars are drawn from the location set with setLocation. ISO/IEC 15438
 * asks for a quiet zone of at least two modules (twice the module width) on
 * all four sides of the symbol, so leave that much space around it; scanners
 * reject symbols with less.
 *
 * Please see Example_12.
 */
public class PDF417 implements Drawable {
    private static final int ALPHA = 0x08;
    private static final int LOWER = 0x04;
    private static final int MIXED = 0x02;
    private static final int PUNCT = 0x01;

    private static final int LATCH_TO_LOWER = 27;
    private static final int SHIFT_TO_ALPHA = 27;
    private static final int LATCH_TO_MIXED = 28;
    private static final int LATCH_TO_ALPHA = 28;
    private static final int SHIFT_TO_PUNCT = 29;

    private float x1 = 0f;
    private float y1 = 0f;

    // Critical defaults!
    private float w1 = 0.75f;
    private float h1 = 0f;

    private int rows;
    private int cols = 18;
    private int[] codewords;
    private String str;
    private String altDescription;

    // The codeword that shifts from text compaction to byte compaction for the
    // one codeword after it.
    private static final int BYTE_SHIFT = 913;

    /**
     * Constructor for PDF417 barcodes.
     * The symbol has 18 columns and as many rows as the string needs, up to the
     * 928 codewords a PDF417 symbol can hold: 864 data codewords, or about 1,300
     * characters of mixed text, with the error correction level 5 used here.
     * The string is ASCII, and a control character other than HT, LF and CR
     * takes a codeword of its own and one more, in byte compaction.
     *
     * @param str the specified string.
     * @throws Exception if there are unencodable characters or the string does not fit in a symbol.
     */
    public PDF417(String str) throws Exception {
        this.str = str;
        this.h1 = 3 * w1;

        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (ch > 126) {
                throw new Exception("The string contains unencodable characters.");
            }
        }

        // The data codewords, after the symbol length descriptor
        List<Integer> list = dataCodewords();
        int dataCodewords = 1 + list.size();
        rows = (dataCodewords + L5ECC.table.length + cols - 1) / cols;
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
        int compression = 5; // Compression Level
        int k = 1;
        for (int i = 0; i < rows; i++) {
            int lf = 0;
            int lr = 0;
            int cf = 30 * (i / 3);
            if (k == 1) {
                lf = cf + (rows - 1) / 3;
                lr = cf + (cols - 1);
            } else if (k == 2) {
                lf = cf + 3 * compression + (rows - 1) % 3;
                lr = cf + (rows - 1) / 3;
            } else if (k == 3) {
                lf = cf + (cols - 1);
                lr = cf + 3 * compression + (rows - 1) % 3;
            }
            lfBuffer[i] = lf;
            lrBuffer[i] = lr;
            k++;
            if (k == 4) {
                k = 1;
            }
        }

        int dataLen = (rows * cols) - L5ECC.table.length;
        for (int i = 0; i < dataLen; i++) {
            buffer[i] = 900; // The default pad codeword
        }
        buffer[0] = dataLen;
        for (int i = 0; i < list.size(); i++) {
            buffer[1 + i] = list.get(i);
        }
        addECC(buffer);

        for (int i = 0; i < rows; i++) {
            int index = (cols + 2) * i;
            codewords[index] = lfBuffer[i];
            for (int j = 0; j < cols; j++) {
                codewords[index + j + 1] = buffer[cols * i + j];
            }
            codewords[index + cols + 1] = lrBuffer[i];
        }
    }

    /**
     * Sets the location of this barcode on the page.
     *
     * @param x the x coordinate of the top left corner of the barcode.
     * @param y the y coordinate of the top left corner of the barcode.
     * @return this Barcode2D object.
     */
    public PDF417 setLocation(float x, float y) {
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the module length of this barcode, the width of its narrowest bar.
     * This changes the barcode size while preserving the aspect.
     * Use value between 0.5f and 0.75f.
     * If the value is too small some scanners may have difficulty reading the
     * barcode.
     *
     * @param moduleLength the module length of the barcode.
     * @return this PDF417 object.
     */
    public PDF417 setModuleLength(float moduleLength) {
        this.w1 = moduleLength;
        this.h1 = 3 * w1;
        return this;
    }

    private List<Integer> textToArrayOfIntegers() {
        List<Integer> list = new ArrayList<Integer>();

        int currentMode = ALPHA;
        int ch = 0;
        for (int i = 0; i < str.length(); i++) {
            ch = str.charAt(i);
            if (ch == 0x20) {
                list.add(26); // The codeword for space
                continue;
            }
            if (isByteShifted(ch)) {
                list.add(-1 - ch);  // Below 0, see dataCodewords
                continue;
            }

            int value = TextCompact.TABLE[ch][1];
            int mode = TextCompact.TABLE[ch][2];
            if (mode == currentMode) {
                list.add(value);
            } else {
                if (mode == ALPHA && currentMode == LOWER) {
                    list.add(SHIFT_TO_ALPHA);
                    list.add(value);
                } else if (mode == ALPHA && currentMode == MIXED) {
                    list.add(LATCH_TO_ALPHA);
                    list.add(value);
                    currentMode = mode;
                } else if (mode == LOWER && currentMode == ALPHA) {
                    list.add(LATCH_TO_LOWER);
                    list.add(value);
                    currentMode = mode;
                } else if (mode == LOWER && currentMode == MIXED) {
                    list.add(LATCH_TO_LOWER);
                    list.add(value);
                    currentMode = mode;
                } else if (mode == MIXED && currentMode == ALPHA) {
                    list.add(LATCH_TO_MIXED);
                    list.add(value);
                    currentMode = mode;
                } else if (mode == MIXED && currentMode == LOWER) {
                    list.add(LATCH_TO_MIXED);
                    list.add(value);
                    currentMode = mode;
                } else if (mode == PUNCT && currentMode == ALPHA) {
                    list.add(SHIFT_TO_PUNCT);
                    list.add(value);
                } else if (mode == PUNCT && currentMode == LOWER) {
                    list.add(SHIFT_TO_PUNCT);
                    list.add(value);
                } else if (mode == PUNCT && currentMode == MIXED) {
                    list.add(SHIFT_TO_PUNCT);
                    list.add(value);
                }
            }
        }

        return list;
    }

    // Tells if the character is a control character that text compaction has
    // no value for, and that is so encoded in byte compaction.
    private static boolean isByteShifted(int ch) {
        return ch < 0x20 && ch != '\t' && ch != '\n' && ch != '\r';
    }

    // Returns the data codewords of the string in text compaction, two values
    // to a codeword; a control character other than HT, LF and CR is the byte
    // compaction shift and its byte, which starts a codeword, so the value
    // before it may be padded, and after which text compaction goes on in the
    // submode it was in.
    final List<Integer> dataCodewords() {
        List<Integer> list = textToArrayOfIntegers();
        List<Integer> codewords = new ArrayList<Integer>();
        int hi = -1;    // The first value of a codeword, if any
        for (int value : list) {
            if (value < 0) {
                if (hi != -1) {
                    codewords.add(30 * hi + SHIFT_TO_PUNCT);    // Pad
                    hi = -1;
                }
                codewords.add(BYTE_SHIFT);
                codewords.add(-1 - value);
            } else if (hi == -1) {
                hi = value;
            } else {
                codewords.add(30 * hi + value);
                hi = -1;
            }
        }
        if (hi != -1) {
            codewords.add(30 * hi + SHIFT_TO_PUNCT);    // Pad
        }
        return codewords;
    }

    private void addECC(int[] buf) {
        int[] ecc = new int[L5ECC.table.length];
        int t1 = 0;
        int t2 = 0;
        int t3 = 0;

        int dataLen = buf.length - ecc.length;
        for (int i = 0; i < dataLen; i++) {
            t1 = (buf[i] + ecc[ecc.length - 1]) % 929;
            for (int j = ecc.length - 1; j > 0; j--) {
                t2 = (t1 * L5ECC.table[j]) % 929;
                t3 = 929 - t2;
                ecc[j] = (ecc[j - 1] + t3) % 929;
            }
            t2 = (t1 * L5ECC.table[0]) % 929;
            t3 = 929 - t2;
            ecc[0] = t3 % 929;
        }

        for (int i = 0; i < ecc.length; i++) {
            if (ecc[i] != 0) {
                buf[(buf.length - 1) - i] = 929 - ecc[i];
            }
        }
    }

    /**
     * Sets what the barcode says for a screen reader: a tagged document, PDF/UA
     * or a PDF/A of level A, then has the barcode as a figure of that
     * description. Without one, its bars are decoration, which a screen reader
     * skips.
     *
     * @param altDescription the description.
     * @return this PDF417 object.
     */
    public PDF417 setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     * Draws this barcode on the specified page. The bars are black, and the
     * pen of the page is as it was after it.
     *
     * @param page the page to draw this barcode on.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        boolean figure = altDescription != null && !altDescription.isEmpty();
        if (page != null) {
            // Described, the barcode is a figure of a tagged document; not
            // described, its bars, which carry no text, are decoration.
            if (figure) {
                page.addBDC(StructElem.FIGURE, null, null, altDescription);
            } else {
                page.addArtifactBMC();
            }
            page.saveGraphicsState();
            page.setPenColor(Color.black);
        }
        float[] xy = drawBars(page);
        if (page != null) {
            page.restoreGraphicsState();
            if (figure) {
                page.setFigureBoundingBox(x1, y1, xy[0] - x1, xy[1] - y1);
            }
            page.addEMC();
        }
        return xy;
    }

    // Draws the bars, or measures them with no page, and returns the bottom
    // right corner of the barcode.
    private float[] drawBars(Page page) throws Exception {
        float x = x1;
        float y = y1;

        int[] startSymbol = { 8, 1, 1, 1, 1, 1, 1, 3 };
        for (int i = 0; i < startSymbol.length; i++) {
            int n = startSymbol[i];
            if (i % 2 == 0) {
                drawBar(page, x, y, n * w1, rows * h1);
            }
            x += n * w1;
        }
        float x0 = x;   // Where the codewords of each row start

        int k = 1; // Cluster index
        for (int i = 0; i < codewords.length; i++) {
            int row = codewords[i];
            String symbol = Integer.toString(Pattern.TABLE[row][k]);
            for (int j = 0; j < 8; j++) {
                int n = symbol.charAt(j) - 0x30;
                if (j % 2 == 0) {
                    drawBar(page, x, y, n * w1, h1);
                }
                x += n * w1;
            }
            if (i == codewords.length - 1) {
                break;
            }
            if ((i + 1) % (cols + 2) == 0) {
                x = x0;
                y += h1;
                k++;
                if (k == 4) {
                    k = 1;
                }
            }
        }

        y = y1;
        int[] endSymbol = { 7, 1, 1, 3, 1, 1, 1, 2, 1 };
        for (int i = 0; i < endSymbol.length; i++) {
            int n = endSymbol[i];
            if (i % 2 == 0) {
                drawBar(page, x, y, n * w1, rows * h1);
            }
            x += n * w1;
        }

        return new float[] { x, y + h1 * rows };
    }

    private void drawBar(
            Page page,
            float x,
            float y,
            float w, // Bar width
            float h) throws Exception {
        if (page == null) {
            return;     // Measured, not drawn
        }
        page.setPenWidth(w);
        page.moveTo(x + w / 2, y);
        page.lineTo(x + w / 2, y + h);
        page.strokePath();
    }
} // End of PDF417.java
