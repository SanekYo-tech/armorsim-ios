//@category IL2CPP
import ghidra.app.script.GhidraScript;
import ghidra.app.decompiler.*;
import ghidra.program.model.address.Address;
import ghidra.program.model.listing.*;

public class TestDecomp extends GhidraScript {
    public void run() throws Exception {
        Address a = toAddr("1805B5770");
        Function f = getFunctionAt(a);
        println("function=" + f);
        if (f == null) return;
        println("body=" + f.getBody() + " entry=" + f.getEntryPoint() + " external=" + f.isExternal() + " thunk=" + f.isThunk());
        println("instructions=" + currentProgram.getListing().getInstructions(f.getBody(), true).hasNext());
        DecompInterface d = new DecompInterface();
        d.toggleCCode(true);
        d.toggleSyntaxTree(true);
        boolean ok = d.openProgram(currentProgram);
        println("open=" + ok + " msg=" + d.getLastMessage());
        DecompileResults r = d.decompileFunction(f, 60, monitor);
        println("completed=" + r.decompileCompleted() + " err='" + r.getErrorMessage() + "' msg=" + d.getLastMessage());
        if (r.getDecompiledFunction() != null) println(r.getDecompiledFunction().getC());
        d.dispose();
    }
}
