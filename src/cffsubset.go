// cffsubset.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// The subset of a font with CFF outlines, a .otf: a CIDFont of the glyphs the
// document draws alone, its charset giving each glyph its number in the
// whole font as its CID, by which the PDF draws it, and the subroutines,
// global and local, that the glyphs kept do not call emptied to no bytes,
// keeping their numbers, so the glyphs kept are drawn by the same bytes as in
// the whole font. A name-keyed font becomes a CIDFont of Adobe-Identity-0,
// with the identity matrix in its one Font DICT if it has a FontMatrix of its
// own.
// The CFF is written again in a fixed order, with every offset of its Top
// DICT and of its Font DICTs in the five-byte form, so that an offset of any
// value has the same size: the header, the Name, Top DICT, String and Global
// Subr INDEXes, the charset, the FDSelect, the new CharStrings INDEX, the
// FDArray, and each Private DICT with its local subroutines.

// cffIndex is an INDEX of a CFF table: where it starts and ends, and the
// offsets of its objects in the table.
type cffIndex struct {
	start, end int
	objects    []int // count+1 offsets in the table, the last one the end of the data
}

// readCFFIndex reads the INDEX at the offset.
func readCFFIndex(cff []byte, at int) (cffIndex, error) {
	if at < 0 || at+2 > len(cff) {
		return cffIndex{}, errNotSubset
	}
	count := int(cff[at])<<8 | int(cff[at+1])
	if count == 0 {
		return cffIndex{start: at, end: at + 2, objects: []int{at + 2}}, nil
	}
	if at+3 > len(cff) {
		return cffIndex{}, errNotSubset
	}
	offSize := int(cff[at+2])
	if offSize < 1 || offSize > 4 || at+3+(count+1)*offSize > len(cff) {
		return cffIndex{}, errNotSubset
	}
	data := at + 3 + (count+1)*offSize - 1 // The offsets count from 1.
	objects := make([]int, count+1)
	for i := range objects {
		offset := 0
		for j := 0; j < offSize; j++ {
			offset = offset<<8 | int(cff[at+3+i*offSize+j])
		}
		objects[i] = data + offset
		if offset < 1 || objects[i] > len(cff) || (i > 0 && objects[i] < objects[i-1]) {
			return cffIndex{}, errNotSubset
		}
	}
	return cffIndex{start: at, end: objects[count], objects: objects}, nil
}

// appendCFFIndex appends an INDEX of the objects.
func appendCFFIndex(out []byte, objects [][]byte) []byte {
	out = append(out, byte(len(objects)>>8), byte(len(objects)))
	if len(objects) == 0 {
		return out
	}
	size := 1
	for _, object := range objects {
		size += len(object)
	}
	offSize := 1
	for size >= 1<<(8*offSize) {
		offSize++
	}
	out = append(out, byte(offSize))
	offset := 1
	for i := 0; i <= len(objects); i++ {
		for j := offSize - 1; j >= 0; j-- {
			out = append(out, byte(offset>>(8*j)))
		}
		if i < len(objects) {
			offset += len(objects[i])
		}
	}
	for _, object := range objects {
		out = append(out, object...)
	}
	return out
}

// cffEntry is an entry of a DICT: its operator, 12 and the second byte of an
// escaped one as 12<<8 | b1, the bytes of its operands and of its operator, and
// its operands when they are integers.
type cffEntry struct {
	op       int
	raw      []byte
	integers []int
}

// The operators whose operands are offsets in the CFF table.
const (
	cffCharset     = 15
	cffEncoding    = 16
	cffCharStrings = 17
	cffPrivate     = 18
	cffSubrs       = 19
	cffFDArray     = 12<<8 | 36
	cffFDSelect    = 12<<8 | 37
	cffROS         = 12<<8 | 30
	cffCIDCount    = 12<<8 | 34
	cffFontMatrix  = 12<<8 | 7
)

// readCFFDict reads the entries of a DICT.
func readCFFDict(dict []byte) ([]cffEntry, error) {
	var entries []cffEntry
	start := 0
	var integers []int
	isInteger := true
	for i := 0; i < len(dict); {
		b0 := int(dict[i])
		switch {
		case b0 <= 21: // An operator
			op := b0
			i++
			if b0 == 12 {
				if i >= len(dict) {
					return nil, errNotSubset
				}
				op = 12<<8 | int(dict[i])
				i++
			}
			entry := cffEntry{op: op, raw: dict[start:i]}
			if isInteger {
				// An operator with no operands is there, with none, as in
				// the other ports, not absent (the review of 9 October 2026:
				// a Subrs of no operands was dropped, and its subroutines)
				entry.integers = integers
				if entry.integers == nil {
					entry.integers = []int{}
				}
			}
			entries = append(entries, entry)
			start = i
			integers = nil
			isInteger = true
		case b0 == 28:
			if i+3 > len(dict) {
				return nil, errNotSubset
			}
			integers = append(integers, int(int16(uint16(dict[i+1])<<8|uint16(dict[i+2]))))
			i += 3
		case b0 == 29:
			if i+5 > len(dict) {
				return nil, errNotSubset
			}
			integers = append(integers, int(int32(uint32(dict[i+1])<<24|uint32(dict[i+2])<<16|uint32(dict[i+3])<<8|uint32(dict[i+4]))))
			i += 5
		case b0 == 30: // A real, in nibbles up to one of 0xF
			isInteger = false
			i++
			for ; i < len(dict) && dict[i]&0x0F != 0x0F && dict[i]&0xF0 != 0xF0; i++ {
			}
			if i >= len(dict) {
				return nil, errNotSubset
			}
			i++
		case b0 >= 32 && b0 <= 246:
			integers = append(integers, b0-139)
			i++
		case b0 >= 247 && b0 <= 250:
			if i+2 > len(dict) {
				return nil, errNotSubset
			}
			integers = append(integers, (b0-247)*256+int(dict[i+1])+108)
			i += 2
		case b0 >= 251 && b0 <= 254:
			if i+2 > len(dict) {
				return nil, errNotSubset
			}
			integers = append(integers, -(b0-251)*256-int(dict[i+1])-108)
			i += 2
		default:
			return nil, errNotSubset
		}
	}
	if start != len(dict) {
		return nil, errNotSubset // Operands without an operator
	}
	return entries, nil
}

