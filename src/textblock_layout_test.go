// textblock_layout_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/json"
	"os"
	"path/filepath"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/alignment"
)

// The layout of TextBlock is the reference for the TypeScript preview of
// pdfjet-client, which lays text out as this port does: Go leads and the
// TypeScript follows. Its check, check-textblock.sh, has this test write the
// layout of its cases, and compares its own with it:
//
//	PDFJET_LAYOUT_CASES=cases.json PDFJET_LAYOUT_OUT=go.json \
//	    go test ./src -run '^TestTextBlockLayoutReference$'
//
// Without the two variables the test is skipped.

type layoutCase struct {
	Name             string  `json:"name"`
	Font             string  `json:"font"`
	Fallback         *string `json:"fallback"`
	FontSize         float32 `json:"fontSize"`
	FallbackFontSize float32 `json:"fallbackFontSize"`
	Width            float32 `json:"width"`
	Height           float32 `json:"height"`
	Padding          float32 `json:"padding"`
	LineSpacing      float32 `json:"lineSpacing"`
	Align            string  `json:"align"`
	Valign           string  `json:"valign"`
	Text             string  `json:"text"`
}

type layoutLine struct {
	Text    string  `json:"text"`
	XOffset float32 `json:"xOffset"`
}

type layoutResult struct {
	Name        string       `json:"name"`
	Lines       []layoutLine `json:"lines"`
	YText       float32      `json:"yText"`
	Leading     float32      `json:"leading"`
	BlockHeight float32      `json:"blockHeight"`
}

func TestTextBlockLayoutReference(t *testing.T) {
	in, out := os.Getenv("PDFJET_LAYOUT_CASES"), os.Getenv("PDFJET_LAYOUT_OUT")
	if in == "" || out == "" {
		t.Skip("PDFJET_LAYOUT_CASES and PDFJET_LAYOUT_OUT are not set")
	}
	data, err := os.ReadFile(in)
	if err != nil {
		t.Fatal(err)
	}
	var cases []layoutCase
	if err := json.Unmarshal(data, &cases); err != nil {
		t.Fatal(err)
	}

	pdf := NewPDF(bufio.NewWriter(new(bytes.Buffer)))
	fonts := map[string]*Font{}
	// A font is named as its file is, IBMPlexSans-Regular, and is read from
	// the stream of its family in the fonts directory.
	fontNamed := func(name string) *Font {
		if font, ok := fonts[name]; ok {
			return font
		}
		family := strings.SplitN(name, "-", 2)[0]
		matches, _ := filepath.Glob(filepath.Join("..", "fonts", family, name+".*.stream"))
		if len(matches) != 1 {
			t.Fatalf("there is no font %s in ../fonts/%s", name, family)
		}
		fonts[name] = NewFontFromFile(pdf, matches[0])
		return fonts[name]
	}
	alignments := map[string]alignment.Alignment{
		"Left": alignment.Left, "Right": alignment.Right, "Center": alignment.Center,
		"Top": alignment.Top, "Bottom": alignment.Bottom,
	}

	results := []layoutResult{}
	for _, c := range cases {
		textBlock := NewTextBlock(fontNamed(c.Font), c.Text)
		if c.Fallback != nil {
			textBlock.SetFallbackFont(fontNamed(*c.Fallback))
		}
		textBlock.SetFontSize(c.FontSize).SetFallbackFontSize(c.FallbackFontSize)
		textBlock.SetSize(c.Width, c.Height).SetPadding(c.Padding).SetLineSpacing(c.LineSpacing)
		textBlock.SetTextAlignment(alignments[c.Align]).SetVerticalAlignment(alignments[c.Valign])
		textBlock.SetLocation(30.0, 150.0)

		textLines, yText, leading, blockHeight := textBlock.layout()
		result := layoutResult{Name: c.Name, Lines: []layoutLine{},
			YText: yText, Leading: leading, BlockHeight: blockHeight}
		for _, textLine := range textLines {
			result.Lines = append(result.Lines, layoutLine{textLine.text, textLine.xOffset})
		}
		results = append(results, result)
	}
	data, err = json.MarshalIndent(results, "", " ")
	if err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(out, data, 0o644); err != nil {
		t.Fatal(err)
	}
}
