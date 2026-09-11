// Bidi.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"unicode"
)

// forms holds the Arabic contextual letter forms.
// Each quintet: General, Isolated, End, Middle, Beginning.
// The lam-alef ligatures that ligateLamAlef puts in are listed by their
// isolated forms.
var forms = []rune{
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
	0x067E, 0x067E, 0xFB57, 0xFB59, 0xFB58, // PEH
	0x0686, 0x0686, 0xFB7B, 0xFB7D, 0xFB7C, // TCHEH
	0x0698, 0x0698, 0xFB8B, 0x0698, 0x0698, // JEH
	0x06A9, 0x06A9, 0xFB8F, 0xFB91, 0xFB90, // KEHEH
	0x06AF, 0x06AF, 0xFB93, 0xFB95, 0xFB94, // GAF
	0x06CC, 0x06CC, 0xFBFD, 0xFBFF, 0xFBFE, // FARSI YEH
	0x06C0, 0x06C0, 0xFBA5, 0x06C0, 0x06C0, // HEH WITH YEH ABOVE
	0x0679, 0x0679, 0xFB67, 0xFB69, 0xFB68, // TTEH
	0x0688, 0x0688, 0xFB89, 0x0688, 0x0688, // DDAL
	0x0691, 0x0691, 0xFB8D, 0x0691, 0x0691, // RREH
	0x06BA, 0x06BA, 0xFB9F, 0x06BA, 0x06BA, // NOON GHUNNA
	0x06BE, 0x06BE, 0xFBAB, 0xFBAD, 0xFBAC, // HEH DOACHASHMEE
	0x06C1, 0x06C1, 0xFBA7, 0xFBA9, 0xFBA8, // HEH GOAL
	0x06D2, 0x06D2, 0xFBAF, 0x06D2, 0x06D2, // YEH BARREE
	0x06D3, 0x06D3, 0xFBB1, 0x06D3, 0x06D3, // YEH BARREE WITH HAMZA ABOVE
	0xFEF5, 0xFEF5, 0xFEF6, 0xFEF5, 0xFEF5, // LAM WITH ALEF WITH MADDA ABOVE
	0xFEF7, 0xFEF7, 0xFEF8, 0xFEF7, 0xFEF7, // LAM WITH ALEF WITH HAMZA ABOVE
	0xFEF9, 0xFEF9, 0xFEFA, 0xFEF9, 0xFEF9, // LAM WITH ALEF WITH HAMZA BELOW
	0xFEFB, 0xFEFB, 0xFEFC, 0xFEFB, 0xFEFB, // LAM WITH ALEF
}

// Joining-type tables (built once at package init).
var dualJoining = map[rune]bool{
	0x0628: true, // BEH
	0x062A: true, // TEH
	0x062B: true, // THEH
	0x062C: true, // JEEM
	0x062D: true, // HAH
	0x062E: true, // KHAH
	0x0633: true, // SEEN
	0x0634: true, // SHEEN
	0x0635: true, // SAD
	0x0636: true, // DAD
	0x0637: true, // TAH
	0x0638: true, // ZAH
	0x0639: true, // AIN
	0x063A: true, // GHAIN
	0x063B: true, // KEHEH WITH 2 DOTS ABOVE
	0x063C: true, // KEHEH WITH 3 DOTS BELOW
	0x063D: true, // FARSI YEH WITH INVERTED V ABOVE
	0x063E: true, // FARSI YEH WITH 2 DOTS ABOVE
	0x063F: true, // FARSI YEH WITH 3 DOTS ABOVE
	0x0641: true, // FEH
	0x0642: true, // QAF
	0x0643: true, // KAF
	0x0644: true, // LAM
	0x0645: true, // MEEM
	0x0646: true, // NOON
	0x0647: true, // HEH
	0x064A: true, // YEH
	0x0626: true, // YEH WITH HAMZA (Dual_Joining)
	0x067E: true, // PEH
	0x0686: true, // TCHEH
	0x06A9: true, // KEHEH
	0x06AF: true, // GAF
	0x06CC: true, // FARSI YEH
	0x0679: true, // TTEH
	0x06BE: true, // HEH DOACHASHMEE
	0x06C1: true, // HEH GOAL
}

