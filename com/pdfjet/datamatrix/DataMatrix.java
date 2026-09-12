/*
 * DataMatrix.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.charset.StandardCharsets;
import java.util.Arrays;

/**
 * Used to create 2D Data Matrix barcodes: ECC 200 symbols as specified in
 * ISO/IEC 16022. The text is encoded as UTF-8 in the smallest symbol that holds
 * it. Please see Example_14.
 */
public final class DataMatrix implements Drawable {
    /** Square symbols, from 10x10 to 144x144 modules. */
    public static final int SQUARE = 0;

    /**
     * Rectangular symbols, from 8x18 to 16x48 modules. Data that does not fit
     * in a rectangular symbol gets a square symbol.
     */
    public static final int RECTANGLE = 1;

    // The symbols, smallest first: the rows and columns of modules, the rows and
    // columns of modules in each data region, the data codewords, the error
    // correction codewords and the number of Reed-Solomon blocks.
    private static final int[][] SQUARES = {
        {10, 10, 8, 8, 3, 5, 1},
        {12, 12, 10, 10, 5, 7, 1},
        {14, 14, 12, 12, 8, 10, 1},
        {16, 16, 14, 14, 12, 12, 1},
        {18, 18, 16, 16, 18, 14, 1},
        {20, 20, 18, 18, 22, 18, 1},
        {22, 22, 20, 20, 30, 20, 1},
        {24, 24, 22, 22, 36, 24, 1},
        {26, 26, 24, 24, 44, 28, 1},
        {32, 32, 14, 14, 62, 36, 1},
        {36, 36, 16, 16, 86, 42, 1},
        {40, 40, 18, 18, 114, 48, 1},
        {44, 44, 20, 20, 144, 56, 1},
        {48, 48, 22, 22, 174, 68, 1},
        {52, 52, 24, 24, 204, 84, 2},
        {64, 64, 14, 14, 280, 112, 2},
        {72, 72, 16, 16, 368, 144, 4},
        {80, 80, 18, 18, 456, 192, 4},
        {88, 88, 20, 20, 576, 224, 4},
        {96, 96, 22, 22, 696, 272, 4},
        {104, 104, 24, 24, 816, 336, 6},
        {120, 120, 18, 18, 1050, 408, 6},
        {132, 132, 20, 20, 1304, 496, 8},
        {144, 144, 22, 22, 1558, 620, 10},
    };

    private static final int[][] RECTANGLES = {
        {8, 18, 6, 16, 5, 7, 1},
        {8, 32, 6, 14, 10, 11, 1},
        {12, 26, 10, 24, 16, 14, 1},
        {12, 36, 10, 16, 22, 18, 1},
        {16, 36, 14, 16, 32, 24, 1},
        {16, 48, 14, 22, 49, 28, 1},
    };

    private static final int PAD = 129;
    private static final int BASE256_LATCH = 231;
    private static final int UPPER_SHIFT = 235;
    private static final int ECI = 241;
    private static final int ECI_UTF8 = 26;

    // Powers and logarithms of 2 in GF(256) with the prime polynomial 301.
    private static final int[] EXP = new int[255];
    private static final int[] LOG = new int[256];
    static {
        int value = 1;
        for (int i = 0; i < 255; i++) {
            EXP[i] = value;
            LOG[value] = i;
            value <<= 1;
            if (value > 255) {
                value ^= 301;
            }
        }
    }

    private final boolean[][] modules;
    private float x;
    private float y;
    private float m1 = 2f;      // Module length
    private int color = Color.black;

    // The codewords being placed in the mapping matrix, and the matrix.
    private int[] codewords;
    private int nrow;
    private int ncol;
    private boolean[][] dark;
    private boolean[][] placed;

    /**
     * Creates a square Data Matrix barcode.
     *
     * @param str the text to encode.
     */
    public DataMatrix(String str) {
        this(str, SQUARE);
    }

