package pdfjet

/**
 * bidi.go
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

/**
 *  Provides BIDI processing for Arabic and Hebrew.
 *
 *  Please see Example_27.
 */
func getForms() []rune {
	// General,Isolated,End,Middle,Beginning
	return []rune{
		'\u0623', '\uFE83', '\uFE84', '\u0623', '\u0623',
		'\u0628', '\uFE8F', '\uFE90', '\uFE92', '\uFE91',
		'\u062A', '\uFE95', '\uFE96', '\uFE98', '\uFE97',
		'\u062B', '\uFE99', '\uFE9A', '\uFE9C', '\uFE9B',
		'\u062C', '\uFE9D', '\uFE9E', '\uFEA0', '\uFE9F',
		'\u062D', '\uFEA1', '\uFEA2', '\uFEA4', '\uFEA3',
		'\u062E', '\uFEA5', '\uFEA6', '\uFEA8', '\uFEA7',
		'\u062F', '\uFEA9', '\uFEAA', '\u062F', '\u062F',
		'\u0630', '\uFEAB', '\uFEAC', '\u0630', '\u0630',
		'\u0631', '\uFEAD', '\uFEAE', '\u0631', '\u0631',
		'\u0632', '\uFEAF', '\uFEB0', '\u0632', '\u0632',
		'\u0633', '\uFEB1', '\uFEB2', '\uFEB4', '\uFEB3',
		'\u0634', '\uFEB5', '\uFEB6', '\uFEB8', '\uFEB7',
		'\u0635', '\uFEB9', '\uFEBA', '\uFEBC', '\uFEBB',
		'\u0636', '\uFEBD', '\uFEBE', '\uFEC0', '\uFEBF',
		'\u0637', '\uFEC1', '\uFEC2', '\uFEC4', '\uFEC3',
		'\u0638', '\uFEC5', '\uFEC6', '\uFEC8', '\uFEC7',
		'\u0639', '\uFEC9', '\uFECA', '\uFECC', '\uFECB',
		'\u063A', '\uFECD', '\uFECE', '\uFED0', '\uFECF',
		'\u0641', '\uFED1', '\uFED2', '\uFED4', '\uFED3',
		'\u0642', '\uFED5', '\uFED6', '\uFED8', '\uFED7',
		'\u0643', '\uFED9', '\uFEDA', '\uFEDC', '\uFEDB',
		'\u0644', '\uFEDD', '\uFEDE', '\uFEE0', '\uFEDF',
		'\u0645', '\uFEE1', '\uFEE2', '\uFEE4', '\uFEE3',
		'\u0646', '\uFEE5', '\uFEE6', '\uFEE8', '\uFEE7',
		'\u0647', '\uFEE9', '\uFEEA', '\uFEEC', '\uFEEB',
		'\u0648', '\uFEED', '\uFEEE', '\u0648', '\u0648',
		'\u064A', '\uFEF1', '\uFEF2', '\uFEF4', '\uFEF3',
		'\u0622', '\uFE81', '\uFE82', '\u0622', '\u0622',
		'\u0629', '\uFE93', '\uFE94', '\u0629', '\u0629',
		'\u0649', '\uFEEF', '\uFEF0', '\u0649', '\u0649'}
}

func isArabicLetter(ch rune) bool {
	forms := getForms()
	for i := 0; i < len(forms); i += 5 {
		if ch == forms[i] {
			return true
		}
	}
	return false
}

