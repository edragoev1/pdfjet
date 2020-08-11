package pdfjet

/**
 * textutils.go
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
