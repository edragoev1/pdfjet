rm docs/java/com/pdfjet/*.html
rm docs/java/*.html

javadoc -public com/pdfjet/*.java -d docs/java
rm -rf docs/_net
cp -r docs/java docs/_net
mv docs/_net/com docs/_net/net
javac util/Translate.java
java util.Translate
