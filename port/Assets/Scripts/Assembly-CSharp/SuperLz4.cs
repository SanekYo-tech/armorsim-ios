using System;
using System.IO;

public static class SuperLz4
{
	[ThreadStatic]
	private static int[] _table;

	private const int MinMatch = 4;

	private const int LastLiterals = 5;

	private const int MfLimit = 12;

	private const int HashLog = 16;

	private const int MaxOffset = 65535;

	private const int SkipTrigger = 6;

	public static int MaxCompressed(int n)
	{
		return n / 255 + 16 + n;
	}

	public static int Compress(byte[] src, int srcOff, int n, byte[] dst, int dstOff)
	{
		if (src == null || dst == null)
		{
			throw new ArgumentNullException((src == null) ? "src" : "dst");
		}
		if (srcOff < 0 || n < 0 || src.Length - n < srcOff)
		{
			throw new ArgumentOutOfRangeException("n", string.Format("LZ4: вход [{0}, +{1}) вне массива {2}", srcOff, n, src.Length));
		}
		if (dstOff < 0 || dst.Length - MaxCompressed(n) < dstOff)
		{
			throw new ArgumentOutOfRangeException("dstOff", string.Format("LZ4: выходу нужно {0} байт с {1}, в массиве {2}", MaxCompressed(n), dstOff, dst.Length));
		}
		int srcEnd = srcOff + n;
		int op = dstOff;
		int anchor = srcOff;
		int ip = srcOff;
		if (n > MfLimit)
		{
			int[] table = _table;
			if (table == null)
			{
				table = new int[1 << HashLog];
				_table = table;
			}
			Array.Clear(table, 0, table.Length);
			int srcLimit = srcEnd - MfLimit;
			int matchLimit = srcEnd - LastLiterals;
			table[Hash(Read32(src, ip))] = ip - srcOff + 1;
			ip++;
			int misses = 0;
			while (true)
			{
				int match;
				while (true)
				{
					if (ip > srcLimit)
					{
						goto LastLiterals;
					}
					uint seq = Read32(src, ip);
					int h = Hash(seq);
					match = table[h] + srcOff - 1;
					table[h] = ip - srcOff + 1;
					if (match >= srcOff && ip - match <= MaxOffset && Read32(src, match) == seq)
					{
						break;
					}
					ip += 1 + (misses >> SkipTrigger);
					misses++;
				}
				while (ip > anchor && match > srcOff && src[ip - 1] == src[match - 1])
				{
					ip--;
					match--;
				}
				int matchLen = Count(src, ip + 4, match + 4, matchLimit);
				int literalLen = ip - anchor;
				int tokenPos = op;
				if (literalLen < 15)
				{
					dst[op] = (byte)(literalLen << 4);
				}
				else
				{
					dst[op] = 240;
					op++;
					int rem = literalLen - 15;
					while (rem >= 255)
					{
						dst[op] = byte.MaxValue;
						op++;
						rem -= 255;
					}
					dst[op] = (byte)rem;
				}
				op++;
				Copy(src, anchor, dst, op, literalLen);
				op += literalLen;
				int offset = ip - match;
				dst[op] = (byte)offset;
				dst[op + 1] = (byte)(offset >> 8);
				op += 2;
				if (matchLen < 15)
				{
					dst[tokenPos] |= (byte)matchLen;
				}
				else
				{
					dst[tokenPos] |= 15;
					int rem = matchLen - 15;
					while (rem >= 255)
					{
						dst[op] = byte.MaxValue;
						op++;
						rem -= 255;
					}
					dst[op] = (byte)rem;
					op++;
				}
				ip += matchLen + 4;
				anchor = ip;
				if (ip > srcLimit)
				{
					goto LastLiterals;
				}
				table[Hash(Read32(src, ip - 2))] = ip - srcOff - 1;
				misses = 0;
			}
		}
		LastLiterals:
		int lastLen = srcEnd - anchor;
		if (lastLen < 15)
		{
			dst[op] = (byte)(lastLen << 4);
		}
		else
		{
			dst[op] = 240;
			op++;
			int rem = lastLen - 15;
			while (rem >= 255)
			{
				dst[op] = byte.MaxValue;
				op++;
				rem -= 255;
			}
			dst[op] = (byte)rem;
		}
		op++;
		if (lastLen > 0)
		{
			Copy(src, anchor, dst, op, lastLen);
		}
		return op + lastLen - dstOff;
	}

