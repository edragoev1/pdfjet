package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_27.java
 */
public class Example_27 {
    public Example_27() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_27.pdf")));
        // Thai font
        Font f1 = new Font(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream");
        f1.setSize(12f);

        // Hebrew font
        Font f2 = new Font(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream");
        f2.setSize(12f);

        // Arabic font
        Font f3 = new Font(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream");
        f3.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x = 50f;
        float y = 50f;

        TextBlock textBlock = new TextBlock(f1, Content.ofTextFile("data/languages/thai.txt"));
        textBlock.setLocation(30f, 30f);
        textBlock.setWidth(430f);
        textBlock.setBorderColor(Color.blue);
        textBlock.setTextPadding(10f);
        textBlock.drawOn(page);

        y += 250f;

        String str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:";
        str = Bidi.reorderVisually(str);
        TextLine textLine = new TextLine(f2, str);
        textLine.setLocation(600f - f2.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.setLocation(600f - f2.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.setLocation(600f - f2.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.setLocation(600f - f2.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f2, str);
        textLine.setLocation(600f - f2.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        y += 40f;

        str = Bidi.reorderVisually(
                "قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا");
        textLine = new TextLine(f3, str);
        textLine.setLocation(600f - f3.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.");
        textLine = new TextLine(f3, str);
        textLine.setLocation(600f - f3.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل");
        textLine = new TextLine(f3, str);
        textLine.setLocation(600f - f3.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "من بيجو وسيتروين ودونغفينغ.");
        textLine = new TextLine(f3, str);
        textLine.setLocation(600f - f3.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.");
        textLine = new TextLine(f3, str);
        textLine.setLocation(600f - f3.stringWidth(str), y += 20f);
        textLine.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_27();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_27", time0, time1);
    }
}   // End of Example_27.java
