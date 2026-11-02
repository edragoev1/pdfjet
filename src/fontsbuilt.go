// fontsbuilt.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build !pdfjet_nofonts

package pdfjet

// withoutBundledFonts is true in a program built with the tag pdfjet_nofonts,
// which leaves out the fonts PDFjet carries (resources/fonts).
const withoutBundledFonts = false