var rightJoining = map[rune]bool{
	0x0622: true, // ALEF WITH MADDA ABOVE
	0x0623: true, // ALEF WITH HAMZA ABOVE
	0x0624: true, // WAW WITH HAMZA ABOVE
	0x0625: true, // ALEF WITH HAMZA BELOW
	0x0627: true, // ALEF
	0x0629: true, // TEH MARBUTA
	0x062F: true, // DAL
	0x0630: true, // THAL
	0x0631: true, // REH
	0x0632: true, // ZAIN
	0x0648: true, // WAW
	0x0649: true, // ALEF MAKSURA (DOTLESS YEH)
	0x0698: true, // JEH
	0x06C0: true, // HEH WITH YEH ABOVE
	0x0688: true, // DDAL
	0x0691: true, // RREH
	0x06BA: true, // NOON GHUNNA, which has no initial or medial form
	0x06D2: true, // YEH BARREE
	0x06D3: true, // YEH BARREE WITH HAMZA ABOVE
	0xFEF5: true, // LAM WITH ALEF WITH MADDA ABOVE
	0xFEF7: true, // LAM WITH ALEF WITH HAMZA ABOVE
	0xFEF9: true, // LAM WITH ALEF WITH HAMZA BELOW
	0xFEFB: true, // LAM WITH ALEF
}

// The bidirectional character types of the Unicode Bidirectional Algorithm,
// https://www.unicode.org/reports/tr9/
const (
	bidiL   = iota // Left to right
	bidiR          // Right to left
	bidiAL         // Arabic letter
	bidiEN         // European number
	bidiAN         // Arabic number
	bidiES         // European number separator
	bidiET         // European number terminator
	bidiCS         // Common number separator
	bidiNSM        // Nonspacing mark
	bidiON         // Other neutral
)

// IsArabic reports whether the code point is in the Arabic block (U+0600..U+06FF).
func IsArabic(ch rune) bool {
	return ch >= 0x0600 && ch <= 0x06FF
}

// isHebrew reports whether the code point is in the Hebrew block (U+0590..U+05FF).
func isHebrew(ch rune) bool {
	return ch >= 0x0590 && ch <= 0x05FF
}

// JoinsForward reports whether the Arabic character joins the character that
// follows it.
func JoinsForward(ch rune) bool {
	if ch == 0x0640 || ch == 0x200D {
		return true // TATWEEL and ZWJ join both sides
	}
	return dualJoining[ch]
}

// JoinsBackward reports whether the Arabic character joins the character
// before it.
func JoinsBackward(ch rune) bool {
	if ch == 0x0640 {
		return true
	}
	if JoinsForward(ch) {
		return true
	}
	return rightJoining[ch]
}

// isArabicLetter reports whether the code point is one of the Arabic letters,
// or lam-alef ligatures, that have contextual forms in the forms table.
func isArabicLetter(ch rune) bool {
	for i := 0; i < len(forms); i += 5 {
		if ch == forms[i] {
			return true
		}
	}
	return false
}

// isTransparent reports whether the character is a Transparent joining type
// (combining mark / diacritic) that should be skipped when determining
// joining context, and kept attached to its base letter during visual
// reordering. The zero width non-joiner and joiner are not transparent: the
// non-joiner keeps the letters on either side of it from joining, and the
// joiner joins them.
func isTransparent(ch rune) bool {
	if ch == 0x200C || ch == 0x200D { // ZWNJ, ZWJ
		return false
	}
	return unicode.Is(unicode.Mn, ch) || // Nonspacing Mark
		unicode.Is(unicode.Me, ch) || // Enclosing Mark
		unicode.Is(unicode.Cf, ch) // Format
}

// isAlphaNumeric reports whether the character is a Unicode letter or
// decimal digit (categories L*, Nd).
func isAlphaNumeric(ch rune) bool {
	return unicode.Is(unicode.Nd, ch) || // Decimal Digit Number
		unicode.Is(unicode.Lu, ch) || // Uppercase Letter
		unicode.Is(unicode.Ll, ch) || // Lowercase Letter
		unicode.Is(unicode.Lt, ch) || // Titlecase Letter
		unicode.Is(unicode.Lm, ch) || // Modifier Letter
		unicode.Is(unicode.Lo, ch) // Other Letter
}

