// barcode_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/direction"
)

var testBarcodeCorners = []struct {
	barcodeType int
	text        string
	direction   direction.Direction
	withFont    bool
	x, y        float32
}{
	{EAN_13, "012345678901", direction.LeftToRight, false, 171.25, 141.25},
	{EAN_13, "012345678901", direction.LeftToRight, true, 171.25, 151.31},
	{EAN_13, "012345678901", direction.BottomToTop, false, 141.25, 171.25},
	{EAN_13, "012345678901", direction.BottomToTop, true, 151.31, 179.59},
	{EAN_13, "012345678901", direction.TopToBottom, false, 141.25, 171.25},
	{EAN_13, "012345678901", direction.TopToBottom, true, 141.25, 171.25},
	{UPC_A, "01234567890", direction.LeftToRight, false, 171.25, 141.25},
	{UPC_A, "01234567890", direction.LeftToRight, true, 179.59, 151.31},
	{UPC_A, "01234567890", direction.BottomToTop, false, 141.25, 171.25},
	{UPC_A, "01234567890", direction.BottomToTop, true, 151.31, 179.59},
	{UPC_A, "01234567890", direction.TopToBottom, false, 141.25, 171.25},
	{UPC_A, "01234567890", direction.TopToBottom, true, 141.25, 179.59},
	{CODE_128, "Hello", direction.LeftToRight, false, 167.5, 137.5},
	{CODE_128, "Hello", direction.LeftToRight, true, 167.5, 154.072},
	{CODE_128, "Hello", direction.BottomToTop, false, 137.5, 167.5},
	{CODE_128, "Hello", direction.BottomToTop, true, 154.072, 167.5},
	{CODE_128, "Hello", direction.TopToBottom, false, 137.5, 167.5},
	{CODE_128, "Hello", direction.TopToBottom, true, 137.5, 167.5},
	{CODE_39, "HELLO-39", direction.LeftToRight, false, 219.25, 137.5},
	{CODE_39, "HELLO-39", direction.LeftToRight, true, 219.25, 154.072},
	{CODE_39, "HELLO-39", direction.BottomToTop, false, 137.5, 219.25},
	{CODE_39, "HELLO-39", direction.BottomToTop, true, 154.072, 219.25},
	{CODE_39, "HELLO-39", direction.TopToBottom, false, 137.5, 219.25},
	{CODE_39, "HELLO-39", direction.TopToBottom, true, 137.5, 219.25},
	// The bearer bars of ITF-14 are around the bars and their quiet zones
	{ITF_14, "1540014128876", direction.LeftToRight, false, 211.375, 143.5},
	{ITF_14, "1540014128876", direction.LeftToRight, true, 211.375, 160.072},
	{ITF_14, "1540014128876", direction.BottomToTop, false, 143.5, 211.375},
	{ITF_14, "1540014128876", direction.BottomToTop, true, 160.072, 211.375},
	{ITF_14, "1540014128876", direction.TopToBottom, false, 143.5, 211.375},
	{ITF_14, "1540014128876", direction.TopToBottom, true, 143.5, 211.375},
}

func TestBarcodeDrawOnReturnsTheCornerOfTheBarsAndTheTextInEveryDirection(t *testing.T) {
	pdf := testNewPDF()
	page := NewPage(pdf, testLetterPortrait())
	font := testHelvetica(pdf)
	for _, row := range testBarcodeCorners {
		barcode := NewBarcode(row.barcodeType, row.text).SetDirection(row.direction)
		if row.withFont {
			barcode.SetFont(font)
		}
		barcode.SetLocation(100, 100)
		name := fmt.Sprintf("%d %v font %v", row.barcodeType, row.direction, row.withFont)
		first := barcode.DrawOn(page)
		testNear(t, name+" x", row.x, first[0], testDelta)
		testNear(t, name+" y", row.y, first[1], testDelta)
		if second := barcode.DrawOn(page); second != first {
			t.Errorf("%s drawn again: %v", name, second)
		}
		testNear(t, name+" height", row.y-100, barcode.GetHeight(), testDelta)
	}
}

func TestBarcodeCode39RejectsCharactersItCannotEncode(t *testing.T) {
	page := testNewPage()
	message, panicked := testPanic(func() { NewBarcode(CODE_39, "hello").DrawOn(page) })
	if !panicked || message != "The input string '*hello*' contains characters that are invalid in a Code39 barcode." {
		t.Errorf("panicked %v with %q", panicked, message)
	}
}

