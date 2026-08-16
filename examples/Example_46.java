package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.encryption.*;

/**
 * Example_46.java
 */
public class Example_46 {
    public Example_46() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_46.pdf")));
        // pdf.setCompliance(Compliance.PDF_UA_1);

        Passwords passwords = new Passwords();
        passwords.setUserPassword("hello");
        passwords.setOwnerPassword("world");

        Permissions permissions = new Permissions();
        permissions.setPermissions(
            UserAccess.PRINT.getValue() |               // Set both to allow the user to print
            UserAccess.PRINT_HIGH_QUALITY.getValue() |  // this document with high quality
            // UserAccess.MODIFY_CONTENTS.getValue() |
            // UserAccess.COPY_CONTENTS.getValue() |
            UserAccess.ASSEMBLE_DOCUMENT.getValue(), true);

        pdf.setEncryption(new Encryption(pdf, passwords, permissions));

        // Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(36f);

        Image image = new Image(pdf, "images/ee-map.png");

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", Compress.NO);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f1, "Hello, World!");
        textLine.setLocation(100f, 100f);
        textLine.drawOn(page);

        image.setLocation(100, 150);
        image.scaleBy(.5f);
        image.drawOn(page);

        // File attachment functionality
        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.setLocation(100f, 550f);
        attachment.setIconPushPin();
        attachment.setIconSize(24f);
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_46();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_46", time0, time1);
    }
}   // End of Example_46.java
