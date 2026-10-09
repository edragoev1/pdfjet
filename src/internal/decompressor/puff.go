// puff.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

/*
  puff.c

  Copyright 2002-2013 Mark Adler, all rights reserved
  version 2.3, 21 Jan 2013

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the author be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Mark Adler    madler@alumni.caltech.edu
*/

// puff.go is a Go translation of puff.c by Mark Adler, as Puff.swift and the
// Puff.cs of net48 are; all credit goes to the original author. It checks the
// Deflate data that InflatePrefix and InflateExact read only the start of,
// as zlib checks it: Go's compress/flate does not refuse a block whose code
// has no end of block, and stops before it needs one, where the decoders of
// the JDK, of .NET and of Swift refuse the block as they read its header
// (found by replaying the Go fuzz corpus, 8 October 2026).

package decompressor

import "errors"

const (
	puffMaxBits   = 15
	puffMaxLCodes = 286
	puffMaxDCodes = 30
	puffMaxCodes  = puffMaxLCodes + puffMaxDCodes
	puffFixLCodes = 288
)

var errPuffInvalid = errors.New("invalid Deflate data")

// The ends of a check: the bytes asked for are decoded, or the data ends
// before them, which the reader of compress/flate reports in its own words.
var (
	errPuffEnough     = errors.New("enough")
	errPuffOutOfInput = errors.New("out of input")
)

type puffHuffman struct {
	count  [puffMaxBits + 1]int16
	symbol []int16
}

type puff struct {
	input     []byte
	next      int
	bitBuffer int
	bitCount  int
	// The last 32 KB of the output, as far back as a distance reaches, and
	// how much was decoded: the output is not kept, only checked (the review
	// of 9 October 2026: kept whole, it took six times the image's size)
	window [1 << 15]byte
	total  int
	length int
}

// puffCheck decodes the raw Deflate data to its first length bytes, and
// returns an error when the data is invalid before them, as zlib finds it.
func puffCheck(raw []byte, length int) (err error) {
	p := &puff{input: raw, length: length}
	defer func() {
		if r := recover(); r != nil {
			e, ok := r.(error)
			if !ok || (e != errPuffInvalid && e != errPuffEnough && e != errPuffOutOfInput) {
				panic(r)
			}
			if e == errPuffInvalid {
				err = e
			}
		}
	}()
	for last := 0; last == 0; {
		last = p.bits(1)
		switch p.bits(2) {
		case 0:
			p.stored()
		case 1:
			p.fixed()
		case 2:
			p.dynamic()
		default:
			panic(errPuffInvalid)
		}
	}
	return nil
}

func (p *puff) put(value byte) {
	if p.total == p.length {
		panic(errPuffEnough)
	}
	p.window[p.total&(len(p.window)-1)] = value
	p.total++
}

func (p *puff) bits(need int) int {
	value := p.bitBuffer
	for p.bitCount < need {
		if p.next == len(p.input) {
			panic(errPuffOutOfInput)
		}
		value |= int(p.input[p.next]) << p.bitCount
		p.next++
		p.bitCount += 8
	}
	p.bitBuffer = value >> need
	p.bitCount -= need
	return value & (1<<need - 1)
}

func (p *puff) stored() {
	// Discard the leftover bits, and the block is whole bytes
	p.bitBuffer = 0
	p.bitCount = 0
	if p.next+4 > len(p.input) {
		panic(errPuffOutOfInput)
	}
	n := int(p.input[p.next]) | int(p.input[p.next+1])<<8
	complement := int(p.input[p.next+2]) | int(p.input[p.next+3])<<8
	p.next += 4
	if n != ^complement&0xffff {
		panic(errPuffInvalid)
	}
	for ; n > 0; n-- {
		if p.next == len(p.input) {
			panic(errPuffOutOfInput)
		}
		p.put(p.input[p.next])
		p.next++
	}
}

func (p *puff) decode(h *puffHuffman) int {
	code, first, index := 0, 0, 0
	for n := 1; n <= puffMaxBits; n++ {
		code |= p.bits(1)
		count := int(h.count[n])
		if code-count < first {
			return int(h.symbol[index+(code-first)])
		}
		index += count
		first += count
		first <<= 1
		code <<= 1
	}
	panic(errPuffInvalid)
}

