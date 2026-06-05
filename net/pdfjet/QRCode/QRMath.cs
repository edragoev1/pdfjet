/**
 * QRMath.cs
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

namespace PDFjet.NET {
public class QRMath {
    private static readonly int[] EXP_TABLE = new int[256];
    private static readonly int[] LOG_TABLE = new int[256];

    static QRMath() {
        for (int i = 0; i < 8; i++) {
            EXP_TABLE[i] = 1 << i;
        }

        for (int i = 8; i < 256; i++) {
            EXP_TABLE[i] = EXP_TABLE[i - 4]
                ^ EXP_TABLE[i - 5]
                ^ EXP_TABLE[i - 6]
                ^ EXP_TABLE[i - 8];
        }

        for (int i = 0; i < 255; i++) {
            LOG_TABLE[EXP_TABLE[i] ] = i;
        }
    }

    public static int Glog(int n) {
        if (n < 1) {
            throw new ArithmeticException("log(" + n + ")");
        }
        return LOG_TABLE[n];
    }

    public static int Gexp(int n) {
        while (n < 0) {
            n += 255;
        }
        while (n >= 256) {
            n -= 255;
        }
        return EXP_TABLE[n];
    }
}
}   // End of namespace PDFjet.NET
