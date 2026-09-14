package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"strings"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/corefont"
)

// Example50 fills in the fields of an existing PDF form: it reads the PDF, adds an image,
// two fonts read from files and the core font Helvetica as resources of a page,
// and writes the text on that page.
//
// The core font is added with AddCoreFontResource(font, objects), which writes a
// font dictionary that names the font and nothing else. The advantage is that
// the page grows by a few hundred bytes and needs no font file, which suits a
// stamp or a field value added to a document that already exists. The
// disadvantages are those of every font that is not embedded: the viewer draws
// the text with its own Helvetica, only the WinAnsi characters can be drawn,
// and the document cannot claim PDF/A or PDF/UA compliance. The two embedded
// fonts of this example show the alternative.
func Example50(fileName string) {
	pdf := pdfjet.NewPDFFile("Example_50.pdf")

	buf, err := os.ReadFile(fileName)
	if err != nil {
		log.Fatal(err)
	}
	objects, err := pdf.Read(buf)
	if err != nil {
		log.Fatal(err)
	}

	file1, err := os.Open("images/qrcode.png")
	if err != nil {
		log.Fatal(err)
	}
	defer file1.Close()
	reader := bufio.NewReader(file1)
	image1 := pdfjet.NewImageForObjects(&objects, reader)
	image1.SetLocation(495.0, 65.0)
	image1.ScaleBy(0.40)

	file2, err := os.Open(IBMPlexSans.Regular)
	if err != nil {
		log.Fatal(err)
	}
	defer file2.Close()
	reader = bufio.NewReader(file2)
	font1 := pdfjet.NewFontStream2(&objects, reader)
	font1.SetSize(12.0)

	file3, err := os.Open(IBMPlexSans.Bold)
	if err != nil {
		log.Fatal(err)
	}
	defer file3.Close()
	reader = bufio.NewReader(file3)
	font2 := pdfjet.NewFontStream2(&objects, reader)
	font2.SetSize(12.0)

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
	page.DrawString(font2, nil, font2.GetSize(), "Иван", x, y)

	// Last Name
	page.DrawString(font3, nil, font3.GetSize(), "Jones", x+258.0, y)

	// Social Insurance Number
	page.DrawStringUsingSpacing(font1, font1.GetSize(), stripSpacesAndDashes("243-590-129"), x+437.0, y, dx)

	// Last Name at Birth
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "Culverton", x, y)

	// Mailing Address
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "10 Elm Street", x, y)

	// City
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "Toronto", x, y)

	// Province or Territory
	page.DrawString(font1, nil, font1.GetSize(), "Ontario", x+365.0, y)

	// Postal Code
	page.DrawStringUsingSpacing(font1, font1.GetSize(), stripSpacesAndDashes("L7B 2E9"), x+482.0, y, dx)

	// Home Address
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "10 Oak Road", x, y)

	// City
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "Toronto", x, y)

	// Previous Province or Territory
	page.DrawString(font1, nil, font1.GetSize(), "Ontario", x+365.0, y)

	// Postal Code
	page.DrawStringUsingSpacing(font1, font1.GetSize(), stripSpacesAndDashes("L7B 2E9"), x+482.0, y, dx)

	// Home telephone number
	page.DrawString(font1, nil, font1.GetSize(), "905-222-3333", x, y+dy)
	// Work telephone number
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "416-567-9903", x+279.0, y)

	// Previous province or territory
	y += dy
	page.DrawString(font1, nil, font1.GetSize(), "British Columbia", x+452.0, y)

	// Move date from previous province or territory
	y += dy
	page.DrawStringUsingSpacing(font1, font1.GetSize(), stripSpacesAndDashes("2016-04-12"), x+452.0, y, dx)

	// Date new marital status began
	page.DrawStringUsingSpacing(font1, font1.GetSize(), stripSpacesAndDashes("2014-11-02"), x+452.0, 467.0, dx)

	// First name of spouse
	y = 521.0
	page.DrawString(font1, nil, font1.GetSize(), "Melanie", x, y)
	// Last name of spouse
	page.DrawString(font1, nil, font1.GetSize(), "Jones", x+258.0, y)

	// Social Insurance number of spouse
	page.DrawStringUsingSpacing(font1, font1.GetSize(), stripSpacesAndDashes("192-760-427"), x+437.0, y, dx)

	// Spouse or common-law partner's address
	page.DrawString(font1, nil, font1.GetSize(), "12 Smithfield Drive", x, 554.0)

	// Signature Date
	page.DrawString(font1, nil, font1.GetSize(), "2016-08-07", x+475.0, 615.0)

	// Signature Date of spouse
	page.DrawString(font1, nil, font1.GetSize(), "2016-08-07", x+475.0, 651.0)

	// Female Checkbox 1
	// pdfjet.XMarkCheckBox(page, 477.5, 197.5, 7.0)

	// Male Checkbox 1
	pdfjet.XMarkCheckBox(page, 534.5, 197.5, 7.0)

	// Married
	pdfjet.XMarkCheckBox(page, 34.5, 424.0, 7.0)

	// Living common-law
	// pdfjet.XMarkCheckBox(page, 121.5, 424.0, 7.0)

	// Widowed
	// pdfjet.XMarkCheckBox(page, 235.5, 424.0, 7.0)

	// Divorced
	// pdfjet.XMarkCheckBox(page, 325.5, 424.0, 7.0)

	// Separated
	// pdfjet.XMarkCheckBox(page, 415.5, 424.0, 7.0)

	// Single
	// pdfjet.XMarkCheckBox(page, 505.5, 424.0, 7.0)

	// Female Checkbox 2
	pdfjet.XMarkCheckBox(page, 478.5, 536.5, 7.0)

	// Male Checkbox 2
	// pdfjet.XMarkCheckBox(page, 535.5, 536.5, 7.0)
	page.Complete(&objects)
	pdf.AddObjects(&objects)

	pdf.Complete()
}

func stripSpacesAndDashes(str string) string {
	var buf strings.Builder
	for _, ch := range str {
		if ch != ' ' && ch != '-' {
			buf.WriteRune(ch)
		}
	}
	return buf.String()
}

func main() {
	time0 := time.Now().UnixMilli()
	Example50("data/testPDFs/rc65-16e.pdf")
	// Example50("data/testPDFs/NoPredictor.pdf")
	// Example50("../../eBooks/UniversityPhysicsVolume1.pdf")
	// Example50("../specifications/ISO_32000-2_2017(en).PDF")
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_50 => %d ms\n", time1-time0)
}
