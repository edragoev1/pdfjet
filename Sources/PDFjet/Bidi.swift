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

    /* General,Isolated,End,Middle,Beginning */
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
        "\u{0621}","\u{FE80}","\u{0621}","\u{0621}","\u{0621}",
        "\u{0624}","\u{FE85}","\u{FE86}","\u{0624}","\u{0624}",
        "\u{0625}","\u{FE87}","\u{FE88}","\u{0625}","\u{0625}",
        "\u{0626}","\u{FE89}","\u{FE8A}","\u{FE8C}","\u{FE8B}",
        "\u{0627}","\u{FE8D}","\u{FE8E}","\u{0627}","\u{0627}",
    ]

    private static func isArabicLetter(_ ch: Character) -> Bool {
        for i in stride(from: 0, to: forms.count, by: 5) {
            if ch == forms[i] {
                return true
            }
        }
        return false
    }

    /// Returns true if the character is a Transparent joining type
    /// (combining mark / diacritic) that should be skipped when
    /// determining joining context, and kept attached to its base
    /// letter during visual reordering.
    private static func isTransparent(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        let cat = scalar.properties.generalCategory
        return cat == .nonspacingMark
            || cat == .enclosingMark
            || cat == .format
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
        var rightToLeft: Bool = false
        for i in 0..<str.count {
            let ch = str[str.index(str.startIndex, offsetBy: i)]
            if ch == "\u{200E}" {
                rightToLeft = false
                continue
            }
            if ch == "\u{200F}" || ch == "\u{061C}" {
                rightToLeft = true
                continue
            }
            if isArabic(ch) ||
                    isHebrew(ch) ||
                    mirrored(ch) != nil {
                rightToLeft = true
                if buf2.count > 0 {
                    buf1.append(process(buf2))
                    buf2 = ""
                }
                buf1.append(mirrored(ch) ?? ch)
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

        // Convert to array for O(1) indexing (fixes Bug #5)
        let chars = Array(buf1)
        let n = chars.count

        var buf3 = String()
        var i: Int = n - 1
        while i >= 0 {
            let ch = chars[i]

            // If this is a transparent character (diacritic) with no
            // base letter to its right (in buf1 order), emit as-is.
            if isTransparent(ch) {
                buf3.append(ch)
                i -= 1
                continue
            }

            // Collect trailing diacritics that follow this base letter
            // in chars (at indices i-1, i-2, ... while transparent).
            var diacritics: [Character] = []
            var d = i - 1
            while d >= 0 {
                if !isTransparent(chars[d]) { break }
                diacritics.append(chars[d])
                d -= 1
            }

            if isArabicLetter(ch) {
                // Find previous non-transparent character (skip diacritics)
                var prevIdx = d
                while prevIdx >= 0 {
                    if !isTransparent(chars[prevIdx]) { break }
                    prevIdx -= 1
                }
                let prevCh: Character = prevIdx >= 0 ? chars[prevIdx] : "\u{0000}"

                // Find next non-transparent character (skip diacritics)
                var nextIdx = i + 1
                while nextIdx < n {
                    if !isTransparent(chars[nextIdx]) { break }
                    nextIdx += 1
                }
                let nextCh: Character = nextIdx < n ? chars[nextIdx] : "\u{0000}"

                for j in stride(from: 0, to: forms.count, by: 5) {
                    if ch == forms[j] {
                        let canJoinPrev = joinsBackward(ch)
                        let canJoinNext = joinsForward(ch)
                        let prevJoins = joinsForward(prevCh)
                        let nextJoins = joinsBackward(nextCh)

                        let joinsOnLeft  = canJoinPrev && prevJoins
                        let joinsOnRight = canJoinNext && nextJoins

                        if (!joinsOnLeft && !joinsOnRight) {
                            buf3.append(forms[j + 1])
                        } else if (joinsOnLeft && !joinsOnRight) {
                            buf3.append(forms[j + 2])
                        } else if (joinsOnLeft && joinsOnRight) {
                            buf3.append(forms[j + 3])
                        } else if (!joinsOnLeft && joinsOnRight) {
                            buf3.append(forms[j + 4])
                        }
                        break
                    }
                }
            } else {
                buf3.append(ch)
            }

            // Emit diacritics in their original order
            for dc in diacritics.reversed() {
                buf3.append(dc)
            }

            i = d
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
        let cat = scalar.properties.generalCategory
        return cat == .decimalNumber     // Nd
            || cat == .uppercaseLetter   // Lu
            || cat == .lowercaseLetter   // Ll
            || cat == .titlecaseLetter   // Lt
            || cat == .modifierLetter    // Lm
            || cat == .otherLetter       // Lo
    }

    /// Returns the mirror image of a bidirectionally mirrored character,
    /// or nil if the character is not mirrored.
    /// Data source: Unicode BidiMirroring.txt.
    private static func mirrored(_ ch: Character) -> Character? {
        switch ch {
        case "(":  return ")"
        case ")":  return "("
        case "[":  return "]"
        case "]":  return "["
        case "{":  return "}"
        case "}":  return "{"
        case "<":  return ">"
        case ">":  return "<"
        case "«":  return "»"
        case "»":  return "«"
        case "\u{2039}": return "\u{203A}"   // ‹ ›  single angle quotes
        case "\u{203A}": return "\u{2039}"
        case "\u{207D}": return "\u{207E}"   // superscript ( )
        case "\u{207E}": return "\u{207D}"
        case "\u{208D}": return "\u{208E}"   // subscript ( )
        case "\u{208E}": return "\u{208D}"
        case "\u{2308}": return "\u{2309}"   // ⌈ ⌉  left/right ceiling
        case "\u{2309}": return "\u{2308}"
        case "\u{230A}": return "\u{230B}"   // ⌊ ⌋  left/right floor
        case "\u{230B}": return "\u{230A}"
        case "\u{2329}": return "\u{232A}"   // ⟨ ⟩  angle brackets
        case "\u{232A}": return "\u{2329}"
        case "\u{FF08}": return "\u{FF09}"   // fullwidth ( )
        case "\u{FF09}": return "\u{FF08}"
        case "\u{FF1C}": return "\u{FF1E}"   // fullwidth < >
        case "\u{FF1E}": return "\u{FF1C}"
        case "\u{FF3B}": return "\u{FF3D}"   // fullwidth [ ]
        case "\u{FF3D}": return "\u{FF3B}"
        case "\u{FF5B}": return "\u{FF5D}"   // fullwidth { }
        case "\u{FF5D}": return "\u{FF5B}"
        case "\u{FE59}": return "\u{FE5A}"   // small ( )
        case "\u{FE5A}": return "\u{FE59}"
        case "\u{FE5B}": return "\u{FE5C}"   // small { }
        case "\u{FE5C}": return "\u{FE5B}"
        case "\u{FE5D}": return "\u{FE5E}"   // small tortoise shell
        case "\u{FE5E}": return "\u{FE5D}"
        case "\u{FE64}": return "\u{FE65}"   // small < >
        case "\u{FE65}": return "\u{FE64}"
        case "\u{3008}": return "\u{3009}"   // CJK 〈 〉
        case "\u{3009}": return "\u{3008}"
        case "\u{300A}": return "\u{300B}"   // CJK 《 》
        case "\u{300B}": return "\u{300A}"
        case "\u{3010}": return "\u{3011}"   // CJK BLACK LENTICULAR BRACKET
        case "\u{3011}": return "\u{3010}"
        case "\u{3014}": return "\u{3015}"   // CJK 〔 〕
        case "\u{3015}": return "\u{3014}"
        case "\u{3016}": return "\u{3017}"   // CJK 〖 〗
        case "\u{3017}": return "\u{3016}"
        case "\u{3018}": return "\u{3019}"   // CJK 〘 〙
        case "\u{3019}": return "\u{3018}"
        case "\u{301A}": return "\u{301B}"   // CJK 〚 〛
        case "\u{301B}": return "\u{301A}"
        default:   return nil
        }
    }

    public static func joinsForward(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        let value = scalar.value

        if value == 0x0640 { return true }

        let dualJoining: Set<UInt32> = [
            0x0628, // BEH
            0x062A, // TEH
            0x062B, // THEH
            0x062C, // JEEM
            0x062D, // HAH
            0x062E, // KHAH
            0x0633, // SEEN
            0x0634, // SHEEN
            0x0635, // SAD
            0x0636, // DAD
            0x0637, // TAH
            0x0638, // ZAH
            0x0639, // AIN
            0x063A, // GHAIN
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
            0x064A, // YEH
            0x0626, // YEH WITH HAMZA (Dual_Joining)
        ]

        return dualJoining.contains(value)
    }

    public static func joinsBackward(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        let value = scalar.value

        if value == 0x0640 { return true }

        if joinsForward(ch) { return true }

        let rightJoining: Set<UInt32> = [
            0x0622, // ALEF WITH MADDA ABOVE
            0x0623, // ALEF WITH HAMZA ABOVE
            0x0624, // WAW WITH HAMZA ABOVE
            0x0625, // ALEF WITH HAMZA BELOW
            0x0627, // ALEF
            0x0629, // TEH MARBUTA
            0x062F, // DAL
            0x0630, // THAL
            0x0631, // REH
            0x0632, // ZAIN
            0x0648, // WAW
            0x0649, // ALEF MAKSURA (DOTLESS YEH)
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
        // If the entire input was separators (loop never hit break),
        // buf3 is empty but buf2 holds the reversed separators.
        // Return them so they aren't silently dropped.
        if buf3.isEmpty {
            return String(buf2.reversed())
        }
        return buf3
    }
}
