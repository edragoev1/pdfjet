/*
 * ShapesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

/** Line, Rect, Arc, Path, RadioButton, CheckBox and CalendarMonth locations and corners. */
class ShapesTest {
    private Page page() throws Exception {
        return new Page(TestSupport.newPDF(), Letter.PORTRAIT);
    }

    @Test
    void lineSetLocationMovesTheWholeLine() throws Exception {
        Line line = new Line(10f, 10f, 50f, 30f).setLocation(100f, 100f);
        assertEquals(100f, line.getStartPoint().getX(), 0f);
        assertEquals(100f, line.getStartPoint().getY(), 0f);
        assertEquals(140f, line.getEndPoint().getX(), 0f);
        assertEquals(120f, line.getEndPoint().getY(), 0f);
        TestSupport.assertXY(140f, 120f, line.drawOn(page()));
    }

    @Test
    void rectScaleByKeepsTheLocation() throws Exception {
        TestSupport.assertXY(70f, 100f, new Rect(10f, 20f, 30f, 40f).scaleBy(2f).drawOn(page()));
    }

    @Test
    void arcDrawOnReturnsTheBottomRightCornerOfItsCircle() throws Exception {
        Arc arc = new Arc().setLocation(100f, 100f).setRadius(20f).setStartAngle(0f).setSweepDegreesCW(90f);
        TestSupport.assertXY(120f, 120f, arc.drawOn(page()));
    }

    @Test
    void pathSetLocationSetsTheOffsetInsteadOfAddingToIt() throws Exception {
        Path path = new Path().add(new Point(0f, 0f)).add(new Point(10f, 20f));
        path.setLocation(5f, 5f);
        path.setLocation(5f, 5f);
        TestSupport.assertXY(15f, 25f, path.drawOn(page()));
    }

    @Test
    void radioButtonAndCheckBoxCorners() throws Exception {
        Page page = page();
        Font font = TestSupport.helvetica(page.pdf);
        RadioButton radio = new RadioButton(font, "rb");
        radio.setLocation(1f, 2f);
        TestSupport.assertXY(45.184f, 15.872f, radio.drawOn(page));
        TestSupport.assertXY(47.188f, 15.872f, new CheckBox(font, "cb").setLocation(1f, 2f).drawOn(page));
    }

    @Test
    void calendarMonthStartsAtTheOriginWithCellsFromTheDayNames() throws Exception {
        Page page = page();
        Font font = TestSupport.helvetica(page.pdf);
        // February and March 2026 start on a Sunday.
        TestSupport.assertXY(252f, 252f, new CalendarMonth(font, font, 2026, 2).drawOn(page));
        TestSupport.assertXY(252f, 252f, new CalendarMonth(font, font, 2026, 3).drawOn(page));
    }

    @Test
    void colorsAreSetAsAnIntOrAsAnArray() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        new Line(10f, 10f, 50f, 10f).setStrokeColor(Color.red).drawOn(page);
        new Line(10f, 20f, 50f, 20f).setStrokeColor(new float[] {0f, 0f, 1f}).drawOn(page);
        Path path = new Path();
        path.add(new Point(10f, 30f));
        path.add(new Point(50f, 30f));
        path.setStrokeColor(new float[] {0f, 1f, 0f}).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("1 0 0 RG"), content);
        assertTrue(content.contains("0 0 1 RG"), content);
        assertTrue(content.contains("0 1 0 RG"), content);
    }
}
