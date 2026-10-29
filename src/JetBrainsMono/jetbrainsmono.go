// jetbrainsmono.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package JetBrainsMono has the paths to the JetBrains Mono fonts, which
// PDFjet does not carry from 9.1.7: each constant is the path of a font
// file of pdfjet-fonts, https://github.com/edragoev1/pdfjet-fonts, which
// NewFontFromFile reads when the file is there.
//
// Deprecated: PDFjet carries Cascadia Mono, which has all of Greek and of
// Cyrillic in every face, as JetBrains Mono does not, and which every
// constant of CascadiaMono reads with nothing to download.
package JetBrainsMono

// The paths of the font files.
const (
	// Bold is JetBrains Mono Bold.
	Bold = "fonts/JetBrainsMono/JetBrainsMono-Bold.ttf"
	// BoldItalic is JetBrains Mono BoldItalic.
	BoldItalic = "fonts/JetBrainsMono/JetBrainsMono-BoldItalic.ttf"
	// ExtraLight is JetBrains Mono ExtraLight.
	ExtraLight = "fonts/JetBrainsMono/JetBrainsMono-ExtraLight.ttf"
	// ExtraLightItalic is JetBrains Mono ExtraLightItalic.
	ExtraLightItalic = "fonts/JetBrainsMono/JetBrainsMono-ExtraLightItalic.ttf"
	// Italic is JetBrains Mono Italic.
	Italic = "fonts/JetBrainsMono/JetBrainsMono-Italic.ttf"
	// Light is JetBrains Mono Light.
	Light = "fonts/JetBrainsMono/JetBrainsMono-Light.ttf"
	// LightItalic is JetBrains Mono LightItalic.
	LightItalic = "fonts/JetBrainsMono/JetBrainsMono-LightItalic.ttf"
	// Medium is JetBrains Mono Medium.
	Medium = "fonts/JetBrainsMono/JetBrainsMono-Medium.ttf"
	// MediumItalic is JetBrains Mono MediumItalic.
	MediumItalic = "fonts/JetBrainsMono/JetBrainsMono-MediumItalic.ttf"
	// Regular is JetBrains Mono Regular.
	Regular = "fonts/JetBrainsMono/JetBrainsMono-Regular.ttf"
	// SemiBold is JetBrains Mono SemiBold.
	SemiBold = "fonts/JetBrainsMono/JetBrainsMono-SemiBold.ttf"
	// SemiBoldItalic is JetBrains Mono SemiBoldItalic.
	SemiBoldItalic = "fonts/JetBrainsMono/JetBrainsMono-SemiBoldItalic.ttf"
)
