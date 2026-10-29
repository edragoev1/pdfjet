// ibmplexsanshebrew.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansHebrew has the paths to the IBM Plex Sans Hebrew
// fonts. Each constant is the path of a font file, embedded as a subset of
// the glyphs a document draws; pass it to NewFontFromFile. PDFjet carries
// these fonts in itself: the path is read from the library when there is no
// such file.
package IBMPlexSansHebrew

// The paths of the font files.
const (
	// Bold is IBM Plex Sans Hebrew Bold.
	Bold = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Bold.ttf"

	// ExtraLight is IBM Plex Sans Hebrew ExtraLight, which PDFjet does not
	// carry from 9.1.7: a font file of pdfjet-fonts, read when the file is
	// there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-ExtraLight.ttf"
	// Light is IBM Plex Sans Hebrew Light.
	Light = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Light.ttf"
	// Medium is IBM Plex Sans Hebrew Medium.
	Medium = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Medium.ttf"
	// Regular is IBM Plex Sans Hebrew Regular.
	Regular = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Regular.ttf"
	// SemiBold is IBM Plex Sans Hebrew SemiBold.
	SemiBold = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-SemiBold.ttf"
	// Text is IBM Plex Sans Hebrew Text.
	Text = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Text.ttf"
	// Thin is IBM Plex Sans Hebrew Thin.
	Thin = "fonts/IBMPlexSansHebrew/IBMPlexSansHebrew-Thin.ttf"
)