// cffEntryOf returns the integer operands of the entry of the operator, or
// nil.
func cffEntryOf(entries []cffEntry, op int) []int {
	for _, entry := range entries {
		if entry.op == op {
			return entry.integers
		}
	}
	return nil
}

// appendCFFOffset appends the integer in the five-byte form.
func appendCFFOffset(out []byte, value int) []byte {
	return append(out, 29, byte(value>>24), byte(value>>16), byte(value>>8), byte(value))
}

// writeCFFDict writes the entries again, the operands of the operators in
// offsets given in the five-byte form: the offset, or for Private its size
// and its offset.
func writeCFFDict(entries []cffEntry, offsets map[int]int, privateSize int) []byte {
	var out []byte
	for _, entry := range entries {
		offset, ok := offsets[entry.op]
		if !ok {
			out = append(out, entry.raw...)
			continue
		}
		if entry.op == cffPrivate {
			out = appendCFFOffset(out, privateSize)
		}
		out = appendCFFOffset(out, offset)
		if entry.op > 0xFF {
			out = append(out, 12, byte(entry.op))
		} else {
			out = append(out, byte(entry.op))
		}
	}
	return out
}

// cffFDSelectLength returns the length of the FDSelect at the offset.
func cffFDSelectLength(cff []byte, at, numGlyphs int) (int, error) {
	if at < 0 || at >= len(cff) {
		return 0, errNotSubset
	}
	switch cff[at] {
	case 0:
		return 1 + numGlyphs, nil
	case 3:
		if at+3 > len(cff) {
			return 0, errNotSubset
		}
		ranges := int(cff[at+1])<<8 | int(cff[at+2])
		return 1 + 2 + 3*ranges + 2, nil
	}
	return 0, errNotSubset
}

// cffBlock returns the bytes of the table from the offset, of the length.
func cffBlock(cff []byte, at, length int) ([]byte, error) {
	if at < 0 || length < 0 || at+length > len(cff) {
		return nil, errNotSubset
	}
	return cff[at : at+length], nil
}

// cffPrivateDict is a Private DICT, its entries and the INDEX of its local
// subroutines, or none.
type cffPrivateDict struct {
	entries []cffEntry
	subrs   *cffIndex
}

// readCFFPrivate reads the Private DICT of the size at the offset.
func readCFFPrivate(cff []byte, size, at int) (cffPrivateDict, error) {
	dict, err := cffBlock(cff, at, size)
	if err != nil {
		return cffPrivateDict{}, err
	}
	entries, err := readCFFDict(dict)
	if err != nil {
		return cffPrivateDict{}, err
	}
	subrs := cffEntryOf(entries, cffSubrs)
	if subrs == nil {
		return cffPrivateDict{entries: entries}, nil
	}
	if len(subrs) != 1 {
		return cffPrivateDict{}, errNotSubset
	}
	index, err := readCFFIndex(cff, at+subrs[0])
	if err != nil {
		return cffPrivateDict{}, err
	}
	return cffPrivateDict{entries: entries, subrs: &index}, nil
}

// cffBias returns the bias of the subroutine numbers of an INDEX of the count.
func cffBias(count int) int {
	if count < 1240 {
		return 107
	} else if count < 33900 {
		return 1131
	}
	return 32768
}

// cffMaxWork is the most bytes of charstrings the subroutines are looked for
// in, which a font of nested calls that come back to the same subroutines
// could otherwise make take as long as it likes.
const cffMaxWork = 1 << 24

// cffUsage finds the subroutines that the charstrings of the glyphs kept call,
// directly or through other subroutines.
type cffUsage struct {
	cff     []byte
	global  cffIndex
	local   *cffIndex
	usedG   []bool
	usedL   []bool
	stack   int // The operands on the stack
	last    int // The last of them, a subroutine number when one is called
	stems   int
	work    int
	err     error
	stopped bool // By endchar
	seac    bool // An accented letter drawn from two others
}

