package com.pdfjet;
import java.text.SimpleDateFormat;
import java.util.*;
public class DateCheck {
    public static void main(String[] a) {
        SimpleDateFormat f = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US);
        f.setTimeZone(TimeZone.getTimeZone("UTC"));
        Random r = new Random(1);
        int bad = 0;
        for (int i = 0; i < 100000; i++) {
            long t = (long) (r.nextDouble() * 13600000000000L);
            if (!f.format(new Date(t)).equals(PDF.utcDate(t))) { bad++; if (bad < 3) System.out.println(t + " " + f.format(new Date(t)) + " " + PDF.utcDate(t)); }
        }
        long now = System.currentTimeMillis();
        System.out.println("mismatches: " + bad + ", now: " + PDF.utcDate(now) + " = " + f.format(new Date(now)));
    }
}
