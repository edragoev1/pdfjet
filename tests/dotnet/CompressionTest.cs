/*
 * CompressionTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// The content of the pages is compressed unless SetCompression(false) is
/// called, and then written as it is, encrypted or not.
/// </summary>
public sealed class CompressionTest {
    // A PDF of one page that draws the text, compressed or not, encrypted or
    // not.
    private static byte[] Pdf(bool compression, bool encrypted) {
        using (TestSupport.AllowCoreFonts()) {
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream);
            if (encrypted) {
                pdf.SetEncryption(new Encryption(pdf, new Passwords(), new Permissions()));
            }
            pdf.SetCompression(compression);
            // Forty lines, as a stream too short to gain is stored as it is
            Page page = new Page(pdf, Letter.PORTRAIT);
            Font font = TestSupport.Helvetica(pdf);
            for (int i = 0; i < 40; i++) {
                new TextLine(font, "Readable").SetLocation(50f, 50f + 15f * i).DrawOn(page);
            }
            pdf.Complete();
            return stream.ToArray();
        }
    }

    // The content of the page, read back from the PDF.
    private static string Content(byte[] bytes) {
        List<PDFobj> objects = TestSupport.Read(bytes, "");
        PDFobj page = new PDF().GetPageObjects(objects)[0];
        return TestSupport.Latin1(page.GetContentObject(objects).GetData());
    }

    [Fact]
    public void TheContentOfThePagesIsCompressedUnlessSetOtherwise() {
        string compressed = TestSupport.Latin1(Pdf(true, false));
        Assert.Contains("/Filter /FlateDecode", compressed);
        Assert.DoesNotContain(TestSupport.Hex("Readable"), compressed);

        string plain = TestSupport.Latin1(Pdf(false, false));
        Assert.DoesNotContain("/Filter /FlateDecode", plain);   // The fonts are core fonts
        Assert.Contains(TestSupport.Hex("Readable"), plain);    // The text as it is drawn
        Assert.Contains("\nBT\n", plain);
    }

    [Fact]
    public void UncompressedContentIsReadBackTheSameEncryptedOrNot() {
        // An encrypted document numbers its font after the encryption, so each
        // is compared with the one compressed the other way.
        string plain = Content(Pdf(true, false));
        Assert.Contains(TestSupport.Hex("Readable"), plain);
        Assert.Equal(plain, Content(Pdf(false, false)));
        string encrypted = Content(Pdf(true, true));
        Assert.Contains(TestSupport.Hex("Readable"), encrypted);
        Assert.Equal(encrypted, Content(Pdf(false, true)));
    }
}
}   // End of namespace PDFjet.NET
