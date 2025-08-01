/**
 * Translate.java
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package util;

import java.io.*;
import java.util.*;

/**
 * Translate.java
 * Translates the documentation created by JavaDoc to match the C# code.
 */
public class Translate {
    public Translate(String readDir, String fileName, String writeDir) {
        // Load the Java to C# translation dictionary
        List<Pair> dict = new ArrayList<Pair>();
        try {
            BufferedReader reader =
                    new BufferedReader(new FileReader("util/translate-words.txt"));
            String line = null;
            while ((line = reader.readLine()) != null) {
                if (!line.equals("") && !line.startsWith("#")) {
                    Pair pair = new Pair();
                    pair.key = line.substring(0, line.indexOf('='));
                    pair.value = line.substring(line.indexOf('=') + 1);
                    dict.add(pair);
                }
            }
            reader.close();
        } catch (IOException ioe) {
            System.err.println(ioe);
        }

        Map<String, String> map = new TreeMap<String, String>();
        // Load the Java to C# capitalization dictionary
        try {
            BufferedReader reader =
                    new BufferedReader(new FileReader("util/capitalize-words.txt"));
            String line = null;
            while ((line = reader.readLine()) != null) {
                if (!line.equals("") && !line.startsWith("#")) {
                    Pair pair = new Pair();
                    pair.key = line;
                    pair.value = line.substring(0, 1).toUpperCase() + line.substring(1);
                    dict.add(pair);
                    map.put(pair.key, pair.value);
                }
            }
            reader.close();
        } catch (IOException ioe) {
            System.err.println(ioe);
        }
/* Use this code to generated sorted list!
        for (String key : map.keySet()) {
            System.out.println(key);
        }
*/
        StringBuffer data  = new StringBuffer();
        try {
            BufferedReader in = new BufferedReader(
                    new FileReader(readDir + "/" + fileName));
            int ch;
            while ((ch = in.read()) != -1) {
                data.append((char) ch);
            }

            BufferedWriter out = new BufferedWriter(
                    new FileWriter(writeDir + "/" + fileName));
            for (int i = 0; i < dict.size(); i++) {
                Pair pair = dict.get(i);
                searchAndReplace(data, pair.key, pair.value);
            }

            out.write(data.toString(), 0, data.length());
            out.close();

            in.close();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    // Searches the buffer for 'key' and replaces it with 'value'
    private void searchAndReplace(
            StringBuffer buf, String key, String value) {
        String str = buf.toString();
        buf.setLength(0);
        int i = 0;
        int j = 0;
        while (true) {
            j = str.indexOf(key, i);
            if (j == -1) {
                buf.append(str.substring(i));
                break;
            }
            buf.append(str.substring(i, j));
            buf.append(value);

            j += key.length();
            i = j;
        }
    }

    public static void main(String[] args) {
        String readDir = "docs/java/com/pdfjet";
        String writeDir = "docs/_net/net/pdfjet";
        File file = new File(readDir);
        String[] fileNames = file.list(new HtmlFilenameFilter());
        for (int i = 0; i < fileNames.length; i++) {
            new Translate(readDir, fileNames[i], writeDir);
        }

        readDir = "docs/java";
        writeDir = "docs/_net";
        file = new File(readDir);
        fileNames = file.list(new HtmlFilenameFilter());
        for (int i = 0; i < fileNames.length; i++) {
            new Translate(readDir, fileNames[i], writeDir);
        }
    }
}   // End of Translate.java

class Pair {
    String key;
    String value;
}

class HtmlFilenameFilter implements FilenameFilter {
    public boolean accept(File dir, String name) {
        if (name.endsWith(".html")) {
            return true;
        }
        return false;
    }
}   // End of Translate.java
