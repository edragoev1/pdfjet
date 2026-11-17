// ibmplexsansdevanagari.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexSansDevanagari has the paths to the IBM Plex Sans Devanagari fonts. Each
// constant is the path of a font file, embedded as a subset of the glyphs a
// document draws; pass it to NewFontFromFile. PDFjet carries these fonts in
// the package world, with its fonts for Chinese, Japanese, Korean, Arabic,
// Hebrew and Thai: the path is read, when there is no such file, from the
// folder resources/fonts/world, else from the library when the program
// imports github.com/edragoev1/pdfjet/v9/resources/fonts/world.
//
// PDFjet does not shape Devanagari: these fonts are for Page.DrawGlyphs, a
// shaping engine shaping the text with the same font file. Complete for
// Hindi, Marathi, Nepali and Sanskrit (without the Vedic accents).
package IBMPlexSansDevanagari

// The paths of the font files.
const (
	// Bold is IBM Plex Sans Devanagari Bold.
	Bold = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Bold.otf"

	// Light is IBM Plex Sans Devanagari Light.
	Light = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Light.otf"
	// Medium is IBM Plex Sans Devanagari Medium.
	Medium = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Medium.otf"
	// Regular is IBM Plex Sans Devanagari Regular.
	Regular = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Regular.otf"
	// SemiBold is IBM Plex Sans Devanagari SemiBold.
	SemiBold = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-SemiBold.otf"
	// Text is IBM Plex Sans Devanagari Text.
	Text = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Text.otf"
	// Thin is IBM Plex Sans Devanagari Thin.
	Thin = "fonts/IBMPlexSansDevanagari/IBMPlexSansDevanagari-Thin.otf"
)
