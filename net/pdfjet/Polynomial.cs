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

/**
 * Polynomial
 * @author Kazuhiko Arase
 */
namespace PDFjet.NET {
public class Polynomial {
    private int[] num;

    public Polynomial(int[] num) : this(num, 0) {
    }

    public Polynomial(int[] num, int shift) {
        int offset = 0;
        while (offset < num.Length && num[offset] == 0) {
            offset++;
        }
        this.num = new int[num.Length - offset + shift];
        Array.Copy(num, offset, this.num, 0, num.Length - offset);
    }

    public int Get(int index) {
        return num[index];
    }

    public int GetLength() {
        return num.Length;
    }

    public Polynomial Multiply(Polynomial e) {
        int[] num = new int[GetLength() + e.GetLength() - 1];
        for (int i = 0; i < GetLength(); i++) {
            for (int j = 0; j < e.GetLength(); j++) {
                num[i + j] ^= QRMath.Gexp(QRMath.Glog(Get(i)) + QRMath.Glog(e.Get(j)));
            }
        }

        return new Polynomial(num);
    }

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
