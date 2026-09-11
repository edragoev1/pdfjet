using System;
using System.IO;
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
        // Font f1 = new Font(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream");
        Font f1 = new Font(pdf, IBMPlexSansThai.Regular);
        f1.SetSize(12f);

        // Hebrew font
        // Font f2 = new Font(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream");
        Font f2 = new Font(pdf, IBMPlexSansHebrew.Regular);
        f2.SetSize(12f);

        // Arabic font
        // Font f3 = new Font(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream");
        Font f3 = new Font(pdf, IBMPlexSansArabic.Regular);
        f3.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBlock textBlock = new TextBlock(f1,
                Content.OfTextFile("data/languages/thai.txt"));
        textBlock.SetLocation(30f, 30f);
        textBlock.SetWidth(430f);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetTextPadding(10f);
        float[] xy = textBlock.DrawOn(page);

        float x = 570f;
        float y = xy[1] + 55f;

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

        y += 65f;
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

        // Right to left text in text blocks, wrapped at their width.
        page = new Page(pdf, Letter.PORTRAIT);

        textBlock = new TextBlock(f2, Content.OfTextFile("data/languages/hebrew.txt"));
        textBlock.SetLocation(180f, 30f);
        textBlock.SetWidth(400f);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetTextPadding(10f);
        textBlock.SetRightToLeft(true);
        xy = textBlock.DrawOn(page);

        textBlock = new TextBlock(f3, Content.OfTextFile("data/languages/arabic.txt"));
        textBlock.SetLocation(180f, xy[1] + 30f);
        textBlock.SetWidth(400f);
        textBlock.SetBorderColor(Color.blue);
        textBlock.SetTextPadding(10f);
        textBlock.SetRightToLeft(true);
        textBlock.DrawOn(page);

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
