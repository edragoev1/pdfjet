package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/corefont"
	"strings"
	"time"
)

// Example42 -- TODO:
func Example42() {
	file, err := os.Create("Example_42.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	f1 := pdfjet.NewCoreFont(pdf, corefont.HelveticaBold())
	f2 := pdfjet.NewCoreFont(pdf, corefont.Helvetica())

	f1.SetSize(10.0)
	f2.SetSize(8.0)

	page := pdfjet.NewPage(pdf, a4.Portrait, true)

	var width float32 = 500.0
	var height float32 = 13.0

	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(0.0, []string{"Company", "Smart Widgets Construction Inc."}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"Street Number", "120"}, false))
	fields = append(fields, pdfjet.NewField(width/8, []string{"Street Name", "Oak"}, false))
	fields = append(fields, pdfjet.NewField(5*width/8, []string{"Street Type", "Street"}, false))
	fields = append(fields, pdfjet.NewField(6*width/8, []string{"Direction", "West"}, false))
	fields = append(fields, pdfjet.NewField(7*width/8, []string{"Suite/Floor/Apt.", "8W"}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"City/Town", "Toronto"}, false))
	fields = append(fields, pdfjet.NewField(width/2, []string{"Province", "Ontario"}, false))
	fields = append(fields, pdfjet.NewField(7*width/8, []string{"Postal Code", "M5M 2N2"}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"Telephone Number", "(416) 331-2245"}, false))
	fields = append(fields, pdfjet.NewField(width/4, []string{"Fax (if applicable)", "(416) 124-9879"}, false))
	fields = append(fields, pdfjet.NewField(width/2, []string{"Email", "jsmith12345@gmail.ca"}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"Other Information", "", ""}, false))

	form := pdfjet.NewForm(fields)
	form.SetLabelFont(f1)
	form.SetLabelFontSize(8.0)
	form.SetValueFont(f2)
	form.SetValueFontSize(10.0)
	form.SetLocation(70.0, 90.0)
	form.SetRowWidth(width)
	form.SetRowHeight(height)
	form.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example42()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_42 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
