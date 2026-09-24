/*
 * AssociatedFileTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace PDFjet.NET {
/// <summary>The files a document carries with it, which PDF/A-3 calls associated files.</summary>
public class AssociatedFileTest {
    private const string XML = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<invoice/>\n";

    private static MemoryStream Bytes(string text) {
        return new MemoryStream(Encoding.UTF8.GetBytes(text));
    }

    private static EmbeddedFile Attach(PDF pdf, string fileName, string text) {
        return new EmbeddedFile(pdf, fileName, Bytes(text), false,
                "text/xml", Relationship.ALTERNATIVE, "The invoice, as data.");
    }

    // A document of one page that carries the files.
    private static string Document(PDF pdf, MemoryStream stream, params string[] fileNames) {
        foreach (string fileName in fileNames) {
            pdf.AddAssociatedFile(Attach(pdf, fileName, XML));
        }
        new TextLine(TestSupport.Helvetica(pdf), "Invoice")
                .SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        return TestSupport.Latin1(stream.ToArray());
    }

    private static string Document(params string[] fileNames) {
        MemoryStream stream = new MemoryStream();
        return Document(new PDF(stream, Compliance.PDF_A_3B), stream, fileNames);
    }

    [Fact]
    public void TheCatalogSaysWhichFilesTheDocumentCarries() {
        string raw = Document("factur-x.xml");
        Match files = Regex.Match(raw, "/AF \\[(\\d+) 0 R\\]");
        Assert.True(files.Success, raw.Substring(raw.LastIndexOf("/Type /Catalog", StringComparison.Ordinal)));
        // The number is the file specification, not the stream of the bytes.
        Assert.True(raw.Contains(files.Groups[1].Value + " 0 obj\n<<\n/Type /Filespec"), files.Groups[1].Value);
    }

    [Fact]
    public void AReaderFindsTheFileByItsName() {
        string raw = Document("factur-x.xml");
        Match names = Regex.Match(raw,
                "/Names <</EmbeddedFiles <</Names \\[<([0-9a-fA-F]+)> (\\d+) 0 R\\]>>>>");
        Assert.True(names.Success, raw.Substring(raw.LastIndexOf("/Type /Catalog", StringComparison.Ordinal)));
        Assert.Equal("factur-x.xml", TestSupport.Utf16Hex(names.Groups[1].Value));
        Assert.Contains("/AF [" + names.Groups[2].Value + " 0 R]", raw);
    }

    [Fact]
    public void TheNamesOfTheFilesAreInOrder() {
        string raw = Document("invoice.xml", "data.xml", "Notes.txt");
        Match names = Regex.Match(raw, "/Names \\[(.+?)\\]>>>>");
        Assert.True(names.Success, raw.Substring(raw.LastIndexOf("/Type /Catalog", StringComparison.Ordinal)));
        StringBuilder order = new StringBuilder();
        foreach (Match name in Regex.Matches(names.Groups[1].Value, "<([0-9a-fA-F]+)>")) {
            order.Append(TestSupport.Utf16Hex(name.Groups[1].Value)).Append(' ');
        }
        Assert.Equal("Notes.txt data.xml invoice.xml ", order.ToString());
        // The /AF array is the order the files were added in, which the
        // specification leaves to the writer of the document.
        Assert.Equal(3, raw.Split("/Type /Filespec").Length - 1);
    }

    [Fact]
    public void TheFileSaysWhatItHoldsAndHowItRelatesToTheDocument() {
        string raw = Document("factur-x.xml");
        int filespec = raw.IndexOf("/Type /Filespec", StringComparison.Ordinal);
        string dictionary = raw.Substring(filespec,
                raw.IndexOf("endobj", filespec, StringComparison.Ordinal) - filespec);
        Assert.True(dictionary.Contains("/AFRelationship /Alternative\n"), dictionary);
        Assert.True(dictionary.Contains("/Desc <"), dictionary);
        int desc = dictionary.IndexOf("/Desc <", StringComparison.Ordinal);
        Assert.Equal("The invoice, as data.", TestSupport.Utf16Hex(
                dictionary.Substring(desc + 6, dictionary.IndexOf('>', desc) + 1 - (desc + 6))));
        // Readers of PDF 1.7 look at /UF first and older ones at /F, and
        // PDF/A-3 asks for both, in the file specification and in /EF.
        Assert.True(dictionary.Contains("/F <"), dictionary);
        Assert.True(dictionary.Contains("/UF <"), dictionary);
        Match stream = Regex.Match(dictionary, "/EF <</F (\\d+) 0 R /UF (\\d+) 0 R>>");
        Assert.True(stream.Success, dictionary);
        Assert.Equal(stream.Groups[1].Value, stream.Groups[2].Value);

        int file = raw.IndexOf(stream.Groups[1].Value + " 0 obj\n<<\n/Type /EmbeddedFile",
                StringComparison.Ordinal);
        string embedded = raw.Substring(file, raw.IndexOf("stream\n", file, StringComparison.Ordinal) - file);
        Assert.True(embedded.Contains("/Subtype /text#2Fxml\n"), embedded);
        Assert.True(embedded.Contains("/Params <</Size " + XML.Length + " /ModDate (D:"), embedded);
        Assert.True(embedded.Contains("/Length " + XML.Length + "\n"), embedded);
    }

    [Fact]
    public void TheSizeOfACompressedFileIsTheSizeItHadBeforeItWasCompressed() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_3B);
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 1000; i++) {
            text.Append("<line>The same line, over and over.</line>\n");
        }
        pdf.AddAssociatedFile(new EmbeddedFile(pdf, "long.xml", Bytes(text.ToString()), true,
                "text/xml", Relationship.DATA, "A long file."));
        new TextLine(TestSupport.Helvetica(pdf), "x")
                .SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.True(raw.Contains("/Params <</Size " + text.Length + " /ModDate (D:"), "the size");
        Assert.True(raw.Contains("/Filter /FlateDecode\n"), "the filter");
        Match length = Regex.Match(raw.Substring(raw.IndexOf("/Type /EmbeddedFile", StringComparison.Ordinal)),
                "/Length (\\d+)\n");
        Assert.True(length.Success);
        Assert.True(int.Parse(length.Groups[1].Value) < text.Length / 10, length.Groups[1].Value);
    }

    [Fact]
    public void TheMediaTypeIsWrittenAsANameWhateverItHolds() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_3B);
        pdf.AddAssociatedFile(new EmbeddedFile(pdf, "sheet.ods", Bytes("x"), false,
                "application/vnd.oasis.opendocument.spreadsheet", Relationship.SOURCE, "A sheet."));
        pdf.AddAssociatedFile(new EmbeddedFile(pdf, "odd.bin", Bytes("x"), false,
                "application/x-(odd) #1", Relationship.SUPPLEMENT, "Something else."));
        new TextLine(TestSupport.Helvetica(pdf), "x")
                .SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.True(raw.Contains(
                "/Subtype /application#2Fvnd.oasis.opendocument.spreadsheet\n"), "the sheet");
        Assert.True(raw.Contains("/Subtype /application#2Fx-#28odd#29#20#231\n"), "the odd one");
        Assert.True(raw.Contains("/AFRelationship /Source\n"), "the source");
        Assert.True(raw.Contains("/AFRelationship /Supplement\n"), "the supplement");
    }

    [Fact]
    public void ADocumentThatCarriesNoFilesSaysNothingAboutThem() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_3B);
        new TextLine(TestSupport.Helvetica(pdf), "x")
                .SetLocation(50f, 50f).DrawOn(new Page(pdf, Letter.PORTRAIT));
        pdf.Complete();
        string raw = TestSupport.Latin1(stream.ToArray());
        Assert.True(!raw.Contains("/AF ["), "the array");
        Assert.True(!raw.Contains("/EmbeddedFiles"), "the names");
    }

    [Fact]
    public void AFileThatSaysNothingAboutItselfIsNotOneTheDocumentCanCarry() {
        PDF pdf = new PDF(new MemoryStream(), Compliance.PDF_A_3B);
        EmbeddedFile file = new EmbeddedFile(pdf, "factur-x.xml", Bytes(XML), false);
        ArgumentException problem = Assert.Throws<ArgumentException>(() => pdf.AddAssociatedFile(file));
        Assert.True(problem.Message.Contains("factur-x.xml"), problem.Message);
        // The file it does not carry is still embedded, for an annotation of a
        // page to point at, and is written without the entries of PDF/A-3.
        Assert.True(!problem.Message.Contains("/AFRelationship"), problem.Message);
    }

    [Fact]
    public void TheLevelOfPdfA3aAndPdfUA1IsBoth() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_3A_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(TestSupport.Helvetica(pdf), "Invoice").SetLocation(50f, 50f).DrawOn(page);
        // Tagged, as PDF/UA asks, where PDF/A-3a alone is not
        Assert.True(TestSupport.Content(page).Contains("BDC"), "the text is not tagged");
        string raw = Document(pdf, stream, "factur-x.xml");
        foreach (string want in new string[] {"<pdfuaid:part>1</pdfuaid:part>", "<pdfaid:part>3</pdfaid:part>", "<pdfaid:conformance>A</pdfaid:conformance>", "<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>", "/StructTreeRoot", "/OutputIntents", "/AF ["}) {
            Assert.True(raw.Contains(want), want);
        }
    }

    [Fact]
    public void TheMetadataHasOneListOfExtensionSchemas() {
        // A document of PDF/A-3a and PDF/UA-1 that adds a list of its own, as
        // Factur-X does, has the PDF/UA identification schema in that list.
        foreach (bool own in new bool[] {false, true}) {
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream, Compliance.PDF_A_3A_UA_1);
            if (own) {
                pdf.AddMetadata("<rdf:Description rdf:about=\"\" xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\">\n" +
                        "  <pdfaExtension:schemas>\n" +
                        "    <rdf:Bag>\n" +
                        "    </rdf:Bag>\n" +
                        "  </pdfaExtension:schemas>\n" +
                        "</rdf:Description>\n");
            }
            string raw = Document(pdf, stream, "factur-x.xml");
            Assert.Equal(1, raw.Split("<pdfaExtension:schemas>").Length - 1);
            Assert.True(raw.Contains("<pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>"), "own list " + own);
        }
    }

    [Fact]
    public void TheDocumentsThatCannotCarryAFileSayTheyCannot() {
        foreach (Compliance compliance in new Compliance[] {
                Compliance.PDF_A_1A, Compliance.PDF_A_1B,
                Compliance.PDF_A_2A, Compliance.PDF_A_2B}) {
            PDF pdf = new PDF(new MemoryStream(), compliance);
            EmbeddedFile file = Attach(pdf, "factur-x.xml", XML);
            InvalidOperationException problem = Assert.Throws<InvalidOperationException>(
                    () => pdf.AddAssociatedFile(file));
            Assert.True(problem.Message.Contains(compliance.ToString()), problem.Message);
        }
        // The documents that can: PDF/A-3, and the ones of no profile at all.
        foreach (Compliance compliance in new Compliance[] {
                Compliance.PDF_A_3A, Compliance.PDF_A_3B,
                Compliance.PDF_1_7, Compliance.PDF_UA_1}) {
            MemoryStream stream = new MemoryStream();
            PDF pdf = new PDF(stream, compliance);
            Document(pdf, stream, "factur-x.xml");
            Assert.True(TestSupport.Latin1(stream.ToArray()).Contains("/AF ["), compliance.ToString());
        }
    }

    [Fact]
    public void TheMetadataCarriesTheDescriptionsOfTheStandardsOfTheDocument() {
        MemoryStream stream = new MemoryStream();
        PDF pdf = new PDF(stream, Compliance.PDF_A_3B);
        pdf.AddMetadata("<rdf:Description rdf:about=\"\" xmlns:fx=\"urn:test:1p0#\">\n"
                + "  <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n"
                + "</rdf:Description>");
        Document(pdf, stream, "factur-x.xml");
        string raw = TestSupport.Latin1(stream.ToArray());
        int description = raw.IndexOf("<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>",
                StringComparison.Ordinal);
        Assert.True(description != -1, "the property is written");
        // Inside the metadata: after the description of the document and
        // before the end of the RDF.
        Assert.True(raw.LastIndexOf("</pdfaid:conformance>", description, StringComparison.Ordinal) != -1,
                "after the document");
        Assert.True(raw.IndexOf("</rdf:RDF>", description, StringComparison.Ordinal) != -1, "before the end");
        Assert.True(raw.IndexOf("</x:xmpmeta>", description, StringComparison.Ordinal) != -1, "inside the metadata");
    }
}
}   // End of namespace PDFjet.NET
