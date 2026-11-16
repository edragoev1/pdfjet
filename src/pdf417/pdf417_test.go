// pdf417_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdf417

import (
	"bufio"
	"bytes"
	"math"
	"strings"
	"testing"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/capstyle"
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

// The bars have butt caps whatever cap the page was left with: a projecting
// or round cap made each bar, one row tall, reach half its width into the
// rows above and below it (ISO 32000, 8.4.3.3), and no reader read the symbol.
func TestPDF417TheBarsHaveButtCapsOnAPageOfProjectingCaps(t *testing.T) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	page := pdfjet.NewPage(pdf, letter.Portrait())
	page.SetLineCapStyle(capstyle.ProjectingSquare)
	NewPDF417("Hello, PDF417 with projecting caps").SetLocation(50, 50).DrawOn(page)
	if content := string(page.GetContent()); !strings.Contains(content, "q\n0 0 0 RG\n[] 0 d\n0 J\n") {
		t.Errorf("the bars have the page's caps: %.80q", content)
	}
}
