// stamp_in_content_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// A stamp with no description of its own, drawn inside the real content of a
// tagged page, is part of that content, not an artifact inside it (ISO
// 14289-1, 7.1): letters drawn once as stamps and placed in a paragraph.

func testLetterStamp(pdf *PDF) *Stamp {
	stamp := NewStamp(pdf).SetSize(6, 8)
	stamp.MoveTo(0, 8).LineTo(3, 0).LineTo(6, 8).FillPath()
	stamp.Complete()
	return stamp
}

func TestStampInContentAnUndescribedStampInAParagraphIsPartOfIt(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	page := NewPage(doc.pdf, letter.Portrait())
	stamp := testLetterStamp(doc.pdf)
	page.AddBDC(structelem.P, "km", "AAA", "AAA")
	for i := 0; i < 3; i++ {
		stamp.SetLocation(50+7*float32(i), 50)
		stamp.DrawOn(page)
	}
	page.AddEMC()
	content := testContent(page)
	if strings.Contains(content, "/Artifact BMC") {
		t.Error(content)
	}
	if strings.Count(content, "BDC\n") != 1 || strings.Count(content, "EMC\n") != 1 || strings.Count(content, " Do\n") != 3 {
		t.Error(content)
	}
	raw := string(doc.complete())
	if !strings.Contains(raw, "/ActualText") {
		t.Error("the paragraph lost its text")
	}
}

func TestStampInContentAnUndescribedStampOutsideContentIsStillAnArtifact(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	page := NewPage(doc.pdf, letter.Portrait())
	stamp := testLetterStamp(doc.pdf)
	stamp.SetLocation(50, 50)
	stamp.DrawOn(page)
	content := testContent(page)
	if !strings.Contains(content, "/Artifact BMC\n") || strings.Count(content, "EMC\n") != 1 {
		t.Error(content)
	}
}

func TestStampInContentADescribedStampInAParagraphIsAnElementOfItsOwn(t *testing.T) {
	doc := testNewDoc()
	doc.pdf.SetCompliance(compliance.PDF_UA_1).SetTitle("Title")
	page := NewPage(doc.pdf, letter.Portrait())
	stamp := testLetterStamp(doc.pdf)
	stamp.SetAltDescription("A logo")
	page.AddBDC(structelem.P, "en", "Text", "Text")
	stamp.SetLocation(50, 50)
	stamp.DrawOn(page)
	page.AddEMC()
	if content := testContent(page); strings.Count(content, "EMC\n") != 2 {
		t.Error(content)
	}
}

func TestStampInContentAnUntaggedPageMarksNothing(t *testing.T) {
	doc := testNewDoc()
	page := NewPage(doc.pdf, letter.Portrait())
	stamp := testLetterStamp(doc.pdf)
	page.AddBDC(structelem.P, "km", "A", "A")
	stamp.SetLocation(50, 50)
	stamp.DrawOn(page)
	page.AddEMC()
	content := testContent(page)
	if strings.Contains(content, "BMC") || strings.Contains(content, "BDC") || strings.Contains(content, "EMC") {
		t.Error(content)
	}
}
