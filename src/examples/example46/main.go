package main

import (
	"fmt"
	"time"

	pdfjet "github.com/edragoev1/pdfjet/src"
	"github.com/edragoev1/pdfjet/src/a4"
	"github.com/edragoev1/pdfjet/src/compliance"
)

// Example46 -- TODO:
func Example46() {
	pdf := pdfjet.NewPDFFile("Example_46.pdf", compliance.PDF15)

	f1 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream")
	f2 := pdfjet.NewFontFromFile(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream")
	f3 := pdfjet.NewFontFromFile(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream")

	f1.SetSize(14.0)
	f2.SetSize(14.0)
	f2.SetSize(12.0)

	page := pdfjet.NewPageAddTo(pdf, a4.Portrait)

	paragraphs := make([]*pdfjet.Paragraph, 0)

	paragraph := pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f1, "Όταν ο Βαρουφάκης δήλωνε κατηγορηματικά πως δεν θα ασχοληθεί ποτέ με την πολιτική (Video)"))
	paragraphs = append(paragraphs, paragraph)

	paragraph = pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f2, "Τις τελευταίες μέρες αδιαμφισβήτητα ο Γιάνης Βαρουφάκης είναι  ο πιο πολυσυζητημένος πολιτικός στην Ευρώπη και όχι μόνο. Κι όμως, κάποτε ο νέος υπουργός Οικονομικών δήλωνε κατηγορηματικά πως δεν πρόκειται ποτέ να εμπλακεί σε αυτό το χώρο."))
	paragraphs = append(paragraphs, paragraph)

	paragraph = pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f2, "Η συγκεκριμένη του δήλωση ήταν σε συνέντευξή που είχε παραχωρήσει στην εκπομπή «Ευθέως» και στον Κωνσταντίνο Μπογδάνο, όταν στις 13 Δεκεμβρίου του 2012 ο νυν υπουργός Οικονομικών δήλωνε ότι δεν υπάρχει περίπτωση να ασχοληθεί με την πολιτική, γιατί θα τον διέγραφε οποιοδήποτε κόμμα."))
	paragraph.Add(pdfjet.NewTextLine(f2, "Συγκεκριμένα, μετά από το 43ο λεπτό του βίντεο, ο δημοσιογράφος τον ρώτησε αν θα ασχολιόταν ποτέ με την πολιτική, με την απάντηση του κ. Βαρουφάκη να είναι κατηγορηματική: «Όχι, ποτέ, ποτέ»."))
	paragraphs = append(paragraphs, paragraph)

	paragraph = pdfjet.NewParagraph()
	paragraph.Add(pdfjet.NewTextLine(f2, "«Μα ποτέ δεν ασχολήθηκα με την πολιτική. Ασχολούμαι ως πολίτης και ως συμμετέχων στον δημόσιο διάλογο. Και είναι κάτι που θα κάνω πάντα. Καταρχήν, αν μπω σε ένα πολιτικό κόμμα μέσα σε μία εβδομάδα θα με έχει διαγράψει, όποιο κι αν είναι αυτό», εξηγούσε τότε, ενώ πρόσθετε ότι δεν μπορεί να ακολουθήσει κομματική γραμμή."))
	paragraphs = append(paragraphs, paragraph)

	paragraph = pdfjet.NewParagraph().Add(pdfjet.NewTextLine(f3, "Hello, World!"))
	paragraphs = append(paragraphs, paragraph)

	textArea := pdfjet.NewText(paragraphs)
	textArea.SetLocation(70.0, 70.0)
	textArea.SetWidth(500.0)
	textArea.DrawOn(page)

	pdf.Complete()
}

func main() {
	start := time.Now()
	Example46()
	elapsed := time.Since(start)
	fmt.Printf("Example_46 => %dµs\n", elapsed.Microseconds())
}
