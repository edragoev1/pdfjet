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

// Polynomial describes polynomial structure.
type Polynomial struct {
	num []int
}

// NewPolynomial constructs polynomial object.
func NewPolynomial(num []int, shift int) *Polynomial {
	polynomial := new(Polynomial)
	offset := 0
	for offset < len(num) && num[offset] == 0 {
		offset++
	}
	polynomial.num = make([]int, len(num)-offset+shift)
	for i := 0; i < len(num)-offset; i++ {
		polynomial.num[i] = num[offset+i]
	}
	return polynomial
}

func (polynomial *Polynomial) get(index int) int {
	return polynomial.num[index]
}

// getLength returns the length.
func (polynomial *Polynomial) getLength() int {
	return len(polynomial.num)
}

func (polynomial *Polynomial) multiply(e *Polynomial) *Polynomial {
	num := make([]int, polynomial.getLength()+e.getLength()-1)
	for i := 0; i < polynomial.getLength(); i++ {
		for j := 0; j < e.getLength(); j++ {
			num[i+j] ^=
				NewQRMath().gexp(NewQRMath().glog(polynomial.get(i)) +
					NewQRMath().glog(e.get(j)))
		}
	}
	return NewPolynomial(num, 0)
}

func (polynomial *Polynomial) mod(e *Polynomial) *Polynomial {
	if polynomial.getLength()-e.getLength() < 0 {
		return polynomial
	}

	ratio := NewQRMath().glog(polynomial.get(0)) - NewQRMath().glog(e.get(0))
	num := make([]int, polynomial.getLength())
	for i := 0; i < polynomial.getLength(); i++ {
		num[i] = polynomial.get(i)
	}

	for i := 0; i < e.getLength(); i++ {
		num[i] ^= NewQRMath().gexp(NewQRMath().glog(e.get(i)) + ratio)
	}

	return NewPolynomial(num, 0).mod(e)
}
