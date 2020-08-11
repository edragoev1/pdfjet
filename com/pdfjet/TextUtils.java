/**
 *  TextUtils.java
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
package com.pdfjet;

import java.util.*;


public class TextUtils {

    public static String[] splitTextIntoTokens(
            String text,
            Font font,
            Font fallbackFont,
            float width) {
        List<String> tokens2 = new ArrayList<String>();

        String[] tokens = text.split("\\s+");
        for (String token : tokens) {
            if (font.stringWidth(fallbackFont, token) <= width) {
                tokens2.add(token);
            }
            else {
                StringBuilder buf = new StringBuilder();
                for (int i = 0; i < token.length(); i++) {
                    String ch = String.valueOf(token.charAt(i));
                    if (font.stringWidth(fallbackFont, buf.toString() + ch) <= width) {
                        buf.append(ch);
                    }
                    else {
                        tokens2.add(buf.toString());
                        buf.setLength(0);
                        buf.append(ch);
                    }
                }
                String str = buf.toString();
                if (!str.equals("")) {
                    tokens2.add(str);
                }
            }
        }

        return tokens2.toArray(new String[] {});
    }

}
