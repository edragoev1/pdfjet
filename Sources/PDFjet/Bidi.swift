/**
 * Bidi.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Provides BIDI processing for Arabic and Hebrew.
 *
 * Please see Example_27.
 */
public class Bidi {
/*
 * General,Isolated,End,Middle,Beginning
 */
private static let forms: [Character] = [
"\u{0623}","\u{FE83}","\u{FE84}","\u{0623}","\u{0623}",
"\u{0628}","\u{FE8F}","\u{FE90}","\u{FE92}","\u{FE91}",
"\u{062A}","\u{FE95}","\u{FE96}","\u{FE98}","\u{FE97}",
"\u{062B}","\u{FE99}","\u{FE9A}","\u{FE9C}","\u{FE9B}",
"\u{062C}","\u{FE9D}","\u{FE9E}","\u{FEA0}","\u{FE9F}",
"\u{062D}","\u{FEA1}","\u{FEA2}","\u{FEA4}","\u{FEA3}",
"\u{062E}","\u{FEA5}","\u{FEA6}","\u{FEA8}","\u{FEA7}",
"\u{062F}","\u{FEA9}","\u{FEAA}","\u{062F}","\u{062F}",
"\u{0630}","\u{FEAB}","\u{FEAC}","\u{0630}","\u{0630}",
"\u{0631}","\u{FEAD}","\u{FEAE}","\u{0631}","\u{0631}",
"\u{0632}","\u{FEAF}","\u{FEB0}","\u{0632}","\u{0632}",
"\u{0633}","\u{FEB1}","\u{FEB2}","\u{FEB4}","\u{FEB3}",
"\u{0634}","\u{FEB5}","\u{FEB6}","\u{FEB8}","\u{FEB7}",
"\u{0635}","\u{FEB9}","\u{FEBA}","\u{FEBC}","\u{FEBB}",
"\u{0636}","\u{FEBD}","\u{FEBE}","\u{FEC0}","\u{FEBF}",
"\u{0637}","\u{FEC1}","\u{FEC2}","\u{FEC4}","\u{FEC3}",
"\u{0638}","\u{FEC5}","\u{FEC6}","\u{FEC8}","\u{FEC7}",
"\u{0639}","\u{FEC9}","\u{FECA}","\u{FECC}","\u{FECB}",
"\u{063A}","\u{FECD}","\u{FECE}","\u{FED0}","\u{FECF}",
"\u{0641}","\u{FED1}","\u{FED2}","\u{FED4}","\u{FED3}",
"\u{0642}","\u{FED5}","\u{FED6}","\u{FED8}","\u{FED7}",
"\u{0643}","\u{FED9}","\u{FEDA}","\u{FEDC}","\u{FEDB}",
"\u{0644}","\u{FEDD}","\u{FEDE}","\u{FEE0}","\u{FEDF}",
"\u{0645}","\u{FEE1}","\u{FEE2}","\u{FEE4}","\u{FEE3}",
"\u{0646}","\u{FEE5}","\u{FEE6}","\u{FEE8}","\u{FEE7}",
"\u{0647}","\u{FEE9}","\u{FEEA}","\u{FEEC}","\u{FEEB}",
"\u{0648}","\u{FEED}","\u{FEEE}","\u{0648}","\u{0648}",
"\u{064A}","\u{FEF1}","\u{FEF2}","\u{FEF4}","\u{FEF3}",
"\u{0622}","\u{FE81}","\u{FE82}","\u{0622}","\u{0622}",
"\u{0629}","\u{FE93}","\u{FE94}","\u{0629}","\u{0629}",
"\u{0649}","\u{FEEF}","\u{FEF0}","\u{0649}","\u{0649}",
]

    private static func isArabicLetter(_ ch: Character) -> Bool {
        for i in stride(from: 0, to: forms.count, by: 5) {
            if ch == forms[i] {
                return true
            }
        }
        return false
    }

