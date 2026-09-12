/**
 * Bidi.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Provides BIDI processing for Arabic, Persian, Urdu and Hebrew.
 *
 * Please see Example_27.
 */
public class Bidi {

    /* General,Isolated,End,Middle,Beginning */
    /* The lam-alef ligatures that ligateLamAlef puts in are listed by their isolated forms. */
    private static let forms: [UInt32] = [
        0x0623, 0xFE83, 0xFE84, 0x0623, 0x0623,
        0x0628, 0xFE8F, 0xFE90, 0xFE92, 0xFE91,
        0x062A, 0xFE95, 0xFE96, 0xFE98, 0xFE97,
        0x062B, 0xFE99, 0xFE9A, 0xFE9C, 0xFE9B,
        0x062C, 0xFE9D, 0xFE9E, 0xFEA0, 0xFE9F,
        0x062D, 0xFEA1, 0xFEA2, 0xFEA4, 0xFEA3,
        0x062E, 0xFEA5, 0xFEA6, 0xFEA8, 0xFEA7,
        0x062F, 0xFEA9, 0xFEAA, 0x062F, 0x062F,
        0x0630, 0xFEAB, 0xFEAC, 0x0630, 0x0630,
        0x0631, 0xFEAD, 0xFEAE, 0x0631, 0x0631,
        0x0632, 0xFEAF, 0xFEB0, 0x0632, 0x0632,
        0x0633, 0xFEB1, 0xFEB2, 0xFEB4, 0xFEB3,
        0x0634, 0xFEB5, 0xFEB6, 0xFEB8, 0xFEB7,
        0x0635, 0xFEB9, 0xFEBA, 0xFEBC, 0xFEBB,
        0x0636, 0xFEBD, 0xFEBE, 0xFEC0, 0xFEBF,
        0x0637, 0xFEC1, 0xFEC2, 0xFEC4, 0xFEC3,
        0x0638, 0xFEC5, 0xFEC6, 0xFEC8, 0xFEC7,
        0x0639, 0xFEC9, 0xFECA, 0xFECC, 0xFECB,
        0x063A, 0xFECD, 0xFECE, 0xFED0, 0xFECF,
        0x0641, 0xFED1, 0xFED2, 0xFED4, 0xFED3,
        0x0642, 0xFED5, 0xFED6, 0xFED8, 0xFED7,
        0x0643, 0xFED9, 0xFEDA, 0xFEDC, 0xFEDB,
        0x0644, 0xFEDD, 0xFEDE, 0xFEE0, 0xFEDF,
        0x0645, 0xFEE1, 0xFEE2, 0xFEE4, 0xFEE3,
        0x0646, 0xFEE5, 0xFEE6, 0xFEE8, 0xFEE7,
        0x0647, 0xFEE9, 0xFEEA, 0xFEEC, 0xFEEB,
        0x0648, 0xFEED, 0xFEEE, 0x0648, 0x0648,
        0x064A, 0xFEF1, 0xFEF2, 0xFEF4, 0xFEF3,
        0x0622, 0xFE81, 0xFE82, 0x0622, 0x0622,
        0x0629, 0xFE93, 0xFE94, 0x0629, 0x0629,
        0x0649, 0xFEEF, 0xFEF0, 0x0649, 0x0649,
        0x0621, 0xFE80, 0x0621, 0x0621, 0x0621,
        0x0624, 0xFE85, 0xFE86, 0x0624, 0x0624,
        0x0625, 0xFE87, 0xFE88, 0x0625, 0x0625,
        0x0626, 0xFE89, 0xFE8A, 0xFE8C, 0xFE8B,
        0x0627, 0xFE8D, 0xFE8E, 0x0627, 0x0627,
        // The Persian and Urdu letters. Their isolated form is the letter itself,
        // which fonts draw isolated, since IBM Plex Sans Arabic has no glyphs for
        // their isolated presentation forms.
        0x067E, 0x067E, 0xFB57, 0xFB59, 0xFB58,     // PEH
        0x0686, 0x0686, 0xFB7B, 0xFB7D, 0xFB7C,     // TCHEH
        0x0698, 0x0698, 0xFB8B, 0x0698, 0x0698,     // JEH
        0x06A9, 0x06A9, 0xFB8F, 0xFB91, 0xFB90,     // KEHEH
        0x06AF, 0x06AF, 0xFB93, 0xFB95, 0xFB94,     // GAF
        0x06CC, 0x06CC, 0xFBFD, 0xFBFF, 0xFBFE,     // FARSI YEH
        0x06C0, 0x06C0, 0xFBA5, 0x06C0, 0x06C0,     // HEH WITH YEH ABOVE
        0x0679, 0x0679, 0xFB67, 0xFB69, 0xFB68,     // TTEH
        0x0688, 0x0688, 0xFB89, 0x0688, 0x0688,     // DDAL
        0x0691, 0x0691, 0xFB8D, 0x0691, 0x0691,     // RREH
        0x06BA, 0x06BA, 0xFB9F, 0x06BA, 0x06BA,     // NOON GHUNNA
        0x06BE, 0x06BE, 0xFBAB, 0xFBAD, 0xFBAC,     // HEH DOACHASHMEE
        0x06C1, 0x06C1, 0xFBA7, 0xFBA9, 0xFBA8,     // HEH GOAL
        0x06D2, 0x06D2, 0xFBAF, 0x06D2, 0x06D2,     // YEH BARREE
        0x06D3, 0x06D3, 0xFBB1, 0x06D3, 0x06D3,     // YEH BARREE WITH HAMZA ABOVE
        0xFEF5, 0xFEF5, 0xFEF6, 0xFEF5, 0xFEF5,     // LAM WITH ALEF WITH MADDA ABOVE
        0xFEF7, 0xFEF7, 0xFEF8, 0xFEF7, 0xFEF7,     // LAM WITH ALEF WITH HAMZA ABOVE
        0xFEF9, 0xFEF9, 0xFEFA, 0xFEF9, 0xFEF9,     // LAM WITH ALEF WITH HAMZA BELOW
        0xFEFB, 0xFEFB, 0xFEFC, 0xFEFB, 0xFEFB,     // LAM WITH ALEF
    ]

