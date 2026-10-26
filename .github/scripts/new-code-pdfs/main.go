// Writes two PDFs of the code new in 9.1.5 for the manual viewer pass, which
// no example draws: Kerning.pdf, the same lines of IBM Plex Sans kerned and
// not kerned, and Copied_Image.pdf, a PNG with transparency drawn on a colored
// box, then copied from a PDF that was read and drawn again, which must keep
// its transparency.
//
//	go run ./.github/scripts/new-code-pdfs OUTPUT_FOLDER
//
// Run it from the root of the repository, which has the fonts and images.

package main

import (
	"bufio"
	"bytes"
	"fmt"
	"log"
	"os"
	"path/filepath"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func complete(pdf *pdfjet.PDF) {
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func text(page *pdfjet.Page, font *pdfjet.Font, x, y float32, s string) {
	line := pdfjet.NewTextLine(font, s)
	line.SetLocation(x, y)
	line.DrawOn(page)
}

func kerning(path string) {
	pdf, err := pdfjet.NewPDFFile(path)
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetTitle("Kerning")
	label := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	label.SetSize(11.0)
	plain := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	plain.SetSize(36.0)
	kerned := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	kerned.SetSize(36.0)
	kerned.SetKernPairs(true)

	page := pdfjet.NewPage(pdf, letter.Portrait())
	text(page, label, 72.0, 72.0, "Each pair of lines: not kerned, then kerned. The kerned line is a little")
	text(page, label, 72.0, 88.0, "shorter, its letters closer where the font asks, as in AV, To and Wa.")
	y := float32(150.0)
	for _, s := range []string{"AVATAR To Wally", "WAVE Yes, Type", "LTA, P. Vogel"} {
		text(page, label, 72.0, y-40.0, "Not kerned")
		text(page, plain, 72.0, y, s)
		text(page, label, 72.0, y+24.0, "Kerned")
		text(page, kerned, 72.0, y+64.0, s)
		y += 150.0
	}
	complete(pdf)
}

func copiedImage(path string) {
	// The PNG drawn into a PDF in memory, which is read back
	buf := new(bytes.Buffer)
	w := bufio.NewWriter(buf)
	first := pdfjet.NewPDF(w)
	png, err := os.ReadFile("images/linux-logo.png")
	if err != nil {
		log.Fatal(err)
	}
	pdfjet.NewImage(first, bytes.NewReader(png)).DrawOn(pdfjet.NewPage(first, letter.Portrait()))
	complete(first)
	w.Flush()
	objects, err := pdfjet.NewPDF(bufio.NewWriter(new(bytes.Buffer))).Read(buf.Bytes())
	if err != nil {
		log.Fatal(err)
	}
	var read *pdfjet.PDFobj
	for _, obj := range objects {
		if obj.GetValue("/Subtype") == "/Image" && obj.GetValue("/SMask") != "" {
			read = obj
		}
	}
	if read == nil {
		log.Fatal("no image with a soft mask in the PDF read")
	}

	pdf, err := pdfjet.NewPDFFile(path)
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetTitle("An image copied from a PDF")
	label := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	label.SetSize(11.0)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	text(page, label, 72.0, 72.0, "Left: the PNG drawn. Right: the same image, copied from a PDF that was read.")
	text(page, label, 72.0, 88.0, "Both must show the orange box around the penguin, with no white box behind it.")
	for i, image := range []*pdfjet.Image{
		pdfjet.NewImage(pdf, bytes.NewReader(png)),
		pdfjet.NewImageFromPDFobj(pdf, read),
	} {
		x := 72.0 + float32(i)*240.0
		box := pdfjet.NewRect(x, 120.0, 220.0, 225.0)
		box.SetFillColor(color.Orange)
		box.SetBorderColor(color.Orange)
		box.DrawOn(page)
		image.ScaleBy(0.5)
		image.SetLocation(x+13.5, 124.0)
		image.DrawOn(page)
	}
	complete(pdf)
}

func main() {
	if len(os.Args) != 2 {
		fmt.Println("Usage: go run ./.github/scripts/new-code-pdfs OUTPUT_FOLDER")
		os.Exit(1)
	}
	out := os.Args[1]
	if err := os.MkdirAll(out, 0o755); err != nil {
		log.Fatal(err)
	}
	kerning(filepath.Join(out, "Kerning.pdf"))
	copiedImage(filepath.Join(out, "Copied_Image.pdf"))
}
