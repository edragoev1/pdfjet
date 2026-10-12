// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// The Go port's side of the cross-port check, tests/crossport/check-crossport.sh.
// It draws random shapes, the ones whose paths are made of floating point
// arithmetic, with the same random numbers as the other ports, and writes the
// content of each page, which every port must write the same, byte for byte:
//
//	go run ./tests/crossport/go SEED CASES OUT
//
// The float paths of DonutChart and Arc were found apart by a run like it
// against Go (review of 11 October 2026, #25).
package main

import (
	"bufio"
	"fmt"
	"io"
	"os"
	"strconv"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// random is SplitMix64, which every port computes the same.
type random struct{ state uint64 }

func (r *random) next() uint64 {
	r.state += 0x9E3779B97F4A7C15
	z := r.state
	z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9
	z = (z ^ (z >> 27)) * 0x94D049BB133111EB
	return z ^ (z >> 31)
}

// between returns a number from lo up to hi, of 24 bits, which a float32 holds
// exactly.
func (r *random) between(lo, hi float32) float32 {
	unit := float32(r.next()>>40) / 16777216
	return lo + float32(unit*(hi-lo))
}

// below returns a whole number from 0 up to n.
func (r *random) below(n int) int {
	return int(r.next() % uint64(n))
}

func (r *random) color() int32 {
	return int32(r.below(0x1000000))
}

func main() {
	seed, _ := strconv.ParseUint(os.Args[1], 10, 64)
	cases, _ := strconv.Atoi(os.Args[2])
	out, err := os.Create(os.Args[3])
	if err != nil {
		panic(err)
	}
	w := bufio.NewWriter(out)
	for i := 0; i < cases; i++ {
		fmt.Fprintf(w, "case %d\n", i)
		w.Write(drawCase(&random{state: seed + uint64(i)}))
	}
	w.Flush()
	out.Close()
}

// styleArc gives the arc a stroke width, a dash pattern or none, a fill color
// or none, and a rotation.
func styleArc(r *random, arc *pdfjet.Arc) {
	arc.SetStrokeWidth(r.between(0, 10))
	if r.below(2) == 0 {
		arc.SetStrokeDashPattern("[" + strconv.Itoa(1+r.below(9)) + " " + strconv.Itoa(1+r.below(9)) + "] 0")
	}
	if r.below(2) == 0 {
		arc.SetFillColor(r.color())
	}
	arc.SetRotation(r.between(-360, 360))
}

// drawCase draws the shapes of one case on a page of a document of its own,
// and returns the content of the page, or "error" when the document fails.
func drawCase(r *random) []byte {
	pdf := pdfjet.NewPDF(bufio.NewWriter(io.Discard))
	font := pdfjet.NewCoreFont(pdf, corefont.Helvetica())
	page := pdfjet.NewPage(pdf, letter.Portrait())
	shapes := 1 + r.below(4)
	for k := 0; k < shapes; k++ {
		switch r.below(5) {
		case 0:
			arc := pdfjet.NewArc()
			arc.SetLocation(r.between(0, 612), r.between(0, 792))
			arc.SetRadiusX(r.between(0, 300))
			arc.SetRadiusY(r.between(0, 300))
			arc.SetStartAngle(r.between(-720, 720))
			arc.SetSweep(r.between(-720, 720))
			styleArc(r, arc)
			arc.DrawOn(page)
		case 1:
			// The whole of an ellipse, which a random sweep never is
			ellipse := pdfjet.NewEllipse()
			ellipse.SetLocation(r.between(0, 612), r.between(0, 792))
			ellipse.SetRadiusX(r.between(0, 300))
			ellipse.SetRadiusY(r.between(0, 300))
			styleArc(r, &ellipse.Arc)
			ellipse.DrawOn(page)
		case 2:
			chart := pdfjet.NewDonutChart(font, font)
			chart.SetLocation(r.between(0, 612), r.between(0, 792))
			outer := r.between(1, 200)
			chart.SetRadii(outer, r.between(0, outer))
			slices := 1 + r.below(6)
			for s := 0; s < slices; s++ {
				chart.AddSlice(pdfjet.NewSlice(r.between(0, 100), r.color(), "s"+strconv.Itoa(s)))
			}
			chart.DrawOn(page)
		case 3:
			path := pdfjet.NewPath()
			points := 2 + r.below(6)
			for p := 0; p < points; p++ {
				x, y := r.between(0, 612), r.between(0, 792)
				if p < points-1 && r.below(3) == 0 {
					path.Add(pdfjet.NewControlPointC(x, y))
				} else {
					path.Add(pdfjet.NewPoint(x, y))
				}
			}
			path.SetClosed(r.below(2) == 0)
			path.SetFillShape(r.below(2) == 0)
			path.SetStrokeWidth(r.between(0, 5))
			path.DrawOn(page)
		default:
			line := pdfjet.NewTextLine(font, "Text "+strconv.Itoa(k))
			line.SetFontSize(r.between(4, 40))
			line.SetLocation(r.between(0, 612), r.between(0, 792))
			line.SetTextRotation(r.below(361) - 180)
			line.DrawOn(page)
		}
	}
	content := append([]byte(nil), page.GetContent()...)
	if err := pdf.Complete(); err != nil {
		return []byte("error\n")
	}
	return append(content, '\n')
}