    /**
     * Reorders the string so that Arabic and Hebrew text flows from right
     * to left while numbers and Latin text flows from left to right.
     *
     * @param str the input string.
     * @return the reordered string.
     */
    public static func reorderVisually(_ str: String) -> String {
        var buf1 = String()
        var buf2 = String()
        var rightToLeft: Bool = true
        for i in 0..<str.count {
            let ch = str[str.index(str.startIndex, offsetBy: i)]
            if ch == "\u{200E}" {
                // LRM  U+200E  LEFT-TO-RIGHT MARK  Left-to-right zero-width character
                rightToLeft = false
                continue
            }
            if ch == "\u{200F}" || ch == "\u{061C}" {
                // RLM  U+200F  RIGHT-TO-LEFT MARK  Right-to-left zero-width non-Arabic character
                // ALM  U+061C  ARABIC LETTER MARK  Right-to-left zero-width Arabic character
                rightToLeft = true
                continue
            }
            if isArabic(ch) ||
                    isHebrew(ch) ||
                    ch == "«" || ch == "»" ||
                    ch == "(" || ch == ")" ||
                    ch == "[" || ch == "]" {
                rightToLeft = true
                if buf2.count > 0 {
                    buf1.append(process(buf2))
                    buf2 = ""
                }
                if ch == "«" {
                    buf1.append("»")
                } else if ch == "»" {
                    buf1.append("«")
                } else if ch == "(" {
                    buf1.append(")")
                } else if ch == ")" {
                    buf1.append("(")
                } else if ch == "[" {
                    buf1.append("]")
                } else if ch == "]" {
                    buf1.append("[")
                } else {
                    buf1.append(ch)
                }
            } else if isAlphaNumeric(ch) {
                rightToLeft = false
                buf2.append(ch)
            } else {
                if rightToLeft {
                    buf1.append(ch)
                } else {
                    buf2.append(ch)
                }
            }
        }
        if buf2.count > 0 {
            buf1.append(process(buf2))
        }
        var buf3 = String()
        var i: Int = buf1.count - 1
        while i >= 0 {
            let ch = buf1[buf1.index(buf1.startIndex, offsetBy: i)]
            if isArabicLetter(ch) {
                let prevCh = (i > 0) ? buf1[buf1.index(buf1.startIndex, offsetBy: i - 1)] : "\u{0000}"
                let nextCh = (i < (buf1.count - 1)) ? buf1[buf1.index(buf1.startIndex, offsetBy: i + 1)] : "\u{0000}"
                for j in stride(from: 0, to: forms.count, by: 5) {
                    if ch == forms[j] {
//                         if (!isArabicLetter(prevCh) && !isArabicLetter(nextCh)) {
//                             buf3.append(forms[j + 1])   // Isolated
//                         } else if (isArabicLetter(prevCh) && !isArabicLetter(nextCh)) {
//                             buf3.append(forms[j + 2])   // End
//                         } else if (isArabicLetter(prevCh) && isArabicLetter(nextCh)) {
//                             buf3.append(forms[j + 3])   // Middle
//                         } else if (!isArabicLetter(prevCh) && isArabicLetter(nextCh)) {
//                             buf3.append(forms[j + 4])   // Beginning
//                         }

                        // prevCh is the character before this one in logical order (i-1)
                        // nextCh is the character after this one in logical order (i+1)
                        let prevJoins = joinsForward(prevCh)   // prev joins forward INTO this letter
                        let nextJoins = joinsBackward(nextCh)  // next joins backward INTO this letter

                        if (!prevJoins && !nextJoins) {
                            buf3.append(forms[j + 1])   // Isolated
                        } else if (prevJoins && !nextJoins) {
                            buf3.append(forms[j + 2])   // End (Final)
                        } else if (prevJoins && nextJoins) {
                            buf3.append(forms[j + 3])   // Middle (Medial)
                        } else if (!prevJoins && nextJoins) {
                            buf3.append(forms[j + 4])   // Beginning (Initial)
                        }
                    }
                }
            } else {
                buf3.append(ch)
            }
            i -= 1
        }
        return buf3
    }

    public static func isArabic(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        return (scalar >= "\u{0600}" && scalar <= "\u{06FF}")
    }

    private static func isHebrew(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        return (scalar >= "\u{0590}" && scalar <= "\u{05FF}")
    }

    private static func isAlphaNumeric(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        if scalar >= "0" && scalar <= "9" {
            return true
        }
        if scalar >= "a" && scalar <= "z" {
            return true
        }
        if scalar >= "A" && scalar <= "Z" {
            return true
        }
        return false
    }

