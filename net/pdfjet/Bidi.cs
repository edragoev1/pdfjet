/**
 *  Bidi.cs
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
using System.Text;

namespace PDFjet.NET {
/**
 *  Provides BIDI processing for Arabic and Hebrew.
 *
 *  Please see Example_27.
 */
public class Bidi {
/*
 * General,Isolated,End,Middle,Beginning
 */
private static char[] forms = new char[] {
'\u0623','\uFE83','\uFE84','\u0623','\u0623',
'\u0628','\uFE8F','\uFE90','\uFE92','\uFE91',
'\u062A','\uFE95','\uFE96','\uFE98','\uFE97',
'\u062B','\uFE99','\uFE9A','\uFE9C','\uFE9B',
'\u062C','\uFE9D','\uFE9E','\uFEA0','\uFE9F',
'\u062D','\uFEA1','\uFEA2','\uFEA4','\uFEA3',
'\u062E','\uFEA5','\uFEA6','\uFEA8','\uFEA7',
'\u062F','\uFEA9','\uFEAA','\u062F','\u062F',
'\u0630','\uFEAB','\uFEAC','\u0630','\u0630',
'\u0631','\uFEAD','\uFEAE','\u0631','\u0631',
'\u0632','\uFEAF','\uFEB0','\u0632','\u0632',
'\u0633','\uFEB1','\uFEB2','\uFEB4','\uFEB3',
'\u0634','\uFEB5','\uFEB6','\uFEB8','\uFEB7',
'\u0635','\uFEB9','\uFEBA','\uFEBC','\uFEBB',
'\u0636','\uFEBD','\uFEBE','\uFEC0','\uFEBF',
'\u0637','\uFEC1','\uFEC2','\uFEC4','\uFEC3',
'\u0638','\uFEC5','\uFEC6','\uFEC8','\uFEC7',
'\u0639','\uFEC9','\uFECA','\uFECC','\uFECB',
'\u063A','\uFECD','\uFECE','\uFED0','\uFECF',
'\u0641','\uFED1','\uFED2','\uFED4','\uFED3',
'\u0642','\uFED5','\uFED6','\uFED8','\uFED7',
'\u0643','\uFED9','\uFEDA','\uFEDC','\uFEDB',
'\u0644','\uFEDD','\uFEDE','\uFEE0','\uFEDF',
'\u0645','\uFEE1','\uFEE2','\uFEE4','\uFEE3',
'\u0646','\uFEE5','\uFEE6','\uFEE8','\uFEE7',
'\u0647','\uFEE9','\uFEEA','\uFEEC','\uFEEB',
'\u0648','\uFEED','\uFEEE','\u0648','\u0648',
'\u064A','\uFEF1','\uFEF2','\uFEF4','\uFEF3',
'\u0622','\uFE81','\uFE82','\u0622','\u0622',
'\u0629','\uFE93','\uFE94','\u0629','\u0629',
'\u0649','\uFEEF','\uFEF0','\u0649','\u0649',
};

    private static bool IsArabicLetter(char ch) {
        for (int i = 0; i < forms.Length; i += 5) {
            if (ch == forms[i]) {
                return true;
            }
        }
        return false;
    }