    /**
     * Creates a Data Matrix barcode.
     *
     * @param str the text to encode.
     * @param shape DataMatrix.SQUARE or DataMatrix.RECTANGLE.
     * @throws IllegalArgumentException if the text does not fit in the largest symbol.
     */
    public DataMatrix(String str, int shape) {
        int[] data = encode(str.getBytes(StandardCharsets.UTF_8));
        int[] symbol = selectSymbol(data.length, shape);
        int[] allCodewords = addErrorCorrection(pad(data, symbol[4]), symbol);
        int rows = symbol[0];
        int cols = symbol[1];
        int regionRows = symbol[2];
        int regionCols = symbol[3];
        placeCodewords(allCodewords, (rows / (regionRows + 2)) * regionRows, (cols / (regionCols + 2)) * regionCols);

        modules = new boolean[rows][cols];
        // Each data region has a solid line on its left and bottom sides and a
        // line of alternating modules on its top and right sides.
        for (int row = 0; row < rows; row += regionRows + 2) {
            for (int col = 0; col < cols; col++) {
                modules[row][col] = (col % 2 == 0);
                modules[row + regionRows + 1][col] = true;
            }
        }
        for (int col = 0; col < cols; col += regionCols + 2) {
            for (int row = 0; row < rows; row++) {
                modules[row][col] = true;
                modules[row][col + regionCols + 1] = (row % 2 == 1);
            }
        }
        for (int row = 0; row < nrow; row++) {
            for (int col = 0; col < ncol; col++) {
                modules[row + 1 + 2*(row / regionRows)][col + 1 + 2*(col / regionCols)] = dark[row][col];
            }
        }
    }

    /**
     * Sets the location where this barcode will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the barcode.
     * @param y the y coordinate of the top left corner of the barcode.
     * @return this DataMatrix object.
     */
    public DataMatrix setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     * Sets the location where this barcode will be drawn on the page.
     *
     * @param x the x coordinate of the top left corner of the barcode.
     * @param y the y coordinate of the top left corner of the barcode.
     * @return this DataMatrix object.
     */
    public DataMatrix setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the module length of this barcode. The default value is 2.0f.
     * Leave a margin of at least one module around the barcode.
     *
     * @param moduleLength the module length.
     * @return this DataMatrix object.
     */
    public DataMatrix setModuleLength(float moduleLength) {
        this.m1 = moduleLength;
        return this;
    }

    /**
     * Sets the module length of this barcode. The default value is 2.0f.
     * Leave a margin of at least one module around the barcode.
     *
     * @param moduleLength the module length.
     * @return this DataMatrix object.
     */
    public DataMatrix setModuleLength(double moduleLength) {
        return setModuleLength((float) moduleLength);
    }

    /**
     * Sets the color of the barcode.
     *
     * @param color the color as a 0xRRGGBB value.
     * @return this DataMatrix object.
     */
    public DataMatrix setColor(int color) {
        this.color = color;
        return this;
    }

    /**
     * Returns the modules of this barcode, by row and column; true is dark.
     *
     * @return the modules.
     */
    public boolean[][] getData() {
        return modules;
    }

    /**
     * Draws this barcode on the specified page. With no page nothing is drawn.
     *
     * @param page the page to draw on.
     * @return the x and y coordinates of the bottom right corner of this barcode.
     * @throws Exception if an input or output exception occurred.
     */
    public float[] drawOn(Page page) throws Exception {
        int rows = modules.length;
        int cols = modules[0].length;
        if (page != null) {
            page.setBrushColor(color);
            for (int row = 0; row < rows; row++) {
                int col = 0;
                while (col < cols) {
                    if (!modules[row][col]) {
                        col++;
                        continue;
                    }
                    int start = col;
                    while (col < cols && modules[row][col]) {
                        col++;
                    }
                    page.fillRect(x + start*m1, y + row*m1, (col - start)*m1, m1);
                }
            }
        }
        return new float[] {x + cols*m1, y + rows*m1};
    }

    // Encodes the bytes in ASCII encodation, or in Base 256 encodation when that
    // takes fewer codewords. Text that is not all ASCII starts with the ECI that
    // tells the reader the bytes are UTF-8.
    private static int[] encode(byte[] bytes) {
        int[] data = new int[2*bytes.length + 5];
        int n = 0;
        boolean ascii = true;
        for (byte b : bytes) {
            if ((b & 0xff) > 127) {
                ascii = false;
            }
        }
        if (!ascii) {
            data[n++] = ECI;
            data[n++] = ECI_UTF8 + 1;
        }
        int base256Length = 1 + ((bytes.length <= 249) ? 1 : 2) + bytes.length;
        if (asciiLength(bytes) <= base256Length) {
            int i = 0;
            while (i < bytes.length) {
                int c = bytes[i] & 0xff;
                if (isDigit(c) && i + 1 < bytes.length && isDigit(bytes[i + 1] & 0xff)) {
                    data[n++] = 130 + 10*(c - '0') + ((bytes[i + 1] & 0xff) - '0');
                    i += 2;
                } else if (c < 128) {
                    data[n++] = c + 1;
                    i++;
                } else {
                    data[n++] = UPPER_SHIFT;
                    data[n++] = c - 127;
                    i++;
                }
            }
        } else {
            data[n++] = BASE256_LATCH;
            if (bytes.length <= 249) {
                data[n] = randomize255(bytes.length, n + 1);
                n++;
            } else {
                data[n] = randomize255(bytes.length/250 + 249, n + 1);
                n++;
                data[n] = randomize255(bytes.length % 250, n + 1);
                n++;
            }
            for (byte b : bytes) {
                data[n] = randomize255(b & 0xff, n + 1);
                n++;
            }
        }
        return Arrays.copyOf(data, n);
    }

