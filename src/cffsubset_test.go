// cffsubset_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"fmt"
	"os"
	"regexp"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
)

const (
	testPlexOTF = "tests/fonts/IBMPlexSans-Regular.otf"
	testHanOTF  = "tests/fonts/SourceHanSansJP-Regular.otf"
)

// testCFF returns the font and its CFF table.
func testCFF(t testing.TB, path string) (*openTypeFont, []byte) {
	t.Helper()
	data, err := os.ReadFile(testRepoPath(t.(*testing.T), path))
	if err != nil {
		t.Fatal(err)
	}
	otf := newOpenTypeFont(data)
	return otf, otf.buf[otf.cffOff : otf.cffOff+otf.cffLen]
}

// testCFFParts returns the charstrings, the global subroutines and the Top
// DICT of a CFF table.
func testCFFParts(t *testing.T, cff []byte) (charStrings, globalSubrs [][]byte, top []cffEntry) {
	t.Helper()
	names, err := readCFFIndex(cff, int(cff[2]))
	if err != nil {
		t.Fatal(err)
	}
	topDicts, _ := readCFFIndex(cff, names.end)
	stringsIndex, _ := readCFFIndex(cff, topDicts.end)
	globals, err := readCFFIndex(cff, stringsIndex.end)
	if err != nil {
		t.Fatal(err)
	}
	top, err = readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	if err != nil {
		t.Fatal(err)
	}
	index, err := readCFFIndex(cff, cffEntryOf(top, cffCharStrings)[0])
	if err != nil {
		t.Fatal(err)
	}
	return emptiedCFFIndex(cff, index, nil), emptiedCFFIndex(cff, globals, nil), top
}

// testCFFUsed returns the glyphs of the text in the font.
func testCFFUsed(otf *openTypeFont, text string) []bool {
	used := make([]bool, 0x10000)
	for _, ch := range text {
		used[otf.unicodeToGID.get(ch)] = true
	}
	return used
}

// testCFFCharset returns the CIDs the charset of the subset gives its glyphs
// after .notdef, of formats 0, 1 and 2.
func testCFFCharset(t *testing.T, cff []byte, top []cffEntry, glyphs int) []int {
	t.Helper()
	at := cffEntryOf(top, cffCharset)[0]
	format := cff[at]
	at++
	var cids []int
	for len(cids) < glyphs-1 {
		switch format {
		case 0:
			cids = append(cids, int(cff[at])<<8|int(cff[at+1]))
			at += 2
		case 1, 2:
			first := int(cff[at])<<8 | int(cff[at+1])
			left := int(cff[at+2])
			at += 3
			if format == 2 {
				left = left<<8 | int(cff[at])
				at++
			}
			for cid := first; cid <= first+left; cid++ {
				cids = append(cids, cid)
			}
		default:
			t.Fatalf("charset format %d", format)
		}
	}
	return cids
}

func TestCFFSubsetIsACIDFontOfTheGlyphsUsedAlone(t *testing.T) {
	for _, path := range []string{testPlexOTF, testHanOTF} {
		otf, cff := testCFF(t, path)
		used := testCFFUsed(otf, "Hello 日本語")
		subset, kept, err := subsetCFF(cff, used)
		if err != nil {
			t.Fatal(path, err)
		}
		whole, _, _ := testCFFParts(t, cff)
		glyphs, globals, top := testCFFParts(t, subset)
		if len(kept) != len(whole) {
			t.Fatalf("%s: kept is of %d glyphs, not %d", path, len(kept), len(whole))
		}
		// The glyphs kept, .notdef and those used, alone, in their order
		var keptGIDs []int
		for gid := range kept {
			if want := gid == 0 || used[gid]; kept[gid] != want {
				t.Errorf("%s: glyph %d kept: %v", path, gid, kept[gid])
			}
			if kept[gid] && gid > 0 {
				keptGIDs = append(keptGIDs, gid)
			}
		}
		if len(glyphs) != len(keptGIDs)+1 {
			t.Fatalf("%s: %d glyphs, not %d", path, len(glyphs), len(keptGIDs)+1)
		}
		// A CIDFont, whose charset gives each glyph its number in the font
		// as its CID, which the content streams draw it by
		if cffEntryOf(top, cffROS) == nil || cffEntryOf(top, cffFDArray) == nil {
			t.Errorf("%s: the subset is not a CIDFont", path)
		}
		if cffEntryOf(top, cffEncoding) != nil {
			t.Errorf("%s: the subset has an Encoding", path)
		}
		cids := testCFFCharset(t, subset, top, len(glyphs))
		if fmt.Sprint(cids) != fmt.Sprint(keptGIDs) {
			t.Errorf("%s: the CIDs %v, not %v", path, cids, keptGIDs)
		}
		// No subroutines: the glyphs are written with them in place, and
		// drawn as in the whole font (TestCFFSubsetWithItsSubroutinesInPlace
		// DrawsTheSameOutlines)
		if len(globals) != 0 {
			t.Errorf("%s: %d global subroutines", path, len(globals))
		}
		if len(subset) > len(cff)/10 {
			t.Errorf("%s: %d bytes of %d", path, len(subset), len(cff))
		}
	}
}

