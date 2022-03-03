package main

import (
	"bufio"
	"fmt"
	"io/ioutil"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src/pdfjet"
	"github.com/edragoev1/pdfjet/src/pdfjet/color"
	"github.com/edragoev1/pdfjet/src/pdfjet/compliance"
	"strings"
	"time"
)

// Example37 -- TODO:
func Example37() {
	file, err := os.Create("Example_37.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	buf, err := ioutil.ReadFile("data/testPDFs/wirth.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/wirth.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/Smalltalk-and-OO.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/InsideSmalltalk1.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/InsideSmalltalk2.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/Greenbook.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/Bluebook.pdf")
	// buf, err := ioutil.ReadFile("data/testPDFs/Orangebook.pdf")
	if err != nil {
		log.Fatal(err)
	}
	objects := pdf.Read(buf)

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
	Example37()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_37 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
