package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"pdfjet"
	"pdfjet/compliance"
	"pdfjet/corefont"
	"pdfjet/letter"
	"strings"
	"time"
)

// Example32 -- TODO:
func Example32() {
	x := float32(50.0)
	y := float32(50.0)
	leading := float32(14.0)

	file, err := os.Create("Example_32.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	font1 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	file2, err := os.Open("examples/Example_02.java")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()

	var page *pdfjet.Page
	scanner := bufio.NewScanner(file2)
	for scanner.Scan() {
		line := scanner.Text()
		if page == nil {
			y = 50.0
			page = newPage(pdf, font1, x, y, leading)
		}
		page.Println(line)
		y += leading
		if y > (letter.Portrait[1] - 20.0) {
			page.SetTextEnd()
			page = nil
		}
	}
	if err := scanner.Err(); err != nil {
		log.Fatal(err)
	}

	pdf.Complete()
}

func newPage(pdf *pdfjet.PDF, font *pdfjet.Font, x, y, leading float32) *pdfjet.Page {
	page := pdfjet.NewPage(pdf, letter.Portrait, true)
	page.SetTextStart()
	page.SetTextFont(font)
	page.SetTextLocation(x, y)
	page.SetTextLeading(leading)
	return page
}

func main() {
	start := time.Now()
	Example32()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_32 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
