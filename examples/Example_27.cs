using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_27.cs
 */
public class Example_27 {
    public Example_27() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_27.pdf", FileMode.Create)));

        // Thai font
        Font f1 = new Font(pdf, "fonts/Noto/NotoSansThai-Regular.ttf.stream");
        // Latin font
        Font f2 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");
        // Hebrew font
        Font f3 = new Font(pdf, "fonts/Noto/NotoSansHebrew-Regular.ttf.stream");
        // Arabic font
        Font f4 = new Font(pdf, "fonts/Noto/NotoNaskhArabic-Regular.ttf.stream");

        Page page = new Page(pdf, Letter.PORTRAIT);

        f1.SetSize(14f);
        f2.SetSize(14f);
        f3.SetSize(14f);
        f4.SetSize(12f);

        float x = 50f;
        float y = 50f;

        TextLine text = new TextLine(f1);
        text.SetFallbackFont(f2);
        text.SetLocation(x, y);

        StringBuilder buf = new StringBuilder();
        for (int i = 0x0E01; i < 0x0E5B; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x, y += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            if (i > 0x0E30 && i < 0x0E3B) {
                buf.Append("\u0E01");
            }
            if (i > 0x0E46 && i < 0x0E4F) {
                buf.Append("\u0E2D");
            }
            buf.Append((char) i);
        }

        text.SetText(buf.ToString());
        text.SetLocation(x, y += 20f);
        text.DrawOn(page);

        y += 20f;

        String str = "\u0E1C\u0E1C\u0E36\u0E49\u0E07 abc 123";
        text.SetText(str);
        text.SetLocation(x, y);
        text.DrawOn(page);

        y += 20f;

        str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:";
        str = Bidi.ReorderVisually(str);
        TextLine textLine = new TextLine(f3, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f3.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f3.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f3.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f3.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)";
        str = Bidi.ReorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f3.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        y += 40f;

        str = Bidi.ReorderVisually(
                "قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا");
        textLine = new TextLine(f4, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f4.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.");
        textLine = new TextLine(f4, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f4.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل");
        textLine = new TextLine(f4, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f4.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "من بيجو وسيتروين ودونغفينغ.");
        textLine = new TextLine(f4, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f4.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

        str = Bidi.ReorderVisually(
                "وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.");
        textLine = new TextLine(f4, str);
        textLine.SetFallbackFont(f2);
        textLine.SetLocation(600f - f4.StringWidth(f2, str), y += 20f);
        textLine.DrawOn(page);

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
