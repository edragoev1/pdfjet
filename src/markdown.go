// markdown.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"unicode/utf16"

	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The heading sizes, as factors of the size of the text.
var markdownHeadingSizes = [6]float32{2.0, 1.6, 1.3, 1.15, 1.0, 0.9}

const (
	markdownCodeBackground        = 0xF3F5F8
	markdownRuleColor             = 0xC8CCD2
	markdownQuoteBarColor         = 0xB0B6BF
	markdownTableHeaderBackground = 0xEEF0F3
)

// Markdown draws a Markdown text on as many pages as it needs: headings of #
// and of underlines, paragraphs with the inline markup of Markup, bullet and
// numbered lists that nest, block quotes, fenced and indented code, thematic
// breaks, the tables of GitHub's Markdown, and images alone in their
// paragraph. In a PDF/UA document the headings are H1 to H6, with no level
// skipped, the lists are lists, the quotes BlockQuotes, the code Code, the
// tables tables and the images figures, with their text as the alternate
// description.
//
// It is not all of Markdown: HTML is drawn as the text it is, a line break is
// a space, links in a table cell are its text, and an image is drawn only when
// it is alone in its paragraph, from the directory that SetImageDirectory
// names; with none, its text is drawn instead, so that a text from anyone
// reads no file. Please see Example_54.
type Markdown struct {
	regular        *Font
	bold           *Font
	italic         *Font
	boldItalic     *Font
	code           *Font
	markup         *Markup
	headingFont    *Font
	marginLeft     float32
	marginTop      float32
	marginRight    float32
	marginBottom   float32
	imageDirectory string
	hasImages      bool // Whether SetImageDirectory set the directory

	// The page being drawn, where the next block goes, and the bottom of the
	// text on a page.
	pdf          *PDF
	pages        *[]*Page
	pageSize     pagesize.PageSize
	page         *Page
	y            float32
	bottom       float32
	atTop        bool // Nothing is drawn on the page yet
	headingLevel int  // Of the last heading, as it is tagged
	// The structure elements that the blocks drawn now are the kids of: a
	// list, its item and the item's body, or a quote. They are open, so that
	// what the next pages draw goes on adding to them. The last is the
	// innermost.
	containers []*structElement
	// The quote bars that are drawn on the page, as [x, the top of the bar].
	quoteBars []*[2]float32
}

// NewMarkdown creates the Markdown of text in the fonts, each at its size: the
// size of the regular font is the size of the text, which the headings are
// drawn larger than.
//   - regular: the font of the text.
//   - bold: the font of **bold** text, and of the headings unless SetHeadingFont sets another.
//   - italic: the font of *italic* text.
//   - boldItalic: the font of ***bold italic*** text.
//   - code: the font of code, usually a monospaced font.
func NewMarkdown(regular, bold, italic, boldItalic, code *Font) *Markdown {
	return &Markdown{
		regular:      regular,
		bold:         bold,
		italic:       italic,
		boldItalic:   boldItalic,
		code:         code,
		markup:       NewMarkup(regular, bold, italic, boldItalic, code),
		headingFont:  bold,
		marginLeft:   72,
		marginTop:    72,
		marginRight:  72,
		marginBottom: 72,
	}
}

// SetHeadingFont sets the font of the headings, which is the bold font unless
// it is set.
func (markdown *Markdown) SetHeadingFont(font *Font) *Markdown {
	markdown.headingFont = font
	return markdown
}

// SetMargins sets the margins of the pages, in points. They are 72 points, an
// inch, unless they are set.
func (markdown *Markdown) SetMargins(left, top, right, bottom float32) *Markdown {
	markdown.marginLeft = left
	markdown.marginTop = top
	markdown.marginRight = right
	markdown.marginBottom = bottom
	return markdown
}

// SetImageDirectory sets the directory that the images are read from: an
// image of ![text](source) is the file of that name in the directory, a JPEG,
// PNG, BMP or SVG file. A source that is an absolute path, a URL or has .. in
// it is not read, and neither is any image unless the directory is set: the
// image's text is drawn instead.
func (markdown *Markdown) SetImageDirectory(directory string) *Markdown {
	markdown.imageDirectory = directory
	markdown.hasImages = true
	return markdown
}

