/*
 * Example_46.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_46.cs
 * Using PDF layers (optional content groups) that can be shown or hidden: a
 * shaded relief map of Europe, the lines of latitude and longitude over it,
 * and the capital cities. The map covers 12°W to 42°E and 34°N to 62°N, and
 * longitude and latitude map linearly to x and y on it.
 */
public class Example_46 {
    private readonly float x0 = 50f;       // The top left corner of the map
    private readonly float y0 = 180f;
    private readonly float w = 512f;       // The size of the map
    private readonly float h = 512f * 840f / 1084f;

    private float MapX(float longitude) {
        return x0 + (longitude + 12f) / 54f * w;
    }

    private float MapY(float latitude) {
        return y0 + (62f - latitude) / 28f * h;
    }

    public Example_46() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_46.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "PDF Layers");
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each part of this map is a layer, an optional content group, that "
                + "a PDF viewer lists in its Layers panel and can show or hide: the "
                + "relief, the lines of latitude and longitude, and the capital "
                + "cities. The lines are shown on the screen but not printed.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(512f);
        textBlock.DrawOn(page);

        Image image = new Image(pdf, "images/europe-relief.png");
        image.ResizeWidth(w);
        image.SetLocation(x0, y0);

        // A layer is hidden and not printed unless it is set to be.
        OptionalContentGroup group = new OptionalContentGroup(pdf, "Relief");
        group.SetVisible(true);
        group.SetPrintable(true);
        group.Add(image);
        group.DrawOn(page);

        group = new OptionalContentGroup(pdf, "Latitude and Longitude");
        group.SetVisible(true);
        for (int longitude = -10; longitude <= 40; longitude += 5) {
            Line line = new Line(MapX(longitude), y0, MapX(longitude), y0 + h);
            line.SetStrokeWidth(0.5f);
            line.SetStrokeColor(Color.white);
            group.Add(line);

            String label = (longitude < 0) ? -longitude + "°W" :
                    (longitude > 0) ? longitude + "°E" : "0°";
            text = new TextLine(f1, label);
            text.SetFontSize(8f);
            text.SetTextColor(Color.gray);
            text.SetLocation(MapX(longitude) - text.GetWidth() / 2f, y0 + h + 12f);
            group.Add(text);
        }
        for (int latitude = 35; latitude <= 60; latitude += 5) {
            Line line = new Line(x0, MapY(latitude), x0 + w, MapY(latitude));
            line.SetStrokeWidth(0.5f);
            line.SetStrokeColor(Color.white);
            group.Add(line);

            text = new TextLine(f1, latitude + "°N");
            text.SetFontSize(8f);
            text.SetTextColor(Color.gray);
            text.SetLocation(x0 + w + 4f, MapY(latitude) + 3f);
            group.Add(text);
        }
        group.DrawOn(page);

        String[] cities = {
            "Athens", "Ankara", "Berlin", "Bucharest", "Dublin",
            "Kyiv", "Lisbon", "London", "Madrid", "Oslo",
            "Paris", "Rome", "Stockholm", "Vienna", "Warsaw",
        };
        float[,] locations = {     // Longitude and latitude
            {23.73f, 37.98f}, {32.86f, 39.93f}, {13.40f, 52.52f},
            {26.10f, 44.43f}, {-6.26f, 53.35f}, {30.52f, 50.45f},
            {-9.14f, 38.72f}, {-0.13f, 51.51f}, {-3.70f, 40.42f},
            {10.75f, 59.91f}, { 2.35f, 48.86f}, {12.50f, 41.90f},
            {18.07f, 59.33f}, {16.37f, 48.21f}, {21.01f, 52.23f},
        };
        group = new OptionalContentGroup(pdf, "Capital Cities");
        group.SetVisible(true);
        group.SetPrintable(true);
        for (int i = 0; i < cities.Length; i++) {
            float x = MapX(locations[i, 0]);
            float y = MapY(locations[i, 1]);

            Point point = new Point(x, y);
            point.SetRadius(2.5f);
            point.SetFillColor(Color.white);
            point.SetStrokeColor(Color.black);
            group.Add(point);

            text = new TextLine(f2, cities[i]);
            text.SetFontSize(8f);
            text.SetLocation(x + 5f, y + 3f);
            group.Add(text);
        }
        group.DrawOn(page);

        text = new TextLine(f1, "Relief: Natural Earth, public domain, naturalearthdata.com");
        text.SetFontSize(8f);
        text.SetTextColor(Color.gray);
        text.SetURIAction("https://www.naturalearthdata.com");
        text.SetLocation(x0, y0 + h + 30f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_46();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_46 => {time1 - time0,4} ms");
    }
}   // End of Example_46.cs
