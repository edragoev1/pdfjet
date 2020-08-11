/**
 *  RadioButton.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
using System;


namespace PDFjet.NET {
/**
 *  Creates a RadioButton, which can be set selected or unselected.
 *
 */
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
        page.AddBMC(StructElem.SPAN, language, altDescription, actualText);

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
                    altDescription,
                    actualText));
        }

        return new float[] { x + 6*r1 + font.StringWidth(label), y + font.bodyHeight };
    }

}   // End of RadioButton.cs
}   // End of namespace PDFjet.NET