// DrawOnPages draws the text on as many new pages as it needs. The pages are
// created detached and added to the list, so that a footer or a page number
// can be drawn on each before they are added to the PDF. A text with no
// blocks needs no page. It returns the x and y coordinates of the bottom
// right corner of the text on the last page.
//   - pdf: the PDF document.
//   - text: the Markdown text.
//   - pages: the list that receives the new pages.
//   - pageSize: the page size, for example letter.Portrait().
func (markdown *Markdown) DrawOnPages(pdf *PDF, text string, pages *[]*Page, pageSize pagesize.PageSize) [2]float32 {
	markdown.pdf = pdf
	markdown.pages = pages
	markdown.pageSize = pageSize
	markdown.page = nil
	markdown.headingLevel = 0
	markdown.containers = markdown.containers[:0]
	markdown.quoteBars = markdown.quoteBars[:0]
	blocks := markdownParser{}.parse(text)
	width := pageSize.GetWidth() - markdown.marginLeft - markdown.marginRight
	if len(blocks) > 0 {
		markdown.newPage()
		markdown.drawBlocks(blocks, markdown.marginLeft, width, false)
		markdown.finishQuoteBars()
	}
	if markdown.page == nil {
		return [2]float32{markdown.marginLeft + width, markdown.marginTop}
	}
	return [2]float32{markdown.marginLeft + width, markdown.y}
}

// --- The flow of the blocks down the pages -----------------------------

func (markdown *Markdown) newPage() {
	if markdown.page != nil {
		markdown.finishQuoteBars()
	}
	markdown.page = NewPageDetached(markdown.pdf, markdown.pageSize)
	*markdown.pages = append(*markdown.pages, markdown.page)
	markdown.y = markdown.marginTop
	markdown.bottom = markdown.pageSize.GetHeight() - markdown.marginBottom
	markdown.atTop = true
	markdown.page.structParent = markdown.peekContainer()
	for _, bar := range markdown.quoteBars {
		bar[1] = markdown.y
	}
}

// peekContainer returns the innermost container, or nil.
func (markdown *Markdown) peekContainer() *structElement {
	if len(markdown.containers) == 0 {
		return nil
	}
	return markdown.containers[len(markdown.containers)-1]
}

// ensure starts a new page unless the height fits under what the page has.
func (markdown *Markdown) ensure(height float32) {
	if !markdown.atTop && markdown.y+height > markdown.bottom {
		markdown.newPage()
	}
}

// gap adds the space before a block, which the top of a page does not have.
func (markdown *Markdown) gap(space float32) {
	if !markdown.atTop {
		markdown.y += space
	}
}

func (markdown *Markdown) size() float32 {
	return markdown.regular.GetSize()
}

func (markdown *Markdown) openContainer(structure structelem.StructElem) {
	page := markdown.page
	element := page.addStructElementOpen(page.structParent, structure, "", true)
	if element != nil {
		markdown.containers = append(markdown.containers, element)
		page.structParent = element
	}
}

func (markdown *Markdown) closeContainer() {
	if markdown.page.structParent != nil && len(markdown.containers) > 0 {
		markdown.containers = markdown.containers[:len(markdown.containers)-1]
		markdown.page.structParent = markdown.peekContainer()
	}
}

// finishQuoteBars draws the quote bars of the page, from their top down to
// where the text is.
func (markdown *Markdown) finishQuoteBars() {
	for _, bar := range markdown.quoteBars {
		markdown.drawQuoteBar(bar[0], bar[1], markdown.y)
	}
}

func (markdown *Markdown) drawQuoteBar(x, top, to float32) {
	if to > top {
		NewLine(x, top, x, to).SetStrokeColor(markdownQuoteBarColor).SetStrokeWidth(2).DrawOn(markdown.page)
	}
}

func (markdown *Markdown) drawBlocks(blocks []*markdownBlock, x, width float32, tight bool) {
	for _, block := range blocks {
		switch block.kind {
		case markdownHeading:
			markdown.drawHeading(block, x, width)
		case markdownParagraph:
			if tight {
				markdown.gap(markdown.size() * 0.25)
			} else {
				markdown.gap(markdown.size() * 0.75)
			}
			markdown.drawParagraph(markdown.markup.Paragraph(block.text), x, width)
		case markdownCode:
			markdown.drawCode(block.text, x, width)
		case markdownQuote:
			markdown.drawQuote(block, x, width)
		case markdownList:
			markdown.drawList(block, x, width)
		case markdownRule:
			markdown.drawRule(x, width)
		case markdownTable:
			markdown.drawTable(block, x, width)
		case markdownImage:
			markdown.drawImage(block, x, width)
		default:
		}
	}
}

