// markdown.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"unicode/utf8"

	"github.com/edragoev1/pdfjet/v9/src/capstyle"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/joinstyle"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/pathoperator"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// The heading sizes, as factors of the size of the text.
var markdownHeadingSizes = [6]float32{2.0, 1.6, 1.3, 1.15, 1.0, 0.9}

const (
	markdownCodeBackground        = 0xF3F5F8
	markdownRuleColor             = 0xC8CCD2
	markdownQuoteBarColor         = 0xB0B6BF
	markdownTableHeaderBackground = 0xEEF0F3
	markdownTaskBoxColor          = 0x8C959F
	// The least width, in sizes of the text, that quotes and lists nested in
	// each other leave their text: deeper ones are not indented further, so
	// that the text is never narrower than nothing.
	markdownMinWidth = 10
)

// Markdown draws a Markdown text on as many pages as it needs: headings of #
// and of underlines, paragraphs with the inline markup of Markup, bullet and
// numbered lists that nest, block quotes, fenced and indented code, thematic
// breaks, the tables and the task lists of GitHub's Markdown, - [ ] and
// - [x], drawn as boxes, and images alone in their paragraph. In a PDF/UA
// document the headings are H1 to H6, with no level skipped, the lists are
// lists, the boxes of the tasks their labels, read as ☐ and ☑, the quotes
// BlockQuotes, the code Code, the tables tables and the images figures, with
// their text as the alternate description. In any document the headings are
// its bookmarks, the outline a reader shows, unless it has bookmarks of its
// own.
//
// A line that ends in two spaces or a backslash, and a <br>, break the line,
// in a table cell too, and the entities of Markup, as &amp; and &mdash;, are
// the characters they name.
//
// It is not all of Markdown: other HTML is drawn as the text it is, any other
// line break is a space, links in a table cell are its text, and an image is
// drawn only when it is alone in its paragraph, from the directory that SetImageDirectory
// names; with none, its text is drawn instead, so that a text from anyone
// reads no file. Please see Example_19.
//
// To build the pages from paragraphs and images of your own, use TextFlow.
type Markdown struct {
	regular         *Font
	bold            *Font
	italic          *Font
	boldItalic      *Font
	code            *Font
	markup          *Markup
	headingFont     *Font
	headingColor    int32 // color.Transparent leaves the headings in the color of the text
	fullWidthTables bool  // Tables as wide as the text, or as their text
	checkColor      int32 // Of the box of a task that is done, and of a check mark in a table
	marginLeft      float32
	marginTop       float32
	marginRight     float32
	marginBottom    float32
	imageDirectory  string
	hasImages       bool // Whether SetImageDirectory set the directory

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
	quoteBars  []*[2]float32
	allowSplit bool // The next table starts where it is, under its heading
	// The tables and the images of the blocks, made when they are first
	// measured and drawn as they were made: an image is in the PDF once
	tables map[*markdownBlock]*Table
	images map[*markdownBlock]*markdownImageOf
}

