// chunk.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// pngChunk is used by the pngimage module.
type pngChunk struct {
	chunkLength uint32
	chunkType   []byte
	chunkData   []byte
	chunkCRC    uint32
}

// newPNGChunk construct new PNG chunk.
func newPNGChunk() *pngChunk {
	chunk := new(pngChunk)
	return chunk
}
