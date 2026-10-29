// ibmplexmono.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package IBMPlexMono has the paths to the IBM Plex Mono fonts, which
// PDFjet does not carry from 9.1.7: each constant is the path of a font
// file of the packages of PDFjet before 9.1.7, which
// NewFontFromFile reads when the file is there.
//
// Deprecated: PDFjet carries Cascadia Mono, which has all of Greek and of
// Cyrillic in every face, as IBM Plex Mono does not, and which every
// constant of CascadiaMono reads with nothing to download.
package IBMPlexMono

// The paths of the font files.
const (
	// BoldItalic is IBM Plex Mono BoldItalic.
	BoldItalic = "fonts/IBMPlexMono/IBMPlexMono-BoldItalic.ttf"
	// Bold is IBM Plex Mono Bold.
	Bold = "fonts/IBMPlexMono/IBMPlexMono-Bold.ttf"
	// ExtraLightItalic is IBM Plex Mono ExtraLightItalic.
	ExtraLightItalic = "fonts/IBMPlexMono/IBMPlexMono-ExtraLightItalic.ttf"
	// ExtraLight is IBM Plex Mono ExtraLight.
	ExtraLight = "fonts/IBMPlexMono/IBMPlexMono-ExtraLight.ttf"
	// Italic is IBM Plex Mono Italic.
	Italic = "fonts/IBMPlexMono/IBMPlexMono-Italic.ttf"
	// LightItalic is IBM Plex Mono LightItalic.
	LightItalic = "fonts/IBMPlexMono/IBMPlexMono-LightItalic.ttf"
	// Light is IBM Plex Mono Light.
	Light = "fonts/IBMPlexMono/IBMPlexMono-Light.ttf"
	// MediumItalic is IBM Plex Mono MediumItalic.
	MediumItalic = "fonts/IBMPlexMono/IBMPlexMono-MediumItalic.ttf"
	// Medium is IBM Plex Mono Medium.
	Medium = "fonts/IBMPlexMono/IBMPlexMono-Medium.ttf"
	// Regular is IBM Plex Mono Regular.
	Regular = "fonts/IBMPlexMono/IBMPlexMono-Regular.ttf"
	// SemiBoldItalic is IBM Plex Mono SemiBoldItalic.
	SemiBoldItalic = "fonts/IBMPlexMono/IBMPlexMono-SemiBoldItalic.ttf"
	// SemiBold is IBM Plex Mono SemiBold.
	SemiBold = "fonts/IBMPlexMono/IBMPlexMono-SemiBold.ttf"
	// TextItalic is IBM Plex Mono TextItalic.
	TextItalic = "fonts/IBMPlexMono/IBMPlexMono-TextItalic.ttf"
	// Text is IBM Plex Mono Text.
	Text = "fonts/IBMPlexMono/IBMPlexMono-Text.ttf"
	// ThinItalic is IBM Plex Mono ThinItalic.
	ThinItalic = "fonts/IBMPlexMono/IBMPlexMono-ThinItalic.ttf"
	// Thin is IBM Plex Mono Thin.
	Thin = "fonts/IBMPlexMono/IBMPlexMono-Thin.ttf"
)
