/**
 *  CheckBox.cs
 *
Copyright 2020 Innovatics Inc.

Portions provided by Shirley C. Christenson
Shirley Christenson Consulting

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
 *  Creates a CheckBox, which can be set checked or unchecked.
 *  By default the check box is unchecked.
 */
public class CheckBox : IDrawable {

    private float x;
    private float y;
    private float w;
    private float h;
    private int boxColor = Color.black;
    private int checkColor = Color.blue;
    private float penWidth;
    private float checkWidth;
    private int mark = 0;
    private Font font = null;
    private String label = "";
    private String uri = null;

    private String language = null;
    private String altDescription = Single.space;
    private String actualText = Single.space;


    /**
     *  Creates a CheckBox with blue check mark.
     *
     */
    public CheckBox(Font font, String label) {
        this.font = font;
        this.label = label;
    }


    /**
     *  Sets the font size to use for this text line.
     *
     *  @param fontSize the fontSize to use.
     *  @return this CheckBox.
     */
    public CheckBox SetFontSize(float fontSize) {
        this.font.SetSize(fontSize);
        return this;
    }


    /**
     *  Sets the color of the check box.
     *
     *  @param boxColor the check box color specified as an 0xRRGGBB integer.
     *  @return this CheckBox.
     */
    public CheckBox SetBoxColor(int boxColor) {
        this.boxColor = boxColor;
        return this;
    }


    /**
     *  Sets the color of the check mark.
     *
     *  @param checkColor the check mark color specified as an 0xRRGGBB integer.
     *  @return this CheckBox.
     */
    public CheckBox SetCheckmark(int checkColor) {
        this.checkColor = checkColor;
        return this;
    }


    /**
     *  Set the x,y position on the Page.
     *
     *  @param x the x coordinate on the Page.
     *  @param y the y coordinate on the Page.
     *  @return this CheckBox.
     */
    public void SetPosition(double x, double y) {
        SetLocation((float) x, (float) y);
    }


    public void SetPosition(float x, float y) {
        SetLocation(x, y);
    }


    public CheckBox SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }


    /**
     *  Set the x,y location on the Page.
     *
     *  @param x the x coordinate on the Page.
     *  @param y the y coordinate on the Page.
     *  @return this CheckBox.
     */
    public CheckBox SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }


    /**
     *  Gets the height of the CheckBox.
     *
     */
    public float GetHeight() {
        return this.h;
    }


    /**
     *  Gets the width of the CheckBox.
     *
     */
    public float GetWidth() {
        return this.w;
    }


    /**
     *  Checks or unchecks this check box. See the Mark class for available options.
     *
     *  @return this CheckBox.
     */
    public CheckBox Check(int mark) {
        this.mark = mark;
        return this;
    }


    /**
     *  Sets the URI for the "click text line" action.
     *
     *  @param uri the URI.
     *  @return this CheckBox.
     */
    public CheckBox SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }


    /**
     *  Sets the alternate description of this check box.
     *
     *  @param altDescription the alternate description of the check box.
     *  @return this CheckBox.
     */
    public CheckBox SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }


    /**
     *  Sets the actual text for this check box.
     *
     *  @param actualText the actual text for the check box.
     *  @return this CheckBox.
     */
    public CheckBox SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }


    /**
     *  Draws this CheckBox on the specified Page.
     *
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.SPAN, language, altDescription, actualText);

        this.w = font.GetAscent();
        this.h = this.w;
        this.penWidth = this.w/15;
        this.checkWidth = this.w/5;

        float yBox = y;
        page.SetPenWidth(penWidth);
        page.SetPenColor(boxColor);
        page.SetLinePattern("[] 0");
        page.DrawRect(x + this.penWidth, yBox + this.penWidth, w, h);

        if (mark == Mark.CHECK || mark == Mark.X) {
            page.SetPenWidth(checkWidth);
            page.SetPenColor(checkColor);
            if (mark == Mark.CHECK) {
                // Draw check mark
                page.MoveTo(x + checkWidth + penWidth, yBox + h/2 + penWidth);
                page.LineTo((x + w/6 + checkWidth) + penWidth, ((yBox + h) - 4f*checkWidth/3f) + penWidth);
                page.LineTo(((x + w) - checkWidth) + penWidth, (yBox + checkWidth) + penWidth);
                page.StrokePath();
            }
            else {
                // Draw 'X' mark
                page.MoveTo(x + checkWidth + penWidth, yBox + checkWidth + penWidth);
                page.LineTo(((x + w) - checkWidth) + penWidth, ((yBox + h) - checkWidth) + penWidth);
                page.MoveTo(((x + w) - checkWidth) + penWidth, (yBox + checkWidth) + penWidth);
                page.LineTo((x + checkWidth) + penWidth, ((yBox + h) - checkWidth) + penWidth);
                page.StrokePath();
            }
        }

        if (uri != null) {
            page.SetBrushColor(Color.blue);
        }
        page.DrawString(font, label, x + 3f*w/2f, y + font.ascent);
        page.SetPenWidth(0f);
        page.SetPenColor(Color.black);
        page.SetBrushColor(Color.black);

        page.AddEMC();

        if (uri != null) {
            page.AddAnnotation(new Annotation(
                    uri,
                    null,
                    x + 3f*w/2f,
                    y,
                    x + 3f*w/2f + font.StringWidth(label),
                    y + font.bodyHeight,
                    language,
                    altDescription,
                    actualText));
        }

        return new float[] { x + 3f*w + font.StringWidth(label), y + font.bodyHeight };
    }

}   // End of CheckBox.java
}   // End of namespace PDFjet.NET
