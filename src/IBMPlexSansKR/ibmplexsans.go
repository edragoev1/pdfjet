// ibmplexsans.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansKR has the paths to the IBM Plex Sans KR fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// itself: the path is read from the library when there is no such file.
package IBMPlexSansKR

// The paths of the font files.
const (
	// Bold is IBM Plex Sans KR Bold.
	Bold = "fonts/IBMPlexSansKR/IBMPlexSansKR-Bold.otf"

	// ExtraLight is IBM Plex Sans KR ExtraLight, which PDFjet does not carry
	// from 9.1.7: a font file of pdfjet-fonts, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansKR/IBMPlexSansKR-ExtraLight.ttf"

	// Light is IBM Plex Sans KR Light, which PDFjet does not carry from 9.1.7:
	// a font file of pdfjet-fonts, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Light = "fonts/IBMPlexSansKR/IBMPlexSansKR-Light.ttf"

	// Medium is IBM Plex Sans KR Medium, which PDFjet does not carry from
	// 9.1.7: a font file of pdfjet-fonts, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Medium = "fonts/IBMPlexSansKR/IBMPlexSansKR-Medium.ttf"
	// Regular is IBM Plex Sans KR Regular.
	Regular = "fonts/IBMPlexSansKR/IBMPlexSansKR-Regular.otf"
	// SemiBold is IBM Plex Sans KR SemiBold.
	SemiBold = "fonts/IBMPlexSansKR/IBMPlexSansKR-SemiBold.otf"

	// Text is IBM Plex Sans KR Text, which PDFjet does not carry from 9.1.7: a
	// font file of pdfjet-fonts, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Text = "fonts/IBMPlexSansKR/IBMPlexSansKR-Text.ttf"

	// Thin is IBM Plex Sans KR Thin, which PDFjet does not carry from 9.1.7: a
	// font file of pdfjet-fonts, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Thin = "fonts/IBMPlexSansKR/IBMPlexSansKR-Thin.ttf"
)
