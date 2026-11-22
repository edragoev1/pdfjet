// notosansmyanmar.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package NotoSansMyanmar has the paths to the Noto Sans Myanmar fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// the package world, with its fonts for Chinese, Japanese, Korean, Arabic,
// Hebrew, Thai and others: the path is read, when there is no such file, from the
// folder resources/fonts/world, else from the library when the program
// imports github.com/edragoev1/pdfjet/v9/resources/fonts/world.
//
// PDFjet does not shape Myanmar: these fonts are for TextBlock and TextLine,
// which draw the text as a shaping engine shapes it with the same font file, for
// Burmese. Regular and Bold, from Noto's release, unmodified.
package NotoSansMyanmar

// The paths of the font files.
const (
	// Bold is Noto Sans Myanmar Bold.
	Bold = "fonts/NotoSansMyanmar/NotoSansMyanmar-Bold.otf"

	// Regular is Noto Sans Myanmar Regular.
	Regular = "fonts/NotoSansMyanmar/NotoSansMyanmar-Regular.otf"
)
