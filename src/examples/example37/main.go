package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/contents"
)

// Example37 -- TODO:
func Example37(fileName string) {
	pdf := pdfjet.NewPDFFile("Example_37.pdf", compliance.PDF15)
	objects := pdf.Read(contents.OfBinaryFile(fileName))
	file1, err := os.Open("fonts/OpenSans/OpenSans-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	reader := bufio.NewReader(file1)
	font1 := pdfjet.NewFontStream2(&objects, reader)
	font1.SetSize(72.0)

	text := pdfjet.NewTextLine(font1, "This is a test!")
	text.SetLocation(50.0, 350.0)
	text.SetColor(color.Peru)

	pages := pdf.GetPageObjects(objects)
	for _, pageObj := range pages {
		gs := pdfjet.NewGraphicsState()
		gs.SetAlphaStroking(0.75)    // Stroking alpha
		gs.SetAlphaNonStroking(0.75) // Nonstroking alpha
		pageObj.SetGraphicsState(gs, &objects)

		page := pdfjet.NewPageFromObject(pdf, pageObj)
		page.AddFontResource(font1, &objects)
		page.SetBrushColor(color.Blue)
		page.DrawString(font1, nil, "Hello, World!", 50.0, 200.0)
		text.DrawOn(page)

		page.Complete(&objects) // The graphics stack is unwinded automatically
	}
	pdf.AddObjects(&objects)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example37("data/testPDFs/wirth.pdf")
	// Example37("../../eBooks/UniversityPhysicsVolume1.pdf")
	// Example37("../../eBooks/Smalltalk-and-OO.pdf")
	// Example37("../../eBooks/InsideSmalltalk1.pdf")
	// Example37("../../eBooks/InsideSmalltalk2.pdf")
	// Example37("../../eBooks/Greenbook.pdf")
	// Example37("../../eBooks/Bluebook.pdf")
	// Example37("../../eBooks/Orangebook.pdf")
	elapsed := time.Since(start)
	fmt.Printf("Example_37 => %dµs\n", elapsed.Microseconds())
}
