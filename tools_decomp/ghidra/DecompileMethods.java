// Декомпиляция методов по CSV/списку адресов в C-псевдокод.
// Аргументы: <method_map.csv> <assemblyFilter> <outputDir> [onlyTypePrefix]
//@category IL2CPP
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.Function;
import java.io.*;

public class DecompileMethods extends GhidraScript {
    @Override
    public void run() throws Exception {
        String[] args = getScriptArgs();
        String csvPath = args[0];
        String filter = args[1];
        String outDir = args[2];
        String typePrefix = args.length > 3 ? args[3] : "";
        new File(outDir).mkdirs();
        DecompInterface decomp = new DecompInterface();
        decomp.toggleCCode(true);
        decomp.toggleSyntaxTree(true);
        decomp.openProgram(currentProgram);
        int count = 0;
        try (BufferedReader br = new BufferedReader(new FileReader(csvPath))) {
            String line = br.readLine();
            while ((line = br.readLine()) != null) {
                if (monitor.isCancelled()) break;
                String[] parts = parseCsv(line);
                if (parts.length < 4 || !parts[3].equals(filter)) continue;
                if (!typePrefix.isEmpty() && !parts[1].startsWith(typePrefix)) continue;
                Address addr = toAddr(parts[0].substring(2));
                Function f = getFunctionAt(addr);
                if (f == null) {
                    disassemble(addr);
                    f = createFunction(addr, parts[1].replaceAll("[^A-Za-z0-9_]", "_") + "__" + parts[0]);
                }
                if (f == null) continue;
                DecompileResults res = decomp.decompileFunction(f, 120, monitor);
                StringBuilder sb = new StringBuilder();
                sb.append("// ").append(parts[1]).append("\n// ").append(parts[2])
                  .append("\n// ").append(parts[0]).append("\n");
                if (res.decompileCompleted()) {
                    sb.append(res.getDecompiledFunction().getC());
                } else {
                    sb.append("// DECOMPILE FAILED: ").append(res.getErrorMessage());
                }
                String safe = parts[1].replaceAll("[^A-Za-z0-9_.-]", "_");
                try (PrintWriter pw = new PrintWriter(new File(outDir, safe + "__" + parts[0] + ".c"))) {
                    pw.print(sb.toString());
                }
                count++;
            }
        }
        decomp.dispose();
        println("DecompileMethods: wrote " + count + " files");
    }

    private static String[] parseCsv(String line) {
        java.util.List<String> out = new java.util.ArrayList<>();
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
