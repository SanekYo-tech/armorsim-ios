#!/usr/bin/env python3
"""Resolve DAT_XXXXXXXX constant addresses from GameAssembly.dll.
Usage: python3 tools_decomp/datlook.py 0x181EB5AC4 0x181EB5B4C ...
Prints float/double/int views of 8 bytes at each address (same logic as isil.py)."""
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from isil import PE, BIN

pe = PE(BIN)
for a in sys.argv[1:]:
    addr = int(a, 16)
    b = pe.read(addr, 16)
    if b is None:
        print('%s  <unmapped>' % a)
        continue
    f0 = struct.unpack('<f', b[:4])[0]
    f1 = struct.unpack('<f', b[4:8])[0]
    d0 = struct.unpack('<d', b[:8])[0]
    d1 = struct.unpack('<d', b[8:16])[0]
    i0 = struct.unpack('<i', b[:4])[0]
    print('0x%X: f=[%.9g, %.9g] d=[%.9g, %.9g] i=%d (0x%X) raw=%s' % (addr, f0, f1, d0, d1, i0, i0, b[:8].hex()))
