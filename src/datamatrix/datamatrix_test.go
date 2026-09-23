// datamatrix_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package datamatrix

import (
	"bufio"
	"bytes"
	"math"
	"strings"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestDataMatrixSixDigitsFitTheSmallestSquareWithItsFinderPattern(t *testing.T) {
	modules := NewDataMatrix("123456").GetModules()
	if len(modules) != 10 || len(modules[0]) != 10 {
		t.Fatalf("size %d x %d", len(modules), len(modules[0]))
	}
	for i := 0; i < 10; i++ {
		if modules[0][i] != (i%2 == 0) {
			t.Errorf("top row module %d", i)
		}
		if !modules[9][i] || !modules[i][0] {
			t.Errorf("bottom row or left column module %d is light", i)
		}
	}
}

func TestDataMatrixLongerDataGetsALargerSymbol(t *testing.T) {
	if got := len(NewDataMatrix(strings.Repeat("Z", 60)).GetModules()); got != 32 {
		t.Errorf("size %d", got)
	}
}

func TestDataMatrixTheRectangleShapeIsWiderThanTall(t *testing.T) {
	modules := NewDataMatrixWithShape("Hello, World!", Rectangle).GetModules()
	if len(modules) != 12 || len(modules[0]) != 26 {
		t.Errorf("size %d x %d", len(modules), len(modules[0]))
	}
}

func TestDataMatrixDrawOnReturnsTheCornerOfTheModules(t *testing.T) {
	page := pdfjet.NewPage(pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer))), letter.Portrait())
	dm := NewDataMatrix("123456").SetModuleLength(3)
	dm.SetLocation(5, 5)
	xy := dm.DrawOn(page)
	if math.Abs(float64(xy[0]-35)) > 0.01 || math.Abs(float64(xy[1]-35)) > 0.01 {
		t.Errorf("corner %v", xy)
	}
}

func TestDataMatrixInAPDFUADocumentTheModulesAreAnArtifact(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	NewDataMatrix("PDFjet").DrawOn(page)
	content := string(page.GetContent())
	if !strings.HasPrefix(content, "/Artifact BMC\n") || !strings.HasSuffix(content, "EMC\n") {
		t.Errorf("the modules are not an artifact: %q", content)
	}
}

// The GS1 symbols below were read back with ZXing, which gave the symbology
// identifier ]d2 of GS1 DataMatrix and these element strings.
func TestGS1DataMatrixWritesTheElementStringWithGSAfterAFieldOfNoSetLength(t *testing.T) {
	for data, want := range map[string]string{
		"(01)09506000134352(17)261231(10)ABC123(21)XYZ-42": "01095060001343521726123110ABC123\x1d21XYZ-42",
		"(10)BATCH7(21)SN001(01)09506000134352":            "10BATCH7\x1d21SN001\x1d0109506000134352",
		"(00)106141412345678908":                           "00106141412345678908",
		"(01)09506000134352(3103)000750(15)270101":         "0109506000134352310300075015270101",
	} {
		if got := gs1ElementString(data); got != want {
			t.Errorf("%s: %q", data, got)
		}
	}
}

func TestGS1DataMatrixStartsWithFNC1AndEncodesInASCII(t *testing.T) {
	gs1 := NewGS1DataMatrix("(01)09506000134352(10)ABC")
	plain := NewDataMatrix("0109506000134352\x1d10ABC")
	if len(gs1.codewords) == 0 || gs1.codewords[0] != 232 {
		t.Fatalf("the first codeword is not FNC1: %v", gs1.codewords)
	}
	// The same characters after FNC1, two digits to a codeword
	for i, want := range []int{131, 139, 180, 190, 130, 143, 173, 182, 140, 66, 67, 68} {
		if gs1.codewords[1+i] != want {
			t.Errorf("codeword %d is %d, not %d", 1+i, gs1.codewords[1+i], want)
		}
	}
	// FNC1 and 12 codewords, as the plain text takes 13 with GS: 18 by 18 holds 18
	if len(gs1.GetModules()) != 18 || len(plain.GetModules()) != 18 {
		t.Errorf("sizes %d and %d", len(gs1.GetModules()), len(plain.GetModules()))
	}
	if rect := NewGS1DataMatrixWithShape("(01)09506000134352", Rectangle).GetModules(); len(rect) >= len(rect[0]) {
		t.Errorf("the rectangle is %d by %d", len(rect), len(rect[0]))
	}
}

func TestGS1DataMatrixRefusesDataThatIsNotGS1(t *testing.T) {
	const format = "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!"
	for data, want := range map[string]string{
		"":                               format,
		"01)09506000134352":              format,
		"(01":                            format,
		"(1)5":                           "The Application Identifier (1) is not two to four digits!",
		"(12345)5":                       "The Application Identifier (12345) is not two to four digits!",
		"(A1)5":                          "The Application Identifier (A1) is not two to four digits!",
		"(10)(21)SN":                     "The Application Identifier (10) has no data!",
		"(10)AB C":                       "The data of (10) has a character that GS1 does not allow!",
		"(10)AB)C":                       "The data of (10) has a character that GS1 does not allow!",
		"(10)Grüße":                      "The data of (10) has a character that GS1 does not allow!",
		"(91)" + strings.Repeat("X", 91): "The data of (91) is longer than 90 characters!",
		"(01)0950600013435":              "The data of (01) must be 14 digits!",
		"(17)2612A1":                     "The data of (17) must be 6 digits!",
		"(3103)00075":                    "The data of (3103) must be 6 digits!",
		"(01)09506000134353":             "The check digit of (01) is wrong!",
		"(00)106141412345678909":         "The check digit of (00) is wrong!",
		"(414)9506000134353":             "The check digit of (414) is wrong!",
	} {
		message := testPanic(func() { NewGS1DataMatrix(data) })
		if message != want {
			t.Errorf("%q: %q", data, message)
		}
	}
	// Fields of no set length, such as (10) and (21), take any data GS1 allows,
	// and (418), a GLN of no check digit here, takes any 13 digits
	NewGS1DataMatrix("(10)!\"%&'*+,-./:;<=>?_az(21)1(418)1234567890123")
}

// testPanic returns what the function panicked with, or "" if it did not.
func testPanic(f func()) (message string) {
	defer func() {
		if r := recover(); r != nil {
			message = r.(string)
		}
	}()
	f()
	return ""
}
