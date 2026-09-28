/*
 * QRUtil.cs
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
using System;
using System.Globalization;

namespace PDFjet.NET {
/// <summary>Helper methods for building QR codes.</summary>
internal class QRUtil {
    // The centers of the alignment patterns of each version, in rows and columns.
    private static readonly int[][] PATTERN_POSITION_TABLE = new int[][] {
        new int[] {},
        new int[] {6, 18},
        new int[] {6, 22},
        new int[] {6, 26},
        new int[] {6, 30},
        new int[] {6, 34},
        new int[] {6, 22, 38},
        new int[] {6, 24, 42},
        new int[] {6, 26, 46},
        new int[] {6, 28, 50},
        new int[] {6, 30, 54},
        new int[] {6, 32, 58},
        new int[] {6, 34, 62},
        new int[] {6, 26, 46, 66},
        new int[] {6, 26, 48, 70},
        new int[] {6, 26, 50, 74},
        new int[] {6, 30, 54, 78},
        new int[] {6, 30, 56, 82},
        new int[] {6, 30, 58, 86},
        new int[] {6, 34, 62, 90},
        new int[] {6, 28, 50, 72, 94},
        new int[] {6, 26, 50, 74, 98},
        new int[] {6, 30, 54, 78, 102},
        new int[] {6, 28, 54, 80, 106},
        new int[] {6, 32, 58, 84, 110},
        new int[] {6, 30, 58, 86, 114},
        new int[] {6, 34, 62, 90, 118},
        new int[] {6, 26, 50, 74, 98, 122},
        new int[] {6, 30, 54, 78, 102, 126},
        new int[] {6, 26, 52, 78, 104, 130},
        new int[] {6, 30, 56, 82, 108, 134},
        new int[] {6, 34, 60, 86, 112, 138},
        new int[] {6, 30, 58, 86, 114, 142},
        new int[] {6, 34, 62, 90, 118, 146},
        new int[] {6, 30, 54, 78, 102, 126, 150},
        new int[] {6, 24, 50, 76, 102, 128, 154},
        new int[] {6, 28, 54, 80, 106, 132, 158},
        new int[] {6, 32, 58, 84, 110, 136, 162},
        new int[] {6, 26, 54, 82, 110, 138, 166},
        new int[] {6, 30, 58, 86, 114, 142, 170},
    };

    internal static int[] GetPatternPosition(int typeNumber) {
        return PATTERN_POSITION_TABLE[typeNumber - 1];
    }

    internal static Polynomial GetErrorCorrectPolynomial(int errorCorrectLength) {
        Polynomial a = new Polynomial(new int[] {1});
        for (int i = 0; i < errorCorrectLength; i++) {
            a = a.Multiply(new Polynomial(new int[] { 1, QRMath.Gexp(i) }));
        }
        return a;
    }

    internal static bool GetMask(int maskPattern, int i, int j) {
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
            throw new ArgumentException("mask: " + maskPattern.ToString(CultureInfo.InvariantCulture));
        }
    }

    // Returns the penalty of the modules with the rules of ISO/IEC 18004
    // 7.8.3.1: N1 = 3 for five modules of a color in a row or a column, and 1
    // for each one more; N2 = 3 for each block of 2 by 2 modules of a color; N3
    // = 40 for each dark-light-dark-dark-dark-light-dark pattern of a row or a
    // column with 4 light modules before or after it, the light quiet zone
    // counting; and N4 = 10 for each 5% that the dark modules are away from half
    // the modules.
    internal static int GetLostPoint(bool[][] modules) {
        int moduleCount = modules.Length;
        int lostPoint = 0;

        for (int a = 0; a < 2; a++) {
            bool across = (a == 1);
            for (int i = 0; i < moduleCount; i++) {
                // N1
                int run = 1;
                for (int j = 1; j <= moduleCount; j++) {
                    if (j < moduleCount && IsDark(modules, across, i, j) == IsDark(modules, across, i, j - 1)) {
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
                    if (IsDark(modules, across, i, j)
                            && !IsDark(modules, across, i, j + 1)
                            &&  IsDark(modules, across, i, j + 2)
                            &&  IsDark(modules, across, i, j + 3)
                            &&  IsDark(modules, across, i, j + 4)
                            && !IsDark(modules, across, i, j + 5)
                            &&  IsDark(modules, across, i, j + 6)
                            && (IsLight(modules, across, i, j - 4, j) || IsLight(modules, across, i, j + 7, j + 11))) {
                        lostPoint += 40;
                    }
                }
            }
        }

        // N2
        for (int row = 0; row < moduleCount - 1; row++) {
            for (int col = 0; col < moduleCount - 1; col++) {
                bool d = modules[row][col];
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
        lostPoint += Math.Abs(2 * darkCount - total) * 10 / total * 10;

        return lostPoint;
    }

    // Returns the module of the row and the column, or of the column and the
    // row across; outside the symbol is the quiet zone, which is light.
    private static bool IsDark(bool[][] modules, bool across, int i, int j) {
        if (j < 0 || j >= modules.Length) {
            return false;
        }
        return across ? modules[j][i] : modules[i][j];
    }

    // Tells if the modules from j to k, not including k, of the row or the
    // column are light.
    private static bool IsLight(bool[][] modules, bool across, int i, int j, int k) {
        for (; j < k; j++) {
            if (IsDark(modules, across, i, j)) {
                return false;
            }
        }
        return true;
    }

    private const int G15 = (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0);

    private const int G15_MASK = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1);

    /// <summary>Returns the BCH code of the format information bits.</summary>
    public static int GetBCHTypeInfo(int data) {
        int d = data << 10;
        while (GetBCHDigit(d) - GetBCHDigit(G15) >= 0) {
            d ^= (G15 << (GetBCHDigit(d) - GetBCHDigit(G15)));
        }
        return ((data << 10) | d) ^ G15_MASK;
    }

    private const int G18 = (1 << 12) | (1 << 11) | (1 << 10) | (1 << 9) | (1 << 8) | (1 << 5) | (1 << 2) | (1 << 0);

    public static int GetBCHTypeNumber(int data) {
        int d = data << 12;
        while (GetBCHDigit(d) - GetBCHDigit(G18) >= 0) {
            d ^= (G18 << (GetBCHDigit(d) - GetBCHDigit(G18)));
        }
        return (data << 12) | d;
    }

    private static int GetBCHDigit(int data) {
        int digit = 0;
        while (data != 0) {
            digit++;
            data = (int) (((uint) data) >> 1);
        }
        return digit;
    }
}
}   // End of namespace PDFjet.NET
