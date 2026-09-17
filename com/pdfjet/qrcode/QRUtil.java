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

    protected static int getLostPoint(QRCode qrCode) {
        int moduleCount = qrCode.getModuleCount();
        int lostPoint = 0;

        // LEVEL1
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount; col++) {
                int sameCount = 0;
                boolean dark = qrCode.isDark(row, col);
                for (int r = -1; r <= 1; r++) {
                    if (row + r < 0 || moduleCount <= row + r) {
                        continue;
                    }
                    for (int c = -1; c <= 1; c++) {
                        if (col + c < 0 || moduleCount <= col + c) {
                            continue;
                        }
                        if (r == 0 && c == 0) {
                            continue;
                        }
                        if (dark == qrCode.isDark(row + r, col + c)) {
                            sameCount++;
                        }
                    }
                }
                if (sameCount > 5) {
                    lostPoint += (3 + sameCount - 5);
                }
            }
        }

        // LEVEL2
        for (int row = 0; row < moduleCount - 1; row++) {
            for (int col = 0; col < moduleCount - 1; col++) {
                int count = 0;
                if (qrCode.isDark(row,     col    )) count++;
                if (qrCode.isDark(row + 1, col    )) count++;
                if (qrCode.isDark(row,     col + 1)) count++;
                if (qrCode.isDark(row + 1, col + 1)) count++;
                if (count == 0 || count == 4) {
                    lostPoint += 3;
                }
            }
        }

        // LEVEL3
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount - 6; col++) {
                if (qrCode.isDark(row, col)
                        && !qrCode.isDark(row, col + 1)
                        &&  qrCode.isDark(row, col + 2)
                        &&  qrCode.isDark(row, col + 3)
                        &&  qrCode.isDark(row, col + 4)
                        && !qrCode.isDark(row, col + 5)
                        &&  qrCode.isDark(row, col + 6)) {
                    lostPoint += 40;
                }
            }
        }

        for (int col = 0; col < moduleCount; col++) {
            for (int row = 0; row < moduleCount - 6; row++) {
                if (qrCode.isDark(row, col)
                        && !qrCode.isDark(row + 1, col)
                        &&  qrCode.isDark(row + 2, col)
                        &&  qrCode.isDark(row + 3, col)
                        &&  qrCode.isDark(row + 4, col)
                        && !qrCode.isDark(row + 5, col)
                        &&  qrCode.isDark(row + 6, col)) {
                    lostPoint += 40;
                }
            }
        }

        // LEVEL4
        int darkCount = 0;
        for (int col = 0; col < moduleCount; col++) {
            for (int row = 0; row < moduleCount; row++) {
                if (qrCode.isDark(row, col)) {
                    darkCount++;
                }
            }
        }

        int ratio = Math.abs(100 * darkCount / moduleCount / moduleCount - 50) / 5;
        lostPoint += ratio * 10;

        return lostPoint;
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
