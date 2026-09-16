// drawable_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// The drawables of the qrcode, pdf417 and datamatrix packages import this
// package, so this test is in a package of its own.
package pdfjet_test

import (
	"bufio"
	"bytes"
	"fmt"
	"math"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/datamatrix"
	"github.com/edragoev1/pdfjet/v9/src/errorcorrectionlevel"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/pdf417"
	"github.com/edragoev1/pdfjet/v9/src/qrcode"
)

type testMaker struct {
	name string
	make func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable
}

// testOpenRepoFile opens a file of the repository, found above the working directory.
func testOpenRepoFile(t *testing.T, path string) *os.File {
	t.Helper()
	dir, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	for {
		if info, err := os.Stat(filepath.Join(dir, "PngSuite")); err == nil && info.IsDir() {
			break
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			t.Fatal("no PngSuite directory above the working directory")
		}
		dir = parent
	}
	file, err := os.Open(filepath.Join(dir, path))
	if err != nil {
		t.Fatal(err)
	}
	t.Cleanup(func() { file.Close() })
	return file
}

func testDrawables() []testMaker {
	return []testMaker{
		{"TextBlock", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewTextBlock(font, "Hello world, this is a text block that wraps.").SetWidth(100)
		}},
		{"TextColumn", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewTextColumn().SetWidth(120).
				AddParagraph(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(font, "Hello column text that wraps around here")))
		}},
		{"TextFrame", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewTextFrame(font, []string{"Hello frame text"}).SetWidth(120).SetHeight(200)
		}},
		{"TextLine", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewTextLine(font, "Hello line")
		}},
		{"CompositeTextLine", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewCompositeTextLine(0, 0).AddComponent(pdfjet.NewTextLine(font, "Composite"))
		}},
		{"Title", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewTitle(font, "Title", 0, 0)
		}},
		{"Image", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewImage(pdf, bufio.NewReader(testOpenRepoFile(t, "images/up-arrow.png")))
		}},
		{"SVGImage", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			image, err := pdfjet.NewSVGImage(testOpenRepoFile(t, "images/svg/arrow_forward_FILL0_wght400_GRAD0_opsz48.svg"))
			if err != nil {
				t.Fatal(err)
			}
			return image
		}},
		{"Barcode", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewBarcode(pdfjet.CODE_128, "Hello")
		}},
		{"QRCode", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return qrcode.NewQRCode("Hello", errorcorrectionlevel.M)
		}},
		{"PDF417", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdf417.NewPDF417("Hello")
		}},
		{"DataMatrix", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return datamatrix.NewDataMatrix("Hello")
		}},
		{"Chart", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			chart := pdfjet.NewChart(font, font)
			chart.SetSize(200, 150)
			chart.AddSeries("s").AddPoint(0, 0).AddPoint(1, 2)
			return chart
		}},
		{"BarChart", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewBarChart(font, font).SetSize(200, 150).AddSeries("s", []float32{1, 2, 3})
		}},
		{"DonutChart", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewDonutChart(font, font).SetRadii(50, 20).
				AddSlice(pdfjet.NewSlice(1, color.Red, "a")).AddSlice(pdfjet.NewSlice(2, color.Green, "b"))
		}},
		{"Table", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			data := make([][]*pdfjet.Cell, 0)
			for r := 0; r < 3; r++ {
				row := make([]*pdfjet.Cell, 0)
				for c := 0; c < 2; c++ {
					row = append(row, pdfjet.NewCell(font, fmt.Sprintf("r%dc%d", r, c)))
				}
				data = append(data, row)
			}
			return pdfjet.NewTable().SetTableData(data, 1)
		}},
		{"Rect", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewRect(0, 0, 50, 30).SetBorderColor(color.Black)
		}},
		{"Container", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewContainer(80, 40).Add(pdfjet.NewRect(0, 0, 80, 40).SetBorderColor(color.Black))
		}},
		{"CalendarMonth", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewCalendarMonth(font, font, 2026, 9)
		}},
		{"Form", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewForm([]*pdfjet.Field{pdfjet.NewField(0, "Name", "John")}).SetLabelFont(font).SetValueFont(font)
		}},
		{"CheckBox", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewCheckBox(font, "Check")
		}},
		{"RadioButton", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewRadioButton(font, "Radio")
		}},
		{"Line", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewLine(0, 0, 50, 20)
		}},
		{"Arc", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewArc().SetRadius(20).SetSweep(90)
		}},
		{"Point", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewPoint(0, 0).SetRadius(5)
		}},
		{"Path", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewPath().Add(pdfjet.NewPoint(0, 0)).Add(pdfjet.NewPoint(30, 20))
		}},
		{"Stamp", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			stamp := pdfjet.NewStamp(pdf).SetSize(50, 30)
			stamp.Complete()
			return stamp
		}},
		{"SquareAnnotation", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewSquareAnnotation().SetSize(30, 20)
		}},
		{"FileAttachment", func(t *testing.T, pdf *pdfjet.PDF, font *pdfjet.Font) pdfjet.Drawable {
			return pdfjet.NewFileAttachment(pdfjet.NewEmbeddedFile(pdf, "hello.txt", strings.NewReader("Hello"), false))
		}},
	}
}

