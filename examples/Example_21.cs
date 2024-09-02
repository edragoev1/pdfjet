using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_21.cs
 *
 */
public class Example_21 {

    public Example_21() {

        PDF pdf = new PDF(new FileStream("Example_21.pdf", FileMode.Create));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f1,
                "QR codes encoded with Low, Medium, High and Very High error correction level - C#");
        text.SetLocation(100.0f, 30.0f);
        text.DrawOn(page);

        // Please note:
        // The higher the error correction level - the shorter the string that you can encode.
        QRCode qr = new QRCode(
                "https://kazuhikoarase.github.io/qrcode-generator/js/demo",
                ErrorCorrectLevel.L);   // Low
        qr.SetModuleLength(3f);
        qr.SetLocation(100f, 100f);
        // qr.SetColor(Color.blue);
        qr.DrawOn(page);

        qr = new QRCode(
                "https://github.com/kazuhikoarase/qrcode-generator",
                ErrorCorrectLevel.M);   // Medium
        qr.SetLocation(400f, 100f);
        qr.SetModuleLength(3f);
        qr.DrawOn(page);

        qr = new QRCode(
                "https://github.com/kazuhikoarase/jaconv",
                ErrorCorrectLevel.Q);   // High
        qr.SetLocation(100f, 400f);
        qr.SetModuleLength(3f);
        qr.DrawOn(page);

        qr = new QRCode(
                "https://github.com/kazuhikoarase",
                ErrorCorrectLevel.H);   // Very High
        qr.SetLocation(400f, 400f);
        qr.SetModuleLength(3f);
        qr.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_21();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_21", time0, time1);
    }

}   // End of Example_21.cs