// mirrored returns the mirror image of a bidirectionally mirrored
// character, and false if the character is not mirrored.
// Data source: Unicode BidiMirroring.txt.
func mirrored(ch rune) (rune, bool) {
	switch ch {
	case '(':
		return ')', true
	case ')':
		return '(', true
	case '[':
		return ']', true
	case ']':
		return '[', true
	case '{':
		return '}', true
	case '}':
		return '{', true
	case '<':
		return '>', true
	case '>':
		return '<', true
	case 0x00AB:
		return 0x00BB, true // « »
	case 0x00BB:
		return 0x00AB, true
	case 0x2039:
		return 0x203A, true // ‹ › single angle quotes
	case 0x203A:
		return 0x2039, true
	case 0x207D:
		return 0x207E, true // superscript ( )
	case 0x207E:
		return 0x207D, true
	case 0x208D:
		return 0x208E, true // subscript ( )
	case 0x208E:
		return 0x208D, true
	case 0x2308:
		return 0x2309, true // ⌈ ⌉ left/right ceiling
	case 0x2309:
		return 0x2308, true
	case 0x230A:
		return 0x230B, true // ⌊ ⌋ left/right floor
	case 0x230B:
		return 0x230A, true
	case 0x2329:
		return 0x232A, true // ⟨ ⟩ angle brackets
	case 0x232A:
		return 0x2329, true
	case 0xFF08:
		return 0xFF09, true // fullwidth ( )
	case 0xFF09:
		return 0xFF08, true
	case 0xFF1C:
		return 0xFF1E, true // fullwidth < >
	case 0xFF1E:
		return 0xFF1C, true
	case 0xFF3B:
		return 0xFF3D, true // fullwidth [ ]
	case 0xFF3D:
		return 0xFF3B, true
	case 0xFF5B:
		return 0xFF5D, true // fullwidth { }
	case 0xFF5D:
		return 0xFF5B, true
	case 0xFE59:
		return 0xFE5A, true // small ( )
	case 0xFE5A:
		return 0xFE59, true
	case 0xFE5B:
		return 0xFE5C, true // small { }
	case 0xFE5C:
		return 0xFE5B, true
	case 0xFE5D:
		return 0xFE5E, true // small tortoise shell
	case 0xFE5E:
		return 0xFE5D, true
	case 0xFE64:
		return 0xFE65, true // small < >
	case 0xFE65:
		return 0xFE64, true
	case 0x3008:
		return 0x3009, true // CJK 〈 〉
	case 0x3009:
		return 0x3008, true
	case 0x300A:
		return 0x300B, true // CJK 《 》
	case 0x300B:
		return 0x300A, true
	case 0x3010:
		return 0x3011, true // CJK BLACK LENTICULAR BRACKET
	case 0x3011:
		return 0x3010, true
	case 0x3014:
		return 0x3015, true // CJK 〔 〕
	case 0x3015:
		return 0x3014, true
	case 0x3016:
		return 0x3017, true // CJK 〖 〗
	case 0x3017:
		return 0x3016, true
	case 0x3018:
		return 0x3019, true // CJK 〘 〙
	case 0x3019:
		return 0x3018, true
	case 0x301A:
		return 0x301B, true // CJK 〚 〛
	case 0x301B:
		return 0x301A, true
	default:
		return 0, false
	}
}

// isOpeningBracket reports whether the character is an opening bracket that
// pairs with the closing bracket it mirrors. The angle brackets and angle
// quotation marks in the mirrored table are not paired brackets.
func isOpeningBracket(ch rune) bool {
	switch ch {
	case '(', '[', '{',
		0x207D, 0x208D, // superscript and subscript (
		0x2308, 0x230A, // left ceiling and floor
		0x2329,                 // left-pointing angle bracket
		0x3008, 0x300A, 0x3010, // CJK brackets
		0x3014, 0x3016, 0x3018, 0x301A,
		0xFE59, 0xFE5B, 0xFE5D, // small ( { and tortoise shell
		0xFF08, 0xFF3B, 0xFF5B: // fullwidth ( [ {
		return true
	default:
		return false
	}
}

