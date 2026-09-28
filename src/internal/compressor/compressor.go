// compressor.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package compressor compresses data with the Deflate algorithm.
package compressor

import (
	"bytes"
	"compress/zlib"
	"io"
	"sync"
)

// writerPool reuses *zlib.Writer instances (and their internal Huffman/hash
// tables) across calls instead of allocating a fresh one every time. Those
// internal tables are the expensive part of a zlib.Writer, not the small
// bytes.Buffer destination, so pooling the writer alone removes most of the
// allocation and CPU cost of repeated calls (e.g. one per page in a large
// document) while keeping memory use low - the pool only ever holds as many
// writers as there are concurrent callers.
var writerPool = sync.Pool{
	New: func() any {
		writer, _ := zlib.NewWriterLevel(io.Discard, level)
		return writer
	},
}

// level is the compression level of the pages and the images. Level 5 is
// about 30% faster than the default 6 on the samples of a screenshot, for an
// output under 1% larger.
const level = 5

// Deflate deflates the input data.
func Deflate(buf []byte) []byte {
	var deflated bytes.Buffer
	// Page content usually compresses to less than an eighth of its size.
	deflated.Grow(len(buf)/8 + 64)
	writer := writerPool.Get().(*zlib.Writer)
	writer.Reset(&deflated)
	if _, err := writer.Write(buf); err != nil {
		panic(err)
	}
	if err := writer.Close(); err != nil {
		panic(err)
	}
	writerPool.Put(writer)
	return deflated.Bytes()
}
