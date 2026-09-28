// review_codes_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package qrcode

import (
	"bufio"
	"bytes"
	"strings"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestQRCodeTextThatIsNotASCIIStartsWithTheECIOfUTF8(t *testing.T) {
	// ECI 0111, the assignment number 26 in 8 bits, then byte mode 0100
	// Version 4 at level L has one block, so the data codewords come first
	data := NewQRCode("Grüße", errorcorrectionlevel.L).createData(errorcorrectionlevel.L)
	if data[0] != 0x71 || data[1]>>4 != 0xA || data[1]&0x0F != 0x4 {
		t.Errorf("the data starts with %x", data[:2])
	}
	// ASCII starts with byte mode, as before
	if data := NewQRCode("Hello", errorcorrectionlevel.L).createData(errorcorrectionlevel.L); data[0] != 0x40 || data[1] != 0x54 {
		t.Errorf("the data starts with %x", data[:2])
	}
}

func TestQRCodeTheECIIsCountedInTheCapacity(t *testing.T) {
	// 2,953 bytes fit at level L, and 2,952 when they are not all ASCII
	fits := strings.Repeat("é", 1476)
	if n := len(NewQRCode(fits, errorcorrectionlevel.L).GetModules()); n != 177 {
		t.Errorf("%d modules", n)
	}
	defer func() {
		if r := recover(); r != "The data is too long for a QR code at level L: 2953 bytes, at most 2952." {
			t.Errorf("%v", r)
		}
	}()
	NewQRCode(fits+"a", errorcorrectionlevel.L)
}

func TestQRCodeThePenaltyIsThatOfISO18004(t *testing.T) {
	light := func(n int) [][]bool {
		matrix := make([][]bool, n)
		for i := range matrix {
			matrix[i] = make([]bool, n)
		}
		return matrix
	}
	// 5 by 5 light modules: N1 10 runs of 5, 30; N2 16 blocks, 48; N4 no
	// dark module, 100.
	if lostPoint := getLostPoint(light(5)); lostPoint != 178 {
		t.Errorf("5 by 5 light modules: %d", lostPoint)
	}
	// 7 by 7 with 1011101 in the first row: N1 60; N2 30 blocks, 90; N3 the
	// pattern after the light quiet zone, 40; N4 5 of 49 dark, 70.
	matrix := light(7)
	for col, dark := range []bool{true, false, true, true, true, false, true} {
		matrix[0][col] = dark
	}
	if lostPoint := getLostPoint(matrix); lostPoint != 260 {
		t.Errorf("7 by 7 with a finder like pattern: %d", lostPoint)
	}
}

func TestQRCodeTheFormatInformationHasTheMaskOfTheLowestPenalty(t *testing.T) {
	qr := NewQRCode("https://pdfjet.com", errorcorrectionlevel.M)
	// The mask of the format information, read back from the modules
	bits := 0
	for i := 0; i < 15; i++ {
		row := i
		if i >= 6 {
			row = i + 1
		}
		if i >= 8 {
			row = qr.moduleCount - 15 + i
		}
		if qr.modules[row][8] {
			bits |= 1 << i
		}
	}
	mask := ((bits ^ g15Mask()) >> 10) & 7
	// The penalty of each mask, with the modules made again unmasked
	qr.modules = newMatrix(qr.moduleCount)
	qr.reserved = newMatrix(qr.moduleCount)
	qr.setupPositionProbePattern(0, 0)
	qr.setupPositionProbePattern(qr.moduleCount-7, 0)
	qr.setupPositionProbePattern(0, qr.moduleCount-7)
	qr.setupPositionAdjustPattern()
	qr.setupTimingPattern()
	qr.setupTypeInfo(qr.modules, 0)
	qr.mapData(qr.createData(qr.errorCorrectionLevel))
	penalty := make([]int, 8)
	for i := range penalty {
		penalty[i] = getLostPoint(qr.applyMask(i))
	}
	// Every other mask has a higher penalty, or the same and a higher number
	for i := range penalty {
		if penalty[i] < penalty[mask] || (penalty[i] == penalty[mask] && i < mask) {
			t.Errorf("mask %d has the penalty %d, mask %d of the symbol %d", i, penalty[i], mask, penalty[mask])
		}
	}
}

func TestQRCodeTheDarkModulesOfARowAreOneRectangle(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	pdf.SetCompliance(compliance.PDF_UA_1)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.SetBrushColor(color.Blue)
	qr := NewQRCode("https://pdfjet.com", errorcorrectionlevel.M).SetModuleColor(color.Red)
	qr.DrawOn(page)
	content := string(page.GetContent())
	runs := 0
	for _, row := range qr.modules {
		for col := range row {
			if row[col] && (col == 0 || !row[col-1]) {
				runs++
			}
		}
	}
	if n := strings.Count(content, " re\n"); n != runs {
		t.Errorf("%d rectangles for %d runs of dark modules", n, runs)
	}
	// The brush is saved and restored around the modules
	if !strings.Contains(content, "/Artifact BMC\nq\n") || !strings.HasSuffix(content, "Q\nEMC\n") {
		t.Errorf("the modules are not in q and Q: %.40q ... %q", content, content[len(content)-20:])
	}
	// The page knows the brush is blue again, and sets red when asked
	page.SetBrushColor(color.Red)
	if !strings.HasSuffix(string(page.GetContent()), "EMC\n1 0 0 rg\n") {
		t.Errorf("the brush was not set after the QR code: %q", string(page.GetContent())[len(content)-20:])
	}
}
