// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/color"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example46 uses PDF layers (optional content groups) that can be shown or
// hidden: a shaded relief map of Europe, the lines of latitude and longitude
// over it, and the capital cities. The map covers 12°W to 42°E and 34°N to
// 62°N, and longitude and latitude map linearly to x and y on it.
func Example46() {
	var x0 float32 = 50.0 // The top left corner of the map
	var y0 float32 = 180.0
	var w float32 = 512.0 // The size of the map
	var h float32 = 512.0 * 840.0 / 1084.0

	mapX := func(longitude float32) float32 {
		return x0 + (longitude+12.0)/54.0*w
	}
	mapY := func(latitude float32) float32 {
		return y0 + (62.0-latitude)/28.0*h
	}

	pdf, err := pdfjet.NewPDFFile("Example_46.pdf")
	if err != nil {
		log.Fatal(err)
	}

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	text := pdfjet.NewTextLine(f2, "PDF Layers")
	text.SetFontSize(22.0)
	text.SetLocation(50.0, 80.0)
	text.DrawOn(page)

	textBlock := pdfjet.NewTextBlock(f1,
		"Each part of this map is a layer, an optional content group, that "+
			"a PDF viewer lists in its Layers panel and can show or hide: the "+
			"relief, the lines of latitude and longitude, and the capital "+
			"cities. The lines are shown on the screen but not printed.")
	textBlock.SetFontSize(12.0)
	textBlock.SetLineSpacing(1.5)
	textBlock.SetLocation(50.0, 95.0)
	textBlock.SetWidth(512.0)
	textBlock.DrawOn(page)

	image := pdfjet.NewImageFromFile(pdf, "images/europe-relief.png")
	image.ResizeWidth(w)
	image.SetLocation(x0, y0)

	// A layer is hidden and not printed unless it is set to be.
	group := pdfjet.NewOptionalContentGroup(pdf, "Relief")
	group.SetVisible(true)
	group.SetPrintable(true)
	group.Add(image)
	group.DrawOn(page)

	group = pdfjet.NewOptionalContentGroup(pdf, "Latitude and Longitude")
	group.SetVisible(true)
	for longitude := -10; longitude <= 40; longitude += 5 {
		line := pdfjet.NewLine(mapX(float32(longitude)), y0, mapX(float32(longitude)), y0+h)
		line.SetStrokeWidth(0.5)
		line.SetStrokeColor(color.White)
		group.Add(line)

		label := "0°"
		if longitude < 0 {
			label = fmt.Sprintf("%d°W", -longitude)
		} else if longitude > 0 {
			label = fmt.Sprintf("%d°E", longitude)
		}
		text = pdfjet.NewTextLine(f1, label)
		text.SetFontSize(8.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(mapX(float32(longitude))-text.GetWidth()/2.0, y0+h+12.0)
		group.Add(text)
	}
	for latitude := 35; latitude <= 60; latitude += 5 {
		line := pdfjet.NewLine(x0, mapY(float32(latitude)), x0+w, mapY(float32(latitude)))
		line.SetStrokeWidth(0.5)
		line.SetStrokeColor(color.White)
		group.Add(line)

		text = pdfjet.NewTextLine(f1, fmt.Sprintf("%d°N", latitude))
		text.SetFontSize(8.0)
		text.SetTextColor(color.Gray)
		text.SetLocation(x0+w+4.0, mapY(float32(latitude))+3.0)
		group.Add(text)
	}
	group.DrawOn(page)

	cities := []string{
		"Athens", "Ankara", "Berlin", "Bucharest", "Dublin",
		"Kyiv", "Lisbon", "London", "Madrid", "Oslo",
		"Paris", "Rome", "Stockholm", "Vienna", "Warsaw",
	}
	locations := [][2]float32{ // Longitude and latitude
		{23.73, 37.98}, {32.86, 39.93}, {13.40, 52.52},
		{26.10, 44.43}, {-6.26, 53.35}, {30.52, 50.45},
		{-9.14, 38.72}, {-0.13, 51.51}, {-3.70, 40.42},
		{10.75, 59.91}, {2.35, 48.86}, {12.50, 41.90},
		{18.07, 59.33}, {16.37, 48.21}, {21.01, 52.23},
	}
	group = pdfjet.NewOptionalContentGroup(pdf, "Capital Cities")
	group.SetVisible(true)
	group.SetPrintable(true)
	for i := 0; i < len(cities); i++ {
		x := mapX(locations[i][0])
		y := mapY(locations[i][1])

		point := pdfjet.NewPoint(x, y)
		point.SetRadius(2.5)
		point.SetFillColor(color.White)
		point.SetStrokeColor(color.Black)
		group.Add(point)

		text = pdfjet.NewTextLine(f2, cities[i])
		text.SetFontSize(8.0)
		text.SetLocation(x+5.0, y+3.0)
		group.Add(text)
	}
	group.DrawOn(page)

	text = pdfjet.NewTextLine(f1, "Relief: Natural Earth, public domain, naturalearthdata.com")
	text.SetFontSize(8.0)
	text.SetTextColor(color.Gray)
	text.SetURIAction("https://www.naturalearthdata.com")
	text.SetLocation(x0, y0+h+30.0)
	text.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example46()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_46 => %4d ms\n", time1-time0)
}