    // The bidirectional character types of the Unicode Bidirectional
    // Algorithm, https://www.unicode.org/reports/tr9/
    private static let L = 0        // Left to right
    private static let R = 1        // Right to left
    private static let AL = 2       // Arabic letter
    private static let EN = 3       // European number
    private static let AN = 4       // Arabic number
    private static let ES = 5       // European number separator
    private static let ET = 6       // European number terminator
    private static let CS = 7       // Common number separator
    private static let NSM = 8      // Nonspacing mark
    private static let ON = 9       // Other neutral

    private static let dualJoining: Set<UInt32> = [
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
        0x067E, // PEH
        0x0686, // TCHEH
        0x06A9, // KEHEH
        0x06AF, // GAF
        0x06CC, // FARSI YEH
        0x0679, // TTEH
        0x06BE, // HEH DOACHASHMEE
        0x06C1, // HEH GOAL
    ]

    private static let rightJoining: Set<UInt32> = [
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
        0x0698, // JEH
        0x06C0, // HEH WITH YEH ABOVE
        0x0688, // DDAL
        0x0691, // RREH
        0x06BA, // NOON GHUNNA, which has no initial or medial form
        0x06D2, // YEH BARREE
        0x06D3, // YEH BARREE WITH HAMZA ABOVE
        0xFEF5, // LAM WITH ALEF WITH MADDA ABOVE
        0xFEF7, // LAM WITH ALEF WITH HAMZA ABOVE
        0xFEF9, // LAM WITH ALEF WITH HAMZA BELOW
        0xFEFB, // LAM WITH ALEF
    ]

    private static func isArabicLetter(_ ch: UInt32) -> Bool {
        for i in stride(from: 0, to: forms.count, by: 5) {
            if ch == forms[i] {
                return true
            }
        }
        return false
    }

