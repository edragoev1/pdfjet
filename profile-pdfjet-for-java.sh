javac -g -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .
javac -g -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_43.java -d out/production
java -XX:+FlightRecorder -XX:StartFlightRecording=duration=60s,filename=recording.jfr -cp PDFjet.jar:out/production examples.Example_43