// bidiType returns the bidirectional character type of the letters, digits
// and punctuation used in Arabic, Hebrew and Latin text.
func bidiType(ch rune) int {
	switch {
	case ch == 0x200E: // LRM
		return bidiL
	case ch == 0x200F: // RLM
		return bidiR
	case ch == 0x061C: // ALM
		return bidiAL
	case unicode.In(ch, unicode.Mn, unicode.Me):
		return bidiNSM
	case (ch >= '0' && ch <= '9') ||
		ch == 0x00B2 || ch == 0x00B3 || ch == 0x00B9 || // superscript 2, 3 and 1
		ch == 0x2070 || (ch >= 0x2074 && ch <= 0x2079) || // superscript digits
		(ch >= 0x2080 && ch <= 0x2089) || // subscript digits
		(ch >= 0x06F0 && ch <= 0x06F9) || // extended Arabic-Indic digits
		(ch >= 0xFF10 && ch <= 0xFF19): // fullwidth digits
		return bidiEN
	case (ch >= 0x0660 && ch <= 0x0669) || // Arabic-Indic digits
		ch == 0x066B || ch == 0x066C: // Arabic decimal and thousands separators
		return bidiAN
	case ch == '+' || ch == '-' || ch == 0x2212: // minus sign
		return bidiES
	case ch == ',' || ch == '.' || ch == '/' || ch == ':' ||
		ch == 0x00A0 || ch == 0x060C: // no-break space, Arabic comma
		return bidiCS
	case ch == '#' || ch == '%' ||
		ch == 0x00B0 || ch == 0x00B1 || // degree, plus-minus
		ch == 0x0609 || ch == 0x060A || ch == 0x066A || // Arabic per mille, per ten thousand, percent
		(ch >= 0x2030 && ch <= 0x2034): // per mille, per ten thousand, primes
		return bidiET
	case isHebrew(ch):
		return bidiR
	case IsArabic(ch):
		return bidiAL
	case unicode.Is(unicode.Sc, ch):
		return bidiET
	case isAlphaNumeric(ch):
		return bidiL
	}
	return bidiON
}

// resolveBidiTypes resolves the direction of each code point with the rules
// of the Unicode Bidirectional Algorithm for a right to left line without
// explicit embeddings: W1 to W7, N0 to N2 and I2. It returns bidiL for each
// code point in a left to right run and bidiR for the others.
func resolveBidiTypes(input []rune) []int {
	n := len(input)
	classes := make([]int, n)
	for i, ch := range input {
		classes[i] = bidiType(ch)
	}
	types := make([]int, n)
	copy(types, classes)

	// W1: a nonspacing mark takes the type of the character before it.
	for i := 0; i < n; i++ {
		if types[i] == bidiNSM {
			if i == 0 {
				types[i] = bidiR
			} else {
				types[i] = types[i-1]
			}
		}
	}

	// W2: a European number after an Arabic letter is an Arabic number.
	// W3: an Arabic letter is right to left.
	lastStrong := bidiR
	for i := 0; i < n; i++ {
		if types[i] == bidiAL {
			lastStrong = bidiAL
			types[i] = bidiR
		} else if types[i] == bidiL || types[i] == bidiR {
			lastStrong = types[i]
		} else if types[i] == bidiEN && lastStrong == bidiAL {
			types[i] = bidiAN
		}
	}

	// W4: a single separator between two numbers of the same kind is part of
	// the number.
	for i := 1; i < n-1; i++ {
		before := types[i-1]
		after := types[i+1]
		if types[i] == bidiES && before == bidiEN && after == bidiEN {
			types[i] = bidiEN
		} else if types[i] == bidiCS && before == after && (before == bidiEN || before == bidiAN) {
			types[i] = before
		}
	}

	// W5: currency, percent and similar signs next to a European number are
	// part of the number.
	start := 0
	for start < n {
		if types[start] != bidiET {
			start++
			continue
		}
		end := start
		for end < n && types[end] == bidiET {
			end++
		}
		if (start > 0 && types[start-1] == bidiEN) || (end < n && types[end] == bidiEN) {
			fillBidiTypes(types, start, end, bidiEN)
		}
		start = end
	}

	// W6: the other separators and terminators are neutral.
	for i := 0; i < n; i++ {
		if types[i] == bidiES || types[i] == bidiET || types[i] == bidiCS {
			types[i] = bidiON
		}
	}

	// W7: a European number after left to right text is left to right.
	lastStrong = bidiR
	for i := 0; i < n; i++ {
		if types[i] == bidiL || types[i] == bidiR {
			lastStrong = types[i]
		} else if types[i] == bidiEN && lastStrong == bidiL {
			types[i] = bidiL
		}
	}

	// N0: both brackets of a pair take the same direction.
	resolveBidiBrackets(input, classes, types)

	// N1, N2: neutral characters with left to right text on both sides are
	// left to right, and the others are right to left. Numbers count as right
	// to left here, and so do the start and the end of the line.
	start = 0
	for start < n {
		if types[start] != bidiON {
			start++
			continue
		}
		end := start
		for end < n && types[end] == bidiON {
			end++
		}
		if start > 0 && types[start-1] == bidiL && end < n && types[end] == bidiL {
			fillBidiTypes(types, start, end, bidiL)
		} else {
			fillBidiTypes(types, start, end, bidiR)
		}
		start = end
	}

	// I2: numbers are displayed left to right.
	for i := 0; i < n; i++ {
		if types[i] != bidiR {
			types[i] = bidiL
		}
	}
	return types
}

