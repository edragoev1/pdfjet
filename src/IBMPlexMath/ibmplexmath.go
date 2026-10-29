// ibmplexmath.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexMath has the paths to the IBM Plex Math fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// itself: the path is read from the library when there is no such file.
package IBMPlexMath

// The paths of the font files.
const (
	// Regular is IBM Plex Math Regular.
	Regular = "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf"
)
