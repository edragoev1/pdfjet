// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// The Go port's side of the corpus check, tests/corpus/check-corpus.py. It
// reads one PDF, merges the whole of it into a document of its own, and splits
// its first and its last page into two more:
//
//	go run ./tests/corpus/go IN.pdf PASSWORD OUTDIR
//
// It writes merged.pdf, first.pdf and last.pdf into OUTDIR, the ones it could
// make, and prints what it read as JSON: the error, or the number of pages and
// the size of each page. A Go runtime error -- an index out of range, a nil
// pointer -- is reported as a crash, which the other ports would have too.
package main

import (
	"bufio"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"runtime"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
)

type report struct {
	Error string            `json:"error,omitempty"`
	Crash string            `json:"crash,omitempty"`
	Pages int               `json:"pages"`
	Sizes [][2]float32      `json:"sizes"`
	Made  map[string]string `json:"made"`
}

// crashOf returns the runtime error that r or err is, or "".
func crashOf(r any) string {
	if e, ok := r.(runtime.Error); ok {
		return e.Error()
	}
	return ""
}

// write merges the pages into OUTDIR/name, and returns "ok" or the error.
func write(out *report, objects []*pdfjet.PDFobj, path string, pages ...int) (result string) {
	defer func() {
		if r := recover(); r != nil {
			if crash := crashOf(r); crash != "" {
				out.Crash = filepath.Base(path) + ": " + crash
			}
			result = fmt.Sprint(r)
		}
	}()
	f, err := os.Create(path)
	if err != nil {
		return err.Error()
	}
	defer f.Close()
	w := bufio.NewWriter(f)
	pdf := pdfjet.NewPDF(w)
	if pages == nil {
		err = pdf.Merge(objects)
	} else {
		err = pdf.MergePages(objects, pages...)
	}
	if err == nil {
		err = pdf.Complete()
	}
	if err == nil {
		err = w.Flush()
	}
	if err != nil {
		os.Remove(path)
		return err.Error()
	}
	return "ok"
}

func main() {
	if len(os.Args) != 4 {
		fmt.Fprintln(os.Stderr, "usage: go run ./tests/corpus/go IN.pdf PASSWORD OUTDIR")
		os.Exit(2)
	}
	out := &report{Sizes: [][2]float32{}, Made: map[string]string{}}
	defer func() {
		if r := recover(); r != nil {
			out.Crash = fmt.Sprint(r)
		}
		json.NewEncoder(os.Stdout).Encode(out)
	}()
	data, err := os.ReadFile(os.Args[1])
	if err != nil {
		out.Error = err.Error()
		return
	}
	objects, err := pdfjet.NewPDF(bufio.NewWriter(nil)).ReadWithPassword(data, os.Args[2])
	if err != nil {
		out.Crash = crashOf(err)
		out.Error = err.Error()
		return
	}
	pages := pdfjet.NewPDF(bufio.NewWriter(nil)).GetPageObjects(objects)
	out.Pages = len(pages)
	for _, page := range pages {
		size := page.GetPageSize()
		out.Sizes = append(out.Sizes, [2]float32{size.GetWidth(), size.GetHeight()})
	}
	dir := os.Args[3]
	// Each document is read again: a merge may change what it merges.
	read := func() []*pdfjet.PDFobj {
		objects, _ := pdfjet.NewPDF(bufio.NewWriter(nil)).ReadWithPassword(data, os.Args[2])
		return objects
	}
	out.Made["merged"] = write(out, objects, filepath.Join(dir, "merged.pdf"))
	if len(pages) > 0 {
		out.Made["first"] = write(out, read(), filepath.Join(dir, "first.pdf"), 1)
		out.Made["last"] = write(out, read(), filepath.Join(dir, "last.pdf"), len(pages))
	}
}