// resolveBidiBrackets applies rule N0. It finds the pairs of brackets with
// rule BD16 and gives both brackets of a pair the direction of the text
// between them, or of the text before them if the text between them is left
// to right.
func resolveBidiBrackets(input []rune, classes, types []int) {
	n := len(input)
	closing := make([]int, n) // The position of each opening bracket's pair
	for i := range closing {
		closing[i] = -1
	}
	var stack []int
	for i := 0; i < n; i++ {
		if types[i] != bidiON {
			continue
		}
		ch := input[i]
		if isOpeningBracket(ch) {
			if len(stack) == 63 {
				break
			}
			stack = append(stack, i)
			continue
		}
		m, ok := mirrored(ch)
		if !ok || !isOpeningBracket(m) {
			continue
		}
		for k := len(stack) - 1; k >= 0; k-- {
			if input[stack[k]] == m {
				closing[stack[k]] = i
				stack = stack[:k]
				break
			}
		}
	}

	for open := 0; open < n; open++ {
		close := closing[open]
		if close < 0 {
			continue
		}
		direction := bidiON
		for i := open + 1; i < close; i++ {
			strong := bidiStrongDirection(types[i])
			if strong == bidiR {
				direction = bidiR
				break
			}
			if strong == bidiL {
				direction = bidiL
			}
		}
		if direction == bidiL {
			direction = bidiR
			for i := open - 1; i >= 0; i-- {
				strong := bidiStrongDirection(types[i])
				if strong != bidiON {
					direction = strong
					break
				}
			}
		}
		if direction != bidiON {
			setBidiBracketType(classes, types, open, direction)
			setBidiBracketType(classes, types, close, direction)
		}
	}
}

// setBidiBracketType sets the type of a bracket and of the nonspacing marks
// after it.
func setBidiBracketType(classes, types []int, i, t int) {
	types[i] = t
	for k := i + 1; k < len(types) && classes[k] == bidiNSM; k++ {
		types[k] = t
	}
}

// bidiStrongDirection returns bidiL or bidiR for a strong type, with numbers
// counting as bidiR, or bidiON.
func bidiStrongDirection(t int) int {
	switch t {
	case bidiL:
		return bidiL
	case bidiR, bidiEN, bidiAN:
		return bidiR
	}
	return bidiON
}

// fillBidiTypes sets types[start:end] to t.
func fillBidiTypes(types []int, start, end, t int) {
	for k := start; k < end; k++ {
		types[k] = t
	}
}