    /**
     *  Reorders the string so that Arabic and Hebrew text flows from right
     *  to left while numbers and Latin text flows from left to right.
     *
     *  @param str the input string.
     *  @return the reordered string.
     */
    public static String ReorderVisually(String str) {
        StringBuilder buf1 = new StringBuilder();
        StringBuilder buf2 = new StringBuilder();
        bool rightToLeft = true;
        for (int i = 0; i < str.Length; i++) {
            char ch = str[i];
            if (ch == '\u200E') {
                // LRM  U+200E  LEFT-TO-RIGHT MARK  Left-to-right zero-width character
                rightToLeft = false;
                continue;
            }
            if (ch == '\u200F' || ch == '\u061C') {
                // RLM  U+200F  RIGHT-TO-LEFT MARK  Right-to-left zero-width non-Arabic character
                // ALM  U+061C  ARABIC LETTER MARK  Right-to-left zero-width Arabic character
                rightToLeft = true;
                continue;
            }
            if (IsArabic(ch) ||
                    IsHebrew(ch) ||
                    ch == '«' || ch == '»' ||
                    ch == '(' || ch == ')' ||
                    ch == '[' || ch == ']') {
                rightToLeft = true;
                if (buf2.Length > 0) {
                    buf1.Append(Process(buf2.ToString()));
                    buf2.Length = 0;
                }
                if (ch == '«') {
                    buf1.Append('»');
                } else if (ch == '»') {
                    buf1.Append('«');
                } else if (ch == '(') {
                    buf1.Append(')');
                } else if (ch == ')') {
                    buf1.Append('(');
                } else if (ch == '[') {
                    buf1.Append(']');
                } else if (ch == ']') {
                    buf1.Append('[');
                } else {
                    buf1.Append(ch);
                }
            } else if (IsAlphaNumeric(ch)) {
                rightToLeft = false;
                buf2.Append(ch);
            } else {
                if (rightToLeft) {
                    buf1.Append(ch);
                } else {
                    buf2.Append(ch);
                }
            }
        }
        if (buf2.Length > 0) {
            buf1.Append(Process(buf2.ToString()));
        }
        StringBuilder buf3 = new StringBuilder();
        for (int i = (buf1.Length - 1); i >= 0; i--) {
            char ch = buf1[i];
            if (IsArabicLetter(ch)) {
                char prevCh = (i > 0) ? buf1[i - 1] : '\u0000';
                char nextCh = (i < (buf1.Length - 1)) ? buf1[i + 1] : '\u0000';
                for (int j = 0; j < forms.Length; j += 5) {
                    if (ch == forms[j]) {
                        if (!IsArabicLetter(prevCh) && !IsArabicLetter(nextCh)) {
                            buf3.Append(forms[j + 1]);  // Isolated
                        } else if (IsArabicLetter(prevCh) && !IsArabicLetter(nextCh)) {
                            buf3.Append(forms[j + 2]);  // End
                        } else if (IsArabicLetter(prevCh) && IsArabicLetter(nextCh)) {
                            buf3.Append(forms[j + 3]);  // Middle
                        } else if (!IsArabicLetter(prevCh) && IsArabicLetter(nextCh)) {
                            buf3.Append(forms[j + 4]);  // Beginning
                        }
                    }
                }
            } else {
                buf3.Append(ch);
            }
        }
        return buf3.ToString();
    }

    private static bool IsArabic(char ch) {
        return (ch >= 0x600 && ch <= 0x6FF);
    }

    private static bool IsHebrew(char ch) {
        return (ch >= 0x0591 && ch <= 0x05F4);
    }

    private static bool IsAlphaNumeric(char ch) {
        if (ch >= '0' && ch <= '9') {
            return true;
        }
        if (ch >= 'a' && ch <= 'z') {
            return true;
        }
        if (ch >= 'A' && ch <= 'Z') {
            return true;
        }
        return false;
    }

    private static String Process(String str) {
        StringBuilder buf1 = new StringBuilder(Reverse(str));
        StringBuilder buf2 = new StringBuilder();
        StringBuilder buf3 = new StringBuilder();
        for (int i = 0; i < buf1.Length; i++) {
            char ch = buf1[i];
            if (ch == ' ' || ch == ',' || ch == '.' || ch == '-') {
                buf2.Append(ch);
                continue;
            }
            buf3.Append(buf1.ToString().Substring(i));
            buf3.Append(Reverse(buf2.ToString()));
            break;
        }
        return buf3.ToString();
    }

    public static String Reverse(String str) {
        StringBuilder buf = new StringBuilder();
        char[] charArray = str.ToCharArray();
        for (int i = (charArray.Length - 1); i >= 0; i--) {
            buf.Append(charArray[i]);
        }
        return buf.ToString();
    }
}
}   // End of namespace PDFjet.NET
