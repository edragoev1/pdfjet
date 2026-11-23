/*
 * DictionaryBreaks.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * The break engine is a port of ICU's dictionary break engines
 * (icu4c/source/common/dictbe.cpp and dictionarydata.cpp), Copyright (C) 2016
 * and later: Unicode, Inc. and others, under the Unicode License, as are the
 * dictionaries it reads: see resources/fonts/world/ICUDictionaries/license.txt.
 */
package com.pdfjet;

import java.io.ByteArrayOutputStream;
import java.io.InputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Thai, Lao, Khmer and Burmese are written without spaces between their words,
 * so where a line may break is found with a dictionary of each, as ICU finds
 * it: wordEnds ports ICU's dictionary break engines, which look three words
 * ahead for the choice that lets dictionary words follow, join a run of
 * characters that is not a word to the word before it, and never end a word
 * before a combining mark. The dictionaries are ICU's, carried with the world
 * fonts (resources/fonts/world/ICUDictionaries, and the jar
 * com.pdfjet:pdfjet-fonts-world), and read when text of their script is first
 * broken; without them no break is found inside such text, as before.
 */
final class DictionaryBreaks {
    private DictionaryBreaks() {
    }

    // What an engine of ICU's knows of its script
    private abstract static class Script {
        final String file;      // The dictionary's file, in ICUDictionaries
        final boolean thai;     // Thai's suffixes, PAIYANNOI and MAIYAMOK, and its shortest span

        Script(String file, boolean thai) {
            this.file = file;
            this.thai = thai;
        }

        abstract boolean word(int c);       // [[:Script:]&[:LineBreak=SA:]]: the characters it breaks
        abstract boolean beginWord(int c);  // The characters a word may begin with
        abstract boolean endWord(int c);    // The characters a word may end with
    }

    // The characters of each script of line break class SA (LineBreak.txt of
    // Unicode 18.0.0)
    private static boolean in(int c, int... ranges) {
        for (int i = 0; i < ranges.length; i += 2) {
            if (c >= ranges[i] && c <= ranges[i + 1]) {
                return true;
            }
        }
        return false;
    }

    static boolean thaiWord(int c) {
        return in(c, 0x0E01, 0x0E3A, 0x0E40, 0x0E4E);
    }

    private static boolean laoWord(int c) {
        return in(c, 0x0E81, 0x0E82, 0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5,
                0x0EA7, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0EC8, 0x0ECE, 0x0EDC, 0x0EDF);
    }

    private static boolean khmerWord(int c) {
        return in(c, 0x1780, 0x17D3, 0x17D7, 0x17D7, 0x17DC, 0x17DD);
    }

    private static boolean burmeseWord(int c) {
        return in(c, 0x1000, 0x103F, 0x1050, 0x108F, 0x109A, 0x109F, 0xA9E0, 0xA9EF, 0xA9FA, 0xA9FE,
                0xAA60, 0xAA7F);
    }

    static final Script THAI = new Script("thaidict.txt", true) {
        boolean word(int c) { return thaiWord(c); }
        // MAI HAN-AKAT and SARA E through SARA AI MAIMALAI end no word
        boolean endWord(int c) { return thaiWord(c) && c != 0x0E31 && !(c >= 0x0E40 && c <= 0x0E44); }
        // KO KAI through HO NOKHUK, SARA E through SARA AI MAIMALAI
        boolean beginWord(int c) { return (c >= 0x0E01 && c <= 0x0E2E) || (c >= 0x0E40 && c <= 0x0E44); }
    };

    private static final Script[] SCRIPTS = {
        THAI,
        new Script("laodict.txt", false) {
            boolean word(int c) { return laoWord(c); }
            // The prefix vowels end no word
            boolean endWord(int c) { return laoWord(c) && !(c >= 0x0EC0 && c <= 0x0EC4); }
            // The consonants, the digraph consonants, the prefix vowels
            boolean beginWord(int c) {
                return (c >= 0x0E81 && c <= 0x0EAE) || (c >= 0x0EDC && c <= 0x0EDD) || (c >= 0x0EC0 && c <= 0x0EC4);
            }
        },
        new Script("khmerdict.txt", false) {
            boolean word(int c) { return khmerWord(c); }
            // KHMER SIGN COENG, which joins the next consonant, ends no word
            boolean endWord(int c) { return khmerWord(c) && c != 0x17D2; }
            boolean beginWord(int c) { return c >= 0x1780 && c <= 0x17B3; }
        },
        new Script("burmesedict.txt", false) {
            boolean word(int c) { return burmeseWord(c); }
            boolean endWord(int c) { return burmeseWord(c); }
            // The consonants and the independent vowels
            boolean beginWord(int c) { return c >= 0x1000 && c <= 0x102A; }
        },
    };

