/*
 * ReviewFollowupCultureTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// A document is the same whatever the culture of the thread that makes or
/// reads it: sv-SE writes a negative number with a minus sign of its own,
/// U+2212, de-DE and sv-SE write a decimal comma, and tr-TR has a dotted and
/// a dotless i, so that "Helvetica" in upper case is "HELVETİCA". Each test
/// makes the document in the invariant culture and in those three, and
/// compares the bytes.
/// </summary>
public class ReviewFollowupCultureTest {
    private static readonly string[] CULTURES = {"sv-SE", "de-DE", "tr-TR"};

    private const string SVG =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"40.5\" height=\"30.25\" viewBox=\"-1.5 -2.5 40.5 30.25\">"
            + "<path d=\"M-1.5,2.25 L10.75,-0.5 C12.5,1.5 20.25,-1.75 30.5,25.5 Z\" fill=\"#336699\" "
            + "stroke=\"rgb(10%, 20.5%, 30%)\" stroke-width=\"1.5\" opacity=\"0.75\"/>"
            + "<circle cx=\"-0.5\" cy=\"12.5\" r=\"4.25\" transform=\"translate(2.5,-1.5) rotate(-12.5)\"/>"
            + "</svg>";

    // Runs the operation in the culture, and puts back the culture of the
    // thread after it.
    private static T InCulture<T>(string name, Func<T> operation) {
        CultureInfo culture = CultureInfo.CurrentCulture;
        CultureInfo uiCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo c = (name == null) ? CultureInfo.InvariantCulture : new CultureInfo(name);
            CultureInfo.CurrentCulture = c;
            CultureInfo.CurrentUICulture = c;
            return operation();
        } finally {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
        }
    }

    // The document ID and the creation date, which are different each time,
    // are set to the same, so that the documents can be compared.
    private static PDF FixedPDF(Stream stream) {
        PDF pdf = new PDF(stream);
        typeof(PDF).GetField("uuid", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(pdf, "00112233445566778899aabbccddeeff");
        typeof(PDF).GetField("createDate", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(pdf, "2026-01-01T00:00:00Z");
        return pdf;
    }

    // A page of text in an OpenType font, whose font box and descent are
    // negative, an SVG image of decimal and negative numbers, and lines and a
    // table drawn partly off the page.
    private static byte[] Document() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = FixedPDF(stream);
        Font font = new Font(pdf, TestSupport.Open(IBMPlexSans.Regular));
        font.SetSize(10.5f);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "Culture 1.5 -2.25").SetLocation(50.5f, 60.25f).DrawOn(page);
        page.DrawLine(-10.5f, -20.25f, 100.75f, 80.5f);
        SVGImage image = new SVGImage(new MemoryStream(Encoding.UTF8.GetBytes(SVG)));
        image.SetLocation(-5.5f, 100.25f);
        image.DrawOn(page);
        Table table = new Table();
        List<List<Cell>> data = new List<List<Cell>>();
        data.Add(new List<Cell> {new Cell(font, "-1.5"), new Cell(font, "2,5")});
        data.Add(new List<Cell> {new Cell(font, "3.75"), new Cell(font, "-4")});
        table.SetTableData(data);
        table.SetLocation(-2.5f, 300.5f);
        table.DrawOn(page);
        pdf.Complete();
        return stream.ToArray();
    }

    // The document read, with an OpenType font and a core font added to its
    // page and text drawn in them, as a form is filled in.
    private static byte[] Filled(byte[] document) {
        MemoryStream stream = new MemoryStream();
        PDF pdf = FixedPDF(stream);
        List<PDFobj> objects = pdf.Read(new MemoryStream(document));
        Font plex = new Font(objects, TestSupport.Open(IBMPlexSans.Bold));
        plex.SetSize(11.5f);
        Page page = new Page(pdf, pdf.GetPageObjects(objects)[0]);
        page.AddResource(plex, objects);
        Font font = page.AddResource(CoreFont.HELVETICA, objects).SetSize(12.5f);
        page.DrawString(plex, plex.GetSize(), "Filled in -1.5", 72.5f, 680.75f);
        page.DrawString(font, font.GetSize(), "Filled in", 72.5f, 700.25f);
        page.Complete(objects);
        pdf.AddObjects(objects);
        pdf.Complete();
        return stream.ToArray();
    }

    private static byte[] Encrypted() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = FixedPDF(stream);
        pdf.SetTitle("Encrypted title");
        pdf.SetEncryption(new Encryption(pdf,
                new Passwords().SetUserPassword("hello").SetOwnerPassword("world"),
                new Permissions().Grant(UserAccess.PRINT)));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(new Font(pdf, CoreFont.HELVETICA), "Secret text -1.5")
                .SetLocation(50.5f, -50.25f).DrawOn(page);
        pdf.Complete();
        return stream.ToArray();
    }

    // The objects of the document as text, each with its dictionary and its
    // decrypted stream.
    private static string Decrypted(byte[] document) {
        StringBuilder sb = new StringBuilder();
        foreach (PDFobj obj in TestSupport.Read(document, "hello")) {
            if (obj == null) {
                continue;
            }
            sb.Append(obj.number).Append(": ").Append(string.Join(" ", obj.dict)).Append('\n');
            if (obj.stream != null) {
                sb.Append(TestSupport.Latin1(obj.stream)).Append('\n');
            }
        }
        return sb.ToString();
    }

    [Fact]
    public void ADocumentIsTheSameInEveryCulture() {
        byte[] expected = InCulture(null, Document);
        Assert.Equal(expected, InCulture(null, Document));
        foreach (string culture in CULTURES) {
            Assert.True(expected.AsSpan().SequenceEqual(InCulture(culture, Document)), culture);
        }
    }

    [Fact]
    public void AFilledInDocumentIsTheSameInEveryCulture() {
        byte[] document = InCulture(null, Document);
        byte[] expected = InCulture(null, () => Filled(document));
        Assert.Contains("/HELVETICA ", TestSupport.Latin1(expected));
        foreach (string culture in CULTURES) {
            Assert.True(expected.AsSpan().SequenceEqual(InCulture(culture, () => Filled(document))), culture);
        }
    }

    [Fact]
    public void AnEncryptedDocumentIsReadTheSameInEveryCulture() {
        byte[] document = InCulture(null, Encrypted);
        string expected = InCulture(null, () => Decrypted(document));
        Assert.Contains("feff0045006e0063", expected); // The title, "Enc", decrypted
        foreach (string culture in CULTURES) {
            Assert.Equal(expected, InCulture(culture, () => Decrypted(document)));
            // The access permissions are a negative number, with an ASCII minus
            string made = TestSupport.Latin1(InCulture(culture, Encrypted));
            Assert.Contains("/P -", made);
            Assert.Equal(expected, Decrypted(Encoding.Latin1.GetBytes(made)));
        }
    }
}
}   // End of namespace PDFjet.NET
