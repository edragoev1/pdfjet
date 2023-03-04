/**
 * NumberFormat.cs
 *
Copyright 2023 Innovatics Inc.

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

namespace PDFjet.NET {
public class NumberFormat {

    int minFractionDigits = 0;
    int maxFractionDigits = 0;


    public static NumberFormat GetInstance() {
        return new NumberFormat();
    }


    public void SetMinimumFractionDigits(int minFractionDigits) {
        this.minFractionDigits = minFractionDigits;
    }


    public void SetMaximumFractionDigits(int maxFractionDigits) {
        this.maxFractionDigits = maxFractionDigits;
    }


    public String Format(double value) {
        String format = "0.";
        for (int i = 0; i < maxFractionDigits; i++) {
            format += "0";
        }
        return value.ToString(format);
    }

}   // End of NumberFormat.cs
}   // End of package PDFjet.NET
