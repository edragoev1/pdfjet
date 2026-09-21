// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// The Go port's side of the image check, tests/references/images/check-images.py.
// It embeds one PNG, JPEG or BMP file in a one-page PDF, the way a user would,
// and draws it at the top left corner of a Letter page at the size PDFjet
// gives it:
//
//	go run ./tests/references/images/go IMAGE OUT.pdf
//
// It prints what it did as JSON: the error, when PDFjet refuses the file, or
// the size the image is drawn at, in points. A Go runtime error -- an index
// out of range, a nil pointer -- is reported as a crash, since an image PDFjet
// cannot read should be refused with an error of its own.
package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"runtime"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

type report struct {
	Error  string  `json:"error,omitempty"`
	Crash  string  `json:"crash,omitempty"`
	Width  float32 `json:"width"`
	Height float32 `json:"height"`
}

// embed writes the PDF with the image in it, and panics as PDFjet does when
// it refuses the image.
func embed(out *report, in, path string) {
	f, err := os.Create(path)
	if err != nil {
		panic(err)
	}
	defer f.Close()
	w := bufio.NewWriter(f)
	pdf := pdfjet.NewPDF(w)
	image := pdfjet.NewImageFromFile(pdf, in)
	out.Width = image.GetWidth()
	out.Height = image.GetHeight()
	page := pdfjet.NewPage(pdf, letter.Portrait())
	image.SetLocation(0.0, 0.0)
	image.DrawOn(page)
	if err := pdf.Complete(); err != nil {
		panic(err)
	}
	if err := w.Flush(); err != nil {
		panic(err)
	}
}

func main() {
	if len(os.Args) != 3 {
		fmt.Fprintln(os.Stderr, "usage: go run ./tests/references/images/go IMAGE OUT.pdf")
		os.Exit(2)
	}
	out := &report{}
	defer func() {
		if r := recover(); r != nil {
			if e, ok := r.(runtime.Error); ok {
				out.Crash = e.Error()
			}
			out.Error = fmt.Sprint(r)
			os.Remove(os.Args[2])
		}
		json.NewEncoder(os.Stdout).Encode(out)
	}()
	embed(out, os.Args[1], os.Args[2])
}
