/*
 * Example_33.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_33.cs
 * Using the SVGImage component to draw a map of Europe, scaled to fit the page,
 * and a set of icons, each labeled with its name.
 */
public class Example_33 {
    public Example_33() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_33.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, A4.PORTRAIT);

        TextLine text = new TextLine(f2, "SVG Images");
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "SVGImage reads the paths of an SVG file and draws them as vector "
                + "graphics, which stay sharp at any zoom. The map is 1,000 points wide "
                + "in its file and is scaled by 0.5 to fit the page.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(495f);
        float[] xy = textBlock.DrawOn(page);

        SVGImage map = new SVGImage("images/svg-test/europe.svg");
        map.ScaleBy(0.5f);
        map.SetLocation((page.GetWidth() - map.GetWidth()) / 2f, xy[1] + 20f);
        xy = map.DrawOn(page);

        textBlock = new TextBlock(f1,
                "The colors come from the file: the peachpuff fill of the svg element "
                + "for most countries, an aliceblue fill for Spain and an olive "
                + "outline for Austria.");
        textBlock.SetFontSize(10f);
        textBlock.SetTextColor(Color.gray);
        textBlock.SetLocation(50f, xy[1] + 10f);
        textBlock.SetWidth(495f);
        xy = textBlock.DrawOn(page);

        String[] iconFiles = {
            "images/svg/home_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/search_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/settings_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg-test/menu-icon.svg",
            "images/svg-test/menu-icon-close.svg",
            "images/svg-test/test-CS.svg",
            "images/svg-test/test-QQ1.svg",
        };
        String[] iconNames = {
            "home",
            "search",
            "checkout",
            "palette",
            "stories",
            "add",
            "star",
            "settings",
            "menu",
            "close",
            "C and S curves",
            "Q curves",
        };

        // Two rows of six icons, 48 by 48 points each.
        float y = xy[1] + 30f;
        for (int i = 0; i < iconFiles.Length; i++) {
            float x = 50f + (i % 6) * 85f;
            float yIcon = y + (i / 6) * 90f;

            SVGImage icon = new SVGImage(iconFiles[i]);
            icon.SetLocation(x, yIcon);
            float[] iconXY = icon.DrawOn(page);

            text = new TextLine(f1, iconNames[i]);
            text.SetFontSize(9f);
            text.SetTextColor(Color.gray);
            text.SetLocation(x, iconXY[1] + 15f);
            text.DrawOn(page);
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_33();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_33 => {time1 - time0,4} ms");
    }
}   // End of Example_33.cs
