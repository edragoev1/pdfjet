// Bidi.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"unicode"
	"unicode/utf8"
)

// forms holds the Arabic contextual letter forms.
// Each quintet: General, Isolated, End, Middle, Beginning.
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
}

// Bidi provides BIDI processing for Arabic and Hebrew.
//
// Please see Example_27.

// IsArabic reports whether the code point is in the Arabic block (U+0600..U+06FF).
func IsArabic(ch rune) bool {
	return ch >= 0x0600 && ch <= 0x06FF
}

// IsHebrew reports whether the code point is in the Hebrew block (U+0590..U+05FF).
func isHebrew(ch rune) bool {
	return ch >= 0x0590 && ch <= 0x05FF
}

// JoinsForward reports whether the character can join on its right side.
func JoinsForward(ch rune) bool {
	if ch == 0x0640 {
		return true // TATWEEL — joins both sides
	}
	return dualJoining[ch]
}

// JoinsBackward reports whether the character can join on its left side.
func JoinsBackward(ch rune) bool {
	if ch == 0x0640 {
		return true
	}
	if JoinsForward(ch) {
		return true
	}
	return rightJoining[ch]
}

// IsArabicLetter reports whether the code point is one of the 37 Arabic
// letters that have contextual presentation forms in the forms table.
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
// reordering.
func isTransparent(ch rune) bool {
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
// character, or -1 (and ok == false) if the character is not mirrored.
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

// ReorderVisually reorders the string so that Arabic and Hebrew text
// flows from right to left while numbers and Latin text flows from
// left to right.
func ReorderVisually(str string) string {
	// Work with code points (runes) so that supplementary characters
	// are handled correctly (mirrors Swift's Character iteration).
	input := []rune(str)

	var buf1, buf2 strings.Builder
	rightToLeft := false

	for j := 0; j < len(input); j++ {
		ch := input[j]

		if ch == 0x200E { // LRM
			rightToLeft = false
			continue
		}
		if ch == 0x200F || ch == 0x061C { // RLM / ALM
			rightToLeft = true
			continue
		}

		m, ok := mirrored(ch)
		if IsArabic(ch) || isHebrew(ch) || ok {
			rightToLeft = true
			if buf2.Len() > 0 {
				buf1.WriteString(processLTR(buf2.String()))
				buf2.Reset()
			}
			if ok {
				buf1.WriteRune(m)
			} else {
				buf1.WriteRune(ch)
			}
		} else if isAlphaNumeric(ch) {
			rightToLeft = false
			buf2.WriteRune(ch)
		} else {
			if rightToLeft {
				buf1.WriteRune(ch)
			} else {
				buf2.WriteRune(ch)
			}
		}
	}

	if buf2.Len() > 0 {
		buf1.WriteString(processLTR(buf2.String()))
	}

	// Convert to slice for O(1) indexing (fixes Bug #5)
	chars := []rune(buf1.String())
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
		} else {
			buf3.WriteRune(ch)
		}

		// Emit diacritics in their original order
		for k := 0; k < diacriticCount; k++ {
			buf3.WriteRune(chars[i-1-k])
		}

		i = d
	}

	return buf3.String()
}

// process reverses the buffer, then peels separator characters (space,
// comma, period, hyphen) off the front (which was the end of the LTR
// run) and re-appends them at the back, so that e.g. trailing
// punctuation stays visually at the end.
func processLTR(buf string) string {
	buf1 := reverseRunesBidi(buf)
	var buf2, buf3 strings.Builder

	cps := []rune(buf1)
	for j := 0; j < len(cps); j++ {
		ch := cps[j]
		if ch == ' ' || ch == ',' || ch == '.' || ch == '-' {
			buf2.WriteRune(ch)
			continue
		}
		buf3.WriteString(string(cps[j:]))
		buf3.WriteString(reverseRunesBidi(buf2.String()))
		break
	}

	// If the entire input was separators (loop never hit break),
	// buf3 is empty but buf2 holds the reversed separators.
	// Return them so they aren't silently dropped.
	if buf3.Len() == 0 {
		return reverseRunesBidi(buf2.String())
	}
	return buf3.String()
}

// reverseRunes reverses a string at the rune (code-point) level.
func reverseRunesBidi(s string) string {
	cps := []rune(s)
	out := make([]rune, len(cps))
	for i, j := 0, len(cps)-1; j >= 0; i, j = i+1, j-1 {
		out[i] = cps[j]
	}
	return string(out)
}

// Ensure utf8 package is referenced (used implicitly by []rune conversions
// and WriteRune; the import prevents "imported and not used" in edge cases
// where the compiler needs explicit verification).
var _ = utf8.RuneError
