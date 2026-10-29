// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package main

import (
	"fmt"
	"log"
	"time"

	// The Chinese, Japanese and Korean fonts PDFjet carries, built into
	// the program
	_ "github.com/edragoev1/pdfjet/v9/resources/fonts/cjk"
	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansSC"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// Example13 draws a paragraph about Switzerland in English, and the same
// paragraph in Simplified Chinese.
func Example13() {
	pdf, err := pdfjet.NewPDFFile("Example_13.pdf")
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("Switzerland, in English and Chinese")

	f1 := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	f1.SetSize(12.0)

	// Chinese (Simplified) font
	f2 := pdfjet.NewFontFromFile(pdf, IBMPlexSansSC.Regular)
	f2.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait())

	column := pdfjet.NewTextColumn()
	column.SetLocation(70.0, 70.0)
	column.SetWidth(500.0)
	column.SetLineSpacing(1.5)      // 1.5 x font height
	column.SetParagraphSpacing(0.5) // 0.5 x line spacing

	p1 := pdfjet.NewParagraph()
	p1.Add(pdfjet.NewTextLine(f1, "The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002."))
	column.AddParagraph(p1)

	chinese := "瑞士联邦于1291年作为三个州之间的防御联盟而建立。此后数年间，其他地区陆续加入了最初的三个州。1499年，瑞士联邦脱离神圣罗马帝国，获得独立。长期以来，欧洲各大国一直尊重瑞士的主权和中立地位，瑞士也没有卷入两次世界大战。过去半个世纪欧洲的政治和经济一体化，以及瑞士在许多联合国机构和国际组织中的作用，加强了瑞士与邻国的联系。然而，瑞士直到2002年才正式成为联合国会员国。"
	xy := column.DrawOn(page)

	// The same paragraph in Chinese, in a column of its own, set tighter: IBM Plex Sans SC has a
	// line gap of a whole em, which the lines of a paragraph are set with.
	column2 := pdfjet.NewTextColumn()
	column2.SetLocation(70.0, xy[1]+12.0)
	column2.SetWidth(500.0)
	column2.SetLineSpacing(1.2)
	column2.AddCJKParagraphWithLanguage(f2, chinese, "zh-Hans") // Read by a screen reader as Chinese
	column2.DrawOn(page)

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	time0 := time.Now().UnixMilli()
	Example13()
	time1 := time.Now().UnixMilli()
	fmt.Printf("Example_13 => %4d ms\n", time1-time0)
}