    // Returns the number of codewords of the bytes in ASCII encodation.
    private static int asciiLength(byte[] bytes) {
        int length = 0;
        int i = 0;
        while (i < bytes.length) {
            int c = bytes[i] & 0xff;
            if (isDigit(c) && i + 1 < bytes.length && isDigit(bytes[i + 1] & 0xff)) {
                length++;
                i += 2;
            } else {
                length += (c < 128) ? 1 : 2;
                i++;
            }
        }
        return length;
    }

    private static boolean isDigit(int c) {
        return c >= '0' && c <= '9';
    }

    // Scrambles a Base 256 codeword at the position, counted from 1, in the data.
    private static int randomize255(int value, int position) {
        int result = value + ((149*position) % 255) + 1;
        return (result <= 255) ? result : result - 256;
    }

    // Returns the smallest symbol of the shape that holds the data codewords.
    private static int[] selectSymbol(int length, int shape) {
        if (shape == RECTANGLE) {
            for (int[] symbol : RECTANGLES) {
                if (symbol[4] >= length) {
                    return symbol;
                }
            }
        }
        for (int[] symbol : SQUARES) {
            if (symbol[4] >= length) {
                return symbol;
            }
        }
        throw new IllegalArgumentException(
                "The text takes " + length + " codewords; a Data Matrix symbol holds 1558 at most.");
    }

    // Fills the rest of the symbol's data capacity with pad codewords: the first
    // one as it is, and the others scrambled by their position.
    private static int[] pad(int[] data, int capacity) {
        int[] padded = Arrays.copyOf(data, capacity);
        for (int i = data.length; i < capacity; i++) {
            if (i == data.length) {
                padded[i] = PAD;
            } else {
                int result = PAD + ((149*(i + 1)) % 253) + 1;
                padded[i] = (result <= 254) ? result : result - 254;
            }
        }
        return padded;
    }

    // Appends the error correction codewords. The codewords are spread over the
    // blocks in turn, the error correction codewords continuing where the data
    // codewords stop, and each block gets its own error correction codewords.
    private static int[] addErrorCorrection(int[] data, int[] symbol) {
        int blocks = symbol[6];
        int eccPerBlock = symbol[5] / blocks;
        int[] generator = generator(eccPerBlock);
        int[][] ecc = new int[blocks][];
        for (int block = 0; block < blocks; block++) {
            int[] blockData = new int[(data.length - block + blocks - 1) / blocks];
            for (int i = block, j = 0; i < data.length; i += blocks, j++) {
                blockData[j] = data[i];
            }
            ecc[block] = reedSolomon(blockData, generator);
        }
        int[] codewords = Arrays.copyOf(data, data.length + symbol[5]);
        int[] next = new int[blocks];
        for (int i = data.length; i < codewords.length; i++) {
            int block = i % blocks;
            codewords[i] = ecc[block][next[block]++];
        }
        return codewords;
    }

    // Returns the coefficients, lowest degree first, of the generator polynomial
    // (x + 2^1)(x + 2^2)...(x + 2^degree).
    private static int[] generator(int degree) {
        int[] g = new int[degree + 1];
        g[0] = 1;
        for (int j = 1; j <= degree; j++) {
            int root = EXP[j];
            for (int i = j; i > 0; i--) {
                g[i] = g[i - 1] ^ multiply(g[i], root);
            }
            g[0] = multiply(g[0], root);
        }
        return g;
    }

    // Returns the error correction codewords of the data: the remainder of the
    // data polynomial times x^degree divided by the generator, highest degree first.
    private static int[] reedSolomon(int[] data, int[] generator) {
        int degree = generator.length - 1;
        int[] remainder = new int[degree];
        for (int codeword : data) {
            int feedback = codeword ^ remainder[degree - 1];
            for (int i = degree - 1; i > 0; i--) {
                remainder[i] = remainder[i - 1] ^ multiply(feedback, generator[i]);
            }
            remainder[0] = multiply(feedback, generator[0]);
        }
        int[] ecc = new int[degree];
        for (int i = 0; i < degree; i++) {
            ecc[i] = remainder[degree - 1 - i];
        }
        return ecc;
    }

