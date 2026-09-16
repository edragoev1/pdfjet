//
// bigtable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

import (
	"bufio"
	"fmt"
	"math"
	"os"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
)

// BigTable represents a table for handling large amounts of data in PDF
type BigTable struct {
	pdf             *PDF
	f1              *Font
	f2              *Font
	pageSize        pagesize.PageSize
	x               float32
	y               float32
	yText           float32
	pages           []*Page
	page            *Page
	widths          []float32
	headerFields    []string
	alignment       []alignment.Alignment
	vertLines       []float32
	bottomMargin    float32
	padding         float32
	highlight       bool
	highlightColor  int32
	penColor        int32
	fileName        string
	delimiter       string
	numberOfColumns int
	startNewPage    bool
	dataRows        int // The rows under the header, counted by SetTableData
	pageCount       int // The pages they take, counted by Complete
	pageNumber      int // The page being drawn
	footerDrawn     bool
}

// NewBigTable creates a new BigTable instance
func NewBigTable(pdf *PDF, f1 *Font, f2 *Font, pageSize pagesize.PageSize) *BigTable {
	return &BigTable{
		pdf:            pdf,
		f1:             f1,
		f2:             f2,
		pageSize:       pageSize,
		pages:          make([]*Page, 0),
		bottomMargin:   20.0,
		padding:        2.0,
		highlight:      true,
		highlightColor: 0xF0F0F0,
		penColor:       0xB0B0B0,
		startNewPage:   true,
		alignment:      make([]alignment.Alignment, 0),
	}
}

// SetLocation sets the table location
func (bt *BigTable) SetLocation(x, y float32) *BigTable {
	bt.x = x
	bt.y = y
	if bt.vertLines != nil {
		bt.setVertLines()
	}
	return bt
}

// SetNumberOfColumns sets the number of columns
func (bt *BigTable) SetNumberOfColumns(numberOfColumns int) *BigTable {
	bt.numberOfColumns = numberOfColumns
	return bt
}

// SetTextAlignment sets text alignment for a column
func (bt *BigTable) SetTextAlignment(column int, alignment alignment.Alignment) *BigTable {
	bt.alignment[column] = alignment
	return bt
}

// SetBottomMargin sets the bottom margin
func (bt *BigTable) SetBottomMargin(bottomMargin float32) *BigTable {
	bt.bottomMargin = bottomMargin
	return bt
}

// GetPages returns the pages, which Complete has already added to the PDF.
func (bt *BigTable) GetPages() []*Page {
	return bt.pages
}

// newPage creates the next page. It is added to the PDF right away, so the
// content of the page before it is compressed and written, and its memory freed.
func (bt *BigTable) newPage() {
	bt.page = NewPage(bt.pdf, bt.pageSize)
	bt.pages = append(bt.pages, bt.page)
	bt.pageNumber++
	bt.footerDrawn = false
	bt.page.SetPenWidth(0.0)
	bt.yText = bt.y + bt.f1.ascent
	bt.highlight = true
	bt.drawFieldsAndLine(bt.headerFields, bt.f1)
	bt.yText += bt.f1.descent + bt.f2.ascent
	bt.startNewPage = false
}

// drawFooter draws the footer of the page that was just finished, once. The
// page is finished before the next one is created, so it is written complete.
func (bt *BigTable) drawFooter() {
	if !bt.footerDrawn {
		footer := NewTextLine(bt.f1, fmt.Sprintf("Page %d of %d", bt.pageNumber, bt.pageCount))
		bt.page.AddFooter(footer)
		bt.footerDrawn = true
	}
}

// countPages counts the pages the rows take, as drawTextAndLine breaks them, so
// that the "Page i of N" footer of a page can be drawn before the next page is
// created. The location and the bottom margin are set after SetTableData, so the
// pages are counted when the table is drawn.
func (bt *BigTable) countPages() int {
	pageHeight := bt.pageSize.GetHeight()
	yTop := bt.y + bt.f1.ascent + bt.f1.descent + bt.f2.ascent
	yPos := yTop
	count := 1
	newPage := false
	for i := 0; i < bt.dataRows; i++ {
		if newPage {
			count++
			yPos = yTop
			newPage = false
		}
		yPos += bt.f2.descent + bt.f2.ascent
		if yPos > (pageHeight - bt.bottomMargin) {
			newPage = true
		}
	}
	return count
}

func (bt *BigTable) drawTextAndLine(fields []string) error {
	if bt.page == nil { // First page
		bt.newPage()
		return nil
	}

	if bt.startNewPage { // New page
		bt.newPage()
	}

	bt.drawFieldsAndLine(fields, bt.f2)
	bt.yText += bt.f2.descent + bt.f2.ascent
	if bt.yText > (bt.page.height - bt.bottomMargin) {
		bt.drawTheVerticalLines()
		bt.drawFooter()
		bt.startNewPage = true
	}

	return nil
}

