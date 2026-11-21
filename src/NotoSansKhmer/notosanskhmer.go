// notosanskhmer.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package NotoSansKhmer has the paths to the Noto Sans Khmer fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// the package world, with its fonts for Chinese, Japanese, Korean, Arabic,
// Hebrew, Thai and others: the path is read, when there is no such file, from the
// folder resources/fonts/world, else from the library when the program
// imports github.com/edragoev1/pdfjet/v9/resources/fonts/world.
//
// PDFjet does not shape Khmer: these fonts are for ComplexText, which
// draws the text as a shaping engine shapes it with the same font file, for
// Khmer. Regular and Bold, from Noto's release, unmodified.
package NotoSansKhmer

// The paths of the font files.
const (
	// Bold is Noto Sans Khmer Bold.
	Bold = "fonts/NotoSansKhmer/NotoSansKhmer-Bold.otf"

	// Regular is Noto Sans Khmer Regular.
	Regular = "fonts/NotoSansKhmer/NotoSansKhmer-Regular.otf"
)
