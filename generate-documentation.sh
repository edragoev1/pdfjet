#!/bin/bash

rm docs/java/com/pdfjet/*.html
rm docs/_net/net/pdfjet/*.html
rm docs/java/*.html
rm docs/_net/*.html

# /opt/jdk1.5.0_22/bin/javadoc -public -notree -noindex -nonavbar com/pdfjet/*.java -d docs/java
# /opt/jdk1.5.0_22/bin/javac util/Translate.java
# /opt/jdk1.5.0_22/bin/java util.Translate

javadoc -public com/pdfjet/*.java -d docs/java
javac util/Translate.java
java util.Translate
