#!/usr/bin/env python3
"""Resolve IL2CPP metadata-usage string literal slots (metadata v39, Unity 6.3).

Usage:
  python3 tools_decomp/strlit.py 0x182628110 [0x... ...]
"""
import struct
import sys
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
META = os.path.join(ROOT, 'extracted', 'ArmorSim', 'ArmorSim_Data', 'il2cpp_data', 'Metadata', 'global-metadata.dat')
BIN = os.path.join(ROOT, 'extracted', 'ArmorSim', 'GameAssembly.dll')


class PE:
    def __init__(self, path):
        self.data = open(path, 'rb').read()
        d = self.data
        pe = struct.unpack_from('<I', d, 0x3C)[0]
        nsec = struct.unpack_from('<H', d, pe + 6)[0]
        opt_size = struct.unpack_from('<H', d, pe + 20)[0]
        opt = pe + 24
        self.image_base = struct.unpack_from('<Q', d, opt + 24)[0]
        self.sections = []
        sec = opt + opt_size
        for i in range(nsec):
            off = sec + i * 40
            vsize, vaddr, rawsize, rawptr = struct.unpack_from('<IIII', d, off + 8)
            self.sections.append((vaddr, max(vsize, rawsize), rawptr))

    def read(self, addr, size):
        rva = addr - self.image_base
        for vaddr, vsize, rawptr in self.sections:
            if vaddr <= rva < vaddr + vsize:
                off = rawptr + (rva - vaddr)
                return self.data[off:off + size]
        return None


_lits = None
_pe = None


def pe():
    global _pe
    if _pe is None:
        _pe = PE(BIN)
    return _pe


def literals():
    global _lits
    if _lits is None:
        d = open(META, 'rb').read()
        sanity, version = struct.unpack_from('<II', d, 0)
        assert sanity == 0xFAB11BAF, hex(sanity)
        lit_off, lit_size, lit_cnt = struct.unpack_from('<iii', d, 8)
        dat_off, dat_size, _ = struct.unpack_from('<iii', d, 0x14)
        n = lit_size // 4
        assert n == lit_cnt, (n, lit_cnt)
        entries = struct.unpack_from('<%di' % n, d, lit_off)
        lits = []
        for i in range(n):
            start = entries[i]
            end = entries[i + 1] if i + 1 < n else dat_size
            lits.append(d[dat_off + start: dat_off + end].decode('utf-8', 'replace'))
        _lits = lits
    return _lits


def usage_of(slot_addr):
    """Decode metadata usage handle at slot. Returns (kind, index)."""
    raw = pe().read(slot_addr, 8)
    if raw is None:
        return None
    val = struct.unpack('<Q', raw)[0] & 0xFFFFFFFF
    kind = (val & 0xE0000000) >> 29
    index = (val & 0x1FFFFFFF) >> 1
    return kind, index


def resolve(slot_addr):
    u = usage_of(slot_addr)
    if u is None:
        return None
    kind, index = u
    if kind == 5:  # StringLiteral
        lits = literals()
        if index < len(lits):
            return lits[index]
    return None


def main():
    for a in sys.argv[1:]:
        addr = int(a, 16) if a.startswith('0x') else int(a)
        u = usage_of(addr)
        s = resolve(addr)
        print('%s -> %r  (kind=%s idx=%s)' % (a, s, u[0] if u else None, u[1] if u else None))


if __name__ == '__main__':
    main()
