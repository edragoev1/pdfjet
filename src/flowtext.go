// flowtext.go
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
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/pagesize"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// FlowText flows headings, paragraphs and images onto as many pages as they
// need, one below the other between the margins: a report, a manual, a long
// letter. Paragraphs break across pages between their lines, as a TextFrame
// breaks them; an image goes whole, to the top of the next page when it does
// not fit below the text; a heading goes to the next page with what follows
// it rather than end a page. The pages are created detached and added to a
// list, so that a footer or a page number can be drawn on each before they are
// added to the PDF.
//
// Use a TextFrame to place text by hand, in frames on the same page or on
// others, the text flowing from one to the next; use FlowText to pour the text
// and the images in, page after page. The order they are added in is the
// reading order of a PDF/UA document: the paragraphs are tagged as a TextFrame
// tags them, the images as figures with their alternate descriptions.
type FlowText struct {
	pdf                      *PDF
	pageSize                 pagesize.PageSize
	top, right, bottom, left float32
	firstPageTop             float32
	items                    []flowItem
}

// flowItem is one thing added to the flow: paragraphs, an image or a space.
type flowItem struct {
	paragraphs []*Paragraph
	gap        float32
	hasGap     bool
	image      *Image
	align      alignment.Alignment
	space      float32
}

// NewFlowText creates a flow of text and images onto pages of the size, with
// margins of 72 points, an inch, on every side.
func NewFlowText(pdf *PDF, pageSize pagesize.PageSize) *FlowText {
	return &FlowText{pdf: pdf, pageSize: pageSize, top: 72, right: 72, bottom: 72, left: 72}
}

// SetMargins sets the margins of every side of the pages, in points. The text
// and the images are as wide as the page less the left and right margins.
func (flow *FlowText) SetMargins(margin float32) *FlowText {
	return flow.SetEachMargin(margin, margin, margin, margin)
}

// SetEachMargin sets the margins of the pages, in points: top, right, bottom
// and left, the order CSS gives them.
func (flow *FlowText) SetEachMargin(top, right, bottom, left float32) *FlowText {
	flow.top = top
	flow.right = right
	flow.bottom = bottom
	flow.left = left
	return flow
}

// SetFirstPageTop sets where the flow starts on the first page that
// DrawOnPagesFrom is given, below what is drawn there already, such as a
// heading or a letterhead. Without it the flow starts at the top margin.
func (flow *FlowText) SetFirstPageTop(y float32) *FlowText {
	flow.firstPageTop = y
	return flow
}

// Add adds a paragraph: a heading, when its structure type is structelem.H1 to
// structelem.H6, or a paragraph of text, a caption, a line.
func (flow *FlowText) Add(paragraph *Paragraph) *FlowText {
	flow.items = append(flow.items, flowItem{paragraphs: []*Paragraph{paragraph}})
	return flow
}

// AddParagraphs adds paragraphs with gap points between them, which flow
// across pages as the text of a TextFrame does.
func (flow *FlowText) AddParagraphs(paragraphs []*Paragraph, gap float32) *FlowText {
	flow.items = append(flow.items, flowItem{paragraphs: paragraphs, gap: gap, hasGap: true})
	return flow
}

// AddImage adds an image at the left margin. An image wider than the text is
// made as wide as the text, and one taller than the space between the top and
// the bottom margins as tall as that space, keeping its shape; it is never
// cut.
func (flow *FlowText) AddImage(image *Image) *FlowText {
	return flow.AddImageAligned(image, alignment.Left)
}

// AddImageAligned adds an image as AddImage does, at the left margin, in the
// center of the text, or at the right margin: alignment.Left, alignment.Center
// or alignment.Right.
func (flow *FlowText) AddImageAligned(image *Image, align alignment.Alignment) *FlowText {
	flow.items = append(flow.items, flowItem{image: image, align: align})
	return flow
}

// AddSpace adds points of space below what was added before. A space at the
// top of a page is left out, as the gap between two paragraphs is.
func (flow *FlowText) AddSpace(points float32) *FlowText {
	if points > 0 {
		flow.items = append(flow.items, flowItem{space: points})
	}
	return flow
}

// DrawOnPages draws what was added on as many new pages as it needs, created
// detached and added to the list; add them to the PDF afterwards. It returns
// the x and y coordinates of the bottom right corner of what was drawn on the
// last page, where something drawn after it can go.
func (flow *FlowText) DrawOnPages(pages *[]*Page) [2]float32 {
	return flow.DrawOnPagesFrom(nil, pages)
}

