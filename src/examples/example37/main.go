package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/content"
)

// Example37 opens an existing PDF, adds a font resource and writes on every page.
func Example37(fileName string) {
	pdf, err := pdfjet.NewPDFFile("Example_37.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdf.Read(content.OfBinaryFile(fileName))
	if err != nil {
		log.Fatal(err)
	}
	file1, err := os.Open(IBMPlexSans.Regular)
	if err != nil {
		log.Fatal(err)
	}
	reader := bufio.NewReader(file1)
	font1 := pdfjet.NewFontStream2(&objects, reader)
	font1.SetSize(72.0)

	text := pdfjet.NewTextLine(font1, "This is a test!")
	text.SetLocation(150.0, 350.0)
	text.SetTextColor(color.Peru)

	pages := pdf.GetPageObjects(objects)
	for _, pageObj := range pages {
		gs := pdfjet.NewGraphicsState()
		gs.SetAlphaStroking(0.75)    // Stroking alpha
		gs.SetAlphaNonStroking(0.75) // Non-stroking alpha
		pageObj.SetGraphicsState(gs, &objects)

		page := pdfjet.NewPageFromObject(pdf, pageObj)
		page.AddFontResource(font1, &objects)
		page.SetBrushColor(color.Blue)
		// page.DrawString(font1, nil, font1.GetSize(), "Hello, World!", 50.0, 200.0)
		text.DrawOn(page)

		page.Complete(&objects) // The graphics stack is unwinded automatically
	}
	if err := pdf.AddObjects(objects); err != nil {
		log.Fatal(err)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example37("data/testPDFs/wirth.pdf")
	// Example37("../../eBooks/UniversityPhysicsVolume1.pdf")
	// Example37("../../eBooks/Smalltalk-and-OO.pdf")
	// Example37("../../eBooks/InsideSmalltalk1.pdf")
	// Example37("../../eBooks/InsideSmalltalk2.pdf")
	// Example37("../../eBooks/Greenbook.pdf")
	// Example37("../../eBooks/Bluebook.pdf")
	// Example37("../../eBooks/Orangebook.pdf")
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_37 => %4d ms\n", time1-time0)
}
