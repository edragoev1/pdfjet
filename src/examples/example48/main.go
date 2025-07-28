package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example48 -- TODO:
func Example48() {
	pdf := pdfjet.NewPDFFile("Example_48.pdf")
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSans/NotoSans-Regular.ttf.stream")

	page := pdfjet.NewPage(pdf, letter.Portrait)

	var toc = pdfjet.NewBookmark(pdf)
	x := float32(70.0)
	y := float32(50.0)
	offset := float32(50.0)

	y += 30.0
	title := pdfjet.NewTitle(f1, "This is a test!", x, y)
	toc.AddBookmark(page, title)
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "General", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "File Header", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "File Body", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Cross-Reference Table", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	y = 50.0
	title = pdfjet.NewTitle(f1, "File Trailer", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Incremental Updates", x, y).SetOffset(offset)
	bm := toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Hello", x, y).SetOffset(offset)
	bm = bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "World", x, y).SetOffset(offset)
	bm = bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Yahoo!!", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Test Test Test ...", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	bm = bm.GetParent()
	title = pdfjet.NewTitle(f1, "Let's see ...", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "One more item.", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "The End :)", x, y)
	toc.AddBookmark(page, title)
	title.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example48()
	pdfjet.PrintDuration("Example_48", time.Since(start))
}
