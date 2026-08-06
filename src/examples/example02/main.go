package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example02 demonstrates creating a PDF document with Japanese and Korean text
// using their respective fonts. The example shows how to:
// 1. Initialize a PDF document
// 2. Load and configure Asian fonts
// 3. Add content from text files
// 4. Position text blocks on the page
func Example02() {
	// Initialize new PDF document that will be saved as Example_02.pdf
	pdf := pdfjet.NewPDFFile("Example_02.pdf")

	// Load Japanese font from file and set its size to 12 points
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansJP/NotoSansJP-Regular.ttf.stream")
	f1.SetSize(14.0)

	// Load Korean font from file and set its size to 12 points
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansKR/NotoSansKR-Regular.ttf.stream")
	f2.SetSize(14.0)

	f3 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansSC/NotoSansSC-Regular.ttf.stream")
	f3.SetSize(14.0)

	f4 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansTC/NotoSansTC-Regular.ttf.stream")
	f4.SetSize(14.0)

	// Create a new page in portrait Letter size
	page := pdfjet.NewPage(pdf, letter.Portrait)

	// Create and draw Japanese text block:
	// - Content loaded from japanese.txt file
	// - Positioned at (50, 50) coordinates
	// - Set to 415 units wide (height auto-adjusts)
	// - Border explicitly disabled
	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/japanese.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(415.0)
	_ = textBlock.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	// Create and draw Korean text block:
	// - Content loaded from korean.txt file
	// - Positioned at (50, 450) coordinates
	// - Same width as Japanese block for consistency
	// - Border explicitly disabled
	textBlock = pdfjet.NewTextBlock(f2, content.OfTextFile("data/languages/korean.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(415.0)
	_ = textBlock.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	textBlock = pdfjet.NewTextBlock(f3, content.OfTextFile("data/languages/simplified-chinese.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(415.0)
	_ = textBlock.DrawOn(page)

	page = pdfjet.NewPage(pdf, letter.Portrait)

	textBlock = pdfjet.NewTextBlock(f4, content.OfTextFile("data/languages/traditional-chinese.txt"))
	textBlock.SetLocation(50.0, 50.0)
	textBlock.SetWidth(415.0)
	_ = textBlock.DrawOn(page)

	// Finalize the PDF document
	pdf.Complete()
}

func main() {
	// Measure and print execution time
	start := time.Now()
	Example02()
	pdfjet.PrintDuration("Example_02", time.Since(start))
}
