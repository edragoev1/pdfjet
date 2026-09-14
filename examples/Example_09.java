package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_09.java
 */
final public class Example_09 {
    public Example_09() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_09.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Bold);
        f1.setSize(8f);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(8f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Chart chart = new Chart(f1, f2);
        chart.setData(getData("data/world-communications.txt", "|"));
        chart.setLocation(70f, 50f);
        chart.setSize(500f, 300f);
        chart.setTitle("World View - Communications");
        chart.setXAxisTitle("Cell phones per capita");
        chart.setYAxisTitle("Internet users % of the population");
        addTrendLine(chart);
        chart.drawOn(page);

        f1.setSize(7f);
        f2.setSize(7f);
        addTableToChart(page, chart, f1, f2);

        pdf.complete();
    }

    public void addTrendLine(Chart chart) {
        List<Point> points = chart.getData().get(0);

        float m = slope(points);
        float b = intercept(points, m);

        List<Point> trendLine = new ArrayList<Point>();
        float x = 0.0f;
        float y = m * x + b;
        Point p1 = new Point(x, y);
        p1.setDrawPath(true);
        p1.setStrokeColor(Color.blue);
        p1.setShape(Shape.INVISIBLE);

        x = 1.5f;
        y = m * x + b;
        Point p2 = new Point(x, y);
        p2.setShape(Shape.INVISIBLE);

        trendLine.add(p1);
        trendLine.add(p2);

        chart.getData().add(trendLine);
    }

    public void addTableToChart(
            Page page, Chart chart, Font f1, Font f2) throws Exception {
        Table table = new Table();
        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        List<Point> points = chart.getData().get(0);
        for (int i = 0; i < points.size(); i++) {
            Point point = points.get(i);
            if (point.getShape() != Shape.CIRCLE) {
                List<Cell> tableRow = new ArrayList<Cell>();

                point.setRadius(2f);
                point.setAlignment(Alignment.LEFT);

                Cell cell = new Cell(f2);
                cell.setMarker(point);
                cell.setText("");

                tableRow.add(cell);

                cell = new Cell(f1);
                cell.setText(point.getText());
                tableRow.add(cell);

                cell = new Cell(f2);
                cell.setText(point.getURIAction());
                tableRow.add(cell);

                tableData.add(tableRow);
            }
        }
        table.setData(tableData);
        table.autoAdjustColumnWidths();
        table.setCellBorderWidth(0.2f);
        table.setLocation(70f, 360f);
        table.setColumnWidth(0, 9f);
        table.drawOn(page);
    }

    public List<List<Point>> getData(
            String fileName,
            String delimiter) throws Exception {
        List<List<Point>> chartData = new ArrayList<List<Point>>();

        List<Point> points = new ArrayList<Point>();
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

                Point point = new Point();
                try {
                    double population =
                            Double.valueOf(cols[1].replace(",", ""));
                    point.setText(cols[0].trim());
                    String country_name = point.getText();
                    country_name = country_name.replace(" ", "_");
                    country_name = country_name.replace("'", "_");
                    country_name = country_name.replace(",", "_");
                    country_name = country_name.replace("(", "_");
                    country_name = country_name.replace(")", "_");
                    point.setURIAction("http://pdfjet.com/country/" + country_name + ".txt");
                    point.setX((float) (Double.valueOf(cols[5].replace(",", "")) / population));
                    point.setY((float) (Double.valueOf(cols[7].replace(",", "")) / population * 100));

                    point.setRadius(2f);
                    point.setStrokeColor(Color.gray);

                    if (point.getX() > 1.25f) {
                        point.setShape(Shape.RIGHT_ARROW);
                        point.setStrokeColor(Color.black);
                    } else if (point.getY() > 80f) {
                        point.setShape(Shape.UP_ARROW);
                        point.setStrokeColor(Color.blue);
                    } else if (point.getText().equals("France")) {
                        point.setShape(Shape.MULTIPLY);
                        point.setStrokeColor(Color.green);
                    } else if (point.getText().equals("Canada")) {
                        point.setShape(Shape.BOX);
                        point.setStrokeColor(Color.orange);
                    } else if (point.getText().startsWith("United States")) {
                        point.setShape(Shape.STAR);
                        point.setStrokeColor(Color.red);
                    }

                    points.add(point);
                } catch (Exception e) {
                }
            }
        } finally {
            reader.close();
        }
        chartData.add(points);

        return chartData;
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
        System.out.println("Example_09 => " + (time1 - time0) + " ms");
    }
}   // End of Example_09.java
