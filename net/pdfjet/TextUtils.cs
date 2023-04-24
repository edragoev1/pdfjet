/**
 *  TextUtils.cs
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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PDFjet.NET {
public class TextUtils {
    public static String[] SplitTextIntoTokens(
            String text,
            Font font,
            Font fallbackFont,
            float width) {
        List<String> tokens2 = new List<String>();
        String[] tokens = Regex.Split(text, @"\s+");
        foreach (String token in tokens) {
            if (font.StringWidth(fallbackFont, token) <= width) {
                tokens2.Add(token);
            } else {
                StringBuilder buf = new StringBuilder();
                for (int i = 0; i < token.Length; i++) {
                    String ch = token[i].ToString();
                    if (font.StringWidth(fallbackFont, buf.ToString() + ch) <= width) {
                        buf.Append(ch);
                    } else {
                        tokens2.Add(buf.ToString());
                        buf.Length = 0;
                        buf.Append(ch);
                    }
                }
                String str = buf.ToString();
                if (!str.Equals("")) {
                    tokens2.Add(str);
                }
            }
        }
        return tokens2.ToArray();
    }

    public static void PrintDuration(String example, long time0, long time1) {
        String duration = String.Format("{0:N2}", time1 - time0);
        if (duration.Length == 4) {
            duration = "   " + duration;
        } else if (duration.Length == 5) {
            duration = " " + duration;
        }
        Console.WriteLine(example + " => " + duration);
    }
}   // End of TextUtils.cs
}   // End of namespace PDFjet.NET