func TestCFFSubsetOfEveryGlyphHasTheIdentityCharset(t *testing.T) {
	// Source Han Sans JP gives its glyphs CIDs of Adobe-Japan1, not their
	// numbers, and a PDF looks the glyphs of a CID-keyed font up by CID:
	// written whole, each glyph's CID is its number.
	for _, path := range []string{testPlexOTF, testHanOTF} {
		_, cff := testCFF(t, path)
		whole, _, _ := testCFFParts(t, cff)
		subset, kept, err := subsetCFF(cff, nil)
		if err != nil {
			t.Fatal(path, err)
		}
		glyphs, _, top := testCFFParts(t, subset)
		if len(glyphs) != len(whole) || len(kept) != len(whole) {
			t.Fatalf("%s: %d glyphs, not %d", path, len(glyphs), len(whole))
		}
		for i, cid := range testCFFCharset(t, subset, top, len(glyphs)) {
			if cid != i+1 {
				t.Fatalf("%s: glyph %d has the CID %d", path, i+1, cid)
			}
		}
		for gid := range glyphs {
			if !bytes.Equal(glyphs[gid], whole[gid]) {
				t.Fatalf("%s: glyph %d is not as it was", path, gid)
			}
		}
	}
}

// testCFFDoc draws the text in the font and returns the document.
func testCFFDoc(t *testing.T, level compliance.Compliance, path string, subset bool, text string) string {
	t.Helper()
	doc := testNewDoc()
	doc.pdf.SetCompliance(level).SetTitle("Test")
	page := NewPage(doc.pdf, testLetterPortrait())
	font := NewFontFromFile(doc.pdf, testRepoPath(t, path))
	font.SetSubset(subset)
	NewTextLine(font, text).SetLocation(50, 50).DrawOn(page)
	return string(doc.complete())
}

func TestCFFAFontWithCFFOutlinesIsEmbeddedAsASubset(t *testing.T) {
	for _, path := range []string{testPlexOTF, testHanOTF} {
		raw := testCFFDoc(t, compliance.PDF_A_1B, path, true, "Hello 日本語")
		if !regexp.MustCompile(`/FontName /[A-Z]{6}\+`).MatchString(raw) {
			t.Errorf("%s: no subset tag", path)
		}
		if !strings.Contains(raw, "/Subtype /CIDFontType0C\n") || !strings.Contains(raw, "/FontFile3 ") {
			t.Errorf("%s: not embedded as CFF", path)
		}
		if strings.Contains(raw, "/Length1 ") {
			t.Errorf("%s: a /Length1 for CFF", path)
		}
		if !strings.Contains(raw, "/CIDSet ") {
			t.Errorf("%s: no CIDSet in PDF/A-1", path)
		}
	}
	// Kept whole, it has no tag.
	raw := testCFFDoc(t, compliance.PDF_1_7, testPlexOTF, false, "Hello")
	if regexp.MustCompile(`/FontName /[A-Z]{6}\+`).MatchString(raw) || !strings.Contains(raw, "/FontFile3 ") {
		t.Error("the font kept whole")
	}
}

