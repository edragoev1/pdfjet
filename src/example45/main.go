package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"pdfjet"
	"pdfjet/a4"
	"pdfjet/color"
	"pdfjet/compliance"
	"pdfjet/imagetype"
	"strings"
	"time"
)

// Example45 -- TODO:
func Example45() {
	file, err := os.Create("Example_45.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF_UA)
    pdf.SetLanguage("en-US");
    pdf.SetTitle("Hello, World!");

	f, err := os.Open("fonts/Droid/DroidSerif-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader := bufio.NewReader(f)
	f1 := pdfjet.NewFontStream1(pdf, reader)

	f, err = os.Open("fonts/Droid/DroidSerif-Italic.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader = bufio.NewReader(f)
	f2 := pdfjet.NewFontStream1(pdf, reader)

	f1.SetSize(14.0)
	f2.SetSize(14.0)

	page := pdfjet.NewPage(pdf, a4.Portrait, true)

	uri := "http://pdfjet.com"

	text := pdfjet.NewTextLine(f1, "")
	text.SetLocation(70.0, 70.0)
	text.SetText("Hasta la vista!")
	text.SetLanguage("es-MX")
	text.SetStrikeout(true)
	text.SetUnderline(true)
	text.SetURIAction(&uri)
	text.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "")
	text.SetLocation(70.0, 90.0)
	text.SetText("416-335-7718")
	text.SetURIAction(&uri)
	text.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "")
	text.SetLocation(70.0, 120.0)
	text.SetText("2014-11-25")
	text.DrawOn(page)

	paragraphs := make([]*pdfjet.Paragraph, 0)

	paragraph := pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f1, "The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it. The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries."))
	paragraph.Add(pdfjet.NewTextLine(f2, "This text is blue color and is written using italic font.").SetColor(color.Blue))

	paragraphs = append(paragraphs, paragraph)

	textArea := pdfjet.NewText(paragraphs)
	textArea.SetLocation(70.0, 150.0)
	textArea.SetWidth(500.0)
	textArea.DrawOn(page)

	linesOfText := []string{
		"The Fibonacci sequence is named after Fibonacci.",
		"His 1202 book Liber Abaci introduced the sequence to Western European mathematics,",
		"although the sequence had been described earlier in Indian mathematics.",
		"By modern convention, the sequence begins either with F0 = 0 or with F1 = 1.",
		"The Liber Abaci began the sequence with F1 = 1, without an initial 0.",
		"",
		"Fibonacci numbers are closely related to Lucas numbers in that they are a complementary pair",
		"of Lucas sequences. They are intimately connected with the golden ratio;",
		"for example, the closest rational approximations to the ratio are 2/1, 3/2, 5/3, 8/5, ... .",
		"Applications include computer algorithms such as the Fibonacci search technique and the",
		"Fibonacci heap data structure, and graphs called Fibonacci cubes used for interconnecting",
		"parallel and distributed systems. They also appear in biological settings, such as branching",
		"in trees, phyllotaxis (the arrangement of leaves on a stem), the fruit sprouts of a pineapple,",
		"the flowering of an artichoke, an uncurling fern and the arrangement of a pine cone."}

	plainText := pdfjet.NewPlainText(f2, linesOfText)
	plainText.SetLocation(70.0, 370.0)
	plainText.SetWidth(520.0)
	plainText.SetFontSize(11.0)
	xy := plainText.DrawOn(page)

	box := pdfjet.NewBox()
	box.SetLocation(xy[0], xy[1])
	box.SetSize(20.0, 20.0)
	box.DrawOn(page)

	page = pdfjet.NewPage(pdf, a4.Portrait, true)

	text = pdfjet.NewTextLine(f1, "")
	text.SetLocation(70.0, 120.0)
	text.SetText("416-877-1395")
	text.DrawOn(page)

	line := pdfjet.NewLine(70.0, 150.0, 300.0, 150.0)
	line.SetWidth(1.0)
	line.SetColor(color.Oldgloryred)
	line.SetAltDescription("This is a red line.")
	line.SetActualText("This is a red line.")
	line.DrawOn(page)

	box = pdfjet.NewBox()
	box.SetLineWidth(1.0)
	box.SetLocation(70.0, 200.0)
	box.SetSize(100.0, 100.0)
	box.SetColor(color.Oldgloryblue)
	box.SetAltDescription("This is a blue box.")
	box.SetActualText("This is a blue box.")
	box.DrawOn(page)

	page.AddBMC("Span", "This is a test", "This is a test", "")
	page.DrawString(f1, f2, "This is a test", 75.0, 230.0)
	page.AddEMC()

	file1, err := os.Open("images/fruit.jpg")
	if err != nil {
		log.Fatal(err)
	}
	defer file1.Close()
	reader = bufio.NewReader(file1)
	image := pdfjet.NewImage(pdf, reader, imagetype.JPG)

	image.SetLocation(70.0, 310.0)
	image.ScaleBy(0.5)
	image.SetAltDescription("This is an image of a strawberry.")
	image.SetActualText("This is an image of a strawberry.")
	image.DrawOn(page)

	var width float32 = 530.0
	var height float32 = 13.0

	fields := make([]*pdfjet.Field, 0)
	fields = append(fields, pdfjet.NewField(0.0, []string{"Company", "Smart Widget Designs"}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"Street Number", "120"}, false))
	fields = append(fields, pdfjet.NewField(width/8, []string{"Street Name", "Oak"}, false))
	fields = append(fields, pdfjet.NewField(5*width/8, []string{"Street Type", "Street"}, false))
	fields = append(fields, pdfjet.NewField(6*width/8, []string{"Direction", "West"}, false))
	fields = append(fields, pdfjet.NewField(7*width/8, []string{"Suite/Floor/Apt.", "8W"}, false).SetAltDescription("Suite/Floor/Apartment").SetActualText("Suite/Floor/Apartment"))
	fields = append(fields, pdfjet.NewField(0.0, []string{"City/Town", "Toronto"}, false))
	fields = append(fields, pdfjet.NewField(width/2, []string{"Province", "Ontario"}, false))
	fields = append(fields, pdfjet.NewField(7*width/8, []string{"Postal Code", "M5M 2N2"}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"Telephone Number", "(416) 331-2245"}, false))
	fields = append(fields, pdfjet.NewField(width/4, []string{"Fax (if applicable)", "(416) 124-9879"}, false))
	fields = append(fields, pdfjet.NewField(width/2, []string{"Email", "jsmith12345@gmail.ca"}, false))
	fields = append(fields, pdfjet.NewField(0.0, []string{"Other Information", "We don't work on weekends.", "Please send us an Email."}, false))

	form := pdfjet.NewForm(fields)
	form.SetLabelFont(f1)
	form.SetLabelFontSize(7.0)
	form.SetValueFont(f2)
	form.SetValueFontSize(9.0)
	form.SetLocation(70.0, 490.0)
	form.SetRowWidth(width)
	form.SetRowHeight(height)
	form.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example45()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_45 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
