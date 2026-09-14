/*
 * MisuseTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.encryption.Passwords;
import com.pdfjet.encryption.Permissions;
import java.io.ByteArrayOutputStream;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.function.Executable;

/**
 * A program that uses the API the wrong way gets an exception, and complete()
 * then refuses to finish the document, so no broken PDF is written.
 */
class MisuseTest {
    private static final String EARLIER = "The PDF was not completed because of an earlier error: ";

    private static void assertRefused(PDF pdf, String message) {
        IllegalStateException e = assertThrows(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.complete(); }
        });
        assertEquals(EARLIER + message, e.getMessage());
    }

    private static String fails(Class<? extends RuntimeException> type, Executable executable) {
        return assertThrows(type, executable).getMessage();
    }

    @Test
    void aNumberThatIsNotFiniteOrTooLargeIsRefused() throws Exception {
        for (final float bad : new float[] {Float.NaN, Float.POSITIVE_INFINITY, -1e30f, 2147483648f}) {
            final PDF pdf = TestSupport.newPDF();
            final Page page = new Page(pdf, Letter.PORTRAIT);
            String message = fails(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { page.drawLine(10f, 10f, bad, 200f); }
            });
            assertEquals("A coordinate, size or width is NaN, infinite or too large for a PDF.", message);
            assertRefused(pdf, message);
        }
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.drawLine(0f, 0f, 100000000f, 0f);
        assertTrue(TestSupport.content(page).contains("100000000 792 l\n"), TestSupport.content(page));
    }

    @Test
    void aNaNFontSizeIsRefused() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Font font = TestSupport.helvetica(pdf);
        final Page page = new Page(pdf, Letter.PORTRAIT);
        font.setSize(Float.NaN);
        fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { new TextLine(font, "Hello").setLocation(50f, 50f).drawOn(page); }
        });
    }

    @Test
    void aDashPatternIsAnArrayAndAPhase() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setStrokeDashPattern("[] 0");
        page.setStrokeDashPattern("[3 3] 0");
        page.setStrokeDashPattern(" [2.5 .5 1]  0.5 ");
        assertTrue(TestSupport.content(page).endsWith(" [2.5 .5 1]  0.5  d\n"), TestSupport.content(page));
        for (final String bad : new String[] {"3 3", "[3 3]", "[a] 0", "[0 0] 0", "[-1 2] 0", "[3-3] 0", "[1.5.5] 0", "[3 3] 0 ET", ""}) {
            final PDF pdf = TestSupport.newPDF();
            final Page page2 = new Page(pdf, Letter.PORTRAIT);
            String message = fails(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { page2.setStrokeDashPattern(bad); }
            });
            assertEquals("The dash pattern \"" + bad + "\" is not an array of non-negative numbers, "
                    + "not all zero, followed by a phase, such as \"[3 3] 0\".", message);
            assertEquals("", TestSupport.content(page2));
        }
    }

    @Test
    void aNegativePenWidthIsRefused() throws Exception {
        final Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        assertEquals("The pen width cannot be negative.", fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { page.setPenWidth(-2f); }
        }));
        page.setPenWidth(0f);
    }

    @Test
    void theGraphicsStatesMustBePaired() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Page page = new Page(pdf, Letter.PORTRAIT);
        String message = fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { page.restoreGraphicsState(); }
        });
        assertEquals("restoreGraphicsState was called without a matching saveGraphicsState.", message);
        assertEquals("", TestSupport.content(page));

        final PDF pdf2 = TestSupport.newPDF();
        new Page(pdf2, Letter.PORTRAIT).saveGraphicsState();
        message = fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { new Page(pdf2, Letter.PORTRAIT); }
        });
        assertEquals("A page ends with a saveGraphicsState that has no restoreGraphicsState.", message);

        final PDF pdf3 = TestSupport.newPDF();
        new Page(pdf3, Letter.PORTRAIT).saveGraphicsState();
        assertEquals("A page ends with a saveGraphicsState that has no restoreGraphicsState.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf3.complete(); }
        }));
    }

    @Test
    void theMarkedContentMustBePaired() throws Exception {
        for (Compliance compliance : new Compliance[] {Compliance.PDF_1_7, Compliance.PDF_UA_1}) {
            PDF pdf = new PDF(new ByteArrayOutputStream(), compliance);
            final Page page = new Page(pdf, Letter.PORTRAIT);
            String message = fails(IllegalStateException.class, new Executable() {
                public void execute() throws Throwable { page.addEMC(); }
            });
            assertEquals("addEMC was called without a matching addBDC or addArtifactBMC.", message);

            final PDF pdf2 = new PDF(new ByteArrayOutputStream(), compliance);
            new Page(pdf2, Letter.PORTRAIT).addBDC(StructElem.P, "x", "x");
            assertEquals("A page ends with an addBDC or addArtifactBMC that has no addEMC.",
                    fails(IllegalStateException.class, new Executable() {
                public void execute() throws Throwable { pdf2.complete(); }
            }));

            PDF pdf3 = new PDF(new ByteArrayOutputStream(), compliance);
            Page page3 = new Page(pdf3, Letter.PORTRAIT);
            page3.addArtifactBMC();
            page3.addEMC();
            pdf3.complete();
        }
    }

    @Test
    void aWrittenPageCannotBeDrawnOn() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Font font = TestSupport.helvetica(pdf);
        final Page page1 = new Page(pdf, Letter.PORTRAIT);
        new Page(pdf, Letter.PORTRAIT);
        String message = fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { new TextLine(font, "Late").setLocation(50f, 50f).drawOn(page1); }
        });
        assertEquals("The page was already written to the PDF: draw on a page before "
                + "creating the next page or completing the PDF.", message);
        assertRefused(pdf, message);
    }

    @Test
    void completeFinishesADocumentOnce() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        assertEquals("complete() was already called.", fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.complete(); }
        }));
        assertEquals("The PDF was already completed.", fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { new Page(pdf, Letter.PORTRAIT); }
        }));
    }

    @Test
    void aDocumentNeedsAPage() throws Exception {
        PDF pdf = TestSupport.newPDF();
        IllegalStateException e = assertThrows(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.complete(); }
        });
        assertEquals("A PDF needs at least one page.", e.getMessage());
    }

    @Test
    void aPageIsAddedOnceAndToItsOwnDocument() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        pdf.addPage(page);
        assertEquals("The page was already added to the PDF.", fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.addPage(page); }
        }));

        final PDF pdf2 = TestSupport.newPDF();
        final Page foreign = new Page(TestSupport.newPDF(), Letter.PORTRAIT, Page.DETACHED);
        assertEquals("The page belongs to another PDF.", fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { pdf2.addPage(foreign); }
        }));
    }

    @Test
    void fontsImagesStampsAndGroupsBelongToOneDocument() throws Exception {
        final PDF other = TestSupport.newPDF();
        final PDF pdf = TestSupport.newPDF();
        final Page page = new Page(pdf, Letter.PORTRAIT);

        final Font font = TestSupport.helvetica(other);
        assertEquals("The font belongs to another PDF.", fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { new TextLine(font, "Hello").setLocation(50f, 50f).drawOn(page); }
        }));

        final Image image = new Image(other, TestSupport.open("images/map407.png"));
        assertEquals("The image belongs to another PDF.", fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { image.setLocation(50f, 50f).drawOn(page); }
        }));

        final Stamp stamp = new Stamp(other).setSize(50f, 50f);
        stamp.complete();
        assertEquals("The stamp belongs to another PDF.", fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { stamp.drawOn(page); }
        }));
        final Stamp stamp2 = new Stamp(pdf).setSize(50f, 50f);
        assertEquals("The font belongs to another PDF.", fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { stamp2.addFont(font); }
        }));

        final OptionalContentGroup group = new OptionalContentGroup(other, "Layer");
        group.add(new Rect(0f, 0f, 1f, 1f));
        assertEquals("The optional content group belongs to another PDF.",
                fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { group.drawOn(page); }
        }));
    }

    @Test
    void aStampIsCompletedOnceBeforeItIsDrawn() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Page page = new Page(pdf, Letter.PORTRAIT);
        final Stamp stamp = new Stamp(pdf).setSize(50f, 50f);
        assertEquals("Call complete() on the stamp before drawing it.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { stamp.drawOn(page); }
        }));

        final PDF pdf2 = TestSupport.newPDF();
        final Stamp stamp2 = new Stamp(pdf2).setSize(50f, 50f);
        stamp2.complete();
        assertEquals("complete() was already called on the stamp.", fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { stamp2.complete(); }
        }));
        assertEquals("The stamp was already completed.", fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { stamp2.drawRect(0f, 0f, 10f, 10f); }
        }));
    }

    @Test
    void stampTextNeedsAFontAndAText() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        final Stamp stamp = new Stamp(pdf).setSize(100f, 50f);
        for (final TextParameters parameters : new TextParameters[] {
                new TextParameters().setText("Paid"), new TextParameters().setFont(TestSupport.helvetica(pdf))}) {
            assertEquals("Stamp text needs a font and a text.", fails(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { stamp.drawText(parameters); }
            }));
        }
    }

    @Test
    void stampTextAddsItsFont() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        final PDF pdf = new PDF(bos);
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        final Font core = TestSupport.helvetica(pdf);
        final Stamp stamp = new Stamp(pdf).setSize(100f, 50f);
        assertEquals("A stamp draws text with an embedded font, not a core or CJK font.",
                fails(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { stamp.drawText(core, 12f, 5f, 20f, "Paid"); }
        }));
        stamp.drawText(font, 12f, 5f, 20f, "Paid");
        stamp.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("/Font <<\n/F" + font.objNumber + " " + font.objNumber + " 0 R\n"), raw);
    }

    @Test
    void encryptionAndComplianceComeBeforeTheContent() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        TestSupport.helvetica(pdf);
        assertEquals("Set the encryption before adding fonts, images or pages to the PDF.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { new Encryption(pdf, new Passwords(), new Permissions()); }
        }));
        assertEquals("Set the compliance before adding fonts, images or pages to the PDF.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf.setCompliance(Compliance.PDF_UA_1); }
        }));
        pdf.setCompliance(Compliance.PDF_1_7);   // No change.

        final PDF pdf2 = TestSupport.newPDF();
        new Page(pdf2, Letter.PORTRAIT, Page.DETACHED);
        assertEquals("Set the compliance before adding fonts, images or pages to the PDF.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf2.setCompliance(Compliance.PDF_UA_1); }
        }));

        final PDF pdf3 = TestSupport.newPDF();
        final Encryption encryption = new Encryption(pdf3, new Passwords(), new Permissions());
        TestSupport.helvetica(pdf3);
        assertEquals("Set the encryption before adding fonts, images or pages to the PDF.",
                fails(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { pdf3.setEncryption(encryption); }
        }));
    }

    @Test
    void aPageIsFromThreeTo14400PointsWideAndHigh() throws Exception {
        new Page(TestSupport.newPDF(), new PageSize(3f, 3f));
        new Page(TestSupport.newPDF(), new PageSize(14400f, 14400f));
        for (final PageSize bad : new PageSize[] {
                new PageSize(0f, 0f), new PageSize(612f, 2f), new PageSize(14401f, 792f), new PageSize(Float.NaN, 792f)}) {
            final PDF pdf = TestSupport.newPDF();
            assertEquals("A page must be from 3 to 14400 points wide and high.",
                    fails(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { new Page(pdf, bad); }
            }));
        }
    }

    @Test
    void theXmpMetadataLeavesOutControlCharacters() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Report\u0001 2026\uFFFF \uD800");
        new Page(pdf, Letter.PORTRAIT);
        pdf.complete();
        String raw = new String(bos.toByteArray(), "UTF-8");
        assertTrue(raw.contains("<rdf:li xml:lang=\"x-default\">Report 2026 </rdf:li>"), raw);
    }

    @Test
    void aZeroSizeImageStampOrContainerDrawsNothing() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Image image = new Image(pdf, TestSupport.open("images/map407.png"));
        image.scaleBy(0f);
        image.setLocation(50f, 50f).drawOn(page);
        Stamp stamp = new Stamp(pdf).setSize(50f, 50f);
        stamp.complete();
        stamp.scaleBy(0f).setLocation(50f, 50f).drawOn(page);
        Container container = new Container(100f, 100f);
        container.add(new Rect(0f, 0f, 10f, 10f));
        container.scaleBy(0f).setLocation(50f, 50f).drawOn(page);
        assertFalse(TestSupport.content(page).contains(" cm\n"), TestSupport.content(page));
    }
}
