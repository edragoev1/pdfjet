# Line breaking by word dictionaries

- `expected.json`: for each run of Thai, Lao, Khmer and Burmese in the UDHR
  of `resources/data/udhr`, where ICU breaks it into words; made by
  `node make-expected.mjs` from the top folder of the repository, with
  Node's ICU (Intl.Segmenter). The dictionary break engines of the four
  ports must find the same.
- `thai-textblock-300.txt`: the lines of the Thai UDHR as Chrome breaks
  them (made 21 November 2026, Chrome's headless mode, the characters'
  places read from the page): each paragraph 300 points wide in IBM Plex
  Sans Thai Regular 12, no kerning, a blank line between paragraphs.
  `TextBlock` of the same width, font and size breaks them the same.

The texts are of the UDHR in Unicode project (github.com/eric-muller/udhr),
from the translations of the Office of the United Nations High
Commissioner for Human Rights.
