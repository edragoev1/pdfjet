// The text document of section 3 of pdfjet-benchmarks.html, written by PDFjet for Go:
// pages of 60 lines of 10 point Latin, Greek and Cyrillic text in IBM Plex
// Sans, one drawing call per line.
//
// Usage: portbench bench|cold <pages>
//        portbench sample <pages> <file>
//
// Run from the root of the repository, as the font path is relative to it.
package main

import (
	"bufio"
	"bytes"
	"fmt"
	"log"
	"os"
	"sort"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

const font = "fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"
const lines = 60

var samples = [3]string{
	"The quick brown fox jumps over the lazy dog",
	"Ξεσκεπάζω την ψυχοφθόρα βδελυγμία",
	"Съешь же ещё этих мягких французских булок",
}

func document(pages int) []byte {
	var buf bytes.Buffer
	pdf := pdfjet.NewPDF(bufio.NewWriter(&buf))
	f := pdfjet.NewFontFromFile(pdf, font)
	for p := 0; p < pages; p++ {
		page := pdfjet.NewPage(pdf, letter.Portrait())
		for l := 0; l < lines; l++ {
			page.DrawString(f, nil, 10.0,
				samples[l%3]+" "+strconv.Itoa(p)+"."+strconv.Itoa(l),
				50.0, 50.0+float32(l)*12.0)
		}
	}
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
	return buf.Bytes()
}

func main() {
	start := time.Now()
	mode := os.Args[1]
	pages, err := strconv.Atoi(os.Args[2])
	if err != nil {
		log.Fatal(err)
	}
	if mode == "cold" {
		pdf := document(pages)
		fmt.Printf("go cold %d pages: %d ms, %d bytes\n",
			pages, time.Since(start).Milliseconds(), len(pdf))
		return
	}
	if mode == "sample" {
		if err := os.WriteFile(os.Args[3], document(pages), 0644); err != nil {
			log.Fatal(err)
		}
		return
	}
	for i := 0; i < 2; i++ {
		document(pages)
	}
	ms := make([]int, 7)
	size := 0
	for i := range ms {
		t := time.Now()
		size = len(document(pages))
		ms[i] = int(time.Since(t).Milliseconds())
	}
	sort.Ints(ms)
	fmt.Printf("go %d pages: median %d ms (min %d, max %d, %d runs), %d bytes\n",
		pages, ms[len(ms)/2], ms[0], ms[len(ms)-1], len(ms), size)
}