func TestBarcodeITF14NeedsThirteenDigits(t *testing.T) {
	for _, text := range []string{"154001412887", "15400141288763", "154001412887A"} {
		if message, _ := testPanic(func() { NewBarcode(ITF_14, text) }); message != "ITF-14 barcodes must have exactly 13 digits!" {
			t.Errorf("%q: %q", text, message)
		}
	}
}

// ZXing reads the ITF-14 barcodes of these GTINs back, their check digits
// added, in each direction.
func TestBarcodeITF14DrawsTheBarsAndTheBearerBars(t *testing.T) {
	page := testNewPage()
	NewBarcode(ITF_14, "1540014128876").SetLocation(100, 100).(*Barcode).DrawOn(page)
	content := string(page.buf)
	// The 39 bars: 2 of the start, 5 of each of the 7 pairs of digits and 2 of
	// the stop, then the 4 bearer bars, 3 thick
	if lines := strings.Count(content, " l\nS\n"); lines != 39+4 || !strings.Contains(content, "3 w\n") {
		t.Errorf("%d lines, bearer bars %v", lines, strings.Contains(content, "3 w\n"))
	}
}

func TestBarcodeUpcAndEanNeedTheirNumberOfDigits(t *testing.T) {
	if message, _ := testPanic(func() { NewBarcode(UPC_A, "123") }); message != "UPC-A barcodes must have exactly 11 digits!" {
		t.Errorf("UPC-A: %q", message)
	}
	if message, _ := testPanic(func() { NewBarcode(EAN_13, "0123456789012") }); message != "EAN-13 barcodes must have exactly 12 digits!" {
		t.Errorf("EAN-13: %q", message)
	}
}

func TestBarcodeCode128RefusesATextItCannotHold(t *testing.T) {
	const tooLong = "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!"
	for text, want := range map[string]string{
		strings.Repeat("A", 49): tooLong,
		strings.Repeat("é", 25): tooLong, // Two codewords each
		strings.Repeat("7", 98): tooLong, // Two digits to a codeword
		"A€":                    "Code 128 barcodes can only hold characters up to U+00FF!",
	} {
		if message, _ := testPanic(func() { NewBarcode(CODE_128, text) }); message != want {
			t.Errorf("%q: %q", text, message)
		}
	}
	// The most a barcode holds is drawn whole: 48 codewords, and the start,
	// the check digit and the stop, of 11 modules each but the stop of 13
	for _, text := range []string{strings.Repeat("A", 48), strings.Repeat("é", 24), strings.Repeat("7", 96)} {
		if width := NewBarcode(CODE_128, text).DrawOn(nil)[0]; width != (11*50+13)*0.75 {
			t.Errorf("%q is %v wide", text, width)
		}
	}
}

// ZXing reads the barcodes of these codewords back as their texts.
func TestBarcodeCode128TakesCodeSetCForRunsOfDigits(t *testing.T) {
	for _, c := range []struct {
		text  string
		start rune
		list  []rune
	}{
		{"0123456789", 105, []rune{1, 23, 45, 67, 89}},
		{"42", 105, []rune{42}},                          // Two digits alone
		{"123", 104, []rune{17, 18, 19}},                 // Too few for code set C
		{"12345", 105, []rune{12, 34, 100, 21}},          // An odd run at the start
		{"A1234B", 104, []rune{33, 99, 12, 34, 100, 34}}, // Code C and back to B
		{"A12345", 104, []rune{33, 17, 99, 23, 45}},      // The first digit of an odd run in B
		{"A12B", 104, []rune{33, 17, 18, 34}},            // Two digits stay in B
		{"12\t34ü5678", 104, []rune{17, 18, 98, 73, 19, 20, 100, 92, 99, 56, 78}},
	} {
		start, list := code128Codewords(c.text)
		if start != c.start || fmt.Sprint(list) != fmt.Sprint(c.list) {
			t.Errorf("%q: start %d, %v", c.text, start, list)
		}
	}
}

// testBarLengths returns where the bars the content draws end, each once, in
// the order of their first bar, in points from the top of a page of the height:
// a bar is a line moved to and drawn from the top of the bars.
func testBarLengths(content string, height float32) []string {
	lengths := make([]string, 0)
	seen := make(map[string]bool)
	lines := strings.Split(content, "\n")
	for i := 0; i+1 < len(lines); i++ {
		move := strings.Fields(lines[i])
		line := strings.Fields(lines[i+1])
		if len(move) == 3 && move[2] == "m" && len(line) == 3 && line[2] == "l" && move[0] == line[0] {
			y, _ := strconv.ParseFloat(line[1], 32)
			length := strconv.FormatFloat(float64(height)-y, 'f', -1, 32)
			if !seen[length] {
				seen[length] = true
				lengths = append(lengths, length)
			}
		}
	}
	return lengths
}

