package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;
import com.pdfjet.encryption.*;

/**
 * Example_30.java
 */
public class Example_30 {
    public Example_30() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_30.pdf")));
        // pdf.setCompliance(Compliance.PDF_UA_1);

        Passwords passwords = new Passwords();
        passwords.setUserPassword("hello");
        passwords.setOwnerPassword("world");

        Permissions permissions = new Permissions();
        permissions.grant(
            UserAccess.PRINT.getValue() |               // Set both to allow the user to print
            UserAccess.PRINT_HIGH_QUALITY.getValue() |  // this document with high quality
            // UserAccess.MODIFY_CONTENTS.getValue() |
            // UserAccess.COPY_CONTENTS.getValue() |
            UserAccess.ASSEMBLE_DOCUMENT.getValue());

        pdf.setEncryption(new Encryption(pdf, passwords, permissions));

        // Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(36f);

        Image image = new Image(pdf, "images/ee-map.png");

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", false);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f1, "Hello, World!");
        textLine.setLocation(100f, 100f);
        textLine.drawOn(page);

        image.setLocation(100, 150);
        image.scaleBy(.5f);
        image.drawOn(page);

        // File attachment functionality
        FileAttachment attachment = new FileAttachment(file1);
        attachment.setLocation(100f, 550f);
        attachment.setIconPushpin();
        attachment.setIconSize(24f);
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_30();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_30 => " + (time1 - time0) + " ms");
    }
}   // End of Example_30.java
