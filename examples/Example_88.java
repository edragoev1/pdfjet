package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_88.java
 *  Example that shows how to use fallback font and the NotoSans symbols font.
 */
public class Example_88 {
    public Example_88() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_88.pdf")));

        Font f1 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");
        Font f2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");

        f1.setSize(14f);
        f2.setSize(14f);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        TextLine text = new TextLine(f1);
        text.setFallbackFont(f2);
        text.setText("【九霄驚魂】加航機空中發緊急求救信號 折返皮爾遜機場時避飛士嘉堡上空  Traditional Chinese");
        text.setLocation(50f, 100f);
        text.drawOn(page);

        // text = new TextLine(f1);
        // text.setFallbackFont(f2);
        text.setText("美国佛罗里达州长参加竞选下届总统 Simplified Chinese");
        text.setLocation(50f, 150f);
        text.drawOn(page);

        // text = new TextLine(f1);
        // text.setFallbackFont(f2);
        text.setText("「宇宙飛行士」から「あなたと人生を」と求愛され、小惑星の石の輸送費として２０１０万円送金 Japanese");
        text.setLocation(50f, 200f);
        text.drawOn(page);

        // text = new TextLine(f1);
        // text.setFallbackFont(f2);
        text.setText("이번 누리호 3차 발사에선 지난 1·2차와 달리 실용급 위성 등 8기(주탑재위성 1기, 큐브위 Korean");
        text.setLocation(50f, 250f);
        text.drawOn(page);


        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_88();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_88", time0, time1);
    }
}   // End of Example_88.java