// run reads the charstring, and the subroutines it calls.
func (u *cffUsage) run(cs []byte, depth int) {
	if depth > 10 { // Type 2 nests subroutines 10 deep at most.
		u.err = errNotSubset
		return
	}
	u.work += len(cs)
	if u.work > cffMaxWork {
		u.err = errNotSubset
		return
	}
	for i := 0; i < len(cs) && u.err == nil && !u.stopped; {
		b0 := int(cs[i])
		switch {
		case b0 == 28:
			if i+3 > len(cs) {
				u.err = errNotSubset
				return
			}
			u.last = int(int16(uint16(cs[i+1])<<8 | uint16(cs[i+2])))
			u.stack++
			i += 3
		case b0 >= 32 && b0 <= 246:
			u.last = b0 - 139
			u.stack++
			i++
		case b0 >= 247 && b0 <= 250:
			if i+2 > len(cs) {
				u.err = errNotSubset
				return
			}
			u.last = (b0-247)*256 + int(cs[i+1]) + 108
			u.stack++
			i += 2
		case b0 >= 251 && b0 <= 254:
			if i+2 > len(cs) {
				u.err = errNotSubset
				return
			}
			u.last = -(b0-251)*256 - int(cs[i+1]) - 108
			u.stack++
			i += 2
		case b0 == 255: // A 16.16 fixed number
			if i+5 > len(cs) {
				u.err = errNotSubset
				return
			}
			u.last = int(int32(uint32(cs[i+1])<<24|uint32(cs[i+2])<<16|uint32(cs[i+3])<<8|uint32(cs[i+4]))) >> 16
			u.stack++
			i += 5
		case b0 == 1 || b0 == 3 || b0 == 18 || b0 == 23: // The stem hints
			u.stems += u.stack / 2
			u.stack = 0
			i++
		case b0 == 19 || b0 == 20: // hintmask and cntrmask, and their mask
			u.stems += u.stack / 2
			u.stack = 0
			i += 1 + (u.stems+7)/8
		case b0 == 10 || b0 == 29: // callsubr and callgsubr
			if u.stack == 0 {
				u.err = errNotSubset
				return
			}
			u.stack--
			subrs, used := u.local, u.usedL
			if b0 == 29 {
				subrs, used = &u.global, u.usedG
			}
			if subrs == nil {
				u.err = errNotSubset
				return
			}
			count := len(subrs.objects) - 1
			number := u.last + cffBias(count)
			if number < 0 || number >= count {
				u.err = errNotSubset
				return
			}
			used[number] = true
			u.run(u.cff[subrs.objects[number]:subrs.objects[number+1]], depth+1)
			i++
		case b0 == 11: // return
			return
		case b0 == 14: // endchar
			if u.stack >= 4 {
				// seac, an accented letter drawn from two others, which
				// would have to be kept too: the font is embedded whole
				// (the review of 9 October 2026: the letter drew blank)
				u.seac = true
				u.err = errNotSubset
			}
			u.stopped = true
			return
		case b0 == 12: // An escaped operator
			u.stack = 0
			i += 2
		default:
			u.stack = 0
			i++
		}
	}
}

// cffDesubroutinize tells whether the glyphs of a subset are written with
// the subroutines they call in place of their calls, and the subset without
// subroutines. Off: a PDF of such subsets is smaller, by 1 to 4% for a page
// of Latin, Cyrillic or Greek and 17 to 56% for 100 to 2000 characters of
// Korean or Chinese, and as fast to write, or 10 to 20% faster up to 500
// characters, but about 6% slower for 2000, where the larger charstrings
// take longer to compress (14 November 2026).
var cffDesubroutinize = false

// cffInliner writes charstrings again with each subroutine they call, local
// or global, in place of its call (Technical Note #5177): the number of the
// subroutine taken off the end, the bytes of the subroutine, its own calls
// in place too, without its return. The charstring written draws the glyph as
// the operators of the one it was written from do, run with its subroutines,
// one after another. A charstring that computes a number, by an arithmetic
// or storage operator, cannot be written so: an error.
type cffInliner struct {
	cff     []byte
	global  cffIndex
	local   *cffIndex
	out     []byte  // The charstrings written, one after another
	numbers [48]int // Where each operand on the stack starts in out
	stack   int     // The operands on the stack, of the 48 of Type 2 at most
	stems   int
	work    int
	ended   bool // By endchar
	err     error
	seac    bool
}

// glyph returns the charstring written again with its subroutines in place,
// after the charstrings written before it in out, which holds them all.
func (d *cffInliner) glyph(cs []byte) []byte {
	start := len(d.out)
	d.stack, d.stems, d.ended = 0, 0, false
	d.run(cs, 0)
	return d.out[start:len(d.out):len(d.out)]
}

