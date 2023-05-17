package dproject

/**
 *
Copyright (c) 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/
import "log"

// QRMath describes the QRMath structure.
type QRMath struct {
	expTable [256]int
	logTable [256]int
}

// NewQRMath constructs QRMath object.
func NewQRMath() *QRMath {
	qrmath := new(QRMath)
	for i := 0; i < 8; i++ {
		qrmath.expTable[i] = 1 << i
	}
	for i := 8; i < 256; i++ {
		qrmath.expTable[i] = qrmath.expTable[i-4] ^
			qrmath.expTable[i-5] ^
			qrmath.expTable[i-6] ^
			qrmath.expTable[i-8]
	}
	for i := 0; i < 255; i++ {
		qrmath.logTable[qrmath.expTable[i]] = i
	}
	return qrmath
}

// Glog returns the log value.
func (qrmath *QRMath) glog(index int) int {
	if index < 1 {
		log.Fatal("The index value must be between 0 and 255.")
	}
	return qrmath.logTable[index]
}

// Gexp returns the exp value.
func (qrmath *QRMath) gexp(n int) int {
	for n < 0 {
		n += 255
	}
	for n >= 256 {
		n -= 255
	}
	return qrmath.expTable[n]
}