// drawParagraph draws a paragraph in a text frame on the page, and on the
// next pages for what does not fit.
func (markdown *Markdown) drawParagraph(paragraph *Paragraph, x, width float32) {
	if len(paragraph.lines) == 0 {
		return
	}
	markdown.ensure(markdownFirstLineHeight(paragraph))
	frame := NewTextFrameFromParagraphs([]*Paragraph{paragraph}).SetParagraphGap(0)
	frame.SetLocation(x, markdown.y)
	frame.SetWidth(width).SetHeight(markdown.bottom - markdown.y)
	frame.DrawOn(markdown.page)
	for frame.HasMoreText() {
		markdown.newPage()
		frame.SetLocation(x, markdown.y)
		frame.SetHeight(markdown.bottom - markdown.y)
		frame.DrawOn(markdown.page)
	}
	markdown.y = paragraph.GetY2()
	markdown.atTop = false
}

func markdownFirstLineHeight(paragraph *Paragraph) float32 {
	var height float32
	for _, line := range paragraph.lines {
		height = max(height, line.font.GetBodyHeight(line.fontSize))
	}
	return height
}

// drawHeading draws a heading, larger than the text, which keeps a line of
// the text after it on its page. Its level as it is tagged is at most one
// more than that of the heading before it, so that no level is skipped.
func (markdown *Markdown) drawHeading(block *markdownBlock, x, width float32) {
	fontSize := markdown.size() * markdownHeadingSizes[block.level-1]
	if block.level <= 2 {
		markdown.gap(markdown.size() * 1.4)
	} else {
		markdown.gap(markdown.size() * 1.1)
	}
	paragraph := markdown.markup.Paragraph(block.text)
	for _, line := range paragraph.lines {
		if line.font == markdown.regular {
			line.SetFont(markdown.headingFont)
		} else if line.font == markdown.italic {
			line.SetFont(markdown.boldItalic)
		}
		line.SetFontSize(fontSize)
	}
	markdown.headingLevel = min(block.level, markdown.headingLevel+1)
	levels := [6]structelem.StructElem{structelem.H1, structelem.H2, structelem.H3,
		structelem.H4, structelem.H5, structelem.H6}
	paragraph.SetStructureType(levels[markdown.headingLevel-1])
	markdown.ensure(markdown.headingFont.GetBodyHeight(fontSize) + 2*markdown.regular.GetBodyHeight(markdown.size()))
	markdown.drawParagraph(paragraph, x, width)
	markdown.y += markdown.size() * 0.25
}

// drawCode draws code in the code font on a light background, a line of the
// source at a time, and a line too long for the width goes on under itself.
// The lines are cut in Java's characters, UTF-16 code units, of which a
// character outside the Basic Multilingual Plane is two, and never inside such
// a character.
func (markdown *Markdown) drawCode(text string, x, width float32) {
	code := markdown.code
	markdown.gap(markdown.size() * 0.75)
	padding := markdown.size() * 0.5
	leading := code.GetBodyHeight(code.GetSize()) * 1.2
	columns := max(1, int((width-2*padding)/code.StringWidth(code.GetSize(), "0")))
	lines := make([]string, 0)
	for _, line := range strings.Split(text, "\n") {
		units := utf16.Encode([]rune(line))
		start := 0
		for len(units)-start > columns {
			// A surrogate pair is not cut in two: it goes on the next
			// line, or on this one when it would be the whole line.
			cut := start + columns
			if markdownIsHighSurrogate(units[cut-1]) {
				if cut-1 > start {
					cut = cut - 1
				} else {
					cut = cut + 1
				}
			}
			lines = append(lines, string(utf16.Decode(units[start:cut])))
			start = cut
		}
		lines = append(lines, string(utf16.Decode(units[start:])))
	}
	markdown.ensure(float32(min(3, len(lines)))*leading + 2*padding)
	markdown.openContainer(structelem.Code)
	i := 0
	for i < len(lines) {
		fit := max(1, int((markdown.bottom-markdown.y-2*padding)/leading))
		count := min(fit, len(lines)-i)
		NewRect(x, markdown.y, width, float32(count)*leading+2*padding).
			SetFillColor(markdownCodeBackground).DrawOn(markdown.page)
		baseline := markdown.y + padding + code.GetAscent(code.GetSize()) +
			(leading-code.GetBodyHeight(code.GetSize()))/2
		for j := 0; j < count; j, i = j+1, i+1 {
			if trimSpace(lines[i]) != "" {
				NewTextLine(code, lines[i]).SetStructureType(structelem.Span).
					SetLocation(x+padding, baseline).DrawOn(markdown.page)
			}
			baseline += leading
		}
		markdown.y += float32(count)*leading + 2*padding
		markdown.atTop = false
		if i < len(lines) {
			markdown.newPage()
		}
	}
	markdown.closeContainer()
}

