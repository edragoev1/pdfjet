package main

import (
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
)

// Example51 splits an existing PDF document into one PDF for each of its
// pages, Example_51_1.pdf to Example_51_5.pdf, and writes all of its pages in
// reverse order to Example_51.pdf. The objects that Read returns are merged
// into every PDF. The pages keep their content, resources, annotations and
// links; a link to a page that is not in the same PDF leads nowhere.
func Example51() {
	buf, err := os.ReadFile("data/testPDFs/wirth.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdfjet.NewPDFReader().Read(buf)
	if err != nil {
		log.Fatal(err)
	}
	count := len(pdfjet.NewPDFReader().GetPageObjects(objects))

	for i := 1; i <= count; i++ {
		part, err := pdfjet.NewPDFFile(fmt.Sprintf("Example_51_%d.pdf", i))
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
	pdf, err := pdfjet.NewPDFFile("Example_51.pdf")
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
	Example51()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_51 => %d ms\n", time1-time0)
}