    private static func generalCategory(_ ch: UInt32) -> Unicode.GeneralCategory {
        return Unicode.Scalar(ch)?.properties.generalCategory ?? .unassigned
    }

    /// Returns true if the character is a Transparent joining type
    /// (combining mark / diacritic) that should be skipped when
    /// determining joining context, and kept attached to its base
    /// letter during visual reordering. The zero width non-joiner and
    /// joiner are not transparent: the non-joiner keeps the letters on
    /// either side of it from joining, and the joiner joins them.
    private static func isTransparent(_ ch: UInt32) -> Bool {
        if ch == 0x200C || ch == 0x200D {   // ZWNJ, ZWJ
            return false
        }
        let cat = generalCategory(ch)
        return cat == .nonspacingMark
            || cat == .enclosingMark
            || cat == .format
    }

    /**
     * Reorders the string so that Arabic and Hebrew text flows from right
     * to left while numbers and Latin text flows from left to right.
     * The string is laid out as a right to left line with left to right text
     * nested in it one level deep. Spaces, punctuation and brackets take
     * their direction from the text around them, as in the Unicode
     * Bidirectional Algorithm. Brackets in right to left text are replaced
     * with their mirror images, each after a right-to-left mark (U+200F).
     * Page does not draw the mark, and gives the bracket after it the
     * bracket it stands for as actual text, so the text copied from the page
     * has the brackets that were typed.
     *
     * - Parameter str: the input string.
     * - Returns: the reordered string.
     */
    public static func reorderVisually(_ str: String) -> String {
        return reorderVisually(str, 0, str.unicodeScalars.count)
    }