func markdownIsHighSurrogate(unit uint16) bool {
	return unit >= 0xD800 && unit <= 0xDBFF
}

// drawQuote draws a quote, indented, with a bar on its left.
func (markdown *Markdown) drawQuote(block *markdownBlock, x, width float32) {
	markdown.gap(markdown.size() * 0.75)
	markdown.ensure(markdown.regular.GetBodyHeight(markdown.size()))
	indent := markdown.size() * 1.2
	bar := &[2]float32{x + 2, markdown.y}
	markdown.quoteBars = append(markdown.quoteBars, bar)
	markdown.openContainer(structelem.BlockQuote)
	top := markdown.atTop
	markdown.atTop = true // The first block of the quote starts where the bar does.
	markdown.drawBlocks(block.children, x+indent, width-indent, false)
	markdown.atTop = top && markdown.atTop
	markdown.closeContainer()
	markdown.removeQuoteBar(bar)
	markdown.drawQuoteBar(bar[0], bar[1], markdown.y)
}

func (markdown *Markdown) removeQuoteBar(bar *[2]float32) {
	for i, b := range markdown.quoteBars {
		if b == bar {
			markdown.quoteBars = append(markdown.quoteBars[:i], markdown.quoteBars[i+1:]...)
			return
		}
	}
}

// drawList draws a list: the label of each item, a bullet or a number, to the
// left of the blocks of the item.
func (markdown *Markdown) drawList(list *markdownBlock, x, width float32) {
	regular := markdown.regular
	markdown.gap(markdown.size() * 0.75)
	indent := markdown.size() * 1.4
	if list.ordered {
		indent = markdown.size() * 2
	}
	markdown.openContainer(structelem.L)
	number := list.start
	for _, item := range list.children {
		if item != list.children[0] {
			if list.loose {
				markdown.gap(markdown.size() * 0.5)
			} else {
				markdown.gap(markdown.size() * 0.2)
			}
		}
		markdown.ensure(regular.GetBodyHeight(markdown.size()))
		markdown.openContainer(structelem.LI)
		label := "•"
		if list.ordered {
			label = strconv.Itoa(number) + "."
		}
		text := NewTextLine(regular, label).SetStructureType(structelem.Lbl)
		labelX := x + markdown.size()*0.3
		if list.ordered {
			labelX = x + indent - markdown.size()*0.4 - text.GetWidth()
		}
		text.SetLocation(labelX, markdown.y+regular.GetAscent(markdown.size())).DrawOn(markdown.page)
		markdown.openContainer(structelem.LBody)
		markdown.atTop = true // The first block of the item is on the line of its label.
		markdown.drawBlocks(item.children, x+indent, width-indent, !list.loose)
		if markdown.atTop {
			// An item with no text still takes the line of its label.
			markdown.y += regular.GetBodyHeight(markdown.size())
		}
		markdown.atTop = false
		markdown.closeContainer()
		markdown.closeContainer()
		number++
	}
	markdown.closeContainer()
}

func (markdown *Markdown) drawRule(x, width float32) {
	markdown.gap(markdown.size() * 0.75)
	markdown.ensure(markdown.size())
	middle := markdown.y + markdown.size()*0.5
	NewLine(x, middle, x+width, middle).SetStrokeColor(markdownRuleColor).SetStrokeWidth(1).DrawOn(markdown.page)
	markdown.y += markdown.size()
	markdown.atTop = false
}

