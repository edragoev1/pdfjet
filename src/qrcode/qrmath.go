// qrmath.go
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

package qrcode

import "log"

// expTable and logTable are built once, when the package is loaded,
// and shared by every glog/gexp call. They used to be recomputed by
// NewQRMath() on every single invocation, which made encoding a QR
// Code dramatically slower than necessary.
var expTable [256]int
var logTable [256]int

func init() {
	for i := 0; i < 8; i++ {
		expTable[i] = 1 << i
	}
	for i := 8; i < 256; i++ {
		expTable[i] = expTable[i-4] ^
			expTable[i-5] ^
			expTable[i-6] ^
			expTable[i-8]
	}
	for i := 0; i < 255; i++ {
		logTable[expTable[i]] = i
	}
}

// glog returns the log value.
func glog(index int) int {
	if index < 1 {
		log.Fatal("The index value must be between 0 and 255.")
	}
	return logTable[index]
}

// gexp returns the exp value.
func gexp(n int) int {
	for n < 0 {
		n += 255
	}
	for n >= 256 {
		n -= 255
	}
	return expTable[n]
}