func TestCFFAFontWhoseLicenseForbidsSubsettingIsEmbeddedWhole(t *testing.T) {
	data, err := os.ReadFile(testRepoPath(t, testPlexOTF))
	if err != nil {
		t.Fatal(err)
	}
	os2 := testOpenTypeTable(t, data, "OS/2")
	data = testOpenTypeWith(data, os2+8, 0x0100)
	doc := testNewDoc()
	font := NewFont(doc.pdf, bytes.NewReader(data))
	NewTextLine(font, "Hello").SetLocation(50, 50).DrawOn(NewPage(doc.pdf, testLetterPortrait()))
	if raw := string(doc.complete()); regexp.MustCompile(`/FontName /[A-Z]{6}\+`).MatchString(raw) {
		t.Error("the font was subset")
	}
}

// FuzzSubsetCFF checks that a CFF table of any bytes is subset or refused, and
// never makes the subsetter panic.
//
//	go test -run '^$' -fuzz FuzzSubsetCFF -fuzztime 60s .
func FuzzSubsetCFF(f *testing.F) {
	data, err := os.ReadFile("../" + testPlexOTF)
	if err != nil {
		f.Fatal(err)
	}
	otf := newOpenTypeFont(data)
	f.Add(otf.buf[otf.cffOff:otf.cffOff+otf.cffLen], uint16(36))
	f.Fuzz(func(t *testing.T, cff []byte, gid uint16) {
		used := make([]bool, 0x10000)
		used[gid] = true
		_, _, _ = subsetCFF(cff, used)
		_, _, _ = subsetCFF(cff, nil)
	})
}

func TestCFFSubsetOfAnAccentedLetterDrawnWithSeacIsRefused(t *testing.T) {
	// Á's charstring made "0 0 65 194 endchar", seac: the A and the acute
	// it is drawn from were emptied, and it drew blank (the review of
	// 9 October 2026). The font is embedded whole.
	otf, cff := testCFF(t, testPlexOTF)
	names, _ := readCFFIndex(cff, int(cff[2]))
	topDicts, _ := readCFFIndex(cff, names.end)
	top, _ := readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	charStrings, err := readCFFIndex(cff, cffEntryOf(top, cffCharStrings)[0])
	if err != nil {
		t.Fatal(err)
	}
	gid := otf.unicodeToGID.get('Á')
	at := charStrings.objects[gid]
	if charStrings.objects[gid+1]-at < 6 {
		t.Fatalf("the charstring of Á is %d bytes", charStrings.objects[gid+1]-at)
	}
	copy(cff[at:], []byte{139, 139, 204, 247, 86, 14})
	if _, _, err := subsetCFF(cff, testCFFUsed(otf, "Á")); err != errNotSubset {
		t.Errorf("error %v, not errNotSubset", err)
	}
}

// testCFFWithTopDict returns the CFF table of a name-keyed font with its Top
// DICT written again with the entries, before its own, and its offsets moved
// by as many bytes.
func testCFFWithTopDict(t *testing.T, cff []byte, entries ...cffEntry) []byte {
	t.Helper()
	names, _ := readCFFIndex(cff, int(cff[2]))
	topDicts, _ := readCFFIndex(cff, names.end)
	top, err := readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	if err != nil {
		t.Fatal(err)
	}
	written := func(delta int) []byte {
		offsets := map[int]int{}
		privateSize := 0
		for _, entry := range top {
			switch entry.op {
			case cffCharset, cffCharStrings:
				offsets[entry.op] = entry.integers[0] + delta
			case cffPrivate:
				privateSize = entry.integers[0]
				offsets[entry.op] = entry.integers[1] + delta
			}
		}
		return writeCFFDict(append(append([]cffEntry{}, entries...), top...), offsets, privateSize)
	}
	delta := len(appendCFFIndex(nil, [][]byte{written(0)})) - (topDicts.end - topDicts.start)
	out := append([]byte{}, cff[:topDicts.start]...)
	out = appendCFFIndex(out, [][]byte{written(delta)})
	return append(out, cff[topDicts.end:]...)
}

