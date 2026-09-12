/*
 * Polynomial.cs
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
/// <summary>A polynomial over GF(256), used for the Reed-Solomon error correction.</summary>
internal class Polynomial {
    private int[] num;

    /// <summary>Creates a polynomial from its coefficients.</summary>
    public Polynomial(int[] num) : this(num, 0) {
    }

    /// <summary>Creates a polynomial from its coefficients, without leading zeros, multiplied by x to the power of shift.</summary>
    public Polynomial(int[] num, int shift) {
        int offset = 0;
        while (offset < num.Length && num[offset] == 0) {
            offset++;
        }
        this.num = new int[num.Length - offset + shift];
        Array.Copy(num, offset, this.num, 0, num.Length - offset);
    }

    /// <summary>Returns the coefficient at the specified index.</summary>
    public int Get(int index) {
        return num[index];
    }

    /// <summary>Returns the number of coefficients.</summary>
    public int GetLength() {
        return num.Length;
    }

    /// <summary>Returns the product of this polynomial and e.</summary>
    public Polynomial Multiply(Polynomial e) {
        int[] num = new int[GetLength() + e.GetLength() - 1];
        for (int i = 0; i < GetLength(); i++) {
            for (int j = 0; j < e.GetLength(); j++) {
                num[i + j] ^= QRMath.Gexp(QRMath.Glog(Get(i)) + QRMath.Glog(e.Get(j)));
            }
        }

        return new Polynomial(num);
    }

    /// <summary>Returns the remainder of dividing this polynomial by e.</summary>
    public Polynomial Mod(Polynomial e) {
        if (GetLength() - e.GetLength() < 0) {
            return this;
        }

        int ratio = QRMath.Glog(Get(0)) - QRMath.Glog(e.Get(0));
        int[] num = new int[GetLength()];
        for (int i = 0; i < GetLength(); i++) {
            num[i] = Get(i);
        }

        for (int i = 0; i < e.GetLength(); i++) {
            num[i] ^= QRMath.Gexp(QRMath.Glog(e.Get(i)) + ratio);
        }

        return new Polynomial(num).Mod(e);
    }
}
}   // End of namespace PDFjet.NET
