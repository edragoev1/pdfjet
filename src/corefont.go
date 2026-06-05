package pdfjet

/**
 * corefont.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Used to select one of the 14 core fonts.
// See the Font class for more details.
const (
	Courier = iota
	CourierBold
	CourierOblique
	CourierBoldOblique
	Helvetica
	HelveticaBold
	HelveticaOblique
	HelveticaBoldOblique
	TimesRoman
	TimesBold
	TimesItalic
	TimesBoldItalic
	Symbol
	ZapfDingbats
)