// run writes the charstring, and the subroutines it calls in place. Its bytes
// are written in runs, from one call of a subroutine to the next.
func (d *cffInliner) run(cs []byte, depth int) {
	if depth > 10 { // Type 2 nests subroutines 10 deep at most.
		d.err = errNotSubset
		return
	}
	from := 0 // The bytes of the charstring from here on are not written yet.
	i := 0
	for i < len(cs) {
		b0 := cs[i]
		if b0 >= 28 && b0 != 29 && b0 != 30 && b0 != 31 { // A number
			n := 1
			switch {
			case b0 == 28:
				n = 3
			case b0 == 255:
				n = 5
			case b0 >= 247:
				n = 2
			}
			if i+n > len(cs) || d.stack == len(d.numbers) {
				d.err = errNotSubset
				return
			}
			d.numbers[d.stack] = len(d.out) + i - from
			d.stack++
			i += n
			continue
		}
		n := 1 // The bytes of the operator
		switch b0 {
		case 1, 3, 18, 23: // The stem hints
			d.stems += d.stack / 2
		case 19, 20: // hintmask and cntrmask, and their mask
			d.stems += d.stack / 2
			n += (d.stems + 7) / 8
		case 10, 29: // callsubr and callgsubr
			if d.stack == 0 {
				d.err = errNotSubset
				return
			}
			d.out = append(d.out, cs[from:i]...)
			d.stack--
			at := d.numbers[d.stack]
			last := cffNumber(d.out[at:])
			d.out = d.out[:at]
			subrs := d.local
			if b0 == 29 {
				subrs = &d.global
			}
			if subrs == nil {
				d.err = errNotSubset
				return
			}
			count := len(subrs.objects) - 1
			number := last + cffBias(count)
			if number < 0 || number >= count {
				d.err = errNotSubset
				return
			}
			subr := d.cff[subrs.objects[number]:subrs.objects[number+1]]
			d.work += len(subr)
			if d.work > cffMaxWork {
				d.err = errNotSubset
				return
			}
			d.run(subr, depth+1)
			if d.err != nil || d.ended {
				return
			}
			i++
			from = i
			continue
		case 11: // return
			d.out = append(d.out, cs[from:i]...)
			return
		case 14: // endchar
			if d.stack >= 4 {
				// seac, an accented letter drawn from two others: the
				// font is embedded whole
				d.seac = true
				d.err = errNotSubset
				return
			}
			d.out = append(d.out, cs[from:i+1]...)
			d.ended = true
			return
		case 12:
			n = 2
			if i+1 < len(cs) {
				switch cs[i+1] {
				case 3, 4, 5, 9, 10, 11, 12, 14, 15, 18, 20, 21, 22, 23, 24, 26, 27, 28, 29, 30:
					// The arithmetic and storage operators, which leave
					// numbers on the stack
					d.err = errNotSubset
					return
				}
			}
		}
		if i+n > len(cs) {
			d.err = errNotSubset
			return
		}
		// An operator, which takes its operands off the stack
		d.stack = 0
		i += n
	}
	d.out = append(d.out, cs[from:i]...)
}

// cffNumber returns the integer part of the number of a charstring the bytes
// start with.
func cffNumber(b []byte) int {
	b0 := int(b[0])
	switch {
	case b0 == 28:
		return int(int16(uint16(b[1])<<8 | uint16(b[2])))
	case b0 >= 32 && b0 <= 246:
		return b0 - 139
	case b0 >= 247 && b0 <= 250:
		return (b0-247)*256 + int(b[1]) + 108
	case b0 >= 251 && b0 <= 254:
		return -(b0-251)*256 - int(b[1]) - 108
	}
	return int(int32(uint32(b[1])<<24|uint32(b[2])<<16|uint32(b[3])<<8|uint32(b[4]))) >> 16
}

// cffIdentityMatrix is the FontMatrix entry [1 0 0 1 0 0].
var cffIdentityMatrix = []byte{140, 139, 139, 140, 139, 139, 12, 7}

// emptiedCFFIndex returns the objects of the INDEX, those not used emptied to
// no bytes, which keeps their numbers and makes their offsets the same as the
// next one's, or every one for used nil.
func emptiedCFFIndex(cff []byte, index cffIndex, used []bool) [][]byte {
	objects := make([][]byte, len(index.objects)-1)
	for i := range objects {
		if used == nil || used[i] {
			objects[i] = cff[index.objects[i]:index.objects[i+1]]
		} else {
			objects[i] = []byte{}
		}
	}
	return objects
}

// cffFDOf returns the Font DICT of each glyph, from the FDSelect of a CID-keyed
// font, or nil for a name-keyed one.
func cffFDOf(fdSelect []byte, numGlyphs, fds int) ([]int, error) {
	if fdSelect == nil {
		return nil, nil
	}
	fdOf := make([]int, numGlyphs)
	switch fdSelect[0] {
	case 0:
		for gid := range fdOf {
			fdOf[gid] = int(fdSelect[1+gid])
		}
	case 3:
		ranges := int(fdSelect[1])<<8 | int(fdSelect[2])
		for r := 0; r < ranges; r++ {
			first := int(fdSelect[3+3*r])<<8 | int(fdSelect[4+3*r])
			next := int(fdSelect[6+3*r])<<8 | int(fdSelect[7+3*r]) // The first of the next range, or the sentinel
			fd := int(fdSelect[5+3*r])
			if first > next || next > numGlyphs {
				return nil, errNotSubset
			}
			for gid := first; gid < next; gid++ {
				fdOf[gid] = fd
			}
		}
	}
	for _, fd := range fdOf {
		if fd >= fds {
			return nil, errNotSubset
		}
	}
	return fdOf, nil
}