func (bt *BigTable) drawFieldsAndLine(fields []string, font *Font) {
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
	bt.page.SetPenColorRGB(original)
	bt.page.SetBrushColor(color.Black)

	for i := 0; i < bt.numberOfColumns; i++ {
		text := fields[i]
		xText := bt.vertLines[i] + bt.padding
		if bt.alignment[i] == alignment.Right {
			xText = (bt.vertLines[i+1] - bt.padding) - font.StringWidth(font.size, text)
		}
		bt.page.drawTextLine(font, text, xText, bt.yText)
	}
}

func (bt *BigTable) highlightRow(page *Page, font *Font, color int32) {
	original := page.GetBrushColor()
	page.SetBrushColor(color)
	page.MoveTo(bt.vertLines[0], bt.yText-font.ascent)
	page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent)
	page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText+font.descent)
	page.LineTo(bt.vertLines[0], bt.yText+font.descent)
	page.FillPath()
	page.SetBrushColorRGB(original)
}

func (bt *BigTable) drawTheVerticalLines() {
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
	bt.page.SetPenColorRGB(original)
}

// getAlignment right-aligns a number, as Table.RightAlignNumbers aligns it.
func (bt *BigTable) getAlignment(str string) alignment.Alignment {
	if isNumber(str) {
		return alignment.Right
	}
	return alignment.Left
}

// splitFields splits a line at the delimiter, reading the quoted fields as RFC
// 4180 does; with no delimiter the line is one field, as in the other ports.
func (bt *BigTable) splitFields(line string) []string {
	return splitDelimited(line, bt.delimiter)
}

// newDataScanner returns a scanner of the lines of the data file, which is
// read as UTF-8, after the byte order mark at its start, if there is one.
func newDataScanner(file *os.File) *bufio.Scanner {
	reader := bufio.NewReader(file)
	if bom, err := reader.Peek(3); err == nil && string(bom) == "\uFEFF" {
		_, _ = reader.Discard(3)
	}
	scanner := bufio.NewScanner(reader)
	scanner.Buffer(make([]byte, 0, 64*1024), math.MaxInt32)
	return scanner
}

// SetTableData sets the table data from the file, with the fields of each line
// separated by the delimiter. It returns the table, or an error if the file
// cannot be read.
func (bt *BigTable) SetTableData(fileName, delimiter string) (*BigTable, error) {
	bt.fileName = fileName
	bt.delimiter = delimiter
	bt.vertLines = make([]float32, bt.numberOfColumns+1)
	bt.headerFields = make([]string, bt.numberOfColumns)
	bt.widths = make([]float32, bt.numberOfColumns)
	bt.alignment = make([]alignment.Alignment, bt.numberOfColumns)

	file, err := os.Open(fileName)
	if err != nil {
		return nil, err
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("failed to close file: " + err.Error())
		}
	}(file)

	scanner := newDataScanner(file)
	rowNumber := 0
	for scanner.Scan() {
		line := scanner.Text()
		fields := bt.splitFields(line)
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
			width := bt.f1.StringWidth(bt.f1.size, field) + 2*bt.padding
			if width > bt.widths[i] {
				bt.widths[i] = width
			}
		}
		rowNumber++
	}

	if err := scanner.Err(); err != nil {
		return nil, err
	}
	bt.dataRows = 0
	if rowNumber > 0 {
		bt.dataRows = rowNumber - 1 // Without the header
	}

	bt.setVertLines()
	return bt, nil
}

// setVertLines sets the x coordinates of the vertical lines from the location
// and the column widths.
func (bt *BigTable) setVertLines() {
	vertLineX := bt.x
	bt.vertLines[0] = vertLineX
	for i := 0; i < len(bt.widths); i++ {
		vertLineX += bt.widths[i]
		bt.vertLines[i+1] = vertLineX
	}
}

// Complete draws the rows read from the data file, then the vertical lines,
// with a "Page i of N" footer on every page. The pages are added to the PDF as
// they are drawn, so the document does not hold them all.
func (bt *BigTable) Complete() error {
	bt.pageCount = bt.countPages()
	file, err := os.Open(bt.fileName)
	if err != nil {
		return err
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("failed to close file: " + err.Error())
		}
	}(file)

	scanner := newDataScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		fields := bt.splitFields(line)
		if len(fields) < bt.numberOfColumns {
			continue
		}
		if err := bt.drawTextAndLine(fields); err != nil {
			return err
		}
	}

	if err := scanner.Err(); err != nil {
		return err
	}

	bt.drawTheVerticalLines()
	bt.drawFooter()
	return nil
}
