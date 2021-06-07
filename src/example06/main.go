package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"pdfjet"
	"pdfjet/color"
	"pdfjet/compliance"
	"pdfjet/letter"
	"pdfjet/shape"
	"strings"
	"time"
)

// Example06 draws the flag of the USA.
func Example06() {
	file, err := os.Create("Example_06.pdf")
	if err != nil {
		log.Fatal(err)
	}
	defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)
	pdf.SetTitle("Hello")
	pdf.SetAuthor("Eugene")
	pdf.SetSubject("Example")
	pdf.SetKeywords("Hello World This is a test")
	pdf.SetCreator("Application Name")

	fileName := "images/linux-logo.png"
	file1, err := os.Open(fileName)
	if err != nil {
		log.Fatal(err)
	}
	defer file1.Close()
	embeddedFile1 := pdfjet.NewEmbeddedFile(
		pdf,
		fileName,
		file1,
		false) // Don't compress images.

	/*
		fileName = "Example_06.java"
		f, err = os.Open(fileName)
		file2 := NewEmbeddedFile(
			pdf,
			fileName,
			f,
			true) // Compress text files.
	*/
	page := pdfjet.NewPage(pdf, letter.Portrait, true)

	flag := pdfjet.NewBox()
	flag.SetLocation(100.0, 100.0)
	flag.SetSize(190.0, 100.0)
	flag.SetColor(color.White)
	flag.DrawOn(page)
	/*
		var xy []float32
		xy[0] = 0.0
		xy[1] = 0.0
	*/
	// var xy [2]float32
	//_ = xy
	var sw float32
	sw = 7.69 // stripe width
	stripe := pdfjet.NewLine(0.0, sw/2, 190.0, sw/2)
	stripe.SetWidth(sw)
	stripe.SetColor(color.Oldgloryred)
	for row := 0; row < 7; row++ {
		stripe.PlaceIn(flag, 0.0, float32(row)*2*sw)
		stripe.DrawOn(page)
	}

	union := pdfjet.NewBox()
	union.SetSize(76.0, 53.85)
	union.SetColor(color.Oldgloryblue)
	union.SetFillShape(true)
	union.PlaceIn(flag, 0.0, 0.0)
	union.DrawOn(page)

	var hSi float32
	hSi = 12.6 // horizontal star interval
	var vSi float32
	vSi = 10.8 // vertical star interval
	star := pdfjet.NewPoint(hSi/float32(2), vSi/float32(2))
	star.SetShape(shape.Star)
	star.SetRadius(3.0)
	star.SetColor(color.White)
	star.SetFillShape(true)

	for row := 0; row < 6; row++ {
		for col := 0; col < 5; col++ {
			star.PlaceIn(union, float32(row)*hSi, float32(col)*vSi)
			star.DrawOn(page)
		}
	}

	star.SetLocation(hSi, vSi)
	for row := 0; row < 5; row++ {
		for col := 0; col < 4; col++ {
			star.PlaceIn(union, float32(row)*hSi, float32(col)*vSi)
			star.DrawOn(page)
		}
	}

	attachment := pdfjet.NewFileAttachment(pdf, embeddedFile1)
	attachment.SetLocation(100.0, 300.0)
	attachment.SetIconPushPin()
	attachment.SetIconSize(24.0)
	attachment.SetTitle("Attached File: " + embeddedFile1.GetFileName())
	attachment.SetDescription(
		"Right mouse click or double click on the icon to save the attached file.")
	attachment.DrawOn(page)
	/*
		attachment = NewFileAttachment(pdf, file2)
		attachment.setLocation(200.0, 300.0)
		attachment.setIconPaperclip()
		attachment.setIconSize(24.0)
		attachment.setTitle("Attached File: " + file2.getFileName())
		attachment.setDescription(
			"Right mouse click or double click on the icon to save the attached file.")
		attachment.DrawOn(page)
	*/

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example06()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_06 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
