package com.pdfjet;


public class FastFloat {
    private static StringBuilder buf = new StringBuilder();

    public static String toString(float f) {
        buf.setLength(0);
        int i = (int) f;
        buf.append(String.valueOf(i));
        float r = f - i;
        if (r < 0.1) {
            buf.append(".0");
        } else if (r < 0.2) {
            buf.append(".1");
        } else if (r < 0.3) {
            buf.append(".2");
        } else if (r < 0.4) {
            buf.append(".3");
        } else if (r < 0.5) {
            buf.append(".4");
        } else if (r < 0.6) {
            buf.append(".5");
        } else if (r < 0.7) {
            buf.append(".6");
        } else if (r < 0.8) {
            buf.append(".7");
        } else if (r < 0.9) {
            buf.append(".8");
        } else  {
            buf.append(".9");
        }
        return buf.toString();
    }
}
