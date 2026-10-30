// ibmplexsans.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansTC has the paths to the IBM Plex Sans TC fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts: the
// path is read, when there is no such file, from the folder
// resources/fonts/cjk, else from the library when the program imports
// github.com/edragoev1/pdfjet/v9/resources/fonts/cjk.
package IBMPlexSansTC

// The paths of the font files.
const (
	// Bold is IBM Plex Sans TC Bold.
	Bold = "fonts/IBMPlexSansTC/IBMPlexSansTC-Bold.otf"

	// ExtraLight is IBM Plex Sans TC ExtraLight, which PDFjet does not carry
	// from 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansTC/IBMPlexSansTC-ExtraLight.ttf"

	// Light is IBM Plex Sans TC Light, which PDFjet does not carry from 9.1.7:
	// a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Light = "fonts/IBMPlexSansTC/IBMPlexSansTC-Light.ttf"

	// Medium is IBM Plex Sans TC Medium, which PDFjet does not carry from
	// 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Medium = "fonts/IBMPlexSansTC/IBMPlexSansTC-Medium.ttf"
	// Regular is IBM Plex Sans TC Regular.
	Regular = "fonts/IBMPlexSansTC/IBMPlexSansTC-Regular.otf"
	// SemiBold is IBM Plex Sans TC SemiBold.
	SemiBold = "fonts/IBMPlexSansTC/IBMPlexSansTC-SemiBold.otf"

	// Text is IBM Plex Sans TC Text, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Text = "fonts/IBMPlexSansTC/IBMPlexSansTC-Text.ttf"

	// Thin is IBM Plex Sans TC Thin, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Thin = "fonts/IBMPlexSansTC/IBMPlexSansTC-Thin.ttf"
)
