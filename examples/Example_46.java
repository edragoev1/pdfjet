package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_46.java
 */
public class Example_46 {
    public Example_46() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_46.pdf")),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Bold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream");

        f1.setSize(14f);
        f2.setSize(14f);
        f3.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new ArrayList<Paragraph>();

        Paragraph paragraph = new Paragraph()
                .add(new TextLine(f1,
"Όταν ο Βαρουφάκης δήλωνε κατηγορηματικά πως δεν θα ασχοληθεί ποτέ με την πολιτική (Video)"));
        paragraphs.add(paragraph);

        paragraph = new Paragraph()
                .add(new TextLine(f2,
"Τις τελευταίες μέρες αδιαμφισβήτητα ο Γιάνης Βαρουφάκης είναι  ο πιο πολυσυζητημένος πολιτικός στην Ευρώπη και όχι μόνο. Κι όμως, κάποτε ο νέος υπουργός Οικονομικών δήλωνε κατηγορηματικά πως δεν πρόκειται ποτέ να εμπλακεί σε αυτό το χώρο."));
        paragraphs.add(paragraph);

        paragraph = new Paragraph()
                .add(new TextLine(f2,
"Η συγκεκριμένη του δήλωση ήταν σε συνέντευξή που είχε παραχωρήσει στην εκπομπή «Ευθέως» και στον Κωνσταντίνο Μπογδάνο, όταν στις 13 Δεκεμβρίου του 2012 ο νυν υπουργός Οικονομικών δήλωνε ότι δεν υπάρχει περίπτωση να ασχοληθεί με την πολιτική, γιατί θα τον διέγραφε οποιοδήποτε κόμμα."))
                .add(new TextLine(f2,
"Συγκεκριμένα, μετά από το 43ο λεπτό του βίντεο, ο δημοσιογράφος τον ρώτησε αν θα ασχολιόταν ποτέ με την πολιτική, με την απάντηση του κ. Βαρουφάκη να είναι κατηγορηματική: «Όχι, ποτέ, ποτέ»."));
        paragraphs.add(paragraph);

        paragraph = new Paragraph()
                .add(new TextLine(f2,
"«Μα ποτέ δεν ασχολήθηκα με την πολιτική. Ασχολούμαι ως πολίτης και ως συμμετέχων στον δημόσιο διάλογο. Και είναι κάτι που θα κάνω πάντα. Καταρχήν, αν μπω σε ένα πολιτικό κόμμα μέσα σε μία εβδομάδα θα με έχει διαγράψει, όποιο κι αν είναι αυτό», εξηγούσε τότε, ενώ πρόσθετε ότι δεν μπορεί να ακολουθήσει κομματική γραμμή."));
        paragraphs.add(paragraph);

        paragraph = new Paragraph().add(new TextLine(f3, "Hello, World!"));
        paragraphs.add(paragraph);

        Text textArea = new Text(paragraphs);
        textArea.setLocation(70f, 70f);
        textArea.setWidth(500f);
        textArea.setBorder(true);
        textArea.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_46();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_46", time0, time1);
    }
}   // End of Example_46.java
