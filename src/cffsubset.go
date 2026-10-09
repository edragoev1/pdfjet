// cffsubset.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// The subset of a font with CFF outlines, a .otf: the CFF table with the
// charstrings of the glyphs the document does not draw emptied, and the
// subroutines, global and local, that the glyphs kept do not call. Every
// glyph and every subroutine keeps its number, so the glyphs kept are drawn
// by the same bytes as in the whole font. The CFF is
// written again in a fixed order, with every offset of its Top DICT, and of the
// Font DICTs of a CID-keyed font, in the five-byte form, so that an offset of
// any value has the same size: the header, the Name, Top DICT, String and
// Global Subr INDEXes, the charset, the Encoding and the FDSelect, the new
// CharStrings INDEX, the FDArray, and each Private DICT with its local
// subroutines.

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
				entry.integers = integers
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

// cffCharsetLength returns the length of the charset at the offset, for the
// glyphs of the font.
func cffCharsetLength(cff []byte, at, numGlyphs int) (int, error) {
	if at < 0 || at >= len(cff) {
		return 0, errNotSubset
	}
	switch cff[at] {
	case 0:
		return 1 + 2*(numGlyphs-1), nil
	case 1, 2:
		size := int(cff[at]) // The size of nLeft: 1 byte in format 1, 2 in format 2
		covered := 1         // .notdef is not in it
		i := at + 1
		for covered < numGlyphs {
			if i+2+size > len(cff) {
				return 0, errNotSubset
			}
			nLeft := int(cff[i+2])
			if size == 2 {
				nLeft = nLeft<<8 | int(cff[i+3])
			}
			covered += nLeft + 1
			i += 2 + size
		}
		return i - at, nil
	}
	return 0, errNotSubset
}

