//go:build texttrace

//
// texttrace.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

package pdfjet

// A build with the texttrace tag records every string the program draws, as it
// is before its characters become glyph codes, and appends it to the file named
// by the PDFJET_TEXT_TRACE environment variable when the PDF is completed. It
// is how .github/scripts/check-example-text.py learns what the examples drew:
//
//	go build -tags texttrace -o Example_01.exe examples/example01/main.go
//	PDFJET_TEXT_TRACE=trace.jsonl ./Example_01.exe
//
// Each line of the file is a JSON object: the base name of the PDF file, the
// number of the page, from 1, the name of the font and one string drawn on the
// page in it, in the order it was drawn. A page of a PDF that was read, drawn
// on with NewPageFromObject, has the object number of its dictionary, "xref",
// in place of its number: it keeps the number when AddObjects writes it. A
// string drawn on a stamp counts on every page the stamp is drawn on. The
// default build has none of this: the hooks it calls are nil.

import (
	"encoding/json"
	"os"
	"path/filepath"
	"sync"
)

type textTraceLine struct {
	PDF  string `json:"pdf"`
	Page int    `json:"page,omitempty"`
	Xref int    `json:"xref,omitempty"`
	Font string `json:"font"`
	Text string `json:"text"`
}

type drawnText struct {
	font string
	text string
}

var textTrace = struct {
	sync.Mutex
	drawn map[any][]drawnText // The strings of each *Page and *Stamp
}{drawn: make(map[any][]drawnText)}

func init() {
	path := os.Getenv("PDFJET_TEXT_TRACE")
	if path == "" {
		return
	}
	traceText = func(owner any, font *Font, text string) {
		textTrace.Lock()
		defer textTrace.Unlock()
		textTrace.drawn[owner] = append(textTrace.drawn[owner], drawnText{font.name, text})
	}
	traceStamp = func(stamp *Stamp, page *Page) {
		textTrace.Lock()
		defer textTrace.Unlock()
		textTrace.drawn[page] = append(textTrace.drawn[page], textTrace.drawn[stamp]...)
	}
	traceComplete = func(pdf *PDF) {
		name := ""
		if pdf.file != nil {
			name = filepath.Base(pdf.file.Name())
		}
		textTrace.Lock()
		defer textTrace.Unlock()
		f, err := os.OpenFile(path, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0o644)
		if err != nil {
			panic(err)
		}
		defer f.Close()
		encoder := json.NewEncoder(f)
		encoder.SetEscapeHTML(false)
		for i, page := range pdf.pages {
			for _, text := range textTrace.drawn[page] {
				if err := encoder.Encode(textTraceLine{PDF: name, Page: i + 1, Font: text.font, Text: text.text}); err != nil {
					panic(err)
				}
			}
			delete(textTrace.drawn, page)
		}
		for owner, texts := range textTrace.drawn {
			if page, ok := owner.(*Page); ok && page.pdf == pdf && page.pageObj != nil {
				for _, text := range texts {
					if err := encoder.Encode(textTraceLine{PDF: name, Xref: page.pageObj.number, Font: text.font, Text: text.text}); err != nil {
						panic(err)
					}
				}
				delete(textTrace.drawn, page)
			}
		}
	}
}
