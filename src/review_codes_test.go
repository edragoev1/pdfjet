// review_codes_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/direction"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/internal/code128"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// testBBox returns the bounding box of the structure element, in the
// coordinates of the PDF.
func testBBox(t *testing.T, element *structElement) [4]float32 {
	t.Helper()
	match := regexp.MustCompile(`/BBox \[(\S+) (\S+) (\S+) (\S+)\]`).FindStringSubmatch(element.attributes)
	if match == nil {
		t.Fatalf("no /BBox in %q", element.attributes)
	}
	var box [4]float32
	for i := range box {
		value, _ := strconv.ParseFloat(match[i+1], 32)
		box[i] = float32(value)
	}
	return box
}

func TestChartFlatDataOfLargeValuesHasARange(t *testing.T) {
	for _, value := range []float32{2e7, -3e7, 1e30} {
		doc := testNewDoc()
		page := NewPage(doc.pdf, testLetterPortrait())
		font := testHelvetica(doc.pdf)
		chart := NewSimpleScatterChart(font, font).SetSize(300, 200)
		chart.SetLocation(50, 50)
		chart.Add(value, value).Add(value, value)
		chart.DrawOn(page)
		if strings.Contains(testContent(page), "NaN") {
			t.Errorf("%v: NaN in the content", value)
		}
		if err := doc.pdf.Complete(); err != nil {
			t.Errorf("%v: %v", value, err)
		}
	}
}

func TestChartTheRoundedRangeOfFlatDataHasAGridLine(t *testing.T) {
	for _, value := range []float32{0, 5, 2e7, -3e7, 1e30} {
		round := roundMaxAndMinValues(value, value)
		if !(round.maxValue > round.minValue) || round.numOfGridLines < 1 {
			t.Errorf("%v: %v to %v with %d grid lines", value, round.minValue, round.maxValue, round.numOfGridLines)
		}
	}
}

// Data with no number, or whose span does not fit a float32, has the range
// from 0 to 1, where Swift stopped the program (review of 12 October 2026,
// R3). Data whose span fits, but whose rounded range would not, has the range
// of its values: from 0 to 1 the bars went past the plot (review of 26
// October 2026).
func TestChartTheRangeOfDataPastTheLargestFloatIsFromZeroToOne(t *testing.T) {
	nan := float32(math.NaN())
	inf := float32(math.Inf(1))
	for _, c := range [][2]float32{{inf, 0}, {0, -inf}, {nan, nan}, {3.4e38, -3.4e38},
		{-math.MaxFloat32, math.MaxFloat32}} {
		round := roundMaxAndMinValues(c[0], c[1])
		if round.minValue != 0 || round.maxValue != 1 || round.numOfGridLines != 1 {
			t.Errorf("%v: %v to %v with %d grid lines", c, round.minValue, round.maxValue, round.numOfGridLines)
		}
	}
	for _, high := range []float32{math.MaxFloat32, 3e38} {
		round := roundMaxAndMinValues(high, 0)
		if round.minValue != 0 || round.maxValue != high || round.numOfGridLines < 1 {
			t.Errorf("%v: %v to %v with %d grid lines", high, round.minValue, round.maxValue, round.numOfGridLines)
		}
	}
}

// Stacked bars whose sums overflow a float32 are drawn on the range from 0
// to 1.
func TestBarChartStackedBarsPastTheLargestFloatAreDrawn(t *testing.T) {
	doc := testNewDoc()
	page := NewPage(doc.pdf, testLetterPortrait())
	chart := testBarChart(doc.pdf).SetCategories("a").SetStacked(true)
	chart.AddSeries("", []float32{3e38})
	chart.AddSeries("", []float32{3e38})
	if content := testDrawBarChart(t, chart, page); !strings.Contains(content, testHex("1")) {
		t.Errorf("the axis is not from 0 to 1: %q", content)
	}
	if err := doc.pdf.Complete(); err != nil {
		t.Error(err)
	}
}

func TestBarChartAValueThatIsNotANumberHasNoBar(t *testing.T) {
	nan := float32(math.NaN())
	inf := float32(math.Inf(1))
	for _, stacked := range []bool{false, true} {
		doc := testNewDoc()
		page := NewPage(doc.pdf, testLetterPortrait())
		chart := testBarChart(doc.pdf).SetCategories("a", "b", "c").SetStacked(stacked).SetValueLabels(true)
		chart.AddSeries("", []float32{nan, 10, inf})
		content := testDrawBarChart(t, chart, page)
		if strings.Contains(content, "NaN") || strings.Contains(content, testHex("NaN")) {
			t.Errorf("stacked %v: NaN in the content", stacked)
		}
		if !strings.Contains(content, testHex("10")) {
			t.Errorf("stacked %v: the axis does not reach 10", stacked)
		}
		if err := doc.pdf.Complete(); err != nil {
			t.Errorf("stacked %v: %v", stacked, err)
		}
	}
}

func TestBarcodeCode128TakesACharacterFrom128To159AsFNC4ShiftAndTheControlCharacter(t *testing.T) {
	start, list := code128Codewords("\u0085x\u009f")
	want := []rune{code128.FNC4, code128.Shift, 0x05 + 64, 'x' - 32, code128.FNC4, code128.Shift, 0x1f + 64}
	if start != code128.StartB || string(list) != string(want) {
		t.Errorf("start %d, codewords %v", start, list)
	}
	// Each takes three of the 48 codewords
	NewBarcode(CODE_128, strings.Repeat("\u0080", 16)).DrawOn(testNewPage())
	message, _ := testPanic(func() { NewBarcode(CODE_128, strings.Repeat("\u0080", 17)) })
	if !strings.Contains(message, "one from 128 to 159 three") {
		t.Errorf("17 characters from 128 to 159: %q", message)
	}
}