    /// Returns true if the character joins with the *following* letter
    /// (i.e., the next letter should take a non-isolated form).
    /// Joining types D (Dual) and C (Join_Causing) join forward.
    /// R (Right) only joins backward, so it does NOT join forward.
    public static func joinsForward(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        let value = scalar.value

        // TATWEEL (Join_Causing) — joins both sides
        if value == 0x0640 { return true }

        // Dual_Joining letters — join both sides
        // Listed by joining group from ArabicShaping.txt:
        // BEH, DOTLESS BEH WITH 2 DOTS ABOVE, DOTLESS BEH WITH 3 DOTS ABOVE,
        // HAH variants, SEEN variants, SAD variants, TAH variants, AIN variants,
        // KEHEH variants, FARSI YEH variants, FEH, QAF, KAF, LAM, MEEM, NOON,
        // HEH, DOTLESS YEH, YEH, YEH WITH HAMZA
        let dualJoining: Set<UInt32> = [
            0x0628, // BEH
            0x062A, // DOTLESS BEH WITH 2 DOTS ABOVE (TEH)
            0x062B, // DOTLESS BEH WITH 3 DOTS ABOVE (THEH)
            0x062C, // HAH WITH DOT BELOW (JEEM)
            0x062D, // HAH
            0x062E, // HAH WITH DOT ABOVE (KHAH)
            0x0633, // SEEN
            0x0634, // SEEN WITH 3 DOTS ABOVE (SHEEN)
            0x0635, // SAD
            0x0636, // SAD WITH DOT ABOVE (DAD)
            0x0637, // TAH
            0x0638, // TAH WITH DOT ABOVE (ZAH)
            0x0639, // AIN
            0x063A, // AIN WITH DOT ABOVE (GHAIN)
            0x063B, // KEHEH WITH 2 DOTS ABOVE
            0x063C, // KEHEH WITH 3 DOTS BELOW
            0x063D, // FARSI YEH WITH INVERTED V ABOVE
            0x063E, // FARSI YEH WITH 2 DOTS ABOVE
            0x063F, // FARSI YEH WITH 3 DOTS ABOVE
            0x0641, // FEH
            0x0642, // QAF
            0x0643, // KAF
            0x0644, // LAM
            0x0645, // MEEM
            0x0646, // NOON
            0x0647, // HEH
            0x0649, // DOTLESS YEH (ALEF MAKSURA — actually R, see note below)
            0x064A, // YEH
            0x0626, // DOTLESS YEH WITH HAMZA ABOVE (YEH WITH HAMZA)
        ]

        return dualJoining.contains(value)
    }

    /// Returns true if the character joins with the *preceding* letter
    /// (i.e., the previous letter should take a non-isolated form).
    /// Joining types D (Dual), C (Join_Causing), and R (Right) all join backward.
    public static func joinsBackward(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        let value = scalar.value

        // TATWEEL (Join_Causing) — joins both sides
        if value == 0x0640 { return true }

        // All Dual_Joining letters also join backward
        if joinsForward(ch) { return true }

        // Right_Joining letters — join backward only
        // ALEF variants, WAW variants, DAL variants, REH variants, TEH MARBUTA
        let rightJoining: Set<UInt32> = [
            0x0622, // ALEF WITH MADDA ABOVE
            0x0623, // ALEF WITH HAMZA ABOVE
            0x0624, // WAW WITH HAMZA ABOVE
            0x0625, // ALEF WITH HAMZA BELOW
            0x0627, // ALEF
            0x0629, // TEH MARBUTA
            0x062F, // DAL
            0x0630, // DAL WITH DOT ABOVE (THAL)
            0x0631, // REH
            0x0632, // REH WITH DOT ABOVE (ZAIN)
            0x0648, // WAW
            0x0649, // ALEF MAKSURA (DOTLESS YEH) — R in Unicode
        ]

        return rightJoining.contains(value)
    }

    private static func process(_ buf: String) -> String {
        let buf1 = String(buf.reversed())
        var buf2 = String()
        var buf3 = String()
        for i in 0..<buf1.count {
            let ch = buf1[buf1.index(buf1.startIndex, offsetBy: i)]
            if (ch == " " || ch == "," || ch == "." || ch == "-") {
                buf2.append(ch)
                continue
            }
            let index1 = buf1.index(buf1.startIndex, offsetBy: i)
            buf3.append(String(buf1[index1...]))
            buf3.append(String(buf2.reversed()))
            break
        }
        return buf3
    }
}
