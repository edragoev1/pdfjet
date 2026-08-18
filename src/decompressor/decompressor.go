/**
 * decompressor.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package decompressor

import (
	"bytes"
	"compress/zlib"
	"io"
	"log"
)

// Inflate inflates the input data.
func Inflate(buf []byte) []byte {
	var inflated bytes.Buffer
	reader, err := zlib.NewReader(bytes.NewBuffer(buf))
	if err != nil {
		log.Fatal(err)
	}
	_, err = io.Copy(&inflated, reader)
	if err != nil {
		log.Fatal(err)
	}
	reader.Close()
	return inflated.Bytes()
}
