// otf_fuzz_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/binary"
	"io"
	"os"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fuzz targets of the OpenType and TrueType fonts, which Font reads from
// an .otf or .ttf file. Any input either makes a font that draws text, or
// panics with a message of PDFjet's own: an index out of range, a nil
// pointer, a hang or gigabytes of memory is a bug. The seeds are fonts PDFjet
// ships; go test runs them, and
//
//	go test ./src -run '^$' -fuzz '^FuzzOpenTypeFontTables$' -fuzztime 5m -fuzzminimizetime 5s
//
// fuzzes. An input that fails is kept in testdata/fuzz, and go test runs it
// from then on.

// fuzzOpenTypeFontSeeds are shipped fonts: TrueType with the GPOS table of
// marks that go on letters (Thai), TrueType with kerning and no marks
// (JetBrains Mono), and an OpenType font whose glyphs are in a CFF table
// (IBM Plex Sans).
var fuzzOpenTypeFontSeeds = []string{
	"../fonts/NotoSansThai/NotoSansThai-Regular.ttf",
	"../fonts/JetBrainsMono/JetBrainsMono-Regular.ttf",
	"../fonts/IBMPlexSans/IBMPlexSans-Regular.otf",
}

// fuzzOpenTypeFont loads the font and draws with it.
func fuzzOpenTypeFont(t *testing.T, data []byte) {
	fuzzRun(t, len(data), func() {
		pdf := NewPDF(bufio.NewWriter(io.Discard))
		font := NewFont(pdf, bytes.NewReader(data))
		font.SetSize(12)
		font.StringWidth(12, fuzzFontText)
		page := NewPage(pdf, letter.Portrait())
		NewTextLine(font, fuzzFontText).SetLocation(50, 50).DrawOn(page)
		if err := pdf.Complete(); err != nil {
			t.Fatal(err)
		}
	})
}

// FuzzOpenTypeFont fuzzes a whole .otf or .ttf file: the version, the table
// directory and the tables together.
func FuzzOpenTypeFont(f *testing.F) {
	for _, path := range fuzzOpenTypeFontSeeds {
		data, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		f.Add(data)
		f.Add(data[:len(data)/2])
	}
	f.Add([]byte{})
	f.Fuzz(fuzzOpenTypeFont)
}

// FuzzOpenTypeFontTables fuzzes the tables PDFjet reads, each as its own
// bytes, and joins them into a font whose directory is right. A change to one
// table then reaches its parser instead of moving the offsets of the others,
// which is what fuzzing a whole file mostly does.
func FuzzOpenTypeFontTables(f *testing.F) {
	for _, path := range fuzzOpenTypeFontSeeds {
		data, err := os.ReadFile(path)
		if err != nil {
			f.Fatal(err)
		}
		version, tables := fuzzSplitOpenTypeFont(f, data)
		f.Add(version, tables["head"], tables["hhea"], tables["OS/2"], tables["name"],
			tables["cmap"], tables["hmtx"], tables["post"], tables["GPOS"], tables["CFF "])
	}
	f.Fuzz(func(t *testing.T, version uint32, head, hhea, os2, name, cmap, hmtx, post, gpos, cff []byte) {
		fuzzOpenTypeFont(t, fuzzJoinOpenTypeFont(version,
			map[string][]byte{"head": head, "hhea": hhea, "OS/2": os2, "name": name,
				"cmap": cmap, "hmtx": hmtx, "post": post, "GPOS": gpos, "CFF ": cff}))
	})
}

// fuzzOpenTypeFontTables are the tables of fuzzJoinOpenTypeFont, in the order
// it writes them, so that a font is joined the same way every time.
var fuzzOpenTypeFontTables = []string{"head", "hhea", "OS/2", "name", "cmap", "hmtx", "post", "GPOS", "CFF "}

// fuzzJoinOpenTypeFont returns the font of a version and the tables, with a
// directory of the tables that are not empty. The checksums are 0: PDFjet
// reads them and does not check them.
func fuzzJoinOpenTypeFont(version uint32, tables map[string][]byte) []byte {
	present := make([]string, 0, len(fuzzOpenTypeFontTables))
	for _, name := range fuzzOpenTypeFontTables {
		if len(tables[name]) > 0 {
			present = append(present, name)
		}
	}
	var directory, data bytes.Buffer
	_ = binary.Write(&directory, binary.BigEndian, version)
	_ = binary.Write(&directory, binary.BigEndian, uint16(len(present)))
	directory.Write(make([]byte, 6)) // The search range, entry selector and range shift
	offset := 12 + 16*len(present)
	for _, name := range present {
		directory.WriteString(name)
		_ = binary.Write(&directory, binary.BigEndian, uint32(0))
		_ = binary.Write(&directory, binary.BigEndian, uint32(offset+data.Len()))
		_ = binary.Write(&directory, binary.BigEndian, uint32(len(tables[name])))
		data.Write(tables[name])
		for data.Len()%4 != 0 {
			data.WriteByte(0)
		}
	}
	return append(directory.Bytes(), data.Bytes()...)
}

// fuzzSplitOpenTypeFont returns the version and the tables of a font that
// fuzzJoinOpenTypeFont joins.
func fuzzSplitOpenTypeFont(f testing.TB, data []byte) (uint32, map[string][]byte) {
	if len(data) < 12 {
		f.Fatal("the font has no table directory")
	}
	version := binary.BigEndian.Uint32(data)
	tables := make(map[string][]byte)
	count := int(binary.BigEndian.Uint16(data[4:]))
	for i := 0; i < count; i++ {
		entry := 12 + 16*i
		if entry+16 > len(data) {
			f.Fatal("the table directory ends too soon")
		}
		name := string(data[entry : entry+4])
		offset := int(binary.BigEndian.Uint32(data[entry+8:]))
		length := int(binary.BigEndian.Uint32(data[entry+12:]))
		if offset < 0 || length < 0 || offset+length > len(data) {
			f.Fatal("the table " + name + " is not in the font")
		}
		tables[name] = data[offset : offset+length]
	}
	return version, tables
}
