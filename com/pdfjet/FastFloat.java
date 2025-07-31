package com.pdfjet;

public class FastFloat {
    public static void toString(PDF pdf, float f) {
        try {
            int i = (int) f;
            pdf.append(i);
            float r = f - i;
            if (r < 0.1) {
                pdf.append(".0");
            } else if (r < 0.2) {
                pdf.append(".1");
            } else if (r < 0.3) {
                pdf.append(".2");
            } else if (r < 0.4) {
                pdf.append(".3");
            } else if (r < 0.5) {
                pdf.append(".4");
            } else if (r < 0.6) {
                pdf.append(".5");
            } else if (r < 0.7) {
                pdf.append(".6");
            } else if (r < 0.8) {
                pdf.append(".7");
            } else if (r < 0.9) {
                pdf.append(".8");
            } else  {
                pdf.append(".9");
            }
        } catch (Exception e) {
        }
    }
}
