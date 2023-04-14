package contents

/**
 * contents.go
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
	"io"
	"log"
	"os"
)

func OfTextFile(fileName string) string {
	file, err := os.Open(fileName)
	if err != nil {
		log.Fatal(err)
	}
	defer func() {
		if err = file.Close(); err != nil {
			log.Fatal(err)
		}
	}()
	contents, err := io.ReadAll(file)
	if err != nil {
		log.Fatal(err)
	}
	runes := []rune(string(contents))
	for _, ch := range runes {
		if ch != '\r' {
			runes = append(runes, ch)
		}
	}
	return string(runes)
}

func OfBinaryFile(fileName string) []uint8 {
	contents, err := os.ReadFile(fileName)
	if err != nil {
		log.Fatal(err)
	}
	return contents
}

func GetFromReader(reader io.Reader) []uint8 {
	contents, err := io.ReadAll(reader)
	if err != nil {
		log.Fatal(err)
	}
	return contents
}