// markdownImageOf is the image of a block, scaled to the width and to a page,
// and its height: no drawable for an image that is not read, whose text is
// drawn in its place, or that failed the document.
type markdownImageOf struct {
	drawable Drawable
	height   float32
	failed   bool // The file could not be read, and the document failed
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
		headingColor: color.Transparent,
		checkColor:   0x1A7F37,
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

// SetHeadingColor sets the color of the headings, as a 0xRRGGBB value. They
// are in the color of the text unless it is set.
func (markdown *Markdown) SetHeadingColor(headingColor int32) *Markdown {
	markdown.headingColor = headingColor
	return markdown
}

// SetFullWidthTables sets whether the tables are as wide as the text, their
// columns widened by the share of the text each holds. They are as wide as
// their text unless it is set, and a table wider than the text is fitted to it
// either way.
func (markdown *Markdown) SetFullWidthTables(fullWidthTables bool) *Markdown {
	markdown.fullWidthTables = fullWidthTables
	return markdown
}

// SetLinkColor sets the color of the text of links, as a 0xRRGGBB value. They
// are blue unless it is set.
func (markdown *Markdown) SetLinkColor(linkColor int32) *Markdown {
	markdown.markup.SetLinkColor(linkColor)
	return markdown
}

// SetCheckColor sets the color of the box of a task that is done, - [x] in
// the Markdown, as a 0xRRGGBB value, which has a white check mark in it, and
// of the text of a table cell that starts with a check mark, U+2713, as a
// table of features marks what each has. It is green, 0x1A7F37, unless it is
// set; color.Transparent leaves it as it is.
func (markdown *Markdown) SetCheckColor(checkColor int32) *Markdown {
	if checkColor == color.Transparent {
		return markdown
	}
	markdown.checkColor = checkColor
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

// SetImageDirectory sets the directory that the images are read from: the
// source of an image in the Markdown is the file of that name in the
// directory, a JPEG, PNG, BMP or SVG file. A source that is an absolute path, a URL or has .. in
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
	markdown.tables = make(map[*markdownBlock]*Table)
	markdown.images = make(map[*markdownBlock]*markdownImageOf)
	if !finite(markdown.marginLeft, markdown.marginTop, markdown.marginRight, markdown.marginBottom) {
		// Pages were made until the memory ran out (review of 12 October
		// 2026, F4)
		pdf.fail("The margins of the Markdown must be numbers, not NaN or infinite.")
		return [2]float32{markdown.marginLeft, markdown.marginTop}
	}
	blocks := markdownParser{}.parse(text)
	width := pageSize.GetWidth() - markdown.marginLeft - markdown.marginRight
	cells, tooWide := tableCells(blocks)
	// A table too wide for a page, or tables too large, make the text an
	// error, as a table of more columns was drawn as a paragraph of pipes
	// (review of 12 October 2026, R33)
	if tooWide {
		pdf.fail("A table of the Markdown has more than " + strconv.Itoa(maxTableColumns) +
			" columns, more than a page can show.")
		return [2]float32{markdown.marginLeft + width, markdown.marginTop}
	}
	if cells > maxTableCells {
		pdf.fail("The tables of the Markdown have more than " + strconv.Itoa(maxTableCells) +
			" cells: draw a table that large with BigTable.")
		return [2]float32{markdown.marginLeft + width, markdown.marginTop}
	}
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
		markdownDrawQuoteBar(markdown.page, bar[0], bar[1], markdown.y)
	}
}

func markdownDrawQuoteBar(page *Page, x, top, to float32) {
	if to > top {
		NewLine(x, top, x, to).SetStrokeColor(markdownQuoteBarColor).SetStrokeWidth(2).DrawOn(page)
	}
}

