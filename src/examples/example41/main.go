package main

import (
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example41 merges existing PDF documents into one, after a cover page drawn
// with PDFjet. The pages of each document follow in their order and keep their
// content, resources, annotations and links. The parts of a document that
// belong to the whole document, such as its bookmarks, form fields and
// tagging, are left out.
func Example41() {
	pdf, err := pdfjet.NewPDFFile("Example_41.pdf")
	if err != nil {
		log.Fatal(err)
	}

	fileNames := []string{
		"data/testPDFs/wirth.pdf",
		"data/testPDFs/rc65-16e.pdf",
		"data/testPDFs/PDFjetLogo.pdf",
	}
	documents := make([][]*pdfjet.PDFobj, 0)
	for _, fileName := range fileNames {
		buf, err := os.ReadFile(fileName)
		if err != nil {
			log.Fatal(err)
		}
		objects, err := pdf.Read(buf)
		if err != nil {
			log.Fatal(err)
		}
		documents = append(documents, objects)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Bold)
	f1.SetSize(24.0)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())
	pdfjet.NewTextLine(f1, "Merged documents").SetLocation(50.0, 80.0).DrawOn(page)
	y := float32(130.0)
	for i, fileName := range fileNames {
		pages := len(pdf.GetPageObjects(documents[i]))
		text := fmt.Sprintf("%s, %d pages", fileName, pages)
		if pages == 1 {
			text = fmt.Sprintf("%s, %d page", fileName, pages)
		}
		pdfjet.NewTextLine(f2, text).SetLocation(50.0, y).DrawOn(page)
		y += 20.0
	}

	for _, objects := range documents {
		if err := pdf.Merge(objects); err != nil {
			log.Fatal(err)
		}
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example41()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_41 => %d ms\n", time1-time0)
}
