using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_28.cs
 *  Example that shows how to use the NotoSansSymbols font.
 */
public class Example_28 {
    public Example_28() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_28.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, "fonts/NotoSansSymbols/NotoSansSymbols-Regular.ttf.stream");
        f1.SetSize(28f);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        float x = 35f;
        float y = 55f;
        float dy = 35f;

        DrawLineOfText(page, f1, x, y, 0x0041, 0x005A);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x0061, 0x007A);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x24B6, 0x24CF);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x24D0, 0x24E9);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x24F5, 0x24FE);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x2624, 0x262F);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x2638, 0x2653);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x2669, 0x267E);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x2690, 0x26A9);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x26AD, 0x26BC);
        y += dy;

        DrawLineOfText(page, f1, x, y, 0x26E2, 0x26FE);
        y += dy;

        pdf.Complete();
    }

    private void DrawLineOfText(
            Page page, Font f1, float x, float y, int c1, int c2) {
        StringBuilder buf = new StringBuilder();
        for (int i = c1; i <= c2; i++) {
            buf.Append((char) i);
        }
        TextLine text = new TextLine(f1);
        text.SetText(buf.ToString());
        text.SetLocation(x, y);
        text.DrawOn(page);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_28();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_28", time0, time1);
    }
}   // End of Example_28.cs
