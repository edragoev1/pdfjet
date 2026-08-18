// content.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package content

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
	runes := make([]rune, 0)
	for _, ch := range string(contents) {
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
