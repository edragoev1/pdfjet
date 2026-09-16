/*
 * Example_09.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_09.java
 *
 * Draws an XY chart of the countries of a data file, each a marker that links
 * to a page about the country, with the trend line of the points, and a table
 * of the countries with a marker of their own.
 */
final public class Example_09 {
    /** A country of the data file: its name and its point on the chart. */
    private static final class Country {
        final String name;
        final Point point;
        Country(String name, Point point) {
            this.name = name;
            this.point = point;
        }
    }

    public Example_09() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_09.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(8f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Country> countries = readCountries("data/world-communications.txt", "|");

        Chart chart = new Chart(f1, f2);
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("World View - Communications");
        chart.setXAxisTitle("Cell phones per capita");
        chart.setYAxisTitle("Internet users % of the population");
        Series markers = chart.addSeries("").setStrokeColor(Color.gray);
        for (Country country : countries) {
            markers.addPoint(country.point);
        }
        addTrendLine(chart, countries);
        chart.drawOn(page);

        f1.setSize(7f);
        f2.setSize(7f);
        addTableToChart(page, countries, f1, f2);

        pdf.complete();
    }

    public void addTrendLine(Chart chart, List<Country> countries) {
        List<Point> points = new ArrayList<Point>();
        for (Country country : countries) {
            points.add(country.point);
        }
        float m = slope(points);
        float b = intercept(points, m);
        chart.addSeries("Trend line")
                .setDrawPath(true)
                .setStrokeColor(Color.blue)
                .setShape(Shape.INVISIBLE)
                .addPoint(0f, b)
                .addPoint(1.5f, m * 1.5f + b);
    }

    public void addTableToChart(
            Page page, List<Country> countries, Font f1, Font f2) throws Exception {
        Table table = new Table();
        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        for (Country country : countries) {
            if (country.point.getShape() != Shape.CIRCLE) {
                List<Cell> tableRow = new ArrayList<Cell>();

                Cell cell = new Cell(f2);
                cell.setMarker(country.point, Alignment.LEFT);
                cell.setText("");
                tableRow.add(cell);

                cell = new Cell(f1);
                cell.setText(country.name);
                tableRow.add(cell);

                cell = new Cell(f2);
                cell.setText(country.point.getURIAction());
                tableRow.add(cell);

                tableData.add(tableRow);
            }
        }
        table.setTableData(tableData);
        table.autoAdjustColumnWidths();
        table.setCellBorderWidth(0.2f);
        table.setLocation(70f, 360f);
        table.setColumnWidth(0, 9f);
        table.drawOn(page);
    }

    public List<Country> readCountries(
            String fileName,
            String delimiter) throws Exception {
        List<Country> countries = new ArrayList<Country>();
        BufferedReader reader = null;
        try {
            reader = new BufferedReader(
                    new InputStreamReader(new FileInputStream(fileName), "UTF-8"));
            String line = null;
            while ((line = reader.readLine()) != null) {
                String[] cols = null;
                if (delimiter.equals("|")) {
                    cols = line.split("\\|", -1);
                } else if (delimiter.equals("\t")) {
                    cols = line.split("\t", -1);
                } else {
                    throw new Exception(
                        "Only pipes and tabs can be used as delimiters");
                }

                try {
                    double population =
                            Double.valueOf(cols[1].replace(",", ""));
                    String name = cols[0].trim();
                    String country_name = name;
                    country_name = country_name.replace(" ", "_");
                    country_name = country_name.replace("'", "_");
                    country_name = country_name.replace(",", "_");
                    country_name = country_name.replace("(", "_");
                    country_name = country_name.replace(")", "_");
                    Point point = new Point(
                            (float) (Double.valueOf(cols[5].replace(",", "")) / population),
                            (float) (Double.valueOf(cols[7].replace(",", "")) / population * 100));
                    point.setURIAction("http://pdfjet.com/country/" + country_name + ".txt");
                    point.setRadius(2f);

                    if (point.getX() > 1.25f) {
                        point.setShape(Shape.RIGHT_ARROW);
                        point.setStrokeColor(Color.black);
                    } else if (point.getY() > 80f) {
                        point.setShape(Shape.UP_ARROW);
                        point.setStrokeColor(Color.blue);
                    } else if (name.equals("France")) {
                        point.setShape(Shape.MULTIPLY);
                        point.setStrokeColor(Color.green);
                    } else if (name.equals("Canada")) {
                        point.setShape(Shape.BOX);
                        point.setStrokeColor(Color.orange);
                    } else if (name.startsWith("United States")) {
                        point.setShape(Shape.STAR);
                        point.setStrokeColor(Color.red);
                    }

                    countries.add(new Country(name, point));
                } catch (Exception e) {
                }
            }
        } finally {
            reader.close();
        }
        return countries;
    }

    // The slope and intercept of the ordinary least squares trend line of the points.
    private static float slope(List<Point> points) {
        return covar(points) / devsq(points) * (points.size() - 1);
    }

    private static float intercept(List<Point> points, float slope) {
        float[] mean = mean(points);
        return mean[1] - slope * mean[0];
    }

    private static float[] mean(List<Point> points) {
        float[] mean = new float[2];
        for (Point point : points) {
            mean[0] += point.getX();
            mean[1] += point.getY();
        }
        mean[0] /= points.size();
        mean[1] /= points.size();
        return mean;
    }

    private static float covar(List<Point> points) {
        float covariance = 0f;
        float[] mean = mean(points);
        for (Point point : points) {
            covariance += (point.getX() - mean[0]) * (point.getY() - mean[1]);
        }
        return covariance / (points.size() - 1);
    }

    private static float devsq(List<Point> points) {
        float devsq = 0f;
        float[] mean = mean(points);
        for (Point point : points) {
            devsq = devsq + (float) Math.pow(point.getX() - mean[0], 2);
        }
        return devsq;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_09();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_09 => %4d ms%n", time1 - time0);
    }
}   // End of Example_09.java
