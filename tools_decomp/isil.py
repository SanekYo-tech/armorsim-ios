#!/usr/bin/env python3
"""Просмотрщик ISIL/дизассемблера Cpp2IL с аннотациями.

- Подставляет значения float/int/double по адресам вида [0x181EB5A50] из GameAssembly.dll.
- Подставляет имена полей по смещениям [rcx+208] на основе diffable-cs (Field offset).

Использование:
  python3 isil.py TypeName [method_filter] [--raw] [--asm]
Пример:
  python3 isil.py RodGeometry NoseLengthM
"""
import os
import re
import struct
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BIN = os.path.join(ROOT, 'extracted', 'ArmorSim', 'GameAssembly.dll')
ISIL_DIR = os.path.join(ROOT, 'reversed', 'cpp2il', 'isil_fields', 'IsilDump', 'Assembly-CSharp')
if not os.path.isdir(ISIL_DIR):
    ISIL_DIR = os.path.join(ROOT, 'reversed', 'cpp2il', 'isil', 'IsilDump', 'Assembly-CSharp')
DIFF_DIR = os.path.join(ROOT, 'reversed', 'cpp2il', 'diffcs', 'DiffableCs', 'Assembly-CSharp')


class PE:
    def __init__(self, path):
        self.data = open(path, 'rb').read()
        d = self.data
        pe = struct.unpack_from('<I', d, 0x3C)[0]
        assert d[pe:pe + 4] == b'PE\0\0'
        nsec = struct.unpack_from('<H', d, pe + 6)[0]
        opt_size = struct.unpack_from('<H', d, pe + 20)[0]
        opt = pe + 24
        magic = struct.unpack_from('<H', d, opt)[0]
        assert magic == 0x20B, hex(magic)
        self.image_base = struct.unpack_from('<Q', d, opt + 24)[0]
        self.sections = []
        sec = opt + opt_size
        for i in range(nsec):
            off = sec + i * 40
            name = d[off:off + 8].rstrip(b'\0').decode('ascii', 'replace')
            vsize, vaddr, rawsize, rawptr = struct.unpack_from('<IIII', d, off + 8)
            self.sections.append((vaddr, max(vsize, rawsize), rawptr, name))

    def read(self, addr, size):
        rva = addr - self.image_base
        for vaddr, vsize, rawptr, _ in self.sections:
            if vaddr <= rva < vaddr + vsize:
                off = rawptr + (rva - vaddr)
                if off + size <= len(self.data):
                    return self.data[off:off + size]
        return None

    def annotate(self, addr):
        b = self.read(addr, 8)
        if b is None:
            return None
        f = struct.unpack('<f', b[:4])[0]
        i = struct.unpack('<i', b[:4])[0]
        parts = []
        if f == f and abs(f) < 1e30 and (f == 0 or 1e-12 < abs(f) < 1e12):
            parts.append('f=%.9g' % f)
        if -100000000 < i < 100000000:
            parts.append('i=%d' % i)
        d = struct.unpack('<d', b)[0]
        if d == d and 1e-12 < abs(d) < 1e12:
            parts.append('d=%.9g' % d)
        return ' '.join(parts)


_pe = None


def pe():
    global _pe
    if _pe is None:
        _pe = PE(BIN)
    return _pe


def field_map(type_name):
    """offset -> field name для типа (и его вложенных полей не трогаем)."""
    path = os.path.join(DIFF_DIR, type_name + '.cs')
    result = {}
    if not os.path.exists(path):
        return result
    field_re = re.compile(r'^\s*(?:public|private|internal|protected)[^;=()]*?\s(\w+)\s*;.*//Field offset: (0x[0-9A-Fa-f]+)')
    for line in open(path, encoding='utf-8', errors='ignore'):
        m = field_re.match(line)
        if m:
            result[int(m.group(2), 16)] = m.group(1)
    return result


ADDR_RE = re.compile(r'\[(0x[0-9A-Fa-f]{6,})\]')
FIELD_RE = re.compile(r'\[r(cx|dx|8|9)\+(\d+)\]')
CALL_RE = re.compile(r'Call (0x[0-9A-Fa-f]+)')

_method_names = None


def method_name(addr_hex):
    global _method_names
    if _method_names is None:
        _method_names = {}
        import csv
        path = os.path.join(ROOT, 'reversed', 'cpp2il', 'method_map.csv')
        if os.path.exists(path):
            with open(path, encoding='utf-8') as f:
                for row in csv.reader(f):
                    if row and row[0].startswith('0x'):
                        _method_names[int(row[0], 16)] = row[1] + '.' + row[2]
    return _method_names.get(int(addr_hex, 16))


def annotate_line(line, fields):
    def addr_sub(m):
        addr = int(m.group(1), 16)
        note = pe().annotate(addr)
        return '%s /* %s */' % (m.group(0), note) if note else m.group(0)

    def field_sub(m):
        off = int(m.group(2))
        name = fields.get(off)
        return '%s /* %s */' % (m.group(0), name) if name else m.group(0)

    def call_sub(m):
        name = method_name(m.group(1))
        return 'Call %s /* %s */' % (m.group(1), name) if name else m.group(0)

    line = ADDR_RE.sub(addr_sub, line)
    line = FIELD_RE.sub(field_sub, line)
    line = CALL_RE.sub(call_sub, line)
    return line


def show(type_name, method_filter=None, raw=False, show_asm=False):
    path = os.path.join(ISIL_DIR, type_name + '.txt')
    if not os.path.exists(path):
        print('Нет файла:', path)
        sys.exit(1)
    text = open(path, encoding='utf-8', errors='ignore').read()
    blocks = re.split(r'(?=^Method: )', text, flags=re.M)
    fields = field_map(type_name)
    for block in blocks:
        if not block.startswith('Method: '):
            continue
        header = block.splitlines()[0]
        if method_filter and method_filter.lower() not in header.lower():
            continue
        print('=' * 100)
        print(header)
        if raw:
            print(block)
            continue
        lines = block.splitlines()
        section = None
        for line in lines[1:]:
            if line.startswith('Disassembly:'):
                section = 'asm'
                if show_asm:
                    print(line)
                continue
            if line.startswith('ISIL:'):
                section = 'isil'
                print(line)
                continue
            if section == 'asm' and not show_asm:
                continue
            print(annotate_line(line, fields) if section == 'isil' else line)


if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    if not args:
        print(__doc__)
        sys.exit(0)
    show(args[0], args[1] if len(args) > 1 else None,
         raw='--raw' in sys.argv, show_asm='--asm' in sys.argv)
