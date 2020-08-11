/**
 * NumberFormat.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