    ///
    /// Reorders the string like reorderVisually(_:), and returns the part of
    /// the result that comes from the Unicode scalars of the string at the
    /// offsets from `from`, inclusive, to `to`, exclusive. The letters are
    /// shaped in the context of the whole string, so a word that is broken
    /// between two lines keeps its joined forms at the break when each line
    /// is made this way.
    ///
    /// - Parameter str: the input string.
    /// - Parameter from: the offset of the first scalar to return.
    /// - Parameter to: the offset after the last scalar to return.
    /// - Returns: the reordered part of the string.
    ///
    public static func reorderVisually(_ str: String, _ from: Int, _ to: Int) -> String {
        // Work with Unicode scalars, as the Java, C# and Go ports work with
        // code points. A Character would hold a letter and its diacritics.
        // The explicit embedding, override and isolate controls are left out,
        // since left to right text is only nested one level deep. Each scalar
        // keeps its offset in str, so that the part asked for can be picked
        // out at the end.
        var input = [UInt32]()
        var indexes = [Int]()
        for (offset, scalar) in str.unicodeScalars.enumerated() where !isExplicitFormatting(scalar.value) {
            input.append(scalar.value)
            indexes.append(offset)
        }
        let types = resolveTypes(input)

        // buf1 gets the right to left text in logical order and each left to
        // right run reversed, so that reversing buf1 below puts the right to
        // left text in visual order and the left to right runs back in theirs.
        var buf1 = [UInt32]()
        var from1 = [Int]()
        var buf2 = [UInt32]()
        var from2 = [Int]()
        for j in 0...input.count {
            let ch: UInt32 = (j < input.count) ? input[j] : 0
            if j < input.count && types[j] == L {
                if ch != 0x200E {                               // LRM
                    buf2.append(ch)
                    from2.append(indexes[j])
                }
                continue
            }
            // An RLM or ALM is left out, but still ends the left to right run.
            buf1.append(contentsOf: buf2.reversed())
            from1.append(contentsOf: from2.reversed())
            buf2.removeAll()
            from2.removeAll()
            if j == input.count || ch == 0x200F || ch == 0x061C {   // RLM, ALM
                continue
            }
            // Brackets and the other mirrored characters are mirrored in
            // right to left text. Text extraction reverses the line, but does
            // not mirror them back, so an RLM before a mirrored character
            // tells Page to give it the character it stands for as actual
            // text. buf1 is reversed below, so the RLM goes after it here.
            if let m = mirrored(ch) {
                buf1.append(m)
                buf1.append(0x200F)
                from1.append(indexes[j])
                from1.append(indexes[j])
            } else {
                buf1.append(ch)
                from1.append(indexes[j])
            }
        }

        // Arabic requires the lam-alef ligature.
        let (chars, origins) = ligateLamAlef(buf1, from1)
        let n = chars.count

        var buf3 = [UInt32]()
        var from3 = [Int]()
        var i: Int = n - 1
        while i >= 0 {
            let ch = chars[i]

            // If this is a transparent character (diacritic) with no
            // base letter to its right (in buf1 order), emit as-is.
            if isTransparent(ch) {
                buf3.append(ch)
                from3.append(origins[i])
                i -= 1
                continue
            }

            var diacriticCount = 0
            var d = i - 1
            while d >= 0 {
                if !isTransparent(chars[d]) { break }
                diacriticCount += 1
                d -= 1
            }

            if isArabicLetter(ch) {
                // Find previous non-transparent character (skip diacritics)
                var prevIdx = d
                while prevIdx >= 0 {
                    if !isTransparent(chars[prevIdx]) { break }
                    prevIdx -= 1
                }
                let prevCh: UInt32 = prevIdx >= 0 ? chars[prevIdx] : 0x0000

                // Find next non-transparent character (skip diacritics)
                var nextIdx = i + 1
                while nextIdx < n {
                    if !isTransparent(chars[nextIdx]) { break }
                    nextIdx += 1
                }
                let nextCh: UInt32 = nextIdx < n ? chars[nextIdx] : 0x0000

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
                        } else {
                            buf3.append(forms[j + 4])
                        }
                        from3.append(origins[i])
                        break
                    }
                }
            } else if ch != 0x200C && ch != 0x200D {
                // A zero width non-joiner or joiner is left out: it only
                // changes whether the letters on either side of it join.
                buf3.append(ch)
                from3.append(origins[i])
            }

            // Emit the diacritics that are before this character in buf1. In a
            // left to right run they are its own, reversed in buf1, so they
            // come out after it in their original order. In right to left text
            // they belong to the letter before it, so they come out reversed,
            // before that letter, like the rest of the text.
            for k in 0..<diacriticCount {
                buf3.append(chars[i - 1 - k])
                from3.append(origins[i - 1 - k])
            }

            i = d
        }

        var result = String.UnicodeScalarView()
        for k in 0..<buf3.count where from3[k] >= from && from3[k] < to {
            append(&result, buf3[k])
        }
        return String(result)
    }

    /// Replaces each lam followed by an alef with the lam-alef ligature. The
    /// right to left text is in logical order here, so the diacritics of the
    /// lam and of the alef come after the ligature. A zero width joiner
    /// between the two is left out. The origins are the offsets the characters
    /// came from; the ligature keeps the lam's.
    private static func ligateLamAlef(_ chars: [UInt32], _ origins: [Int]) -> ([UInt32], [Int]) {
        var ligated = [UInt32]()
        var ligatedOrigins = [Int]()
        ligated.reserveCapacity(chars.count)
        ligatedOrigins.reserveCapacity(chars.count)
        var i = 0
        while i < chars.count {
            ligated.append(chars[i])
            ligatedOrigins.append(origins[i])
            if chars[i] == 0x0644 {                     // LAM
                var alef = i + 1
                while alef < chars.count && (isTransparent(chars[alef]) || chars[alef] == 0x200D) {
                    alef += 1
                }
                if alef < chars.count, let ligature = lamAlef(chars[alef]) {
                    ligated[ligated.count - 1] = ligature
                    for k in (i + 1)..<alef where chars[k] != 0x200D {
                        ligated.append(chars[k])
                        ligatedOrigins.append(origins[k])
                    }
                    i = alef
                }
            }
            i += 1
        }
        return (ligated, ligatedOrigins)
    }

    /// Returns the letters that a presentation form put in by reorderVisually
    /// stands for, or nil for any other character. The fonts map these forms
    /// back to their letters, so text copied from a PDF has the letters. The
    /// letters are in the order they are drawn: right to left text is drawn in
    /// visual order, and text extraction reverses it, so a lam-alef ligature
    /// gives its alef before its lam.
    static func lettersOf(_ ch: UInt32) -> [UInt32]? {
        if ch >= 0xFEF5 && ch <= 0xFEFC {               // the lam-alef ligatures
            let alefs: [UInt32] = [0x0622, 0x0623, 0x0625, 0x0627]
            return [alefs[Int((ch - 0xFEF5) / 2)], 0x0644]
        }
        if ch < 0xFB50 {
            return nil
        }
        for i in stride(from: 0, to: forms.count, by: 5) {
            for j in (i + 1)..<(i + 5) {
                if forms[j] == ch {
                    return [forms[i]]
                }
            }
        }
        return nil
    }

    /// Returns the letter that an isolated form put in by reorderVisually
    /// stands for, or nil for any other character. An isolated form looks
    /// like its letter.
    static func letterOfIsolatedForm(_ ch: UInt32) -> UInt32? {
        if ch >= 0xFB50 {
            for i in stride(from: 0, to: forms.count, by: 5) where forms[i + 1] == ch {
                return forms[i]
            }
        }
        return nil
    }

    /// Returns the isolated lam-alef ligature for the alef, or nil for any other character.
    private static func lamAlef(_ ch: UInt32) -> UInt32? {
        switch ch {
        case 0x0622: return 0xFEF5      // ALEF WITH MADDA ABOVE
        case 0x0623: return 0xFEF7      // ALEF WITH HAMZA ABOVE
        case 0x0625: return 0xFEF9      // ALEF WITH HAMZA BELOW
        case 0x0627: return 0xFEFB      // ALEF
        default:     return nil
        }
    }

    /// Returns true for the explicit embedding, override and isolate controls:
    /// LRE, RLE, PDF, LRO, RLO, LRI, RLI, FSI and PDI.
    private static func isExplicitFormatting(_ ch: UInt32) -> Bool {
        return (ch >= 0x202A && ch <= 0x202E) || (ch >= 0x2066 && ch <= 0x2069)
    }

    /// Appends a code point that came from a Unicode scalar or from the tables.
    private static func append(_ buf: inout String.UnicodeScalarView, _ ch: UInt32) {
        buf.append(Unicode.Scalar(ch)!)
    }

    /// Returns true if the character is in the Arabic Unicode block, U+0600 to U+06FF.
    public static func isArabic(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        return isArabic(scalar.value)
    }

    private static func isArabic(_ ch: UInt32) -> Bool {
        return ch >= 0x0600 && ch <= 0x06FF
    }

    private static func isHebrew(_ ch: UInt32) -> Bool {
        return ch >= 0x0590 && ch <= 0x05FF
    }

    private static func isAlphaNumeric(_ ch: UInt32) -> Bool {
        let cat = generalCategory(ch)
        return cat == .decimalNumber     // Nd
            || cat == .uppercaseLetter   // Lu
            || cat == .lowercaseLetter   // Ll
            || cat == .titlecaseLetter   // Lt
            || cat == .modifierLetter    // Lm
            || cat == .otherLetter       // Lo
    }

    // ---- Bidirectional types ----------------------------------------------

    /// Returns the bidirectional character type of the letters, digits and
    /// punctuation used in Arabic, Hebrew and Latin text.
    private static func bidiType(_ ch: UInt32) -> Int {
        if ch == 0x200E {                                           // LRM
            return L
        }
        if ch == 0x200F {                                           // RLM
            return R
        }
        if ch == 0x061C {                                           // ALM
            return AL
        }
        let cat = generalCategory(ch)
        if cat == .nonspacingMark || cat == .enclosingMark {
            return NSM
        }
        if (ch >= 0x30 && ch <= 0x39)                               // 0 to 9
                || ch == 0x00B2 || ch == 0x00B3 || ch == 0x00B9     // superscript 2, 3 and 1
                || ch == 0x2070 || (ch >= 0x2074 && ch <= 0x2079)   // superscript digits
                || (ch >= 0x2080 && ch <= 0x2089)                   // subscript digits
                || (ch >= 0x06F0 && ch <= 0x06F9)                   // extended Arabic-Indic digits
                || (ch >= 0xFF10 && ch <= 0xFF19) {                 // fullwidth digits
            return EN
        }
        if (ch >= 0x0660 && ch <= 0x0669)                           // Arabic-Indic digits
                || ch == 0x066B || ch == 0x066C {                   // Arabic decimal and thousands separators
            return AN
        }
        if ch == 0x2B || ch == 0x2D || ch == 0x2212 {               // + - and minus sign
            return ES
        }
        if ch == 0x2C || ch == 0x2E || ch == 0x2F || ch == 0x3A     // , . / :
                || ch == 0x00A0 || ch == 0x060C {                   // no-break space, Arabic comma
            return CS
        }
        if ch == 0x23 || ch == 0x25                                 // # %
                || ch == 0x00B0 || ch == 0x00B1                     // degree, plus-minus
                || ch == 0x0609 || ch == 0x060A || ch == 0x066A     // Arabic per mille, per ten thousand, percent
                || (ch >= 0x2030 && ch <= 0x2034) {                 // per mille, per ten thousand, primes
            return ET
        }
        if isHebrew(ch) {
            return R
        }
        if isArabic(ch) {
            return AL
        }
        if cat == .currencySymbol {
            return ET
        }
        if isAlphaNumeric(ch) {
            return L
        }
        return ON
    }

    /// Resolves the direction of each code point with the rules of the Unicode
    /// Bidirectional Algorithm for a right to left line without explicit
    /// embeddings: W1 to W7, N0 to N2 and I2.
    ///
    /// - Parameter input: the code points.
    /// - Returns: L for each code point in a left to right run and R for the others.
    private static func resolveTypes(_ input: [UInt32]) -> [Int] {
        let n = input.count
        let classes = input.map { bidiType($0) }
        var types = classes

        // W1: a nonspacing mark takes the type of the character before it.
        for i in 0..<n {
            if types[i] == NSM {
                types[i] = (i == 0) ? R : types[i - 1]
            }
        }

        // W2: a European number after an Arabic letter is an Arabic number.
        // W3: an Arabic letter is right to left.
        var lastStrong = R
        for i in 0..<n {
            if types[i] == AL {
                lastStrong = AL
                types[i] = R
            } else if types[i] == L || types[i] == R {
                lastStrong = types[i]
            } else if types[i] == EN && lastStrong == AL {
                types[i] = AN
            }
        }

        // W4: a single separator between two numbers of the same kind is part
        // of the number.
        for i in stride(from: 1, to: n - 1, by: 1) {
            let before = types[i - 1]
            let after = types[i + 1]
            if types[i] == ES && before == EN && after == EN {
                types[i] = EN
            } else if types[i] == CS && before == after && (before == EN || before == AN) {
                types[i] = before
            }
        }

        // W5: currency, percent and similar signs next to a European number
        // are part of the number.
        var start = 0
        while start < n {
            if types[start] != ET {
                start += 1
                continue
            }
            var end = start
            while end < n && types[end] == ET {
                end += 1
            }
            if (start > 0 && types[start - 1] == EN) || (end < n && types[end] == EN) {
                for k in start..<end {
                    types[k] = EN
                }
            }
            start = end
        }

        // W6: the other separators and terminators are neutral.
        for i in 0..<n {
            if types[i] == ES || types[i] == ET || types[i] == CS {
                types[i] = ON
            }
        }

        // W7: a European number after left to right text is left to right.
        lastStrong = R
        for i in 0..<n {
            if types[i] == L || types[i] == R {
                lastStrong = types[i]
            } else if types[i] == EN && lastStrong == L {
                types[i] = L
            }
        }

        // N0: both brackets of a pair take the same direction.
        resolveBrackets(input, classes, &types)

        // N1, N2: neutral characters with left to right text on both sides are
        // left to right, and the others are right to left. Numbers count as
        // right to left here, and so do the start and the end of the line.
        start = 0
        while start < n {
            if types[start] != ON {
                start += 1
                continue
            }
            var end = start
            while end < n && types[end] == ON {
                end += 1
            }
            let leftToRight =
                    start > 0 && types[start - 1] == L && end < n && types[end] == L
            for k in start..<end {
                types[k] = leftToRight ? L : R
            }
            start = end
        }

        // I2: numbers are displayed left to right.
        for i in 0..<n {
            if types[i] != R {
                types[i] = L
            }
        }
        return types
    }

    /// Applies rule N0. Finds the pairs of brackets with rule BD16 and gives
    /// both brackets of a pair the direction of the text between them, or of
    /// the text before them if the text between them is left to right.
    private static func resolveBrackets(
            _ input: [UInt32], _ classes: [Int], _ types: inout [Int]) {
        let n = input.count
        var closing = [Int](repeating: -1, count: n)    // The position of each opening bracket's pair
        var stack = [Int]()
        for i in 0..<n {
            if types[i] != ON {
                continue
            }
            let ch = input[i]
            if isOpeningBracket(ch) {
                if stack.count == 63 {
                    break
                }
                stack.append(i)
                continue
            }
            guard let m = mirrored(ch), isOpeningBracket(m) else {
                continue
            }
            var k = stack.count - 1
            while k >= 0 {
                if input[stack[k]] == m {
                    closing[stack[k]] = i
                    stack.removeSubrange(k...)
                    break
                }
                k -= 1
            }
        }

        for open in 0..<n {
            let close = closing[open]
            if close < 0 {
                continue
            }
            var direction = ON
            for i in stride(from: open + 1, to: close, by: 1) {
                let strong = strongDirection(types[i])
                if strong == R {
                    direction = R
                    break
                }
                if strong == L {
                    direction = L
                }
            }
            if direction == L {
                direction = R
                for i in stride(from: open - 1, through: 0, by: -1) {
                    let strong = strongDirection(types[i])
                    if strong != ON {
                        direction = strong
                        break
                    }
                }
            }
            if direction != ON {
                setBracketType(classes, &types, open, direction)
                setBracketType(classes, &types, close, direction)
            }
        }
    }

    /// Sets the type of a bracket and of the nonspacing marks after it.
    private static func setBracketType(
            _ classes: [Int], _ types: inout [Int], _ i: Int, _ type: Int) {
        types[i] = type
        var k = i + 1
        while k < types.count && classes[k] == NSM {
            types[k] = type
            k += 1
        }
    }

    /// Returns L or R for a strong type, with numbers counting as R, or ON.
    private static func strongDirection(_ type: Int) -> Int {
        if type == L {
            return L
        }
        if type == R || type == EN || type == AN {
            return R
        }
        return ON
    }

    /// Returns true if the character is an opening bracket that pairs with the
    /// closing bracket it mirrors. The angle brackets and angle quotation marks
    /// in the mirrored table are not paired brackets.
    private static func isOpeningBracket(_ ch: UInt32) -> Bool {
        switch ch {
        case 0x28, 0x5B, 0x7B,                  // ( [ {
             0x207D, 0x208D,                    // superscript and subscript (
             0x2308, 0x230A,                    // left ceiling and floor
             0x2329,                            // left-pointing angle bracket
             0x3008, 0x300A, 0x3010,            // CJK brackets
             0x3014, 0x3016, 0x3018, 0x301A,
             0xFE59, 0xFE5B, 0xFE5D,            // small ( { and tortoise shell
             0xFF08, 0xFF3B, 0xFF5B:            // fullwidth ( [ {
            return true
        default:
            return false
        }
    }

    /// Returns the mirror image of a bidirectionally mirrored character,
    /// or nil if the character is not mirrored.
    /// Data source: Unicode BidiMirroring.txt.
    static func mirrored(_ ch: UInt32) -> UInt32? {
        switch ch {
        case 0x28: return 0x29      // ( )
        case 0x29: return 0x28
        case 0x5B: return 0x5D      // [ ]
        case 0x5D: return 0x5B
        case 0x7B: return 0x7D      // { }
        case 0x7D: return 0x7B
        case 0x3C: return 0x3E      // < >
        case 0x3E: return 0x3C
        case 0x00AB: return 0x00BB   // « »
        case 0x00BB: return 0x00AB
        case 0x2039: return 0x203A   // ‹ ›  single angle quotes
        case 0x203A: return 0x2039
        case 0x207D: return 0x207E   // superscript ( )
        case 0x207E: return 0x207D
        case 0x208D: return 0x208E   // subscript ( )
        case 0x208E: return 0x208D
        case 0x2308: return 0x2309   // ⌈ ⌉  left/right ceiling
        case 0x2309: return 0x2308
        case 0x230A: return 0x230B   // ⌊ ⌋  left/right floor
        case 0x230B: return 0x230A
        case 0x2329: return 0x232A   // ⟨ ⟩  angle brackets
        case 0x232A: return 0x2329
        case 0xFF08: return 0xFF09   // fullwidth ( )
        case 0xFF09: return 0xFF08
        case 0xFF1C: return 0xFF1E   // fullwidth < >
        case 0xFF1E: return 0xFF1C
        case 0xFF3B: return 0xFF3D   // fullwidth [ ]
        case 0xFF3D: return 0xFF3B
        case 0xFF5B: return 0xFF5D   // fullwidth { }
        case 0xFF5D: return 0xFF5B
        case 0xFE59: return 0xFE5A   // small ( )
        case 0xFE5A: return 0xFE59
        case 0xFE5B: return 0xFE5C   // small { }
        case 0xFE5C: return 0xFE5B
        case 0xFE5D: return 0xFE5E   // small tortoise shell
        case 0xFE5E: return 0xFE5D
        case 0xFE64: return 0xFE65   // small < >
        case 0xFE65: return 0xFE64
        case 0x3008: return 0x3009   // CJK 〈 〉
        case 0x3009: return 0x3008
        case 0x300A: return 0x300B   // CJK 《 》
        case 0x300B: return 0x300A
        case 0x3010: return 0x3011   // CJK BLACK LENTICULAR BRACKET
        case 0x3011: return 0x3010
        case 0x3014: return 0x3015   // CJK 〔 〕
        case 0x3015: return 0x3014
        case 0x3016: return 0x3017   // CJK 〖 〗
        case 0x3017: return 0x3016
        case 0x3018: return 0x3019   // CJK 〘 〙
        case 0x3019: return 0x3018
        case 0x301A: return 0x301B   // CJK 〚 〛
        case 0x301B: return 0x301A
        default:   return nil
        }
    }

    /// Returns true if the Arabic character joins the character that follows it.
    public static func joinsForward(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        return joinsForward(scalar.value)
    }

    private static func joinsForward(_ ch: UInt32) -> Bool {
        if ch == 0x0640 || ch == 0x200D { return true }     // TATWEEL and ZWJ join both sides
        return dualJoining.contains(ch)
    }

    /// Returns true if the Arabic character joins the character before it.
    public static func joinsBackward(_ ch: Character) -> Bool {
        guard let scalar = ch.unicodeScalars.first else { return false }
        return joinsBackward(scalar.value)
    }

    private static func joinsBackward(_ ch: UInt32) -> Bool {
        if ch == 0x0640 { return true }
        if joinsForward(ch) { return true }
        return rightJoining.contains(ch)
    }
}
