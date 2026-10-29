// ibmplexsansarabic.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansArabic has the paths to the IBM Plex Sans Arabic
// fonts. Each constant is the path of a font file, embedded as a subset of
// the glyphs a document draws; pass it to NewFontFromFile. PDFjet carries
// these fonts in itself: the path is read from the library when there is no
// such file.
package IBMPlexSansArabic

// The paths of the font files.
const (
	// Bold is IBM Plex Sans Arabic Bold.
	Bold = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Bold.ttf"

	// ExtraLight is IBM Plex Sans Arabic ExtraLight, which PDFjet does not
	// carry from 9.1.7: a font file of pdfjet-fonts, read when the file is
	// there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-ExtraLight.ttf"
	// Light is IBM Plex Sans Arabic Light.
	Light = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Light.ttf"
	// Medium is IBM Plex Sans Arabic Medium.
	Medium = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Medium.ttf"
	// Regular is IBM Plex Sans Arabic Regular.
	Regular = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Regular.ttf"
	// SemiBold is IBM Plex Sans Arabic SemiBold.
	SemiBold = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-SemiBold.ttf"
	// Text is IBM Plex Sans Arabic Text.
	Text = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Text.ttf"
	// Thin is IBM Plex Sans Arabic Thin.
	Thin = "fonts/IBMPlexSansArabic/IBMPlexSansArabic-Thin.ttf"
)
