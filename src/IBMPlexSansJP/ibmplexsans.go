// ibmplexsans.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansJP has the paths to the IBM Plex Sans JP fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// the package world, with its fonts for Chinese, Japanese, Korean, Arabic,
// Hebrew and Thai: the path is read, when there is no such file, from the
// folder resources/fonts/world, else from the library when the program
// imports github.com/edragoev1/pdfjet/v9/resources/fonts/world.
package IBMPlexSansJP

// The paths of the font files.
const (
	// Bold is IBM Plex Sans JP Bold.
	Bold = "fonts/IBMPlexSansJP/IBMPlexSansJP-Bold.otf"

	// ExtraLight is IBM Plex Sans JP ExtraLight, which PDFjet does not carry
	// from 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansJP/IBMPlexSansJP-ExtraLight.ttf"

	// Light is IBM Plex Sans JP Light, which PDFjet does not carry from 9.1.7:
	// a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Light = "fonts/IBMPlexSansJP/IBMPlexSansJP-Light.ttf"

	// Medium is IBM Plex Sans JP Medium, which PDFjet does not carry from
	// 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Medium = "fonts/IBMPlexSansJP/IBMPlexSansJP-Medium.ttf"
	// Regular is IBM Plex Sans JP Regular.
	Regular = "fonts/IBMPlexSansJP/IBMPlexSansJP-Regular.otf"
	// SemiBold is IBM Plex Sans JP SemiBold.
	SemiBold = "fonts/IBMPlexSansJP/IBMPlexSansJP-SemiBold.otf"

	// Text is IBM Plex Sans JP Text, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Text = "fonts/IBMPlexSansJP/IBMPlexSansJP-Text.ttf"

	// Thin is IBM Plex Sans JP Thin, which PDFjet does not carry from 9.1.7: a
	// font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	Thin = "fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf"
)