func TestBarcodeTheGuardBarsReachFiveModulesBelowTheOthers(t *testing.T) {
	// At a module of 2 the bars are 100 long, so the guard bars are 110.
	for _, barcodeType := range []int{EAN_13, UPC_A} {
		page := testNewPage()
		text := "012345678901"
		if barcodeType == UPC_A {
			text = "01234567890"
		}
		barcode := NewBarcode(barcodeType, text)
		barcode.SetModuleLength(2)
		barcode.SetLocation(0, 0)
		barcode.DrawOn(page)
		lengths := testBarLengths(string(page.buf), page.height)
		if len(lengths) != 2 {
			t.Fatalf("%d: the bars are of %d lengths, %v, want 2", barcodeType, len(lengths), lengths)
		}
		if lengths[0] != "110" || lengths[1] != "100" {
			t.Errorf("%d: the bars reach to %v, want the guard bars to 110 and the others to 100", barcodeType, lengths)
		}
	}
}

func TestBarcodeUPCATheBarsOfTheFirstAndTheLastDigitAreAsLongAsTheGuardBars(t *testing.T) {
	page := testNewPage()
	barcode := NewBarcode(UPC_A, "01234567890")
	barcode.SetModuleLength(2)
	barcode.SetLocation(0, 0)
	barcode.DrawOn(page)
	// The guard bars and the two bars of each of the digits outside them are
	// long: 3 guards of 2 bars and 2 digits of 2 bars.
	long := strings.Count(string(page.buf), fmt.Sprintf(" %g l\n", page.height-110))
	if long != 10 {
		t.Errorf("the long bars: %d, want 10", long)
	}
}

func TestBarcodeIsBlackWhateverPenColorThePageHas(t *testing.T) {
	page := testNewPage()
	page.SetPenColor(color.Blue)
	NewBarcode(CODE_128, "AB").DrawOn(page)
	content := string(page.buf)
	if !strings.Contains(content, "q\n0 0 0 RG\n") || !strings.HasSuffix(content, "Q\n") {
		t.Errorf("the barcode is not drawn in black in a state of its own:\n%s", content)
	}
}

// The GS1-128 barcodes below were read back with ZXing, which gave the
// symbology identifier ]C1 of GS1-128, and the fields with GS between them
// where these have FNC1 (102).
func TestBarcodeGS1128TakesCodeSetCForRunsOfDigits(t *testing.T) {
	for _, c := range []struct {
		data  string
		start rune
		list  []rune
	}{
		// An SSCC: FNC1 and the 20 digits two to a codeword
		{"(00)106141412345678908", 105, []rune{102, 0, 10, 61, 41, 41, 23, 45, 67, 89, 8}},
		// A GTIN, then a batch after a switch to code set B
		{"(01)09506000134352(10)ABC", 105, []rune{102, 1, 9, 50, 60, 0, 13, 43, 52, 10, 100, 33, 34, 35}},
		// Code set B first; one digit of an odd run in it before code set C;
		// FNC1 after the batch, a field of no set length
		{"(10)A12345B(21)7", 104, []rune{102, 17, 16, 33, 17, 99, 23, 45, 100, 34, 102, 18, 17, 23}},
	} {
		start, list := gs1128Codewords(c.data)
		if start != c.start || fmt.Sprint(list) != fmt.Sprint(c.list) {
			t.Errorf("%s: start %d, %v", c.data, start, list)
		}
	}
	// Start C, FNC1, 10 codewords, the check digit and the stop
	if width := NewBarcode(GS1_128, "(00)106141412345678908").DrawOn(nil)[0]; width != (11*13+13)*0.75 {
		t.Errorf("the SSCC is %v wide", width)
	}
}

func TestBarcodeGS1128RefusesDataThatIsNotGS1OrTooLong(t *testing.T) {
	NewBarcode(GS1_128, "(91)"+strings.Repeat("X", 46)) // 48 characters
	for data, want := range map[string]string{
		"(91)" + strings.Repeat("X", 47): "GS1-128 barcodes hold at most 48 characters, not counting the separators!",
		"(01)09506000134353":             "The check digit of (01) is wrong!",
		"01095060001343":                 "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!",
	} {
		if message, _ := testPanic(func() { NewBarcode(GS1_128, data) }); message != want {
			t.Errorf("%q: %q", data, message)
		}
	}
}
