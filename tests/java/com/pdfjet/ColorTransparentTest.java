/*
 * ColorTransparentTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.lang.reflect.Array;
import java.lang.reflect.Field;
import java.lang.reflect.Modifier;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.TreeMap;
import java.util.function.BiConsumer;
import java.util.function.Supplier;
import org.junit.jupiter.api.Test;

/**
 * Each setter of a 0xRRGGBB color leaves the color as it was for
 * Color.transparent, -1, which it drew white, its low 24 bits, before v9.0.3:
 * the object set to red, then to transparent, is the object set to red.
 */
class ColorTransparentTest {
    private static final class Case {
        final String name;
        final Supplier<Object> make;
        final BiConsumer<Object, Integer> set;

        Case(String name, Supplier<Object> make, BiConsumer<Object, Integer> set) {
            this.name = name;
            this.make = make;
            this.set = set;
        }
    }

    @Test
    void everyColorSetterLeavesTheColorForTransparent() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Case> cases = Arrays.asList(
            new Case("Arc.setStrokeColor", Arc::new, (o, c) -> ((Arc) o).setStrokeColor(c)),
            new Case("Arc.setFillColor", Arc::new, (o, c) -> ((Arc) o).setFillColor(c)),
            new Case("Chart.setGridLineColor", () -> new Chart(font, font), (o, c) -> ((Chart) o).setGridLineColor(c)),
            new Case("BarChart.setGridLineColor", () -> new BarChart(font, font), (o, c) -> ((BarChart) o).setGridLineColor(c)),
            new Case("CheckBox.setBorderColor", () -> new CheckBox(font, "A"), (o, c) -> ((CheckBox) o).setBorderColor(c)),
            new Case("CheckBox.setCheckmarkColor", () -> new CheckBox(font, "A"), (o, c) -> ((CheckBox) o).setCheckmarkColor(c)),
            new Case("BaseAnnotation.setFillColor", SquareAnnotation::new, (o, c) -> ((SquareAnnotation) o).setFillColor(c)),
            new Case("Container.setBorderColor", () -> new Container(100f, 50f), (o, c) -> ((Container) o).setBorderColor(c)),
            new Case("Page.setPenColor", () -> page(pdf), (o, c) -> ((Page) o).setPenColor(c)),
            new Case("Page.setBrushColor", () -> page(pdf), (o, c) -> ((Page) o).setBrushColor(c)),
            new Case("Markup.setLinkColor", () -> new Markup(font, font, font, font, font), (o, c) -> ((Markup) o).setLinkColor(c)),
            new Case("Form.setLabelColor", () -> new Form(new ArrayList<>()), (o, c) -> ((Form) o).setLabelColor(c)),
            new Case("Form.setValueColor", () -> new Form(new ArrayList<>()), (o, c) -> ((Form) o).setValueColor(c)),
            new Case("Line.setStrokeColor", () -> new Line(0f, 0f, 10f, 10f), (o, c) -> ((Line) o).setStrokeColor(c)),
            new Case("Point.setStrokeColor", () -> new Point(1f, 1f), (o, c) -> ((Point) o).setStrokeColor(c)),
            new Case("Point.setFillColor", () -> new Point(1f, 1f), (o, c) -> ((Point) o).setFillColor(c)),
            new Case("Paragraph.setTextColor", () -> new Paragraph().add(new TextLine(font, "A")), (o, c) -> ((Paragraph) o).setTextColor(c)),
            new Case("Stamp.setStrokeColor", () -> new Stamp(pdf), (o, c) -> ((Stamp) o).setStrokeColor(c)),
            new Case("Stamp.setFillColor", () -> new Stamp(pdf), (o, c) -> ((Stamp) o).setFillColor(c)),
            new Case("Table.setCellBorderColor", () -> {
                List<List<Cell>> data = new ArrayList<>();
                data.add(new ArrayList<>(Arrays.asList(new Cell(font, "A"))));
                return new Table().setTableData(data, 0);
            }, (o, c) -> ((Table) o).setCellBorderColor(c)),
            new Case("Path.setStrokeColor", Path::new, (o, c) -> ((Path) o).setStrokeColor(c)),
            new Case("Rect.setFillColor", () -> new Rect(0f, 0f, 10f, 10f), (o, c) -> ((Rect) o).setFillColor(c)),
            new Case("Series.setStrokeColor", () -> new Series("A"), (o, c) -> ((Series) o).setStrokeColor(c)));
        List<String> changed = new ArrayList<>();
        for (Case c : cases) {
            Object o = c.make.get();
            c.set.accept(o, Color.red);
            String red = dump(o, 4);
            c.set.accept(o, Color.transparent);
            if (!dump(o, 4).equals(red)) {
                changed.add(c.name);
            }
        }
        assertEquals(new ArrayList<String>(), changed, "Color.transparent changed them");
    }

    private static Page page(PDF pdf) {
        try {
            return new Page(pdf, Letter.PORTRAIT);
        } catch (Exception e) {
            throw new IllegalStateException(e);
        }
    }

    // The object, its private fields too, following references that many
    // levels down, so that a color kept in a cell of a table, or a line of
    // a paragraph, is seen
    private static String dump(Object o, int depth) {
        if (o == null) {
            return "null";
        }
        Class<?> type = o.getClass();
        if (type.isPrimitive() || o instanceof Number || o instanceof Boolean
                || o instanceof Character || o instanceof CharSequence || type.isEnum()) {
            return o.toString();
        }
        if (type.isArray()) {
            StringBuilder sb = new StringBuilder("[");
            for (int i = 0; i < Math.min(Array.getLength(o), 4096); i++) {
                sb.append(dump(Array.get(o, i), depth)).append(' ');
            }
            return sb.append(']').toString();
        }
        if (depth == 0) {
            return "&";
        }
        if (o instanceof Collection<?>) {
            StringBuilder sb = new StringBuilder("[");
            for (Object item : (Collection<?>) o) {
                sb.append(dump(item, depth - 1)).append(' ');
            }
            return sb.append(']').toString();
        }
        if (o instanceof Map<?, ?>) {
            Map<String, String> sorted = new TreeMap<>();
            ((Map<?, ?>) o).forEach((k, v) -> sorted.put(String.valueOf(k), dump(v, depth - 1)));
            return sorted.toString();
        }
        if (!type.getName().startsWith("com.pdfjet")) {
            return type.getSimpleName();
        }
        StringBuilder sb = new StringBuilder("{");
        for (Class<?> t = type; t != null && t != Object.class; t = t.getSuperclass()) {
            for (Field f : t.getDeclaredFields()) {
                if (Modifier.isStatic(f.getModifiers())) {
                    continue;
                }
                try {
                    f.setAccessible(true);
                    sb.append(f.getName()).append(':').append(dump(f.get(o), depth - 1)).append(' ');
                } catch (ReflectiveOperationException | RuntimeException e) {
                    sb.append(f.getName()).append(":? ");
                }
            }
        }
        return sb.append('}').toString();
    }
}
