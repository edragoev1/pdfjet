/*
 * AndroidShaper.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.lang.reflect.Constructor;
import java.lang.reflect.Method;
import java.nio.ByteBuffer;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.WeakHashMap;

// The shaper of Android 12 (API 31) and later: Android's own text engine,
// HarfBuzz as in Chrome. StaticLayout wraps the text and TextRunShaper shapes
// each line, with the fonts as a typeface of custom fallbacks: each character
// in the first font that has it.
//
// The classes of Android are reached by name, at run time, so that PDFjet's
// one jar builds with any JDK and runs everywhere: they are only looked up
// on Android.
final class AndroidShaper implements Shaper {
    // Android lays text out in whole pixels: laid out 100 times larger and
    // drawn 100 times smaller, the glyphs fall to a hundredth of a point
    private static final float SCALE = 100f;

    // Whether this is Android 12 or later, which has TextRunShaper
    static boolean available() {
        try {
            Class.forName("android.graphics.text.TextRunShaper");
            return true;
        } catch (ClassNotFoundException | LinkageError e) {
            return false;
        }
    }

    // Whether this is Android at all
    static boolean onAndroid() {
        try {
            Class.forName("android.os.Build");
            return true;
        } catch (ClassNotFoundException | LinkageError e) {
            return false;
        }
    }

    private static final class Api {
        final Constructor<?> fontBuilder;           // Font.Builder(ByteBuffer)
        final Method fontBuild;                     // Font.Builder.build()
        final Constructor<?> familyBuilder;         // FontFamily.Builder(Font)
        final Method familyBuild;                   // FontFamily.Builder.build()
        final Constructor<?> typefaceBuilder;       // Typeface.CustomFallbackBuilder(FontFamily)
        final Method addFallback;                   // CustomFallbackBuilder.addCustomFallback(FontFamily)
        final Method typefaceBuild;                 // CustomFallbackBuilder.build()
        final Constructor<?> textPaint;             // TextPaint()
        final Method setTypeface;                   // Paint.setTypeface(Typeface)
        final Method setTextSize;                   // Paint.setTextSize(float)
        final Method setTextLocale;                 // Paint.setTextLocale(Locale)
        final Method obtain;                        // StaticLayout.Builder.obtain(...)
        final Method layoutBuild;                   // StaticLayout.Builder.build()
        final Method lineCount, lineStart, lineEnd, lineLeft, lineWidth;
        final Method shapeTextRun;                  // TextRunShaper.shapeTextRun(...)
        final Method glyphCount, glyphId, glyphX, glyphY, glyphFont;

        Api() throws Exception {
            Class<?> font = Class.forName("android.graphics.fonts.Font");
            Class<?> fontB = Class.forName("android.graphics.fonts.Font$Builder");
            Class<?> family = Class.forName("android.graphics.fonts.FontFamily");
            Class<?> familyB = Class.forName("android.graphics.fonts.FontFamily$Builder");
            Class<?> typeface = Class.forName("android.graphics.Typeface");
            Class<?> typefaceB = Class.forName("android.graphics.Typeface$CustomFallbackBuilder");
            Class<?> paint = Class.forName("android.graphics.Paint");
            Class<?> textPaintC = Class.forName("android.text.TextPaint");
            Class<?> layoutB = Class.forName("android.text.StaticLayout$Builder");
            Class<?> layout = Class.forName("android.text.Layout");
            Class<?> shaper = Class.forName("android.graphics.text.TextRunShaper");
            Class<?> glyphs = Class.forName("android.graphics.text.PositionedGlyphs");

            fontBuilder = fontB.getConstructor(ByteBuffer.class);
            fontBuild = fontB.getMethod("build");
            familyBuilder = familyB.getConstructor(font);
            familyBuild = familyB.getMethod("build");
            typefaceBuilder = typefaceB.getConstructor(family);
            addFallback = typefaceB.getMethod("addCustomFallback", family);
            typefaceBuild = typefaceB.getMethod("build");
            textPaint = textPaintC.getConstructor();
            setTypeface = paint.getMethod("setTypeface", typeface);
            setTextSize = paint.getMethod("setTextSize", float.class);
            setTextLocale = paint.getMethod("setTextLocale", Locale.class);
            obtain = layoutB.getMethod("obtain", CharSequence.class, int.class, int.class, textPaintC, int.class);
            layoutBuild = layoutB.getMethod("build");
            lineCount = layout.getMethod("getLineCount");
            lineStart = layout.getMethod("getLineStart", int.class);
            lineEnd = layout.getMethod("getLineEnd", int.class);
            lineLeft = layout.getMethod("getLineLeft", int.class);
            lineWidth = layout.getMethod("getLineWidth", int.class);   // Without the white space at its end
            shapeTextRun = shaper.getMethod("shapeTextRun", CharSequence.class, int.class, int.class,
                    int.class, int.class, float.class, float.class, boolean.class, paint);
            glyphCount = glyphs.getMethod("glyphCount");
            glyphId = glyphs.getMethod("getGlyphId", int.class);
            glyphX = glyphs.getMethod("getGlyphX", int.class);
            glyphY = glyphs.getMethod("getGlyphY", int.class);
            glyphFont = glyphs.getMethod("getFont", int.class);
        }
    }

    private static Api api;

    private static synchronized Api api() throws Exception {
        if (api == null) {
            api = new Api();
        }
        return api;
    }

    // Android's font of each font file, made once
    private static final Map<byte[], Object> FONTS = new WeakHashMap<byte[], Object>();

    private static Object font(Api a, byte[] file) throws Exception {
        synchronized (FONTS) {
            Object font = FONTS.get(file);
            if (font == null) {
                // Android reads a font from a direct buffer only
                ByteBuffer buffer = ByteBuffer.allocateDirect(file.length);
                buffer.put(file);
                buffer.flip();
                font = a.fontBuild.invoke(a.fontBuilder.newInstance(buffer));
                FONTS.put(file, font);
            }
            return font;
        }
    }

    private static Object family(Api a, Object font) throws Exception {
        return a.familyBuild.invoke(a.familyBuilder.newInstance(font));
    }

    @Override
    public List<ShapedLine> shape(ShapingRequest request) throws Exception {
        String text = request.getText();
        String language = request.getLanguage();
        byte[][] fontFiles = request.getFontFiles();
        float fontSize = request.getFontSize();
        float width = request.getWidth();
        Api a = api();
        Object[] fonts = new Object[fontFiles.length];
        for (int i = 0; i < fonts.length; i++) {
            fonts[i] = font(a, fontFiles[i]);
        }
        Object builder = a.typefaceBuilder.newInstance(family(a, fonts[0]));
        for (int i = 1; i < fonts.length; i++) {
            a.addFallback.invoke(builder, family(a, fonts[i]));
        }
        Object paint = a.textPaint.newInstance();
        a.setTypeface.invoke(paint, a.typefaceBuild.invoke(builder));
        a.setTextSize.invoke(paint, fontSize * SCALE);
        if (language != null && !language.isEmpty()) {
            a.setTextLocale.invoke(paint, Locale.forLanguageTag(language));
        }

        List<ShapedLine> lines = new ArrayList<ShapedLine>();
        if (text.isEmpty()) {
            return lines;
        }
        Object layout = a.layoutBuild.invoke(a.obtain.invoke(null,
                text, 0, text.length(), paint, Math.round(width * SCALE)));
        int count = (Integer) a.lineCount.invoke(layout);
        for (int line = 0; line < count; line++) {
            int start = (Integer) a.lineStart.invoke(layout, line);
            int end = (Integer) a.lineEnd.invoke(layout, line);
            float left = (Float) a.lineLeft.invoke(layout, line);
            Object shaped = a.shapeTextRun.invoke(null,
                    text, start, end - start, start, end - start, left, 0f, false, paint);
            int n = (Integer) a.glyphCount.invoke(shaped);
            int[] fontOf = new int[n];
            int[] glyphs = new int[n];
            float[] positions = new float[2 * n];
            for (int i = 0; i < n; i++) {
                Object font = a.glyphFont.invoke(shaped, i);
                fontOf[i] = -1;     // A font of the system's, not one given
                for (int k = 0; k < fonts.length; k++) {
                    if (fonts[k].equals(font)) {
                        fontOf[i] = k;
                        break;
                    }
                }
                glyphs[i] = (Integer) a.glyphId.invoke(shaped, i);
                positions[2 * i] = (Float) a.glyphX.invoke(shaped, i) / SCALE;
                positions[2 * i + 1] = (Float) a.glyphY.invoke(shaped, i) / SCALE;
            }
            float visible = (Float) a.lineWidth.invoke(layout, line) / SCALE;
            lines.add(new ShapedLine(start, end, fontOf, glyphs, positions, visible));
        }
        return lines;
    }
}   // End of AndroidShaper.java
