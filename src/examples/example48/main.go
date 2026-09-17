// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example48 draws the outline of a short guide to the structure of a PDF
// file, and adds a bookmark for each of its titles. The numbers of the titles
// come from their place in the tree of bookmarks.
func Example48() {
	pdf, err := pdfjet.NewPDFFile("Example_48.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("The structure of a PDF file")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(14.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(20.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	var toc = pdfjet.NewBookmark(pdf)
	x := float32(70.0)
	y := float32(80.0)
	offset := float32(50.0)

	// A bookmark without a number.
	title := pdfjet.NewTitle(f2, "The structure of a PDF file", x, y)
	toc.AddBookmark(page, title)
	title.DrawOn(page)

	y += 50.0
	title = pdfjet.NewTitle(f1, "File header", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "File body", x, y).SetOffset(offset)
	body := toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	// Bookmarks nested in "File body".
	y += 30.0
	title = pdfjet.NewTitle(f1, "Objects", x, y).SetOffset(offset)
	body.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Streams", x, y).SetOffset(offset)
	body.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Cross-reference table", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "File trailer", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait())

	y = 80.0
	title = pdfjet.NewTitle(f1, "Incremental updates", x, y).SetOffset(offset)
	bm := toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "New and changed objects", x, y).SetOffset(offset)
	bm = bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	// Two levels down.
	y += 30.0
	title = pdfjet.NewTitle(f1, "Changed objects keep their numbers", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Deleted objects are marked as free", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	// Back up one level.
	y += 30.0
	bm = bm.GetParent()
	title = pdfjet.NewTitle(f1, "A new cross-reference section", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "A new trailer", x, y).SetOffset(offset)
	bm.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 30.0
	title = pdfjet.NewTitle(f1, "Linearized files", x, y).SetOffset(offset)
	toc.AddBookmark(page, title).AutoNumber(title.GetPrefix())
	title.DrawOn(page)

	y += 50.0
	title = pdfjet.NewTitle(f2, "Summary", x, y)
	toc.AddBookmark(page, title)
	title.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example48()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_48 => %4d ms\n", time1-time0)
}
