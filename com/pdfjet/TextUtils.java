/**
 * TextUtils.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

public class TextUtils {
    public static void printDuration(String example, long time0, long time1) {
        String duration = String.valueOf(time1 - time0);
        if (duration.length() == 1) {
            duration = "    " + duration;
        } else if (duration.length() == 2) {
            duration = "   " + duration;
        } else if (duration.length() == 3) {
            duration = "  " + duration;
        } else if (duration.length() == 4) {
            duration = " " + duration;
        }
        duration += ".0";
        System.out.println(example + " => " + duration);
    }
}