// subsetCFF returns the CFF table of the glyphs used and glyph 0, or of every
// glyph for used nil, and the glyphs kept, by their numbers in the font. The
// subset is a CIDFont of the glyphs kept alone, in the order of their
// numbers, its charset giving each its number in the font as its CID: the
// PDF draws a glyph of a CIDFontType0C program by its CID, which the charset
// maps to the glyph (ISO 32000-2, 9.7.4.2; Technical Note #5176, section 18,
// "Subset CIDFonts will generally need to use a more complex charset table
// representing a non-identity mapping"), so the content streams, the widths
// and the ToUnicode map stay those of the whole font. A name-keyed font is
// written as a CIDFont of the ordering Adobe-Identity-0 with one Font DICT,
// its Private DICT. The subroutines the glyphs kept do not call are emptied
// to no bytes, keeping their numbers, and the Font DICTs no glyph kept uses
// are left out. Every INDEX has the least offSize its offsets need; the
// offsets of the Top DICT and of the Font DICTs are in the five-byte form. A
// name-keyed font's FontMatrix of its own, as Cascadia Mono's of 2048 units
// to the em, stays in its Top DICT, and its Font DICT has the FontMatrix [1 0
// 0 1 0 0], written: the glyph space of a CIDFont is that of the FontMatrix
// of the glyph's Font DICT concatenated with the Top DICT's (PostScript
// Language Reference, third edition, 5.11.3, Table 5.14), the Top DICT's
// interpreting the coordinates of the CIDFont as a whole, as its FontBBox.
// Readers take a Font DICT of no FontMatrix as the identity, as they do for
// the other name-keyed fonts, but Technical Note #5176 gives a FontMatrix no
// default but the Top DICT's, [0.001 0 0 0.001 0 0] (Table 9), so the
// identity is written. A CID-keyed font's matrices are kept as they are. A table that cannot be
// read gives an error.
func subsetCFF(cff []byte, used []bool) ([]byte, []bool, error) {
	if len(cff) < 4 || cff[0] != 1 {
		return nil, nil, errNotSubset // Not CFF version 1
	}
	headerSize := int(cff[2])
	names, err := readCFFIndex(cff, headerSize)
	if err != nil {
		return nil, nil, err
	}
	topDicts, err := readCFFIndex(cff, names.end)
	if err != nil || len(topDicts.objects) != 2 { // One font
		return nil, nil, errNotSubset
	}
	strings, err := readCFFIndex(cff, topDicts.end)
	if err != nil {
		return nil, nil, err
	}
	globalSubrs, err := readCFFIndex(cff, strings.end)
	if err != nil {
		return nil, nil, err
	}
	top, err := readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	if err != nil {
		return nil, nil, err
	}
	charStringsAt := cffEntryOf(top, cffCharStrings)
	if len(charStringsAt) != 1 {
		return nil, nil, errNotSubset
	}
	charStrings, err := readCFFIndex(cff, charStringsAt[0])
	if err != nil {
		return nil, nil, err
	}
	numGlyphs := len(charStrings.objects) - 1
	if numGlyphs == 0 {
		return nil, nil, errNotSubset
	}

	cidKeyed := cffEntryOf(top, cffROS) != nil
	var fdSelect []byte
	if cidKeyed {
		if count := cffEntryOf(top, cffCIDCount); count != nil && (len(count) != 1 || count[0] < numGlyphs) {
			return nil, nil, errNotSubset
		}
		if count := cffEntryOf(top, cffCIDCount); count == nil && numGlyphs > 8720 {
			return nil, nil, errNotSubset // The default CIDCount
		}
		at := cffEntryOf(top, cffFDSelect)
		if len(at) != 1 {
			return nil, nil, errNotSubset
		}
		length, err := cffFDSelectLength(cff, at[0], numGlyphs)
		if err == nil {
			fdSelect, err = cffBlock(cff, at[0], length)
		}
		if err != nil {
			return nil, nil, err
		}
	}
	// A name-keyed font's FontMatrix of its own stays in the Top DICT, and
	// its one Font DICT has the identity matrix, written.
	hasMatrix := !cidKeyed && cffHasEntry(top, cffFontMatrix)

	// The Font DICTs of a CID-keyed font, each with its Private DICT, or the
	// Private DICT of a name-keyed one, which becomes its one Font DICT's.
	var privates []cffPrivateDict
	var fontDicts [][]cffEntry
	if cidKeyed {
		at := cffEntryOf(top, cffFDArray)
		if len(at) != 1 {
			return nil, nil, errNotSubset
		}
		fdArray, err := readCFFIndex(cff, at[0])
		if err != nil {
			return nil, nil, err
		}
		for i := 0; i+1 < len(fdArray.objects); i++ {
			entries, err := readCFFDict(cff[fdArray.objects[i]:fdArray.objects[i+1]])
			if err != nil {
				return nil, nil, err
			}
			private := cffEntryOf(entries, cffPrivate)
			if len(private) != 2 {
				return nil, nil, errNotSubset
			}
			block, err := readCFFPrivate(cff, private[0], private[1])
			if err != nil {
				return nil, nil, err
			}
			fontDicts = append(fontDicts, entries)
			privates = append(privates, block)
		}
	} else {
		private := cffEntryOf(top, cffPrivate)
		if len(private) != 2 {
			return nil, nil, errNotSubset
		}
		block, err := readCFFPrivate(cff, private[0], private[1])
		if err != nil {
			return nil, nil, err
		}
		fontDicts = [][]cffEntry{{{op: cffPrivate}}}
		if hasMatrix {
			fontDicts[0] = []cffEntry{{op: cffFontMatrix, raw: cffIdentityMatrix}, {op: cffPrivate}}
		}
		privates = append(privates, block)
	}

	// The glyphs kept, by their numbers in the font, which are their CIDs.
	kept := make([]bool, numGlyphs)
	var keptGIDs []int
	for gid := 0; gid < numGlyphs; gid++ {
		if used == nil || gid == 0 || (gid < len(used) && used[gid]) {
			kept[gid] = true
			keptGIDs = append(keptGIDs, gid)
		}
	}
	glyphs := make([][]byte, len(keptGIDs))
	for i, gid := range keptGIDs {
		glyphs[i] = cff[charStrings.objects[gid]:charStrings.objects[gid+1]]
	}

	// The subroutines the glyphs kept call, the others emptied. A font whose
	// charstrings cannot be followed keeps them all.
	fdOf, err := cffFDOf(fdSelect, numGlyphs, len(privates))
	if err != nil {
		return nil, nil, err
	}
	usedG := make([]bool, len(globalSubrs.objects)-1)
	usedL := make([][]bool, len(privates))
	for i, private := range privates {
		if private.subrs != nil {
			usedL[i] = make([]bool, len(private.subrs.objects)-1)
		}
	}
	usage := cffUsage{cff: cff, global: globalSubrs, usedG: usedG}
	if used == nil {
		usage.err = errNotSubset // Every glyph and subroutine kept
	}
	// The glyphs of a subset written with the subroutines they call in
	// place of their calls, and no subroutines then, or, when that cannot
	// be done, with the subroutines they call.
	glyphsInlined := false
	if used != nil && cffDesubroutinize {
		size := 0
		for _, glyph := range glyphs {
			size += len(glyph)
		}
		inlined := cffInliner{cff: cff, global: globalSubrs, out: make([]byte, 0, 3*size)}
		desub := make([][]byte, len(keptGIDs))
		for i := 0; i < len(keptGIDs) && inlined.err == nil; i++ {
			fd := 0
			if fdOf != nil {
				fd = fdOf[keptGIDs[i]]
			}
			inlined.local = privates[fd].subrs
			desub[i] = inlined.glyph(glyphs[i])
		}
		if inlined.seac {
			return nil, nil, errNotSubset
		}
		if inlined.err == nil {
			glyphs, glyphsInlined = desub, true
			for i := range usedL {
				if usedL[i] != nil {
					usedL[i] = make([]bool, len(usedL[i]))
				}
			}
		}
	}
	for i := 0; i < len(keptGIDs) && usage.err == nil && !glyphsInlined; i++ {
		fd := 0
		if fdOf != nil {
			fd = fdOf[keptGIDs[i]]
		}
		usage.local, usage.usedL = nil, nil
		if fd < len(privates) && privates[fd].subrs != nil {
			usage.local, usage.usedL = privates[fd].subrs, usedL[fd]
		}
		usage.stack, usage.stems, usage.stopped = 0, 0, false
		usage.run(glyphs[i], 0)
	}
	if usage.err != nil {
		if usage.work > cffMaxWork || usage.seac {
			return nil, nil, errNotSubset
		}
		usedG = nil // Kept whole
		for i := range usedL {
			usedL[i] = nil
		}
	}

	// The Font DICTs the glyphs kept use, in their order in the font, and the
	// new number of each.
	usedFD := make([]bool, len(privates))
	for _, gid := range keptGIDs {
		if fdOf != nil {
			usedFD[fdOf[gid]] = true
		} else {
			usedFD[0] = true
		}
	}
	newFD := make([]int, len(privates))
	var fds []int
	for fd, isUsed := range usedFD {
		if isUsed {
			newFD[fd] = len(fds)
			fds = append(fds, fd)
		}
	}
	fdOfGlyph := make([]int, len(keptGIDs))
	for i, gid := range keptGIDs {
		if fdOf != nil {
			fdOfGlyph[i] = newFD[fdOf[gid]]
		}
	}

	// The blocks: the Name INDEX again; the String INDEX of the strings the
	// Top DICT and the Font DICTs kept name, in the order they name them, a
	// name-keyed font's with those of its ROS, Adobe and Identity, after
	// them, and not the names of its glyphs, which a CIDFont has not; the
	// charset of the CIDs; the FDSelect; the CharStrings of the glyphs kept.
	nameIndex := appendCFFIndex(nil, emptiedCFFIndex(cff, names, nil))
	sids := cffStrings{old: emptiedCFFIndex(cff, strings, nil), sids: map[int]int{}}
	var entries []cffEntry
	for _, entry := range top {
		switch entry.op {
		case cffCharset, cffEncoding, cffCharStrings, cffPrivate, cffFDArray, cffFDSelect:
			continue
		}
		if entry, err = sids.renumber(entry); err != nil {
			return nil, nil, err
		}
		entries = append(entries, entry)
	}
	keptFontDicts := make([][]cffEntry, len(fds))
	for i, fd := range fds {
		for _, entry := range fontDicts[fd] {
			if entry, err = sids.renumber(entry); err != nil {
				return nil, nil, err
			}
			keptFontDicts[i] = append(keptFontDicts[i], entry)
		}
	}
	if !cidKeyed {
		sid := 391 + len(sids.kept)
		sids.kept = append(sids.kept, []byte("Adobe"), []byte("Identity"))
		ros := appendCFFInteger(nil, sid)
		ros = appendCFFInteger(ros, sid+1)
		ros = appendCFFInteger(ros, 0)
		entries = append([]cffEntry{{op: cffROS, raw: append(ros, 12, 30)}}, entries...)
		count := appendCFFInteger(nil, numGlyphs)
		entries = append(entries, cffEntry{op: cffCIDCount, raw: append(count, 12, 34)})
	}
	stringIndex := appendCFFIndex(nil, sids.kept)
	entries = append(entries, cffEntry{op: cffCharset}, cffEntry{op: cffFDSelect},
		cffEntry{op: cffCharStrings}, cffEntry{op: cffFDArray})
	charset := cffCharsetOf(keptGIDs[1:])
	newFDSelect := cffFDSelectOf(fdOfGlyph)
	newCharStrings := appendCFFIndex(nil, glyphs)
	// An INDEX of subroutines none of which is called is left out, global
	// ones as an empty INDEX, local ones with their Subrs operator: one of
	// objects of no bytes alone is not read by FreeType
	globals := emptiedCFFIndex(cff, globalSubrs, usedG)
	if usedG != nil && !cffAnyUsed(usedG) {
		globals = nil
	}
	newGlobalSubrs := appendCFFIndex(nil, globals)

	// Each Private DICT of a Font DICT kept is written again with its local
	// subroutines right after it, at its size, whatever they were before.
	blocks := make([][]byte, len(fds))
	dictSizes := make([]int, len(fds))
	for i, fd := range fds {
		private := privates[fd]
		if private.subrs != nil && usedL[fd] != nil && !cffAnyUsed(usedL[fd]) {
			var entries []cffEntry
			for _, entry := range private.entries {
				if entry.op != cffSubrs {
					entries = append(entries, entry)
				}
			}
			private = cffPrivateDict{entries: entries}
		}
		subrs := map[int]int{}
		if private.subrs != nil {
			subrs[cffSubrs] = 0
		}
		dictSizes[i] = len(writeCFFDict(private.entries, subrs, 0))
		if private.subrs != nil {
			subrs[cffSubrs] = dictSizes[i]
		}
		blocks[i] = writeCFFDict(private.entries, subrs, 0)
		if private.subrs != nil {
			blocks[i] = appendCFFIndex(blocks[i], emptiedCFFIndex(cff, *private.subrs, usedL[fd]))
		}
	}

	// The layout. The Top DICT and the Font DICTs have the same size whatever
	// their offsets, so they are written once with none to know where the
	// blocks after them go, and once more with their offsets.
	topOffsets := func(charsetAt, fdSelectAt, charStringsAt, fdArrayAt int) map[int]int {
		return map[int]int{cffCharset: charsetAt, cffFDSelect: fdSelectAt,
			cffCharStrings: charStringsAt, cffFDArray: fdArrayAt}
	}
	fontDictOf := func(i, privateAt int) []byte {
		return writeCFFDict(keptFontDicts[i], map[int]int{cffPrivate: privateAt}, dictSizes[i])
	}
	topSize := len(writeCFFDict(entries, topOffsets(0, 0, 0, 0), 0))
	topIndexSize := len(appendCFFIndex(nil, [][]byte{make([]byte, topSize)}))
	at := headerSize + len(nameIndex) + topIndexSize + len(stringIndex) + len(newGlobalSubrs)
	charsetAt := at
	at += len(charset)
	fdSelectAt := at
	at += len(newFDSelect)
	newCharStringsAt := at
	at += len(newCharStrings)
	fdArrayAt := at
	sized := make([][]byte, len(fds))
	for i := range fds {
		sized[i] = fontDictOf(i, 0)
	}
	at += len(appendCFFIndex(nil, sized))
	privateAts := make([]int, len(fds))
	for i := range blocks {
		privateAts[i] = at
		at += len(blocks[i])
	}
	dicts := make([][]byte, len(fds))
	for i := range fds {
		dicts[i] = fontDictOf(i, privateAts[i])
	}
	fdArray := appendCFFIndex(nil, dicts)
	newTop := writeCFFDict(entries, topOffsets(charsetAt, fdSelectAt, newCharStringsAt, fdArrayAt), 0)

	out := make([]byte, 0, at)
	out = append(out, cff[:headerSize]...)
	out = append(out, nameIndex...)
	out = appendCFFIndex(out, [][]byte{newTop})
	out = append(out, stringIndex...)
	out = append(out, newGlobalSubrs...)
	out = append(out, charset...)
	out = append(out, newFDSelect...)
	out = append(out, newCharStrings...)
	out = append(out, fdArray...)
	for _, block := range blocks {
		out = append(out, block...)
	}
	if len(out) != at {
		return nil, nil, errNotSubset
	}
	return out, kept, nil
}

