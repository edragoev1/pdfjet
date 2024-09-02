using System;
using System.Collections.Generic;
using System.IO;
using PDFjet.NET;

/**
 *  Example_46.cs
 */
public class Example_46 {
    public Example_46() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_46.pdf", FileMode.Create)),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream");

        f1.SetSize(14f);
        f2.SetSize(14f);
        f3.SetSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new List<Paragraph>();

        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(f1,
"Όταν ο Βαρουφάκης δήλωνε κατηγορηματικά πως δεν θα ασχοληθεί ποτέ με την πολιτική (Video)"));
        paragraphs.Add(paragraph);

        paragraph = new Paragraph()
                .Add(new TextLine(f2,
"Τις τελευταίες μέρες αδιαμφισβήτητα ο Γιάνης Βαρουφάκης είναι  ο πιο πολυσυζητημένος πολιτικός στην Ευρώπη και όχι μόνο. Κι όμως, κάποτε ο νέος υπουργός Οικονομικών δήλωνε κατηγορηματικά πως δεν πρόκειται ποτέ να εμπλακεί σε αυτό το χώρο."));
        paragraphs.Add(paragraph);

        paragraph = new Paragraph()
                .Add(new TextLine(f2,
"Η συγκεκριμένη του δήλωση ήταν σε συνέντευξή που είχε παραχωρήσει στην εκπομπή «Ευθέως» και στον Κωνσταντίνο Μπογδάνο, όταν στις 13 Δεκεμβρίου του 2012 ο νυν υπουργός Οικονομικών δήλωνε ότι δεν υπάρχει περίπτωση να ασχοληθεί με την πολιτική, γιατί θα τον διέγραφε οποιοδήποτε κόμμα."))
                .Add(new TextLine(f2,
"Συγκεκριμένα, μετά από το 43ο λεπτό του βίντεο, ο δημοσιογράφος τον ρώτησε αν θα ασχολιόταν ποτέ με την πολιτική, με την απάντηση του κ. Βαρουφάκη να είναι κατηγορηματική: «Όχι, ποτέ, ποτέ»."));
        paragraphs.Add(paragraph);

        paragraph = new Paragraph()
                .Add(new TextLine(f2,
"«Μα ποτέ δεν ασχολήθηκα με την πολιτική. Ασχολούμαι ως πολίτης και ως συμμετέχων στον δημόσιο διάλογο. Και είναι κάτι που θα κάνω πάντα. Καταρχήν, αν μπω σε ένα πολιτικό κόμμα μέσα σε μία εβδομάδα θα με έχει διαγράψει, όποιο κι αν είναι αυτό», εξηγούσε τότε, ενώ πρόσθετε ότι δεν μπορεί να ακολουθήσει κομματική γραμμή."));
        paragraphs.Add(paragraph);

        paragraph = new Paragraph().Add(new TextLine(f3, "Hello, World!"));

        paragraphs.Add(paragraph);

        Text textArea = new Text(paragraphs);
        textArea.SetLocation(70f, 70f);
        textArea.SetWidth(500f);
        textArea.SetBorder(true);
        textArea.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        System.Diagnostics.Stopwatch sw =
                System.Diagnostics.Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_46();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_46", time0, time1);
    }
}   // End of Example_46.cs
