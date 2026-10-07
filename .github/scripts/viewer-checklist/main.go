// Draws the checklist of the manual viewer pass, goal 4 of TODO.md, as a PDF
// to print and tick off with a pen: a box before each item, a line for the
// version of each viewer, and lines for notes after it. It is PDF/UA, as the
// examples are.
//
//	go run ./.github/scripts/viewer-checklist viewer-files-to-test/Checklist.pdf
//
// Run it from the root of the repository, which has the fonts.
package main

import (
	"log"
	"os"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/letter"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

type section struct {
	title string
	intro string
	items []string
}

var sections = []section{
	{"Acrobat Reader on Windows", "", []string{
		"Example_30 asks for a password, and opens with the user password hello.",
		"Opened with hello: File > Properties > Security shows printing allowed and copying of content not allowed.",
		"Example_30 opens with the owner password world, and then copying is allowed.",
		"Encrypted_Cyrillic opens with the password пароль (a Russian keyboard, or pasted).",
		"Encrypted_200_Bytes opens with 0123456789 typed 20 times, 200 characters (paste it).",
		"Example_06: the push pin and the paperclip open linux-logo.png and Example_02.java, and each can be saved.",
		"Example_06: the note icon shows \"Please check the figures on page 2.\", the link opens pdfjet.com, " +
			"and the triangle, square and circle are half transparent.",
		"Example_22: each of the 8 entries of the contents jumps to its page.",
		"Example_07, 34 and 55, the PDF/A examples, open with the PDF/A bar and no error. Keep a screenshot of each.",
		"Example_01, 02, 08, 13, 27, 38 and 54 open with no warning and no offer to repair the file. " +
			"Example_02 takes some seconds: it embeds four whole Chinese, Japanese and Korean fonts, 12.6 MB, " +
			"as PDFjet does not subset fonts.",
		"Example_04 and 44, whose Chinese, Japanese and Korean fonts are not embedded, show that text " +
			"(Acrobat may offer its Asian font pack, and to make the file accessible: they are not PDF/UA, " +
			"on purpose, as PDF/UA needs embedded fonts).",
		"Example_46: the Layers panel lists Relief, Latitude and Longitude, and Capital Cities, " +
			"and switching each off and on hides and shows it on the map of Europe.",
	}},
	{"NVDA with Acrobat Reader", "", []string{
		"Example_01: NVDA reads the English, Greek and Bulgarian text, each in its own language.",
		"Example_54: H moves from heading to heading, in the order of the page.",
		"Example_38: in the table, NVDA says the column header of each cell (Ctrl+Alt+arrow keys).",
		"Example_25: NVDA reads the description of the donut chart, not only \"graphic\".",
		"Example_22: NVDA says \"link\" and the text of each entry of the contents.",
	}},
	{"Edge on Windows", "Edge draws PDFs with Adobe's engine.", []string{
		"Example_30 opens with hello, and prints; copying is not allowed.",
		"Encrypted_Cyrillic and Encrypted_200_Bytes open with their passwords.",
		"Example_06: the icons and the shapes are where Acrobat draws them, and the note and the attachments open.",
		"Example_01, 02, 08, 13, 27, 38 and 54 look as they do in Acrobat Reader.",
	}},
	{"Foxit PDF Reader", "", []string{
		"Example_30 opens with hello, and prints; copying is not allowed.",
		"Example_46: the Layers panel lists its three layers, and each hides and shows.",
		"Encrypted_Cyrillic and Encrypted_200_Bytes open with their passwords.",
		"Example_06: the icons and the shapes are where Acrobat draws them, and the note and the attachments open.",
		"Example_01, 02, 08, 13, 27, 38 and 54 look as they do in Acrobat Reader.",
	}},
	{"Preview on the Mac", "", []string{
		"Example_30 opens with hello; the Inspector (Cmd-I), lock tab, shows printing allowed and copying not.",
		"Example_30 opens with the owner password world.",
		"Encrypted_Cyrillic and Encrypted_200_Bytes open with their passwords.",
		"Example_06: the attachments open, the note shows its text, the link opens, and the shapes are drawn.",
		"Example_22: the entries of the contents jump to their pages.",
		"Example_04 and 44 show their Chinese, Japanese and Korean text.",
	}},
	{"VoiceOver in Preview", "", []string{
		"Example_01: VoiceOver reads the English, Greek and Bulgarian text, each in its own language.",
		"Example_54: the headings come in the order of the page (VO+Cmd+H).",
		"Example_38: VoiceOver says the column header of each cell of the table.",
		"Example_25: VoiceOver reads the description of the donut chart.",
	}},
	{"Firefox, now and then", "Firefox is checked automatically; this is a look by hand.", []string{
		"Example_30 asks for a password, and opens with hello.",
		"Example_06: the attachments are listed in the sidebar, and open.",
	}},
}

const (
	marginX   = 54.0
	marginTop = 54.0
	marginBot = 60.0
	boxSize   = 10.0
	textX     = marginX + 20.0
	textWidth = 612.0 - textX - marginX
	fontSize  = 10.5
	ink       = 0x1D2433
	soft      = 0x4A5263
	rule      = 0x9AA1AD
)

var (
	pdf        *pdfjet.PDF
	regular    *pdfjet.Font
	semiBold   *pdfjet.Font
	page       *pdfjet.Page
	pages      int
	y          float32
	pageHeight float32
)

func newPage() {
	page = pdfjet.NewPage(pdf, letter.Portrait())
	pageHeight = page.GetHeight()
	pages++
	y = marginTop
}

func ensure(height float32) {
	if y+height > pageHeight-marginBot {
		newPage()
	}
}

func textBlock(font *pdfjet.Font, size float32, text string, x, width float32, color int32) *pdfjet.TextBlock {
	block := pdfjet.NewTextBlock(font, text)
	block.SetFontSize(size)
	block.SetWidth(width)
	block.SetTextColor(color)
	block.SetLocation(x, y)
	return block
}

// A line to write on, from x to the right margin
func writingLine(x, at float32) {
	line := pdfjet.NewLine(x, at, 612.0-marginX, at)
	line.SetStrokeWidth(0.5)
	line.SetStrokeColor(rule)
	line.DrawOn(page)
}

// The height a section takes, as main draws it
func sectionHeight(s section) float32 {
	h := float32(14 + 20 + 36)
	if s.intro != "" {
		h += textBlock(regular, 9.5, s.intro, marginX, 612-2*marginX, soft).GetHeight() + 4
	}
	for _, item := range s.items {
		h += textBlock(regular, fontSize, item, textX, textWidth, ink).GetHeight() + 5
	}
	return h
}

func main() {
	out := "viewer-checklist.pdf"
	if len(os.Args) > 1 {
		out = os.Args[1]
	}
	var err error
	pdf, err = pdfjet.NewPDFFile(out)
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetCompliance(compliance.PDF_UA_1)
	pdf.SetTitle("PDFjet v9.0.3: the manual viewer pass")
	pdf.SetLanguage("en-US")

	regular = pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	semiBold = pdfjet.NewFontFromFile(pdf, IBMPlexSans.SemiBold)
	newPage()

	title := pdfjet.NewTextLine(semiBold, "PDFjet v9.0.3: the manual viewer pass")
	title.SetStructureType(structelem.H1)
	title.SetFontSize(18)
	title.SetTextColor(ink)
	title.SetLocation(marginX, y+18)
	title.DrawOn(page)
	y += 30

	intro := textBlock(regular, 10, "Goal 4 of TODO.md. The files are in viewer-files-to-test: the Java "+
		"examples built from master, Example_30 with the user password hello and the owner password world, "+
		"and Encrypted_Cyrillic and Encrypted_200_Bytes. Tick each box when it holds; when it does not, "+
		"write what you see on the lines below the viewer.", marginX, 612-2*marginX, soft)
	y = intro.DrawOn(page)[1] + 8

	for _, s := range sections {
		// A section stays on one page: its heading, its items and its notes
		ensure(sectionHeight(s))
		y += 14
		heading := pdfjet.NewTextLine(semiBold, s.title)
		heading.SetStructureType(structelem.H2)
		heading.SetFontSize(13)
		heading.SetTextColor(ink)
		heading.SetLocation(marginX, y+13)
		heading.DrawOn(page)

		version := pdfjet.NewTextLine(regular, "Version")
		version.SetFontSize(9)
		version.SetTextColor(soft)
		version.SetLocation(360, y+13)
		version.DrawOn(page)
		writingLine(398, y+14)
		y += 20

		if s.intro != "" {
			note := textBlock(regular, 9.5, s.intro, marginX, 612-2*marginX, soft)
			y = note.DrawOn(page)[1] + 4
		}

		for _, item := range s.items {
			block := textBlock(regular, fontSize, item, textX, textWidth, ink)
			ensure(block.GetHeight() + 6)
			block.SetLocation(textX, y)
			box := pdfjet.NewRect(marginX, y+3, boxSize, boxSize)
			box.SetBorderWidth(0.8)
			box.SetBorderColor(ink)
			box.DrawOn(page)
			y = block.DrawOn(page)[1] + 5
		}

		// Two lines for notes
		ensure(40)
		y += 18
		writingLine(marginX, y)
		y += 18
		writingLine(marginX, y)
	}

	// What was found, at the end
	ensure(110)
	y += 24
	found := pdfjet.NewTextLine(semiBold, "What was found, and where it goes")
	found.SetStructureType(structelem.H2)
	found.SetFontSize(13)
	found.SetTextColor(ink)
	found.SetLocation(marginX, y+13)
	found.DrawOn(page)
	y += 20
	for i := 0; i < 4; i++ {
		y += 20
		writingLine(marginX, y)
	}

	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}
