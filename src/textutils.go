package pdfjet

/**
 * textutils.go
 *
Copyright 2022 Innovatics Inc.

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

import (
	"strings"
)

// SplitTextIntoTokens splits the text into tokens.
func SplitTextIntoTokens(text string, font, fallbackFont *Font, width float32) []string {
	tokens2 := make([]string, 0)

	tokens := strings.Fields(text)
	for _, token := range tokens {
		if font.StringWidth(fallbackFont, token) <= width {
			tokens2 = append(tokens2, token)
		} else {
			runes := []rune(token)
			var buf strings.Builder
			for _, ch := range runes {
				if font.StringWidth(fallbackFont, buf.String()+string(ch)) <= width {
					buf.WriteRune(ch)
				} else {
					tokens2 = append(tokens2, buf.String())
					buf.Reset()
					buf.WriteRune(ch)
				}
			}
			str := buf.String()
			if str != "" {
				tokens2 = append(tokens2, str)
			}
		}
	}

	return tokens2
}