// cffSIDOperands are the operators of the Top DICT and of the Font DICTs
// whose first operands are SIDs, and how many: version, Notice, FullName,
// FamilyName, Weight, Copyright, PostScript, BaseFontName, ROS and FontName.
var cffSIDOperands = map[int]int{0: 1, 1: 1, 2: 1, 3: 1, 4: 1, 12 << 8: 1, 12<<8 | 21: 1, 12<<8 | 22: 1,
	cffROS: 2, 12<<8 | 38: 1}

// cffStrings gives the strings of the font that the entries name new SIDs,
// in the order they are first named, after the standard strings.
type cffStrings struct {
	old  [][]byte    // The String INDEX of the font
	kept [][]byte    // The strings named, in the order of their new SIDs
	sids map[int]int // The new SID of each SID of the font named
}

// renumber returns the entry with its SIDs of the font's strings renumbered,
// its operands written again in their shortest form, or as it was when it
// names none.
func (s *cffStrings) renumber(entry cffEntry) (cffEntry, error) {
	n, ok := cffSIDOperands[entry.op]
	if !ok {
		return entry, nil
	}
	if len(entry.integers) < n {
		return entry, errNotSubset
	}
	var raw []byte
	integers := make([]int, len(entry.integers))
	for i, v := range entry.integers {
		if i < n && v >= 391 {
			if v-391 >= len(s.old) {
				return entry, errNotSubset
			}
			sid, ok := s.sids[v]
			if !ok {
				sid = 391 + len(s.kept)
				s.sids[v] = sid
				s.kept = append(s.kept, s.old[v-391])
			}
			v = sid
		}
		integers[i] = v
		raw = appendCFFInteger(raw, v)
	}
	if entry.op > 0xFF {
		raw = append(raw, 12, byte(entry.op))
	} else {
		raw = append(raw, byte(entry.op))
	}
	return cffEntry{op: entry.op, raw: raw, integers: integers}, nil
}

