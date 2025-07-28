package pdfjet

/**
 * textutils.go
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

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"
)

func ReadTextLines(filePath string) []string {
	lines := make([]string, 0)
	file, err := os.Open(filePath)
	if err != nil {
		log.Fatal(err)
	}
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		lines = append(lines, line)
	}
	return lines
}

func PrintDuration(example string, duration time.Duration) {
	durationAsString := fmt.Sprintf("%.1f", float32(duration.Microseconds())/float32(1000.0))
	if len(durationAsString) == 3 {
		durationAsString = "    " + durationAsString
	} else if len(durationAsString) == 4 {
		durationAsString = "   " + durationAsString
	} else if len(durationAsString) == 5 {
		durationAsString = "  " + durationAsString
	} else if len(durationAsString) == 6 {
		durationAsString = " " + durationAsString
	}
	fmt.Print(example + " => " + durationAsString + "\n")
}
