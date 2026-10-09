// jpgscan.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// jpegScansWhole returns whether the scans of a sequential JPEG that has no
// end-of-image marker hold every block of its image: a JPEG whose only fault
// is the missing marker, which viewers draw, rather than one cut short in
// its upload or its copy. It reads the header from the start for the
// Huffman tables, the restart interval and the components, and walks the
// entropy-coded data of each scan, decoding the Huffman codes of each block
// and passing over the bits of their values, without decoding the image.
// Every step reads at least one bit, so the walk is no longer than the data.
// A table, a scan or a header it cannot read makes the JPEG not whole.
func jpegScansWhole(buffer []byte) bool {
	type component struct {
		id, h, v int
		seen     bool
	}
	var dc, ac [4]*jpegHuffman
	var components []*component
	width, height := 0, 0
	maxH, maxV := 1, 1
	restartInterval := 0
	index := 2 // After the start of the image
	marker := func() (int, bool) {
		for index < len(buffer) {
			if buffer[index] != 0xFF {
				index++
				continue
			}
			for index < len(buffer) && buffer[index] == 0xFF {
				index++
			}
			if index >= len(buffer) {
				return 0, false
			}
			ch := int(buffer[index])
			index++
			if ch != 0x00 {
				return ch, true
			}
		}
		return 0, false
	}
	segment := func() ([]byte, bool) {
		if index+2 > len(buffer) {
			return nil, false
		}
		length := int(buffer[index])<<8 | int(buffer[index+1])
		if length < 2 || index+length > len(buffer) {
			return nil, false
		}
		data := buffer[index+2 : index+length]
		index += length
		return data, true
	}
	scans := 0
	for {
		ch, ok := marker()
		if !ok {
			// The data ends after a whole scan, where the end-of-image
			// marker would be: every component must have been in one.
			if scans == 0 {
				return false
			}
			for _, c := range components {
				if !c.seen {
					return false
				}
			}
			return true
		}
		switch {
		case ch == int(mTEM) || ch == int(mSOI) || (ch >= int(mRST0) && ch <= int(mRST7)):
			continue
		case ch == int(mEOI):
			return scans > 0
		case ch == int(mSOF0) || ch == int(mSOF1):
			data, ok := segment()
			if !ok || len(data) < 6 || components != nil {
				return false
			}
			height = int(data[1])<<8 | int(data[2])
			width = int(data[3])<<8 | int(data[4])
			n := int(data[5])
			if width == 0 || height == 0 || len(data) < 6+3*n || n == 0 {
				return false
			}
			for i := 0; i < n; i++ {
				c := &component{id: int(data[6+3*i]), h: int(data[7+3*i] >> 4), v: int(data[7+3*i] & 15)}
				if c.h < 1 || c.h > 4 || c.v < 1 || c.v > 4 {
					return false
				}
				maxH, maxV = max(maxH, c.h), max(maxV, c.v)
				components = append(components, c)
			}
		case ch == 0xC4: // Define Huffman tables
			data, ok := segment()
			if !ok {
				return false
			}
			for len(data) > 0 {
				if len(data) < 17 {
					return false
				}
				class, id := int(data[0]>>4), int(data[0]&15)
				count := 0
				for i := 1; i <= 16; i++ {
					count += int(data[i])
				}
				if class > 1 || id > 3 || count > 256 || len(data) < 17+count {
					return false
				}
				table := newJPEGHuffman(data[1:17], data[17:17+count])
				if table == nil {
					return false
				}
				if class == 0 {
					dc[id] = table
				} else {
					ac[id] = table
				}
				data = data[17+count:]
			}
		case ch == 0xDD: // Define restart interval
			data, ok := segment()
			if !ok || len(data) < 2 {
				return false
			}
			restartInterval = int(data[0])<<8 | int(data[1])
		case ch == int(mSOS):
			data, ok := segment()
			if !ok || components == nil || len(data) < 1 {
				return false
			}
			n := int(data[0])
			if n < 1 || n > 4 || len(data) < 1+2*n+3 {
				return false
			}
			type part struct {
				c      *component
				dc, ac *jpegHuffman
			}
			parts := make([]part, 0, n)
			for i := 0; i < n; i++ {
				id, tables := int(data[1+2*i]), int(data[2+2*i])
				var c *component
				for _, each := range components {
					if each.id == id {
						c = each
					}
				}
				if c == nil || dc[tables>>4&3] == nil || ac[tables&3] == nil || tables>>4 > 3 || tables&15 > 3 {
					return false
				}
				parts = append(parts, part{c, dc[tables>>4], ac[tables&15]})
			}
			// The blocks of each unit of the scan, and the units across
			// and down: an interleaved scan's unit is an MCU, the blocks
			// of each component by its sampling; a scan of one component
			// has a unit of one block.
			ceilDiv := func(a, b int) int { return (a + b - 1) / b }
			units := 0
			if n == 1 {
				c := parts[0].c
				w := ceilDiv(ceilDiv(width*c.h, maxH), 8)
				h := ceilDiv(ceilDiv(height*c.v, maxV), 8)
				units = w * h
			} else {
				units = ceilDiv(width, 8*maxH) * ceilDiv(height, 8*maxV)
			}
			reader := &jpegBitReader{buffer: buffer, index: index}
			for u := 0; u < units; u++ {
				if restartInterval > 0 && u > 0 && u%restartInterval == 0 && !reader.restart() {
					return false
				}
				for _, p := range parts {
					blocks := 1
					if n > 1 {
						blocks = p.c.h * p.c.v
					}
					for b := 0; b < blocks; b++ {
						if !reader.block(p.dc, p.ac) {
							return false
						}
					}
				}
			}
			for _, p := range parts {
				p.c.seen = true
			}
			scans++
			index = reader.index
		default:
			// SOF2 and the other frames are not walked: the caller asks only
			// of a sequential JPEG.
			if ch >= 0xC0 && ch <= 0xCF && ch != 0xC4 && ch != 0xC8 && ch != 0xCC {
				return false
			}
			if _, ok := segment(); !ok {
				return false
			}
		}
	}
}

