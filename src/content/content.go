// content.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package content reads the contents of files and readers.
package content

import (
	"io"
	"os"
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
	for _, ch := range string(contents) {
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

// GetFromReader returns all the bytes read from the reader.
// It panics if the reader fails.
func GetFromReader(reader io.Reader) []uint8 {
	contents, err := io.ReadAll(reader)
	if err != nil {
		panic(err)
	}
	return contents
}
