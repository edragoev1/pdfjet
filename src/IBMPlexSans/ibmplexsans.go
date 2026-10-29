// ibmplexsans.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSans has the paths to the IBM Plex Sans fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// itself: the path is read from the library when there is no such file.
package IBMPlexSans

// The paths of the font files.
const (
	// BoldItalic is IBM Plex Sans BoldItalic.
	BoldItalic = "fonts/IBMPlexSans/IBMPlexSans-BoldItalic.ttf"
	// Bold is IBM Plex Sans Bold.
	Bold = "fonts/IBMPlexSans/IBMPlexSans-Bold.ttf"

	// ExtraLightItalic is IBM Plex Sans ExtraLightItalic, which PDFjet does
	// not carry from 9.1.7: a font file of pdfjet-fonts, read when the file is
	// there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLightItalic = "fonts/IBMPlexSans/IBMPlexSans-ExtraLightItalic.ttf"

	// ExtraLight is IBM Plex Sans ExtraLight, which PDFjet does not carry from
	// 9.1.7: a font file of pdfjet-fonts, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSans/IBMPlexSans-ExtraLight.ttf"
	// Italic is IBM Plex Sans Italic.
	Italic = "fonts/IBMPlexSans/IBMPlexSans-Italic.ttf"
	// LightItalic is IBM Plex Sans LightItalic.
	LightItalic = "fonts/IBMPlexSans/IBMPlexSans-LightItalic.ttf"
	// Light is IBM Plex Sans Light.
	Light = "fonts/IBMPlexSans/IBMPlexSans-Light.ttf"
	// MediumItalic is IBM Plex Sans MediumItalic.
	MediumItalic = "fonts/IBMPlexSans/IBMPlexSans-MediumItalic.ttf"
	// Medium is IBM Plex Sans Medium.
	Medium = "fonts/IBMPlexSans/IBMPlexSans-Medium.ttf"
	// Regular is IBM Plex Sans Regular.
	Regular = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"
	// SemiBoldItalic is IBM Plex Sans SemiBoldItalic.
	SemiBoldItalic = "fonts/IBMPlexSans/IBMPlexSans-SemiBoldItalic.ttf"
	// SemiBold is IBM Plex Sans SemiBold.
	SemiBold = "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf"
	// TextItalic is IBM Plex Sans TextItalic.
	TextItalic = "fonts/IBMPlexSans/IBMPlexSans-TextItalic.ttf"
	// Text is IBM Plex Sans Text.
	Text = "fonts/IBMPlexSans/IBMPlexSans-Text.ttf"
	// ThinItalic is IBM Plex Sans ThinItalic.
	ThinItalic = "fonts/IBMPlexSans/IBMPlexSans-ThinItalic.ttf"
	// Thin is IBM Plex Sans Thin.
	Thin = "fonts/IBMPlexSans/IBMPlexSans-Thin.ttf"
)
