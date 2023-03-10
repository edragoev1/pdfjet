package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"github.com/edragoev1/pdfjet/src/imagetype"
)

// Example50 shows how to fill in an existing PDF form.
func Example50(fileName string) {
	file, err := os.Create("Example_50.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	buf, err := os.ReadFile("data/testPDFs/" + fileName)
	if err != nil {
		log.Fatal(err)
	}

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	objects := pdf.Read(buf)

	file1, err := os.Open("fonts/Droid/DroidSans.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer file1.Close()
	reader := bufio.NewReader(file1)
	font1 := pdfjet.NewFontStream2(&objects, reader)
	font1.SetSize(12.0)

	file2, err := os.Open("fonts/Droid/DroidSans-Bold.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader = bufio.NewReader(file2)
	font2 := pdfjet.NewFontStream2(&objects, reader)
	font2.SetSize(12.0)

	file3, err := os.Open("images/qrcode.png")
	if err != nil {
		log.Fatal(err)
	}
	defer file3.Close()
	reader = bufio.NewReader(file3)
	image1 := pdfjet.NewImage2(&objects, reader, imagetype.PNG)
	image1.SetLocation(495.0, 65.0)
	image1.ScaleBy(0.40)

	pages := pdf.GetPageObjects(objects)
	page := pdfjet.NewPageFromObject(pdf, pages[0])
	// page.InvertYAxis()

	page.AddImageResource(image1, &objects)
	page.AddFontResource(font1, &objects)
	page.AddFontResource(font2, &objects)
	font3 := page.AddCoreFontResource(corefont.Helvetica(), &objects)
	font3.SetSize(12.0)

	image1.DrawOn(page)

	x := float32(23.0)
	y := float32(185.0)
	dx := float32(15.0)
	dy := float32(24.0)

	page.SetBrushColor(color.Blue)

	// First Name and Initial
	page.DrawString(font2, nil, "Иван", x, y)

	// Last Name
	page.DrawString(font3, nil, "Jones", x+258.0, y)

	// Social Insurance Number
	page.DrawArrayOfCharacters(font1, stripSpacesAndDashes("243-590-129"), x+437.0, y, dx)

	// Last Name at Birth
	y += dy
	page.DrawString(font1, nil, "Culverton", x, y)

	// Mailing Address
	y += dy
	page.DrawString(font1, nil, "10 Elm Street", x, y)

	// City
	y += dy
	page.DrawString(font1, nil, "Toronto", x, y)

	// Province or Territory
	page.DrawString(font1, nil, "Ontario", x+365.0, y)

	// Postal Code
	page.DrawArrayOfCharacters(font1, stripSpacesAndDashes("L7B 2E9"), x+482.0, y, dx)

	// Home Address
	y += dy
	page.DrawString(font1, nil, "10 Oak Road", x, y)

	// City
	y += dy
	page.DrawString(font1, nil, "Toronto", x, y)

	// Previous Province or Territory
	page.DrawString(font1, nil, "Ontario", x+365.0, y)

	// Postal Code
	page.DrawArrayOfCharacters(font1, stripSpacesAndDashes("L7B 2E9"), x+482.0, y, dx)

	// Home telephone number
	page.DrawString(font1, nil, "905-222-3333", x, y+dy)
	// Work telephone number
	y += dy
	page.DrawString(font1, nil, "416-567-9903", x+279.0, y)

	// Previous province or territory
	y += dy
	page.DrawString(font1, nil, "British Columbia", x+452.0, y)

	// Move date from previous province or territory
	y += dy
	page.DrawArrayOfCharacters(font1, stripSpacesAndDashes("2016-04-12"), x+452.0, y, dx)

	// Date new marital status began
	page.DrawArrayOfCharacters(font1, stripSpacesAndDashes("2014-11-02"), x+452.0, 467.0, dx)

	// First name of spouse
	y = 521.0
	page.DrawString(font1, nil, "Melanie", x, y)
	// Last name of spouse
	page.DrawString(font1, nil, "Jones", x+258.0, y)

	// Social Insurance number of spouse
	page.DrawArrayOfCharacters(font1, stripSpacesAndDashes("192-760-427"), x+437.0, y, dx)

	// Spouse or common-law partner's address
	page.DrawString(font1, nil, "12 Smithfield Drive", x, 554.0)

	// Signature Date
	page.DrawString(font1, nil, "2016-08-07", x+475.0, 615.0)

	// Signature Date of spouse
	page.DrawString(font1, nil, "2016-08-07", x+475.0, 651.0)

	// Female Checkbox 1
	// xMarkCheckBox(page, 477.5, 197.5, 7.0)

	// Male Checkbox 1
	xMarkCheckBox(page, 534.5, 197.5, 7.0)

	// Married
	xMarkCheckBox(page, 34.5, 424.0, 7.0)

	// Living common-law
	// xMarkCheckBox(page, 121.5, 424.0, 7.0)

	// Widowed
	// xMarkCheckBox(page, 235.5, 424.0, 7.0)

	// Divorced
	// xMarkCheckBox(page, 325.5, 424.0, 7.0)

	// Separated
	// xMarkCheckBox(page, 415.5, 424.0, 7.0)

	// Single
	// xMarkCheckBox(page, 505.5, 424.0, 7.0)

	// Female Checkbox 2
	xMarkCheckBox(page, 478.5, 536.5, 7.0)

	// Male Checkbox 2
	// xMarkCheckBox(page, 535.5, 536.5, 7.0)

	page.Complete(&objects)

	pdf.AddObjects(&objects)

	pdf.Complete()
}

func xMarkCheckBox(page *pdfjet.Page, x, y, diagonal float32) {
	page.SetPenColor(color.Blue)
	page.SetPenWidth(diagonal / 5.0)
	page.MoveTo(x, y)
	page.LineTo(x+diagonal, y+diagonal)
	page.MoveTo(x, y+diagonal)
	page.LineTo(x+diagonal, y)
	page.StrokePath()
}

func stripSpacesAndDashes(str string) string {
	var buf strings.Builder
	runes := []rune(str)
	for _, ch := range runes {
		if ch != ' ' && ch != '-' {
			buf.WriteRune(ch)
		}
	}
	return buf.String()
}

func main() {
	start := time.Now()
	Example50("rc65-16e.pdf")
	// Example50("PDF32000_2008.pdf")
	// Example50("NoPredictor.pdf")
	elapsed := time.Since(start)
	fmt.Printf("Example_50 => %dµs\n", elapsed.Microseconds())
}
