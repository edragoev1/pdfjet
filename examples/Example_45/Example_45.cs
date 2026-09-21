/*
 * Example_45.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_45.cs
 * Using the Form and Field classes to draw a shipment request. A field at x = 0
 * starts a new row, the other fields of the row start at their own x, and a
 * field with an empty label continues the value above it on a new line.
 */
public class Example_45 {
    public Example_45() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_45.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Shipment Request");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Shipment Request");
        text.SetStructureType(StructElem.H1);
        text.SetFontSize(22f);
        text.SetLocation(56f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "A Form draws its fields in rows. A field at x = 0 starts a new row, "
                + "the other fields of the row start at their own x, and a field with "
                + "an empty label continues the value above it on a new line.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(56f, 95f);
        textBlock.SetWidth(500f);
        float[] xy = textBlock.DrawOn(page);

        float w = 500f; // The width of the form

        List<Field> fields = new List<Field>();
        fields.Add(new Field(   0f, "Sender", "Maple Leaf Instruments Ltd."));
        fields.Add(new Field(   0f, "Street Address", "480 King Street West"));
        fields.Add(new Field(6*w/8, "Suite", "1200"));
        fields.Add(new Field(   0f, "City", "Toronto"));
        fields.Add(new Field(3*w/8, "Province", "Ontario"));
        fields.Add(new Field(5*w/8, "Postal Code", "M5V 1L7"));
        fields.Add(new Field(6*w/8, "Country", "Canada"));
        fields.Add(new Field(   0f, "Recipient", "Nordic Sensor Labs AB"));
        fields.Add(new Field(   0f, "Street Address", "Drottninggatan 55"));
        fields.Add(new Field(6*w/8, "Floor", "3"));
        fields.Add(new Field(   0f, "City", "Stockholm"));
        fields.Add(new Field(5*w/8, "Postal Code", "111 21"));
        fields.Add(new Field(6*w/8, "Country", "Sweden"));
        fields.Add(new Field(   0f, "Contact", "Anna Lindqvist"));
        fields.Add(new Field(3*w/8, "Email", "anna.lindqvist@example.com"));
        fields.Add(new Field(   0f, "Contents", "Two calibrated pressure sensors"));
        fields.Add(new Field(5*w/8, "Weight", "3.2 kg"));
        fields.Add(new Field(6*w/8, "Declared Value", "CAD 1,450.00"));
        fields.Add(new Field(   0f, "Instructions",
                "Keep upright and away from magnets. Deliver on a weekday"));
        fields.Add(new Field(   0f, "", "between 9:00 and 17:00, to the reception on the third floor."));

        xy = new Form(fields)
                .SetLabelFont(f1)
                .SetLabelFontSize(8f)
                .SetLabelColor(Color.gray)
                .SetValueFont(f2)
                .SetValueFontSize(10f)
                .SetValueColor(Color.black)
                .SetLocation(56f, xy[1] + 20f)
                .SetWidth(w)
                .SetStrokeWidth(0.5f)
                .DrawOn(page);

        text = new TextLine(f1, "The recipient signs for the package on delivery.");
        text.SetFontSize(10f);
        text.SetTextColor(Color.gray);
        text.SetLocation(56f, xy[1] + 20f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_45();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_45 => {time1 - time0,4} ms");
    }
}   // End of Example_45.cs
