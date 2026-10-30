// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

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

// Example49 merges and splits existing PDF documents. merge draws a cover page and
// merges three documents after it, into Example_49.pdf: the pages of each
// follow in their order and keep their content, resources, annotations and
// links, and what belongs to a whole document, such as its bookmarks, form
// fields and tagging, is left out. split writes one PDF for each page of a
// document, Example_49_1.pdf to Example_49_5.pdf, and all of its pages in
// reverse order to Example_49_reversed.pdf; a link to a page that is not in
// the same PDF leads nowhere.
func Example49() {
	merge()
	split()
}

func merge() {
	pdf, err := pdfjet.NewPDFFile("Example_49.pdf")
	if err != nil {
		log.Fatal(err)
	}

	fileNames := []string{
		"resources/data/testPDFs/scanned-linearized.pdf",
		"resources/data/testPDFs/sample-document.pdf",
		"resources/data/testPDFs/PDFjetLogo.pdf",
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

func split() {
	buf, err := os.ReadFile("resources/data/testPDFs/scanned-linearized.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdfjet.NewPDFReader().Read(buf)
	if err != nil {
		log.Fatal(err)
	}
	count := len(pdfjet.NewPDFReader().GetPageObjects(objects))

	for i := 1; i <= count; i++ {
		part, err := pdfjet.NewPDFFile(fmt.Sprintf("Example_49_%d.pdf", i))
		if err != nil {
			log.Fatal(err)
		}
		if err := part.MergePages(objects, i); err != nil {
			log.Fatal(err)
		}
		if err := part.Complete(); err != nil {
			log.Fatal(err)
		}
	}

	reversed := make([]int, count)
	for i := range reversed {
		reversed[i] = count - i
	}
	pdf, err := pdfjet.NewPDFFile("Example_49_reversed.pdf")
	if err != nil {
		log.Fatal(err)
	}
	if err := pdf.MergePages(objects, reversed...); err != nil {
		log.Fatal(err)
	}
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example49()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_49 => %4d ms\n", time1-time0)
}
