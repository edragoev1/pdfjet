using System;
using System.Collections.Generic;
using System.IO;

using PDFjet.NET;


/**
 *  Example_73.cs
 *
 */
class Example_73 {

    public Example_73() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_73.pdf", FileMode.Create)));

        BufferedStream bis = new BufferedStream(
                new FileStream("data/testPDFs/PDF32000_2008.pdf", FileMode.Open));
        List<PDFobj> objects1 = pdf.Read(bis);
        bis.Close();

        bis = new BufferedStream(
                new FileStream("data/testPDFs/50008-RON.pdf", FileMode.Open));
        List<PDFobj> objects2 = pdf.Read(bis);
        bis.Close();

        pdf.AddObjects(objects1);
        pdf.AddObjects(objects2);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        new Example_73();
    }

}   // End of Example_73.cs