func TestBarcodeTheFigureHasTheBoxOfTheBarsAndTheDigits(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1)
	font := NewFontFromFile(pdf, testRepoPath(t, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"))
	page := NewPage(pdf, letter.Portrait())
	height := page.height
	cases := []struct {
		barcodeType int
		text        string
		direction   direction.Direction
	}{
		{EAN_13, "400638133393", direction.LeftToRight},
		{UPC_A, "03600029145", direction.LeftToRight},
		{UPC_A, "03600029145", direction.TopToBottom},
		{CODE_128, "1111111111111111", direction.LeftToRight},
	}
	for i, c := range cases {
		barcode := NewBarcode(c.barcodeType, c.text).SetDirection(c.direction)
		barcode.SetFont(font)
		barcode.SetAltDescription(c.text)
		barcode.SetLocation(100, 100)
		xy := barcode.DrawOn(page)
		box := testBBox(t, page.structures[i])
		// The first digit of EAN-13 and UPC-A is left of the bars, and the
		// digits of a barcode drawn top to bottom left of them, the first
		// above them; the digits of Code 128 are wider than its bars.
		if box[0] >= 100 {
			t.Errorf("case %d: the box starts at x %v, right of the digits", i, box[0])
		}
		top := height - box[3]
		if c.direction == direction.TopToBottom && top >= 100 {
			t.Errorf("case %d: the box has its top at %v, under the first digit", i, top)
		} else if c.direction == direction.LeftToRight && math.Abs(float64(top-100)) > 0.01 {
			t.Errorf("case %d: the box has its top at %v", i, top)
		}
		if math.Abs(float64(box[2]-xy[0])) > 0.01 || math.Abs(float64(box[1]-(height-xy[1]))) > 0.01 {
			t.Errorf("case %d: the box %v does not end at %v", i, box, xy)
		}
	}
}

func TestDonutChartTheFigureHasTheBoxOfTheLabels(t *testing.T) {
	pdf := testNewPDF()
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := NewPage(pdf, testLetterPortrait())
	testDonutChart(pdf).AddWithColor("Apples and pears", 25, color.Red).
		AddWithColor("Oranges", 75, color.Blue).DrawOn(page)
	box := testBBox(t, page.structures[0])
	// The circle is from 100 to 300; the labels are right and left of it
	if box[0] >= 100 || box[2] <= 300 {
		t.Errorf("the box %v is not wider than the circle", box)
	}
}

func TestDecryptorAnAESKeyThatIsTooShortIsRefused(t *testing.T) {
	encrypt := newPDFobj()
	encrypt.number = 6
	encrypt.dict = strings.Split("6 0 obj << /Filter /Standard /V 5 /R 4 /Length 40 "+
		"/CF << /StdCF << /CFM /AESV2 >> >> /StmF /StdCF /StrF /StdCF /O <00> /U <00> /P -4 >> endobj", " ")
	_, err := newDecryptor(encrypt, nil, nil, "")
	if err == nil || err.Error() != "The encryption of the PDF is not valid: /R 4 with a key of 40 bits for AES" {
		t.Errorf("error %v", err)
	}
	// A decryptor of such a key decrypts to nothing
	d := &decryptor{key: make([]byte, 5), streamMethod: cryptAES128, stringMethod: cryptAES128}
	if decrypted := d.decrypt(make([]byte, 48), cryptAES128, encrypt); len(decrypted) != 0 {
		t.Errorf("decrypted %v", decrypted)
	}
}

func TestDecryptorTheContentsOfASignatureAreNotDecrypted(t *testing.T) {
	d := &decryptor{key: make([]byte, 16), streamMethod: cryptRC4, stringMethod: cryptRC4}
	signature := "<0123456789ABCDEF>"
	for _, raw := range []string{
		"7 0 obj << /Type /Sig /Filter /Adobe.PPKLite /Contents " + signature + " >> endobj",
		"7 0 obj << /ByteRange [ 0 10 20 30 ] /Contents " + signature + " >> endobj",
		"7 0 obj << /FT /Sig /V << /Type /Sig /Contents " + signature + " >> /T (Signature1) >> endobj",
	} {
		obj := newPDFobj()
		obj.number = 7
		obj.dict = strings.Split(raw, " ")
		d.decryptStrings(obj)
		if !strings.Contains(strings.Join(obj.dict, " "), "/Contents "+signature) {
			t.Errorf("the signature was decrypted: %v", obj.dict)
		}
	}
	// The /Contents of another dictionary is decrypted
	obj := newPDFobj()
	obj.number = 7
	obj.dict = strings.Split("7 0 obj << /Type /Annot /Contents (Note) >> endobj", " ")
	d.decryptStrings(obj)
	if strings.Contains(strings.Join(obj.dict, " "), "(Note)") {
		t.Errorf("the note was not decrypted: %v", obj.dict)
	}
}

func TestEncryptionThePermissionsOfTheCallerAreNotChanged(t *testing.T) {
	testAllowCoreFonts(t)
	permissions := encryption.NewPermissions().SetAccess(encryption.Print)
	pdf := testEncrypted(t, compliance.PDF_UA_1,
		encryption.NewPasswords().SetUserPassword("").SetOwnerPassword("owner"), permissions)
	if permissions.GetAccess() != encryption.Print {
		t.Errorf("the permissions are %v", permissions.GetAccess())
	}
	// The PDF grants the extraction for accessibility all the same
	if !encryption.ExtractContentsForAccessibility.IsSetIn(encryption.UserAccess(testAccessValue(t, pdf))) {
		t.Error("the PDF/UA does not grant the extraction for accessibility")
	}
}
