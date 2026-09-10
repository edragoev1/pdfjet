// cjkfont.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package cjkfont defines the Chinese, Japanese and Korean fonts.
package cjkfont

// Font is used to select Chinese, Japanese and Korean fonts.
// See the NewCJKFont constructor in the pdfjet package.
type Font int

// The Chinese, Japanese and Korean fonts.
const (
	// AdobeMingStdLight is Chinese (Traditional) font
	AdobeMingStdLight Font = iota

	// STHeitiSCLight is Chinese (Simplified) font
	STHeitiSCLight

	// KozMinProVIRegular is Japanese font
	KozMinProVIRegular

	// AdobeMyungjoStdMedium is Korean font
	AdobeMyungjoStdMedium
)
