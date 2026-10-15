import java.text.SimpleDateFormat;
import java.time.*;
import java.time.format.DateTimeFormatter;
import java.util.*;

public class JdkCold {
    public static void main(String[] args) throws Exception {
        String what = args[0];
        long t0 = System.nanoTime();
        switch (what) {
        case "securerandom": new java.security.SecureRandom().nextBytes(new byte[16]); break;
        case "simpledateformat": {
            SimpleDateFormat f = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.US);
            f.setTimeZone(TimeZone.getTimeZone("UTC"));
            f.format(new Date());
            break;
        }
        case "javatime": DateTimeFormatter.ISO_LOCAL_DATE_TIME.format(LocalDateTime.now(ZoneOffset.UTC)); break;
        case "manual": {
            long now = System.currentTimeMillis();
            long days = Math.floorDiv(now, 86400000L);
            int secs = (int) Math.floorMod(now, 86400000L) / 1000;
            LocalDate d = LocalDate.ofEpochDay(days);
            String s = String.valueOf(d.getYear()) + d.getMonthValue() + d.getDayOfMonth() + secs;
            break;
        }
        case "logger": java.util.logging.Logger.getLogger("x"); break;
        case "format": String.format(Locale.ROOT, "%.2f", 1.5); break;
        }
        System.out.printf("%s %.1f ms%n", what, (System.nanoTime() - t0) / 1e6);
    }
}
