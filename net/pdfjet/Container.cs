using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class Container : IDrawable {
    public float x;
    public float y;
    public float width;
    public float height;
    public float rotateDegrees;
    public float scaleX;
    public float scaleY;
    private List<IDrawable> elements;
    internal Container parent = null;

    /// <summary>
    /// Creates a new container with the specified width and height.
    /// The container is initialized with:
    /// <list type="bullet">
    ///   <item><description>Rotation set to 0 degrees</description></item>
    ///   <item><description>Scaling factors set to 1.0 for both axes</description></item>
    ///   <item><description>An empty list of drawable elements</description></item>
    /// </list>
    /// </summary>
    /// <param name="width">The width of the container.</param>
    /// <param name="height">The height of the container.</param>
    public Container(float width, float height) {
        this.width = width;
        this.height = height;
        this.rotateDegrees = 0f;
        this.scaleX = 1f;
        this.scaleY = 1f;
        this.elements = new List<IDrawable>();
    }

    /// <summary>
    /// Sets the position of the container on the page.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void SetPosition(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Sets the location of the container on the page (alias for <see cref="SetPosition"/>).
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    public void SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Sets the rotation angle.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees.</param>
    public void SetRotation(double degrees) {
        this.rotateDegrees = (float)degrees;
    }

    public void Rotate(double degrees) {
        this.rotateDegrees = (float)degrees;
    }

    /// <summary>
    /// Sets clockwise rotation.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (clockwise).</param>
    public void SetRotationClockwise(double degrees) {
        this.rotateDegrees = (float)-degrees;
    }

    /// <summary>
    /// Sets counter-clockwise rotation.
    /// </summary>
    /// <param name="degrees">The rotation angle in degrees (counter-clockwise).</param>
    public void SetRotationCounterClockwise(double degrees) {
        this.rotateDegrees = (float)degrees;
    }

    public float[] GetRotationCenter() {
        return new float[] {x + width/2f, y + height/2f};
    }

    /// <summary>
    /// Sets a uniform scaling factor for both X and Y axes.
    /// </summary>
    /// <param name="factor">The scaling factor to apply.</param>
    public void SetScaleFactor(float factor) {
        SetScaleFactorXY(factor, factor);
    }

    /// <summary>
    /// Sets non-uniform scaling factors for the X and Y axes.
    /// </summary>
    /// <param name="sx">The scaling factor for X.</param>
    /// <param name="sy">The scaling factor for Y.</param>
    public void SetScaleFactorXY(float sx, float sy) {
        this.scaleX = sx;
        this.scaleY = sy;
    }

    public void SetBorderColor(int borderColor) {
        Rect rect = new Rect(0f, 0f, width, height);
        rect.SetBorderColor(borderColor);
        this.Add(rect);
    }

    public void AddBorder() {
        Rect rect = new Rect(0f, 0f, width, height);
        rect.SetBorderColor(Color.black);
        this.Add(rect);
    }

    public List<IDrawable> GetElements() {
        return this.elements;
    }

    /// <summary>
    /// Adds a drawable element to this container.
    /// </summary>
    /// <param name="element">The element to add.</param>
    public void Add(IDrawable element) {
        if (element.GetType() == typeof(Container)) {
            ((Container) element).parent = this;
        }
        this.elements.Add(element);
    }

    internal static float[] RotateAroundCenter(float[] point, float[] center, double degrees) {
        double rad = degrees * Math.PI / 180.0; // convert to radians

        // translate to centre
        double dx = (double) (point[0] - center[0]);
        double dy = (double) (point[1] - center[1]);

        // rotate
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double dxRot =  dx * cos - dy * sin;
        double dyRot =  dx * sin + dy * cos;

        // translate back
        double nx = center[0] + dxRot;
        double ny = center[1] + dyRot;

        return new float[] {(float) nx, (float) ny};
    }

    /// <summary>
    /// Draws this container and its child elements onto the page.
    /// </summary>
    /// <param name="page">The <see cref="Page"/> to draw on.</param>
    /// <returns>An array containing the bottom-right position of the container.</returns>
    /// <exception cref="Exception">Thrown if drawing fails.</exception>
    public float[] DrawOn(Page page) {
        page.Append("q\n"); // Save the graphics state

        // 1) Translate container to its final position on the page
        //    This is logically the last transformation, but in PDF it’s applied first
        page.Append("1 0 0 1 ");
        page.Append(this.x);
        page.Append(' ');
        page.Append(-this.y);
        page.Append(" cm\n");

        float cx = width / 2f;
        float cy = height / 2f;

        // 2) Move origin to the center of the container
        //    Needed for rotation and scaling
        //    This transformation is applied on top of the previous translation
        page.Append("1 0 0 1 ");
        page.Append(cx);
        page.Append(' ');
        page.Append(page.height - cy);
        page.Append(" cm\n");

        // 3) Rotate around the container center
        double rad = rotateDegrees * (Math.PI / 180.0);
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);
        page.Append(FastFloat.ToByteArray(cos));
        page.Append(' ');
        page.Append(FastFloat.ToByteArray(sin));
        page.Append(' ');
        page.Append(FastFloat.ToByteArray(-sin));
        page.Append(' ');
        page.Append(FastFloat.ToByteArray(cos));
        page.Append(" 0 0 cm\n");

        // 4) Scale around the container center
        page.Append(scaleX);
        page.Append(' ');
        page.Append('0');
        page.Append(' ');
        page.Append('0');
        page.Append(' ');
        page.Append(scaleY);
        page.Append(' ');
        page.Append('0');
        page.Append(' ');
        page.Append('0');
        page.Append(" cm\n");

        // 5) Move origin back so children can draw using local coordinates (0,0)
        //    Executed last in the stream, but logically the first transformation for child drawing
        page.Append("1 0 0 1 ");
        page.Append(-cx);
        page.Append(' ');
        page.Append(-(page.height - cy));
        page.Append(" cm\n");

        // 6) Draw children elements
        foreach (IDrawable element in elements) {
            if (element.GetType() == typeof(SquareAnnotation) ||
                element.GetType() == typeof(CircleAnnotation) ||
                element.GetType() == typeof(PolygonAnnotation) ||
                element.GetType() == typeof(TextAnnotation)) {
                BaseAnnotation annot = (BaseAnnotation) element;
                annot.container = this;
                annot.point1[0] += x;
                annot.point1[1] += y;
                annot.point2[0] += x;
                annot.point2[1] += y;
                if (this.parent != null) {
                    annot.point1[0] += parent.x;
                    annot.point1[1] += parent.y;
                    annot.point2[0] += parent.x;
                    annot.point2[1] += parent.y;
                }
                annot.Rotate(-rotateDegrees);
            }
            element.DrawOn(page);
        }

        page.Append("Q\n"); // Restore the graphics state

        // Return bottom-right position of container
        return new float[] { this.x + width, this.y + height };
    }
}
}