// drawTable draws a table of the text of the cells, with the header row in
// bold on a light background, as wide as its text or as the width when it
// would be wider, and on the next pages for the rows that do not fit.
func (markdown *Markdown) drawTable(block *markdownBlock, x, width float32) {
	markdown.gap(markdown.size() * 0.75)
	rows := make([][]*Cell, 0, len(block.rows))
	for r := range block.rows {
		row := make([]*Cell, 0, len(block.rows[r]))
		for c := range block.rows[r] {
			font := markdown.regular
			if r == 0 {
				font = markdown.bold
			}
			cell := NewCell(font, markdown.plainText(block.rows[r][c]))
			cell.SetTextAlignment(block.alignments[c])
			row = append(row, cell)
		}
		rows = append(rows, row)
	}
	table := NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(markdown.bold, 0x000000, markdownTableHeaderBackground)
	table.SetCellBorderColor(0xC8CCD2)
	table.AutoAdjustColumnWidths()
	if table.GetWidth() > width {
		table.FitToWidth(width)
	}
	rowHeight := 2 * markdown.regular.GetBodyHeight(markdown.size())
	markdown.ensure(2 * rowHeight)
	table.SetLocation(x, markdown.marginTop)
	table.SetFirstPageTopMargin(markdown.y)
	table.SetBottomMargin(markdown.marginBottom)
	before := len(*markdown.pages)
	xy := table.drawOnPages(markdown.pdf, markdown.page, markdown.pages, markdown.pageSize)
	if len(*markdown.pages) > before {
		// The table drew its last rows on a page of its own.
		markdown.finishQuoteBars()
		markdown.page = (*markdown.pages)[len(*markdown.pages)-1]
		markdown.page.structParent = markdown.peekContainer()
		for _, bar := range markdown.quoteBars {
			bar[1] = markdown.marginTop
		}
	}
	markdown.y = xy[1]
	markdown.atTop = false
}

// plainText returns the text of a cell, without its inline markup.
func (markdown *Markdown) plainText(text string) string {
	var buf strings.Builder
	paragraph := markdown.markup.Paragraph(text)
	for i, line := range paragraph.lines {
		if i > 0 && !paragraph.joinsPrevious(i) {
			buf.WriteByte(' ')
		}
		buf.WriteString(trimSpace(line.text))
	}
	return buf.String()
}

// drawImage draws an image as wide as it is, or as the width when it is
// wider, and as tall as the page when it is taller; or its text, in italic,
// when it is not read. An image file that cannot be read panics, as the Image
// of a file does.
func (markdown *Markdown) drawImage(block *markdownBlock, x, width float32) {
	markdown.gap(markdown.size() * 0.75)
	path := markdown.imagePath(block.source)
	if path == "" {
		text := block.text
		if text == "" {
			text = block.source
		}
		paragraph := NewParagraph().Add(NewTextLine(markdown.italic, text))
		markdown.drawParagraph(paragraph, x, width)
		return
	}
	alt := block.text
	if alt == "" {
		alt = filepath.Base(block.source)
	}
	var scale, imageWidth, imageHeight float32
	var drawable Drawable
	if strings.HasSuffix(strings.ToLower(path), ".svg") {
		image, err := NewSVGImageFromFile(path)
		if err != nil {
			panic(err)
		}
		image.SetAltDescription(alt)
		imageWidth = image.GetWidth()
		imageHeight = image.GetHeight()
		scale = min(1, min(width/imageWidth, (markdown.bottom-markdown.marginTop)/imageHeight))
		image.ScaleBy(scale)
		drawable = image
	} else {
		image := NewImageFromFile(markdown.pdf, path)
		image.SetAltDescription(alt)
		imageWidth = image.GetWidth()
		imageHeight = image.GetHeight()
		scale = min(1, min(width/imageWidth, (markdown.bottom-markdown.marginTop)/imageHeight))
		image.ScaleBy(scale)
		drawable = image
	}
	markdown.ensure(imageHeight * scale)
	drawable.SetLocation(x, markdown.y)
	drawable.DrawOn(markdown.page)
	markdown.y += imageHeight * scale
	markdown.atTop = false
}

// imagePath returns the path of an image in the image directory, or "" when
// there is no directory, or the source is an absolute path, a URL, has .. in
// it, or names no file.
func (markdown *Markdown) imagePath(source string) string {
	if !markdown.hasImages || strings.HasPrefix(source, "/") || strings.HasPrefix(source, "\\") ||
		strings.IndexByte(source, ':') != -1 || strings.IndexByte(source, '\\') != -1 {
		return ""
	}
	for _, part := range strings.Split(source, "/") {
		if part == ".." {
			return ""
		}
	}
	path := filepath.Join(markdown.imageDirectory, source)
	if info, err := os.Stat(path); err != nil || !info.Mode().IsRegular() {
		return ""
	}
	return path
}
