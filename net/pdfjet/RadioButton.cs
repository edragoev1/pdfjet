/**
 *  RadioButton.cs
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;

/**
 *  Creates a RadioButton, which can be set selected or unselected.
 *
 */
namespace PDFjet.NET {
public class RadioButton : IDrawable {
    private bool selected = false;
    private float x;
    private float y;
    private float r1;
    private float r2;
    private float penWidth;
    private Font font = null;
    private String label = "";
    private String uri = null;
    private String language = null;
    private String altDescription = Single.space;
    private String actualText = Single.space;

    /**
     *  Creates a RadioButton that is not selected.
     *
     */
    public RadioButton(Font font, String label) {
        this.font = font;
        this.label = label;
    }

    /**
     *  Sets the font size to use for this text line.
     *
     *  @param fontSize the fontSize to use.
     *  @return this RadioButton.
     */
    public RadioButton SetFontSize(float fontSize) {
        this.font.SetSize(fontSize);
        return this;
    }

    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }

    /**
     *  Set the x,y position on the Page.
     *
     *  @param x the x coordinate on the Page.
     *  @param y the y coordinate on the Page.
     *  @return this RadioButton.
     */
    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }

    public RadioButton SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    /**
     *  Set the x,y location on the Page.
     *
     *  @param x the x coordinate on the Page.
     *  @param y the y coordinate on the Page.
     *  @return this RadioButton.
     */
    public RadioButton SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     *  Selects or deselects this radio button.
     *
     *  @param selected the selection flag.
     *  @return this RadioButton.
     */
    public RadioButton Select(bool selected) {
        this.selected = selected;
        return this;
    }

    /**
     *  Sets the URI for the "click text line" action.
     *
     *  @param uri the URI.
     *  @return this RadioButton.
     */
    public RadioButton SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     *  Sets the alternate description of this radio button.
     *
     *  @param altDescription the alternate description of the radio button.
     *  @return this RadioButton.
     */
    public RadioButton SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     *  Sets the actual text for this radio button.
     *
     *  @param actualText the actual text for the radio button.
     *  @return this RadioButton.
     */
    public RadioButton SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     *  Draws this RadioButton on the specified Page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);

        this.r1 = font.GetAscent()/2;
        this.r2 = r1/2;
        this.penWidth = r1/10;

        float yBox = y;
        page.SetPenWidth(1f);
        page.SetPenColor(Color.black);
        page.SetLinePattern("[] 0");
        page.SetBrushColor(Color.black);
        page.DrawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r1);

        if (this.selected) {
            page.DrawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r2, Operation.FILL);
        }

        if (uri != null) {
            page.SetBrushColor(Color.blue);
        }
        page.DrawString(font, label, x + 3*r1, y + font.ascent);
        page.SetPenWidth(0f);
        page.SetBrushColor(Color.black);

        page.AddEMC();

        if (uri != null) {
            page.AddAnnotation(new Annotation(
                    uri,
                    null,
                    x + 3*r1,
                    y,
                    x + 3*r1 + font.StringWidth(label),
                    y + font.bodyHeight,
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] { x + 6*r1 + font.StringWidth(label), y + font.bodyHeight };
    }
}   // End of RadioButton.cs
}   // End of namespace PDFjet.NET
