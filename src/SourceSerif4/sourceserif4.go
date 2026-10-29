// sourceserif4.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package SourceSerif4 has the paths to the Source Serif 4 fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// itself: the path is read from the folder resources/fonts when there is no
// such file, else from the library.
package SourceSerif4

// The paths of the font files.
const (
	// Black is Source Serif 4 Black, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Black = "fonts/SourceSerif4/SourceSerif4-Black.ttf"

	// BlackItalic is Source Serif 4 BlackItalic, which PDFjet does not carry
	// from 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	BlackItalic = "fonts/SourceSerif4/SourceSerif4-BlackItalic.ttf"
	// Bold is Source Serif 4 Bold.
	Bold = "fonts/SourceSerif4/SourceSerif4-Bold.ttf"
	// BoldItalic is Source Serif 4 BoldItalic.
	BoldItalic = "fonts/SourceSerif4/SourceSerif4-BoldItalic.ttf"

	// ExtraLight is Source Serif 4 ExtraLight, which PDFjet does not carry
	// from 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/SourceSerif4/SourceSerif4-ExtraLight.ttf"

	// ExtraLightItalic is Source Serif 4 ExtraLightItalic, which PDFjet does
	// not carry from 9.1.7: a font file of its packages before, read when the file is
	// there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLightItalic = "fonts/SourceSerif4/SourceSerif4-ExtraLightItalic.ttf"
	// Italic is Source Serif 4 Italic.
	Italic = "fonts/SourceSerif4/SourceSerif4-Italic.ttf"
	// Light is Source Serif 4 Light.
	Light = "fonts/SourceSerif4/SourceSerif4-Light.ttf"
	// LightItalic is Source Serif 4 LightItalic.
	LightItalic = "fonts/SourceSerif4/SourceSerif4-LightItalic.ttf"
	// Medium is Source Serif 4 Medium.
	Medium = "fonts/SourceSerif4/SourceSerif4-Medium.ttf"
	// MediumItalic is Source Serif 4 MediumItalic.
	MediumItalic = "fonts/SourceSerif4/SourceSerif4-MediumItalic.ttf"
	// Regular is Source Serif 4 Regular.
	Regular = "fonts/SourceSerif4/SourceSerif4-Regular.ttf"
	// SemiBold is Source Serif 4 SemiBold.
	SemiBold = "fonts/SourceSerif4/SourceSerif4-SemiBold.ttf"
	// SemiBoldItalic is Source Serif 4 SemiBoldItalic.
	SemiBoldItalic = "fonts/SourceSerif4/SourceSerif4-SemiBoldItalic.ttf"
)
