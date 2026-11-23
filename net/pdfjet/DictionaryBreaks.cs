/*
 * DictionaryBreaks.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * The break engine is a port of ICU's dictionary break engines
 * (icu4c/source/common/dictbe.cpp and dictionarydata.cpp), Copyright (C) 2016
 * and later: Unicode, Inc. and others, under the Unicode License, as are the
 * dictionaries it reads: see resources/fonts/world/ICUDictionaries/license.txt.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Thai, Lao, Khmer and Burmese are written without spaces between their
/// words, so where a line may break is found with a dictionary of each, as ICU
/// finds it: WordEnds ports ICU's dictionary break engines, which look three
/// words ahead for the choice that lets dictionary words follow, join a run of
/// characters that is not a word to the word before it, and never end a word
/// before a combining mark. The dictionaries are ICU's, carried with the world
/// fonts (resources/fonts/world/ICUDictionaries, and the NuGet package
/// PDFjet.Fonts.World), and read when text of their script is first broken;
/// without them no break is found inside such text, as before.
/// </summary>
internal static class DictionaryBreaks {
    // What an engine of ICU's knows of its script
    internal sealed class Script {
        internal String file;                   // The dictionary's file, in ICUDictionaries
        internal bool thai;                     // Thai's suffixes, PAIYANNOI and MAIYAMOK, and its shortest span
        internal Func<int, bool> word;          // [[:Script:]&[:LineBreak=SA:]]: the characters it breaks
        internal Func<int, bool> beginWord;     // The characters a word may begin with
        internal Func<int, bool> endWord;       // The characters a word may end with
    }

    // The characters of each script of line break class SA (LineBreak.txt of
    // Unicode 18.0.0)
    private static bool In(int c, params int[] ranges) {
        for (int i = 0; i < ranges.Length; i += 2) {
            if (c >= ranges[i] && c <= ranges[i + 1]) {
                return true;
            }
        }
        return false;
    }

    internal static bool ThaiWord(int c) {
        return In(c, 0x0E01, 0x0E3A, 0x0E40, 0x0E4E);
    }

    private static bool LaoWord(int c) {
        return In(c, 0x0E81, 0x0E82, 0x0E84, 0x0E84, 0x0E86, 0x0E8A, 0x0E8C, 0x0EA3, 0x0EA5, 0x0EA5,
                0x0EA7, 0x0EBD, 0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0EC8, 0x0ECE, 0x0EDC, 0x0EDF);
    }

    private static bool KhmerWord(int c) {
        return In(c, 0x1780, 0x17D3, 0x17D7, 0x17D7, 0x17DC, 0x17DD);
    }

    private static bool BurmeseWord(int c) {
        return In(c, 0x1000, 0x103F, 0x1050, 0x108F, 0x109A, 0x109F, 0xA9E0, 0xA9EF, 0xA9FA, 0xA9FE,
                0xAA60, 0xAA7F);
    }

    internal static readonly Script THAI = new Script {
        file = "thaidict.txt", thai = true, word = ThaiWord,
        // MAI HAN-AKAT and SARA E through SARA AI MAIMALAI end no word
        endWord = c => ThaiWord(c) && c != 0x0E31 && !(c >= 0x0E40 && c <= 0x0E44),
        // KO KAI through HO NOKHUK, SARA E through SARA AI MAIMALAI
        beginWord = c => (c >= 0x0E01 && c <= 0x0E2E) || (c >= 0x0E40 && c <= 0x0E44),
    };

    private static readonly Script[] SCRIPTS = {
        THAI,
        new Script {
            file = "laodict.txt", word = LaoWord,
            // The prefix vowels end no word
            endWord = c => LaoWord(c) && !(c >= 0x0EC0 && c <= 0x0EC4),
            // The consonants, the digraph consonants, the prefix vowels
            beginWord = c => (c >= 0x0E81 && c <= 0x0EAE) || (c >= 0x0EDC && c <= 0x0EDD) || (c >= 0x0EC0 && c <= 0x0EC4),
        },
        new Script {
            file = "khmerdict.txt", word = KhmerWord,
            // KHMER SIGN COENG, which joins the next consonant, ends no word
            endWord = c => KhmerWord(c) && c != 0x17D2,
            beginWord = c => c >= 0x1780 && c <= 0x17B3,
        },
        new Script {
            file = "burmesedict.txt", word = BurmeseWord, endWord = BurmeseWord,
            // The consonants and the independent vowels
            beginWord = c => c >= 0x1000 && c <= 0x102A,
        },
    };

    // What ICU's engines share: how many words in a row are good enough; the
    // longest dictionary word a non-word is joined to; the prefix a non-word
    // may share with a dictionary word and still be joined; the shortest
    // span of two words
    private const int LOOKAHEAD = 3;
    private const int ROOT_COMBINE_THRESHOLD = 3;
    private const int PREFIX_COMBINE_THRESHOLD = 3;
    private const int MIN_WORD_SPAN = 2 * 2;
    private const int POSSIBLE_WORD_LIST_MAX = 20;
    private const int THAI_PAIYANNOI = 0x0E2F;
    private const int THAI_MAIYAMOK = 0x0E46;

    // A mark of the script's characters, or a space: a word never ends
    // before one. The four scripts' characters are all of the BMP.
    private static bool Mark(Script script, int c) {
        if (c == ' ') {
            return true;
        }
        if (!script.word(c)) {
            return false;
        }
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory((char) c);
        return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark
                || category == UnicodeCategory.EnclosingMark;
    }

    // A dictionary's words, sorted, for ICU's trie's answers: whether a
    // string is a word, and whether a longer word begins with it
    internal sealed class Dictionary {
        private readonly String[] words;
        private readonly HashSet<String> isWord = new HashSet<String>(StringComparer.Ordinal);

        // A dictionary of ICU's: a word a line, after an optional tab its
        // value, which is not used; comments begin with #
        internal Dictionary(String data) {
            if (data.StartsWith("\uFEFF", StringComparison.Ordinal)) {
                data = data.Substring(1);
            }
            List<String> list = new List<String>();
            foreach (String row in data.Split('\n')) {
                int tab = row.IndexOf('\t');
                String word = (tab >= 0 ? row.Substring(0, tab) : row).Trim();
                if (word.Length == 0 || word.StartsWith("#", StringComparison.Ordinal) || !isWord.Add(word)) {
                    continue;
                }
                list.Add(word);
            }
            words = list.ToArray();
            Array.Sort(words, StringComparer.Ordinal);
        }

        // Tells if a word begins with the prefix and is longer than it, as
        // ICU's trie can go on from the prefix
        private bool Continues(String prefix) {
            int i = Array.BinarySearch(words, prefix, StringComparer.Ordinal);
            i = (i >= 0) ? i + 1 : ~i;
            return i < words.Length && words[i].StartsWith(prefix, StringComparison.Ordinal);
        }

        // ICU's DictionaryMatcher.matches: the lengths, shortest first, of
        // the words the text begins with at the position, no longer than
        // maxLength; returns their count and, in prefix, the characters the
        // trie took, the one it refused among them
        internal int Matches(int[] text, int at, int maxLength, int[] lengths, out int prefix) {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            int taken = 0;
            for (int i = at; i < text.Length; i++) {
                sb.Append(Char.ConvertFromUtf32(text[i]));
                String s = sb.ToString();
                int lengthMatched = i + 1 - at;
                taken++;
                bool word = isWord.Contains(s);
                bool more = Continues(s);
                if (word) {
                    if (count < lengths.Length) {
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
            prefix = taken;
            return count;
        }
    }

    // ICU's UText as the engines use it: the text, in code points, and a position
    private sealed class Text {
        internal readonly int[] cps;
        internal int pos;

        internal Text(int[] cps) {
            this.cps = cps;
        }

        internal int Current32() {
            return (pos < cps.Length) ? cps[pos] : -1;
        }

        internal int Next32() {
            return (pos < cps.Length) ? cps[pos++] : -1;
        }

        internal int Previous32() {
            return (pos > 0) ? cps[--pos] : -1;
        }
    }

    // ICU's PossibleWord: the words of the dictionary at a position of the
    // text, the one chosen, the one looked at
    private sealed class PossibleWord {
        internal int count;
        internal int prefix;
        internal int offset = -1;
        internal int mark;
        internal int current;
        internal readonly int[] lengths = new int[POSSIBLE_WORD_LIST_MAX];

        internal int Candidates(Text t, Dictionary d, int rangeEnd) {
            int start = t.pos;
            if (start != offset) {
                offset = start;
                count = d.Matches(t.cps, start, rangeEnd - start, lengths, out prefix);
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

        internal int AcceptMarked(Text t) {
            t.pos = offset + lengths[mark];
            return lengths[mark];
        }

        internal bool BackUp(Text t) {
            if (current > 0) {
                t.pos = offset + lengths[--current];
                return true;
            }
            return false;
        }
    }

    // Of more candidate words at the position, the one that lets the most
    // words follow, marked in the word
    private static void FindBest(PossibleWord[] words, int wordsFound, PossibleWord word,
            Text t, Dictionary d, int rangeEnd) {
        if (t.pos >= rangeEnd) {
            return;
        }
        do {
            if (words[(wordsFound + 1) % LOOKAHEAD].Candidates(t, d, rangeEnd) > 0) {
                // Followed by another word: a good candidate
                word.mark = word.current;
                if (t.pos >= rangeEnd) {
                    return;
                }
                // A third word after any of the second words: done
                do {
                    if (words[(wordsFound + 2) % LOOKAHEAD].Candidates(t, d, rangeEnd) > 0) {
                        word.mark = word.current;
                        return;
                    }
                } while (words[(wordsFound + 1) % LOOKAHEAD].BackUp(t));
            }
        } while (word.BackUp(t));
    }

    // ICU's engine on a run of the script's characters, from rangeStart to
    // rangeEnd: the positions where its words end, but its end
    private static void DivideUpDictionaryRange(Script script, Dictionary d, Text t,
            int rangeStart, int rangeEnd, List<int> breaks) {
        // Not enough characters for two words: Thai's engine moves four
        // characters on and wants the end beyond them, the others want four
        if ((script.thai && rangeEnd - rangeStart <= MIN_WORD_SPAN)
                || (!script.thai && rangeEnd - rangeStart < MIN_WORD_SPAN)) {
            return;
        }
        int first = breaks.Count;
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
            int candidates = word.Candidates(t, d, rangeEnd);
            if (candidates == 1) {
                wordLength = word.AcceptMarked(t);
                cpWordLength = word.lengths[word.mark];
                wordsFound++;
            } else if (candidates > 1) {
                FindBest(words, wordsFound, word, t, d, rangeEnd);
                wordLength = word.AcceptMarked(t);
                cpWordLength = word.lengths[word.mark];
                wordsFound++;
            }

            // A non-word after the word is joined to it, if the word is short
            // and the non-word shares too little with a dictionary word
            int uc;
            if (t.pos < rangeEnd && cpWordLength < ROOT_COMBINE_THRESHOLD) {
                PossibleWord next = words[wordsFound % LOOKAHEAD];
                if (next.Candidates(t, d, rangeEnd) <= 0
                        && (wordLength == 0 || next.prefix < PREFIX_COMBINE_THRESHOLD)) {
                    // A plausible word boundary
                    int remaining = rangeEnd - (current + wordLength);
                    int chars = 0;
                    for (;;) {
                        int pcIndex = t.pos;
                        int pc = t.Next32();
                        int pcSize = t.pos - pcIndex;
                        chars += pcSize;
                        remaining -= pcSize;
                        if (remaining <= 0) {
                            break;
                        }
                        uc = t.Current32();
                        if (script.endWord(pc) && script.beginWord(uc)) {
                            int n = words[(wordsFound + 1) % LOOKAHEAD].Candidates(t, d, rangeEnd);
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
            while (t.pos < rangeEnd && Mark(script, t.Current32())) {
                int currPos = t.pos;
                t.Next32();
                wordLength += t.pos - currPos;
            }

            // Thai's suffixes, when no word follows
            if (script.thai && t.pos < rangeEnd && wordLength > 0) {
                uc = t.Current32();
                if (words[wordsFound % LOOKAHEAD].Candidates(t, d, rangeEnd) <= 0
                        && (uc == THAI_PAIYANNOI || uc == THAI_MAIYAMOK)) {
                    if (uc == THAI_PAIYANNOI) {
                        int prev = t.Previous32();
                        if (prev != THAI_PAIYANNOI && prev != THAI_MAIYAMOK) {
                            t.Next32();
                            int paiyannoiIndex = t.pos;
                            t.Next32();
                            wordLength += t.pos - paiyannoiIndex;
                            uc = t.Current32();
                        } else {
                            t.Next32();
                        }
                    }
                    if (uc == THAI_MAIYAMOK) {
                        if (t.Previous32() != THAI_MAIYAMOK) {
                            t.Next32();
                            int maiyamokIndex = t.pos;
                            t.Next32();
                            wordLength += t.pos - maiyamokIndex;
                        } else {
                            t.Next32();
                        }
                    }
                } else {
                    t.pos = current + wordLength;
                }
            }

            if (wordLength > 0) {
                breaks.Add(current + wordLength);
            }
        }
        // No break at the end of the run
        if (breaks.Count > first && breaks[breaks.Count - 1] >= rangeEnd) {
            breaks.RemoveAt(breaks.Count - 1);
        }
    }

    private static readonly Dictionary<String, Dictionary> dictionaries =
            new Dictionary<String, Dictionary>(StringComparer.Ordinal);
    private static readonly Object dictionariesLock = new Object();

    // The dictionary of the script, read once, or null when it is not
    // carried: from the folder resources/fonts/world of the working
    // directory, else from the assembly PDFjet.Fonts.World
    private static Dictionary DictionaryOf(Script script) {
        lock (dictionariesLock) {
            Dictionary d;
            if (dictionaries.TryGetValue(script.file, out d)) {
                return d;
            }
            d = null;
            String path = "world/ICUDictionaries/" + script.file;
            Stream stream = null;
            try {
                if (File.Exists(BundledFonts.FOLDER + path)) {
                    stream = new FileStream(BundledFonts.FOLDER + path, FileMode.Open, FileAccess.Read);
                } else if (BundledFonts.WorldAssembly != null) {
                    stream = BundledFonts.WorldAssembly.GetManifestResourceStream(BundledFonts.RESOURCE_PREFIX + path);
                }
                if (stream != null) {
                    using (StreamReader reader = new StreamReader(stream, new UTF8Encoding(false))) {
                        d = new Dictionary(reader.ReadToEnd());
                    }
                }
            } catch (IOException) {
                d = null;   // Not carried, as if not there
            } catch (UnauthorizedAccessException) {
                d = null;
            } finally {
                if (stream != null) {
                    stream.Dispose();
                }
            }
            dictionaries[script.file] = d;
            return d;
        }
    }

    // The code points of the text
    internal static int[] CodePoints(String text) {
        List<int> cps = new List<int>(text.Length);
        for (int i = 0; i < text.Length; i++) {
            if (Char.IsHighSurrogate(text[i]) && i + 1 < text.Length && Char.IsLowSurrogate(text[i + 1])) {
                cps.Add(Char.ConvertToUtf32(text[i], text[i + 1]));
                i++;
            } else {
                cps.Add(text[i]);
            }
        }
        return cps.ToArray();
    }

    /// <summary>
    /// The indexes in the code points where a word of Thai, Lao, Khmer or
    /// Burmese ends inside a run of the script, where a line may break: in
    /// increasing order, none at a run's end.
    /// </summary>
    internal static List<int> WordEnds(int[] cps) {
        return WordEnds(cps, null);
    }

    // WordEnds of the runs of one script, or of all four when it is null
    internal static List<int> WordEnds(int[] cps, Script only) {
        List<int> ends = new List<int>();
        Text t = new Text(cps);
        for (int i = 0; i < cps.Length;) {
            Script script = null;
            foreach (Script s in SCRIPTS) {
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
            while (i < cps.Length && script.word(cps[i])) {
                i++;
            }
            Dictionary d = DictionaryOf(script);
            if (d != null) {
                DivideUpDictionaryRange(script, d, t, start, i, ends);
            }
        }
        // The ends before a mark left out, as ICU's rules never break a letter
        // from its marks: a mark after a sign that takes none, as ฯ then ุ, ended
        // a word before it (found by FuzzDictionaryWordEnds)
        ends.RemoveAll(end => end < cps.Length && IsMark(cps[end]));
        return ends;
    }

    // A nonspacing, spacing or enclosing mark, which stays with the letter
    // before it
    private static bool IsMark(int cp) {
        if (cp < 0 || cp > 0x10FFFF || (cp >= 0xD800 && cp <= 0xDFFF)) {
            return false;
        }
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(Char.ConvertFromUtf32(cp), 0);
        return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark
                || category == UnicodeCategory.EnclosingMark;
    }

    /// <summary>
    /// The line with a zero width space at each end of a Thai word inside a
    /// run of Thai, where TextBlock may break it, when the line has no zero
    /// width space of its own; else, or without the dictionary, the line as it
    /// is. A zero width space is not drawn.
    /// </summary>
    internal static String ThaiWordBreaks(String line) {
        if (line.IndexOf('\u200B') >= 0) {
            return line;
        }
        bool thai = false;
        for (int i = 0; i < line.Length && !thai; i++) {
            thai = ThaiWord(line[i]);
        }
        if (!thai) {
            return line;
        }
        int[] cps = CodePoints(line);
        List<int> ends = WordEnds(cps, THAI);
        if (ends.Count == 0) {
            return line;
        }
        StringBuilder sb = new StringBuilder();
        int next = 0;
        for (int i = 0; i < cps.Length; i++) {
            if (next < ends.Count && ends[next] == i) {
                sb.Append('\u200B');
                next++;
            }
            // A lone surrogate of CodePoints as it was, which
            // Char.ConvertFromUtf32 refuses (ultra review, 22 November 2026: a
            // Thai line with half of an emoji failed the whole document)
            sb.AppendCodePoint(cps[i]);
        }
        return sb.ToString();
    }
}
}   // End of namespace PDFjet.NET
