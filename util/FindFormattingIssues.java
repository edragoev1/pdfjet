/**
 * FindEmptyLines.java
 *
Copyright 2023 Innovatics Inc.

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
import java.util.ArrayList;
import java.util.List;

public class FindFormattingIssues {
    public FindFormattingIssues(String filePath) {
        File file = new File(filePath);
        String[] fileNames = file.list();
        for (String fileName : fileNames) {
            if (new File(filePath + "/" + fileName).isDirectory()) {
                continue;
            }
            try {
                List<String> lines = new ArrayList<String>();
                BufferedReader reader = new BufferedReader(new FileReader(filePath + "/" + fileName));
                String line;
                while ((line = reader.readLine()) != null) {
                    lines.add(line);
                }
                reader.close();
                for (int i = 0; i < (lines.size() - 1); i++) {
                    if (lines.get(i).equals("") && lines.get(i + 1).equals("")) {
                        System.out.println(fileName);
                    }
                    if (lines.get(i).trim().equals("else {")) {
                        System.out.println(fileName);
                    }
                    if (lines.get(i).trim().startsWith("else if (")) {
                        System.out.println(fileName);
                    }
                    if (!rtrim(lines.get(i)).equals(lines.get(i))) {
                        System.out.println(fileName + " " + lines.get(i) + " " + i);
                    }
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }
    }

    public static void main(String[] args) {
        new FindFormattingIssues("com/pdfjet");
        new FindFormattingIssues("net/pdfjet");
        new FindFormattingIssues("Sources/PDFjet");
        new FindFormattingIssues("src");
    }

    private static String rtrim(String str) {
        int i = str.length()-1;
        while (i >= 0 && Character.isWhitespace(str.charAt(i))) {
            i--;
        }
        return str.substring(0,i+1);
    }
}   // End of FindEmptyLines.java
