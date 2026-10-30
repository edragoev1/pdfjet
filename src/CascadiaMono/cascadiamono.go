// cascadiamono.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package CascadiaMono has the paths to the Cascadia Mono fonts, Microsoft's
// monospace font, with all of Latin, Greek and Cyrillic in every face, the
// italic ones too. Each constant is the path of a font file, embedded as a
// subset of the glyphs a document draws; pass it to NewFontFromFile. PDFjet
// carries these fonts in itself: the path is read from the folder
// resources/fonts when there is no such file, else from the library.
package CascadiaMono

// The paths of the font files.
const (
	// Bold is Cascadia Mono Bold.
	Bold = "fonts/CascadiaMono/CascadiaMono-Bold.ttf"
	// BoldItalic is Cascadia Mono BoldItalic.
	BoldItalic = "fonts/CascadiaMono/CascadiaMono-BoldItalic.ttf"
	// Italic is Cascadia Mono Italic.
	Italic = "fonts/CascadiaMono/CascadiaMono-Italic.ttf"
	// Light is Cascadia Mono Light.
	Light = "fonts/CascadiaMono/CascadiaMono-Light.ttf"
	// LightItalic is Cascadia Mono LightItalic.
	LightItalic = "fonts/CascadiaMono/CascadiaMono-LightItalic.ttf"
	// Regular is Cascadia Mono Regular.
	Regular = "fonts/CascadiaMono/CascadiaMono-Regular.ttf"
	// SemiBold is Cascadia Mono SemiBold.
	SemiBold = "fonts/CascadiaMono/CascadiaMono-SemiBold.ttf"
	// SemiBoldItalic is Cascadia Mono SemiBoldItalic.
	SemiBoldItalic = "fonts/CascadiaMono/CascadiaMono-SemiBoldItalic.ttf"
	// SemiLight is Cascadia Mono SemiLight.
	SemiLight = "fonts/CascadiaMono/CascadiaMono-SemiLight.ttf"
	// SemiLightItalic is Cascadia Mono SemiLightItalic.
	SemiLightItalic = "fonts/CascadiaMono/CascadiaMono-SemiLightItalic.ttf"
)
