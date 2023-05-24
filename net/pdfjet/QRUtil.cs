/**
 *
Copyright 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/
using System;

namespace PDFjet.NET {
public class QRUtil {
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
            throw new ArgumentException("mask: " + maskPattern);
        }
    }

    internal static int GetLostPoint(QRCode qrCode) {
        int moduleCount = qrCode.GetModuleCount();
        int lostPoint = 0;

        // LEVEL1
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount; col++) {
                int sameCount = 0;
                bool dark = qrCode.IsDark(row, col);
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
                        if (dark == qrCode.IsDark(row + r, col + c)) {
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
                if (qrCode.IsDark(row,     col    )) count++;
                if (qrCode.IsDark(row + 1, col    )) count++;
                if (qrCode.IsDark(row,     col + 1)) count++;
                if (qrCode.IsDark(row + 1, col + 1)) count++;
                if (count == 0 || count == 4) {
                    lostPoint += 3;
                }
            }
        }

        // LEVEL3
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount - 6; col++) {
                if (qrCode.IsDark(row, col)
                        && !qrCode.IsDark(row, col + 1)
                        &&  qrCode.IsDark(row, col + 2)
                        &&  qrCode.IsDark(row, col + 3)
                        &&  qrCode.IsDark(row, col + 4)
                        && !qrCode.IsDark(row, col + 5)
                        &&  qrCode.IsDark(row, col + 6)) {
                    lostPoint += 40;
                }
            }
        }

        for (int col = 0; col < moduleCount; col++) {
            for (int row = 0; row < moduleCount - 6; row++) {
                if (qrCode.IsDark(row, col)
                        && !qrCode.IsDark(row + 1, col)
                        &&  qrCode.IsDark(row + 2, col)
                        &&  qrCode.IsDark(row + 3, col)
                        &&  qrCode.IsDark(row + 4, col)
                        && !qrCode.IsDark(row + 5, col)
                        &&  qrCode.IsDark(row + 6, col)) {
                    lostPoint += 40;
                }
            }
        }

        // LEVEL4
        int darkCount = 0;
        for (int col = 0; col < moduleCount; col++) {
            for (int row = 0; row < moduleCount; row++) {
                if (qrCode.IsDark(row, col)) {
                    darkCount++;
                }
            }
        }

        int ratio = Math.Abs(100 * darkCount / moduleCount / moduleCount - 50) / 5;
        lostPoint += ratio * 10;

        return lostPoint;
    }

    private const int G15 = (1 << 10) | (1 << 8) | (1 << 5) | (1 << 4) | (1 << 2) | (1 << 1) | (1 << 0);

    private const int G15_MASK = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1);

    public static int GetBCHTypeInfo(int data) {
        int d = data << 10;
        while (GetBCHDigit(d) - GetBCHDigit(G15) >= 0) {
            d ^= (G15 << (GetBCHDigit(d) - GetBCHDigit(G15)));
        }
        return ((data << 10) | d) ^ G15_MASK;
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
