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

// writerPools reuse *zlib.Writer instances (and their internal Huffman/hash
// tables) across calls instead of allocating a fresh one every time, one pool
// for each level. Those internal tables are the expensive part of a
// zlib.Writer, not the small bytes.Buffer destination, so pooling the writer
// alone removes most of the allocation and CPU cost of repeated calls (e.g.
// one per page in a large document) while keeping memory use low - a pool
// only ever holds as many writers as there are concurrent callers.
var pagePool = newWriterPool(zlib.DefaultCompression)
var imagePool = newWriterPool(imageLevel)

// imageLevel is the compression level of the samples of an image. Level 5 is
// about 30% faster than the default 6 on the samples of a screenshot, for an
// output under 1% larger; on the content of the pages it makes the PDF 4%
// larger, so they keep the default.
const imageLevel = 5

func newWriterPool(level int) *sync.Pool {
	return &sync.Pool{
		New: func() any {
			writer, _ := zlib.NewWriterLevel(io.Discard, level)
			return writer
		},
	}
}

// Deflate deflates the input data.
func Deflate(buf []byte) []byte {
	return deflate(pagePool, buf)
}

// DeflateImage deflates the samples of an image, at imageLevel.
func DeflateImage(buf []byte) []byte {
	return deflate(imagePool, buf)
}

func deflate(pool *sync.Pool, buf []byte) []byte {
	var deflated bytes.Buffer
	// Page content usually compresses to less than an eighth of its size.
	deflated.Grow(len(buf)/8 + 64)
	writer := pool.Get().(*zlib.Writer)
	writer.Reset(&deflated)
	if _, err := writer.Write(buf); err != nil {
		panic(err)
	}
	if err := writer.Close(); err != nil {
		panic(err)
	}
	pool.Put(writer)
	return deflated.Bytes()
}

// ImageWriter deflates the samples of an image as they are written, at
// imageLevel, to the bytes DeflateImage makes of them written whole, so that
// an image is compressed a row at a time, its samples never in memory whole.
// It tells whether every byte written was one value, as a soft mask all of
// 255 is left out.
type ImageWriter struct {
	deflated bytes.Buffer
	writer   *zlib.Writer
	written  int
	same     int // The one value of every byte, -1 for none written yet, -2 for more
}

// NewImageWriter returns an ImageWriter.
func NewImageWriter() *ImageWriter {
	iw := &ImageWriter{same: -1}
	iw.writer = imagePool.Get().(*zlib.Writer)
	iw.writer.Reset(&iw.deflated)
	return iw
}

// Write deflates the bytes, which it does not keep.
func (iw *ImageWriter) Write(p []byte) {
	iw.written += len(p)
	if iw.same != -2 {
		for _, b := range p {
			if iw.same == -1 {
				iw.same = int(b)
			} else if int(b) != iw.same {
				iw.same = -2
				break
			}
		}
	}
	if _, err := iw.writer.Write(p); err != nil {
		panic(err)
	}
}

// Empty tells whether no byte was written.
func (iw *ImageWriter) Empty() bool {
	return iw.written == 0
}

// AllAre tells whether bytes were written and every one of them was the value.
func (iw *ImageWriter) AllAre(value byte) bool {
	return iw.written > 0 && iw.same == int(value)
}

// Bytes ends the stream and returns it; the ImageWriter is done with.
func (iw *ImageWriter) Bytes() []byte {
	if err := iw.writer.Close(); err != nil {
		panic(err)
	}
	imagePool.Put(iw.writer)
	iw.writer = nil
	return iw.deflated.Bytes()
}

// Discard gives the writer back without its stream.
func (iw *ImageWriter) Discard() {
	if iw.writer != nil {
		imagePool.Put(iw.writer)
		iw.writer = nil
	}
}