func TestCFFSubsetOfANameKeyedFontWithAFontMatrixOfItsOwn(t *testing.T) {
	// As Cascadia Mono's .otf, of 2048 units to the em, has: the matrix
	// stays in the Top DICT, and the Font DICT has the identity matrix, the
	// two concatenated the font's.
	otf, cff := testCFF(t, testPlexOTF)
	matrix := []byte{30, 0xa0, 0x00, 0x48, 0x82, 0x8f} // .00048828
	raw := append(append(append([]byte{}, matrix...), 139, 139), matrix...)
	raw = append(raw, 139, 139, 12, 7)
	font := testCFFWithTopDict(t, cff, cffEntry{op: cffFontMatrix, raw: raw})
	whole, _, _ := testCFFParts(t, font)
	matrices := func(cff []byte) [][]byte {
		_, _, top := testCFFParts(t, cff)
		var raws [][]byte
		for _, entry := range top {
			if entry.op == cffFontMatrix {
				raws = append(raws, entry.raw)
			}
		}
		fdArray, err := readCFFIndex(cff, cffEntryOf(top, cffFDArray)[0])
		if err != nil || len(fdArray.objects) != 2 {
			t.Fatal("not one Font DICT", err)
		}
		fd, _ := readCFFDict(cff[fdArray.objects[0]:fdArray.objects[1]])
		for _, entry := range fd {
			if entry.op == cffFontMatrix {
				raws = append(raws, entry.raw)
			}
		}
		return raws
	}
	want := fmt.Sprint([][]byte{raw, {140, 139, 139, 140, 139, 139, 12, 7}})
	var subset []byte
	for _, used := range [][]bool{testCFFUsed(otf, "Hello"), nil} {
		var err error
		subset, _, err = subsetCFF(font, used)
		if err != nil {
			t.Fatal(err)
		}
		glyphs, _, _ := testCFFParts(t, subset)
		if used == nil && len(glyphs) != len(whole) {
			t.Errorf("%d glyphs, not %d", len(glyphs), len(whole))
		}
		if got := fmt.Sprint(matrices(subset)); got != want {
			t.Errorf("the matrices %s, not %s", got, want)
		}
	}
	// A CID-keyed font's matrices are kept as they are: the subset's
	// subset has them both again.
	again, _, err := subsetCFF(subset, testCFFUsed(otf, "He"))
	if err != nil {
		t.Fatal(err)
	}
	if got := fmt.Sprint(matrices(again)); got != want {
		t.Errorf("the matrices of a CID-keyed font: %s, not %s", got, want)
	}
	// A name-keyed font of no FontMatrix, as IBM Plex Sans, has the
	// identity matrix in its Font DICT too, and the default written in its
	// Top DICT: no reader takes the default of 1000 units to the em twice,
	// or the Font DICT's matrix as the whole one.
	for _, used := range [][]bool{testCFFUsed(otf, "Hello"), nil} {
		subset, _, _ = subsetCFF(cff, used)
		if got := fmt.Sprint(matrices(subset)); got != fmt.Sprint([][]byte{cffThousandthMatrix, cffIdentityMatrix}) {
			t.Errorf("the matrices %v", got)
		}
	}
}

// testCFFOutlines returns the outline of each glyph of the CFF table, the
// points of its path operators in order, drawn by its Type 2 charstring run
// with its subroutines.
func testCFFOutlines(t *testing.T, cff []byte) []string {
	t.Helper()
	charStrings, globals, top := testCFFParts(t, cff)
	locals := [][][]byte{nil}
	var fdOf []int
	if at := cffEntryOf(top, cffFDArray); at != nil {
		fdArray, _ := readCFFIndex(cff, at[0])
		locals = nil
		for i := 0; i+1 < len(fdArray.objects); i++ {
			fd, _ := readCFFDict(cff[fdArray.objects[i]:fdArray.objects[i+1]])
			locals = append(locals, testCFFLocals(t, cff, cffEntryOf(fd, cffPrivate)))
		}
		at := cffEntryOf(top, cffFDSelect)[0]
		length, _ := cffFDSelectLength(cff, at, len(charStrings))
		fdOf, _ = cffFDOf(cff[at:at+length], len(charStrings), len(locals))
	} else {
		locals[0] = testCFFLocals(t, cff, cffEntryOf(top, cffPrivate))
	}
	outlines := make([]string, len(charStrings))
	for gid, cs := range charStrings {
		fd := 0
		if fdOf != nil {
			fd = fdOf[gid]
		}
		p := &testType2{globals: globals, locals: locals[fd]}
		p.run(cs, 0)
		outlines[gid] = p.path.String()
	}
	return outlines
}

