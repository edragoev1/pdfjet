package examples;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Paths;

import com.pdfjet.*;

/**
 *  Example_27.java
 */
public class Example_27 {
    public Example_27() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_27.pdf")));
        // Latin font
        Font f1 = new Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf.stream");
        f1.setSize(14f);

        // Thai font
        Font f2 = new Font(pdf, "fonts/NotoSansThai/NotoSansThai-Regular.ttf.stream");
        f2.setSize(12f);

        // Hebrew font
        Font f3 = new Font(pdf, "fonts/NotoSansHebrew/NotoSansHebrew-Regular.ttf.stream");
        f3.setSize(12f);

        // Arabic font
        Font f4 = new Font(pdf, "fonts/NotoSansArabic/NotoSansArabic-Regular.ttf.stream");
        f4.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x = 50f;
        float y = 50f;

        TextBox textBox = new TextBox(f2, new String(
                Files.readAllBytes(Paths.get("data/languages/thai.txt"))));
        textBox.setLocation(50f, 50f);
        textBox.setWidth(430f);
        textBox.drawOn(page);

        y += 200f;

        String str = "כך נראית תחתית הטבלה עם סיום הפלייאוף התחתון:";
        str = Bidi.reorderVisually(str);
        TextLine textLine = new TextLine(f3, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f3.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = "10. הפועל כפר סבא 38 נקודות (הפרש שערים 14-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f3.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = "11. הפועל קריית שמונה 36 נקודות (הפרש שערים 7-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f3.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = "12. הפועל חיפה 34 נקודות (הפרש שערים 10-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f3.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = "13. הפועל עכו 34 נקודות (הפרש שערים 21-)";
        str = Bidi.reorderVisually(str);
        textLine = new TextLine(f3, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f3.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        y += 40f;

        str = Bidi.reorderVisually(
                "قالت شركة PSA بيجو ستروين الفرنسية وشريكتها الصينية شركة دونغفينغ موترز الاربعاء إنهما اتفقتا");
        textLine = new TextLine(f4, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f4.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "على التعاون في تطوير السيارات التي تعمل بالطاقة الكهربائية اعتبارا من عام 2019.");
        textLine = new TextLine(f4, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f4.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "وجاء في تصريح اصدرته في باريس الشركة الفرنسية ان الشركتين ستنتجان نموذجا كهربائيا مشتركا تستخدمه كل");
        textLine = new TextLine(f4, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f4.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "من بيجو وسيتروين ودونغفينغ.");
        textLine = new TextLine(f4, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f4.stringWidth(f2, str), y += 20f);
        textLine.drawOn(page);

        str = Bidi.reorderVisually(
                "وقالت إن الخطة تهدف الى تحقيق عائد يزيد على 100 مليار يوان (15,4 مليار دولار) بحلول عام 2020.");
        textLine = new TextLine(f4, str);
        textLine.setFallbackFont(f2);
        textLine.setLocation(600f - f4.stringWidth(f2, str), y += 20f);
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
