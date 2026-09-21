//
// bigtable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

import (
	"bufio"
	"iter"
	"math"
	"os"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/internal/utf8text"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// BigTable is a table for large amounts of data, read row by row from a
// delimited text file or from an iterator. Each page is written as soon as it
// is full, so the memory stays flat however many rows there are. A PDF/UA
// document holds no more than that either: the table is tagged as a table,
// and the structure elements of a page are written with it. What is left is
// one cross-reference entry for each of them, which every object of a PDF
// has.
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
	shadingColor    [3]float32
	hasShading      bool // false for no shading
	borderColor     [3]float32
	hasBorder       bool // false for no lines
	footerText      string
	footerFont      *Font // nil for the header font
	rows            iter.Seq[[]string]
	checkLineBreaks bool  // The rows are not read from a file, which has no line breaks left
	readErr         error // The error reading the data file, if any
	columns         []int // The fields drawn, in the order they are drawn
	numberOfColumns int   // The length of columns
	fieldsNeeded    int   // The fields a row needs: the largest index plus 1
	startNewPage    bool
	dataRows        int // The rows under the header, counted by SetTableData
	pageCount       int // The pages they take, counted by Complete
	pageNumber      int // The page being drawn
	footerDrawn     bool
	// In a PDF/UA document the table is a Table element that the rows of
	// every page go on adding to; see drawFieldsAndLine.
	structElement *structElement
}

