// errorcorrectionlevel.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//
// Original author: Kazuhiko Arase, 2009
// URL: http://www.d-project.com/
// Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
//
// The word "QR Code" is a registered trademark of
// DENSO WAVE INCORPORATED
// http://www.denso-wave.com/qrcode/faqpatent-e.html
//
// Modified and adapted for use in PDFjet by PDFjet Software

// Package errorcorrectionlevel defines the error correction levels of a QR code.
package errorcorrectionlevel

// ErrorCorrectionLevel is the error correction level of a QR code. The values
// are the bits of the level in the format information.
type ErrorCorrectionLevel int

// L, M, Q and H are the error correction levels of a QR code, as
// ErrorCorrectionLevel.L and so on in the other ports.
const (
	L ErrorCorrectionLevel = 1 // Recovers about 7% of the data
	M ErrorCorrectionLevel = 0 // Recovers about 15% of the data
	Q ErrorCorrectionLevel = 3 // Recovers about 25% of the data
	H ErrorCorrectionLevel = 2 // Recovers about 30% of the data
)
