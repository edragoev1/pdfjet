package pdfjet

/**
 * svg.go
 *
Copyright 2023 Innovatics Inc.

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
	"log"
	"os"
	"strings"
)

func GetSVGPaths(filename string) []string {
	str, err := os.ReadFile(filename)
	if err != nil {
		log.Fatal(err)
	}
	var paths = make([]string, 0)
	var inPath = false
	var buffer = make([]rune, 0)
	for _, ch := range str {
		if !inPath && strings.HasSuffix(string(buffer), "<path d=") {
			inPath = true
			buffer = nil
		} else if inPath && ch == '"' {
			inPath = false
			paths = append(paths, string(buffer))
			buffer = nil
		} else {
			paths = append(paths, string(buffer))
		}
	}
	return paths
}