// testCFFLocals returns the local subroutines of the Private DICT.
func testCFFLocals(t *testing.T, cff []byte, private []int) [][]byte {
	block, err := readCFFPrivate(cff, private[0], private[1])
	if err != nil {
		t.Fatal(err)
	}
	if block.subrs == nil {
		return nil
	}
	return emptiedCFFIndex(cff, *block.subrs, nil)
}

// testType2 draws a Type 2 charstring (Technical Note #5177) as the points
// of its path, apart from the subsetter's code.
type testType2 struct {
	globals, locals [][]byte
	stack           []float64
	x, y            float64
	stems           int
	width           bool // Looked for already
	ended           bool
	path            strings.Builder
}

func (p *testType2) point(op string, dx, dy float64) {
	p.x += dx
	p.y += dy
	fmt.Fprintf(&p.path, "%s %g %g ", op, p.x, p.y)
}

// args takes the width off the stack, the first time, when it has more
// operands than even, for even, or than n.
func (p *testType2) args(even bool, n int) {
	if !p.width {
		p.width = true
		if (even && len(p.stack)%2 == 1) || (!even && len(p.stack) > n) {
			p.stack = p.stack[1:]
		}
	}
}

func (p *testType2) run(cs []byte, depth int) {
	for i := 0; i < len(cs) && !p.ended; {
		b0 := int(cs[i])
		switch {
		case b0 == 28:
			p.stack = append(p.stack, float64(int16(uint16(cs[i+1])<<8|uint16(cs[i+2]))))
			i += 3
			continue
		case b0 >= 32 && b0 <= 246:
			p.stack = append(p.stack, float64(b0-139))
			i++
			continue
		case b0 >= 247 && b0 <= 250:
			p.stack = append(p.stack, float64((b0-247)*256+int(cs[i+1])+108))
			i += 2
			continue
		case b0 >= 251 && b0 <= 254:
			p.stack = append(p.stack, float64(-(b0-251)*256-int(cs[i+1])-108))
			i += 2
			continue
		case b0 == 255:
			p.stack = append(p.stack, float64(int32(uint32(cs[i+1])<<24|uint32(cs[i+2])<<16|uint32(cs[i+3])<<8|uint32(cs[i+4])))/65536)
			i += 5
			continue
		}
		s := p.stack
		i++
		switch b0 {
		case 1, 3, 18, 23: // hstem, vstem, hstemhm, vstemhm
			p.args(true, 0)
			p.stems += len(p.stack) / 2
			fmt.Fprintf(&p.path, "stem%d %v ", b0, p.stack)
		case 19, 20: // hintmask, cntrmask
			p.args(true, 0)
			p.stems += len(p.stack) / 2
			n := (p.stems + 7) / 8
			fmt.Fprintf(&p.path, "mask%d %v %x ", b0, p.stack, cs[i:i+n])
			i += n
		case 21: // rmoveto
			p.args(false, 2)
			p.point("M", p.stack[0], p.stack[1])
		case 22: // hmoveto
			p.args(false, 1)
			p.point("M", p.stack[0], 0)
		case 4: // vmoveto
			p.args(false, 1)
			p.point("M", 0, p.stack[0])
		case 5: // rlineto
			for j := 0; j+1 < len(s); j += 2 {
				p.point("L", s[j], s[j+1])
			}
		case 6, 7: // hlineto, vlineto
			for j, h := 0, b0 == 6; j < len(s); j, h = j+1, !h {
				if h {
					p.point("L", s[j], 0)
				} else {
					p.point("L", 0, s[j])
				}
			}
		case 8: // rrcurveto
			for j := 0; j+5 < len(s); j += 6 {
				p.curve(s[j], s[j+1], s[j+2], s[j+3], s[j+4], s[j+5])
			}
		case 24: // rcurveline
			j := 0
			for ; j+5 < len(s)-2; j += 6 {
				p.curve(s[j], s[j+1], s[j+2], s[j+3], s[j+4], s[j+5])
			}
			p.point("L", s[j], s[j+1])
		case 25: // rlinecurve
			j := 0
			for ; j+1 < len(s)-6; j += 2 {
				p.point("L", s[j], s[j+1])
			}
			p.curve(s[j], s[j+1], s[j+2], s[j+3], s[j+4], s[j+5])
		case 26, 27: // vvcurveto, hhcurveto
			j, d := 0, 0.0
			if len(s)%4 == 1 {
				d, j = s[0], 1
			}
			for ; j+3 < len(s); j += 4 {
				if b0 == 26 {
					p.curve(d, s[j], s[j+1], s[j+2], 0, s[j+3])
				} else {
					p.curve(s[j], d, s[j+1], s[j+2], s[j+3], 0)
				}
				d = 0
			}
		case 30, 31: // vhcurveto, hvcurveto
			h := b0 == 31
			for j := 0; j+3 < len(s); j, h = j+4, !h {
				last := 0.0
				if len(s)-j == 5 {
					last = s[j+4]
				}
				if h {
					p.curve(s[j], 0, s[j+1], s[j+2], last, s[j+3])
				} else {
					p.curve(0, s[j], s[j+1], s[j+2], s[j+3], last)
				}
			}
		case 10, 29: // callsubr, callgsubr
			subrs := p.locals
			if b0 == 29 {
				subrs = p.globals
			}
			number := int(s[len(s)-1]) + cffBias(len(subrs))
			p.stack = s[:len(s)-1]
			p.run(subrs[number], depth+1)
			continue
		case 11: // return
			return
		case 14: // endchar
			p.args(false, 0)
			p.path.WriteString("Z")
			p.ended = true
			return
		case 12:
			b1 := cs[i]
			i++
			switch b1 {
			case 35: // flex
				p.curve(s[0], s[1], s[2], s[3], s[4], s[5])
				p.curve(s[6], s[7], s[8], s[9], s[10], s[11])
			case 34: // hflex
				p.curve(s[0], 0, s[1], s[2], s[3], 0)
				p.curve(s[4], 0, s[5], -s[2], s[6], 0)
			case 36: // hflex1
				p.curve(s[0], s[1], s[2], s[3], s[4], 0)
				p.curve(s[5], 0, s[6], s[7], s[8], -(s[1] + s[3] + s[7]))
			case 37: // flex1
				dx := s[0] + s[2] + s[4] + s[6] + s[8]
				dy := s[1] + s[3] + s[5] + s[7] + s[9]
				p.curve(s[0], s[1], s[2], s[3], s[4], s[5])
				if dx < 0 {
					dx = -dx
				}
				if dy < 0 {
					dy = -dy
				}
				if dx > dy {
					p.curve(s[6], s[7], s[8], s[9], s[10], -(s[1] + s[3] + s[5] + s[7] + s[9]))
				} else {
					p.curve(s[6], s[7], s[8], s[9], -(s[0] + s[2] + s[4] + s[6] + s[8]), s[10])
				}
			default:
				fmt.Fprintf(&p.path, "op12.%d ", b1)
			}
		default:
			fmt.Fprintf(&p.path, "op%d ", b0)
		}
		p.stack = p.stack[:0]
	}
}

