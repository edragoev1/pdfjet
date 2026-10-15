// The first PDF of a process, by phase, as CommonBench's FontCold does in
// Java: new PDF, the first font, a second one, a page drawn and complete().
package main

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func ms(d time.Duration) float64 { return float64(d.Microseconds()) / 1000 }

func main() {
	root := os.Args[1] + "/"
	t0 := time.Now()
	pdf := pdfjet.NewPDF(bufio.NewWriter(io.Discard))
	t1 := time.Now()
	regular := pdfjet.NewFontFromFile(pdf, root+"fonts/IBMPlexSans/IBMPlexSans-Regular.ttf")
	t2 := time.Now()
	bold := pdfjet.NewFontFromFile(pdf, root+"fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf")
	t3 := time.Now()
	page := pdfjet.NewPage(pdf, letter.Portrait())
	for i := 0; i < 40; i++ {
		f := regular
		if i%5 == 0 {
			f = bold
		}
		line := pdfjet.NewTextLine(f, fmt.Sprintf("Line %d of the invoice, with some words on it", i))
		line.SetLocation(50, float32(60+15*i))
		line.DrawOn(page)
	}
	t4 := time.Now()
	if err := pdf.Complete(); err != nil {
		panic(err)
	}
	t5 := time.Now()
	fmt.Printf("new PDF %.1f, first font %.1f, second %.1f, page %.1f, complete %.1f, total %.1f ms\n",
		ms(t1.Sub(t0)), ms(t2.Sub(t1)), ms(t3.Sub(t2)), ms(t4.Sub(t3)), ms(t5.Sub(t4)), ms(t5.Sub(t0)))
}
