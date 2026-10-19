// blendmode.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package blendmode defines the blend modes of a graphics state: how what is
// drawn is mixed with what is already on the page. Multiply, for example,
// draws like a highlighter pen that leaves the text under it readable.
package blendmode

// BlendMode is a blend mode, its name in the PDF. The zero value, "", is none:
// a graphics state of no blend mode writes no /BM entry.
type BlendMode string

// The blend modes of the PDF.
const (
	Normal     BlendMode = "Normal"     // Paints over what is under it: the default.
	Multiply   BlendMode = "Multiply"   // Multiplies the colors: always darker, like a highlighter.
	Screen     BlendMode = "Screen"     // Multiplies the inverse of the colors: always lighter.
	Overlay    BlendMode = "Overlay"    // MULTIPLY or SCREEN, by the color under it.
	Darken     BlendMode = "Darken"     // The darker of the two colors.
	Lighten    BlendMode = "Lighten"    // The lighter of the two colors.
	ColorDodge BlendMode = "ColorDodge" // Brightens the color under it.
	ColorBurn  BlendMode = "ColorBurn"  // Darkens the color under it.
	HardLight  BlendMode = "HardLight"  // MULTIPLY or SCREEN, by the color drawn.
	SoftLight  BlendMode = "SoftLight"  // A softer HARD_LIGHT.
	Difference BlendMode = "Difference" // The difference of the two colors.
	Exclusion  BlendMode = "Exclusion"  // Like DIFFERENCE, with less contrast.
	Hue        BlendMode = "Hue"        // The hue of the color drawn, the rest of the color under it.
	Saturation BlendMode = "Saturation" // The saturation of the color drawn, the rest of the color under it.
	Color      BlendMode = "Color"      // The hue and saturation of the color drawn.
	Luminosity BlendMode = "Luminosity" // The luminosity of the color drawn.
)