func (p *testType2) curve(dx1, dy1, dx2, dy2, dx3, dy3 float64) {
	p.point("C", dx1, dy1)
	p.point("", dx2, dy2)
	p.point("", dx3, dy3)
}

func TestCFFSubsetWithItsSubroutinesInPlaceDrawsTheSameOutlines(t *testing.T) {
	for _, path := range []string{testPlexOTF, "tests/fonts/IBMPlexSans-Italic.otf", testHanOTF} {
		otf, cff := testCFF(t, path)
		used := testCFFUsed(otf, "Hello, Wörld! Ýou ﬁnd 日本語の文字 ½ € @ & ŞŢ")
		every := make([]bool, otf.numGlyphs)
		for gid := 0; gid < otf.numGlyphs; gid += 3 {
			every[gid] = true
		}
		want := testCFFOutlines(t, cff)
		for _, used := range [][]bool{used, every} {
			subset, kept, err := subsetCFF(cff, used)
			if err != nil {
				t.Fatal(path, err)
			}
			// No subroutines, global or local
			_, globals, top := testCFFParts(t, subset)
			if len(globals) != 0 {
				t.Errorf("%s: %d global subroutines", path, len(globals))
			}
			fdArray, _ := readCFFIndex(subset, cffEntryOf(top, cffFDArray)[0])
			for i := 0; i+1 < len(fdArray.objects); i++ {
				fd, _ := readCFFDict(subset[fdArray.objects[i]:fdArray.objects[i+1]])
				private := cffEntryOf(fd, cffPrivate)
				block, err := readCFFPrivate(subset, private[0], private[1])
				if err != nil || block.subrs != nil {
					t.Errorf("%s: local subroutines in Font DICT %d", path, i)
				}
			}
			// Each glyph drawn as in the whole font, its hints and masks too
			got := testCFFOutlines(t, subset)
			i := 0
			for gid, keep := range kept {
				if !keep {
					continue
				}
				if got[i] != want[gid] {
					t.Errorf("%s: glyph %d drawn otherwise:\n%s\n%s", path, gid, got[i], want[gid])
				}
				if !strings.HasSuffix(got[i], "Z") || !strings.Contains(got[i], "M ") && gid > 0 && got[i] != "Z" {
					t.Errorf("%s: glyph %d: %s", path, gid, got[i])
				}
				i++
			}
			if i != len(got) {
				t.Errorf("%s: %d glyphs, not %d", path, len(got), i)
			}
		}
	}
}

