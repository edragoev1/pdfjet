/*
 * Example_46.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_46.java
 * Using PDF layers (optional content groups) that can be shown or hidden: a
 * shaded relief map of Europe, the lines of latitude and longitude over it,
 * and the capital cities. The map covers 12°W to 42°E and 34°N to 62°N, and
 * longitude and latitude map linearly to x and y on it.
 */
public class Example_46 {
    private final float x0 = 50f;       // The top left corner of the map
    private final float y0 = 180f;
    private final float w = 512f;       // The size of the map
    private final float h = 512f * 840f / 1084f;

    private float mapX(float longitude) {
        return x0 + (longitude + 12f) / 54f * w;
    }

    private float mapY(float latitude) {
        return y0 + (62f - latitude) / 28f * h;
    }

    public Example_46() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_46.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "PDF Layers");
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "Each part of this map is a layer, an optional content group, that "
                + "a PDF viewer lists in its Layers panel and can show or hide: the "
                + "relief, the lines of latitude and longitude, and the capital "
                + "cities. The lines are shown on the screen but not printed.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(512f);
        textBlock.drawOn(page);

        Image image = new Image(pdf, "images/europe-relief.png");
        image.resizeWidth(w);
        image.setLocation(x0, y0);

        // A layer is hidden and not printed unless it is set to be.
        OptionalContentGroup group = new OptionalContentGroup(pdf, "Relief");
        group.setVisible(true);
        group.setPrintable(true);
        group.add(image);
        group.drawOn(page);

        group = new OptionalContentGroup(pdf, "Latitude and Longitude");
        group.setVisible(true);
        for (int longitude = -10; longitude <= 40; longitude += 5) {
            Line line = new Line(mapX(longitude), y0, mapX(longitude), y0 + h);
            line.setStrokeWidth(0.5f);
            line.setStrokeColor(Color.white);
            group.add(line);

            String label = (longitude < 0) ? -longitude + "°W" :
                    (longitude > 0) ? longitude + "°E" : "0°";
            text = new TextLine(f1, label);
            text.setFontSize(8f);
            text.setTextColor(Color.gray);
            text.setLocation(mapX(longitude) - text.getWidth() / 2f, y0 + h + 12f);
            group.add(text);
        }
        for (int latitude = 35; latitude <= 60; latitude += 5) {
            Line line = new Line(x0, mapY(latitude), x0 + w, mapY(latitude));
            line.setStrokeWidth(0.5f);
            line.setStrokeColor(Color.white);
            group.add(line);

            text = new TextLine(f1, latitude + "°N");
            text.setFontSize(8f);
            text.setTextColor(Color.gray);
            text.setLocation(x0 + w + 4f, mapY(latitude) + 3f);
            group.add(text);
        }
        group.drawOn(page);

        String[] cities = {
            "Athens", "Ankara", "Berlin", "Bucharest", "Dublin",
            "Kyiv", "Lisbon", "London", "Madrid", "Oslo",
            "Paris", "Rome", "Stockholm", "Vienna", "Warsaw",
        };
        float[][] locations = {     // Longitude and latitude
            {23.73f, 37.98f}, {32.86f, 39.93f}, {13.40f, 52.52f},
            {26.10f, 44.43f}, {-6.26f, 53.35f}, {30.52f, 50.45f},
            {-9.14f, 38.72f}, {-0.13f, 51.51f}, {-3.70f, 40.42f},
            {10.75f, 59.91f}, { 2.35f, 48.86f}, {12.50f, 41.90f},
            {18.07f, 59.33f}, {16.37f, 48.21f}, {21.01f, 52.23f},
        };
        group = new OptionalContentGroup(pdf, "Capital Cities");
        group.setVisible(true);
        group.setPrintable(true);
        for (int i = 0; i < cities.length; i++) {
            float x = mapX(locations[i][0]);
            float y = mapY(locations[i][1]);

            Point point = new Point(x, y);
            point.setRadius(2.5f);
            point.setFillColor(Color.white);
            point.setStrokeColor(Color.black);
            group.add(point);

            text = new TextLine(f2, cities[i]);
            text.setFontSize(8f);
            text.setLocation(x + 5f, y + 3f);
            group.add(text);
        }
        group.drawOn(page);

        text = new TextLine(f1, "Relief: Natural Earth, public domain, naturalearthdata.com");
        text.setFontSize(8f);
        text.setTextColor(Color.gray);
        text.setURIAction("https://www.naturalearthdata.com");
        text.setLocation(x0, y0 + h + 30f);
        text.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_46();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_46 => %4d ms%n", time1 - time0);
    }
}   // End of Example_46.java