func (markdown *Markdown) drawBlocks(blocks []*markdownBlock, x, width float32, tight bool) {
	for i, block := range blocks {
		switch block.kind {
		case markdownHeading:
			// An empty heading draws nothing, and takes no level of the
			// tags: "#" made the next "##" an H2 with no H1 over it
			if trimSpace(block.text) == "" {
				break
			}
			markdown.drawHeading(block, x, width, markdown.headingKeep(blocks, i, width, tight))
		case markdownParagraph:
			gap := float32(markdown.size() * 0.75)
			if tight {
				gap = float32(markdown.size() * 0.25)
			}
			if i+1 < len(blocks) && markdownLeadsIntoTable(block, blocks[i+1]) {
				markdown.keepWithTable(block, blocks[i+1], gap, x, width)
			}
			markdown.gap(gap)
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

// headingKeep returns the room a heading keeps on its page: itself and what
// comes after it, as far as a page keeps that together, so that the heading
// does not end a page. When the two do not fit on one page, the heading and
// two lines, or the first rows of a table under it, or of one after its
// lead-in, which then starts there: the table went to the next page alone.
func (markdown *Markdown) headingKeep(blocks []*markdownBlock, i int, width float32, tight bool) float32 {
	keep := markdown.keepHeight(blocks, i, width, tight)
	if keep <= markdown.bottom-markdown.marginTop {
		return keep
	}
	line := markdown.regular.GetBodyHeight(markdown.size())
	heading := markdown.headingHeight(blocks[i], width)
	if i+1 < len(blocks) && blocks[i+1].kind == markdownTable {
		markdown.allowSplit = true
		return heading + float32(markdown.size()*0.75) + float32(4*line)
	}
	if i+2 < len(blocks) && markdownLeadsIntoTable(blocks[i+1], blocks[i+2]) {
		markdown.allowSplit = true
		return heading + markdown.paragraphGap(tight) + markdown.paragraphHeight(blocks[i+1].text, width) +
			float32(markdown.size()*0.75) + float32(4*line)
	}
	return heading + float32(2*line)
}

// firstKeep returns the room the first block of a quote or a list item needs
// on its page, which the block does not check, drawn where the quote's bar or
// the item's label is: a first image, code or table was drawn into the bottom
// margin, and a short table was split.
func (markdown *Markdown) firstKeep(blocks []*markdownBlock, width float32, tight bool) float32 {
	if len(blocks) == 0 {
		return markdown.regular.GetBodyHeight(markdown.size())
	}
	var keep float32
	if blocks[0].kind == markdownHeading {
		keep = markdown.headingKeep(blocks, 0, width, tight)
	} else {
		keep = markdown.keepHeight(blocks, 0, width, tight)
	}
	return min(keep, markdown.bottom-markdown.marginTop)
}

// keepHeight returns the height a block needs on its page where it starts:
// the whole of a table, an image or a lead-in and its table that fit on a
// page; three lines of code; a heading and what it keeps; or a line of text.
func (markdown *Markdown) keepHeight(blocks []*markdownBlock, i int, width float32, tight bool) float32 {
	block := blocks[i]
	line := markdown.regular.GetBodyHeight(markdown.size())
	room := markdown.bottom - markdown.marginTop
	switch block.kind {
	case markdownHeading:
		heading := markdown.headingHeight(block, width)
		if i+1 < len(blocks) {
			next := blocks[i+1]
			gap := float32(markdown.size() * 0.75)
			if next.kind == markdownParagraph {
				gap = markdown.paragraphGap(tight)
			} else if next.kind == markdownHeading {
				gap = markdown.headingGap(next)
			}
			return heading + max(gap+markdown.keepHeight(blocks, i+1, width, tight), float32(2*line))
		}
		return heading + float32(2*line)
	case markdownParagraph:
		if i+1 < len(blocks) && markdownLeadsIntoTable(block, blocks[i+1]) {
			keep := markdown.paragraphHeight(block.text, width) + float32(markdown.size()*0.75) +
				markdown.tableOf(blocks[i+1], width).getTotalHeight()
			if keep <= room {
				return keep
			}
		}
		return line
	case markdownTable:
		height := markdown.tableOf(block, width).getTotalHeight()
		if height <= room {
			return height
		}
		return float32(4 * line)
	case markdownImage:
		image := markdown.imageOf(block, width)
		if image.drawable == nil {
			return line
		}
		return image.height
	case markdownCode:
		if block.text == "" {
			return 0
		}
		return markdown.codeHeight(block.text, width)
	case markdownQuote:
		if len(block.children) == 0 {
			return line
		}
		return markdown.keepHeight(block.children, 0,
			width-markdown.indentWithin(float32(markdown.size()*1.2), width), false)
	case markdownList:
		if len(block.children) == 0 || len(block.children[0].children) == 0 {
			return line
		}
		indent := float32(1.4)
		if block.ordered {
			indent = 2
		}
		return markdown.keepHeight(block.children[0].children, 0,
			width-markdown.indentWithin(float32(markdown.size()*indent), width), !block.loose)
	default:
		return line
	}
}

// paragraphGap returns the space before a paragraph.
func (markdown *Markdown) paragraphGap(tight bool) float32 {
	if tight {
		return float32(markdown.size() * 0.25)
	}
	return float32(markdown.size() * 0.75)
}

// headingHeight returns the height of a heading's text at the width, of all
// its lines: a heading of two lines was kept on its page by its first.
func (markdown *Markdown) headingHeight(block *markdownBlock, width float32) float32 {
	frame := NewTextFrameFromParagraphs([]*Paragraph{markdown.headingParagraph(block)}).SetParagraphGap(0)
	frame.SetLocation(0, 0)
	frame.SetWidth(width).SetHeight(markdown.bottom - markdown.marginTop)
	frame.DrawOn(nil)
	return frame.GetTextBottom()
}

// paragraphHeight returns the height of a paragraph's text at the width.
func (markdown *Markdown) paragraphHeight(text string, width float32) float32 {
	frame := NewTextFrameFromParagraphs([]*Paragraph{markdown.markup.Paragraph(text)}).SetParagraphGap(0)
	frame.SetLocation(0, 0)
	frame.SetWidth(width).SetHeight(markdown.bottom - markdown.marginTop)
	frame.DrawOn(nil)
	return frame.GetTextBottom()
}

// codeLines returns the lines of code at the width, each cut where the next
// character would pass it, measured in the code font: by the count of
// characters as wide as a "0", CJK and other wide characters ran past the
// background. The widths of the characters are added up in one pass, so that
// a long line is cut in linear time. A character is not cut in two, a line
// has a character at least, and a code font with no width cuts none.
func (markdown *Markdown) codeLines(text string, width float32) []string {
	code := markdown.code
	room := width - float32(float32(2*markdown.size())*0.5)
	lines := make([]string, 0)
	for _, line := range strings.Split(text, "\n") {
		start := 0
		used := float32(0)
		i := 0
		for i < len(line) {
			_, n := utf8.DecodeRuneInString(line[i:])
			next := i + n
			advance := code.StringWidth(code.GetSize(), line[i:next])
			if i > start && used+advance > room {
				lines = append(lines, line[start:i])
				start = i
				used = 0
			}
			used += advance
			i = next
		}
		lines = append(lines, line[start:])
	}
	return lines
}

// codeHeight returns the height code keeps together on a page: three lines
// at most.
func (markdown *Markdown) codeHeight(text string, width float32) float32 {
	code := markdown.code
	leading := float32(code.GetBodyHeight(code.GetSize()) * 1.2)
	return float32(float32(min(3, len(markdown.codeLines(text, width))))*leading) +
		float32(float32(2*markdown.size())*0.5)
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
func (markdown *Markdown) drawHeading(block *markdownBlock, x, width, keep float32) {
	markdown.gap(markdown.headingGap(block))
	paragraph := markdown.headingParagraph(block)
	markdown.headingLevel = min(block.level, markdown.headingLevel+1)
	levels := [6]structelem.StructElem{structelem.H1, structelem.H2, structelem.H3,
		structelem.H4, structelem.H5, structelem.H6}
	paragraph.SetStructureType(levels[markdown.headingLevel-1])
	markdown.ensure(keep)
	if !markdown.pdf.isTagged() {
		// The bookmark of the heading, at its level as it would be tagged: a
		// tagged document notes its headings as it draws them
		title := strings.Join(strings.Fields(paragraph.text()), " ")
		if title != "" {
			markdown.pdf.headings = append(markdown.pdf.headings, heading{
				level: markdown.headingLevel, title: title, page: markdown.page, top: markdown.y})
		}
	}
	markdown.drawParagraph(paragraph, x, width)
	markdown.y += float32(markdown.size() * 0.25)
}

// headingGap returns the space above a heading.
func (markdown *Markdown) headingGap(block *markdownBlock) float32 {
	if block.level <= 2 {
		return float32(markdown.size() * 1.4)
	}
	return float32(markdown.size() * 1.1)
}

// headingParagraph returns the text of a heading in the heading font, at its
// level's size and in the heading color.
func (markdown *Markdown) headingParagraph(block *markdownBlock) *Paragraph {
	fontSize := float32(markdown.size() * markdownHeadingSizes[block.level-1])
	paragraph := markdown.markup.Paragraph(block.text)
	for _, line := range paragraph.lines {
		if line.font == markdown.regular {
			line.SetFont(markdown.headingFont)
		} else if line.font == markdown.italic {
			line.SetFont(markdown.boldItalic)
		}
		line.SetFontSize(fontSize)
		if line.uri == "" && line.key == "" {
			line.SetTextColor(markdown.headingColor)
		}
	}
	return paragraph
}

// drawCode draws code in the code font on a light background, a line of the
// source at a time, and a line too long for the width goes on under itself.
func (markdown *Markdown) drawCode(text string, x, width float32) {
	if text == "" {
		return // An empty block of code has nothing to tag, and no background
	}
	code := markdown.code
	markdown.gap(float32(markdown.size() * 0.75))
	padding := float32(markdown.size() * 0.5)
	leading := float32(code.GetBodyHeight(code.GetSize()) * 1.2)
	lines := markdown.codeLines(text, width)
	markdown.ensure(markdown.codeHeight(text, width))
	// Code is an inline element, so the block is a paragraph that holds
	// its Code: PAC warns of a Code right under the document.
	markdown.openContainer(structelem.P)
	markdown.openContainer(structelem.Code)
	i := 0
	for i < len(lines) {
		fit := len(lines) - i // Lines of no height all fit
		if leading > 0 {
			fit = max(1, saturatingInt((markdown.bottom-markdown.y-float32(2*padding))/leading))
		}
		count := min(fit, len(lines)-i)
		NewRect(x, markdown.y, width, float32(float32(count)*leading)+float32(2*padding)).
			SetFillColor(markdownCodeBackground).DrawOn(markdown.page)
		baseline := markdown.y + padding + code.GetAscent(code.GetSize()) +
			float32((leading-code.GetBodyHeight(code.GetSize()))/2)
		for j := 0; j < count; j, i = j+1, i+1 {
			if trimSpace(lines[i]) != "" {
				NewTextLine(code, lines[i]).SetStructureType(structelem.Span).
					SetLocation(x+padding, baseline).DrawOn(markdown.page)
			}
			baseline += leading
		}
		markdown.y += float32(float32(count)*leading) + float32(2*padding)
		markdown.atTop = false
		if i < len(lines) {
			markdown.newPage()
		}
	}
	markdown.closeContainer()
	markdown.closeContainer()
}

// drawQuote draws a quote, indented, with a bar on its left.
func (markdown *Markdown) drawQuote(block *markdownBlock, x, width float32) {
	markdown.gap(float32(markdown.size() * 0.75))
	indent := markdown.indentWithin(float32(markdown.size()*1.2), width)
	// Room for its first block, which does not check the room it has
	markdown.ensure(markdown.firstKeep(block.children, width-indent, false))
	bar := &[2]float32{x + 2, markdown.y}
	markdown.quoteBars = append(markdown.quoteBars, bar)
	markdown.openContainer(structelem.BlockQuote)
	top := markdown.atTop
	markdown.atTop = true // The first block of the quote starts where the bar does.
	markdown.drawBlocks(block.children, x+indent, width-indent, false)
	markdown.atTop = top && markdown.atTop
	markdown.closeContainer()
	markdown.removeQuoteBar(bar)
	markdownDrawQuoteBar(markdown.page, bar[0], bar[1], markdown.y)
}

// indentWithin returns the indent, or less, so that the blocks inside a quote
// or a list are not narrower than markdownMinWidth sizes of the text.
func (markdown *Markdown) indentWithin(indent, width float32) float32 {
	return max(0, min(indent, width-float32(markdownMinWidth*markdown.size())))
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
	markdown.gap(float32(markdown.size() * 0.75))
	indent := float32(markdown.size() * 1.4)
	if list.ordered {
		indent = float32(markdown.size() * 2)
	}
	indent = markdown.indentWithin(indent, width)
	markdown.openContainer(structelem.L)
	number := list.start
	for _, item := range list.children {
		if item != list.children[0] {
			if list.loose {
				markdown.gap(float32(markdown.size() * 0.5))
			} else {
				markdown.gap(float32(markdown.size() * 0.2))
			}
		}
		markdown.ensure(markdown.firstKeep(item.children, width-indent, !list.loose))
		markdown.openContainer(structelem.LI)
		if item.task != markdownNoTask {
			markdown.drawTaskBox(item.task == markdownTaskDone, x, indent)
		} else {
			label := "•"
			if list.ordered {
				label = strconv.Itoa(number) + "."
			}
			text := NewTextLine(regular, label).SetStructureType(structelem.Lbl)
			labelX := x + float32(markdown.size()*0.3)
			if list.ordered {
				labelX = x + indent - float32(markdown.size()*0.4) - text.GetWidth()
			}
			text.SetLocation(labelX, markdown.y+regular.GetAscent(markdown.size())).DrawOn(markdown.page)
		}
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

// drawTaskBox draws the box of a task in place of the label of its item, on
// the line of its first text: a gray box, or a box of the check color with a
// white check mark when the task is done. A screen reader reads it as ☐ or ☑,
// the label of the item.
func (markdown *Markdown) drawTaskBox(done bool, x, indent float32) {
	page := markdown.page
	size := markdown.size()
	side := float32(size * 0.8)
	left := x + float32((indent-float32(size*0.4)-side)/2)
	baseline := markdown.y + markdown.regular.GetAscent(size)
	top := (baseline - float32(size*0.36)) - float32(side/2)
	actualText := "☐"
	if done {
		actualText = "☑"
	}
	// The box is drawn with the page's paths, not a Rect and Lines, which
	// are artifacts, as artifacts may not be inside the label.
	page.AddBDC(structelem.Lbl, "", actualText, actualText)
	page.SaveGraphicsState()
	box := markdownRoundedBox(left, top, side, float32(side*0.2))
	if done {
		page.SetBrushColor(markdown.checkColor)
		page.DrawPath(box, pathoperator.Fill)
		page.SetPenColor(color.White)
		page.SetPenWidth(float32(side * 0.14))
		page.SetLineCapStyle(capstyle.Round)
		page.SetLineJoinStyle(joinstyle.Round)
		page.MoveTo(left+float32(side*0.24), top+float32(side*0.52))
		page.LineTo(left+float32(side*0.43), top+float32(side*0.70))
		page.LineTo(left+float32(side*0.77), top+float32(side*0.31))
		page.StrokePath()
	} else {
		page.SetBrushColor(color.White)
		page.SetPenColor(markdownTaskBoxColor)
		page.SetPenWidth(float32(size * 0.08))
		page.DrawPath(box, pathoperator.FillAndStroke)
	}
	page.RestoreGraphicsState()
	page.AddEMC()
}

// markdownRoundedBox returns the path of a square with rounded corners, of
// the side and the radius, its top left corner at x and y.
func markdownRoundedBox(x, y, side, r float32) []*Point {
	const k float32 = 0.55228
	c := float32(r * k)
	return []*Point{
		NewPoint(x+r, y), NewPoint((x+side)-r, y),
		NewControlPointC(((x+side)-r)+c, y), NewControlPointC(x+side, (y+r)-c), NewPoint(x+side, y+r),
		NewPoint(x+side, (y+side)-r),
		NewControlPointC(x+side, ((y+side)-r)+c), NewControlPointC(((x+side)-r)+c, y+side), NewPoint((x+side)-r, y+side),
		NewPoint(x+r, y+side),
		NewControlPointC((x+r)-c, y+side), NewControlPointC(x, ((y+side)-r)+c), NewPoint(x, (y+side)-r),
		NewPoint(x, y+r),
		NewControlPointC(x, (y+r)-c), NewControlPointC((x+r)-c, y), NewPoint(x+r, y),
	}
}

func (markdown *Markdown) drawRule(x, width float32) {
	markdown.gap(float32(markdown.size() * 0.75))
	markdown.ensure(markdown.size())
	middle := markdown.y + float32(markdown.size()*0.5)
	NewLine(x, middle, x+width, middle).SetStrokeColor(markdownRuleColor).SetStrokeWidth(1).DrawOn(markdown.page)
	markdown.y += markdown.size()
	markdown.atTop = false
}

// markdownCheckMark starts a table cell drawn in the check color.
const markdownCheckMark = "\u2713"

// markdownLeadsIntoTable tells if the block is a paragraph that ends in a
// colon, which leads into the table after it.
// The lead-in is trimmed as Java's String.trim() trims, of the characters up
// to U+0020 only.
func markdownLeadsIntoTable(block, next *markdownBlock) bool {
	text := trimSpace(block.text)
	return block.kind == markdownParagraph && next.kind == markdownTable &&
		(strings.HasSuffix(text, ":") || strings.HasSuffix(text, "：")) // Or the full-width colon of CJK text
}

// keepWithTable starts a page for a paragraph that ends in a colon and the
// table after it, which it leads into as "The size of the PDFs, KB:" does,
// when the two fit on a page together but not in what is left of this one.
func (markdown *Markdown) keepWithTable(paragraph, table *markdownBlock, gap, x, width float32) {
	if markdown.atTop || markdown.allowSplit {
		return // Its table starts under it, after a heading that keeps them
	}
	room := markdown.bottom - markdown.marginTop
	frame := NewTextFrameFromParagraphs([]*Paragraph{markdown.markup.Paragraph(paragraph.text)}).SetParagraphGap(0)
	frame.SetLocation(x, markdown.y+gap)
	frame.SetWidth(width).SetHeight(room)
	frame.DrawOn(nil)
	needed := (frame.GetTextBottom() - markdown.y) + float32(markdown.size()*0.75) +
		markdown.tableOf(table, width).getTotalHeight()
	if markdown.y+needed > markdown.bottom && needed-gap <= room {
		markdown.newPage()
	}
}

// drawTable draws a table of the text of the cells, with the header row in
// bold on a light background, as wide as its text or as the width when it
// would be wider, and on the next pages for the rows that do not fit. A cell
// that starts with a check mark is in the check color, as a table of
// features marks what each has.
func (markdown *Markdown) drawTable(block *markdownBlock, x, width float32) {
	markdown.gap(float32(markdown.size() * 0.75))
	table := markdown.tableOf(block, width)
	// A table that fits on a page is not split: it starts the next page when
	// it does not fit in what is left of this one, but under a heading that
	// keeps it, with which it does not fit on a page
	height := table.getTotalHeight()
	if !markdown.atTop && !markdown.allowSplit && markdown.y+height > markdown.bottom &&
		height <= markdown.bottom-markdown.marginTop {
		markdown.newPage()
	}
	markdown.allowSplit = false
	// Room for its header and a row where it does not fit, not where it
	// does: a short table went to the next page alone
	rowHeight := float32(2 * markdown.regular.GetBodyHeight(markdown.size()))
	if markdown.y+height > markdown.bottom {
		markdown.ensure(float32(2 * rowHeight))
	}
	table.SetLocation(x, markdown.marginTop)
	table.SetFirstPageTopMargin(markdown.y)
	table.SetBottomMargin(markdown.marginBottom)
	before := len(*markdown.pages)
	xy := table.drawOnPages(markdown.pdf, markdown.page, markdown.pages, markdown.pageSize)
	if len(*markdown.pages) > before {
		// The table drew its last rows on a page of its own. The bars of
		// the quotes it is in go down to the bottom of its first page and
		// the length of the pages between: they stopped where it started
		for _, bar := range markdown.quoteBars {
			markdownDrawQuoteBar(markdown.page, bar[0], bar[1], markdown.bottom)
			for p := before; p < len(*markdown.pages)-1; p++ {
				markdownDrawQuoteBar((*markdown.pages)[p], bar[0], markdown.marginTop, markdown.bottom)
			}
		}
		markdown.page = (*markdown.pages)[len(*markdown.pages)-1]
		markdown.page.structParent = markdown.peekContainer()
		for _, bar := range markdown.quoteBars {
			bar[1] = markdown.marginTop
		}
	}
	markdown.y = xy[1]
	markdown.atTop = false
}

// tableOf returns the table of the block, made once: it was made to be
// measured and made again to be drawn.
func (markdown *Markdown) tableOf(block *markdownBlock, width float32) *Table {
	table, ok := markdown.tables[block]
	if !ok {
		table = markdown.makeTable(block, width)
		markdown.tables[block] = table
	}
	return table
}

// makeTable returns the table of the text of the cells, as wide as its text
// or as the width.
func (markdown *Markdown) makeTable(block *markdownBlock, width float32) *Table {
	rows := make([][]*Cell, 0, len(block.rows))
	for r := range block.rows {
		row := make([]*Cell, 0, len(block.rows[r]))
		for c := range block.rows[r] {
			text := block.rows[r][c]
			font := markdown.cellFont(text)
			if r == 0 {
				font = markdown.bold
			}
			plain := markdown.plainText(text)
			cell := NewCell(font, plain)
			cell.properties |= cellLineBreaks // At its <br>
			cell.SetTextAlignment(block.alignments[c])
			if r > 0 && strings.HasPrefix(plain, markdownCheckMark) {
				cell.SetTextColor(markdown.checkColor)
			}
			row = append(row, cell)
		}
		rows = append(rows, row)
	}
	table := NewTable()
	table.SetTableData(rows, 1)
	table.SetHeaderRowStyle(markdown.bold, 0x000000, markdownTableHeaderBackground)
	table.SetCellBorderColor(0xC8CCD2)
	table.AutoAdjustColumnWidths()
	if markdown.fullWidthTables || table.GetWidth() > width {
		table.FitToWidth(width)
	}
	return table
}

// cellFont returns the font of a cell: the font of its emphasis when the whole
// of its text is in one, as **2.1** is in bold, so that a table can mark a
// value; the regular font when it has none, or more than one, as a cell has one
// font.
func (markdown *Markdown) cellFont(text string) *Font {
	var font *Font
	for _, line := range markdown.markup.Paragraph(text).lines {
		if trimSpace(line.text) == "" {
			continue
		}
		if font == nil {
			font = line.font
		} else if font != line.font {
			return markdown.regular
		}
	}
	if font == nil {
		return markdown.regular
	}
	return font
}

// altText returns the text of an image, its alternate description, as it
// reads: its entities and escapes the characters they are, and its emphasis
// marks left out. "Fish &amp; Chips" was its alternate description as written.
func (markdown *Markdown) altText(block *markdownBlock) string {
	return trimSpace(strings.ReplaceAll(markdown.plainText(block.text), "\n", " "))
}

// plainText returns the text of a cell, without its inline markup, a line
// break at each <br>.
func (markdown *Markdown) plainText(text string) string {
	var buf strings.Builder
	paragraph := markdown.markup.Paragraph(text)
	for i, line := range paragraph.lines {
		if i > 0 && paragraph.lines[i-1].lineBreaks > 0 {
			for k := 0; k < paragraph.lines[i-1].lineBreaks; k++ {
				buf.WriteByte('\n') // An empty line in the cell for each more <br>
			}
		} else if i > 0 && !paragraph.joinsPrevious(i) {
			buf.WriteByte(' ')
		}
		buf.WriteString(trimSpace(line.text))
	}
	return buf.String()
}

// drawImage draws an image as wide as it is, or as the width when it is
// wider, and as tall as the page when it is taller; or its text, in italic,
// when it is not read. An image file that cannot be read fails the document:
// it panicked, and Markdown from a user could stop a server (review of 12
// October 2026, F5).
func (markdown *Markdown) drawImage(block *markdownBlock, x, width float32) {
	markdown.gap(float32(markdown.size() * 0.75))
	image := markdown.imageOf(block, width)
	if image.failed {
		return
	}
	if image.drawable == nil {
		text := markdown.altText(block)
		if text == "" {
			text = block.source
		}
		paragraph := NewParagraph().Add(NewTextLine(markdown.italic, text))
		markdown.drawParagraph(paragraph, x, width)
		return
	}
	markdown.ensure(image.height)
	image.drawable.SetLocation(x, markdown.y)
	image.drawable.DrawOn(markdown.page)
	markdown.y += image.height
	markdown.atTop = false
}

// imageOf returns the image of the block, scaled to the width and to a page,
// made when it is first measured or drawn, and its height kept; no drawable
// for an image that is not read, whose text is drawn in its place, or for one
// whose file could not be read, which fails the document.
func (markdown *Markdown) imageOf(block *markdownBlock, width float32) *markdownImageOf {
	if image, ok := markdown.images[block]; ok {
		return image
	}
	image := &markdownImageOf{}
	markdown.images[block] = image
	path := markdown.imagePath(block.source)
	if path == "" {
		return image
	}
	alt := markdown.altText(block)
	if alt == "" {
		alt = filepath.Base(block.source)
	}
	var scale, imageWidth, imageHeight float32
	if strings.HasSuffix(strings.ToLower(path), ".svg") {
		svg, err := NewSVGImageFromFile(path)
		if err != nil {
			markdown.pdf.fail("The image " + block.source + " could not be read: " + err.Error())
			image.failed = true
			return image
		}
		svg.SetAltDescription(alt)
		imageWidth = svg.GetWidth()
		imageHeight = svg.GetHeight()
		scale = min(1, min(width/imageWidth, (markdown.bottom-markdown.marginTop)/imageHeight))
		svg.ScaleBy(scale)
		image.drawable = svg
	} else {
		raster, err := readMarkdownImage(markdown.pdf, path)
		if err != nil {
			markdown.pdf.fail("The image " + block.source + " could not be read: " + err.Error())
			image.failed = true
			return image
		}
		raster.SetAltDescription(alt)
		imageWidth = raster.GetWidth()
		imageHeight = raster.GetHeight()
		scale = min(1, min(width/imageWidth, (markdown.bottom-markdown.marginTop)/imageHeight))
		raster.ScaleBy(scale)
		image.drawable = raster
	}
	image.height = float32(imageHeight * scale)
	return image
}

// readMarkdownImage returns the image of the file, or the error that reading
// it panicked with.
func readMarkdownImage(pdf *PDF, path string) (image *Image, err error) {
	defer func() {
		if r := recover(); r != nil {
			image = nil
			if e, ok := r.(error); ok {
				err = e
			} else {
				err = fmt.Errorf("%v", r)
			}
		}
	}()
	return NewImageFromFile(pdf, path), nil
}

// imagePath returns the path of an image in the image directory, or "" when
// there is no directory, or the source is an absolute path, a URL, has .. in
// it, names a directory, or names no file. The source is read the same way in
// every port: its parts between the slashes, less the empty ones and those
// that are ".", are the names of the directories under the image directory
// and of the file, and a source that ends in a slash or in "." names a
// directory. An empty image directory is the working directory.
func (markdown *Markdown) imagePath(source string) string {
	if !markdown.hasImages || strings.HasPrefix(source, "/") || strings.HasPrefix(source, "\\") ||
		strings.IndexByte(source, ':') != -1 || strings.IndexByte(source, '\\') != -1 {
		return ""
	}
	parts := strings.Split(source, "/")
	if last := parts[len(parts)-1]; last == "" || last == "." {
		return ""
	}
	names := []string{markdown.imageDirectory}
	for _, part := range parts {
		if part == ".." {
			return ""
		}
		if part != "" && part != "." {
			names = append(names, part)
		}
	}
	path := filepath.Join(names...)
	if info, err := os.Stat(path); err != nil || !info.Mode().IsRegular() {
		return ""
	}
	return path
}