    // What ICU's engines share: how many words in a row are good enough; the
    // longest dictionary word a non-word is joined to; the prefix a non-word
    // may share with a dictionary word and still be joined; the shortest
    // span of two words
    private static final int LOOKAHEAD = 3;
    private static final int ROOT_COMBINE_THRESHOLD = 3;
    private static final int PREFIX_COMBINE_THRESHOLD = 3;
    private static final int MIN_WORD_SPAN = 2 * 2;
    private static final int POSSIBLE_WORD_LIST_MAX = 20;
    private static final int THAI_PAIYANNOI = 0x0E2F;
    private static final int THAI_MAIYAMOK = 0x0E46;

    // A mark of the script's characters, or a space: a word never ends before one
    private static boolean mark(Script script, int c) {
        if (c == ' ') {
            return true;
        }
        if (!script.word(c)) {
            return false;
        }
        int type = Character.getType(c);
        return type == Character.NON_SPACING_MARK || type == Character.COMBINING_SPACING_MARK
                || type == Character.ENCLOSING_MARK;
    }

    // A dictionary's words, sorted, for ICU's trie's answers: whether a
    // string is a word, and whether a longer word begins with it
    static final class Dictionary {
        private final String[] words;
        private final Set<String> isWord = new HashSet<String>();

        // A dictionary of ICU's: a word a line, after an optional tab its
        // value, which is not used; comments begin with #
        Dictionary(String data) {
            if (data.startsWith("\uFEFF")) {
                data = data.substring(1);
            }
            List<String> list = new ArrayList<String>();
            for (String line : data.split("\n", -1)) {
                int tab = line.indexOf('\t');
                String word = (tab >= 0 ? line.substring(0, tab) : line).trim();
                if (word.isEmpty() || word.startsWith("#") || !isWord.add(word)) {
                    continue;
                }
                list.add(word);
            }
            words = list.toArray(new String[0]);
            Arrays.sort(words);
        }

        // Tells if a word begins with the prefix and is longer than it, as
        // ICU's trie can go on from the prefix
        private boolean continues(String prefix) {
            int i = Arrays.binarySearch(words, prefix);
            i = (i >= 0) ? i + 1 : -i - 1;
            return i < words.length && words[i].startsWith(prefix);
        }

        // ICU's DictionaryMatcher.matches: the lengths, shortest first, of
        // the words the text begins with at the position, no longer than
        // maxLength; returns their count and, in prefix[0], the characters
        // the trie took, the one it refused among them
        int matches(int[] text, int at, int maxLength, int[] lengths, int[] prefix) {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            int taken = 0;
            for (int i = at; i < text.length; i++) {
                sb.appendCodePoint(text[i]);
                String s = sb.toString();
                int lengthMatched = i + 1 - at;
                taken++;
                boolean word = isWord.contains(s);
                boolean more = continues(s);
                if (word) {
                    if (count < lengths.length) {
                        lengths[count++] = lengthMatched;
                    }
                    if (!more) {    // A final value
                        break;
                    }
                } else if (!more) { // No match
                    break;
                }
                if (lengthMatched >= maxLength) {
                    break;
                }
            }
            prefix[0] = taken;
            return count;
        }
    }

    // ICU's UText as the engines use it: the text, in code points, and a position
    private static final class Text {
        final int[] cps;
        int pos;

        Text(int[] cps) {
            this.cps = cps;
        }

        int current32() {
            return (pos < cps.length) ? cps[pos] : -1;
        }

        int next32() {
            return (pos < cps.length) ? cps[pos++] : -1;
        }

        int previous32() {
            return (pos > 0) ? cps[--pos] : -1;
        }
    }

    // ICU's PossibleWord: the words of the dictionary at a position of the
    // text, the one chosen, the one looked at
    private static final class PossibleWord {
        int count;
        int prefix;
        int offset = -1;
        int mark;
        int current;
        final int[] lengths = new int[POSSIBLE_WORD_LIST_MAX];
        private final int[] taken = new int[1];

        int candidates(Text t, Dictionary d, int rangeEnd) {
            int start = t.pos;
            if (start != offset) {
                offset = start;
                count = d.matches(t.cps, start, rangeEnd - start, lengths, taken);
                prefix = taken[0];
                if (count <= 0) {
                    t.pos = start;
                }
            }
            if (count > 0) {
                t.pos = start + lengths[count - 1];
            }
            current = count - 1;
            mark = current;
            return count;
        }

        int acceptMarked(Text t) {
            t.pos = offset + lengths[mark];
            return lengths[mark];
        }

        boolean backUp(Text t) {
            if (current > 0) {
                t.pos = offset + lengths[--current];
                return true;
            }
            return false;
        }
    }

