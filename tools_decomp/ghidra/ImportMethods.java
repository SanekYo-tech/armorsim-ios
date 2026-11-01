// Импорт имён методов IL2CPP из method_map.csv (address,type,signature,assembly).
//@category IL2CPP
import ghidra.app.script.GhidraScript;
import ghidra.program.model.address.Address;
import ghidra.program.model.symbol.SourceType;
import ghidra.program.model.listing.Function;
import java.io.*;
import java.util.*;

public class ImportMethods extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        String csvPath = args[0];
        String filter = args.length > 1 ? args[1] : "";
        Map<Address, String> comments = new HashMap<>();
        int count = 0;
        try (BufferedReader br = new BufferedReader(new FileReader(csvPath))) {
            String line = br.readLine();
            while ((line = br.readLine()) != null) {
                String[] parts = parseCsv(line);
                if (parts.length < 4) continue;
                if (!filter.isEmpty() && !parts[3].equals(filter)) continue;
                Address addr = toAddr(parts[0].substring(2));
                String type = parts[1].replaceAll("[^A-Za-z0-9_]", "_");
                String sig = parts[2];
                String mname = sig.replaceAll("^[^(]*\\s+", "").replaceAll("\\(.*$", "")
                                  .replaceAll("[^A-Za-z0-9_]", "_");
                String name = type + "__" + mname;
                Function f = getFunctionAt(addr);
                if (f == null) {
                    disassemble(addr);
                    f = createFunction(addr, name);
                } else {
                    f.setName(name, SourceType.USER_DEFINED);
                }
                if (f != null) {
                    f.setComment(sig);
                    count++;
                }
            }
        }
        println("ImportMethods: named " + count + " functions");
    }

    private static String[] parseCsv(String line) {
        java.util.List<String> out = new ArrayList<>();
        StringBuilder cur = new StringBuilder();
        boolean quoted = false;
        for (int i = 0; i < line.length(); i++) {
            char c = line.charAt(i);
            if (c == '"') {
                if (quoted && i + 1 < line.length() && line.charAt(i + 1) == '"') { cur.append('"'); i++; }
                else quoted = !quoted;
            } else if (c == ',' && !quoted) {
                out.add(cur.toString()); cur.setLength(0);
            } else cur.append(c);
        }
        out.add(cur.toString());
        return out.toArray(new String[0]);
    }
}