func TestCFFSubsetOfAGlyphThatComputesANumberKeepsItsSubroutines(t *testing.T) {
	// "0 0 add 0 endchar": a number computed, by add, which the subroutines
	// are not written in place for. The subset keeps the glyphs as they were
	// and the subroutines they call, the others emptied to no bytes, keeping
	// their numbers.
	otf, cff := testCFF(t, testPlexOTF)
	charStrings, _ := testCFFIndexOf(t, cff)
	gid := otf.unicodeToGID.get('Á')
	copy(cff[charStrings.objects[gid]:], []byte{139, 139, 12, 10, 139, 14})
	used := testCFFUsed(otf, "Hello AÁ")
	subset, kept, err := subsetCFF(cff, used)
	if err != nil {
		t.Fatal(err)
	}
	glyphs, globals, _ := testCFFParts(t, subset)
	whole, wholeGlobals, _ := testCFFParts(t, cff)
	i := 0
	for g, keep := range kept {
		if keep {
			if !bytes.Equal(glyphs[i], whole[g]) {
				t.Errorf("glyph %d is not as it was", g)
			}
			i++
		}
	}
	if len(globals) != len(wholeGlobals) {
		t.Fatalf("%d global subroutines, not %d", len(globals), len(wholeGlobals))
	}
	emptied := 0
	for i := range globals {
		if len(globals[i]) == 0 && len(wholeGlobals[i]) > 0 {
			emptied++
		} else if !bytes.Equal(globals[i], wholeGlobals[i]) {
			t.Errorf("global subroutine %d changed", i)
		}
	}
	if emptied == 0 || emptied == len(globals) {
		t.Errorf("%d of %d global subroutines emptied", emptied, len(globals))
	}
}

// testCFFIndexOf returns the CharStrings INDEX of the table and its Top DICT.
func testCFFIndexOf(t *testing.T, cff []byte) (cffIndex, []cffEntry) {
	t.Helper()
	names, _ := readCFFIndex(cff, int(cff[2]))
	topDicts, _ := readCFFIndex(cff, names.end)
	top, _ := readCFFDict(cff[topDicts.objects[0]:topDicts.objects[1]])
	charStrings, err := readCFFIndex(cff, cffEntryOf(top, cffCharStrings)[0])
	if err != nil {
		t.Fatal(err)
	}
	return charStrings, top
}
