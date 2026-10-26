// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"strconv"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/alignment"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

var countries = []string{"Spain", "Italy", "Switzerland", "Greece"}

var maps = []string{
	"images/spain-admin.jpg",
	"images/italy-admin.jpg",
	"images/swiss-admin.jpg",
	"images/gr-map.jpg",
}

var texts = [][]string{
	{
		"Spain is divided into 17 autonomous communities and two autonomous cities, Ceuta and " +
			"Melilla, on the coast of North Africa. The communities differ in their powers: some, " +
			"such as the Basque Country and Navarre, collect most of their own taxes.",
		"Within the communities are 50 provinces, the units of the national government since " +
			"1833, and more than 8,000 municipalities. Madrid is both the capital of the country and " +
			"an autonomous community of its own.",
		"Spanish is the official language of the whole country. Catalan, Galician and Basque are " +
			"official too in the communities where they are spoken, and their schools and regional " +
			"governments use them.",
	},
	{
		"Italy has 20 regions. Five of them have a special statute and wider self-government: " +
			"Sicily, Sardinia, Trentino-Alto Adige, Friuli Venezia Giulia and the Aosta Valley, " +
			"islands or regions on a border, most with languages of their own.",
		"The regions are divided into provinces and metropolitan cities, and those into nearly " +
			"8,000 comuni, the municipalities, which run the local services from schools to roads.",
		"Two independent states lie within Italy: Vatican City, inside Rome, and San Marino, in " +
			"the hills near the Adriatic coast.",
	},
	{
		"Switzerland is a federation of 26 cantons. Each has its own constitution, parliament, " +
			"government and courts, and the cantons keep every power the federal constitution does " +
			"not give to the confederation.",
		"Bern is the seat of the federal government, called the federal city rather than the " +
			"capital. Zurich is the largest city, and Geneva is the home of many international " +
			"organizations.",
		"The country has four national languages: German, French, Italian and Romansh. Most " +
			"cantons have one official language; Bern, Fribourg and Valais have two, and Graubünden " +
			"has three.",
	},
	{
		"Greece is divided into 13 regions, each with an elected governor and council, and the " +
			"regions into 74 regional units and more than 300 municipalities.",
		"Mount Athos, a peninsula of monasteries in the north, is self-governing under the " +
			"sovereignty of Greece.",
		"Greece has about 6,000 islands and islets, of which a little over 200 are inhabited. " +
			"The largest, Crete, is a region of its own.",
	},
}

var captions = []string{
	"Spain: its autonomous communities and provinces.",
	"Italy: its regions and provinces.",
	"Switzerland: its cantons.",
	"Greece: its regions.",
}

var altTexts = []string{
	"A map of Spain with its autonomous communities and provinces outlined and named.",
	"A map of Italy with its regions and provinces outlined and named.",
	"A map of Switzerland with its cantons outlined and named.",
	"A map of Greece with its regions outlined and named.",
}

// Example22 flows a guide to four countries onto pages with TextFlow: its
// headings, paragraphs and maps are added in order, and TextFlow breaks the
// paragraphs across pages between their lines, moves a map that does not fit
// to the next page whole with its caption, and keeps each heading with the
// text after it.
func Example22() {
	pdf, err := pdfjet.NewPDFFile("Example_22.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Four Countries and Their Regions")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(11.0)

	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	f2.SetSize(16.0)

	flow := pdfjet.NewTextFlow(pdf, letter.Portrait()).SetMargins(60.0)
	flow.Add(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f2, "Four Countries and Their Regions").SetFontSize(22.0)).
		SetStructureType(structelem.H1))
	flow.AddSpace(12.0)
	flow.Add(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f1,
		"This guide is drawn with TextFlow: its headings, paragraphs and maps are added in "+
			"order, and TextFlow puts them on as many pages as they need. A paragraph breaks across "+
			"pages between its lines, a map goes whole to the next page when it does not fit, and a "+
			"heading never ends a page. The maps are of the U.S. government, in the public domain.")))

	for i := range countries {
		flow.AddSpace(24.0)
		flow.Add(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f2, countries[i])).SetStructureType(structelem.H2))
		flow.AddSpace(6.0)

		paragraphs := make([]*pdfjet.Paragraph, 0)
		for _, text := range texts[i] {
			paragraphs = append(paragraphs, pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f1, text)))
		}
		flow.AddParagraphs(paragraphs, 8.0)
		flow.AddSpace(12.0)

		mapImage := pdfjet.NewImageFromFile(pdf, maps[i])
		mapImage.SetAltDescription(altTexts[i])
		mapImage.ResizeHeight(320.0)
		flow.AddImageAligned(mapImage, alignment.Center)
		flow.AddSpace(4.0)
		flow.Add(pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f1, captions[i]).SetFontSize(9.0).
			SetTextColor(color.DimGray)).SetTextAlignment(alignment.Center).
			SetStructureType(structelem.Caption))
	}

	pages := make([]*pdfjet.Page, 0)
	flow.DrawOnPages(&pages)
	for i, page := range pages {
		page.AddFooter(pdfjet.NewTextLine(f1, "Page "+strconv.Itoa(i+1)+" of "+strconv.Itoa(len(pages))).SetFontSize(9.0))
	}
	pdf.AddPages(pages)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example22()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_22 => %4d ms\n", time1-time0)
}