// DrawOnPagesFrom draws what was added as DrawOnPages does, starting on first,
// a page with other content above the flow, which SetFirstPageTop puts the
// flow under. The next pages are new and are added to the list; first is not.
func (flow *FlowText) DrawOnPagesFrom(first *Page, pages *[]*Page) [2]float32 {
	width := flow.pageSize.GetWidth() - flow.left - flow.right
	height := flow.pageSize.GetHeight() - flow.top - flow.bottom
	if !finite(flow.top, flow.right, flow.bottom, flow.left, flow.firstPageTop) || width <= 0 || height <= 0 {
		flow.pdf.fail("The margins of the flow must be numbers that leave room on the page for the text.")
		return [2]float32{flow.left, flow.top}
	}
	page := first
	y := flow.top
	atTop := true // Nothing drawn yet on this page: a space here is left out
	if first != nil && flow.firstPageTop > flow.top {
		y = flow.firstPageTop
		atTop = false
	}
	newPage := func() {
		page = NewPageDetached(flow.pdf, flow.pageSize)
		*pages = append(*pages, page)
		y = flow.top
		atTop = true
	}
	limit := flow.pageSize.GetHeight() - flow.bottom
	for i, item := range flow.items {
		switch {
		case item.space > 0:
			if page != nil && !atTop {
				y += item.space
				if y >= limit {
					newPage() // The space ends the page, and is not carried over
				}
			}
		case item.image != nil:
			fitImage(item.image, width, height)
			if page == nil || (!atTop && y+item.image.GetHeight() > limit) {
				newPage()
			}
			x := flow.left
			if item.align == alignment.Center {
				x += float32((width - item.image.GetWidth()) / 2)
			} else if item.align == alignment.Right {
				x += width - item.image.GetWidth()
			}
			item.image.SetLocation(x, y)
			item.image.DrawOn(page)
			y += item.image.GetHeight()
			atTop = false
		default:
			if firstLineHeight(item.paragraphs) == 0 {
				continue // No text to draw
			}
			needed := firstLineHeight(item.paragraphs)
			if isHeading(item.paragraphs[len(item.paragraphs)-1]) {
				// The heading, and the first line or the image after it
				textHeight := flow.textHeight(item, width)
				if textHeight+flow.nextHeight(i+1, width, height) <= height {
					needed = textHeight + flow.nextHeight(i+1, width, height)
				}
			}
			if page == nil || (!atTop && y+needed > limit) {
				newPage()
			}
			frame := flow.frame(item, width)
			frame.SetLocation(flow.left, y)
			frame.SetHeight(limit - y)
			frame.DrawOn(page)
			for frame.HasMoreText() {
				newPage()
				frame.SetLocation(flow.left, y)
				frame.SetHeight(limit - y)
				frame.DrawOn(page)
			}
			y = frame.GetTextBottom()
			atTop = false
		}
	}
	return [2]float32{flow.left + width, y}
}

// frame returns a text frame of the paragraphs of the item, as wide as the text.
func (flow *FlowText) frame(item flowItem, width float32) *TextFrame {
	frame := NewTextFrameFromParagraphs(item.paragraphs).SetWidth(width)
	if item.hasGap {
		frame.SetParagraphGap(item.gap)
	}
	return frame
}

// textHeight returns the height of the paragraphs of the item, all of them
// drawn in one frame.
func (flow *FlowText) textHeight(item flowItem, width float32) float32 {
	frame := flow.frame(item, width)
	frame.SetLocation(0, 0)
	frame.DrawOn(nil) // With no page the text is measured and kept
	return frame.GetTextBottom()
}

// nextHeight returns the height that has to fit below a heading so that it
// does not end a page: the spaces after it, and the first line of the
// paragraphs or the image that comes next.
func (flow *FlowText) nextHeight(index int, width, height float32) float32 {
	total := float32(0)
	for _, item := range flow.items[index:] {
		switch {
		case item.space > 0:
			total += item.space
		case item.image != nil:
			w, h := item.image.GetWidth(), item.image.GetHeight()
			if w > width {
				h = float32(h * (width / w))
			}
			if h > height {
				h = height
			}
			return total + h
		default:
			if line := firstLineHeight(item.paragraphs); line > 0 {
				return total + line
			}
		}
	}
	return total
}

// fitImage makes the image as wide as the text at most, and as tall as the
// space between the margins at most, keeping its shape.
func fitImage(image *Image, width, height float32) {
	if image.GetWidth() > width {
		image.ResizeWidth(width)
	}
	if image.GetHeight() > height {
		image.ResizeHeight(height)
	}
}

// firstLineHeight returns the height of the first line of the paragraphs, its
// ascent and descent, which a TextFrame places at its top; 0 when they have no
// text.
func firstLineHeight(paragraphs []*Paragraph) float32 {
	for _, paragraph := range paragraphs {
		if len(paragraph.lines) > 0 {
			line := paragraph.lines[0]
			return line.font.GetAscent(line.fontSize) + line.font.GetDescent(line.fontSize)
		}
	}
	return 0
}

// isHeading reports whether the paragraph is a heading, structelem.H1 to H6.
func isHeading(paragraph *Paragraph) bool {
	switch paragraph.structureType {
	case structelem.H1, structelem.H2, structelem.H3, structelem.H4, structelem.H5, structelem.H6:
		return true
	}
	return false
}