// cffAnyUsed tells whether any of the subroutines is used.
func cffAnyUsed(used []bool) bool {
	for _, u := range used {
		if u {
			return true
		}
	}
	return false
}

// cffHasEntry tells whether the DICT has an entry of the operator, of any
// operands.
func cffHasEntry(entries []cffEntry, op int) bool {
	for _, entry := range entries {
		if entry.op == op {
			return true
		}
	}
	return false
}

// appendCFFInteger appends the integer of a DICT in its shortest form.
func appendCFFInteger(out []byte, v int) []byte {
	switch {
	case v >= -107 && v <= 107:
		return append(out, byte(v+139))
	case v >= 108 && v <= 1131:
		v -= 108
		return append(out, byte((v>>8)+247), byte(v))
	case v >= -1131 && v <= -108:
		v = -v - 108
		return append(out, byte((v>>8)+251), byte(v))
	case v >= -32768 && v <= 32767:
		return append(out, 28, byte(v>>8), byte(v))
	}
	return appendCFFOffset(out, v)
}

// cffRanges returns the runs of consecutive CIDs, each its first and the
// number after it, nLeft, of at most max.
func cffRanges(cids []int, max int) [][2]int {
	var ranges [][2]int
	for i := 0; i < len(cids); {
		j := i
		for j+1 < len(cids) && cids[j+1] == cids[j]+1 && j+1-i <= max {
			j++
		}
		ranges = append(ranges, [2]int{cids[i], j - i})
		i = j + 1
	}
	return ranges
}

