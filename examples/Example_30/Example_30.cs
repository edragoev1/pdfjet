using System;
using System.IO;

using PDFjet.NET;

/**
 * Example_30.cs
 */
public class Example_30 {
    public Example_30() {
        PDF pdf = new PDF(new BufferedStream(
            new FileStream("Example_30.pdf", FileMode.Create)));
        // pdf.SetCompliance(Compliance.PDF_UA_1);

        var passwords = new Passwords();
        passwords.SetUserPassword("hello");
        passwords.SetOwnerPassword("world");

        var permissions = new Permissions();
        permissions.Grant(
            UserAccess.PRINT |               // Set both to allow the user to print
            UserAccess.PRINT_HIGH_QUALITY |    // this document with high quality
            // UserAccess.MODIFY_CONTENTS |
            // UserAccess.COPY_CONTENTS |
            UserAccess.ASSEMBLE_DOCUMENT);

        pdf.SetEncryption(new Encryption(pdf, passwords, permissions));

        // Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(36f);

        Image image = new Image(pdf, "images/ee-map.png");

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", false);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f1, "Hello, World!");
        textLine.SetLocation(100f, 100f);
        textLine.DrawOn(page);

        image.SetLocation(100, 150);
        image.ScaleBy(.5f);
        image.DrawOn(page);

        // File attachment functionality
        FileAttachment attachment = new FileAttachment(file1);
        attachment.SetLocation(100f, 550f);
        attachment.SetIconPushpin();
        attachment.SetIconSize(24f);
        attachment.SetTitle("Attached File: " + file1.GetFileName());
        attachment.SetContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        System.Diagnostics.Stopwatch sw =
                System.Diagnostics.Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_30();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_30", time0, time1);
    }
}   // End of Example_30.cs
