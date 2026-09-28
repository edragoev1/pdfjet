/*
 * QRUtil.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Kazuhiko Arase, 2009
 * URL: http://www.d-project.com/
 * Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
 *
 * The word "QR Code" is a registered trademark of
 * DENSO WAVE INCORPORATED
 * http://www.denso-wave.com/qrcode/faqpatent-e.html
 *
 * Modified and adapted for use in PDFjet by PDFjet Software
 */
package com.pdfjet.qrcode;

import com.pdfjet.*;

class QRUtil {
    /** The centers of the alignment patterns of each version, in rows and columns. */
    private static final int[][] PATTERN_POSITION_TABLE = {
        {},
        {6, 18},
        {6, 22},
        {6, 26},
        {6, 30},
        {6, 34},
        {6, 22, 38},
        {6, 24, 42},
        {6, 26, 46},
        {6, 28, 50},
        {6, 30, 54},
        {6, 32, 58},
        {6, 34, 62},
        {6, 26, 46, 66},
        {6, 26, 48, 70},
        {6, 26, 50, 74},
        {6, 30, 54, 78},
        {6, 30, 56, 82},
        {6, 30, 58, 86},
        {6, 34, 62, 90},
        {6, 28, 50, 72, 94},
        {6, 26, 50, 74, 98},
        {6, 30, 54, 78, 102},
        {6, 28, 54, 80, 106},
        {6, 32, 58, 84, 110},
        {6, 30, 58, 86, 114},
        {6, 34, 62, 90, 118},
        {6, 26, 50, 74, 98, 122},
        {6, 30, 54, 78, 102, 126},
        {6, 26, 52, 78, 104, 130},
        {6, 30, 56, 82, 108, 134},
        {6, 34, 60, 86, 112, 138},
        {6, 30, 58, 86, 114, 142},
        {6, 34, 62, 90, 118, 146},
        {6, 30, 54, 78, 102, 126, 150},
        {6, 24, 50, 76, 102, 128, 154},
        {6, 28, 54, 80, 106, 132, 158},
        {6, 32, 58, 84, 110, 136, 162},
        {6, 26, 54, 82, 110, 138, 166},
        {6, 30, 58, 86, 114, 142, 170},
    };

    protected static int[] getPatternPosition(int typeNumber) {
        return PATTERN_POSITION_TABLE[typeNumber - 1];
    }

    protected static Polynomial getErrorCorrectPolynomial(int errorCorrectLength) {
        Polynomial a = new Polynomial(new int[] {1});
        for (int i = 0; i < errorCorrectLength; i++) {
            a = a.multiply(new Polynomial(new int[] { 1, QRMath.gexp(i) }));
        }
        return a;
    }

    protected static boolean getMask(int maskPattern, int i, int j) {
        switch (maskPattern) {
        case MaskPattern.PATTERN000 : return (i + j) % 2 == 0;
        case MaskPattern.PATTERN001 : return (i % 2) == 0;
        case MaskPattern.PATTERN010 : return (j % 3) == 0;
        case MaskPattern.PATTERN011 : return (i + j) % 3 == 0;
        case MaskPattern.PATTERN100 : return (i / 2 + j / 3) % 2 == 0;
        case MaskPattern.PATTERN101 : return (i * j) % 2 + (i * j) % 3 == 0;
        case MaskPattern.PATTERN110 : return ((i * j) % 2 + (i * j) % 3) % 2 == 0;
        case MaskPattern.PATTERN111 : return ((i * j) % 3 + (i + j) % 2) % 2 == 0;
        default :
            throw new IllegalArgumentException("mask: " + maskPattern);
        }
    }

    // Returns the penalty of the modules with the rules of ISO/IEC 18004
    // 7.8.3.1: N1 = 3 for five modules of a color in a row or a column, and 1
    // for each one more; N2 = 3 for each block of 2 by 2 modules of a color;
    // N3 = 40 for each dark-light-dark-dark-dark-light-dark pattern of a row or
    // a column with 4 light modules before or after it, the light quiet zone
    // counting; and N4 = 10 for each 5% that the dark modules are away from
    // half the modules.
    static int getLostPoint(boolean[][] modules) {
        int moduleCount = modules.length;
        int lostPoint = 0;

        for (int pass = 0; pass < 2; pass++) {
            boolean across = (pass == 1);
            for (int i = 0; i < moduleCount; i++) {
                // N1
                int run = 1;
                for (int j = 1; j <= moduleCount; j++) {
                    if (j < moduleCount && dark(modules, across, i, j) == dark(modules, across, i, j - 1)) {
                        run++;
                        continue;
                    }
                    if (run >= 5) {
                        lostPoint += 3 + run - 5;
                    }
                    run = 1;
                }
                // N3
                for (int j = 0; j + 6 < moduleCount; j++) {
                    if (dark(modules, across, i, j)
                            && !dark(modules, across, i, j + 1)
                            &&  dark(modules, across, i, j + 2)
                            &&  dark(modules, across, i, j + 3)
                            &&  dark(modules, across, i, j + 4)
                            && !dark(modules, across, i, j + 5)
                            &&  dark(modules, across, i, j + 6)
                            && (isLight(modules, across, i, j - 4, j)
                                    || isLight(modules, across, i, j + 7, j + 11))) {
                        lostPoint += 40;
                    }
                }
            }
        }

        // N2
        for (int row = 0; row < moduleCount - 1; row++) {
            for (int col = 0; col < moduleCount - 1; col++) {
                boolean d = modules[row][col];
                if (d == modules[row + 1][col] && d == modules[row][col + 1] && d == modules[row + 1][col + 1]) {
                    lostPoint += 3;
                }
            }
        }

        // N4
        int darkCount = 0;
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount; col++) {
                if (modules[row][col]) {
                    darkCount++;
                }
            }
        }
        int total = moduleCount * moduleCount;
        lostPoint += Math.abs(2 * darkCount - total) * 10 / total * 10;

        return lostPoint;
    }

    // Returns the module of the row and the column, or of the column and the
    // row across; outside the symbol is the quiet zone, which is light.
    private static boolean dark(boolean[][] modules, boolean across, int i, int j) {
        if (j < 0 || j >= modules.length) {
            return false;
        }
        return across ? modules[j][i] : modules[i][j];
    }

    // Tells if the modules from j to k, not including k, of the row or the column are light.
    private static boolean isLight(boolean[][] modules, boolean across, int i, int j, int k) {
        for (; j < k; j++) {
            if (dark(modules, across, i, j)) {
                return false;
            }
        }
        return true;
    }

    private static final int G15 = (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0);

    private static final int G15_MASK = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1);

    public static int getBCHTypeInfo(int data) {
        int d = data << 10;
        while (getBCHDigit(d) - getBCHDigit(G15) >= 0) {
            d ^= (G15 << (getBCHDigit(d) - getBCHDigit(G15)));
        }
        return ((data << 10) | d) ^ G15_MASK;
    }

    private static final int G18 = (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0);

    public static int getBCHTypeNumber(int data) {
        int d = data << 12;
        while (getBCHDigit(d) - getBCHDigit(G18) >= 0) {
            d ^= (G18 << (getBCHDigit(d) - getBCHDigit(G18)));
        }
        return (data << 12) | d;
    }

    private static int getBCHDigit(int data) {
        int digit = 0;
        while (data != 0) {
            digit++;
            data >>>= 1;
        }
        return digit;
    }

}