// cffCharsetOf returns the charset of the CIDs of the glyphs after .notdef,
// in the format of the three that is the shortest, the first of those as
// short.
func cffCharsetOf(cids []int) []byte {
	ranges1 := cffRanges(cids, 0xFF)
	ranges2 := cffRanges(cids, 0xFFFF)
	size0, size1, size2 := 1+2*len(cids), 1+3*len(ranges1), 1+4*len(ranges2)
	var out []byte
	switch {
	case size0 <= size1 && size0 <= size2:
		out = append(out, 0)
		for _, cid := range cids {
			out = append(out, byte(cid>>8), byte(cid))
		}
	case size1 <= size2:
		out = append(out, 1)
		for _, r := range ranges1 {
			out = append(out, byte(r[0]>>8), byte(r[0]), byte(r[1]))
		}
	default:
		out = append(out, 2)
		for _, r := range ranges2 {
			out = append(out, byte(r[0]>>8), byte(r[0]), byte(r[1]>>8), byte(r[1]))
		}
	}
	return out
}

// cffFDSelectOf returns the FDSelect of the Font DICT of each glyph, in
// format 3, ranges, when that is shorter than format 0, a byte for each.
func cffFDSelectOf(fdOf []int) []byte {
	var firsts []int
	for gid, fd := range fdOf {
		if gid == 0 || fd != fdOf[gid-1] {
			firsts = append(firsts, gid)
		}
	}
	if 5+3*len(firsts) >= 1+len(fdOf) {
		out := []byte{0}
		for _, fd := range fdOf {
			out = append(out, byte(fd))
		}
		return out
	}
	out := []byte{3, byte(len(firsts) >> 8), byte(len(firsts))}
	for _, first := range firsts {
		out = append(out, byte(first>>8), byte(first), byte(fdOf[first]))
	}
	return append(out, byte(len(fdOf)>>8), byte(len(fdOf)))
}
