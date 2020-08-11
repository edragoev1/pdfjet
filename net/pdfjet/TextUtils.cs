/**
 *  TextUtils.cs
 *
 Copyright 2020 Innovatics Inc.

 Redistribution and use in source and binary forms, with or without modification,
 are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice,
 this list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice,
 this list of conditions and the following disclaimer in the documentation
 and / or other materials provided with the distribution.

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
            }
            else {
                StringBuilder buf = new StringBuilder();
                for (int i = 0; i < token.Length; i++) {
                    String ch = token[i].ToString();
                    if (font.StringWidth(fallbackFont, buf.ToString() + ch) <= width) {
                        buf.Append(ch);
                    }
                    else {
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

}   // End of TextUtils.cs
}   // End of namespace PDFjet.NET
