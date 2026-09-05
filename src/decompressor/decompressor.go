// decompressor.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package decompressor

import (
	"bytes"
	"compress/zlib"
	"fmt"
	"io"
)

// Inflate decompresses zlib-compressed data (RFC 1950).
// Returns an error if the data is not valid zlib format.
func Inflate(buf []byte) (result []byte, err error) {
	reader, err := zlib.NewReader(bytes.NewReader(buf))
	if err != nil {
		return nil, fmt.Errorf("invalid zlib data: %w", err)
	}
	defer func() {
		if closeErr := reader.Close(); closeErr != nil && err == nil {
			err = fmt.Errorf("failed to close reader: %w", closeErr)
		}
	}()

	var inflated bytes.Buffer
	inflated.Grow(len(buf))
	if _, err := io.Copy(&inflated, reader); err != nil {
		return nil, fmt.Errorf("decompression failed: %w", err)
	}

	return inflated.Bytes(), nil
}