// ReorderVisually reorders the string so that Arabic and Hebrew text
// flows from right to left while numbers and Latin text flows from
// left to right. The string is laid out as a right to left line with left
// to right text nested in it one level deep. Spaces, punctuation and
// brackets take their direction from the text around them, as in the
// Unicode Bidirectional Algorithm. Brackets in right to left text are
// replaced with their mirror images, each after a right-to-left mark
// (U+200F). Page does not draw the mark, and gives the bracket after it the
// bracket it stands for as actual text, so the text copied from the page has
// the brackets that were typed.
//
// Please see Example_27.
func ReorderVisually(str string) string {
	// Work with code points (runes) so that supplementary characters
	// are handled correctly. The explicit embedding, override and isolate
	// controls are left out, since left to right text is only nested one
	// level deep.
	var input []rune
	for _, ch := range str {
		if !isExplicitFormatting(ch) {
			input = append(input, ch)
		}
	}
	types := resolveBidiTypes(input)

	// buf1 gets the right to left text in logical order and each left to
	// right run reversed, so that reversing buf1 below puts the right to left
	// text in visual order and the left to right runs back in theirs.
	var buf1, buf2 []rune
	for j, ch := range input {
		if types[j] == bidiL {
			if ch != 0x200E { // LRM
				buf2 = append(buf2, ch)
			}
			continue
		}
		// An RLM or ALM is left out, but still ends the left to right run.
		buf1 = appendRunesReversed(buf1, buf2)
		buf2 = buf2[:0]
		if ch == 0x200F || ch == 0x061C { // RLM, ALM
			continue
		}
		// Brackets and the other mirrored characters are mirrored in right
		// to left text. Text extraction reverses the line, but does not mirror
		// them back, so an RLM before a mirrored character tells Page to give
		// it the character it stands for as actual text. buf1 is reversed
		// below, so the RLM goes after it here.
		if m, ok := mirrored(ch); ok {
			buf1 = append(buf1, m, 0x200F)
			continue
		}
		buf1 = append(buf1, ch)
	}
	buf1 = appendRunesReversed(buf1, buf2)

	// Arabic requires the lam-alef ligature.
	chars := ligateLamAlef(buf1)
	n := len(chars)

	var buf3 strings.Builder
	i := n - 1
	for i >= 0 {
		ch := chars[i]

		// If this is a transparent character (diacritic) with no
		// base letter to its right (in buf1 order), emit as-is.
		if isTransparent(ch) {
			buf3.WriteRune(ch)
			i--
			continue
		}

		diacriticCount := 0
		d := i - 1
		for d >= 0 {
			if !isTransparent(chars[d]) {
				break
			}
			diacriticCount++
			d--
		}

		if isArabicLetter(ch) {
			// Find previous non-transparent character (skip diacritics)
			prevIdx := d
			for prevIdx >= 0 {
				if !isTransparent(chars[prevIdx]) {
					break
				}
				prevIdx--
			}
			var prevCh rune
			if prevIdx >= 0 {
				prevCh = chars[prevIdx]
			}

			// Find next non-transparent character (skip diacritics)
			nextIdx := i + 1
			for nextIdx < n {
				if !isTransparent(chars[nextIdx]) {
					break
				}
				nextIdx++
			}
			var nextCh rune
			if nextIdx < n {
				nextCh = chars[nextIdx]
			}

			for j := 0; j < len(forms); j += 5 {
				if ch == forms[j] {
					canJoinPrev := JoinsBackward(ch)
					canJoinNext := JoinsForward(ch)
					prevJoins := JoinsForward(prevCh)
					nextJoins := JoinsBackward(nextCh)

					joinsOnLeft := canJoinPrev && prevJoins
					joinsOnRight := canJoinNext && nextJoins

					if !joinsOnLeft && !joinsOnRight {
						buf3.WriteRune(forms[j+1])
					} else if joinsOnLeft && !joinsOnRight {
						buf3.WriteRune(forms[j+2])
					} else if joinsOnLeft && joinsOnRight {
						buf3.WriteRune(forms[j+3])
					} else if !joinsOnLeft && joinsOnRight {
						buf3.WriteRune(forms[j+4])
					}
					break
				}
			}
		} else if ch != 0x200C && ch != 0x200D {
			// A zero width non-joiner or joiner is left out: it only
			// changes whether the letters on either side of it join.
			buf3.WriteRune(ch)
		}

		// Emit the diacritics that are before this character in buf1. In a
		// left to right run they are its own, reversed in buf1, so they
		// come out after it in their original order. In right to left text
		// they belong to the letter before it, so they come out reversed,
		// before that letter, like the rest of the text.
		for k := 0; k < diacriticCount; k++ {
			buf3.WriteRune(chars[i-1-k])
		}

		i = d
	}

	return buf3.String()
}

