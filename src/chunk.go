package pdfjet

/**
 * chunk.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Chunk is used by the pngimage module.
type Chunk struct {
	ChunkLength uint32
	ChunkType   []byte
	ChunkData   []byte
	ChunkCRC    uint32
}

// NewChunk construct new PNG chunk.
func NewChunk() *Chunk {
	chunk := new(Chunk)
	return chunk
}