// puffConstruct makes the canonical Huffman code of the lengths, and returns
// the number of codes left unused: 0 for a complete code, negative for one
// that is over-subscribed.
func puffConstruct(h *puffHuffman, lengths []int16) int {
	for i := range h.count {
		h.count[i] = 0
	}
	for _, n := range lengths {
		h.count[n]++
	}
	if int(h.count[0]) == len(lengths) {
		return 0
	}
	left := 1
	for n := 1; n <= puffMaxBits; n++ {
		left <<= 1
		left -= int(h.count[n])
		if left < 0 {
			return left
		}
	}
	var offs [puffMaxBits + 1]int16
	for n := 1; n < puffMaxBits; n++ {
		offs[n+1] = offs[n] + h.count[n]
	}
	for symbol, n := range lengths {
		if n != 0 {
			h.symbol[offs[n]] = int16(symbol)
			offs[n]++
		}
	}
	return left
}

var (
	puffLBase = [...]int{3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31,
		35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258}
	puffLExt = [...]int{0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2,
		3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0}
	puffDBase = [...]int{1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193,
		257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577}
	puffDExt = [...]int{0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6,
		7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13}
)

func (p *puff) codes(lencode, distcode *puffHuffman) {
	for {
		symbol := p.decode(lencode)
		if symbol < 0 {
			panic(errPuffInvalid)
		}
		if symbol < 256 {
			p.put(byte(symbol))
		} else if symbol == 256 {
			return
		} else {
			symbol -= 257
			if symbol >= 29 {
				panic(errPuffInvalid)
			}
			n := puffLBase[symbol] + p.bits(puffLExt[symbol])
			symbol = p.decode(distcode)
			if symbol < 0 || symbol >= 30 {
				panic(errPuffInvalid)
			}
			dist := puffDBase[symbol] + p.bits(puffDExt[symbol])
			if dist > p.total {
				panic(errPuffInvalid)
			}
			for ; n > 0; n-- {
				p.put(p.window[(p.total-dist)&(len(p.window)-1)])
			}
		}
	}
}

func (p *puff) fixed() {
	lencode := &puffHuffman{symbol: make([]int16, puffFixLCodes)}
	distcode := &puffHuffman{symbol: make([]int16, puffMaxDCodes)}
	lengths := make([]int16, puffFixLCodes)
	symbol := 0
	for ; symbol < 144; symbol++ {
		lengths[symbol] = 8
	}
	for ; symbol < 256; symbol++ {
		lengths[symbol] = 9
	}
	for ; symbol < 280; symbol++ {
		lengths[symbol] = 7
	}
	for ; symbol < puffFixLCodes; symbol++ {
		lengths[symbol] = 8
	}
	puffConstruct(lencode, lengths)
	for symbol = 0; symbol < puffMaxDCodes; symbol++ {
		lengths[symbol] = 5
	}
	puffConstruct(distcode, lengths[:puffMaxDCodes])
	p.codes(lencode, distcode)
}

var puffOrder = [...]int{16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15}

func (p *puff) dynamic() {
	lengths := make([]int16, puffMaxCodes)
	nlen := p.bits(5) + 257
	ndist := p.bits(5) + 1
	ncode := p.bits(4) + 4
	if nlen > puffMaxLCodes || ndist > puffMaxDCodes {
		panic(errPuffInvalid)
	}
	index := 0
	for ; index < ncode; index++ {
		lengths[puffOrder[index]] = int16(p.bits(3))
	}
	for ; index < 19; index++ {
		lengths[puffOrder[index]] = 0
	}
	lencode := &puffHuffman{symbol: make([]int16, puffMaxLCodes)}
	distcode := &puffHuffman{symbol: make([]int16, puffMaxDCodes)}
	if puffConstruct(lencode, lengths[:19]) != 0 {
		panic(errPuffInvalid)
	}
	index = 0
	for index < nlen+ndist {
		symbol := p.decode(lencode)
		if symbol < 0 {
			panic(errPuffInvalid)
		}
		if symbol < 16 {
			lengths[index] = int16(symbol)
			index++
			continue
		}
		n := int16(0)
		if symbol == 16 {
			if index == 0 {
				panic(errPuffInvalid)
			}
			n = lengths[index-1]
			symbol = 3 + p.bits(2)
		} else if symbol == 17 {
			symbol = 3 + p.bits(3)
		} else {
			symbol = 11 + p.bits(7)
		}
		if index+symbol > nlen+ndist {
			panic(errPuffInvalid)
		}
		for ; symbol > 0; symbol-- {
			lengths[index] = n
			index++
		}
	}
	// A code with no end of block, which compress/flate reads
	if lengths[256] == 0 {
		panic(errPuffInvalid)
	}
	if left := puffConstruct(lencode, lengths[:nlen]); left < 0 || (left > 0 && nlen-int(lencode.count[0]) != 1) {
		panic(errPuffInvalid)
	}
	if left := puffConstruct(distcode, lengths[nlen:nlen+ndist]); left < 0 || (left > 0 && ndist-int(distcode.count[0]) != 1) {
		panic(errPuffInvalid)
	}
	p.codes(lencode, distcode)
}