func testNewDrawablePDF() (*pdfjet.PDF, *pdfjet.Font) {
	pdf := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	return pdf, pdfjet.NewCoreFont(pdf, corefont.Helvetica())
}

func testDrawDrawable(t *testing.T, maker testMaker, measureFirst bool) ([2]float32, string) {
	pdf, font := testNewDrawablePDF()
	drawable := maker.make(t, pdf, font)
	drawable.SetLocation(40, 60)
	if measureFirst {
		drawable.DrawOn(nil)
	}
	page := pdfjet.NewPage(pdf, letter.Portrait())
	xy := drawable.DrawOn(page)
	return xy, string(page.GetContent())
}

// Every Drawable measures itself with DrawOn(nil): it draws nothing, returns
// the corner that drawing returns, and does not change what drawing draws.
func TestDrawOnNilMeasuresWithoutDrawing(t *testing.T) {
	failures := make([]string, 0)
	for _, maker := range testDrawables() {
		func() {
			defer func() {
				if r := recover(); r != nil {
					failures = append(failures, fmt.Sprintf("%s: %v", maker.name, r))
				}
			}()
			pdf, font := testNewDrawablePDF()
			drawable := maker.make(t, pdf, font)
			drawable.SetLocation(40, 60)
			measured := drawable.DrawOn(nil)
			drawn, plain := testDrawDrawable(t, maker, false)
			drawnAfterMeasuring, afterMeasuring := testDrawDrawable(t, maker, true)
			if math.Abs(float64(measured[0]-drawn[0])) > 0.01 || math.Abs(float64(measured[1]-drawn[1])) > 0.01 {
				failures = append(failures, fmt.Sprintf("%s: measured %v, drawn %v", maker.name, measured, drawn))
			}
			if plain != afterMeasuring || drawn != drawnAfterMeasuring {
				failures = append(failures, maker.name+": measuring first changes what is drawn")
			}
		}()
	}
	if len(failures) > 0 {
		t.Error("\n" + strings.Join(failures, "\n"))
	}
}

func TestDrawableTablesAndTextColumnsReturnTheirRightEdge(t *testing.T) {
	makers := testDrawables()
	for _, maker := range makers {
		if maker.name != "Table" && maker.name != "TextColumn" {
			continue
		}
		pdf, font := testNewDrawablePDF()
		drawable := maker.make(t, pdf, font)
		want := float32(160)
		if table, ok := drawable.(*pdfjet.Table); ok {
			want = 40 + table.GetWidth()
		}
		xy := drawable.SetLocation(40, 60).DrawOn(pdfjet.NewPage(pdf, letter.Portrait()))
		if math.Abs(float64(xy[0]-want)) > 0.01 {
			t.Errorf("%s: x %v, want %v", maker.name, xy[0], want)
		}
	}
}