    // ICU's engine on a run of the script's characters, from rangeStart to
    // rangeEnd: the positions where its words end, but its end
    private static void divideUpDictionaryRange(Script script, Dictionary d, Text t,
            int rangeStart, int rangeEnd, List<Integer> breaks) {
        // Not enough characters for two words: Thai's engine moves four
        // characters on and wants the end beyond them, the others want four
        if ((script.thai && rangeEnd - rangeStart <= MIN_WORD_SPAN)
                || (!script.thai && rangeEnd - rangeStart < MIN_WORD_SPAN)) {
            return;
        }
        int first = breaks.size();
        int wordsFound = 0;
        PossibleWord[] words = new PossibleWord[LOOKAHEAD];
        for (int i = 0; i < LOOKAHEAD; i++) {
            words[i] = new PossibleWord();
        }
        t.pos = rangeStart;
        while (t.pos < rangeEnd) {
            int current = t.pos;
            int wordLength = 0;
            int cpWordLength = 0;
            PossibleWord word = words[wordsFound % LOOKAHEAD];

            // Candidate words at the position: one is taken; of more, the
            // one that lets the most words follow
            int candidates = word.candidates(t, d, rangeEnd);
            if (candidates == 1) {
                wordLength = word.acceptMarked(t);
                cpWordLength = word.lengths[word.mark];
                wordsFound++;
            } else if (candidates > 1) {
                foundBest:
                if (t.pos < rangeEnd) {
                    do {
                        if (words[(wordsFound + 1) % LOOKAHEAD].candidates(t, d, rangeEnd) > 0) {
                            // Followed by another word: a good candidate
                            word.mark = word.current;
                            if (t.pos >= rangeEnd) {
                                break foundBest;
                            }
                            // A third word after any of the second words: done
                            do {
                                if (words[(wordsFound + 2) % LOOKAHEAD].candidates(t, d, rangeEnd) > 0) {
                                    word.mark = word.current;
                                    break foundBest;
                                }
                            } while (words[(wordsFound + 1) % LOOKAHEAD].backUp(t));
                        }
                    } while (word.backUp(t));
                }
                wordLength = word.acceptMarked(t);
                cpWordLength = word.lengths[word.mark];
                wordsFound++;
            }

            // A non-word after the word is joined to it, if the word is short
            // and the non-word shares too little with a dictionary word
            int uc = 0;
            if (t.pos < rangeEnd && cpWordLength < ROOT_COMBINE_THRESHOLD) {
                PossibleWord next = words[wordsFound % LOOKAHEAD];
                if (next.candidates(t, d, rangeEnd) <= 0
                        && (wordLength == 0 || next.prefix < PREFIX_COMBINE_THRESHOLD)) {
                    // A plausible word boundary
                    int remaining = rangeEnd - (current + wordLength);
                    int chars = 0;
                    for (;;) {
                        int pcIndex = t.pos;
                        int pc = t.next32();
                        int pcSize = t.pos - pcIndex;
                        chars += pcSize;
                        remaining -= pcSize;
                        if (remaining <= 0) {
                            break;
                        }
                        uc = t.current32();
                        if (script.endWord(pc) && script.beginWord(uc)) {
                            int n = words[(wordsFound + 1) % LOOKAHEAD].candidates(t, d, rangeEnd);
                            t.pos = current + wordLength + chars;
                            if (n > 0) {
                                break;
                            }
                        }
                    }
                    if (wordLength <= 0) {
                        wordsFound++;
                    }
                    wordLength += chars;
                } else {
                    t.pos = current + wordLength;
                }
            }

            // Never a break before a combining mark
            while (t.pos < rangeEnd && mark(script, t.current32())) {
                int currPos = t.pos;
                t.next32();
                wordLength += t.pos - currPos;
            }

            // Thai's suffixes, when no word follows
            if (script.thai && t.pos < rangeEnd && wordLength > 0) {
                uc = t.current32();
                if (words[wordsFound % LOOKAHEAD].candidates(t, d, rangeEnd) <= 0
                        && (uc == THAI_PAIYANNOI || uc == THAI_MAIYAMOK)) {
                    if (uc == THAI_PAIYANNOI) {
                        int prev = t.previous32();
                        if (prev != THAI_PAIYANNOI && prev != THAI_MAIYAMOK) {
                            t.next32();
                            int paiyannoiIndex = t.pos;
                            t.next32();
                            wordLength += t.pos - paiyannoiIndex;
                            uc = t.current32();
                        } else {
                            t.next32();
                        }
                    }
                    if (uc == THAI_MAIYAMOK) {
                        if (t.previous32() != THAI_MAIYAMOK) {
                            t.next32();
                            int maiyamokIndex = t.pos;
                            t.next32();
                            wordLength += t.pos - maiyamokIndex;
                        } else {
                            t.next32();
                        }
                    }
                } else {
                    t.pos = current + wordLength;
                }
            }

            if (wordLength > 0) {
                breaks.add(current + wordLength);
            }
        }
        // No break at the end of the run
        if (breaks.size() > first && breaks.get(breaks.size() - 1) >= rangeEnd) {
            breaks.remove(breaks.size() - 1);
        }
    }

