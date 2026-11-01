#!/usr/bin/env python3
"""Resolve IL2CPP metadata-usage slots to type names (metadata v39, Unity 6.3).

Usage:
  python3 tools_decomp/typeusage.py 0x182616C78 [0x... ...]
"""
import struct
import sys
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
META = os.path.join(ROOT, 'extracted', 'ArmorSim', 'ArmorSim_Data', 'il2cpp_data', 'Metadata', 'global-metadata.dat')
BIN = os.path.join(ROOT, 'extracted', 'ArmorSim', 'GameAssembly.dll')

md = open(META, 'rb').read()

# --- header anchors (see strlit.py): string literals + identifier strings
lit_off, lit_size, lit_cnt = struct.unpack_from('<iii', md, 8)
dat_off, dat_size, _ = struct.unpack_from('<iii', md, 0x14)
STR_BASE = dat_off + dat_size          # identifier string table base
# sanity: STR_BASE must match int[8]
STR_BASE = struct.unpack_from('<i', md, 0x20)[0]
STR_SIZE = struct.unpack_from('<i', md, 0x24)[0]


def cstr_at(off):
    end = md.index(b'\0', off)
    return md[off:end].decode('utf-8', 'replace')


def find_name_idx(name):
    """Return list of string-table-relative offsets where `name\0` occurs."""
    pat = name.encode() + b'\0'
    res = []
    start = STR_BASE
    while True:
        i = md.find(pat, start, STR_BASE + STR_SIZE)
        if i < 0:
            break
        res.append(i - STR_BASE)
        start = i + 1
    return res


def scan_int32(value, lo=0, hi=None):
    hi = hi if hi is not None else len(md) - 4
    pat = struct.pack('<i', value)
    res = []
    start = lo
    while True:
        i = md.find(pat, start, hi)
        if i < 0:
            break
        res.append(i)
        start = i + 1
    return res


# --- PE reader for slots
data = open(BIN, 'rb').read()
pe = struct.unpack_from('<I', data, 0x3C)[0]
nsec = struct.unpack_from('<H', data, pe + 6)[0]
opt_size = struct.unpack_from('<H', data, pe + 20)[0]
opt = pe + 24
image_base = struct.unpack_from('<Q', data, opt + 24)[0]
secs = []
sec = opt + opt_size
for i in range(nsec):
    off = sec + i * 40
    vsize, vaddr, rawsize, rawptr = struct.unpack_from('<IIII', data, off + 8)
    secs.append((vaddr, max(vsize, rawsize), rawptr))


def read_slot(addr):
    rva = addr - image_base
    for vaddr, vsize, rawptr in secs:
        if vaddr <= rva < vaddr + vsize:
            off = rawptr + (rva - vaddr)
            return struct.unpack('<Q', data[off:off + 8])[0] & 0xFFFFFFFF
    return None


def usage_of(slot):
    val = read_slot(slot)
    if val is None:
        return None
    kind = (val & 0xE0000000) >> 29
    index = (val & 0x1FFFFFFF) >> 1
    return kind, index


_TYPETABLE = None


def init_typetable():
    """Locate typeDefinitions table using two anchors with known indices."""
    global _TYPETABLE
    # anchors: slots we identified from context
    anchors = []
    for slot, name in ((0x182616C78, 'CrewAnatomy'), (0x182616D10, 'CrewFigure')):
        u = usage_of(slot)
        idxs = find_name_idx(name)
        anchors.append((u[1], idxs, name))
    (i1, p1, n1), (i2, p2, n2) = anchors
    empty_ns = find_name_idx('')[0]  # empty namespace index (usually 0)
    for stride in (0x50, 0x54, 0x58, 0x5C, 0x60, 0x64, 0x68, 0x6C, 0x70, 0x74, 0x78):
        for a in p1:
            for b in p2:
                if a == b:
                    continue
                # b - a = stride * (i2 - i1)
                d = b - a
                di = i2 - i1
                if di == 0 or d <= 0 != di <= 0:
                    continue
                if d % stride != 0 or d // stride != di:
                    continue
                base = a - stride * i1
                if base < 0:
                    continue
                # verify: entry at base must have plausible namespace (empty or known)
                ok = True
                for idx, _p, nm in ((i1, a, n1), (i2, b, n2)):
                    nidx = struct.unpack_from('<i', md, base + stride * idx)[0]
                    nsidx = struct.unpack_from('<i', md, base + stride * idx + 4)[0]
                    if nidx < 0 or nidx >= STR_SIZE:
                        ok = False
                        break
                    if cstr_at(STR_BASE + nidx) != nm:
                        ok = False
                        break
                    if nsidx != empty_ns and cstr_at(STR_BASE + nsidx) not in ('', 'System.Collections.Generic', 'UnityEngine'):
                        ok = False
                        break
                if ok:
                    _TYPETABLE = (base, stride)
                    return
    raise RuntimeError('type table not found')


def type_name(index):
    if _TYPETABLE is None:
        init_typetable()
    base, stride = _TYPETABLE
    if index < 0:
        return None
    off = base + stride * index
    if off + 8 > len(md):
        return None
    nidx, nsidx = struct.unpack_from('<ii', md, off)
    if nidx < 0 or nidx >= STR_SIZE:
        return None
    name = cstr_at(STR_BASE + nidx)
    ns = ''
    if 0 <= nsidx < STR_SIZE:
        ns = cstr_at(STR_BASE + nsidx)
    return (ns + '.' + name) if ns else name


def resolve(slot):
    u = usage_of(slot)
    if u is None:
        return None, None
    kind, index = u
    if kind == 1:
        return 'type:' + str(type_name(index)), (kind, index)
    return 'kind%d:%d' % (kind, index), (kind, index)


if __name__ == '__main__':
    for a in sys.argv[1:]:
        addr = int(a, 16) if a.startswith('0x') else int(a)
        r, u = resolve(addr)
        print('%s -> %s %s' % (a, r, u))