// ReorderVisually reorders the string so that Arabic and Hebrew text flows from right
// to left while numbers and Latin text flows from left to right.
// @param str the input string.
// @return the reordered string.
func ReorderVisually(text string) string {
	forms := getForms()

	buf1 := make([]rune, 0)
	buf2 := make([]rune, 0)

	runes := []rune(text)
	rightToLeft := true
	for _, ch := range runes {
		if ch == '\u200E' {
			// LRM  U+200E  LEFT-TO-RIGHT MARK  Left-to-right zero-width character
			rightToLeft = false
			continue
		}
		if ch == '\u200F' || ch == '\u061C' {
			// RLM  U+200F  RIGHT-TO-LEFT MARK  Right-to-left zero-width non-Arabic character
			// ALM  U+061C  ARABIC LETTER MARK  Right-to-left zero-width Arabic character
			rightToLeft = true
			continue
		}
		if isArabic(ch) || isHebrew(ch) ||
			ch == '«' || ch == '»' ||
			ch == '(' || ch == ')' ||
			ch == '[' || ch == ']' {
			rightToLeft = true
			if len(buf2) > 0 {
				buf1 = append(buf1, processText(buf2)...)
				buf2 = make([]rune, 0)
			}
			if ch == '«' {
				buf1 = append(buf1, '»')
			} else if ch == '»' {
				buf1 = append(buf1, '«')
			} else if ch == '(' {
				buf1 = append(buf1, ')')
			} else if ch == ')' {
				buf1 = append(buf1, '(')
			} else if ch == '[' {
				buf1 = append(buf1, ']')
			} else if ch == ']' {
				buf1 = append(buf1, '[')
			} else {
				buf1 = append(buf1, ch)
			}
		} else if isAlphaNumeric(ch) {
			rightToLeft = false
			buf2 = append(buf2, ch)
		} else {
			if rightToLeft {
				buf1 = append(buf1, ch)
			} else {
				buf2 = append(buf2, ch)
			}
		}
	}
	if len(buf2) > 0 {
		buf1 = append(buf1, processText(buf2)...)
	}

	buf3 := make([]rune, 0)
	for i := (len(buf1) - 1); i >= 0; i-- {
		ch := buf1[i]
		if isArabicLetter(ch) {
			prevCh := '\u0000'
			if i > 0 {
				prevCh = runes[i-1]
			}
			nextCh := '\u0000'
			if i < len(runes)-1 {
				nextCh = runes[i+1]
			}

			for j := 0; j < len(forms); j += 5 {
				if ch == forms[j] {
					if !isArabicLetter(prevCh) && !isArabicLetter(nextCh) {
						buf3 = append(buf3, forms[j+1]) // Isolated
					} else if isArabicLetter(prevCh) && !isArabicLetter(nextCh) {
						buf3 = append(buf3, forms[j+2]) // End
					} else if isArabicLetter(prevCh) && isArabicLetter(nextCh) {
						buf3 = append(buf3, forms[j+3]) // Middle
					} else if !isArabicLetter(prevCh) && isArabicLetter(nextCh) {
						buf3 = append(buf3, forms[j+4]) // Beginning
					}
				}
			}
		} else {
			buf3 = append(buf3, ch)
		}
	}

	return string(buf3)
}

func isArabic(ch rune) bool {
	return (ch >= 0x600 && ch <= 0x6FF)
}

func isHebrew(ch rune) bool {
	return (ch >= 0x0591 && ch <= 0x05F4)
}

func isAlphaNumeric(ch rune) bool {
	if ch >= '0' && ch <= '9' {
		return true
	}
	if ch >= 'a' && ch <= 'z' {
		return true
	}
	if ch >= 'A' && ch <= 'Z' {
		return true
	}
	return false
}

func reverse(buf []rune) []rune {
	i := 0
	j := len(buf) - 1
	for i < j {
		buf[i], buf[j] = buf[j], buf[i]
		i++
		j--
	}
	return buf
}

func processText(buf []rune) []rune {
	buf1 := reverse(buf)
	buf2 := make([]rune, 0)
	buf3 := make([]rune, 0)
	for i, ch := range buf1 {
		if ch == ' ' || ch == ',' || ch == '.' || ch == '-' {
			buf2 = append(buf2, ch)
			continue
		}
		buf3 = append(buf3, buf1[i:]...)
		buf3 = append(buf3, reverse(buf2)...)
		break
	}
	return buf3
}