    private static final Map<String, Dictionary> DICTIONARIES = new HashMap<String, Dictionary>();

    // The dictionary of the script, read once, or null when it is not
    // carried: from the folder resources/fonts/world of the working
    // directory, else from the jar com.pdfjet:pdfjet-fonts-world
    private static synchronized Dictionary dictionaryOf(Script script) {
        if (DICTIONARIES.containsKey(script.file)) {
            return DICTIONARIES.get(script.file);
        }
        Dictionary d = null;
        String path = "world/ICUDictionaries/" + script.file;
        InputStream stream = null;
        try {
            java.io.File file = new java.io.File(BundledFonts.FOLDER + path);
            if (file.isFile()) {
                stream = new java.io.FileInputStream(file);
            } else {
                stream = BundledFonts.class.getResourceAsStream("/com/pdfjet/fonts/" + path);
                ClassLoader context = Thread.currentThread().getContextClassLoader();
                if (stream == null && context != null) {
                    stream = context.getResourceAsStream("com/pdfjet/fonts/" + path);
                }
            }
            if (stream != null) {
                ByteArrayOutputStream out = new ByteArrayOutputStream();
                byte[] buf = new byte[65536];
                for (int n; (n = stream.read(buf)) != -1;) {
                    out.write(buf, 0, n);
                }
                d = new Dictionary(new String(out.toByteArray(), StandardCharsets.UTF_8));
            }
        } catch (java.io.IOException e) {
            d = null;   // Not carried, as if not there
        } finally {
            if (stream != null) {
                try {
                    stream.close();
                } catch (java.io.IOException e) {
                    // Read already
                }
            }
        }
        DICTIONARIES.put(script.file, d);
        return d;
    }

    /**
     * The indexes in the code points where a word of Thai, Lao, Khmer or
     * Burmese ends inside a run of the script, where a line may break: in
     * increasing order, none at a run's end.
     */
    static List<Integer> wordEnds(int[] cps) {
        return wordEnds(cps, null);
    }

    // wordEnds of the runs of one script, or of all four when it is null
    static List<Integer> wordEnds(int[] cps, Script only) {
        List<Integer> ends = new ArrayList<Integer>();
        Text t = new Text(cps);
        for (int i = 0; i < cps.length;) {
            Script script = null;
            for (Script s : SCRIPTS) {
                if (s.word(cps[i])) {
                    script = s;
                    break;
                }
            }
            if (script == null || (only != null && script != only)) {
                i++;
                continue;
            }
            int start = i;
            while (i < cps.length && script.word(cps[i])) {
                i++;
            }
            Dictionary d = dictionaryOf(script);
            if (d != null) {
                divideUpDictionaryRange(script, d, t, start, i, ends);
            }
        }
        // The ends before a mark left out, as ICU's rules never break a letter
        // from its marks: a mark after a sign that takes none, as ฯ then ุ, ended
        // a word before it (found by FuzzDictionaryWordEnds)
        ends.removeIf(end -> end < cps.length && isMark(cps[end]));
        return ends;
    }

    // A nonspacing, spacing or enclosing mark, which stays with the letter
    // before it
    private static boolean isMark(int cp) {
        int type = Character.getType(cp);
        return type == Character.NON_SPACING_MARK || type == Character.COMBINING_SPACING_MARK
                || type == Character.ENCLOSING_MARK;
    }

    /**
     * The line with a zero width space at each end of a Thai word inside a
     * run of Thai, where TextBlock may break it, when the line has no zero
     * width space of its own; else, or without the dictionary, the line as it
     * is. A zero width space is not drawn.
     */
    static String thaiWordBreaks(String line) {
        if (line.indexOf('\u200B') >= 0) {
            return line;
        }
        boolean thai = false;
        for (int i = 0; i < line.length() && !thai; i++) {
            thai = thaiWord(line.charAt(i));
        }
        if (!thai) {
            return line;
        }
        int[] cps = line.codePoints().toArray();
        List<Integer> ends = wordEnds(cps, THAI);
        if (ends.isEmpty()) {
            return line;
        }
        StringBuilder sb = new StringBuilder();
        int next = 0;
        for (int i = 0; i < cps.length; i++) {
            if (next < ends.size() && ends.get(next) == i) {
                sb.append('\u200B');
                next++;
            }
            sb.appendCodePoint(cps[i]);
        }
        return sb.toString();
    }
}   // End of DictionaryBreaks.java
