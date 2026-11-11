//
// bigtable.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"bufio"
	"iter"
	"math"
	"os"
	"sort"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/internal/utf8text"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// BigTable is a table of plain rows of any length, a statement, a list or an
// export: every row has one style, and a field too wide for its column is cut
// short, or, with SetWrapText, wraps onto more lines, the row as tall as its
// tallest field. For drawables in cells, spanned cells or page totals, use
// Table. The rows are read row by row from a delimited text
// file or from an iterator. Each page is written as soon as it
// is full, so the memory stays flat however many rows there are. A PDF/UA
// document holds no more than that either: the table is tagged as a table,
// and the structure elements of a page are written with it. What is left is
// one cross-reference entry for each of them, which every object of a PDF
// has.
type BigTable struct {
	pdf          *PDF
	f1           *Font
	f2           *Font
	pageSize     pagesize.PageSize
	x            float32
	y            float32
	yText        float32
	top          float32 // The top of the table on the page being drawn
	firstPage    *Page   // The page the table starts on, or nil for a new one
	firstPageY   float32 // The top of the table on firstPage
	pages        []*Page
	page         *Page
	widths       []float32
	headerFields []string
	alignment    []alignment.Alignment
	vertLines    []float32
	// The width the text of each column has when the table is too wide for
	// the page and the last of its columns are cut back to fit it, or nil
	// when the table fits: a column that is as wide as its widest field
	// holds every field of it whole and needs no measuring as it is drawn.
	clipped         []float32
	bottomMargin    float32
	padding         float32
	verticalPadding float32 // Above and below the text of a row
	highlight       bool
	shadingColor    [3]float32
	hasShading      bool // false for no shading
	headerColor     [3]float32
	hasHeaderColor  bool // false for the shading of every other row
	borderColor     [3]float32
	noVerticalLines bool // true for the rows' lines alone
	hasBorder       bool // false for no lines
	footerText      string
	footerFont      *Font   // nil for the header font
	footerFontSize  float32 // 0 for the size of the footer's font
	footerOffset    float32 // Below 0 for the ascent of the footer's font
	rows            iter.Seq[[]string]
	checkLineBreaks bool      // The rows are not read from a file, which has no line breaks left
	readErr         error     // The error reading the data file, if any
	columns         []int     // The fields drawn, in the order they are drawn
	percents        []float32 // The width of each column, as a percent of the table's, or nil
	numberOfColumns int       // The length of columns
	fieldsNeeded    int       // The fields a row needs: the largest index plus 1
	startNewPage    bool
	dataRows        int // The rows under the header, counted by SetTableData
	pageCount       int // The pages they take, counted by Complete
	pageNumber      int // The page being drawn
	footerDrawn     bool
	// In a PDF/UA document the table is a Table element that the rows of
	// every page go on adding to; see drawFieldsAndLine.
	structElement *structElement
	// With SetWrapText: the fields wrap within their columns, textWidths the
	// width of the text of each column, headerLines the header's lines, and
	// firstRowOnPage whether no row is drawn yet on the page.
	wrap           bool
	textWidths     []float32
	headerLines    [][]string
	headerCount    int // The most lines of a header field
	firstRowOnPage bool
	// With SetFallbackFonts: the fonts of the characters the header's font
	// and the rows' font do not have, or nil
	fallback1 *Font
	fallback2 *Font
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
		footerOffset: -1,
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

// SetColumnWidthsInPercent gives the columns, in the order SetColumns draws
// them, their percentages of the width of the table, the width of the page
// less the margin of SetLocation on each side, as Table's
// SetColumnWidthsInPercent does; percentages that do not add up to 100 are
// shares of it. A field wider than its column is cut to fit it, its text
// ending in " ...". Without them each column is as wide as its widest field.
// Call it after SetColumns or SetNumberOfColumns: percentages that are not one
// for each column, or one that is negative or not a number, are recorded on
// the PDF as misuse, and the widths are left as they were.
func (bt *BigTable) SetColumnWidthsInPercent(percents ...float32) *BigTable {
	if len(percents) != bt.numberOfColumns {
		bt.pdf.fail("The table has " + strconv.Itoa(bt.numberOfColumns) + " columns, and " +
			strconv.Itoa(len(percents)) + " percentages: give one for each column.")
		return bt
	}
	for _, percent := range percents {
		if !(percent >= 0) || math.IsInf(float64(percent), 1) {
			bt.pdf.fail("The width of a column cannot be a negative percentage, or not a number.")
			return bt
		}
	}
	bt.percents = append([]float32(nil), percents...)
	if bt.vertLines != nil {
		bt.setVertLines()
	}
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

// SetWrapText sets whether a field too wide for its column wraps onto more
// lines, at its spaces, a word longer than its column broken between its
// characters, rather than being cut short; a row is then as tall as its
// tallest field. The columns keep the widths of SetColumnWidthsInPercent, or,
// without them, their widths, as wide as their widest fields, when the table
// fits its page; when it does not, the narrow columns keep theirs and the
// others share what is left equally. A row that does not fit what is left of
// a page starts the next; one taller than a whole page is cut at the lines
// that fit, ending in " ...", its cell keeping the whole of its text. The rows
// are read once more to count the pages, three times in all, and the memory
// stays flat. It is off unless set, and the table is drawn as before.
func (bt *BigTable) SetWrapText(wrap bool) *BigTable {
	bt.wrap = wrap
	if bt.vertLines != nil {
		bt.setVertLines()
	}
	return bt
}

// SetFallbackFonts sets the fonts that draw the characters the header's font
// and the rows' font do not have, such as Korean or Chinese in a table of a
// Latin font: each character in the font of its row if it has it, else in the
// fallback font, and measured so. A nil font is none, as unless set. Call it
// before SetTableData or SetTableRows, which measure the columns.
func (bt *BigTable) SetFallbackFonts(header, rows *Font) *BigTable {
	bt.fallback1 = header
	bt.fallback2 = rows
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

// SetVerticalLines sets whether the vertical lines between the columns and at
// the sides of the table are drawn. They are unless set; without them the table
// has only the lines between its rows, a lighter look for a long table.
func (bt *BigTable) SetVerticalLines(verticalLines bool) *BigTable {
	bt.noVerticalLines = !verticalLines
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

// SetVerticalPadding sets the space between the text of a row and the lines
// above and below it, the header row's too. It is 0 by default, and a row is as
// tall as its font; a table with it takes more pages. A negative padding, or one
// that is not a number, is recorded on the PDF as misuse, and the padding is left
// as it was.
func (bt *BigTable) SetVerticalPadding(verticalPadding float32) *BigTable {
	if !(verticalPadding >= 0) || math.IsInf(float64(verticalPadding), 0) {
		bt.pdf.fail("The vertical padding must be a number, 0 or more.")
		return bt
	}
	bt.verticalPadding = verticalPadding
	return bt
}

// SetHeaderColor sets the background color of the header row, on every page, as
// a 0xRRGGBB value. Unless it is set, the header is shaded as the first of every
// other row, in the shading color. color.Transparent goes back to that.
func (bt *BigTable) SetHeaderColor(c int32) *BigTable {
	bt.hasHeaderColor = c != color.Transparent
	bt.headerColor = colorToRGB(c)
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

// SetFooterFontSize sets the size of the footer's text, so that it can be
// smaller than the table's with the same font. It is the size of the footer's
// font unless it is set. A size of 0 or less, or one that is not a number, is
// recorded on the PDF as misuse, and the size is left as it was.
func (bt *BigTable) SetFooterFontSize(size float32) *BigTable {
	if !(size > 0) || math.IsInf(float64(size), 0) {
		bt.pdf.fail("The size of the footer must be a number more than 0.")
		return bt
	}
	bt.footerFontSize = size
	return bt
}

// SetFooterOffset sets the distance from the bottom of the page to the baseline
// of the footer, as Page.AddFooterOffsetBy takes it, so that the footer can sit at the
// bottom margin. It is the ascent of the footer's font unless it is set, the
// footer just above the edge of the page. A negative offset, or one that is not
// a number, is recorded on the PDF as misuse, and the offset is left as it was.
func (bt *BigTable) SetFooterOffset(offset float32) *BigTable {
	if !(offset >= 0) || math.IsInf(float64(offset), 0) {
		bt.pdf.fail("The offset of the footer must be a number, 0 or more.")
		return bt
	}
	bt.footerOffset = offset
	return bt
}

// SetBottomMargin sets the bottom margin
func (bt *BigTable) SetBottomMargin(bottomMargin float32) *BigTable {
	bt.bottomMargin = bottomMargin
	return bt
}

// SetFirstPage starts the table on a page of the PDF that has other content
// above it, such as a heading, at y on that page. The next pages are new, and
// the table starts on them at the y of its location. The page is one that
// NewPage has added to the PDF, and nothing is drawn on it after the table.
// When the header and the first row do not fit on the page above its bottom
// margin, the table starts on a new page instead, and draws nothing on it.
func (bt *BigTable) SetFirstPage(page *Page, y float32) *BigTable {
	// A page of another PDF is refused: the table was drawn on it, and its
	// mistakes recorded on the other PDF (review of 17 October 2026)
	if page != nil && page.pdf != bt.pdf {
		bt.pdf.fail("The page belongs to another PDF.")
		return bt
	}
	bt.firstPage = page
	bt.firstPageY = y
	return bt
}

// GetPages returns the pages, which Complete has already added to the PDF.
func (bt *BigTable) GetPages() []*Page {
	return bt.pages
}

// newPage creates the next page. It is added to the PDF right away, so the
// content of the page before it is compressed and written, and its memory freed.
func (bt *BigTable) newPage() {
	if bt.pageNumber == 0 && bt.startsOnFirstPage() {
		bt.page = bt.firstPage
		bt.top = bt.firstPageY
	} else {
		bt.page = NewPage(bt.pdf, bt.pageSize)
		bt.top = bt.y
	}
	bt.pages = append(bt.pages, bt.page)
	bt.pageNumber++
	bt.footerDrawn = false
	bt.page.SetPenWidth(0.0)
	bt.yText = bt.top + bt.verticalPadding + bt.f1.ascent
	bt.highlight = true
	// The header fields are the TH cells of the table the first time they are
	// drawn, and an artifact where they repeat on the next pages.
	header := structelem.StructElem("")
	if bt.structElement == nil {
		bt.structElement = bt.page.addStructElementOpen(
			bt.page.structParent, structelem.Table, "", true)
		header = structelem.TH
	}
	if bt.wrap {
		bt.drawWrappedFields(bt.headerLines, bt.headerCount, nil, bt.f1, header, true)
	} else {
		bt.drawFieldsAndLine(bt.headerFields, bt.f1, header, true)
	}
	if extra := bt.headerExtra(); extra > 0 {
		bt.yText += extra
	}
	bt.yText += bt.f1.descent + float32(2*bt.verticalPadding) + bt.f2.ascent
	bt.startNewPage = false
	bt.firstRowOnPage = true
}

// headerExtra returns how much taller than one line the header is: 0 unless
// its fields wrap onto more lines.
func (bt *BigTable) headerExtra() float32 {
	if !bt.wrap || bt.headerCount <= 1 {
		return 0
	}
	return float32(float32(bt.headerCount-1) * (bt.f1.ascent + bt.f1.descent))
}

// startsOnFirstPage returns true when the table starts on the page SetFirstPage
// set: when the header and the first row fit on it above its bottom margin.
func (bt *BigTable) startsOnFirstPage() bool {
	if bt.firstPage == nil {
		return false
	}
	need := bt.firstPageY + bt.f1.ascent + bt.f1.descent + bt.f2.ascent + bt.f2.descent +
		float32(4*bt.verticalPadding)
	if extra := bt.headerExtra(); extra > 0 {
		need += extra
	}
	return need <= bt.firstPage.height-bt.bottomMargin
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
		// The page number repeats on every page, which makes it an artifact:
		// AddFooter marks it as a pagination artifact, a footer.
		footer := NewTextLine(font, text)
		if bt.footerFontSize > 0 {
			footer.SetFontSize(bt.footerFontSize)
		}
		if bt.footerOffset >= 0 {
			bt.page.AddFooterOffsetBy(footer, bt.footerOffset)
		} else {
			bt.page.AddFooter(footer)
		}
	}
	bt.footerDrawn = true
}

// countPages counts the pages the rows take, as drawTextAndLine breaks them, so
// that the "Page i of N" footer of a page can be drawn before the next page is
// created. The location and the bottom margin are set after SetTableData, so the
// pages are counted when the table is drawn.
func (bt *BigTable) countPages() int {
	if bt.wrap {
		return bt.countWrappedPages()
	}
	pageHeight := bt.pageSize.GetHeight()
	yTop := bt.firstRowY(bt.y)
	yPos := yTop
	if bt.startsOnFirstPage() {
		// The first page can be of another size than the next ones.
		pageHeight = bt.firstPage.height
		yPos = bt.firstRowY(bt.firstPageY)
	}
	count := 1
	newPage := false
	for i := 0; i < bt.dataRows; i++ {
		if newPage {
			count++
			pageHeight = bt.pageSize.GetHeight()
			yPos = yTop
			newPage = false
		}
		yPos += bt.f2.descent + float32(2*bt.verticalPadding) + bt.f2.ascent
		if yPos > (pageHeight - bt.bottomMargin) {
			newPage = true
		}
	}
	return count
}

// firstRowY returns the baseline of the first row of a page, of a table whose
// top is there, in the order newPage adds it up: added up otherwise, it can
// round to another float32, and the count was a page off the drawing.
func (bt *BigTable) firstRowY(top float32) float32 {
	y := top + bt.verticalPadding + bt.f1.ascent
	if extra := bt.headerExtra(); extra > 0 {
		y += extra
	}
	y += bt.f1.descent + float32(2*bt.verticalPadding) + bt.f2.ascent
	return y
}

func (bt *BigTable) drawTextAndLine(fields []string) {
	if bt.wrap {
		bt.drawWrappedRow(fields)
		return
	}
	if bt.startNewPage { // New page
		bt.newPage()
	}

	bt.drawFieldsAndLine(fields, bt.f2, structelem.TD, false)
	bt.yText += bt.f2.descent + float32(2*bt.verticalPadding) + bt.f2.ascent
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
	fields []string, font *Font, cellStructure structelem.StructElem, header bool) {
	// The shading and the line above the text carry no meaning of their own.
	bt.page.AddArtifactBMC()
	if header && bt.hasHeaderColor {
		bt.highlightRow(bt.page, font, bt.headerColor)
		bt.highlight = false
	} else if bt.highlight {
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
		bt.page.MoveTo(bt.vertLines[0], bt.yText-font.ascent-bt.verticalPadding)
		bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent-bt.verticalPadding)
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
		// A column the page was too narrow for holds as much of the text as
		// fits it, ending in " ..."; the cell of the structure tree keeps the
		// whole of the text, which is what a reader reads. A column that was
		// not cut is as wide as its widest field, so every field of it is
		// drawn whole and none is measured here.
		drawn := text
		if bt.clipped != nil && bt.clipped[i] < bt.widths[i] {
			drawn = bigTableFit(text, bt.measureOf(font), bt.clipped[i])
		}
		xText := bt.vertLines[i] + bt.padding
		if bt.alignment[i] == alignment.Right {
			xText = (bt.vertLines[i+1] - bt.padding) - bt.measureOf(font).width(drawn)
		}
		if tagged {
			// A cell holds its text and nothing else, so the cell element
			// holds the marked content of the text: it needs no paragraph of
			// its own, which would be another object for every cell.
			bt.page.structParent = rowElement
			// A text cut short to fit its column is described by the whole
			// of it; a text drawn whole needs no description, which PAC
			// warns of on text
			description := ""
			if drawn != text {
				description = text
			}
			bt.page.addBDC(cellStructure, "", "", description, bigTableCellAttributes(cellStructure))
		} else {
			bt.page.AddArtifactBMC()
		}
		bt.drawText(font, drawn, xText, bt.yText)
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
	page.fillRectBetween(bt.vertLines[0], bt.yText-font.ascent-bt.verticalPadding,
		bt.vertLines[bt.numberOfColumns], bt.yText+font.descent+bt.verticalPadding)
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
	for i := 0; !bt.noVerticalLines && i <= bt.numberOfColumns; i++ {
		bt.page.DrawLine(
			bt.vertLines[i],
			bt.top,
			bt.vertLines[i],
			bt.yText-bt.f2.ascent-bt.verticalPadding)
	}
	bt.page.MoveTo(bt.vertLines[0], bt.yText-bt.f2.ascent-bt.verticalPadding)
	bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-bt.f2.ascent-bt.verticalPadding)
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
// read as UTF-8, after the byte order mark at its start, if there is one. A
// line ends at a line feed, a carriage return or the two together.
func newDataScanner(file *os.File) *bufio.Scanner {
	reader := bufio.NewReader(file)
	if bom, err := reader.Peek(3); err == nil && string(bom) == "\uFEFF" {
		_, _ = reader.Discard(3)
	}
	scanner := bufio.NewScanner(reader)
	scanner.Buffer(make([]byte, 0, 64*1024), math.MaxInt32)
	scanner.Split(scanDataLines)
	return scanner
}

// scanDataFile calls yield with the fields of each line of the data file until
// it returns false. A line is split at the delimiter, reading the quoted fields
// as RFC 4180 does; with no delimiter the line is one field, as in the other
// ports. A file that cannot be read that way returns the error that says why.
func scanDataFile(fileName, delimiter string, yield func([]string) bool) (err error) {
	defer func() {
		if r := recover(); r != nil {
			if e, ok := r.(delimitedError); ok {
				err = e
				return
			}
			panic(r)
		}
	}()
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
		width := bt.measureOf(font).width(text)
		if width > bt.widths[i] {
			bt.widths[i] = width
		}
	}
}

// bigTableEllipsis is the mark that ends a field whose text was cut to fit its
// column, which takes the place of the last four characters of what fits.
const bigTableEllipsis = " ..."

// setVertLines puts each vertical line where the columns before it end, and
// cuts the last of the columns back when they run past the right edge of the
// page: a column as wide as its widest field is what the table asks for, and a
// table wider than its page draws the last of its columns off the edge, where
// they are lost. The columns are cut from the last one back, so the table
// keeps the widths it asked for as far as the page allows, and the fields of a
// column that was cut are drawn with the end of their text replaced by " ...".
func (bt *BigTable) setVertLines() {
	if bt.percents != nil && len(bt.percents) != len(bt.widths) {
		// The columns were set again after the percentages: fewer panicked,
		// and more were drawn past the page (review of 12 October 2026, C4)
		bt.pdf.fail("The table has " + strconv.Itoa(len(bt.widths)) + " columns, and " +
			strconv.Itoa(len(bt.percents)) + " percentages: give one for each column.")
		bt.percents = nil
	}
	if bt.wrap {
		bt.setWrapWidths()
		return
	}
	if bt.percents != nil {
		bt.setVertLinesInPercent()
		return
	}
	widths := make([]float32, len(bt.widths))
	copy(widths, bt.widths)
	// The table is drawn between the margin it starts at and the same margin
	// on the right of the page.
	room := bt.pageSize.GetWidth() - float32(2*bt.x) - float32(float32(float32(len(widths))*2)*bt.padding)
	least := bt.f1.StringWidth(bt.f1.size, bigTableEllipsis)
	if width := bt.f2.StringWidth(bt.f2.size, bigTableEllipsis); width > least {
		least = width
	}
	var total float32 = 0.0
	for _, width := range widths {
		total += width
	}
	bt.clipped = nil
	for i := len(widths) - 1; i >= 0 && total > room; i-- {
		cut := total - room
		if room := widths[i] - least; cut > room {
			cut = room
		}
		if cut > 0.0 {
			widths[i] -= cut
			total -= cut
			if bt.clipped == nil {
				bt.clipped = make([]float32, len(bt.widths))
				copy(bt.clipped, bt.widths)
			}
			bt.clipped[i] = widths[i]
		}
	}
	vertLineX := bt.x
	bt.vertLines[0] = vertLineX
	for i := 0; i < len(widths); i++ {
		vertLineX += widths[i] + float32(2*bt.padding)
		bt.vertLines[i+1] = vertLineX
	}
}

// setVertLinesInPercent puts each vertical line where the columns before it
// end, each column the percentage of the table's width SetColumnWidthsInPercent
// gives it, its padding in it, and cuts the fields of a column narrower than
// its widest field.
func (bt *BigTable) setVertLinesInPercent() {
	tableWidth := bt.pageSize.GetWidth() - float32(2*bt.x)
	if !(tableWidth > 0) {
		// The columns were as wide as less than nothing (review of
		// 12 October 2026, T5)
		bt.pdf.fail("The margin of the table, its x, is half the width of the page or more: " +
			"the columns have no room.")
		tableWidth = 0
	}
	// The shares are added up and taken in float64, the largest float32 a
	// share as any other: tableWidth*percent was an infinity, and Complete
	// failed (review of 12 October 2026, T5)
	var total float64
	for _, percent := range bt.percents {
		total += float64(percent)
	}
	bt.clipped = nil
	vertLineX := bt.x
	bt.vertLines[0] = vertLineX
	for i, percent := range bt.percents {
		// Percentages that are all 0 share the width equally.
		width := tableWidth / float32(len(bt.percents))
		if total > 0 {
			width = float32(float64(tableWidth) * float64(percent) / total)
		}
		if text := max(0, width-float32(2*bt.padding)); bt.widths[i] > text {
			if bt.clipped == nil {
				bt.clipped = make([]float32, len(bt.widths))
				copy(bt.clipped, bt.widths)
			}
			bt.clipped[i] = text
		}
		vertLineX += width
		bt.vertLines[i+1] = vertLineX
	}
}

// bigTableFit returns the text as much of it as fits the width, ending in the
// mark that says it was cut. The mark takes the place of the last four
// characters of what fits, rather than being added to them, so the text stays
// inside the column. The text is cut between runes, never inside one.
func bigTableFit(text string, m bigTableMeasure, width float32) string {
	if m.width(text) <= width {
		return text
	}
	return bigTableCut(text, m, width)
}

// bigTableCut returns as much of the text as fits the width with the mark
// that says it was cut, the mark in place of the last four characters of what
// fits; or "" when the width is too narrow for the mark.
func bigTableCut(text string, m bigTableMeasure, width float32) string {
	runes := []rune(text)
	end := 0 // The most runes that fit.
	high := len(runes)
	for end < high {
		middle := end + (high-end+1)/2
		if m.width(string(runes[:middle])) <= width {
			end = middle
		} else {
			high = middle - 1
		}
	}
	end -= len([]rune(bigTableEllipsis))
	if end < 0 {
		end = 0
	}
	for end > 0 && m.width(string(runes[:end])+bigTableEllipsis) > width {
		end--
	}
	if m.font.StringWidth(m.font.size, bigTableEllipsis) > width {
		// A column too narrow for the mark draws nothing: the mark was drawn
		// over the next column (review of 12 October 2026, T5)
		return ""
	}
	return string(runes[:end]) + bigTableEllipsis
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

// The wrapping of the fields, with SetWrapText

// setWrapWidths gives each column the width of its text, and puts each
// vertical line where the columns before it end: the widths of
// SetColumnWidthsInPercent, or each column as wide as its widest field when
// they fit the page; when they do not, the columns narrower than an equal
// share of what is left keep their widths, from the narrowest, and the others
// share the rest equally. No field is cut: each wraps within its column.
func (bt *BigTable) setWrapWidths() {
	n := len(bt.widths)
	bt.clipped = nil
	bt.textWidths = make([]float32, n)
	vertLineX := bt.x
	bt.vertLines[0] = vertLineX
	if bt.percents != nil {
		tableWidth := bt.pageSize.GetWidth() - float32(2*bt.x)
		if !(tableWidth > 0) {
			bt.pdf.fail("The margin of the table, its x, is half the width of the page or more: " +
				"the columns have no room.")
			tableWidth = 0
		}
		var total float64
		for _, percent := range bt.percents {
			total += float64(percent)
		}
		for i, percent := range bt.percents {
			width := tableWidth / float32(len(bt.percents))
			if total > 0 {
				width = float32(float64(tableWidth) * float64(percent) / total)
			}
			bt.textWidths[i] = max(0, width-float32(2*bt.padding))
			vertLineX += width
			bt.vertLines[i+1] = vertLineX
		}
	} else {
		copy(bt.textWidths, bt.widths)
		room := bt.pageSize.GetWidth() - float32(2*bt.x) - float32(float32(float32(n)*2)*bt.padding)
		var total float32 = 0.0
		for _, width := range bt.widths {
			total += width
		}
		if total > room {
			// From the narrowest column, in the order of the columns among
			// those as wide
			order := make([]int, n)
			for i := range order {
				order[i] = i
			}
			sort.SliceStable(order, func(a, b int) bool { return bt.widths[order[a]] < bt.widths[order[b]] })
			remaining := room
			left := n
			share := float32(-1)
			for _, i := range order {
				if share < 0 {
					fair := max(0, remaining/float32(left))
					if bt.widths[i] <= fair {
						remaining -= bt.widths[i]
						left--
						continue
					}
					share = fair
				}
				bt.textWidths[i] = share
			}
		}
		for i := 0; i < n; i++ {
			vertLineX += bt.textWidths[i] + float32(2*bt.padding)
			bt.vertLines[i+1] = vertLineX
		}
	}
	bt.headerLines, bt.headerCount = bt.wrapFields(bt.headerFields, bt.f1)
}

// wrapFields returns the lines of each field of the row drawn, wrapped to the
// width of its column in the font, and the most lines of any of them.
func (bt *BigTable) wrapFields(fields []string, font *Font) ([][]string, int) {
	lines := make([][]string, bt.numberOfColumns)
	count := 1
	for i := 0; i < bt.numberOfColumns; i++ {
		text := fields[bt.columns[i]]
		if bt.checkLineBreaks && hasLineBreak(text) {
			text = lineBreaksToSpaces(text)
		}
		lines[i] = bigTableWrap(text, bt.measureOf(font), bt.textWidths[i])
		count = max(count, len(lines[i]))
	}
	return lines, count
}

// bigTableWrap returns the lines of the text within the width: its words, the
// parts between its spaces, as many on a line as fit, a word wider than the
// width broken between its characters, at least one on a line. An empty text,
// or one of spaces, is one empty line.
func bigTableWrap(text string, m bigTableMeasure, width float32) []string {
	lines := make([]string, 0, 1)
	line := ""
	for _, word := range strings.Split(text, " ") {
		if word == "" {
			continue
		}
		if line != "" {
			if candidate := line + " " + word; m.width(candidate) <= width {
				line = candidate
				continue
			}
			lines = append(lines, line)
			line = ""
		}
		if m.width(word) <= width {
			line = word
			continue
		}
		runes := []rune(word)
		for len(runes) > 0 {
			end := bigTableRunesThatFit(runes, m, width)
			if end == len(runes) {
				line = string(runes)
				break
			}
			lines = append(lines, string(runes[:end]))
			runes = runes[end:]
		}
	}
	return append(lines, line)
}

// bigTableRunesThatFit returns how many of the runes, from the first, fit the
// width: at least one, so that a column too narrow for a character still
// draws the text, a character on each line.
func bigTableRunesThatFit(runes []rune, m bigTableMeasure, width float32) int {
	end := 1
	high := len(runes)
	for end < high {
		middle := end + (high-end+1)/2
		if m.width(string(runes[:middle])) <= width {
			end = middle
		} else {
			high = middle - 1
		}
	}
	return end
}

// bigTableMostLines returns the most lines of a row whose first line is at y
// that the page holds above the limit: at least one.
func bigTableMostLines(y, limit, lineHeight float32) int {
	most := 1
	for y+float32(float32(most)*lineHeight) <= limit {
		most++
	}
	return most
}

// drawWrappedRow draws a row of fields that wrap: on the next page when it
// does not fit what is left of this one, cut at the lines a whole page holds
// when it does not fit that either.
func (bt *BigTable) drawWrappedRow(fields []string) {
	if bt.startNewPage {
		bt.newPage()
	}
	font := bt.f2
	lineHeight := font.ascent + font.descent
	lines, count := bt.wrapFields(fields, font)
	limit := bt.page.height - bt.bottomMargin
	if count > 1 && bt.yText+float32(float32(count-1)*lineHeight) > limit && !bt.firstRowOnPage {
		bt.drawTheVerticalLines()
		bt.drawFooter()
		bt.newPage()
		limit = bt.page.height - bt.bottomMargin
	}
	var whole []string
	if count > 1 && bt.yText+float32(float32(count-1)*lineHeight) > limit {
		most := bigTableMostLines(bt.yText, limit, lineHeight)
		if most < count {
			whole = make([]string, bt.numberOfColumns)
			for i := range lines {
				if len(lines[i]) > most {
					text := fields[bt.columns[i]]
					if bt.checkLineBreaks && hasLineBreak(text) {
						text = lineBreaksToSpaces(text)
					}
					whole[i] = text
					lines[i] = lines[i][:most]
					lines[i][most-1] = bigTableMark(lines[i][most-1], bt.measureOf(font), bt.textWidths[i])
				}
			}
			count = most
		}
	}
	bt.drawWrappedFields(lines, count, whole, font, structelem.TD, false)
	bt.firstRowOnPage = false
	if count > 1 {
		bt.yText += float32(float32(count-1) * lineHeight)
	}
	bt.yText += font.descent + float32(2*bt.verticalPadding) + font.ascent
	if bt.yText > limit {
		bt.drawTheVerticalLines()
		bt.drawFooter()
		bt.startNewPage = true
	}
}

// bigTableMark returns the line with the mark that says the text goes on,
// cut to make room for it within the width.
func bigTableMark(line string, m bigTableMeasure, width float32) string {
	if marked := line + bigTableEllipsis; m.width(marked) <= width {
		return marked
	}
	return bigTableCut(line, m, width)
}

// drawWrappedFields draws a row of the lines of its fields, count lines tall,
// as drawFieldsAndLine draws a row of one line: the cell of a field holds all
// of its lines, and the whole of the text of a field that was cut, whole[i]
// when there is one.
func (bt *BigTable) drawWrappedFields(
	lines [][]string, count int, whole []string, font *Font, cellStructure structelem.StructElem, header bool) {
	lineHeight := font.ascent + font.descent
	var extra float32 = 0.0
	if count > 1 {
		extra = float32(float32(count-1) * lineHeight)
	}
	bt.page.AddArtifactBMC()
	if header && bt.hasHeaderColor {
		bt.highlightRows(bt.page, font, bt.headerColor, extra)
		bt.highlight = false
	} else if bt.highlight {
		if bt.hasShading {
			bt.highlightRows(bt.page, font, bt.shadingColor, extra)
		}
		bt.highlight = false
	} else {
		bt.highlight = true
	}
	if bt.hasBorder {
		original := bt.page.GetPenColor()
		bt.page.SetPenColorRGB(bt.borderColor)
		bt.page.MoveTo(bt.vertLines[0], bt.yText-font.ascent-bt.verticalPadding)
		bt.page.LineTo(bt.vertLines[bt.numberOfColumns], bt.yText-font.ascent-bt.verticalPadding)
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
		if tagged {
			bt.page.structParent = rowElement
			description := ""
			if whole != nil {
				description = whole[i]
			}
			bt.page.addBDC(cellStructure, "", "", description, bigTableCellAttributes(cellStructure))
		} else {
			bt.page.AddArtifactBMC()
		}
		for k, line := range lines[i] {
			xText := bt.vertLines[i] + bt.padding
			if bt.alignment[i] == alignment.Right {
				xText = (bt.vertLines[i+1] - bt.padding) - bt.measureOf(font).width(line)
			}
			yText := bt.yText
			if k > 0 {
				yText += float32(float32(k) * lineHeight)
			}
			bt.drawText(font, line, xText, yText)
		}
		bt.page.AddEMC()
	}
	bt.page.structParent = parent
}

// highlightRows fills the background of a row, extra taller than one line.
func (bt *BigTable) highlightRows(page *Page, font *Font, rgb [3]float32, extra float32) {
	original := page.GetBrushColor()
	page.SetBrushColorRGB(rgb)
	bottom := bt.yText + font.descent + bt.verticalPadding
	if extra > 0 {
		bottom = bt.yText + extra + font.descent + bt.verticalPadding
	}
	page.fillRectBetween(bt.vertLines[0], bt.yText-font.ascent-bt.verticalPadding,
		bt.vertLines[bt.numberOfColumns], bottom)
	page.SetBrushColorRGB(original)
}

// countWrappedPages counts the pages the rows take as drawWrappedRow breaks
// them, reading the rows once more, so that the footer of each page says how
// many there are. The arithmetic is that of the drawing, in its order.
func (bt *BigTable) countWrappedPages() int {
	font := bt.f2
	lineHeight := font.ascent + font.descent
	pageHeight := bt.pageSize.GetHeight()
	yTop := bt.firstRowY(bt.y)
	yPos := yTop
	if bt.startsOnFirstPage() {
		pageHeight = bt.firstPage.height
		yPos = bt.firstRowY(bt.firstPageY)
	}
	count := 1
	newPage := false
	first := true
	for fields := range bt.rows {
		if len(fields) < bt.fieldsNeeded {
			continue
		}
		if newPage {
			count++
			pageHeight = bt.pageSize.GetHeight()
			yPos = yTop
			newPage = false
			first = true
		}
		_, lines := bt.wrapFields(fields, font)
		limit := pageHeight - bt.bottomMargin
		if lines > 1 && yPos+float32(float32(lines-1)*lineHeight) > limit && !first {
			count++
			pageHeight = bt.pageSize.GetHeight()
			yPos = yTop
			limit = pageHeight - bt.bottomMargin
		}
		if lines > 1 && yPos+float32(float32(lines-1)*lineHeight) > limit {
			lines = min(lines, bigTableMostLines(yPos, limit, lineHeight))
		}
		if lines > 1 {
			yPos += float32(float32(lines-1) * lineHeight)
		}
		yPos += font.descent + float32(2*bt.verticalPadding) + font.ascent
		first = false
		if yPos > limit {
			newPage = true
		}
	}
	return count
}

// bigTableMeasure measures text in a font, and in its fallback font, if it
// has one, the characters the font does not have.
type bigTableMeasure struct {
	font     *Font
	fallback *Font
}

func (m bigTableMeasure) width(text string) float32 {
	if m.fallback == nil {
		return m.font.StringWidth(m.font.size, text)
	}
	return m.font.StringWidthUsingFallbackFont(m.fallback, m.font.size, text)
}

// measureOf returns the measure of the font: the rows' with their fallback
// font, the header's with its own.
func (bt *BigTable) measureOf(font *Font) bigTableMeasure {
	if font == bt.f2 {
		return bigTableMeasure{font, bt.fallback2}
	}
	return bigTableMeasure{font, bt.fallback1}
}

// drawText draws the text at its baseline, in the font and its fallback
// font, if it has one.
func (bt *BigTable) drawText(font *Font, text string, x, y float32) {
	fallback := bt.measureOf(font).fallback
	if fallback == nil {
		bt.page.drawTextLine(font, text, x, y)
		return
	}
	bt.page.DrawString(font, fallback, font.size, text, x, y)
}
