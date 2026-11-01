// ibmplexmath.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexMath has the paths to the IBM Plex Math fonts, which
// PDFjet does not carry from 9.1.7: each constant is the path of a font
// file of the packages of PDFjet before 9.1.7, which
// NewFontFromFile reads when the file is there.
//
// Deprecated: PDFjet carries IBM Plex Sans and Serif, whose math signs serve
// business documents.
package IBMPlexMath

// The paths of the font files.
const (
	// Regular is IBM Plex Math Regular.
	Regular = "fonts/IBMPlexMath/IBMPlexMath-Regular.ttf"
)