	public static void Decompress(byte[] src, int srcOff, int srcLen, byte[] dst, int dstOff, int dstLen)
	{
		if (src == null || dst == null)
		{
			throw new ArgumentNullException((src == null) ? "src" : "dst");
		}
		if (srcOff < 0 || srcLen < 0 || src.Length - srcLen < srcOff)
		{
			throw new IOException(string.Format("LZ4: блок [{0}, +{1}) вне буфера {2}", srcOff, srcLen, src.Length));
		}
		if (dstOff < 0 || dstLen < 0 || dst.Length - dstLen < dstOff)
		{
			throw new IOException(string.Format("LZ4: выход [{0}, +{1}) вне буфера {2}", dstOff, dstLen, dst.Length));
		}
		int ip = srcOff;
		int op = dstOff;
		int srcEnd = srcOff + srcLen;
		int dstEnd = dstOff + dstLen;
		if (srcLen == 0)
		{
			throw new IOException("LZ4: пустой блок — даже нулевой выход кодируется одним токеном");
		}
		while (ip < srcEnd)
		{
			int token = src[ip++];
			int literalLen = token >> 4;
			if (literalLen == 15)
			{
				int b;
				do
				{
					if (ip >= srcEnd)
					{
						throw new IOException(string.Format("LZ4: блок оборван в длине литералов (байт {0})", ip - srcOff));
					}
					b = src[ip++];
					literalLen += b;
					if (literalLen > dstLen)
					{
						throw new IOException(string.Format("LZ4: длина литералов {0} больше выхода {1}", literalLen, dstLen));
					}
				}
				while (b == 255);
			}
			if (literalLen > srcEnd - ip)
			{
				throw new IOException(string.Format("LZ4: литералов {0}, во входе осталось {1}", literalLen, srcEnd - ip));
			}
			if (literalLen > dstEnd - op)
			{
				throw new IOException(string.Format("LZ4: литералов {0}, в выходе осталось {1}", literalLen, dstEnd - op));
			}
			Copy(src, ip, dst, op, literalLen);
			ip += literalLen;
			op += literalLen;
			if (ip == srcEnd)
			{
				if (op != dstEnd)
				{
					throw new IOException(string.Format("LZ4: блок дал {0} байт, заявлено {1}", op - dstOff, dstLen));
				}
				return;
			}
			if (srcEnd - ip < 2)
			{
				throw new IOException(string.Format("LZ4: блок оборван в смещении (байт {0})", ip - srcOff));
			}
			int offset = src[ip] | (src[ip + 1] << 8);
			ip += 2;
			if (offset == 0)
			{
				throw new IOException(string.Format("LZ4: нулевое смещение (байт {0})", ip - 2 - srcOff));
			}
			if (offset > op - dstOff)
			{
				throw new IOException(string.Format("LZ4: смещение {0} раньше начала выхода (выход {1})", offset, op - dstOff));
			}
			int matchLen = token & 0xF;
			if (matchLen == 15)
			{
				int b;
				do
				{
					if (ip >= srcEnd)
					{
						throw new IOException(string.Format("LZ4: блок оборван в длине совпадения (байт {0})", ip - srcOff));
					}
					b = src[ip++];
					matchLen += b;
					if (matchLen > dstLen)
					{
						throw new IOException(string.Format("LZ4: длина совпадения {0} больше выхода {1}", matchLen, dstLen));
					}
				}
				while (b == 255);
			}
			matchLen += 4;
			if (matchLen > dstEnd - op)
			{
				throw new IOException(string.Format("LZ4: совпадение {0} байт, в выходе осталось {1}", matchLen, dstEnd - op));
			}
			if (offset >= matchLen)
			{
				Copy(dst, op - offset, dst, op, matchLen);
			}
			else if (matchLen >= 32)
			{
				int copied = 0;
				do
				{
					int chunk = Math.Min(copied + offset, matchLen - copied);
					Buffer.BlockCopy(dst, op - offset, dst, op + copied, chunk);
					copied += chunk;
				}
				while (copied < matchLen);
			}
			else
			{
				Copy(dst, op - offset, dst, op, matchLen);
			}
			op += matchLen;
		}
		throw new IOException(string.Format("LZ4: блок оборван перед токеном (байт {0} из {1})", ip - srcOff, srcLen));
	}

	private static uint Read32(byte[] b, int i)
	{
		return (uint)(b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24));
	}

	private static int Hash(uint v)
	{
		return (int)((v * 2654435761u) >> 16);
	}

	private static int Count(byte[] b, int p, int m, int limit)
	{
		int p0 = p;
		while (p + 8 <= limit)
		{
			ulong a = BitConverter.ToUInt64(b, p);
			ulong c = BitConverter.ToUInt64(b, m);
			if (a != c)
			{
				ulong diff = c ^ a;
				while ((diff & 0xFF) == 0)
				{
					p++;
					diff >>= 8;
				}
				return p - p0;
			}
			p += 8;
			m += 8;
		}
		while (p < limit && b[p] == b[m])
		{
			p++;
			m++;
		}
		return p - p0;
	}

	private static void Copy(byte[] s, int si, byte[] d, int di, int n)
	{
		if (n <= 0)
		{
			return;
		}
		if (n >= 32)
		{
			Buffer.BlockCopy(s, si, d, di, n);
			return;
		}
		for (int i = 0; i < n; i++)
		{
			d[di + i] = s[si + i];
		}
	}
}
