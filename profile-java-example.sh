javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .

javac -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_$1.java -d out/production
java -cp .:PDFjet.jar:out/production examples.Example_$1

java -XX:+FlightRecorder -XX:StartFlightRecording=filename=recording.jfr -cp .:PDFjet.jar:out/production examples.Example_$1
