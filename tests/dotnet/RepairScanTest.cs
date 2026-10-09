/*
 * RepairScanTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace PDFjet.NET {
// The repair scan of a PDF whose cross-reference table is broken, which reads
// the PDF by looking for its objects.
public class RepairScanTest {
    private const string INNER = "BT /F1 12 Tf 72 720 Td (x) Tj ET\n"
            + "5 0 obj\n<< /Length 3 >>\nstream\nabc\nendstream\nendobj\n"
            + "q Q";

    // A PDF whose page's content is an unfiltered stream with a wrong /Length
    // that holds an object of an embedded PDF, with a stream of its own.
    private static byte[] PdfWithAnObjectInAStream() {
        StringBuilder sb = new StringBuilder();
        sb.Append("%PDF-1.7\n");
        sb.Append("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        sb.Append("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        sb.Append("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        sb.Append("4 0 obj\n<< /Length 10 >>\nstream\n" + INNER + "\nendstream\nendobj\n");
        sb.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n999999\n%%EOF\n");
        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    [Fact]
    public void AStreamThatHoldsAnObjectIsReadWhole() {
        // An object ended at the next "number generation obj": one in the
        // bytes of a stream, as an embedded PDF has, unfiltered, cut the stream
        // short and was read as an object of the PDF, and the first endstream
        // after the stream, that of the embedded object, was taken for the
        // stream's own.
        List<PDFobj> objects = TestSupport.Read(PdfWithAnObjectInAStream());
        PDFobj content = null;
        foreach (PDFobj obj in objects) {
            if (obj == null) {
                continue;
            }
            Assert.NotEqual(5, obj.number);
            if (obj.number == 4) {
                content = obj;
            }
        }
        Assert.NotNull(content);
        Assert.Equal(INNER, Encoding.Latin1.GetString(content.GetData()));
    }
}
}   // End of namespace PDFjet.NET
