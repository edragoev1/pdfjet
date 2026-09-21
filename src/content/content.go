// content.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package content reads the contents of files and readers.
package content

import (
	"io"
	"os"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/internal/utf8text"
)

// OfTextFile returns the contents of the specified text file.
// It panics if the file cannot be read.
func OfTextFile(fileName string) string {
	file, err := os.Open(fileName)
	if err != nil {
		panic(err)
	}
	defer func() {
		if err = file.Close(); err != nil {
			panic(err)
		}
	}()
	contents, err := io.ReadAll(file)
	if err != nil {
		panic(err)
	}
	runes := make([]rune, 0)
	for _, ch := range utf8text.Decode(contents) {
		if ch != '\r' {
			runes = append(runes, ch)
		}
	}
	// A byte order mark at the start of the file is not part of the text.
	if len(runes) > 0 && runes[0] == '\uFEFF' {
		runes = runes[1:]
	}
	return string(runes)
}

// OfBinaryFile returns the contents of the specified file as bytes.
// It panics if the file cannot be read.
func OfBinaryFile(fileName string) []uint8 {
	contents, err := os.ReadFile(fileName)
	if err != nil {
		panic(err)
	}
	return contents
}

// GetFromStream returns all the bytes read from the reader.
// It panics if the reader fails.
func GetFromStream(reader io.Reader) []uint8 {
	contents, err := io.ReadAll(reader)
	if err != nil {
		panic(err)
	}
	return contents
}

// LinesOfTextFile returns the lines of a UTF-8 text file, without a byte order
// mark and without the line separators. An empty line is an empty string. It
// panics if the file cannot be read.
func LinesOfTextFile(fileName string) []string {
	lines := make([]string, 0)
	var buffer strings.Builder
	for _, ch := range OfTextFile(fileName) {
		if ch == '\n' {
			lines = append(lines, buffer.String())
			buffer.Reset()
		} else {
			buffer.WriteRune(ch)
		}
	}
	if buffer.Len() > 0 {
		lines = append(lines, buffer.String())
	}
	return lines
}