// ligateLamAlef replaces each lam followed by an alef with the lam-alef
// ligature. The right to left text is in logical order here, so the
// diacritics of the lam and of the alef come after the ligature. A zero
// width joiner between the two is left out.
func ligateLamAlef(chars []rune) []rune {
	ligated := make([]rune, 0, len(chars))
	for i := 0; i < len(chars); i++ {
		ligated = append(ligated, chars[i])
		if chars[i] != 0x0644 { // LAM
			continue
		}
		alef := i + 1
		for alef < len(chars) && (isTransparent(chars[alef]) || chars[alef] == 0x200D) {
			alef++
		}
		if alef == len(chars) || lamAlefLigature(chars[alef]) == 0 {
			continue
		}
		ligated[len(ligated)-1] = lamAlefLigature(chars[alef])
		for _, ch := range chars[i+1 : alef] {
			if ch != 0x200D {
				ligated = append(ligated, ch)
			}
		}
		i = alef
	}
	return ligated
}

// lettersOf returns the letters that a presentation form put in by
// ReorderVisually stands for, or nil for any other character. The fonts map
// these forms back to their letters, so text copied from a PDF has the letters.
// The letters are in the order they are drawn: right to left text is drawn in
// visual order, and text extraction reverses it, so a lam-alef ligature gives
// its alef before its lam.
func lettersOf(ch rune) []rune {
	if ch >= 0xFEF5 && ch <= 0xFEFC { // the lam-alef ligatures
		alefs := []rune{0x0622, 0x0623, 0x0625, 0x0627}
		return []rune{alefs[(ch-0xFEF5)/2], 0x0644}
	}
	if ch < 0xFB50 {
		return nil
	}
	for i := 0; i < len(forms); i += 5 {
		for j := i + 1; j < i+5; j++ {
			if forms[j] == ch {
				return []rune{forms[i]}
			}
		}
	}
	return nil
}

// letterOfIsolatedForm returns the letter that an isolated form put in by
// ReorderVisually stands for, or 0 for any other character. An isolated form
// looks like its letter.
func letterOfIsolatedForm(ch rune) rune {
	if ch >= 0xFB50 {
		for i := 0; i < len(forms); i += 5 {
			if forms[i+1] == ch {
				return forms[i]
			}
		}
	}
	return 0
}

// lamAlefLigature returns the isolated lam-alef ligature for the alef, or 0
// for any other character.
func lamAlefLigature(ch rune) rune {
	switch ch {
	case 0x0622: // ALEF WITH MADDA ABOVE
		return 0xFEF5
	case 0x0623: // ALEF WITH HAMZA ABOVE
		return 0xFEF7
	case 0x0625: // ALEF WITH HAMZA BELOW
		return 0xFEF9
	case 0x0627: // ALEF
		return 0xFEFB
	}
	return 0
}

// isExplicitFormatting reports whether the character is one of the explicit
// embedding, override and isolate controls: LRE, RLE, PDF, LRO, RLO, LRI,
// RLI, FSI and PDI.
func isExplicitFormatting(ch rune) bool {
	return (ch >= 0x202A && ch <= 0x202E) || (ch >= 0x2066 && ch <= 0x2069)
}

// appendRunesReversed appends the runes to buf in reverse order.
func appendRunesReversed(buf, runes []rune) []rune {
	for i := len(runes) - 1; i >= 0; i-- {
		buf = append(buf, runes[i])
	}
	return buf
}
