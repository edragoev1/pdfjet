using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_27.cs
 */
public class Example_27 {
    public Example_27() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_27.pdf", FileMode.Create)));

        // Thai font
        Font f1 = new Font(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream");
        f1.SetSize(12f);

        // Hebrew font
        Font f2 = new Font(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream");
        f2.SetSize(12f);

        // Arabic font
        Font f3 = new Font(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream");
        f3.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // Thai text from a file
        TextBlock textBlock = new TextBlock(f1,
                Content.OfTextFile("data/languages/thai.txt"));
        textBlock.SetLocation(30f, 30f);
        textBlock.SetWidth(430f);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetTextPadding(10f);
        float[] xy = textBlock.DrawOn(page);  // Draw the text and get coordinates

        float x = 585f;
        float y = xy[1] + 10f;

        String str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:";
        str = Bidi.ReorderVisually(str);
        TextLine textLine = new TextLine(f2, str);
        textLine.SetLocation(x - f2.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.SetLocation(x - f2.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.SetLocation(x - f2.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.SetLocation(x - f2.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.SetLocation(x - f2.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        y += 60f;
        str = Bidi.ReorderVisually(
                "قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا");
        textLine = new TextLine(f3, str);
        textLine.SetLocation(x - f3.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.");
        textLine = new TextLine(f3, str);
        textLine.SetLocation(x - f3.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل");
        textLine = new TextLine(f3, str);
        textLine.SetLocation(x - f3.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "من بيجو وسيتروين ودونغفينغ.");
        textLine = new TextLine(f3, str);
        textLine.SetLocation(x - f3.StringWidth(str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.");
        textLine = new TextLine(f3, str);
        textLine.SetLocation(x - f3.StringWidth(str), y += 20f);
        textLine.DrawOn(page);
/*
        y += 15f;
        f3.SetSize(14);
        // Arabic text from a file
        textBlock = new TextBlock(f3,
                Content.OfTextFile("data/languages/arabic.txt"));
        textBlock.SetLocation(50f, y);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetWidth(520f);
        textBlock.SetTextIsArabic();
        textBlock.SetTextAlignment(Alignment.RIGHT);
        textBlock.SetTextPadding(10f);
        textBlock.DrawOn(page);
*/
        pdf.Complete();
    }

    public static void Main(String[] args) {
        // Console.WriteLine(Bidi.Reverse("Les Mise\u0301rables"));
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_27();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_27", time0, time1);
    }
}   // End of Example_27.cs
