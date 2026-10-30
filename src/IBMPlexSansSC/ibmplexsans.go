// ibmplexsans.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansSC has the paths to the IBM Plex Sans SC fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts: the
// path is read, when there is no such file, from the folder
// resources/fonts/cjk, else from the library when the program imports
// github.com/edragoev1/pdfjet/v9/resources/fonts/cjk.
package IBMPlexSansSC

// The paths of the font files.
const (
	// Bold is IBM Plex Sans SC Bold.
	Bold = "fonts/IBMPlexSansSC/IBMPlexSansSC-Bold.otf"

	// ExtraLight is IBM Plex Sans SC ExtraLight, which PDFjet does not carry
	// from 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansSC/IBMPlexSansSC-ExtraLight.ttf"

	// Light is IBM Plex Sans SC Light, which PDFjet does not carry from 9.1.7:
	// a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Light = "fonts/IBMPlexSansSC/IBMPlexSansSC-Light.ttf"

	// Medium is IBM Plex Sans SC Medium, which PDFjet does not carry from
	// 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Medium = "fonts/IBMPlexSansSC/IBMPlexSansSC-Medium.ttf"
	// Regular is IBM Plex Sans SC Regular.
	Regular = "fonts/IBMPlexSansSC/IBMPlexSansSC-Regular.otf"
	// SemiBold is IBM Plex Sans SC SemiBold.
	SemiBold = "fonts/IBMPlexSansSC/IBMPlexSansSC-SemiBold.otf"

	// Text is IBM Plex Sans SC Text, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Text = "fonts/IBMPlexSansSC/IBMPlexSansSC-Text.ttf"

	// Thin is IBM Plex Sans SC Thin, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Thin = "fonts/IBMPlexSansSC/IBMPlexSansSC-Thin.ttf"
)
