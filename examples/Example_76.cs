using System;
using System.Collections.Generic;
using System.IO;

using PDFjet.NET;


/**
 *  Example_76.cs
 *
 *  This example program will print out all the fonts that are not embedded in the PDF file we read.
 */
class Example_76 {

    public Example_76(String fileName) {

        try {
            PDF pdf = new PDF();

            BufferedStream bis = new BufferedStream(
                    new FileStream(fileName, FileMode.Open, FileAccess.Read));
            List < PDFobj> objects = pdf.Read(bis);
            bis.Close();

            foreach (PDFobj obj in objects) {
                String type = obj.GetValue("/Type");
                if (type.Equals("/Font")
                        && obj.GetValue("/Subtype").Equals("/Type0") == false
                        && obj.GetValue("/FontDescriptor").Equals("")) {

                    Console.WriteLine("Non-EmbeddedFont -> " + obj.GetValue("/BaseFont").Substring(1));
                }
                else if (type.Equals("/FontDescriptor")) {
                    String fontFile = obj.GetValue("/FontFile");
                    if (fontFile.Equals("")) {
                        fontFile = obj.GetValue("/FontFile2");
                    }
                    if (fontFile.Equals("")) {
                        fontFile = obj.GetValue("/FontFile3");
                    }

                    if (fontFile.Equals("")) {
                        Console.WriteLine("Non-EmbeddedFont -> "
                                + obj.GetValue("/FontName").Substring(1));
                    }
                }

            }

        }
        catch (Exception e) {
            Console.WriteLine(e.StackTrace);
        }

    }


    public static void Main(String[] args) {
        if (args.Length == 0) return;

        try {
            new Example_76(args[0]);
        }
        catch (Exception e) {
            Console.WriteLine(e.StackTrace);
        }
    }

}   // End of Example_76.cs
