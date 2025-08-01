/**
 *  TextUtils.cs
 *
©2025 PDFjet Software

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
public class TextUtils {
    public static void PrintDuration(String example, long time0, long time1) {
        String duration = String.Format("{0:N1}", (time1 - time0)/1.0).Replace(",", "");
        if (duration.Length == 3) {
            duration = "    " + duration;
        } else if (duration.Length == 4) {
            duration = "   " + duration;
        } else if (duration.Length == 5) {
            duration = "  " + duration;
        } else if (duration.Length == 6) {
            duration = " " + duration;
        }
        Console.WriteLine(example + " => " + duration);
    }
}   // End of TextUtils.cs
}   // End of namespace PDFjet.NET
