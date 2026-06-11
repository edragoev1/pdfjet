package main

import (
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/content"
	"github.com/edragoev1/pdfjet/src/letter"
)

// Example27 -- TODO:
func Example27() {
	pdf := pdfjet.NewPDFFile("Example_27.pdf")

	// Thai font
	f1 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream")
	f1.SetSize(12.0)

	// Hebrew font
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream")
	f2.SetSize(12.0)

	// Arabic font
	f3 := pdfjet.NewFontFromFile(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream")
	f3.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait)

	// Thai text from a file
	textBlock := pdfjet.NewTextBlock(f1, content.OfTextFile("data/languages/thai.txt"))
	textBlock.SetLocation(30.0, 30.0)
	textBlock.SetWidth(430.0)
	textBlock.SetBorderColor(color.Blue)
	textBlock.SetTextPadding(10.0)
	xy := textBlock.DrawOn(page) // Draw the text and get coordinates

	x := float32(570.0)
	y := xy[1] + 55.0

	str := "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:"
	str = pdfjet.ReorderVisually(str)
	textLine := pdfjet.NewTextLine(f2, str)
	textLine.SetLocation(x-f2.StringWidth(f2.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f2, str)
	textLine.SetLocation(x-f2.StringWidth(f2.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f2, str)
	textLine.SetLocation(x-f2.StringWidth(f2.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f2, str)
	textLine.SetLocation(x-f2.StringWidth(f2.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f2, str)
	textLine.SetLocation(x-f2.StringWidth(f2.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 65.0
	str = pdfjet.ReorderVisually(
		"قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا")
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetLocation(x-f3.StringWidth(f3.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.")
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetLocation(x-f3.StringWidth(f3.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل")
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetLocation(x-f3.StringWidth(f3.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"من بيجو وسيتروين ودونغفينغ.")
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetLocation(x-f3.StringWidth(f3.GetSize(), str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.")
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetLocation(x-f3.StringWidth(f3.GetSize(), str), y)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example27()
	pdfjet.PrintDuration("Example_27", time.Since(start))
}