// NewBigTable creates a new BigTable instance
func NewBigTable(pdf *PDF, f1 *Font, f2 *Font, pageSize pagesize.PageSize) *BigTable {
	return &BigTable{
		pdf:          pdf,
		f1:           f1,
		f2:           f2,
		pageSize:     pageSize,
		pages:        make([]*Page, 0),
		bottomMargin: 20.0,
		padding:      2.0,
		highlight:    true,
		shadingColor: colorToRGB(0xF0F0F0),
		hasShading:   true,
		borderColor:  colorToRGB(0xB0B0B0),
		hasBorder:    true,
		footerText:   "Page {page} of {pages}",
		startNewPage: true,
		alignment:    make([]alignment.Alignment, 0),
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

// SetNumberOfColumns sets the number of columns in this table: the first fields
// of every row, in their order. It is the same as SetColumns(0, 1, ...
// numberOfColumns - 1).
func (bt *BigTable) SetNumberOfColumns(numberOfColumns int) *BigTable {
	columns := make([]int, max(numberOfColumns, 0))
	for i := range columns {
		columns[i] = i
	}
	return bt.SetColumns(columns...)
}

// SetColumns sets the fields of every row that the table draws, by their index
// from 0, in the order they are drawn, so SetColumns(3, 0, 11) draws the fourth
// field, then the first, then the twelfth. The indexes pick the header fields
// the same way. A row without a field for every index is skipped. Call it
// before SetTableData; SetTextAlignment counts the columns as they are drawn. A
// negative index is recorded on the PDF as misuse, and the columns are left as
// they were.
func (bt *BigTable) SetColumns(columns ...int) *BigTable {
	fieldsNeeded := 0
	for _, column := range columns {
		if column < 0 {
			bt.pdf.fail("A column index cannot be negative.")
			return bt
		}
		fieldsNeeded = max(fieldsNeeded, column+1)
	}
	bt.columns = append([]int(nil), columns...)
	bt.numberOfColumns = len(columns)
	bt.fieldsNeeded = fieldsNeeded
	return bt
}

// SetTextAlignment sets the text alignment of the column, which is one of the
// columns of the table. Call it after SetTableData, which makes the columns: a
// column that the table does not have is recorded on the PDF as misuse, and
// the alignment is left as it was.
func (bt *BigTable) SetTextAlignment(column int, alignment alignment.Alignment) *BigTable {
	if column < 0 || column >= len(bt.alignment) {
		bt.pdf.fail("The table has no column " + strconv.Itoa(column) +
			": set the alignment of a column after SetTableData.")
		return bt
	}
	bt.alignment[column] = alignment
	return bt
}

// SetShadingColor sets the color of every other row, starting with the header,
// as a 0xRRGGBB value. color.Transparent turns the shading off.
func (bt *BigTable) SetShadingColor(c int32) *BigTable {
	bt.hasShading = c != color.Transparent
	bt.shadingColor = colorToRGB(c)
	return bt
}

// SetShadingColorRGB sets the color of every other row, starting with the
// header, from its red, green and blue values from 0 to 1.
func (bt *BigTable) SetShadingColorRGB(rgb [3]float32) *BigTable {
	bt.shadingColor = rgb
	bt.hasShading = true
	return bt
}

// SetBorderColor sets the color of the lines between the rows and the columns
// and around the table, as a 0xRRGGBB value. color.Transparent leaves the lines
// out.
func (bt *BigTable) SetBorderColor(c int32) *BigTable {
	bt.hasBorder = c != color.Transparent
	bt.borderColor = colorToRGB(c)
	return bt
}

// SetBorderColorRGB sets the color of the lines between the rows and the
// columns and around the table from its red, green and blue values from 0 to 1.
func (bt *BigTable) SetBorderColorRGB(rgb [3]float32) *BigTable {
	bt.borderColor = rgb
	bt.hasBorder = true
	return bt
}

// SetPadding sets the space between the text of a column and the lines on its
// left and right. It is 2 points by default. A negative padding is recorded on
// the PDF as misuse, and the padding is left as it was.
func (bt *BigTable) SetPadding(padding float32) *BigTable {
	if padding < 0 {
		bt.pdf.fail("The padding cannot be negative.")
		return bt
	}
	bt.padding = padding
	if bt.vertLines != nil {
		bt.setVertLines()
	}
	return bt
}

// SetFooter sets the footer drawn at the bottom of every page, centered. In the
// text, {page} stands for the number of the page and {pages} for the number of
// pages. The footer is "Page {page} of {pages}" in the header font by default.
// An empty text leaves the footer out, and a nil font is the header font.
func (bt *BigTable) SetFooter(text string, font *Font) *BigTable {
	bt.footerText = text
	bt.footerFont = font
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
	// The header fields are the TH cells of the table the first time they are
	// drawn, and an artifact where they repeat on the next pages.
	header := structelem.StructElem("")
	if bt.structElement == nil {
		bt.structElement = bt.page.addStructElementOpen(
			bt.page.structParent, structelem.Table, "", true)
		header = structelem.TH
	}
	bt.drawFieldsAndLine(bt.headerFields, bt.f1, header)
	bt.yText += bt.f1.descent + bt.f2.ascent
	bt.startNewPage = false
}

// drawFooter draws the footer of the page that was just finished, once. The
// page is finished before the next one is created, so it is written complete.
func (bt *BigTable) drawFooter() {
	if !bt.footerDrawn && bt.footerText != "" {
		text := strings.ReplaceAll(bt.footerText, "{page}", strconv.Itoa(bt.pageNumber))
		text = strings.ReplaceAll(text, "{pages}", strconv.Itoa(bt.pageCount))
		font := bt.footerFont
		if font == nil {
			font = bt.f1
		}
		// The page number repeats on every page, which makes it an artifact.
		bt.page.AddArtifactBMC()
		bt.page.AddFooter(NewTextLine(font, text))
		bt.page.AddEMC()
	}
	bt.footerDrawn = true
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

func (bt *BigTable) drawTextAndLine(fields []string) {
	if bt.startNewPage { // New page
		bt.newPage()
	}

	bt.drawFieldsAndLine(fields, bt.f2, structelem.TD)
	bt.yText += bt.f2.descent + bt.f2.ascent
	if bt.yText > (bt.page.height - bt.bottomMargin) {
		bt.drawTheVerticalLines()
		bt.drawFooter()
		bt.startNewPage = true
	}
}

// drawFieldsAndLine draws a row of the table. In a PDF/UA document the row is
// a TR element and each field a TH or TD element that holds the text, and the
// shading and the lines are artifacts. A row with no cell structure is drawn
// as an artifact, which is what the header rows that repeat on the next pages
// are.
func (bt *BigTable) drawFieldsAndLine(
	fields []string, font *Font, cellStructure structelem.StructElem) {
	// The shading and the line above the text carry no meaning of their own.
	bt.page.AddArtifactBMC()
	if bt.highlight {
		if bt.hasShading {
			bt.highlightRow(bt.page, font, bt.shadingColor)
		}
		bt.highlight = false
	} else {
		bt.highlight = true
	}

	// The line above the text
	if bt.hasBorder {
		original := bt.page.GetPenColor()
		bt.page.SetPenColorRGB(bt.borderColor)
		bt.page.MoveTo(bt.vertLines[0], bt.yText-font.ascent)
		bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent)
		bt.page.StrokePath()
		bt.page.SetPenColorRGB(original)
	}
	bt.page.AddEMC()
	bt.page.SetBrushColor(color.Black)

	tagged := bt.structElement != nil && cellStructure != ""
	parent := bt.page.structParent
	var rowElement *structElement
	if tagged {
		rowElement = bt.page.addStructElement(bt.structElement, structelem.TR, "")
	}
	for i := 0; i < bt.numberOfColumns; i++ {
		text := fields[bt.columns[i]]
		if bt.checkLineBreaks && hasLineBreak(text) {
			text = lineBreaksToSpaces(text)
		}
		xText := bt.vertLines[i] + bt.padding
		if bt.alignment[i] == alignment.Right {
			xText = (bt.vertLines[i+1] - bt.padding) - font.StringWidth(font.size, text)
		}
		if tagged {
			// A cell holds its text and nothing else, so the cell element
			// holds the marked content of the text: it needs no paragraph of
			// its own, which would be another object for every cell.
			bt.page.structParent = rowElement
			bt.page.addBDC(cellStructure, "", "", text, bigTableCellAttributes(cellStructure))
		} else {
			bt.page.AddArtifactBMC()
		}
		bt.page.drawTextLine(font, text, xText, bt.yText)
		bt.page.AddEMC()
	}
	bt.page.structParent = parent
}

// bigTableCellAttributes returns the attributes of a cell element: a header
// cell heads the column it is in.
func bigTableCellAttributes(cellStructure structelem.StructElem) string {
	if cellStructure == structelem.TH {
		return "<</O /Table /Scope /Column>>"
	}
	return ""
}

func (bt *BigTable) highlightRow(page *Page, font *Font, rgb [3]float32) {
	original := page.GetBrushColor()
	page.SetBrushColorRGB(rgb)
	page.fillRectBetween(bt.vertLines[0], bt.yText-font.ascent,
		bt.vertLines[bt.numberOfColumns], bt.yText+font.descent)
	page.SetBrushColorRGB(original)
}

func (bt *BigTable) drawTheVerticalLines() {
	if !bt.hasBorder {
		return
	}
	// The lines of the table carry no meaning of their own.
	bt.page.AddArtifactBMC()
	defer bt.page.AddEMC()
	original := bt.page.GetPenColor()
	bt.page.SetPenColorRGB(bt.borderColor)
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

// scanDataFile calls yield with the fields of each line of the data file until
// it returns false. A line is split at the delimiter, reading the quoted fields
// as RFC 4180 does; with no delimiter the line is one field, as in the other
// ports.
func scanDataFile(fileName, delimiter string, yield func([]string) bool) error {
	file, err := os.Open(fileName)
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
	nextLine := func() (string, bool) {
		if scanner.Scan() {
			return utf8text.Decode(scanner.Bytes()), true
		}
		return "", false
	}
	for scanner.Scan() {
		if !yield(readDelimitedRecord(utf8text.Decode(scanner.Bytes()), delimiter, nextLine)) {
			return nil
		}
	}
	return scanner.Err()
}

// SetTableData sets the table data from the file, which is read as UTF-8. Its
// first line with a field for every column is the header, and the lines after
// it are the rows. A quoted field is read as RFC 4180 reads it, so a delimiter
// inside one is text, and a line break inside one goes on to the next line of
// the file and is drawn as a space. It returns the table, or an error if the
// file cannot be read.
func (bt *BigTable) SetTableData(fileName, delimiter string) (*BigTable, error) {
	fieldsNeeded := bt.fieldsNeeded
	header := []string{}
	err := scanDataFile(fileName, delimiter, func(fields []string) bool {
		if len(fields) >= fieldsNeeded {
			header = fields
			return false
		}
		return true
	})
	if err != nil {
		return nil, err
	}

	rows := func(yield func([]string) bool) {
		inHeader := true
		err := scanDataFile(fileName, delimiter, func(fields []string) bool {
			if inHeader {
				inHeader = len(fields) < fieldsNeeded
				return true
			}
			return yield(fields)
		})
		if err != nil {
			bt.readErr = err
		}
	}
	bt.setTableRows(header, rows, false)
	if bt.readErr != nil {
		return nil, bt.readErr
	}
	return bt, nil
}

// SetTableRows sets the table data from rows that are not in a file: the
// results of a query, or a slice of objects. The rows are iterated twice, once
// here to measure the columns and once by Complete to draw them, and neither
// keeps them, so an iterator that runs the query again, or maps the objects to
// fields as it goes, keeps the memory flat. A row without a field for every
// column is skipped, as a short line of a file is, and a line break in a field
// is drawn as a space. A header without a field for
// every column is recorded on the PDF as misuse, and the table is left without
// data.
func (bt *BigTable) SetTableRows(header []string, rows iter.Seq[[]string]) *BigTable {
	return bt.setTableRows(header, rows, true)
}

// setTableRows sets the table data. With checkLineBreaks, each field is looked
// at for line breaks to draw as spaces; the rows of a data file have them as
// spaces already.
func (bt *BigTable) setTableRows(header []string, rows iter.Seq[[]string], checkLineBreaks bool) *BigTable {
	bt.checkLineBreaks = checkLineBreaks
	if len(header) < bt.fieldsNeeded {
		bt.pdf.fail("The header does not have a field for every column.")
		return bt
	}
	bt.rows = rows
	bt.readErr = nil
	bt.vertLines = make([]float32, bt.numberOfColumns+1)
	bt.headerFields = append([]string(nil), header...)
	bt.widths = make([]float32, bt.numberOfColumns)
	bt.alignment = make([]alignment.Alignment, bt.numberOfColumns)

	bt.measure(header, bt.f1)
	rowNumber := 0
	for fields := range rows {
		if len(fields) < bt.fieldsNeeded {
			continue
		}
		if rowNumber == 0 { // Determine alignment from first data row
			for i := 0; i < bt.numberOfColumns; i++ {
				bt.alignment[i] = bt.getAlignment(fields[bt.columns[i]])
			}
		}
		bt.measure(fields, bt.f2)
		rowNumber++
	}
	bt.dataRows = rowNumber

	bt.setVertLines()
	return bt
}

// measure widens the columns to fit the fields of a row, measured in the font
// the row is drawn with: the header font for the header and the body font for
// a row under it. The widths are those of the text, and setVertLines adds the
// padding, so it can be set later.
func (bt *BigTable) measure(fields []string, font *Font) {
	for i := 0; i < bt.numberOfColumns; i++ {
		text := fields[bt.columns[i]]
		if bt.checkLineBreaks && hasLineBreak(text) {
			text = lineBreaksToSpaces(text)
		}
		width := font.StringWidth(font.size, text)
		if width > bt.widths[i] {
			bt.widths[i] = width
		}
	}
}

// setVertLines sets the x coordinates of the vertical lines from the location,
// the column widths and the padding.
func (bt *BigTable) setVertLines() {
	vertLineX := bt.x
	bt.vertLines[0] = vertLineX
	for i := 0; i < len(bt.widths); i++ {
		vertLineX += bt.widths[i] + 2*bt.padding
		bt.vertLines[i+1] = vertLineX
	}
}

// Complete draws the rows, then the vertical lines, with the footer on every
// page. The pages are added to the PDF as they are drawn, so the
// document does not hold them all. It returns an error if the data file cannot
// be read. A table without data draws nothing.
func (bt *BigTable) Complete() error {
	if bt.rows == nil {
		return nil
	}
	bt.pageCount = bt.countPages()
	bt.readErr = nil
	bt.newPage()
	for fields := range bt.rows {
		if len(fields) < bt.fieldsNeeded {
			continue
		}
		bt.drawTextAndLine(fields)
	}
	if bt.readErr != nil {
		return bt.readErr
	}

	bt.drawTheVerticalLines()
	bt.drawFooter()
	return nil
}
