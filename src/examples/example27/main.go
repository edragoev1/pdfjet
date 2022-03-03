package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/compliance"
	"github.com/edragoev1/pdfjet/src/letter"
	"strings"
	"time"
)

// Example27 -- TODO:
func Example27() {
	file, err := os.Create("Example_27.pdf")
	if err != nil {
		log.Fatal(err)
	}
    defer file.Close()
	w := bufio.NewWriter(file)

	pdf := pdfjet.NewPDF(w, compliance.PDF15)

	// Thai font
	f, err := os.Open("fonts/Noto/NotoSansThai-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader := bufio.NewReader(f)
	f1 := pdfjet.NewFontStream1(pdf, reader)
	f1.SetSize(14.0)

	// Latin font
	f, err = os.Open("fonts/Droid/DroidSans.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader = bufio.NewReader(f)
	f2 := pdfjet.NewFontStream1(pdf, reader)
	f2.SetSize(12.0)

	// Hebrew font
	f, err = os.Open("fonts/Noto/NotoSansHebrew-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader = bufio.NewReader(f)
	f3 := pdfjet.NewFontStream1(pdf, reader)
	f3.SetSize(12.0)

	// Arabic font
	f, err = os.Open("fonts/Noto/NotoNaskhArabic-Regular.ttf.stream")
	if err != nil {
		log.Fatal(err)
	}
	defer f.Close()
	reader = bufio.NewReader(f)
	f4 := pdfjet.NewFontStream1(pdf, reader)
	f4.SetSize(12.0)

	page := pdfjet.NewPage(pdf, letter.Portrait, true)

	x := float32(50.0)
	y := float32(50.0)

	text := pdfjet.NewTextLine(f1, "")
	text.SetFallbackFont(f2)
	text.SetLocation(x, y)

	var buf strings.Builder
	for ch := 0x0E01; ch < 0x0E5B; ch++ {
		if ch%16 == 0 {
			y += 24.0
			text.SetText(buf.String())
			text.SetLocation(x, y)
			text.DrawOn(page)
			buf.Reset()
		}
		if ch > 0x0E30 && ch < 0x0E3B {
			buf.WriteString("\u0E01")
		}
		if ch > 0x0E46 && ch < 0x0E4F {
			buf.WriteString("\u0E2D")
		}
		buf.WriteRune(rune(ch))
	}

	y += 20.0
	text.SetText(buf.String())
	text.SetLocation(x, y)
	text.DrawOn(page)

	y += 20.0
	str := "\u0E1C\u0E1C\u0E36\u0E49\u0E07 abc 123"
	text.SetText(str)
	text.SetLocation(x, y)
	text.DrawOn(page)

	y += 40.0
	str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:"
	str = pdfjet.ReorderVisually(str)
	textLine := pdfjet.NewTextLine(f3, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f3.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f3.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f3.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f3.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)"
	str = pdfjet.ReorderVisually(str)
	textLine = pdfjet.NewTextLine(f3, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f3.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 60.0
	str = pdfjet.ReorderVisually(
		"قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا")
	textLine = pdfjet.NewTextLine(f4, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f4.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.")
	textLine = pdfjet.NewTextLine(f4, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f4.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل")
	textLine = pdfjet.NewTextLine(f4, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f4.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"من بيجو وسيتروين ودونغفينغ.")
	textLine = pdfjet.NewTextLine(f4, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f4.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	y += 20.0
	str = pdfjet.ReorderVisually(
		"وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.")
	textLine = pdfjet.NewTextLine(f4, str)
	textLine.SetFallbackFont(f2)
	textLine.SetLocation(600.0-f4.StringWidth(f2, str), y)
	textLine.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example27()
	elapsed := time.Since(start).String()
	fmt.Printf("Example_27 => %s\n", elapsed[:strings.Index(elapsed, ".")])
}
