// compress.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package compress defines the options for compressing embedded files.
package compress

// Compress specifies whether an embedded file is compressed.
type Compress int

// Yes compresses the embedded file and No stores it uncompressed.
const (
	Yes Compress = iota
	No
)
