#!/usr/bin/env python3
"""Parse CrewFigure cctor asm: extract the 25 FigurePart entries."""
import re
import struct

ROOT = '/root/opencode-projects/ArmorSim'
d = open(ROOT + '/extracted/ArmorSim/GameAssembly.dll', 'rb').read()
pe = struct.unpack_from('<I', d, 0x3C)[0]
opt_size = struct.unpack_from('<H', d, pe+20)[0]
opt = pe+24
image_base = struct.unpack_from('<Q', d, opt+24)[0]
secs = []
sec = opt+opt_size
nsec = struct.unpack_from('<H', d, pe+6)[0]
for i in range(nsec):
    off = sec+i*40
    vsize, vaddr, rawsize, rawptr = struct.unpack_from('<IIII', d, off+8)
    secs.append((vaddr, max(vsize, rawsize), rawptr))


def read(addr, size):
    rva = addr - image_base
    for vaddr, vsize, rawptr in secs:
        if vaddr <= rva < vaddr+vsize:
            return d[rawptr+(rva-vaddr): rawptr+(rva-vaddr)+size]
    return None


md = open(ROOT + '/extracted/ArmorSim/ArmorSim_Data/il2cpp_data/Metadata/global-metadata.dat', 'rb').read()
lit_off, lit_size, lit_cnt = struct.unpack_from('<iii', md, 8)
dat_off, dat_size, _ = struct.unpack_from('<iii', md, 0x14)
N_LIT = lit_size // 4
LIT_ENTRIES = struct.unpack_from('<%di' % N_LIT, md, lit_off)


def resolve_lit(slot):
    raw = read(slot, 8)
    val = struct.unpack('<Q', raw)[0] & 0xFFFFFFFF
    kind = (val & 0xE0000000) >> 29
    idx = (val & 0x1FFFFFFF) >> 1
    if kind != 5:
        return 'KIND%d:%d' % (kind, idx)
    start = LIT_ENTRIES[idx]
    end = LIT_ENTRIES[idx+1] if idx+1 < N_LIT else dat_size
    return md[dat_off+start: dat_off+end].decode('utf-8')


def f32(hexstr):
    h = hexstr.rstrip('h')
    return struct.unpack('<f', struct.pack('<I', int(h, 16)))[0]


def fmt(x):
    if x is None:
        return '???'
    r = repr(x)
    if r.endswith('.0'):
        r = r[:-2]
    # shortest round-trip
    for prec in range(1, 10):
        s = '%%.%gf' % prec % x
        if struct.unpack('<f', struct.pack('<f', float(s)))[0] == struct.unpack('<f', struct.pack('<f', x))[0]:
            return s + 'f'
    return '%gf' % x


lines = open('/tmp/opencode/cf_cctor_asm.txt').read().splitlines()
entries = []
cur = None


def flush():
    global cur
    if cur and 'name' in cur:
        entries.append(cur)
    cur = None


for ln in lines:
    m = re.search(r'mov rdx,\[(1825[DE]\w+?)h\]', ln)
    if m:
        flush()
        cur = {'shape': 0, 'region': 0}
        cur['name'] = resolve_lit(int(m.group(1), 16))
        continue
    if cur is None:
        continue
    m = re.search(r'movdqa xmm0,\[(181EB\w+?)h\]', ln)
    if m:
        b = read(int(m.group(1), 16), 16)
        cur['sp'] = struct.unpack('<4f', b)
        continue
    m = re.search(r'mov dword ptr \[rbp-18h\],(\w+)h?$', ln.strip())
    if m:
        cur['pos_y'] = f32(m.group(1))
        continue
    m = re.search(r'mov qword ptr \[rbp-14h\],(\w+)h$', ln.strip())
    if m:
        v = int(m.group(1), 16)
        cur['pos_z'] = struct.unpack('<f', struct.pack('<I', v & 0xFFFFFFFF))[0]
        cur['shape'] = (v >> 32) & 0xFFFFFFFF
        continue
    m = re.search(r'mov dword ptr \[rbp-14h\],(\w+)h?$', ln.strip())
    if m:
        cur['pos_z'] = f32(m.group(1))
        continue
    m = re.search(r'mov qword ptr \[rbp-10h\],(\w+)$', ln.strip())
    if m:
        v = int(m.group(1), 16)
        cur['shape'] = v & 0xFFFFFFFF
        cur['region'] = (v >> 32) & 0xFFFFFFFF
        continue
    m = re.search(r'mov (?:dword ptr )?\[rbp-10h\],(\w+)$', ln.strip())
    if m:
        v = m.group(1)
        if v in ('edi', 'rdi', 'esi', 'rsi', 'ebx'):
            cur['shape'] = 0
        else:
            cur['shape'] = int(v) & 0xFFFFFFFF
        continue
    m = re.search(r'mov dword ptr \[rbp-0Ch\],(\w+)h?$', ln.strip())
    if m:
        v = m.group(1)
        cur['region'] = int(v, 16) if v[-1] == 'h' else int(v)
        continue
flush()

SH = {0: 'CrewShape.Box', 1: 'CrewShape.Ellipsoid'}
REG = {0: 'CrewRegion.Head', 1: 'CrewRegion.Neck', 2: 'CrewRegion.Thorax', 3: 'CrewRegion.Abdomen', 4: 'CrewRegion.Pelvis', 5: 'CrewRegion.Limb'}
for i, e in enumerate(entries):
    sp = e.get('sp', (None,)*4)
    print('Parts[%d] = P("%s", new Vector3(%s, %s, %s), new Vector3(%s, %s, %s), %s, %s);' % (
        i, e['name'], fmt(sp[0]), fmt(sp[1]), fmt(sp[2]),
        fmt(sp[3]), fmt(e.get('pos_y')), fmt(e.get('pos_z')),
        SH.get(e['shape'], e['shape']), REG.get(e['region'], e['region'])))
print('total:', len(entries))
