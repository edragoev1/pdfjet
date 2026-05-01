package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

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
        // Test OTF with CFF outlines!
        // Font f1 = new Font(pdf, "data/SourceSansPro-Regular.otf");
        f1.setSize(36f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f1, "Hello, World!");
        textLine.setLocation(100f, 100f);
        textLine.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_46();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_46", time0, time1);
    }
}   // End of Example_46.java