// jpegHuffman is a Huffman table of a JPEG, as the JPEG standard decodes it
// (Annex F.2.2.3): for each length, the smallest and the largest code, and
// where its symbols begin.
type jpegHuffman struct {
	minCode, maxCode [17]int
	valPtr           [17]int
	symbols          []byte
}

func newJPEGHuffman(counts, symbols []byte) *jpegHuffman {
	h := &jpegHuffman{symbols: symbols}
	code, k := 0, 0
	for length := 1; length <= 16; length++ {
		n := int(counts[length-1])
		h.valPtr[length] = k
		h.minCode[length] = code
		code += n
		k += n
		h.maxCode[length] = code - 1
		if n == 0 {
			h.maxCode[length] = -1
		}
		if code > 1<<length {
			return nil // More codes than the length has
		}
		code <<= 1
	}
	return h
}

// jpegBitReader reads the entropy-coded data of a scan a bit at a time: a
// 0xFF is followed by a 0x00 that is not data, and any other marker ends the
// data, which a block that needs more bits does not survive.
type jpegBitReader struct {
	buffer []byte
	index  int
	bits   int
	count  int
}

func (r *jpegBitReader) bit() (int, bool) {
	if r.count == 0 {
		if r.index >= len(r.buffer) {
			return 0, false
		}
		b := r.buffer[r.index]
		if b == 0xFF {
			if r.index+1 >= len(r.buffer) || r.buffer[r.index+1] != 0x00 {
				return 0, false // A marker, or the end of the data
			}
			r.index++
		}
		r.index++
		r.bits, r.count = int(b), 8
	}
	r.count--
	return (r.bits >> r.count) & 1, true
}

func (r *jpegBitReader) skip(n int) bool {
	for ; n > 0; n-- {
		if _, ok := r.bit(); !ok {
			return false
		}
	}
	return true
}

func (r *jpegBitReader) decode(h *jpegHuffman) (int, bool) {
	code := 0
	for length := 1; length <= 16; length++ {
		b, ok := r.bit()
		if !ok {
			return 0, false
		}
		code = code<<1 | b
		if code <= h.maxCode[length] {
			k := h.valPtr[length] + code - h.minCode[length]
			if k < 0 || k >= len(h.symbols) {
				return 0, false
			}
			return int(h.symbols[k]), true
		}
	}
	return 0, false
}

// block passes over the codes of one block of 64 coefficients.
func (r *jpegBitReader) block(dc, ac *jpegHuffman) bool {
	s, ok := r.decode(dc)
	if !ok || s > 11 || !r.skip(s) {
		return false
	}
	for k := 1; k < 64; {
		rs, ok := r.decode(ac)
		if !ok {
			return false
		}
		run, size := rs>>4, rs&15
		if size == 0 {
			if run != 15 {
				return true // The end of the block
			}
			k += 16
			continue
		}
		k += run
		if k > 63 || !r.skip(size) {
			return false
		}
		k++
	}
	return true
}

// restart goes past the restart marker that ends an interval, the bits left
// in the byte before it being fill.
func (r *jpegBitReader) restart() bool {
	r.count = 0
	for r.index+1 < len(r.buffer) && r.buffer[r.index] == 0xFF && r.buffer[r.index+1] == 0xFF {
		r.index++ // A fill byte before the marker
	}
	if r.index+1 >= len(r.buffer) || r.buffer[r.index] != 0xFF ||
		r.buffer[r.index+1] < mRST0 || r.buffer[r.index+1] > mRST7 {
		return false
	}
	r.index += 2
	return true
}