// cffEncodingLength returns the length of the Encoding at the offset.
func cffEncodingLength(cff []byte, at int) (int, error) {
	if at < 0 || at+2 > len(cff) {
		return 0, errNotSubset
	}
	length := 0
	switch cff[at] & 0x7F {
	case 0:
		length = 2 + int(cff[at+1])
	case 1:
		length = 2 + 2*int(cff[at+1])
	default:
		return 0, errNotSubset
	}
	if cff[at]&0x80 != 0 { // Supplements
		if at+length >= len(cff) {
			return 0, errNotSubset
		}
		length += 1 + 3*int(cff[at+length])
	}
	return length, nil
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
				// would have to be kept too.
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

// emptied returns the objects of the INDEX, those not used emptied to the
// charstring of the byte given.
func emptiedCFFIndex(cff []byte, index cffIndex, used []bool, empty byte) [][]byte {
	objects := make([][]byte, len(index.objects)-1)
	for i := range objects {
		if used == nil || used[i] {
			objects[i] = cff[index.objects[i]:index.objects[i+1]]
		} else {
			objects[i] = []byte{empty}
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

// subsetCFF returns the CFF table with the charstrings of the glyphs not used
// emptied, and the subroutines they do not call, and the glyphs kept: those
// used and glyph 0, or every glyph for used nil, which writes the table again
// with nothing left out, for the identity charset of a CID-keyed font. A
// table that cannot be read gives an error.
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

	// The blocks copied as they are, but the charset of a CID-keyed font,
	// which maps each glyph to its CID: it is made the identity, so that the
	// glyph numbers PDFjet writes are the CIDs of the glyphs, as they are in a
	// name-keyed font.
	var charset, encoding, fdSelect []byte
	cidKeyed := cffEntryOf(top, cffROS) != nil
	if cidKeyed {
		if count := cffEntryOf(top, cffCIDCount); count != nil && (len(count) != 1 || count[0] < numGlyphs) {
			return nil, nil, errNotSubset
		}
		if count := cffEntryOf(top, cffCIDCount); count == nil && numGlyphs > 8720 {
			return nil, nil, errNotSubset // The default CIDCount
		}
		charset = []byte{0} // Format 0, for .notdef alone
		if numGlyphs > 1 {
			// Format 2, one range: CIDs 1 to numGlyphs-1.
			charset = []byte{2, 0, 1, byte((numGlyphs - 2) >> 8), byte(numGlyphs - 2)}
		}
	} else if at := cffEntryOf(top, cffCharset); len(at) == 1 && at[0] > 2 {
		length, err := cffCharsetLength(cff, at[0], numGlyphs)
		if err == nil {
			charset, err = cffBlock(cff, at[0], length)
		}
		if err != nil {
			return nil, nil, err
		}
	}
	if at := cffEntryOf(top, cffEncoding); len(at) == 1 && at[0] > 1 {
		length, err := cffEncodingLength(cff, at[0])
		if err == nil {
			encoding, err = cffBlock(cff, at[0], length)
		}
		if err != nil {
			return nil, nil, err
		}
	}
	if at := cffEntryOf(top, cffFDSelect); at != nil {
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

	// The Private DICT of a name-keyed font, or the Font DICTs of a CID-keyed
	// one, each with its Private DICT.
	var privates []cffPrivateDict
	var fontDicts [][]cffEntry
	if at := cffEntryOf(top, cffFDArray); at != nil {
		if len(at) != 1 || fdSelect == nil {
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
	} else if private := cffEntryOf(top, cffPrivate); private != nil {
		if len(private) != 2 {
			return nil, nil, errNotSubset
		}
		block, err := readCFFPrivate(cff, private[0], private[1])
		if err != nil {
			return nil, nil, err
		}
		privates = append(privates, block)
	}

	// The charstrings, of the glyphs kept as they were, of the others endchar.
	kept := make([]bool, numGlyphs)
	glyphs := make([][]byte, numGlyphs)
	endchar := []byte{14}
	for gid := 0; gid < numGlyphs; gid++ {
		if used == nil || gid == 0 || (gid < len(used) && used[gid]) {
			kept[gid] = true
			glyphs[gid] = cff[charStrings.objects[gid]:charStrings.objects[gid+1]]
		} else {
			glyphs[gid] = endchar
		}
	}
	newCharStrings := appendCFFIndex(nil, glyphs)

	// The subroutines the glyphs kept call, the others emptied to return. A
	// font whose charstrings cannot be followed keeps them all.
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
	for gid := 0; gid < numGlyphs && usage.err == nil; gid++ {
		if !kept[gid] {
			continue
		}
		fd := 0
		if fdOf != nil {
			fd = fdOf[gid]
		}
		usage.local, usage.usedL = nil, nil
		if fd < len(privates) && privates[fd].subrs != nil {
			usage.local, usage.usedL = privates[fd].subrs, usedL[fd]
		}
		usage.stack, usage.stems, usage.stopped = 0, 0, false
		usage.run(glyphs[gid], 0)
	}
	if usage.err != nil {
		if usage.work > cffMaxWork {
			return nil, nil, errNotSubset
		}
		usedG = nil // Kept whole
		for i := range usedL {
			usedL[i] = nil
		}
	}
	newGlobalSubrs := appendCFFIndex(nil, emptiedCFFIndex(cff, globalSubrs, usedG, 11))
	// Each Private DICT is written again with its local subroutines right
	// after it, at its size, whatever they were before.
	blocks := make([][]byte, len(privates))
	dictSizes := make([]int, len(privates))
	for i, private := range privates {
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
			blocks[i] = appendCFFIndex(blocks[i], emptiedCFFIndex(cff, *private.subrs, usedL[i], 11))
		}
	}

	// The layout. The Top DICT and the Font DICTs have the same size whatever
	// their offsets, so they are written once with none to know where the
	// blocks after them go, and once more with their offsets.
	topOffsets := func(charsetAt, encodingAt, fdSelectAt, charStringsAt, fdArrayAt, privateAt int) map[int]int {
		offsets := map[int]int{cffCharStrings: charStringsAt}
		if charset != nil {
			offsets[cffCharset] = charsetAt
		}
		if encoding != nil {
			offsets[cffEncoding] = encodingAt
		}
		if fdSelect != nil {
			offsets[cffFDSelect] = fdSelectAt
			offsets[cffFDArray] = fdArrayAt
		} else if len(privates) == 1 {
			offsets[cffPrivate] = privateAt
		}
		return offsets
	}
	privateDictSize := func(i int) int {
		// The size of the Private DICT itself, which the block starts with.
		return dictSizes[i]
	}
	topSize := len(writeCFFDict(top, topOffsets(0, 0, 0, 0, 0, 0), 0))
	fontDictOf := func(i, privateAt int) []byte {
		return writeCFFDict(fontDicts[i], map[int]int{cffPrivate: privateAt}, privateDictSize(i))
	}

	head := headerSize + (names.end - names.start)
	topIndexSize := len(appendCFFIndex(nil, [][]byte{make([]byte, topSize)}))
	at := head + topIndexSize + (strings.end - strings.start) + len(newGlobalSubrs)
	charsetAt := at
	at += len(charset)
	encodingAt := at
	at += len(encoding)
	fdSelectAt := at
	at += len(fdSelect)
	newCharStringsAt := at
	at += len(newCharStrings)
	fdArrayAt := at
	var fdArray []byte
	privateAts := make([]int, len(privates))
	if fontDicts != nil {
		sized := make([][]byte, len(fontDicts))
		for i := range fontDicts {
			sized[i] = fontDictOf(i, 0)
		}
		at += len(appendCFFIndex(nil, sized))
	}
	for i := range blocks {
		privateAts[i] = at
		at += len(blocks[i])
	}
	if fontDicts != nil {
		dicts := make([][]byte, len(fontDicts))
		for i := range fontDicts {
			dicts[i] = fontDictOf(i, privateAts[i])
		}
		fdArray = appendCFFIndex(nil, dicts)
	}
	privateAt := 0
	if fontDicts == nil && len(privates) == 1 {
		privateAt = privateAts[0]
	}
	topPrivateSize := 0
	if fontDicts == nil && len(privates) == 1 {
		topPrivateSize = privateDictSize(0)
	}
	newTop := writeCFFDict(top, topOffsets(charsetAt, encodingAt, fdSelectAt, newCharStringsAt, fdArrayAt, privateAt),
		topPrivateSize)

	out := make([]byte, 0, at)
	out = append(out, cff[:headerSize]...)
	out = append(out, cff[names.start:names.end]...)
	out = appendCFFIndex(out, [][]byte{newTop})
	out = append(out, cff[strings.start:strings.end]...)
	out = append(out, newGlobalSubrs...)
	out = append(out, charset...)
	out = append(out, encoding...)
	out = append(out, fdSelect...)
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
