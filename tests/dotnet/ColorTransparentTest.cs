/*
 * ColorTransparentTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace PDFjet.NET {
public class ColorTransparentTest {
    // Each setter of a 0xRRGGBB color leaves the color as it was for
    // Color.transparent, -1, which they drew white, its low 24 bits, before
    // v9.0.3: the object set to red, then to transparent, is the object set
    // to red.
    [Fact]
    public void EveryColorSetterLeavesTheColorForTransparent() {
        PDF pdf = TestSupport.NewPDF();
        Font font = TestSupport.Helvetica(pdf);
        var cases = new List<(string name, Func<object> make, Action<object, int> set)> {
            ("Arc.SetStrokeColor", () => new Arc(), (o, c) => ((Arc) o).SetStrokeColor(c)),
            ("Arc.SetFillColor", () => new Arc(), (o, c) => ((Arc) o).SetFillColor(c)),
            ("Chart.SetGridLineColor", () => new Chart(font, font), (o, c) => ((Chart) o).SetGridLineColor(c)),
            ("BarChart.SetGridLineColor", () => new BarChart(font, font), (o, c) => ((BarChart) o).SetGridLineColor(c)),
            ("CheckBox.SetBorderColor", () => new CheckBox(font, "A"), (o, c) => ((CheckBox) o).SetBorderColor(c)),
            ("CheckBox.SetCheckmarkColor", () => new CheckBox(font, "A"), (o, c) => ((CheckBox) o).SetCheckmarkColor(c)),
            ("BaseAnnotation.SetFillColor", () => new SquareAnnotation(), (o, c) => ((SquareAnnotation) o).SetFillColor(c)),
            ("Container.SetBorderColor", () => new Container(100f, 50f), (o, c) => ((Container) o).SetBorderColor(c)),
            ("Page.SetPenColor", () => new Page(pdf, Letter.PORTRAIT), (o, c) => ((Page) o).SetPenColor(c)),
            ("Page.SetBrushColor", () => new Page(pdf, Letter.PORTRAIT), (o, c) => ((Page) o).SetBrushColor(c)),
            ("Markup.SetLinkColor", () => new Markup(font, font, font, font, font), (o, c) => ((Markup) o).SetLinkColor(c)),
            ("Form.SetLabelColor", () => new Form(new List<Field>()), (o, c) => ((Form) o).SetLabelColor(c)),
            ("Form.SetValueColor", () => new Form(new List<Field>()), (o, c) => ((Form) o).SetValueColor(c)),
            ("Line.SetStrokeColor", () => new Line(0f, 0f, 10f, 10f), (o, c) => ((Line) o).SetStrokeColor(c)),
            ("Point.SetStrokeColor", () => new Point(1f, 1f), (o, c) => ((Point) o).SetStrokeColor(c)),
            ("Point.SetFillColor", () => new Point(1f, 1f), (o, c) => ((Point) o).SetFillColor(c)),
            ("Paragraph.SetTextColor", () => new Paragraph(new TextLine(font, "A")), (o, c) => ((Paragraph) o).SetTextColor(c)),
            ("Stamp.SetStrokeColor", () => new Stamp(pdf), (o, c) => ((Stamp) o).SetStrokeColor(c)),
            ("Stamp.SetFillColor", () => new Stamp(pdf), (o, c) => ((Stamp) o).SetFillColor(c)),
            ("Table.SetCellBorderColor", () => new Table().SetTableData(
                    new List<List<Cell>> {new List<Cell> {new Cell(font, "A")}}),
                    (o, c) => ((Table) o).SetCellBorderColor(c)),
            ("Path.SetStrokeColor", () => new Path(), (o, c) => ((Path) o).SetStrokeColor(c)),
            ("Rect.SetFillColor", () => new Rect(0f, 0f, 10f, 10f), (o, c) => ((Rect) o).SetFillColor(c)),
            ("Series.SetStrokeColor", () => new Series("A"), (o, c) => ((Series) o).SetStrokeColor(c)),
        };
        var changed = new List<string>();
        foreach (var (name, make, set) in cases) {
            object o = make();
            set(o, Color.red);
            string red = Dump(o, 4);
            set(o, Color.transparent);
            if (Dump(o, 4) != red) {
                changed.Add(name);
            }
        }
        Assert.True(changed.Count == 0, "Color.transparent changed: " + string.Join(", ", changed));
    }

    // The object, its private fields too, following references that many
    // levels down, so that a color kept in a cell of a table, or a line of a
    // paragraph, is seen
    private static string Dump(object o, int depth) {
        if (o == null) {
            return "null";
        }
        Type t = o.GetType();
        if (t.IsPrimitive || o is string || o is decimal || t.IsEnum) {
            return Convert.ToString(o, System.Globalization.CultureInfo.InvariantCulture);
        }
        if (depth == 0 || o is Delegate || o is System.IO.Stream) {
            return "&";
        }
        if (o is IDictionary map) {
            var items = new List<string>();
            foreach (DictionaryEntry e in map) {
                items.Add(Dump(e.Key, depth - 1) + "=" + Dump(e.Value, depth - 1));
            }
            items.Sort(StringComparer.Ordinal);
            return "{" + string.Join(" ", items) + "}";
        }
        if (o is IEnumerable list) {
            var sb = new StringBuilder("[");
            int n = 0;
            foreach (object item in list) {
                if (n++ == 64) {
                    break;
                }
                sb.Append(Dump(item, depth - 1)).Append(' ');
            }
            return sb.Append(']').ToString();
        }
        var fields = new StringBuilder("{");
        for (Type type = t; type != null && type != typeof(object); type = type.BaseType) {
            foreach (FieldInfo f in type.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                fields.Append(f.Name).Append(':').Append(Dump(f.GetValue(o), depth - 1)).Append(' ');
            }
        }
        return fields.Append('}').ToString();
    }
}
}
