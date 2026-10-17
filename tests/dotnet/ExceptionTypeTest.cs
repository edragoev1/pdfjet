/*
 * ExceptionTypeTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using Xunit;

namespace PDFjet.NET {

// The exceptions are of their kind, not a plain Exception, so that a caller
// can tell a file that cannot be read from one PDFjet does not support, or
// an argument that is wrong (GitHub Discussion #5). A catch of Exception
// still catches them all.
public class ExceptionTypeTest {
    [Fact]
    public void ADamagedImageIsInvalidData() {
        byte[] notPng = {0x89, (byte) 'P', (byte) 'N', (byte) 'X', 0, 0, 0, 0};
        Assert.Throws<InvalidDataException>(() => new PNGImage(new MemoryStream(notPng)));
    }

    [Fact]
    public void AnImageCutShortIsTheEndOfTheStream() {
        byte[] bmp = {(byte) 'B', (byte) 'M', 0, 0};
        Assert.Throws<EndOfStreamException>(() => new BMPImage(new MemoryStream(bmp)));
    }

    [Fact]
    public void AWrongArgumentIsAnArgumentException() {
        Page page = new Page(TestSupport.NewPDF(), Letter.PORTRAIT);
        Assert.Throws<ArgumentException>(() => page.SetTextRenderingMode(9));
    }

    [Fact]
    public void AWrongPasswordIsUnauthorizedAccess() {
        using Stream stream = TestSupport.Open("encrypted-files-to-test/Example_30 B mupdf-aes256 open-password-hello.pdf");
        Exception e = Assert.Throws<UnauthorizedAccessException>(() => new PDF().Read(stream, "wrong"));
        Assert.Equal("The password of the PDF is not correct.", e.Message);
    }

    [Fact]
    public void AFontThatIsNoFontIsInvalidData() {
        byte[] junk = new byte[12];
        Assert.Throws<InvalidDataException>(() => new Font(TestSupport.NewPDF(), new MemoryStream(junk)));
    }

    [Fact]
    public void NoPlainExceptionIsThrownByTheLibrary() {
        foreach (string file in Directory.GetFiles(TestSupport.RepoPath("net/pdfjet"), "*.cs", SearchOption.AllDirectories)) {
            // Made by a helper and thrown by its callers too, as the
            // password's and the font's were, which "throw new" missed
            Assert.DoesNotContain("new Exception(", File.ReadAllText(file));
        }
    }
}
}
