package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/letter"
	"strings"
	"time"
)

// Example18 draws donut chart or pie chart depending on R1.
func Example18() {
	f, err := os.Create("Example_18.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	w := bufio.NewWriter(f)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	page := pdfjet.NewPage(pdf, letter.Portrait, true)
	page.SetPenWidth(5.0)
	page.SetBrushColor(0x353638)

	donutChart := pdfjet.NewDonutChart(f1, f2)
	donutChart.SetLocation(200.0, 200.0)
	donutChart.SetR1AndR2(50.0, 100.0)
	donutChart.AddSector(30.0, color.Darkblue)
	donutChart.AddSector(90.0, color.Green)
	donutChart.AddSector(60.0, color.Indigo)
	donutChart.AddSector(120.0, color.Red)
	donutChart.AddSector(60.0, color.Goldenrod)
	donutChart.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example18()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_18 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
