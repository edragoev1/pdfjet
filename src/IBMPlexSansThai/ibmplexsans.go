// ibmplexsans.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansThai has the paths to the IBM Plex Sans Thai fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// the package world, with its fonts for Chinese, Japanese, Korean, Arabic,
// Hebrew and Thai: the path is read, when there is no such file, from the
// folder resources/fonts/world, else from the library when the program
// imports github.com/edragoev1/pdfjet/v9/resources/fonts/world.
package IBMPlexSansThai

// The paths of the font files.
const (
	// Bold is IBM Plex Sans Thai Bold.
	Bold = "fonts/IBMPlexSansThai/IBMPlexSansThai-Bold.ttf"

	// ExtraLight is IBM Plex Sans Thai ExtraLight, which PDFjet does not carry
	// from 9.1.7: a font file of its packages before, read when the file is there.
	//
	// Deprecated: PDFjet carries the other weights of the family.
	ExtraLight = "fonts/IBMPlexSansThai/IBMPlexSansThai-ExtraLight.ttf"
	// Light is IBM Plex Sans Thai Light.
	Light = "fonts/IBMPlexSansThai/IBMPlexSansThai-Light.ttf"
	// Medium is IBM Plex Sans Thai Medium.
	Medium = "fonts/IBMPlexSansThai/IBMPlexSansThai-Medium.ttf"
	// Regular is IBM Plex Sans Thai Regular.
	Regular = "fonts/IBMPlexSansThai/IBMPlexSansThai-Regular.ttf"
	// SemiBold is IBM Plex Sans Thai SemiBold.
	SemiBold = "fonts/IBMPlexSansThai/IBMPlexSansThai-SemiBold.ttf"
	// Text is IBM Plex Sans Thai Text.
	Text = "fonts/IBMPlexSansThai/IBMPlexSansThai-Text.ttf"
	// Thin is IBM Plex Sans Thai Thin.
	Thin = "fonts/IBMPlexSansThai/IBMPlexSansThai-Thin.ttf"
)
