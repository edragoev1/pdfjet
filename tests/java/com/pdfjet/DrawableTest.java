/*
 * DrawableTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import com.pdfjet.barcodes.Barcode;
import com.pdfjet.datamatrix.DataMatrix;
import com.pdfjet.pdf417.PDF417;
import com.pdfjet.qrcode.ErrorCorrectionLevel;
import com.pdfjet.qrcode.QRCode;
import java.io.ByteArrayInputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import org.junit.jupiter.api.Test;

/**
 * Every Drawable measures itself with drawOn(null): it draws nothing, returns
 * the corner that drawing returns, and does not change what drawing draws.
 */
class DrawableTest {
    interface Maker {
        Drawable make(PDF pdf, Font font) throws Exception;
    }

    static Map<String, Maker> drawables() {
        Map<String, Maker> m = new LinkedHashMap<String, Maker>();
        m.put("TextBlock", (pdf, font) -> new TextBlock(font, "Hello world, this is a text block that wraps.").setWidth(100f));
        m.put("TextColumn", (pdf, font) -> new TextColumn().setWidth(120f)
                .addParagraph(new Paragraph(new TextLine(font, "Hello column text that wraps around here"))));
        m.put("TextFrame", (pdf, font) -> new TextFrame(font, Arrays.asList("Hello frame text")).setWidth(120f).setHeight(200f));
        m.put("TextLine", (pdf, font) -> new TextLine(font, "Hello line"));
        m.put("CompositeTextLine", (pdf, font) -> new CompositeTextLine(0f, 0f).addComponent(new TextLine(font, "Composite")));
        m.put("Title", (pdf, font) -> new Title(font, "Title", 0f, 0f));
        m.put("Image", (pdf, font) -> new Image(pdf, TestSupport.open("images/up-arrow.png")));
        m.put("SVGImage", (pdf, font) -> new SVGImage(TestSupport.open("images/svg/arrow_forward_FILL0_wght400_GRAD0_opsz48.svg")));
        m.put("Barcode", (pdf, font) -> new Barcode(Barcode.CODE_128, "Hello"));
        m.put("QRCode", (pdf, font) -> new QRCode("Hello", ErrorCorrectionLevel.M));
        m.put("PDF417", (pdf, font) -> new PDF417("Hello"));
        m.put("DataMatrix", (pdf, font) -> new DataMatrix("Hello"));
        m.put("Chart", (pdf, font) -> {
            Chart chart = new Chart(font, font);
            chart.setSize(200f, 150f);
            chart.addSeries("s").addPoint(0f, 0f).addPoint(1f, 2f);
            return chart;
        });
        m.put("BarChart", (pdf, font) -> new BarChart(font, font).setSize(200f, 150f).addSeries("s", new float[] {1f, 2f, 3f}));
        m.put("DonutChart", (pdf, font) -> new DonutChart(font, font).setRadii(50f, 20f)
                .addSlice(new Slice(1f, Color.red, "a")).addSlice(new Slice(2f, Color.green, "b")));
        m.put("Table", (pdf, font) -> {
            List<List<Cell>> data = new ArrayList<List<Cell>>();
            for (int r = 0; r < 3; r++) {
                List<Cell> row = new ArrayList<Cell>();
                for (int c = 0; c < 2; c++) {
                    row.add(new Cell(font, "r" + r + "c" + c));
                }
                data.add(row);
            }
            return new Table().setTableData(data, 1);
        });
        m.put("Rect", (pdf, font) -> new Rect().setSize(50f, 30f).setBorderColor(Color.black));
        m.put("Container", (pdf, font) -> new Container(80f, 40f).add(new Rect().setSize(80f, 40f).setBorderColor(Color.black)));
        m.put("CalendarMonth", (pdf, font) -> new CalendarMonth(font, font, 2026, 9));
        m.put("Form", (pdf, font) -> new Form(Arrays.asList(new Field(0f, "Name", "John"))).setLabelFont(font).setValueFont(font));
        m.put("CheckBox", (pdf, font) -> new CheckBox(font, "Check"));
        m.put("RadioButton", (pdf, font) -> new RadioButton(font, "Radio"));
        m.put("Line", (pdf, font) -> new Line(0f, 0f, 50f, 20f));
        m.put("Arc", (pdf, font) -> new Arc().setRadius(20f).setSweep(90f));
        m.put("Point", (pdf, font) -> new Point(0f, 0f).setRadius(5f));
        m.put("Path", (pdf, font) -> new Path().add(new Point(0f, 0f)).add(new Point(30f, 20f)));
        m.put("Stamp", (pdf, font) -> {
            Stamp stamp = new Stamp(pdf).setSize(50f, 30f);
            stamp.complete();
            return stamp;
        });
        m.put("SquareAnnotation", (pdf, font) -> new SquareAnnotation().setSize(30f, 20f));
        m.put("FileAttachment", (pdf, font) -> new FileAttachment(new EmbeddedFile(
                pdf, "hello.txt", new ByteArrayInputStream("Hello".getBytes("UTF-8")), false)));
        return m;
    }

    private static float[] draw(Maker maker, boolean measureFirst, StringBuilder content) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Drawable drawable = maker.make(pdf, TestSupport.helvetica(pdf));
        drawable.setLocation(40f, 60f);
        if (measureFirst) {
            drawable.drawOn(null);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        float[] xy = drawable.drawOn(page);
        content.append(TestSupport.content(page));
        return xy;
    }

    @Test
    void drawOnNullMeasuresWithoutDrawing() throws Exception {
        List<String> failures = new ArrayList<String>();
        for (Map.Entry<String, Maker> entry : drawables().entrySet()) {
            String name = entry.getKey();
            try {
                PDF pdf = TestSupport.newPDF();
                Drawable drawable = entry.getValue().make(pdf, TestSupport.helvetica(pdf));
                drawable.setLocation(40f, 60f);
                float[] measured = drawable.drawOn(null);

                StringBuilder plain = new StringBuilder();
                float[] drawn = draw(entry.getValue(), false, plain);
                StringBuilder afterMeasuring = new StringBuilder();
                float[] drawnAfterMeasuring = draw(entry.getValue(), true, afterMeasuring);

                if (Math.abs(measured[0] - drawn[0]) > TestSupport.DELTA
                        || Math.abs(measured[1] - drawn[1]) > TestSupport.DELTA) {
                    failures.add(name + ": measured " + Arrays.toString(measured) + ", drawn " + Arrays.toString(drawn));
                }
                if (!plain.toString().equals(afterMeasuring.toString())
                        || !Arrays.equals(drawn, drawnAfterMeasuring)) {
                    failures.add(name + ": measuring first changes what is drawn");
                }
            } catch (Exception e) {
                failures.add(name + ": " + e);
            }
        }
        assertEquals(new ArrayList<String>(), failures);
    }

    @Test
    void tablesAndTextColumnsReturnTheirRightEdge() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Table table = (Table) drawables().get("Table").make(pdf, font);
        table.setLocation(40f, 60f);
        assertEquals(40f + table.getWidth(), table.drawOn(new Page(pdf, Letter.PORTRAIT))[0], TestSupport.DELTA);
        Drawable column = drawables().get("TextColumn").make(pdf, font).setLocation(40f, 60f);
        assertEquals(160f, column.drawOn(new Page(pdf, Letter.PORTRAIT))[0], TestSupport.DELTA);
    }
}
