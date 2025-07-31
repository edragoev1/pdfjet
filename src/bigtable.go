/*
  - bigtable.go

©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package pdfjet

import (
	"bufio"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/align"
	"github.com/edragoev1/pdfjet/src/color"
)

// BigTable represents a table for handling large amounts of data in PDF
type BigTable struct {
	pdf             *PDF
	f1              *Font
	f2              *Font
	pageSize        [2]float32
	y               float32
	yText           float32
	pages           []*Page
	page            *Page
	widths          []float32
	headerFields    []string
	alignment       []int
	vertLines       []float32
	bottomMargin    float32
	padding         float32
	language        string
	highlight       bool
	highlightColor  int32
	penColor        int32
	fileName        string
	delimiter       string
	numberOfColumns int
	startNewPage    bool
}

// NewBigTable creates a new BigTable instance
func NewBigTable(pdf *PDF, f1 *Font, f2 *Font, pageSize [2]float32) *BigTable {
	return &BigTable{
		pdf:            pdf,
		f1:             f1,
		f2:             f2,
		pageSize:       pageSize,
		pages:          make([]*Page, 0),
		bottomMargin:   20.0,
		padding:        2.0,
		language:       "en-US",
		highlight:      true,
		highlightColor: 0xF0F0F0,
		penColor:       0xB0B0B0,
		startNewPage:   true,
	}
}

// SetLocation sets the table location
func (bt *BigTable) SetLocation(x, y float32) {
	for i := 0; i <= bt.numberOfColumns; i++ {
		bt.vertLines[i] += x
	}
	bt.y = y
}

// SetNumberOfColumns sets the number of columns
func (bt *BigTable) SetNumberOfColumns(numberOfColumns int) {
	bt.numberOfColumns = numberOfColumns
}

// SetTextAlignment sets text alignment for a column
func (bt *BigTable) SetTextAlignment(column, alignment int) {
	bt.alignment[column] = alignment
}

// SetBottomMargin sets the bottom margin
func (bt *BigTable) SetBottomMargin(bottomMargin float32) {
	bt.bottomMargin = bottomMargin
}

// SetLanguage sets the language
func (bt *BigTable) SetLanguage(language string) {
	bt.language = language
}

// GetPages returns the generated pages
func (bt *BigTable) GetPages() []*Page {
	return bt.pages
}

func (bt *BigTable) drawTextAndLine(fields []string, font *Font) error {
	if len(fields) < bt.numberOfColumns {
		return nil
	}
	if bt.page == nil { // First page
		bt.page = NewPageDetached(bt.pdf, bt.pageSize)
		bt.pages = append(bt.pages, bt.page)
		bt.page.SetPenWidth(0.0)
		bt.yText = bt.y + bt.f1.ascent
		bt.highlight = true
		bt.drawFieldsAndLine(bt.headerFields, bt.f1)
		bt.yText += (-bt.f1.descent) + bt.f2.ascent
		bt.startNewPage = false
		return nil
	}

	if bt.startNewPage { // New page
		bt.page = NewPageDetached(bt.pdf, bt.pageSize)
		bt.pages = append(bt.pages, bt.page)
		bt.page.SetPenWidth(0.0)
		bt.yText = bt.y + bt.f1.ascent
		bt.highlight = true
		bt.drawFieldsAndLine(bt.headerFields, bt.f1)
		bt.yText += (-bt.f1.descent) + bt.f2.ascent
		bt.startNewPage = false
	}

	bt.drawFieldsAndLine(fields, font)
	bt.yText += font.ascent - font.descent
	if bt.yText > (bt.page.GetHeight() - bt.bottomMargin) {
		bt.drawTheVerticalLines()
		bt.startNewPage = true
	}

	return nil
}

func (bt *BigTable) drawFieldsAndLine(fields []string, font *Font) {
	bt.page.AddArtifactBMC()
	if bt.highlight {
		bt.highlightRow(bt.page, font, bt.highlightColor)
		bt.highlight = false
	} else {
		bt.highlight = true
	}

	original := bt.page.GetPenColor()
	bt.page.SetPenColor(bt.penColor)
	bt.page.MoveTo(bt.vertLines[0], bt.yText-font.ascent)
	bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent)
	bt.page.StrokePath()
	bt.page.SetPenColorWithFloat32Array(original)
	bt.page.AddEMC()

	rowText := bt.getRowText(fields)
	bt.page.AddBMC("P", bt.language, rowText, rowText)
	bt.page.SetPenWidth(0.0)
	bt.page.SetTextFont(font)
	bt.page.SetBrushColor(color.Black)
	for i := 0; i < bt.numberOfColumns; i++ {
		text := fields[i]
		xText1 := bt.vertLines[i] + bt.padding
		xText2 := bt.vertLines[i+1] - bt.padding
		bt.page.BeginText()
		switch bt.alignment[i] {
		case align.Left: // Align Left
			bt.page.SetTextLocation(xText1, bt.yText)
		case align.Right: // Align Right
			bt.page.SetTextLocation(xText2-font.StringWidth(nil, text), bt.yText)
		}
		bt.page.DrawText(text)
		bt.page.EndText()
	}
	bt.page.AddEMC()
}

func (bt *BigTable) highlightRow(page *Page, font *Font, color int32) {
	original := page.GetBrushColor()
	page.SetBrushColor(color)
	page.MoveTo(bt.vertLines[0], bt.yText-font.ascent)
	page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent)
	page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.descent)
	page.LineTo(bt.vertLines[0], bt.yText-font.descent)
	page.FillPath()
	page.SetBrushColorWithFloat32Array(original)
}

func (bt *BigTable) drawTheVerticalLines() {
	bt.page.AddArtifactBMC()
	original := bt.page.GetPenColor()
	bt.page.SetPenColor(bt.penColor)
	for i := 0; i <= bt.numberOfColumns; i++ {
		bt.page.DrawLine(
			bt.vertLines[i],
			bt.y,
			bt.vertLines[i],
			bt.yText-bt.f2.ascent)
	}
	bt.page.MoveTo(bt.vertLines[0], bt.yText-bt.f2.ascent)
	bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-bt.f2.ascent)
	bt.page.StrokePath()
	bt.page.SetPenColorWithFloat32Array(original)
	bt.page.AddEMC()
}

func (bt *BigTable) getRowText(row []string) string {
	var buf strings.Builder
	for _, field := range row {
		buf.WriteString(field)
		buf.WriteString(" ")
	}
	return buf.String()
}

func (bt *BigTable) getAlignment(str string) int {
	var buf strings.Builder
	if strings.HasPrefix(str, "(") && strings.HasSuffix(str, ")") {
		str = str[1 : len(str)-1]
	}
	for i := 0; i < len(str); i++ {
		ch := str[i]
		if ch != '.' && ch != ',' && ch != '\'' {
			buf.WriteByte(ch)
		}
	}
	_, err := strconv.ParseFloat(buf.String(), 64)
	if err == nil {
		return align.Right
	}
	return align.Left
}

// SetTableData sets the table data from file
func (bt *BigTable) SetTableData(fileName, delimiter string) error {
	bt.fileName = fileName
	bt.delimiter = delimiter
	bt.vertLines = make([]float32, bt.numberOfColumns+1)
	bt.headerFields = make([]string, bt.numberOfColumns)
	bt.widths = make([]float32, bt.numberOfColumns)
	bt.alignment = make([]int, bt.numberOfColumns)

	file, err := os.Open(fileName)
	if err != nil {
		return err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	rowNumber := 0
	for scanner.Scan() {
		line := scanner.Text()
		fields := strings.Split(line, bt.delimiter)
		if len(fields) < bt.numberOfColumns {
			continue
		}
		if rowNumber == 0 {
			for i := 0; i < bt.numberOfColumns; i++ {
				bt.headerFields[i] = fields[i]
			}
		}
		if rowNumber == 1 {
			for i := 0; i < bt.numberOfColumns; i++ {
				bt.alignment[i] = bt.getAlignment(fields[i])
			}
		}
		for i := 0; i < bt.numberOfColumns; i++ {
			field := fields[i]
			width := bt.f1.StringWidth(nil, field) + 2*bt.padding
			if width > bt.widths[i] {
				bt.widths[i] = width
			}
		}
		rowNumber++
	}

	if err := scanner.Err(); err != nil {
		return err
	}

	bt.vertLines[0] = 0.0
	vertLineX := float32(0.0)
	for i := 0; i < len(bt.widths); i++ {
		vertLineX += bt.widths[i]
		bt.vertLines[i+1] = vertLineX
	}

	return nil
}

// Complete finishes the table and writes all data
func (bt *BigTable) Complete() error {
	file, err := os.Open(bt.fileName)
	if err != nil {
		return err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		fields := strings.Split(line, bt.delimiter)
		if err := bt.drawTextAndLine(fields, bt.f2); err != nil {
			return err
		}
	}

	if err := scanner.Err(); err != nil {
		return err
	}

	bt.drawTheVerticalLines()
	return nil
}
