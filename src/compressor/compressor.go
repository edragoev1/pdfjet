package compressor

/**
 * compressor.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"bytes"
	"compress/zlib"
)

// Deflate deflates the input data.
func Deflate(buf []byte) []byte {
	var deflated bytes.Buffer
	writer := zlib.NewWriter(&deflated)
	_, _ = writer.Write(buf)
	_ = writer.Close()
	return deflated.Bytes()
}
