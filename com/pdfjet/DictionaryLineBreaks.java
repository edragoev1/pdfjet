/*
 * DictionaryLineBreaks.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.text.BreakIterator;
import java.text.CharacterIterator;
import java.text.StringCharacterIterator;
import java.util.Arrays;
import java.util.List;
import java.util.TreeSet;

/**
 * The places a line may break: the JDK's line breaks, and the ends of the
 * words of Thai, Lao, Khmer and Burmese that ICU's dictionaries find, of
 * which the JDK has Thai's alone. For java.awt.font's LineBreakMeasurer,
 * which breaks Lao, Khmer and Burmese nowhere without them.
 */
final class DictionaryLineBreaks extends BreakIterator {
    private CharacterIterator text = new StringCharacterIterator("");
    private int[] boundaries = {0};
    private int current;    // The index of the boundary at

    @Override
    public void setText(CharacterIterator newText) {
        text = newText;
        StringBuilder sb = new StringBuilder();
        for (char c = newText.first(); c != CharacterIterator.DONE; c = newText.next()) {
            sb.append(c);
        }
        String s = sb.toString();
        int begin = newText.getBeginIndex();
        TreeSet<Integer> set = new TreeSet<Integer>();
        BreakIterator lines = BreakIterator.getLineInstance();
        lines.setText(s);
        for (int b = lines.first(); b != BreakIterator.DONE; b = lines.next()) {
            set.add(begin + b);
        }
        int[] cps = s.codePoints().toArray();
        List<Integer> ends = DictionaryBreaks.wordEnds(cps);
        if (!ends.isEmpty()) {
            // The code point indexes as char indexes
            int[] offsets = new int[cps.length + 1];
            for (int i = 0; i < cps.length; i++) {
                offsets[i + 1] = offsets[i] + Character.charCount(cps[i]);
            }
            for (int end : ends) {
                set.add(begin + offsets[end]);
            }
        }
        set.add(begin);
        set.add(newText.getEndIndex());
        boundaries = new int[set.size()];
        int i = 0;
        for (int b : set) {
            boundaries[i++] = b;
        }
        current = 0;
    }

    @Override
    public CharacterIterator getText() {
        return text;
    }

    @Override
    public int first() {
        current = 0;
        return boundaries[0];
    }

    @Override
    public int last() {
        current = boundaries.length - 1;
        return boundaries[current];
    }

    @Override
    public int next(int n) {
        int to = current + n;
        if (to < 0 || to >= boundaries.length) {
            current = (to < 0) ? 0 : boundaries.length - 1;
            return DONE;
        }
        current = to;
        return boundaries[current];
    }

    @Override
    public int next() {
        return next(1);
    }

    @Override
    public int previous() {
        return next(-1);
    }

    @Override
    public int following(int offset) {
        int i = Arrays.binarySearch(boundaries, offset);
        i = (i >= 0) ? i + 1 : -i - 1;
        if (i >= boundaries.length) {
            current = boundaries.length - 1;
            return DONE;
        }
        current = i;
        return boundaries[current];
    }

    @Override
    public int preceding(int offset) {
        int i = Arrays.binarySearch(boundaries, offset);
        i = (i >= 0) ? i - 1 : -i - 2;
        if (i < 0) {
            current = 0;
            return DONE;
        }
        current = i;
        return boundaries[current];
    }

    @Override
    public int current() {
        return boundaries[current];
    }
}   // End of DictionaryLineBreaks.java