    private static int multiply(int a, int b) {
        if (a == 0 || b == 0) {
            return 0;
        }
        return EXP[(LOG[a] + LOG[b]) % 255];
    }

    // Places the codewords in the mapping matrix, the data regions of the symbol
    // without their borders, along the diagonals of ISO/IEC 16022 Annex F.
    private void placeCodewords(int[] codewords, int nrow, int ncol) {
        this.codewords = codewords;
        this.nrow = nrow;
        this.ncol = ncol;
        this.dark = new boolean[nrow][ncol];
        this.placed = new boolean[nrow][ncol];
        int chr = 0;
        int row = 4;
        int col = 0;
        do {
            if (row == nrow && col == 0) {
                corner1(chr++);
            }
            if (row == nrow - 2 && col == 0 && ncol % 4 != 0) {
                corner2(chr++);
            }
            if (row == nrow - 2 && col == 0 && ncol % 8 == 4) {
                corner3(chr++);
            }
            if (row == nrow + 4 && col == 2 && ncol % 8 == 0) {
                corner4(chr++);
            }
            // Up and to the right
            do {
                if (row < nrow && col >= 0 && !placed[row][col]) {
                    utah(row, col, chr++);
                }
                row -= 2;
                col += 2;
            } while (row >= 0 && col < ncol);
            row += 1;
            col += 3;
            // Down and to the left
            do {
                if (row >= 0 && col < ncol && !placed[row][col]) {
                    utah(row, col, chr++);
                }
                row += 2;
                col -= 2;
            } while (row < nrow && col >= 0);
            row += 3;
            col += 1;
        } while (row < nrow || col < ncol);
        // The bottom right corner is not always used by the codewords.
        if (!placed[nrow - 1][ncol - 1]) {
            dark[nrow - 1][ncol - 1] = true;
            dark[nrow - 2][ncol - 2] = true;
        }
    }

    // Places bit 1 (the most significant) to bit 8 of a codeword at the module,
    // wrapping positions outside the matrix around to the other side.
    private void module(int row, int col, int chr, int bit) {
        if (row < 0) {
            row += nrow;
            col += 4 - ((nrow + 4) % 8);
        }
        if (col < 0) {
            col += ncol;
            row += 4 - ((ncol + 4) % 8);
        }
        placed[row][col] = true;
        dark[row][col] = ((codewords[chr] >> (8 - bit)) & 1) == 1;
    }

    // The usual shape of a codeword, with its last bit at the row and column.
    private void utah(int row, int col, int chr) {
        module(row - 2, col - 2, chr, 1);
        module(row - 2, col - 1, chr, 2);
        module(row - 1, col - 2, chr, 3);
        module(row - 1, col - 1, chr, 4);
        module(row - 1, col, chr, 5);
        module(row, col - 2, chr, 6);
        module(row, col - 1, chr, 7);
        module(row, col, chr, 8);
    }

    private void corner1(int chr) {
        module(nrow - 1, 0, chr, 1);
        module(nrow - 1, 1, chr, 2);
        module(nrow - 1, 2, chr, 3);
        module(0, ncol - 2, chr, 4);
        module(0, ncol - 1, chr, 5);
        module(1, ncol - 1, chr, 6);
        module(2, ncol - 1, chr, 7);
        module(3, ncol - 1, chr, 8);
    }

    private void corner2(int chr) {
        module(nrow - 3, 0, chr, 1);
        module(nrow - 2, 0, chr, 2);
        module(nrow - 1, 0, chr, 3);
        module(0, ncol - 4, chr, 4);
        module(0, ncol - 3, chr, 5);
        module(0, ncol - 2, chr, 6);
        module(0, ncol - 1, chr, 7);
        module(1, ncol - 1, chr, 8);
    }

    private void corner3(int chr) {
        module(nrow - 3, 0, chr, 1);
        module(nrow - 2, 0, chr, 2);
        module(nrow - 1, 0, chr, 3);
        module(0, ncol - 2, chr, 4);
        module(0, ncol - 1, chr, 5);
        module(1, ncol - 1, chr, 6);
        module(2, ncol - 1, chr, 7);
        module(3, ncol - 1, chr, 8);
    }

    private void corner4(int chr) {
        module(nrow - 1, 0, chr, 1);
        module(nrow - 1, ncol - 1, chr, 2);
        module(0, ncol - 3, chr, 3);
        module(0, ncol - 2, chr, 4);
        module(0, ncol - 1, chr, 5);
        module(1, ncol - 3, chr, 6);
        module(1, ncol - 2, chr, 7);
        module(1, ncol - 1, chr, 8);
    }
}   // End of DataMatrix.java
