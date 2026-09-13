// pdf417_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdf417

import (
	"bufio"
	"bytes"
	"math"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func TestPDF417DrawingTwiceDoesNotMoveTheSymbol(t *testing.T) {
	page := pdfjet.NewPage(pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer))), letter.Portrait())
	symbol := NewPDF417("Hello, World!")
	for i := 0; i < 2; i++ {
		xy := symbol.DrawOn(page)
		if math.Abs(float64(xy[0]-281.25)) > 0.01 || math.Abs(float64(xy[1]-11.25)) > 0.01 {
			t.Errorf("draw %d: corner %v", i+1, xy)
		}
	}
}
