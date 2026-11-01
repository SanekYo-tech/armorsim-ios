using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class SuperTape
{
	public enum Detail
	{
		Full = 0,
		OneField = 1,
		BoundsOnly = 2
	}

	public sealed class BodyFrame
	{
		public Vector3 aabbLo;

		public Vector3 aabbHi;

		public Vector3[] vertices;

		public int[] triangles;

		public float[][] fields;

		public Vector3 blockLo;

		public float blockCell;

		public int bnx;

		public int bny;

		public int bnz;

		public float[][] blockFields;

		public byte[][] blockHalf;
	}

	public struct BodyTelemetry
	{
		public double kineticJ;

		public Vector3 momentum;

		public double massInDomainKg;

		public double particleVolumeM3;
	}

	public sealed class Frame
	{
		public int index;

		public float simTimeS;

		public BodyFrame[] bodies;

		public BodyTelemetry[] telemetry;

		public double escapedMassKg;

		public int verdictSoFar;

		public float[] fieldMin;

		public float[] fieldMax;

		public Vector3[] debrisPos;

		public float[][] debrisFields;

		public int debrisCount;

		public Detail detail;

		public int detailField;

		public Frame()
		{
			debrisPos = new Vector3[0];
			debrisFields = new float[0][];
			detailField = -1;
		}

		public bool HasField(int f)
		{
			if (detail == Detail.Full)
			{
				return true;
			}
			if (detail == Detail.OneField)
			{
				return f == detailField;
			}
			return false;
		}
	}

	public sealed class Header
	{
		public float cadenceS;

		public string scene;

		public string[] bodyNames;

		public string[] fieldNames;

		public string[] fieldUnits;

		public int frameCount;
	}

	public sealed class Writer : IDisposable
	{
		private readonly FileStream fs;

		private readonly BinaryWriter w;

		private readonly long frameCountPos;

		private readonly int bodyCount;

		private readonly int fieldCount;

		private readonly int version;

		private readonly Codec codec;

		private int frames;

		public bool LeaveHeaderOpen;

		public int Version => version;

		public int Frames => frames;

		public int BodyCount => bodyCount;

		public int FieldCount => fieldCount;

		public Writer(string path, float cadenceS, string scene, string[] bodyNames, string[] fieldNames, string[] fieldUnits, byte[] decor = null)
		{
			bodyCount = bodyNames.Length;
			fieldCount = fieldNames.Length;
			version = SuperTape.WriteVersion;
			if (version != 5 && version != 6)
			{
				throw new ArgumentException(string.Format("писать ленту версии {0} эта сборка не умеет (5 или 6)", version));
			}
			if (version == 6)
			{
				CheckDecor(decor, version);
			}
			codec = SuperTape.WriteCodec;
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
			w = new BinaryWriter(fs);
			w.Write(Magic);
			w.Write(version);
			w.Write(cadenceS);
			WriteString(w, scene);
			w.Write(bodyNames.Length);
			for (int i = 0; i < bodyNames.Length; i++)
			{
				WriteString(w, bodyNames[i]);
			}
			w.Write(fieldNames.Length);
			for (int i = 0; i < fieldNames.Length; i++)
			{
				WriteString(w, fieldNames[i]);
				WriteString(w, fieldUnits[i]);
			}
			if (version == 6)
			{
				WriteFlagsAndDecor(w, CodecLevel(codec), decor);
			}
			frameCountPos = fs.Position;
			w.Write(0);
			w.Write(0L);
		}

		private static void WriteFlagsAndDecor(BinaryWriter w, byte codecLevel, byte[] decor)
		{
			if (decor != null)
			{
				w.Write(FlagDecor);
				w.Write(codecLevel != 0);
				w.Write(decor.Length);
				w.Write(decor);
			}
			else
			{
				w.Write(0u);
				w.Write(codecLevel != 0);
			}
		}

		private static void CheckDecor(byte[] decor, int version)
		{
			if (decor == null)
			{
				return;
			}
			if (version != 6)
			{
				throw new ArgumentException(string.Format("декор за областью пишется только в ленту v{0}, а эта — v{1}", 6, version));
			}
			if (decor.Length == 0 || decor.Length > MaxDecorBytes)
			{
				throw new ArgumentException(string.Format("секция декора {0} байт вне пределов (1…{1})", decor.Length, MaxDecorBytes));
			}
		}

		public Writer(string path, byte[] headerFields, int bodyCount, int fieldCount, Codec codec, byte[] decor = null)
		{
			this.bodyCount = bodyCount;
			this.fieldCount = fieldCount;
			version = 6;
			this.codec = codec;
			if (decor != null && (decor.Length == 0 || decor.Length > MaxDecorBytes))
			{
				throw new ArgumentException(string.Format("секция декора {0} байт вне пределов (1…{1})", decor.Length, MaxDecorBytes));
			}
			fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
			w = new BinaryWriter(fs);
			w.Write(Magic);
			w.Write(version);
			w.Write(headerFields);
			WriteFlagsAndDecor(w, CodecLevel(codec), decor);
			frameCountPos = fs.Position;
			w.Write(0);
			w.Write(0L);
		}

		private static byte CodecLevel(Codec c)
		{
			return (byte)((c != 0) ? 1 : 0);
		}

		public long FlushAndTell()
		{
			w.Flush();
			fs.Flush(flushToDisk: true);
			return fs.Position;
		}

		public long Tell()
		{
			w.Flush();
			return fs.Position;
		}

		public void FlushToDisk()
		{
			w.Flush();
			fs.Flush(flushToDisk: true);
		}

		public Writer(string path, long resumeBytes, int resumeFrames)
		{
			long pos = HeaderFrameCountPos(path, out int bodyCount2, out int fieldCount2, out int version2);
			codec = SuperTape.WriteCodec;
			fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
			if (resumeBytes < pos + 12 || resumeBytes > fs.Length)
			{
				throw new IOException(string.Format("дозапись ленты: длина {0} вне файла ({1})", resumeBytes, fs.Length));
			}
			fs.SetLength(resumeBytes);
			fs.Seek(resumeBytes, SeekOrigin.Begin);
			w = new BinaryWriter(fs);
			frameCountPos = pos;
			frames = resumeFrames;
		}

		public static long HeaderFrameCountPos(string path, out int bodyCount, out int fieldCount)
		{
			return HeaderFrameCountPos(path, out bodyCount, out fieldCount, out int _);
		}

		public static long HeaderFrameCountPos(string path, out int bodyCount, out int fieldCount, out int version)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write | FileShare.Delete))
			{
				BinaryReader r = new BinaryReader(fs);
				if ((uint)r.ReadInt32() != Magic)
				{
					throw new IOException("не лента SUPER");
				}
				version = r.ReadInt32();
				if (!SuperTape.Readable(version))
				{
					throw new IOException(string.Format("чужая версия ленты ({0})", version));
				}
				r.ReadSingle();
				SuperTape.ReadString(r);
				bodyCount = r.ReadInt32();
				for (int i = 0; i < bodyCount; i++)
				{
					SuperTape.ReadString(r);
				}
				fieldCount = r.ReadInt32();
				for (int i = 0; i < fieldCount; i++)
				{
					SuperTape.ReadString(r);
					SuperTape.ReadString(r);
				}
				if (version == 6)
				{
					uint flags = r.ReadUInt32();
					if ((flags & ~KnownFlags) != 0)
					{
						throw new IOException(string.Format("флаги ленты 0x{0:X8} — эта сборка их не понимает", flags));
					}
					r.ReadByte();
					if ((flags & FlagDecor) != 0)
					{
						int decorLen = r.ReadInt32();
						if (decorLen < 1 || decorLen > MaxDecorBytes || fs.Position + decorLen > fs.Length)
						{
							throw new IOException(string.Format("секция декора шапки длиной {0} — шапка битая", decorLen));
						}
						fs.Seek(decorLen, SeekOrigin.Current);
					}
				}
				return fs.Position;
			}
		}

		public static byte[] ReadDecorBytes(string path)
		{
			byte[] result = null;
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write | FileShare.Delete))
			{
				BinaryReader r = new BinaryReader(fs);
				if ((uint)r.ReadInt32() != Magic)
				{
					throw new IOException("не лента SUPER");
				}
				int version = r.ReadInt32();
				if (version == 5)
				{
					result = null;
				}
				else if (version == 6)
				{
					r.ReadSingle();
					SuperTape.ReadString(r);
					int nb = r.ReadInt32();
					for (int i = 0; i < nb; i++)
					{
						SuperTape.ReadString(r);
					}
					int nf = r.ReadInt32();
					for (int i = 0; i < nf; i++)
					{
						SuperTape.ReadString(r);
						SuperTape.ReadString(r);
					}
					uint flags = r.ReadUInt32();
					if ((flags & ~KnownFlags) != 0)
					{
						throw new IOException(string.Format("флаги ленты 0x{0:X8} — эта сборка их не понимает", flags));
					}
					r.ReadByte();
					if ((flags & FlagDecor) != 0)
					{
						int decorLen = r.ReadInt32();
						if (decorLen < 1 || decorLen > MaxDecorBytes || fs.Position + decorLen > fs.Length)
						{
							throw new IOException(string.Format("секция декора шапки длиной {0} — шапка битая", decorLen));
						}
						result = r.ReadBytes(decorLen);
					}
				}
				else
				{
					throw new IOException(string.Format("чужая версия ленты ({0})", version));
				}
			}
			return result;
		}

		public void WriteFrame(Frame f)
		{
			Encoded e;
			if (version == 6)
			{
				e = SuperTape.EncodeV6(f, bodyCount, fieldCount, codec);
			}
			else
			{
				byte[] payload = Encode(f, bodyCount, fieldCount, out ulong hash);
				e = new Encoded();
				e.version = version;
				e.bytes = payload;
				e.hash = hash;
				e.canonicalLen = payload.Length;
			}
			WriteEncoded(e);
		}

		public Encoded EncodeFrame(Frame f)
		{
			if (version == 6)
			{
				return EncodeV6(f, bodyCount, fieldCount, codec);
			}
			byte[] payload = Encode(f, bodyCount, fieldCount, out ulong hash);
			Encoded e = new Encoded();
			e.version = version;
			e.bytes = payload;
			e.hash = hash;
			e.canonicalLen = payload.Length;
			return e;
		}

		public void WriteEncoded(Encoded e)
		{
			if (e.version != version)
			{
				throw new ArgumentException(string.Format("кадр закодирован версией {0}, лента — версии {1}", e.version, version));
			}
			if (version == 6)
			{
				w.Write(e.bytes.Length);
				w.Write(~e.bytes.Length);
				w.Write(e.bytes);
				w.Write(e.storedHash);
				w.Write(e.hash);
			}
			else
			{
				w.Write(e.bytes.Length);
				w.Write(e.bytes);
				w.Write(e.hash);
			}
			frames++;
		}

		public void WriteEncoded(byte[] payload, ulong hash)
		{
			Encoded e = new Encoded();
			e.version = version;
			e.bytes = payload;
			e.hash = hash;
			e.canonicalLen = payload.Length;
			WriteEncoded(e);
		}

		public static byte[] Encode(Frame f, int bodyCount, int fieldCount, out ulong hash)
		{
			ValidateShape(f, bodyCount, fieldCount);
			using (MemoryStream ms = new MemoryStream())
			{
				BinaryWriter b = new BinaryWriter(ms);
				b.Write(f.index);
				b.Write(f.simTimeS);
				for (int i = 0; i < f.bodies.Length; i++)
				{
					WriteBody(b, f.bodies[i]);
				}
				for (int i = 0; i < f.telemetry.Length; i++)
				{
					b.Write(f.telemetry[i].kineticJ);
					b.Write(f.telemetry[i].momentum.x);
					b.Write(f.telemetry[i].momentum.y);
					b.Write(f.telemetry[i].momentum.z);
					b.Write(f.telemetry[i].massInDomainKg);
					b.Write(f.telemetry[i].particleVolumeM3);
				}
				b.Write(f.escapedMassKg);
				b.Write(f.verdictSoFar);
				b.Write(f.fieldMin.Length);
				for (int i = 0; i < f.fieldMin.Length; i++)
				{
					b.Write(f.fieldMin[i]);
					b.Write(f.fieldMax[i]);
				}
				b.Write(f.debrisCount);
				for (int i = 0; i < f.debrisCount; i++)
				{
					b.Write(f.debrisPos[i].x);
					b.Write(f.debrisPos[i].y);
					b.Write(f.debrisPos[i].z);
					for (int c = 0; c < fieldCount; c++)
					{
						b.Write(Mathf.FloatToHalf(f.debrisFields[c][i]));
					}
				}
				byte[] payload = ms.ToArray();
				hash = Fnv1a(BitConverter.GetBytes(payload.Length), 0, 4);
				hash = Fnv1a(payload, 0, payload.Length, hash);
				return payload;
			}
		}

		private static void WriteBody(BinaryWriter b, BodyFrame bf)
		{
			b.Write(bf.aabbLo.x);
			b.Write(bf.aabbLo.y);
			b.Write(bf.aabbLo.z);
			b.Write(bf.aabbHi.x);
			b.Write(bf.aabbHi.y);
			b.Write(bf.aabbHi.z);
			b.Write(bf.vertices.Length);
			b.Write(bf.triangles.Length / 3);
			for (int i = 0; i < bf.vertices.Length; i++)
			{
				b.Write(Mathf.FloatToHalf(bf.vertices[i].x - bf.aabbLo.x));
				b.Write(Mathf.FloatToHalf(bf.vertices[i].y - bf.aabbLo.y));
				b.Write(Mathf.FloatToHalf(bf.vertices[i].z - bf.aabbLo.z));
			}
			for (int c = 0; c < bf.fields.Length; c++)
			{
				for (int i = 0; i < bf.fields[c].Length; i++)
				{
					b.Write(Mathf.FloatToHalf(bf.fields[c][i]));
				}
			}
			b.Write(bf.vertices.Length <= 0xFFFF);
			if (bf.vertices.Length > 0xFFFF)
			{
				for (int i = 0; i < bf.triangles.Length; i++)
				{
					b.Write(bf.triangles[i]);
				}
			}
			else
			{
				for (int i = 0; i < bf.triangles.Length; i++)
				{
					b.Write((ushort)bf.triangles[i]);
				}
			}
			b.Write(bf.blockLo.x);
			b.Write(bf.blockLo.y);
			b.Write(bf.blockLo.z);
			b.Write(bf.blockCell);
			b.Write(bf.bnx);
			b.Write(bf.bny);
			b.Write(bf.bnz);
			for (int c = 0; c < bf.blockFields.Length; c++)
			{
				for (int i = 0; i < bf.blockFields[c].Length; i++)
				{
					b.Write(Mathf.FloatToHalf(bf.blockFields[c][i]));
				}
			}
		}

		public void Dispose()
		{
			Pool.Clear();
			if (!LeaveHeaderOpen)
			{
				fs.Seek(frameCountPos, SeekOrigin.Begin);
				w.Write(frames);
				w.Flush();
				byte[] head = new byte[frameCountPos + 4];
				fs.Seek(0L, SeekOrigin.Begin);
				int done = 0;
				while (done < head.Length)
				{
					int n = fs.Read(head, done, head.Length - done);
					if (n <= 0)
					{
						break;
					}
					done += n;
				}
				fs.Seek(frameCountPos + 4, SeekOrigin.Begin);
				ulong h = Fnv1a(head, 0, head.Length);
				w.Write(h);
			}
			else
			{
				w.Flush();
			}
			w.Dispose();
			fs.Close();
		}
	}

	public sealed class Reader : IDisposable
	{
		private sealed class Cursor
		{
			private readonly FileStream fs;

			private readonly byte[] win;

			private long start;

			private long pos;

			private long winAt;

			private int winLen;

			public long Consumed => pos - start;

			public Cursor(FileStream fs)
			{
				winAt = -1L;
				this.fs = fs;
				win = new byte[65536];
			}

			public Cursor(byte[] frame, int len)
			{
				winAt = -1L;
				fs = null;
				win = frame;
				winLen = len;
				winAt = 0L;
			}

			public void Begin(long payloadStart)
			{
				start = payloadStart;
				pos = payloadStart;
				if (fs != null)
				{
					winAt = -1L;
					winLen = 0;
				}
			}

			private int Win(int n)
			{
				if (winAt >= 0 && winAt <= pos && pos + n <= winAt + winLen)
				{
					return (int)(pos - winAt);
				}
				if (fs == null)
				{
					throw new IOException(string.Format("кадр кончился на {0}: ждали ещё {1} байт из {2}", pos, n, winLen));
				}
				fs.Seek(pos, SeekOrigin.Begin);
				winAt = pos;
				winLen = 0;
				while (n > winLen)
				{
					int got = fs.Read(win, winLen, win.Length - winLen);
					if (got <= 0)
					{
						throw new IOException(string.Format("лента кончилась на {0}: ждали ещё {1} байт", pos + winLen, n - winLen));
					}
					winLen += got;
				}
				return 0;
			}

			public int I32()
			{
				int at = Win(4);
				pos += 4;
				return (int)BitConverter.ToUInt32(win, at);
			}

			public float F32()
			{
				int at = Win(4);
				pos += 4;
				return BitConverter.ToSingle(win, at);
			}

			public double F64()
			{
				int at = Win(8);
				pos += 8;
				return BitConverter.ToDouble(win, at);
			}

			public byte U8()
			{
				int at = Win(1);
				pos += 1;
				return win[at];
			}

			public Vector3 V3()
			{
				float x = F32();
				float y = F32();
				float z = F32();
				return new Vector3(x, y, z);
			}

			public void Skip(long n)
			{
				if (n < 0)
				{
					throw new IOException(string.Format("пропуск {0} байт — длина в ленте отрицательная", n));
				}
				pos += n;
			}

			public void Bulk(byte[] dst, int off, int n)
			{
				if (n <= 0)
				{
					return;
				}
				if (winAt >= 0 && winAt <= pos && pos + n <= winAt + winLen)
				{
					Buffer.BlockCopy(win, (int)(pos - winAt), dst, off, n);
					pos += n;
					return;
				}
				if (fs == null)
				{
					throw new IOException(string.Format("кадр кончился на {0}: ждали {1} байт из {2}", pos, n, winLen));
				}
				fs.Seek(pos, SeekOrigin.Begin);
				int done = 0;
				while (done < n)
				{
					int got = fs.Read(dst, off + done, n - done);
					if (got <= 0)
					{
						throw new IOException(string.Format("лента кончилась на {0}: ждали {1} байт", pos + done, n));
					}
					done += got;
				}
				pos += n;
				winAt = -1L;
				winLen = 0;
			}
		}

		private struct Sec
		{
			public SectionKind kind;

			public Transform t;

			public Codec c;

			public int k;

			public int body;

			public int channel;

			public int rawLen;

			public int storedLen;

			public ulong hash;

			public long pos;
		}

		public struct SectionView
		{
			public SectionKind kind;

			public Transform transform;

			public Codec codec;

			public int body;

			public int channel;

			public int rawLen;

			public int storedLen;

			public long filePos;
		}

		private sealed class Job
		{
			public Sec s;

			public bool idx16;

			public byte[] blob;

			public Action<byte[]> use;

			public byte[] keep;
		}

		public Header header;

		private readonly FileStream fs;

		private long[] frameOffset;

		private readonly long fileBytes;

		private byte[] scratch;

		private byte[] headerFields;

		private Cursor cursor;

		private byte[] v6Rec;

		public int FrameCount => header.frameCount;

		public long FileBytes => fileBytes;

		public int FormatVersion { get; private set; }

		public uint Flags { get; private set; }

		public byte CodecLevelByte { get; private set; }

		public byte[] DecorBytes { get; private set; }

		public bool Recovered { get; private set; }

		public string RecoveryNote { get; private set; }

		public long TrailingBytes { get; private set; }

		public string FilePath { get; private set; }

		public string LastVerifyNote { get; private set; }

		public byte[] HeaderFields()
		{
			return (byte[])headerFields.Clone();
		}

		public static Reader OpenRecovering(string path)
		{
			Reader r;
			try
			{
				r = new Reader(path, recover: false);
			}
			catch (Exception ex)
			{
				r = new Reader(path, recover: true);
				r.RecoveryNote = ex.Message + "; " + r.RecoveryNote;
			}
			return r;
		}

		public Reader(string path)
			: this(path, recover: false)
		{
		}

		public Reader SecondHandle()
		{
			return new Reader(this);
		}

		private Reader(Reader src)
		{
			scratch = new byte[65536];
			RecoveryNote = "";
			LastVerifyNote = "";
			v6Rec = new byte[0];
			FilePath = src.FilePath;
			fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write | FileShare.Delete, 1048576, FileOptions.RandomAccess);
			header = src.header;
			frameOffset = src.frameOffset;
			fileBytes = src.fileBytes;
			Recovered = src.Recovered;
			RecoveryNote = src.RecoveryNote;
			TrailingBytes = src.TrailingBytes;
			FormatVersion = src.FormatVersion;
			Flags = src.Flags;
			CodecLevelByte = src.CodecLevelByte;
			headerFields = src.headerFields;
			DecorBytes = src.DecorBytes;
		}

		private Reader(string path, bool recover)
		{
			scratch = new byte[65536];
			RecoveryNote = "";
			LastVerifyNote = "";
			v6Rec = new byte[0];
			FilePath = path;
			fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write | FileShare.Delete, 1048576, FileOptions.RandomAccess);
			fileBytes = fs.Length;
			BinaryReader r = new BinaryReader(fs);
			uint magic = (uint)r.ReadInt32();
			if (magic != Magic)
			{
				throw new IOException(string.Format("не лента SUPER v5: magic 0x{0:X8}, ожидался 0x{1:X8}", magic, Magic));
			}
			FormatVersion = r.ReadInt32();
			if (!SuperTape.Readable(FormatVersion))
			{
				throw new IOException(string.Format("версия формата {0}, эта сборка читает {1} и {2} — отказ без частичного чтения ", FormatVersion, 5, 6) + "(зеркально GeometryTimelineFile: чужую версию не разбираем и не угадываем)");
			}
			header = new Header();
			header.cadenceS = r.ReadSingle();
			header.scene = SuperTape.ReadString(r);
			int nb = r.ReadInt32();
			if (nb > 4096)
			{
				throw new IOException(string.Format("тел в шапке {0} — шапка битая или не дописана", nb));
			}
			header.bodyNames = new string[nb];
			for (int i = 0; i < nb; i++)
			{
				header.bodyNames[i] = SuperTape.ReadString(r);
			}
			int nf = r.ReadInt32();
			if (nf > 256)
			{
				throw new IOException(string.Format("каналов в шапке {0} — шапка битая или не дописана", nf));
			}
			header.fieldNames = new string[nf];
			header.fieldUnits = new string[nf];
			for (int i = 0; i < nf; i++)
			{
				header.fieldNames[i] = SuperTape.ReadString(r);
				header.fieldUnits[i] = SuperTape.ReadString(r);
			}
			long firstFrame = fs.Position;
			if (FormatVersion == 6)
			{
				Flags = r.ReadUInt32();
				if ((Flags & ~KnownFlags) != 0)
				{
					throw new IOException(string.Format("флаги ленты 0x{0:X8}: эта сборка знает только 0x{1:X8} — отказ без частичного чтения", Flags, KnownFlags));
				}
				CodecLevelByte = r.ReadByte();
				if ((Flags & FlagDecor) != 0)
				{
					int decorLen = r.ReadInt32();
					if (decorLen < 1 || decorLen > MaxDecorBytes || fs.Position + decorLen > fileBytes)
					{
						throw new IOException(string.Format("секция декора шапки длиной {0} — шапка битая или не дописана", decorLen));
					}
					DecorBytes = r.ReadBytes(decorLen);
				}
			}
			header.frameCount = r.ReadInt32();
			long headerEnd = fs.Position;
			ulong storedHash = r.ReadUInt64();
			byte[] image = At(0, (int)headerEnd);
			ulong computedHash = SuperTape.Fnv1a(image, 0, (int)headerEnd);
			headerFields = new byte[firstFrame - 8];
			Buffer.BlockCopy(image, 8, headerFields, 0, headerFields.Length);
			if (FormatVersion == 6)
			{
				OpenV6(recover, headerEnd + 8, storedHash == computedHash, header.frameCount);
				return;
			}
			if (!recover)
			{
				if (storedHash != computedHash)
				{
					throw new IOException("хэш шапки не сошёлся — лента не дописана (запись оборвалась до закрытия) или шапка повреждена; частичного чтения нет");
				}
				if (header.frameCount < 1)
				{
					throw new IOException("в ленте 0 кадров — пустую ленту не показываем, это отказ");
				}
				fs.Seek(headerEnd + 8, SeekOrigin.Begin);
				frameOffset = new long[header.frameCount];
				for (int i = 0; i < header.frameCount; i++)
				{
					frameOffset[i] = fs.Position;
					int frameLen = r.ReadInt32();
					if (frameLen < 1 || fs.Position + 8 + frameLen > fileBytes)
					{
						throw new IOException(string.Format("кадр {0}: длина {1} выходит за файл — лента усечена", i, frameLen));
					}
					fs.Seek(frameLen + 8, SeekOrigin.Current);
				}
				if (fs.Position != fileBytes)
				{
					throw new IOException(string.Format("за последним кадром {0} лишних байт — ", fileBytes - fs.Position) + "счётчик кадров битый или запись оборвана");
				}
				return;
			}
			fs.Seek(headerEnd + 8, SeekOrigin.Begin);
			List<long> offsets = new List<long>();
			long scan = fs.Position;
			while (scan + 4 <= fileBytes)
			{
				byte[] head = At(scan, 4);
				int len = (int)BitConverter.ToUInt32(head, 0);
				if (len < 1 || fileBytes < len + 12 + scan)
				{
					break;
				}
				byte[] rec = At(scan, len + 12);
				ulong h = SuperTape.Fnv1a(rec, 0, 4);
				h = SuperTape.Fnv1a(rec, 4, len, h);
				ulong stored = BitConverter.ToUInt64(rec, len + 4);
				if (h != stored)
				{
					break;
				}
				int idx = BitConverter.ToInt32(rec, 4);
				if (idx != offsets.Count)
				{
					break;
				}
				offsets.Add(scan);
				scan += len + 12;
			}
			if (offsets.Count == 0)
			{
				throw new IOException("ни одного целого кадра — восстанавливать нечего");
			}
			header.frameCount = offsets.Count;
			frameOffset = offsets.ToArray();
			TrailingBytes = fileBytes - scan;
			Recovered = true;
			RecoveryNote = string.Format("восстановлено {0} кадров, за ними {1} байт ", offsets.Count, TrailingBytes) + "оборванной записи" + ((storedHash == computedHash) ? "" : "; хэш шапки не записан (запись не была закрыта)");
		}

		public void Dispose()
		{
			fs?.Close();
		}

		private byte[] At(long at, int count)
		{
			Need(count);
			fs.Seek(at, SeekOrigin.Begin);
			int done = 0;
			while (done < count)
			{
				int n = fs.Read(scratch, done, count - done);
				if (n <= 0)
				{
					throw new IOException(string.Format("лента кончилась на {0}: ждали {1} байт", at + done, count));
				}
				done += n;
			}
			return scratch;
		}

		public long FramePayloadOffset(int index)
		{
			return frameOffset[index] + ((FormatVersion == 6) ? 8 : 4);
		}

		public float FrameTime(int index)
		{
			if (FormatVersion == 6)
			{
				return FrameTimeV6(index);
			}
			return BitConverter.ToSingle(At(FramePayloadOffset(index) + 4, 4), 0);
		}

		public int VerifyHashes()
		{
			for (int i = 0; i < header.frameCount; i++)
			{
				if (!FrameHashOk(i))
				{
					return i;
				}
			}
			return -1;
		}

		public bool FrameHashOk(int i)
		{
			if (FormatVersion == 6)
			{
				return StoredHashOkV6(i);
			}
			byte[] head = At(frameOffset[i], 4);
			uint len = BitConverter.ToUInt32(head, 0);
			byte[] rec = At(frameOffset[i], (int)(len + 12));
			ulong h = SuperTape.Fnv1a(rec, 0, 4);
			h = SuperTape.Fnv1a(rec, 4, (int)len, h);
			return h == BitConverter.ToUInt64(rec, (int)(len + 4));
		}

		public int VerifyCanonical()
		{
			LastVerifyNote = "";
			int i = 0;
			try
			{
				for (; i < header.frameCount; i++)
				{
					if (FormatVersion == 6)
					{
						ReadCanonical(i, out int len);
					}
					else if (!FrameHashOk(i))
					{
						LastVerifyNote = string.Format("кадр {0}: хэш кадра не сошёлся", i);
						return i;
					}
				}
			}
			catch (Exception ex)
			{
				LastVerifyNote = ex.Message;
				return i;
			}
			return -1;
		}

		public int CanonicalLengthOf(int i)
		{
			long at = frameOffset[i];
			if (FormatVersion == 6)
			{
				RecordLenV6(i);
				at += 8;
			}
			return (int)BitConverter.ToUInt32(At(at, 4), 0);
		}

		public ulong CanonicalHashOf(int i)
		{
			long at = ((FormatVersion == 6) ? (RecordLenV6(i) + frameOffset[i] + 16) : (frameOffset[i] + 4 + CanonicalLengthOf(i)));
			return BitConverter.ToUInt64(At(at, 8), 0);
		}

		public byte[] ReadCanonical(int i, out int len)
		{
			if (FormatVersion == 6)
			{
				return ReadCanonicalV6(i, out len);
			}
			len = (int)BitConverter.ToUInt32(At(frameOffset[i], 4), 0);
			byte[] dst = new byte[len];
			Buffer.BlockCopy(At(frameOffset[i] + 4, len), 0, dst, 0, len);
			return dst;
		}

		public Frame ReadFrame(int index)
		{
			return ReadFrame(index, Detail.Full, -1);
		}

		public Frame ReadFrame(int index, Detail detail, int field)
		{
			if (index < 0 || index >= header.frameCount)
			{
				throw new ArgumentOutOfRangeException("index", string.Format("кадр {0} вне ленты (кадров {1})", index, header.frameCount));
			}
			int nf = header.fieldNames.Length;
			if (detail == Detail.OneField && (field < 0 || field >= nf))
			{
				throw new ArgumentOutOfRangeException("field", string.Format("канал {0} вне ленты (каналов {1})", field, nf));
			}
			if (FormatVersion == 6)
			{
				if (detail != Detail.Full)
				{
					return ReadPartialV6(index, detail, field);
				}
				byte[] bytes = ReadCanonicalV6(index, out int len);
				Cursor cur = new Cursor(bytes, len);
				return Parse(cur, index, len, Detail.Full, field);
			}
			int len2 = (int)BitConverter.ToUInt32(At(frameOffset[index], 4), 0);
			if (cursor == null)
			{
				cursor = new Cursor(fs);
			}
			cursor.Begin(FramePayloadOffset(index));
			return Parse(cursor, index, len2, detail, field);
		}

		private Frame Parse(Cursor cur, int index, int len, Detail detail, int field)
		{
			int nf = header.fieldNames.Length;
			Frame f = new Frame();
			f.detail = detail;
			f.detailField = ((detail == Detail.OneField) ? field : (-1));
			f.index = cur.I32();
			f.simTimeS = cur.F32();
			f.bodies = new BodyFrame[header.bodyNames.Length];
			f.telemetry = new BodyTelemetry[header.bodyNames.Length];
			for (int b = 0; b < f.bodies.Length; b++)
			{
				f.bodies[b] = ReadBody(cur, nf, detail, field);
			}
			for (int b = 0; b < f.telemetry.Length; b++)
			{
				double kineticJ = cur.F64();
				Vector3 momentum = default(Vector3);
				momentum.x = cur.F32();
				momentum.y = cur.F32();
				momentum.z = cur.F32();
				double massInDomainKg = cur.F64();
				double particleVolumeM3 = cur.F64();
				f.telemetry[b].kineticJ = kineticJ;
				f.telemetry[b].momentum = momentum;
				f.telemetry[b].massInDomainKg = massInDomainKg;
				f.telemetry[b].particleVolumeM3 = particleVolumeM3;
			}
			f.escapedMassKg = cur.F64();
			f.verdictSoFar = cur.I32();
			int nf2 = cur.I32();
			if (nf2 > 256)
			{
				throw new IOException(string.Format("кадр {0}: границ полей {1} — лента битая", index, nf2));
			}
			f.fieldMin = new float[nf2];
			f.fieldMax = new float[nf2];
			for (int i = 0; i < nf2; i++)
			{
				f.fieldMin[i] = cur.F32();
				f.fieldMax[i] = cur.F32();
			}
			f.debrisCount = cur.I32();
			if (f.debrisCount > 0x400000)
			{
				throw new IOException(string.Format("кадр {0}: счётчик обломков {1} — лента битая", index, f.debrisCount));
			}
			f.debrisPos = new Vector3[f.debrisCount];
			f.debrisFields = new float[nf][];
			for (int i = 0; i < nf; i++)
			{
				if (Wanted(i, detail, field))
				{
					f.debrisFields[i] = new float[f.debrisCount];
				}
			}
			if (f.debrisCount > 0)
			{
				int stride = 2 * nf + 12;
				Need(stride * f.debrisCount);
				cur.Bulk(scratch, 0, stride * f.debrisCount);
				for (int i = 0; i < f.debrisCount; i++)
				{
					f.debrisPos[i] = new Vector3(BitConverter.ToSingle(scratch, i * stride), BitConverter.ToSingle(scratch, i * stride + 4), BitConverter.ToSingle(scratch, i * stride + 8));
					for (int c = 0; c < nf; c++)
					{
						if (f.debrisFields[c] != null)
						{
							f.debrisFields[c][i] = SuperHalf.ToFloat(scratch, i * stride + 12 + 2 * c);
						}
					}
				}
			}
			if (cur.Consumed != len)
			{
				throw new IOException(string.Format("кадр {0}: разобрано {1} байт ", index, cur.Consumed) + string.Format("из {0} — формат разъехался с читателем", len));
			}
			return f;
		}

		private static bool Wanted(int channel, Detail detail, int field)
		{
			if (detail == Detail.Full)
			{
				return true;
			}
			if (detail != Detail.OneField)
			{
				return false;
			}
			return channel == field;
		}

		private void Need(int n)
		{
			if (n < 0)
			{
				throw new IOException(string.Format("буфер на {0} байт — длина в ленте битая", n));
			}
			if (scratch.Length >= n)
			{
				return;
			}
			int size = ((scratch.Length != 0) ? scratch.Length : 65536);
			while (size < n && size < MaxCanonicalBytes)
			{
				size *= 2;
			}
			scratch = new byte[Math.Max(size, n)];
		}

		private BodyFrame ReadBody(Cursor cur, int nf, Detail detail, int field)
		{
			BodyFrame bf = new BodyFrame();
			bf.aabbLo = cur.V3();
			bf.aabbHi = cur.V3();
			int nv = cur.I32();
			int ni = cur.I32();
			if (nv < 0 || ni < 0)
			{
				throw new IOException(string.Format("вершин {0}, треугольников {1} — лента битая", nv, ni));
			}
			if (detail != Detail.BoundsOnly)
			{
				Need(6 * nv);
				cur.Bulk(scratch, 0, 6 * nv);
				bf.vertices = DecodeVertices(scratch, 0, nv, bf.aabbLo);
			}
			else
			{
				cur.Skip(6 * nv);
				bf.vertices = new Vector3[0];
			}
			bf.fields = new float[nf][];
			for (int c = 0; c < nf; c++)
			{
				if (Wanted(c, detail, field))
				{
					Need(2 * nv);
					cur.Bulk(scratch, 0, 2 * nv);
					bf.fields[c] = DecodeHalfs(scratch, 0, nv);
				}
				else
				{
					cur.Skip(2 * nv);
				}
			}
			bool idx16 = cur.U8() != 0;
			int ni3 = 3 * ni;
			if (detail != Detail.BoundsOnly)
			{
				int elem = idx16 ? 2 : 4;
				Need(elem * ni3);
				cur.Bulk(scratch, 0, elem * ni3);
				bf.triangles = DecodeTriangles(scratch, 0, ni3, idx16);
			}
			else
			{
				cur.Skip((idx16 ? 2 : 4) * ni3);
				bf.triangles = new int[0];
			}
			bf.blockLo = cur.V3();
			bf.blockCell = cur.F32();
			bf.bnx = cur.I32();
			bf.bny = cur.I32();
			bf.bnz = cur.I32();
			long cells = (long)bf.bny * bf.bnx * bf.bnz;
			if (bf.bnx < 0 || bf.bny < 0 || bf.bnz < 0 || cells > 67108864)
			{
				throw new IOException(string.Format("блок {0}×{1}×{2} — лента битая", bf.bnx, bf.bny, bf.bnz));
			}
			bf.blockHalf = new byte[nf][];
			for (int c = 0; c < nf; c++)
			{
				if (Wanted(c, detail, field))
				{
					byte[] buf = new byte[2 * cells];
					cur.Bulk(buf, 0, buf.Length);
					bf.blockHalf[c] = buf;
				}
				else
				{
					cur.Skip(2 * cells);
				}
			}
			return bf;
		}

		private static Vector3[] DecodeVertices(byte[] src, int off, int nv, Vector3 lo)
		{
			Vector3[] dst = new Vector3[nv];
			for (int i = 0; i < nv; i++)
			{
				dst[i] = new Vector3(SuperHalf.ToFloat(src, off + 6 * i) + lo.x, SuperHalf.ToFloat(src, off + 6 * i + 2) + lo.y, SuperHalf.ToFloat(src, off + 6 * i + 4) + lo.z);
			}
			return dst;
		}

		private static float[] DecodeHalfs(byte[] src, int off, int n)
		{
			float[] dst = new float[n];
			for (int i = 0; i < n; i++)
			{
				dst[i] = SuperHalf.ToFloat(src, off + 2 * i);
			}
			return dst;
		}

		private static int[] DecodeTriangles(byte[] src, int off, int ni, bool idx16)
		{
			int[] dst = new int[ni];
			if (!idx16)
			{
				Buffer.BlockCopy(src, off, dst, 0, 4 * ni);
				return dst;
			}
			for (int i = 0; i < ni; i++)
			{
				dst[i] = (int)(src[off + 2 * i] | (src[off + 2 * i + 1] << 8));
			}
			return dst;
		}

		private static string SecName(Sec s)
		{
			string text = s.kind.ToString();
			string text2 = (s.body >= 0) ? (s.body.ToString() + " тела ") : "";
			string text3 = (s.channel >= 0) ? (s.channel.ToString() + " канала ") : "";
			return text + text2 + text3;
		}

		private void OpenV6(bool recover, long firstFrame, bool headerOk, int declaredFrames)
		{
			if (!recover)
			{
				if (!headerOk)
				{
					throw new IOException("хэш шапки не сошёлся — лента не дописана (запись оборвалась до закрытия) или шапка повреждена; частичного чтения нет");
				}
				if (declaredFrames <= 0)
				{
					throw new IOException("в ленте 0 кадров — пустую ленту не показываем, это отказ");
				}
				frameOffset = new long[declaredFrames];
				long at = firstFrame;
				for (int i = 0; i < declaredFrames; i++)
				{
					if (at + 8 > fileBytes)
					{
						throw new IOException(string.Format("кадр {0}: запись начинается за концом файла — лента усечена", i));
					}
					byte[] head = At(at, 8);
					uint len = BitConverter.ToUInt32(head, 0);
					uint inv = BitConverter.ToUInt32(head, 4);
					if (inv != ~len)
					{
						throw new IOException(string.Format("кадр {0}: длина {1} не сходится со своим дополнением 0x{2:X8} — поле длины испорчено", i, len, inv));
					}
					if (len <= 0 || at + len + 24 > fileBytes)
					{
						throw new IOException(string.Format("кадр {0}: длина {1} выходит за файл — лента усечена", i, len));
					}
					frameOffset[i] = at;
					at += len + 24;
				}
				if (at != fileBytes)
				{
					throw new IOException(string.Format("за последним кадром {0} лишних байт — счётчик кадров битый или запись оборвана", fileBytes - at));
				}
				return;
			}
			List<long> offsets = new List<long>();
			string note = "";
			long at2 = firstFrame;
			while (at2 + 8 <= fileBytes)
			{
				byte[] head = At(at2, 8);
				uint len = BitConverter.ToUInt32(head, 0);
				uint inv = BitConverter.ToUInt32(head, 4);
				if (len <= 0 || inv != ~len)
				{
					note = string.Format("кадр {0}: длина {1} не сходится со своим дополнением", offsets.Count, len);
					break;
				}
				if (at2 + len + 24 > fileBytes)
				{
					note = string.Format("кадр {0}: запись обрублена", offsets.Count);
					break;
				}
				byte[] rec = At(at2 + 8, (int)(len + 16));
				ulong computed = SuperTape.StoredHash(rec, 0, (int)len);
				ulong stored = BitConverter.ToUInt64(rec, (int)len);
				if (computed != stored)
				{
					note = string.Format("кадр {0}: хранимый хэш не сошёлся", offsets.Count);
					break;
				}
				int idx = FrameIndexInBlock(rec, (int)len);
				if (idx != offsets.Count)
				{
					note = string.Format("кадр {0}: в голове записан номер {1}", offsets.Count, idx);
					break;
				}
				offsets.Add(at2);
				at2 += len + 24;
			}
			if (offsets.Count == 0)
			{
				throw new IOException("ни одного целого кадра — восстанавливать нечего" + ((note != "") ? (" (" + note + ")") : ""));
			}
			header.frameCount = offsets.Count;
			frameOffset = offsets.ToArray();
			TrailingBytes = fileBytes - at2;
			Recovered = true;
			RecoveryNote = string.Format("восстановлено {0} кадров, за ними {1} байт ", offsets.Count, TrailingBytes) + "оборванной записи" + ((note != "") ? (" (" + note + ")") : "") + ((headerOk) ? "" : "; хэш шапки не записан (запись не была закрыта)");
		}

		private int FrameIndexInBlock(byte[] block, int len)
		{
			if (len < 8)
			{
				return -1;
			}
			int nsec = (int)BitConverter.ToUInt32(block, 4);
			if (nsec != SuperTape.SectionCount(header.bodyNames.Length, header.fieldNames.Length))
			{
				return -1;
			}
			if (len < 24 * nsec + 24)
			{
				return -1;
			}
			if (block[8] != 0 || block[10] != 0)
			{
				return -1;
			}
			return (int)BitConverter.ToUInt32(block, 24 * nsec + 16);
		}

		private int RecordLenV6(int i)
		{
			byte[] head = At(frameOffset[i], 8);
			uint len = BitConverter.ToUInt32(head, 0);
			uint inv = BitConverter.ToUInt32(head, 4);
			if (inv != ~len || len <= 0)
			{
				throw new IOException(string.Format("кадр {0}: длина записи испорчена", i));
			}
			return (int)len;
		}

		private float FrameTimeV6(int index)
		{
			long blockPos = frameOffset[index];
			int nsec = (int)BitConverter.ToUInt32(At(blockPos + 8, 8), 4);
			if (nsec != SuperTape.SectionCount(header.bodyNames.Length, header.fieldNames.Length))
			{
				throw new IOException(string.Format("кадр {0}: разделов {1} — оглавление испорчено", index, nsec));
			}
			return BitConverter.ToSingle(At(blockPos + 28 + 24 * nsec, 4), 0);
		}

		private bool StoredHashOkV6(int i)
		{
			try
			{
				int len = RecordLenV6(i);
				byte[] rec = At(frameOffset[i] + 8, len + 8);
				return SuperTape.StoredHash(rec, 0, len) == BitConverter.ToUInt64(rec, len);
			}
			catch (IOException)
			{
				return false;
			}
		}

		private Sec[] ParseDirectory(int index, byte[] src, int off, int storedLen, long blockPos, out int canonicalLen)
		{
			int nb = header.bodyNames.Length;
			int nf = header.fieldNames.Length;
			if (storedLen < 8)
			{
				throw new IOException(string.Format("кадр {0}: запись {1} байт — меньше заголовка", index, storedLen));
			}
			canonicalLen = (int)BitConverter.ToUInt32(src, off);
			int nsec = (int)BitConverter.ToUInt32(src, off + 4);
			if (canonicalLen < 0 || canonicalLen > MaxCanonicalBytes)
			{
				throw new IOException(string.Format("кадр {0}: канонический кадр {1} байт — вне [0, {2}]", index, canonicalLen, MaxCanonicalBytes));
			}
			int expected = SuperTape.SectionCount(nb, nf);
			if (nsec != expected)
			{
				throw new IOException(string.Format("кадр {0}: разделов {1}, у ленты ({2} тел, {3} каналов) их {4} — оглавление испорчено", index, nsec, nb, nf, expected));
			}
			int dirBytes = nsec * 24 + 16;
			if (storedLen < dirBytes)
			{
				throw new IOException(string.Format("кадр {0}: оглавление {1} байт длиннее записи {2}", index, dirBytes, storedLen));
			}
			if (SuperTape.Fnv1a(src, off, nsec * 24 + 8) != BitConverter.ToUInt64(src, off + nsec * 24 + 8))
			{
				throw new IOException(string.Format("кадр {0}: оглавление испорчено (хэш оглавления не сошёлся)", index));
			}
			Sec[] secs = new Sec[nsec];
			long pos = blockPos + dirBytes;
			long rawSum = 0L;
			long storedSum = 0L;
			for (int k = 0; k < nsec; k++)
			{
				SuperTape.SlotOf(k, nb, nf, out SectionKind kind, out int body, out int channel);
				Sec s = default(Sec);
				int at = off + 8 + 24 * k;
				s.kind = (SectionKind)src[at];
				s.t = (Transform)src[at + 1];
				s.c = (Codec)src[at + 2];
				s.body = BitConverter.ToInt16(src, at + 4);
				s.channel = BitConverter.ToInt16(src, at + 6);
				s.rawLen = (int)BitConverter.ToUInt32(src, at + 8);
				s.storedLen = (int)BitConverter.ToUInt32(src, at + 12);
				s.hash = BitConverter.ToUInt64(src, at + 16);
				s.k = k;
				s.pos = pos;
				if (src[at + 3] != 0 || s.kind != kind || s.body != body || s.channel != channel)
				{
					throw new IOException(string.Format("кадр {0}: раздел {1} — {2}/{3}/{4}, на его месте ждали {5}/{6}/{7}", index, k, s.kind, s.body, s.channel, kind, body, channel));
				}
				if ((int)s.t > 4)
				{
					throw new IOException(string.Format("кадр {0}: раздел {1} — преобразование {2} неизвестно (лента новее этой сборки или битая)", index, k, s.t));
				}
				if ((int)s.c > 2)
				{
					throw new IOException(string.Format("кадр {0}: раздел {1} — кодек {2} неизвестен (лента новее этой сборки или битая)", index, k, s.c));
				}
				if (s.rawLen < 0 || s.storedLen < 0)
				{
					throw new IOException(string.Format("кадр {0}: раздел {1} — длины {2}/{3}", index, k, s.rawLen, s.storedLen));
				}
				if (s.c == Codec.Stored && (s.storedLen != s.rawLen || s.t != 0))
				{
					throw new IOException(string.Format("кадр {0}: раздел {1} хранится как есть, но {2} ≠ {3} или преобразование {4}", index, k, s.storedLen, s.rawLen, s.t));
				}
				if (s.c != 0)
				{
					long bound = (long)(s.storedLen + 16) * (((s.c == Codec.Lz4) ? 256 : 1040));
					if (bound < s.rawLen)
					{
						throw new IOException(string.Format("кадр {0}: раздел {1} — {2} байт из {3} сжатых, больше предела кодека {4}", index, k, s.rawLen, s.storedLen, s.c));
					}
				}
				secs[k] = s;
				rawSum += s.rawLen;
				storedSum += s.storedLen;
				pos += s.storedLen;
			}
			if (storedSum + dirBytes != storedLen)
			{
				throw new IOException(string.Format("кадр {0}: разделы занимают {1} байт, запись — {2}", index, storedSum + dirBytes, storedLen));
			}
			if (rawSum != canonicalLen)
			{
				throw new IOException(string.Format("кадр {0}: разделы дают {1} канонических байт, заявлено {2}", index, rawSum, canonicalLen));
			}
			return secs;
		}

		public SectionView[] DescribeSections(int index)
		{
			if (FormatVersion != 6)
			{
				throw new NotSupportedException("оглавление разделов есть только у ленты v6");
			}
			Sec[] secs = DirectoryOf(index, out int canonicalLen);
			SectionView[] views = new SectionView[secs.Length];
			for (int i = 0; i < secs.Length; i++)
			{
				views[i].kind = secs[i].kind;
				views[i].transform = secs[i].t;
				views[i].codec = secs[i].c;
				views[i].body = secs[i].body;
				views[i].channel = secs[i].channel;
				views[i].rawLen = secs[i].rawLen;
				views[i].storedLen = secs[i].storedLen;
				views[i].filePos = secs[i].pos;
			}
			return views;
		}

		private Sec[] DirectoryOf(int index, out int canonicalLen)
		{
			int len = RecordLenV6(index);
			long at = frameOffset[index] + 8;
			int nsec = (int)BitConverter.ToUInt32(At(at, 8), 4);
			if (nsec > 0x100000)
			{
				throw new IOException(string.Format("кадр {0}: разделов {1} — оглавление испорчено", index, nsec));
			}
			int dirBytes = nsec * 24 + 16;
			if (len < dirBytes)
			{
				throw new IOException(string.Format("кадр {0}: оглавление {1} байт длиннее записи {2}", index, dirBytes, len));
			}
			return ParseDirectory(index, At(at, dirBytes), 0, len, at, out canonicalLen);
		}

		private static void DecodeBlob(int index, Sec s, byte[] b, int bo, bool idx16, byte[] dst)
		{
			if (SuperTape.SectionCheck(b, bo, s.storedLen) != s.hash)
			{
				throw new IOException("хэш раздела не сошёлся — байты испорчены");
			}
			if (s.rawLen == 0)
			{
				return;
			}
			if (s.c == Codec.Stored)
			{
				Buffer.BlockCopy(b, bo, dst, 0, s.rawLen);
			}
			else if (s.t == Transform.None)
			{
				SuperTape.DecompressInto(s.c, b, bo, s.storedLen, dst, s.rawLen);
			}
			else
			{
				int transformed = SuperTape.TransformedLength(s.t, s.rawLen, idx16);
				byte[] work = Pool.Take(Math.Max(1, transformed));
				SuperTape.DecompressInto(s.c, b, bo, s.storedLen, work, transformed);
				SuperTape.Inverse(s.t, s.kind, idx16, work, s.rawLen, dst);
				Pool.Give(work);
			}
		}

		private byte[] Small(int index, Sec s, bool idx16)
		{
			byte[] blob = new byte[s.storedLen];
			ReadExact(s.pos, blob, s.storedLen);
			byte[] dst = new byte[s.rawLen];
			DecodeBlob(index, s, blob, 0, idx16, dst);
			return dst;
		}

		private void Run(int index, List<Job> jobs)
		{
			long total = 0L;
			foreach (Job j in jobs)
			{
				j.blob = new byte[j.s.storedLen];
				ReadExact(j.s.pos, j.blob, j.s.storedLen);
				total += j.s.rawLen;
			}
			Exception[] errors = new Exception[jobs.Count];
			Action<int> action = delegate(int k)
			{
				Job job = jobs[k];
				byte[] buf = job.keep;
				if (buf == null)
				{
					buf = Pool.Take(Math.Max(1, job.s.rawLen));
				}
				try
				{
					DecodeBlob(index, job.s, job.blob, 0, job.idx16, buf);
					job.use?.Invoke(buf);
				}
				catch (Exception ex)
				{
					errors[k] = ex;
				}
				finally
				{
					if (job.keep == null)
					{
						Pool.Give(buf);
					}
				}
			};
			if (total > 0x100000 && jobs.Count > 1)
			{
				Parallel.For(0, jobs.Count, action);
			}
			else
			{
				for (int k = 0; k < jobs.Count; k++)
				{
					action(k);
				}
			}
			foreach (Exception e in errors)
			{
				if (e != null)
				{
					ExceptionDispatchInfo.Capture(e).Throw();
				}
			}
		}

		private byte[] ReadCanonicalV6(int index, out int len)
		{
			int reclen = RecordLenV6(index);
			long at = frameOffset[index];
			if (v6Rec == null || v6Rec.Length < reclen + 16)
			{
				v6Rec = new byte[Grow(reclen + 16)];
			}
			ReadExact(at + 8, v6Rec, reclen + 16);
			if (SuperTape.StoredHash(v6Rec, 0, reclen) != BitConverter.ToUInt64(v6Rec, reclen))
			{
				throw new IOException(string.Format("кадр {0}: хранимый хэш не сошёлся — запись испорчена", index));
			}
			ulong canonicalHash = BitConverter.ToUInt64(v6Rec, reclen + 8);
			Sec[] secs = ParseDirectory(index, v6Rec, 0, reclen, 0L, out int canonicalLen);
			int nb = header.bodyNames.Length;
			int nf = header.fieldNames.Length;
			Raw[] raws = new Raw[secs.Length];
			bool[] idx16 = new bool[nb];
			for (int k = 0; k < secs.Length; k++)
			{
				if (secs[k].kind == SectionKind.BodyHead)
				{
					byte[] blob = new byte[secs[k].rawLen];
					DecodeBlob(index, secs[k], v6Rec, (int)secs[k].pos, false, blob);
					if (blob.Length != BodyHeadBytes)
					{
						throw new IOException(string.Format("кадр {0}: заголовок тела {1} — {2} байт", index, secs[k].body, blob.Length));
					}
					idx16[secs[k].body] = blob[BodyHeadA] != 0;
					raws[k] = new Raw(blob, 0, blob.Length);
				}
			}
			Exception[] errors = new Exception[secs.Length];
			Action<int> action = delegate(int k)
			{
				if (secs[k].kind != SectionKind.BodyHead)
				{
					byte[] dst = new byte[secs[k].rawLen];
					bool flag = secs[k].body >= 0 && idx16[secs[k].body];
					DecodeBlob(index, secs[k], v6Rec, (int)secs[k].pos, flag, dst);
					raws[k] = new Raw(dst, 0, dst.Length);
				}
			};
			if (canonicalLen > 0x100000)
			{
				Parallel.For(0, secs.Length, action);
			}
			else
			{
				for (int k = 0; k < secs.Length; k++)
				{
					action(k);
				}
			}
			foreach (Exception e in errors)
			{
				if (e != null)
				{
					ExceptionDispatchInfo.Capture(e).Throw();
				}
			}
			byte[] canonical = new byte[canonicalLen];
			int got = SuperTape.Assemble(raws, nb, nf, canonical);
			if (got != canonicalLen)
			{
				throw new IOException(string.Format("кадр {0}: собрано {1} байт, заявлено {2}", index, got, canonicalLen));
			}
			ulong hash = SuperTape.Fnv1a(BitConverter.GetBytes(canonicalLen), 0, 4);
			hash = SuperTape.Fnv1a(canonical, 0, canonicalLen, hash);
			if (hash != canonicalHash)
			{
				throw new IOException(string.Format("кадр {0}: канонические байты не сошлись с хэшем кадра (0x{1:X16} против 0x{2:X16})", index, hash, canonicalHash));
			}
			len = canonicalLen;
			return canonical;
		}

		private static int Grow(int n)
		{
			if (n < 0x100000)
			{
				return 0x100000;
			}
			if (n > 0x3FFFFFFF)
			{
				return n;
			}
			return n + (n >> 2);
		}

		private void ReadExact(long at, byte[] dst, int n)
		{
			fs.Seek(at, SeekOrigin.Begin);
			int done = 0;
			while (done < n)
			{
				int got = fs.Read(dst, done, n - done);
				if (got <= 0)
				{
					throw new IOException(string.Format("лента кончилась на {0}: ждали {1} байт", at + done, n));
				}
				done += got;
			}
		}

		internal byte[] CanonicalHeaderImage()
		{
			if (Recovered)
			{
				throw new IOException("канонический образ у оборванной ленты не определён — нет честной шапки");
			}
			int len = headerFields.Length;
			byte[] img = new byte[len + 20];
			PutI32(img, 0, (int)Magic);
			PutI32(img, 4, 5);
			Buffer.BlockCopy(headerFields, 0, img, 8, len);
			PutI32(img, len + 8, header.frameCount);
			ulong h = Fnv1a(img, 0, len + 12);
			PutU64(img, len + 12, h);
			return img;
		}

		private Frame ReadPartialV6(int index, Detail detail, int field)
		{
			int nb = header.bodyNames.Length;
			int nf = header.fieldNames.Length;
			Sec[] secs = DirectoryOf(index, out int canonicalLen);
			if (secs[0].rawLen != 8)
			{
				throw new IOException(string.Format("кадр {0}: голова кадра {1} байт", index, secs[0].rawLen));
			}
			byte[] frameHead = Small(index, secs[0], idx16: false);
			Frame f = new Frame();
			f.detail = detail;
			f.detailField = ((detail == Detail.OneField) ? field : (-1));
			f.index = (int)BitConverter.ToUInt32(frameHead, 0);
			f.simTimeS = BitConverter.ToSingle(frameHead, 4);
			f.bodies = new BodyFrame[nb];
			f.telemetry = new BodyTelemetry[nb];
			List<Job> jobs = new List<Job>();
			for (int b = 0; b < nb; b++)
			{
				BodyFrame body = new BodyFrame();
				Sec secBody = secs[SuperTape.SlotIndex(SectionKind.BodyHead, b, -1, nb, nf)];
				if (secBody.rawLen != BodyHeadBytes)
				{
					throw new IOException(string.Format("кадр {0}: заголовок тела {1} — {2} байт", index, b, secBody.rawLen));
				}
				byte[] head = Small(index, secBody, idx16: false);
				body.aabbLo = new Vector3(BitConverter.ToSingle(head, 0), BitConverter.ToSingle(head, 4), BitConverter.ToSingle(head, 8));
				body.aabbHi = new Vector3(BitConverter.ToSingle(head, 12), BitConverter.ToSingle(head, 16), BitConverter.ToSingle(head, 20));
				int nv = (int)BitConverter.ToUInt32(head, 24);
				int ni = (int)BitConverter.ToUInt32(head, 28);
				if (nv < 0 || ni < 0)
				{
					throw new IOException(string.Format("вершин {0}, треугольников {1} — лента битая", nv, ni));
				}
				bool idx16 = head[BodyHeadA] != 0;
				body.blockLo = new Vector3(BitConverter.ToSingle(head, 33), BitConverter.ToSingle(head, 37), BitConverter.ToSingle(head, 41));
				body.blockCell = BitConverter.ToSingle(head, 45);
				body.bnx = (int)BitConverter.ToUInt32(head, 49);
				body.bny = (int)BitConverter.ToUInt32(head, 53);
				body.bnz = (int)BitConverter.ToUInt32(head, 57);
				long cells = (long)body.bny * body.bnx * body.bnz;
				if (body.bnx < 0 || body.bny < 0 || body.bnz < 0 || cells > 67108864)
				{
					throw new IOException(string.Format("блок {0}×{1}×{2} — лента битая", body.bnx, body.bny, body.bnz));
				}
				long ni3 = (long)ni * 3;
				Sec secV = secs[SuperTape.SlotIndex(SectionKind.Vertices, b, -1, nb, nf)];
				Sec secT = secs[SuperTape.SlotIndex(SectionKind.Triangles, b, -1, nb, nf)];
				if (secV.rawLen != nv * 6)
				{
					throw new IOException(string.Format("кадр {0}: тело {1} — вершины {2} / индексы {3} байт против головы ({4}, {5})", index, b, secV.rawLen, secT.rawLen, nv, ni3));
				}
				if (secT.rawLen != (idx16 ? 2 : 4) * ni3)
				{
					throw new IOException(string.Format("кадр {0}: тело {1} — вершины {2} / индексы {3} байт против головы ({4}, {5})", index, b, secV.rawLen, secT.rawLen, nv, ni3));
				}
				for (int ch = 0; ch < nf; ch++)
				{
					if (secs[SuperTape.SlotIndex(SectionKind.VertexField, b, ch, nb, nf)].rawLen != nv * 2)
					{
						throw new IOException(string.Format("кадр {0}: тело {1}, канал {2} — длины полей против головы", index, b, ch));
					}
					if (secs[SuperTape.SlotIndex(SectionKind.BlockField, b, ch, nb, nf)].rawLen != cells * 2)
					{
						throw new IOException(string.Format("кадр {0}: тело {1}, канал {2} — длины полей против головы", index, b, ch));
					}
				}
				body.fields = new float[nf][];
				body.blockHalf = new byte[nf][];
				if (detail == Detail.BoundsOnly)
				{
					body.vertices = new Vector3[0];
					body.triangles = new int[0];
				}
				else
				{
					Job jobV = new Job();
					jobV.s = secV;
					jobV.use = delegate (byte[] raw)
					{
						body.vertices = DecodeVertices(raw, 0, nv, body.aabbLo);
					};
					jobs.Add(jobV);
					for (int ch = 0; ch < nf; ch++)
					{
						int channel = ch;
						if (Wanted(channel, detail, field))
						{
							Job jobF = new Job();
							jobF.s = secs[SuperTape.SlotIndex(SectionKind.VertexField, b, channel, nb, nf)];
							jobF.use = delegate (byte[] raw)
							{
								body.fields[channel] = DecodeHalfs(raw, 0, nv);
							};
							jobs.Add(jobF);
						}
					}
					Job jobT = new Job();
					jobT.s = secT;
					jobT.idx16 = idx16;
					jobT.use = delegate (byte[] raw)
					{
						body.triangles = DecodeTriangles(raw, 0, (int)ni3, idx16);
					};
					jobs.Add(jobT);
					for (int ch = 0; ch < nf; ch++)
					{
						if (Wanted(ch, detail, field))
						{
							byte[] buf = new byte[cells * 2];
							body.blockHalf[ch] = buf;
							Job jobB = new Job();
							jobB.s = secs[SuperTape.SlotIndex(SectionKind.BlockField, b, ch, nb, nf)];
							jobB.keep = buf;
							jobs.Add(jobB);
						}
					}
				}
				f.bodies[b] = body;
			}
			Sec secTail = secs[SuperTape.SlotIndex(SectionKind.Tail, -1, -1, nb, nf)];
			if (secTail.rawLen < nb * 36 + 20)
			{
				throw new IOException(string.Format("кадр {0}: хвост {1} байт", index, secTail.rawLen));
			}
			byte[] tail = Small(index, secTail, idx16: false);
			for (int b = 0; b < nb; b++)
			{
				f.telemetry[b].kineticJ = BitConverter.ToDouble(tail, 36 * b);
				f.telemetry[b].momentum = new Vector3(BitConverter.ToSingle(tail, 36 * b + 8), BitConverter.ToSingle(tail, 36 * b + 12), BitConverter.ToSingle(tail, 36 * b + 16));
				f.telemetry[b].massInDomainKg = BitConverter.ToDouble(tail, 36 * b + 20);
				f.telemetry[b].particleVolumeM3 = BitConverter.ToDouble(tail, 36 * b + 28);
			}
			f.escapedMassKg = BitConverter.ToDouble(tail, 36 * nb);
			f.verdictSoFar = (int)BitConverter.ToUInt32(tail, 36 * nb + 8);
			int nf2 = (int)BitConverter.ToUInt32(tail, 36 * nb + 12);
			if (nf2 > 256)
			{
				throw new IOException(string.Format("кадр {0}: границ полей {1} — лента битая", index, nf2));
			}
			if (secTail.rawLen != nf2 * 8 + 20 + nb * 36)
			{
				throw new IOException(string.Format("кадр {0}: хвост {1} байт при {2} границах", index, secTail.rawLen, nf2));
			}
			f.fieldMin = new float[nf2];
			f.fieldMax = new float[nf2];
			for (int i = 0; i < nf2; i++)
			{
				f.fieldMin[i] = BitConverter.ToSingle(tail, 36 * nb + 16 + 8 * i);
				f.fieldMax[i] = BitConverter.ToSingle(tail, 36 * nb + 16 + 8 * i + 4);
			}
			f.debrisCount = (int)BitConverter.ToUInt32(tail, 36 * nb + 16 + 8 * nf2);
			if (f.debrisCount > 0x400000)
			{
				throw new IOException(string.Format("кадр {0}: счётчик обломков {1} — лента битая", index, f.debrisCount));
			}
			if (f.debrisCount < 1)
			{
				Run(index, jobs);
				return f;
			}
			int nd = f.debrisCount;
			Vector3[] pos = new Vector3[nd];
			Sec secDP = secs[SuperTape.SlotIndex(SectionKind.DebrisPos, -1, -1, nb, nf)];
			if (secDP.rawLen != nd * 12)
			{
				throw new IOException(string.Format("кадр {0}: позиции обломков {1} байт при счётчике {2}", index, secDP.rawLen, nd));
			}
			for (int ch = 0; ch < nf; ch++)
			{
				if (secs[SuperTape.SlotIndex(SectionKind.DebrisField, -1, ch, nb, nf)].rawLen != nd * 2)
				{
					throw new IOException(string.Format("кадр {0}: канал обломков {1} против счётчика {2}", index, ch, nd));
				}
			}
			f.debrisPos = pos;
			f.debrisFields = new float[nf][];
			for (int ch = 0; ch < nf; ch++)
			{
				if (Wanted(ch, detail, field))
				{
					f.debrisFields[ch] = new float[nd];
				}
			}
			Job jobDP = new Job();
			jobDP.s = secDP;
			jobDP.use = delegate (byte[] raw)
			{
				for (int i = 0; i < nd; i++)
				{
					pos[i] = new Vector3(BitConverter.ToSingle(raw, 12 * i), BitConverter.ToSingle(raw, 12 * i + 4), BitConverter.ToSingle(raw, 12 * i + 8));
				}
			};
			jobs.Add(jobDP);
			for (int ch = 0; ch < nf; ch++)
			{
				float[] dst = f.debrisFields[ch];
				if (dst != null)
				{
					Job jobDF = new Job();
					jobDF.s = secs[SuperTape.SlotIndex(SectionKind.DebrisField, -1, ch, nb, nf)];
					jobDF.use = delegate (byte[] raw)
					{
						for (int i = 0; i < nd; i++)
						{
							dst[i] = SuperHalf.ToFloat(raw, 2 * i);
						}
					};
					jobs.Add(jobDF);
				}
			}
			Run(index, jobs);
			return f;
		}

		private static Vector3 V3(byte[] b, int o)
		{
			return new Vector3(BitConverter.ToSingle(b, o), BitConverter.ToSingle(b, o + 4), BitConverter.ToSingle(b, o + 8));
		}
	}

	public enum SectionKind : byte
	{
		FrameHead = 0,
		BodyHead = 1,
		Vertices = 2,
		VertexField = 3,
		Triangles = 4,
		BlockField = 5,
		Tail = 6,
		DebrisPos = 7,
		DebrisField = 8
	}

	public enum Transform : byte
	{
		None = 0,
		Shuffle16 = 1,
		Delta16 = 2,
		IndexDelta = 3,
		DebrisPos = 4
	}

	public enum Codec : byte
	{
		Stored = 0,
		Deflate = 1,
		Lz4 = 2
	}

	public struct Raw
	{
		public byte[] buf;

		public int off;

		public int len;

		public Raw(byte[] b, int o, int n)
		{
			buf = b;
			off = o;
			len = n;
		}
	}

	private static class Pool
	{
		private const long CapBytes = 268435456L;

		private static readonly Dictionary<int, Stack<byte[]>> free;

		private static long freeBytes;

		private static int ClassOf(int n)
		{
			if (n <= 65536)
			{
				return 65536;
			}
			int k = 0;
			for (int v = n - 1; v > 1; v >>= 1)
			{
				k++;
			}
			long size = 1L << Math.Max(0, k - 3);
			long cls = (n - 1 + size) / size * size;
			if (cls > int.MaxValue)
			{
				return n;
			}
			return (int)cls;
		}

		public static byte[] Take(int n)
		{
			int cls = ClassOf(n);
			lock (free)
			{
				if (free.TryGetValue(cls, out Stack<byte[]> stack) && stack.Count > 0)
				{
					freeBytes -= cls;
					return stack.Pop();
				}
			}
			return new byte[cls];
		}

		public static void Give(byte[] b)
		{
			if (b == null || ClassOf(b.Length) != b.Length)
			{
				return;
			}
			lock (free)
			{
				if (b.Length + freeBytes <= CapBytes)
				{
					if (!free.TryGetValue(b.Length, out Stack<byte[]> stack))
					{
						stack = new Stack<byte[]>();
						free[b.Length] = stack;
					}
					stack.Push(b);
					freeBytes += b.Length;
				}
			}
		}

		public static void Clear()
		{
			lock (free)
			{
				free.Clear();
				freeBytes = 0L;
			}
		}
	}

	public sealed class Encoded
	{
		public int version;

		public byte[] bytes;

		public ulong hash;

		public ulong storedHash;

		public int canonicalLen;

		public long FileBytes => (version != 6) ? (bytes.Length + 12) : (bytes.Length + 24);
	}

	public const uint Magic = 894457171u;

	public const int Version = 5;

	public const int VersionV6 = 6;

	public static int WriteVersion;

	public static Codec WriteCodec;

	public const uint FlagDecor = 2u;

	public const uint KnownFlags = 2u;

	public const int MaxDecorBytes = 335544320;

	public const int BodyHeadBytes = 61;

	private const int BodyHeadA = 32;

	private const int EntryBytes = 24;

	public static bool SabotageNoZigZag;

	public const int MaxCanonicalBytes = 1073741824;

	static SuperTape()
	{
		WriteVersion = ArgInt("-s36tapever", 6);
		WriteCodec = ArgCodec("-s36codec", Codec.Lz4);
	}

	public static Func<SectionKind, Transform> PolicyOverride;

	public static bool Readable(int version)
	{
		return version == 5 || version == 6;
	}

	private static void WriteString(BinaryWriter w, string s)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(s ?? "");
		w.Write(bytes.Length);
		w.Write(bytes);
	}

	private static string ReadString(BinaryReader r)
	{
		int count = r.ReadInt32();
		if ((uint)count > 0x100000u)
		{
			throw new IOException(string.Format("строка длиной {0} — файл битый", count));
		}
		byte[] bytes = r.ReadBytes(count);
		return Encoding.UTF8.GetString(bytes);
	}

	public static ulong Fnv1a(byte[] data, int offset, int count, ulong seed = 14695981039346656037uL)
	{
		ulong h = seed;
		for (int i = offset; i < offset + count; i++)
		{
			h = (h ^ data[i]) * 1099511628211uL;
		}
		return h;
	}

	public static ulong Fnv1a(ReadOnlySpan<byte> data, ulong seed = 14695981039346656037uL)
	{
		ulong h = seed;
		for (int i = 0; i < data.Length; i++)
		{
			h = (h ^ data[i]) * 1099511628211uL;
		}
		return h;
	}

	internal static int ArgInt(string key, int def)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == key && int.TryParse(args[i + 1], out int value))
			{
				return value;
			}
		}
		return def;
	}

	private static Codec ArgCodec(string key, Codec def)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length - 1; i++)
		{
			if (args[i] == key)
			{
				if (args[i + 1] == "deflate")
				{
					return Codec.Deflate;
				}
				if (args[i + 1] == "none")
				{
					return Codec.Stored;
				}
				return Codec.Lz4;
			}
		}
		return def;
	}

	public static int SectionCount(int nb, int nf)
	{
		return (nf * 2 + 3) * nb + 3 + nf;
	}

	private static void SlotOf(int k, int nb, int nf, out SectionKind kind, out int body, out int channel)
	{
		body = -1;
		channel = -1;
		if (k == 0)
		{
			kind = SectionKind.FrameHead;
			return;
		}
		int kk = k - 1;
		int per = 2 * nf + 3;
		if (kk < per * nb)
		{
			body = kk / per;
			int r = kk % per;
			if (r == 0)
			{
				kind = SectionKind.BodyHead;
			}
			else if (r == 1)
			{
				kind = SectionKind.Vertices;
			}
			else if (r < nf + 2)
			{
				kind = SectionKind.VertexField;
				channel = r - 2;
			}
			else if (r == nf + 2)
			{
				kind = SectionKind.Triangles;
			}
			else
			{
				kind = SectionKind.BlockField;
				channel = r - nf - 3;
			}
		}
		else
		{
			int t = kk - per * nb;
			if (t == 0)
			{
				kind = SectionKind.Tail;
			}
			else if (t == 1)
			{
				kind = SectionKind.DebrisPos;
			}
			else
			{
				kind = SectionKind.DebrisField;
				channel = t - 2;
			}
		}
	}

	private static int SlotIndex(SectionKind kind, int body, int channel, int nb, int nf)
	{
		int per = 2 * nf + 3;
		switch (kind)
		{
		case SectionKind.FrameHead:
			return 0;
		case SectionKind.BodyHead:
			return per * body + 1;
		case SectionKind.Vertices:
			return per * body + 2;
		case SectionKind.VertexField:
			return per * body + channel + 3;
		case SectionKind.Triangles:
			return per * body + nf + 3;
		case SectionKind.BlockField:
			return per * body + nf + channel + 4;
		case SectionKind.Tail:
			return per * nb + 1;
		case SectionKind.DebrisPos:
			return per * nb + 2;
		default:
			return per * nb + channel + 3;
		}
	}

	private static int TailDebrisCount(Raw tail)
	{
		if (tail.len < 4)
		{
			throw new IOException(string.Format("хвост кадра {0} байт — меньше счётчика обломков", tail.len));
		}
		return BitConverter.ToInt32(tail.buf, tail.off + tail.len - 4);
	}

	public static long CanonicalLength(Raw[] s)
	{
		long num = 0L;
		for (int i = 0; i < s.Length; i++)
		{
			num += s[i].len;
		}
		return num;
	}

	public static int Assemble(Raw[] s, int nb, int nf, byte[] dst)
	{
		int o = 0;
		Put(s[0], dst, ref o);
		for (int b = 0; b < nb; b++)
		{
			Raw bh = s[SlotIndex(SectionKind.BodyHead, b, -1, nb, nf)];
			if (bh.len != BodyHeadBytes)
			{
				throw new IOException(string.Format("заголовок тела {0}: {1} байт, ждали {2}", b, bh.len, BodyHeadBytes));
			}
			Put(bh, 0, 32, dst, ref o);
			Put(s[SlotIndex(SectionKind.Vertices, b, -1, nb, nf)], dst, ref o);
			for (int c = 0; c < nf; c++)
			{
				Put(s[SlotIndex(SectionKind.VertexField, b, c, nb, nf)], dst, ref o);
			}
			Put(bh, 32, 1, dst, ref o);
			Put(s[SlotIndex(SectionKind.Triangles, b, -1, nb, nf)], dst, ref o);
			Put(bh, 33, 28, dst, ref o);
			for (int c = 0; c < nf; c++)
			{
				Put(s[SlotIndex(SectionKind.BlockField, b, c, nb, nf)], dst, ref o);
			}
		}
		Raw tail = s[SlotIndex(SectionKind.Tail, -1, -1, nb, nf)];
		Put(tail, dst, ref o);
		int nd = TailDebrisCount(tail);
		Raw pos = s[SlotIndex(SectionKind.DebrisPos, -1, -1, nb, nf)];
		if (nd < 0 || pos.len != 12 * nd)
		{
			throw new IOException(string.Format("позиций обломков {0} байт при счётчике {1}", pos.len, nd));
		}
		Raw[] fields = new Raw[nf];
		for (int c = 0; c < nf; c++)
		{
			fields[c] = s[SlotIndex(SectionKind.DebrisField, -1, c, nb, nf)];
			if (fields[c].len != ((nd > 0) ? (2 * nd) : 0))
			{
				throw new IOException(string.Format("канал обломков {0}: {1} байт при счётчике {2}", c, fields[c].len, nd));
			}
		}
		if (nd > 0 && dst.Length - o < 12 * nd)
		{
			throw new IOException(string.Format("обломки не помещаются в собираемый кадр ({0} байт)", dst.Length - o));
		}
		for (int i = 0; i < nd; i++)
		{
			Buffer.BlockCopy(pos.buf, pos.off + 12 * i, dst, o, 12);
			o += 12;
			for (int c = 0; c < nf; c++)
			{
				dst[o] = fields[c].buf[fields[c].off + 2 * i];
				dst[o + 1] = fields[c].buf[fields[c].off + 2 * i + 1];
				o += 2;
			}
		}
		return o;
	}

	public static ulong HashCanonical(Raw[] s, int nb, int nf, int canonicalLen)
	{
		ulong h = Fnv1a(BitConverter.GetBytes(canonicalLen), 0, 4);
		h = Fnv(h, s[0]);
		for (int b = 0; b < nb; b++)
		{
			Raw bh = s[SlotIndex(SectionKind.BodyHead, b, -1, nb, nf)];
			h = Fnv(h, bh, 0, 32);
			h = Fnv(h, s[SlotIndex(SectionKind.Vertices, b, -1, nb, nf)]);
			for (int c = 0; c < nf; c++)
			{
				h = Fnv(h, s[SlotIndex(SectionKind.VertexField, b, c, nb, nf)]);
			}
			h = Fnv(h, bh, 32, 1);
			h = Fnv(h, s[SlotIndex(SectionKind.Triangles, b, -1, nb, nf)]);
			h = Fnv(h, bh, 33, 28);
			for (int c = 0; c < nf; c++)
			{
				h = Fnv(h, s[SlotIndex(SectionKind.BlockField, b, c, nb, nf)]);
			}
		}
		h = Fnv(h, s[SlotIndex(SectionKind.Tail, -1, -1, nb, nf)]);
		int nd = TailDebrisCount(s[SlotIndex(SectionKind.Tail, -1, -1, nb, nf)]);
		Raw pos = s[SlotIndex(SectionKind.DebrisPos, -1, -1, nb, nf)];
		Raw[] fields = new Raw[nf];
		for (int c = 0; c < nf; c++)
		{
			fields[c] = s[SlotIndex(SectionKind.DebrisField, -1, c, nb, nf)];
		}
		for (int i = 0; i < nd; i++)
		{
			for (int j = 0; j < 12; j++)
			{
				h = (h ^ pos.buf[pos.off + 12 * i + j]) * 1099511628211uL;
			}
			for (int c = 0; c < nf; c++)
			{
				h = (h ^ fields[c].buf[fields[c].off + 2 * i]) * 1099511628211uL;
				h = (h ^ fields[c].buf[fields[c].off + 2 * i + 1]) * 1099511628211uL;
			}
		}
		return h;
	}

	public static Raw[] Split(byte[] p, int len, int nb, int nf)
	{
		Raw[] s = new Raw[SectionCount(nb, nf)];
		int o = 0;
		s[0] = Take(p, len, ref o, 8, "голова кадра");
		for (int b = 0; b < nb; b++)
		{
			byte[] head = new byte[BodyHeadBytes];
			Raw box = Take(p, len, ref o, 32, "заголовок тела");
			Buffer.BlockCopy(p, box.off, head, 0, 32);
			int nv = (int)BitConverter.ToUInt32(head, 24);
			int ni = (int)BitConverter.ToUInt32(head, 28);
			if (nv < 0 || (uint)ni > 0xAAAAAAA)
			{
				throw new IOException(string.Format("тело {0}: вершин {1}, треугольников {2} — кадр битый", b, nv, ni));
			}
			s[SlotIndex(SectionKind.Vertices, b, -1, nb, nf)] = Take(p, len, ref o, nv * 6L, "вершины");
			for (int c = 0; c < nf; c++)
			{
				s[SlotIndex(SectionKind.VertexField, b, c, nb, nf)] = Take(p, len, ref o, nv * 2L, "поле вершин");
			}
			Raw flag = Take(p, len, ref o, 1, "флаг индексов");
			head[BodyHeadA] = p[flag.off];
			s[SlotIndex(SectionKind.Triangles, b, -1, nb, nf)] = Take(p, len, ref o, (long)ni * ((head[BodyHeadA] != 0) ? 6 : 12), "индексы");
			Raw blk = Take(p, len, ref o, 28, "блок тела");
			Buffer.BlockCopy(p, blk.off, head, 33, 28);
			long cells = BlockCells(head, b);
			for (int c = 0; c < nf; c++)
			{
				s[SlotIndex(SectionKind.BlockField, b, c, nb, nf)] = Take(p, len, ref o, cells * 2, "блок полей");
			}
			s[SlotIndex(SectionKind.BodyHead, b, -1, nb, nf)] = new Raw(head, 0, BodyHeadBytes);
		}
		long tailMin = nb * 36L + 12;
		if (o + 4 + tailMin > len)
		{
			throw new IOException("хвост кадра выходит за кадр");
		}
		int nf2 = (int)BitConverter.ToUInt32(p, o + (int)tailMin);
		if ((uint)nf2 > 0x100)
		{
			throw new IOException(string.Format("границ полей {0} — кадр битый", nf2));
		}
		s[SlotIndex(SectionKind.Tail, -1, -1, nb, nf)] = Take(p, len, ref o, tailMin + 8 + nf2 * 8L, "хвост");
		int nd = (int)BitConverter.ToUInt32(p, o - 4);
		if ((uint)nd > 0x400000)
		{
			throw new IOException(string.Format("счётчик обломков {0} — кадр битый", nd));
		}
		Raw debris = Take(p, len, ref o, (2 * nf + 12) * (long)nd, "обломки");
		byte[] pos = new byte[nd * 12];
		byte[][] fields = new byte[nf][];
		for (int c = 0; c < nf; c++)
		{
			fields[c] = new byte[(nd > 0) ? (2 * nd) : 0];
		}
		if (nd > 0)
		{
			int stride = 2 * nf + 12;
			for (int i = 0; i < nd; i++)
			{
				Buffer.BlockCopy(p, debris.off + stride * i, pos, 12 * i, 12);
				for (int c = 0; c < nf; c++)
				{
					fields[c][2 * i] = p[debris.off + stride * i + 12 + 2 * c];
					fields[c][2 * i + 1] = p[debris.off + stride * i + 13 + 2 * c];
				}
			}
		}
		s[SlotIndex(SectionKind.DebrisPos, -1, -1, nb, nf)] = new Raw(pos, 0, pos.Length);
		for (int c = 0; c < nf; c++)
		{
			s[SlotIndex(SectionKind.DebrisField, -1, c, nb, nf)] = new Raw(fields[c], 0, fields[c].Length);
		}
		if (o != len)
		{
			throw new IOException(string.Format("кадр разрезан на {0} байт из {1} — формат разъехался с разрезкой", o, len));
		}
		return s;
	}

	private static long BlockCells(byte[] head, int b)
	{
		int nx = BitConverter.ToInt32(head, 49);
		int ny = BitConverter.ToInt32(head, 53);
		int nz = BitConverter.ToInt32(head, 57);
		long cells = (long)nz * ny * nx;
		if (nx < 0 || ny < 0 || nz < 0 || cells > 67108864)
		{
			throw new IOException(string.Format("тело {0}: блок {1}×{2}×{3} — кадр битый", b, nx, ny, nz));
		}
		return cells;
	}

	private static Raw Take(byte[] p, int len, ref int o, long n, string what)
	{
		if (n < 0 || o + n > len)
		{
			throw new IOException(string.Format("{0}: {1} байт с {2} выходит за кадр ({3})", what, n, o, len));
		}
		Raw raw = new Raw(p, o, (int)n);
		o += (int)n;
		return raw;
	}

	private static void Put(Raw r, byte[] dst, ref int o)
	{
		Put(r, 0, r.len, dst, ref o);
	}

	private static void Put(Raw r, int from, int n, byte[] dst, ref int o)
	{
		if (n < 0 || from < 0 || from + n > r.len)
		{
			throw new IOException(string.Format("срез раздела [{0}, +{1}) вне {2} байт", from, n, r.len));
		}
		if (dst.Length - o < n)
		{
			throw new IOException(string.Format("собираемый кадр переполнен ({0} байт)", dst.Length));
		}
		Buffer.BlockCopy(r.buf, r.off + from, dst, o, n);
		o += n;
	}

	private static ulong Fnv(ulong h, Raw r)
	{
		return Fnv(h, r, 0, r.len);
	}

	private static ulong Fnv(ulong h, Raw r, int from, int n)
	{
		for (int i = 0; i < n; i++)
		{
			h = (h ^ r.buf[r.off + from + i]) * 1099511628211uL;
		}
		return h;
	}

	public static int TransformedLength(Transform t, int rawLen, bool idx16)
	{
		if (t == Transform.IndexDelta && idx16)
		{
			return checked(rawLen * 2);
		}
		return rawLen;
	}

	private static int Components(SectionKind k)
	{
		return (k == SectionKind.Vertices) ? 3 : 1;
	}

	public static int Forward(Transform t, SectionKind kind, bool idx16, byte[] src, int off, int n, byte[] dst)
	{
		if (t == Transform.None)
		{
			Buffer.BlockCopy(src, off, dst, 0, n);
			return n;
		}
		if (t == Transform.Shuffle16)
		{
			if ((n & 1) != 0)
			{
				throw new ArgumentException(string.Format("перестановка u16: {0} байт — нечётно", n));
			}
			int half = n >> 1;
			for (int i = 0; i < half; i++)
			{
				dst[i] = src[off + 2 * i];
				dst[i + half] = src[off + 2 * i + 1];
			}
			return n;
		}
		if (t == Transform.Delta16)
		{
			int components = Components(kind);
			int count = n / (components * 2);
			if (n % (components * 2) != 0)
			{
				throw new ArgumentException(string.Format("разности u16×{0}: {1} байт — не кратно", components, n));
			}
			int half = n >> 1;
			for (int c = 0; c < components; c++)
			{
				int prev = 0;
				for (int i = 0; i < count; i++)
				{
					int v = src[off + 2 * c + components * 2 * i] | (src[off + 2 * c + components * 2 * i + 1] << 8);
					int d = (v - prev) & 0xFFFF;
					prev = v;
					dst[c * count + i] = (byte)d;
					dst[half + c * count + i] = (byte)(d >> 8);
				}
			}
			return n;
		}
		if (t == Transform.IndexDelta)
		{
			int elem = idx16 ? 2 : 4;
			int count = n / elem;
			if (n % elem != 0)
			{
				throw new ArgumentException(string.Format("индексы по {0} байта: {1} байт — не кратно", elem, n));
			}
			int prev = 0;
			for (int i = 0; i < count; i++)
			{
				int v = (idx16 ? (src[off + elem * i] | (src[off + elem * i + 1] << 8)) : ((int)BitConverter.ToUInt32(src, off + elem * i)));
				int d = v - prev;
				prev = v;
				if (!SabotageNoZigZag)
				{
					d = (d << 1) ^ (d >> 31);
				}
				dst[i] = (byte)d;
				dst[i + count] = (byte)(d >> 8);
				dst[i + 2 * count] = (byte)(d >> 16);
				dst[i + 3 * count] = (byte)(d >> 24);
			}
			return count * 4;
		}
		if (t != Transform.DebrisPos)
		{
			throw new ArgumentException("неизвестное преобразование " + t);
		}
		if (n != n / 12 * 12)
		{
			throw new ArgumentException(string.Format("позиции обломков: {0} байт — не кратно 12", n));
		}
		int count2 = n / 12;
		for (int j = 0; j < 3; j++)
		{
			for (int k = 0; k < 4; k++)
			{
				for (int i = 0; i < count2; i++)
				{
					dst[(4 * j + k) * count2 + i] = src[off + 4 * j + k + 12 * i];
				}
			}
		}
		return n;
	}

	public static void Inverse(Transform t, SectionKind kind, bool idx16, byte[] src, int rawLen, byte[] dst)
	{
		if (t == Transform.None)
		{
			Buffer.BlockCopy(src, 0, dst, 0, rawLen);
			return;
		}
		if (t == Transform.Shuffle16)
		{
			if ((rawLen & 1) != 0)
			{
				throw new IOException(string.Format("раздел u16 длиной {0} — нечётно", rawLen));
			}
			int half = rawLen >> 1;
			for (int i = 0; i < half; i++)
			{
				dst[2 * i] = src[i];
				dst[2 * i + 1] = src[half + i];
			}
			return;
		}
		if (t == Transform.Delta16)
		{
			int components = Components(kind);
			int count = rawLen / (components * 2);
			if (rawLen % (components * 2) != 0)
			{
				throw new IOException(string.Format("раздел u16×{0} длиной {1} — не кратно", components, rawLen));
			}
			int half = rawLen >> 1;
			for (int c = 0; c < components; c++)
			{
				int prev = 0;
				for (int i = 0; i < count; i++)
				{
					int d = src[c * count + i] | (src[half + c * count + i] << 8);
					int v = (d + prev) & 0xFFFF;
					prev = v;
					dst[2 * c + components * 2 * i] = (byte)v;
					dst[2 * c + components * 2 * i + 1] = (byte)(v >> 8);
				}
			}
			return;
		}
		if (t == Transform.IndexDelta)
		{
			int elem = idx16 ? 2 : 4;
			int count = rawLen / elem;
			if (rawLen % elem != 0)
			{
				throw new IOException(string.Format("индексы по {0} байта: {1} байт — не кратно", elem, rawLen));
			}
			int v = 0;
			for (int i = 0; i < count; i++)
			{
				int d = src[i] | (src[i + count] << 8) | (src[i + 2 * count] << 16) | (src[i + 3 * count] << 24);
				v += (int)((uint)d >> 1) ^ -(d & 1);
				if (idx16)
				{
					if ((uint)v > 0xFFFF)
					{
						throw new IOException(string.Format("индекс {0}: {1} после обратных разностей — вне u16", i, v));
					}
					dst[elem * i] = (byte)v;
					dst[elem * i + 1] = (byte)(v >> 8);
				}
				else
				{
					dst[elem * i] = (byte)v;
					dst[elem * i + 1] = (byte)(v >> 8);
					dst[elem * i + 2] = (byte)(v >> 16);
					dst[elem * i + 3] = (byte)(v >> 24);
				}
			}
			return;
		}
		if (t != Transform.DebrisPos)
		{
			throw new IOException("неизвестное преобразование " + t + " — лента новее этой сборки или битая");
		}
		if (rawLen != rawLen / 12 * 12)
		{
			throw new IOException(string.Format("позиции обломков длиной {0} — не кратно 12", rawLen));
		}
		int count2 = rawLen / 12;
		for (int j = 0; j < 3; j++)
		{
			for (int k = 0; k < 4; k++)
			{
				for (int i = 0; i < count2; i++)
				{
					dst[4 * j + k + 12 * i] = src[(4 * j + k) * count2 + i];
				}
			}
		}
	}

	public static Transform PolicyFor(SectionKind k)
	{
		switch (k)
		{
		case SectionKind.Vertices:
			return Transform.Delta16;
		case SectionKind.VertexField:
		case SectionKind.BlockField:
		case SectionKind.DebrisField:
			return Transform.Shuffle16;
		case SectionKind.Triangles:
			return Transform.IndexDelta;
		case SectionKind.DebrisPos:
			return Transform.DebrisPos;
		default:
			return Transform.None;
		}
	}

	private static int CompressInto(Codec codec, byte[] src, int n, byte[] dst, int at)
	{
		if (codec == Codec.Deflate)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
				{
					ds.Write(src, 0, n);
				}
				if (ms.Length > dst.Length - at)
				{
					return int.MaxValue;
				}
				Buffer.BlockCopy(ms.ToArray(), 0, dst, at, (int)ms.Length);
				return (int)ms.Length;
			}
		}
		if (codec != Codec.Lz4)
		{
			throw new ArgumentException("кодек " + codec);
		}
		return SuperLz4.Compress(src, 0, n, dst, at);
	}

	private static int CodecBound(int n)
	{
		return SuperLz4.MaxCompressed(n) + 64;
	}

	private static void DecompressInto(Codec codec, byte[] src, int off, int len, byte[] dst, int n)
	{
		if (codec == Codec.Stored)
		{
			if (len != n)
			{
				throw new IOException(string.Format("хранимый раздел {0} байт, ждали {1}", len, n));
			}
			Buffer.BlockCopy(src, off, dst, 0, n);
			return;
		}
		if (codec == Codec.Deflate)
		{
			try
			{
				using (MemoryStream ms = new MemoryStream(src, off, len, writable: false))
				{
					using (DeflateStream ds = new DeflateStream(ms, CompressionMode.Decompress))
					{
						int done = 0;
						while (done < n)
						{
							int got = ds.Read(dst, done, n - done);
							if (got <= 0)
							{
								throw new IOException(string.Format("deflate: раздел дал {0} байт, ждали {1}", done, n));
							}
							done += got;
						}
						if (ds.Read(new byte[1], 0, 1) != 0)
						{
							throw new IOException(string.Format("deflate: раздел длиннее заявленных {0} байт", n));
						}
					}
				}
			}
			catch (Exception ex)
			{
				throw new IOException("deflate: раздел испорчен — " + ex.Message, ex);
			}
			return;
		}
		if (codec != Codec.Lz4)
		{
			throw new IOException("неизвестный кодек " + codec + " — лента новее этой сборки или битая");
		}
		SuperLz4.Decompress(src, off, len, dst, 0, n);
	}

	public static void ReleasePools()
	{
		Pool.Clear();
	}

	private static Encoded BuildStored(Raw[] s, int nb, int nf, int canonicalLen, ulong canonicalHash, Codec codec)
	{
		if (s.Length != SectionCount(nb, nf))
		{
			throw new ArgumentException(string.Format("разделов {0}, у кадра формы ({1}, {2}) их {3}", s.Length, nb, nf, SectionCount(nb, nf)));
		}
		int nsec = s.Length;
		int dirBytes = nsec * 24 + 16;
		int maxRaw = 16;
		for (int k = 0; k < s.Length; k++)
		{
			if (maxRaw < s[k].len)
			{
				maxRaw = s[k].len;
			}
		}
		byte[] work = Pool.Take(checked(maxRaw * 2));
		byte[] comp = Pool.Take(CodecBound(checked(maxRaw * 2)));
		try
		{
			byte[][] sections = new byte[nsec][];
			byte[] dir = new byte[nsec * 24];
			long total = dirBytes;
			bool idx16 = false;
			for (int k = 0; k < nsec; k++)
			{
				SlotOf(k, nb, nf, out SectionKind kind, out int body, out int channel);
				Raw raw = s[k];
				Transform t;
				Codec useCodec;
				if (kind == SectionKind.BodyHead)
				{
					if (raw.len != BodyHeadBytes)
					{
						throw new ArgumentException(string.Format("заголовок тела {0}: {1} байт", body, raw.len));
					}
					idx16 = raw.buf[raw.off + BodyHeadA] != 0;
					t = Transform.None;
					useCodec = Codec.Stored;
				}
				else if (kind == SectionKind.FrameHead || kind == SectionKind.Tail)
				{
					t = Transform.None;
					useCodec = Codec.Stored;
				}
				else
				{
					t = ((PolicyOverride != null) ? PolicyOverride(kind) : PolicyFor(kind));
					useCodec = codec;
				}
				byte[] best = null;
				Transform bestT = t;
				if (raw.len >= 1 && useCodec != Codec.Stored)
				{
					int attempts = ((kind == SectionKind.Vertices && PolicyOverride == null) ? 2 : 1);
					for (int a = 0; a < attempts; a++)
					{
						Transform tryT = ((a == 0) ? t : Transform.Shuffle16);
						int transformed = Forward(tryT, kind, idx16, raw.buf, raw.off, raw.len, work);
						int compressed = CompressInto(useCodec, work, transformed, comp, 0);
						if (compressed < raw.len && (best == null || compressed < best.Length))
						{
							best = new byte[compressed];
							Buffer.BlockCopy(comp, 0, best, 0, compressed);
							bestT = tryT;
						}
					}
				}
				byte[] stored;
				if (best == null)
				{
					stored = new byte[raw.len];
					Buffer.BlockCopy(raw.buf, raw.off, stored, 0, raw.len);
					bestT = Transform.None;
					useCodec = Codec.Stored;
				}
				else
				{
					stored = best;
				}
				sections[k] = stored;
				int at = 24 * k;
				dir[at] = (byte)kind;
				dir[at + 1] = (byte)bestT;
				dir[at + 2] = (byte)useCodec;
				dir[at + 3] = 0;
				dir[at + 4] = (byte)body;
				dir[at + 5] = (byte)(body >> 8);
				dir[at + 6] = (byte)channel;
				dir[at + 7] = (byte)(channel >> 8);
				dir[at + 8] = (byte)raw.len;
				dir[at + 9] = (byte)(raw.len >> 8);
				dir[at + 10] = (byte)(raw.len >> 16);
				dir[at + 11] = (byte)(raw.len >> 24);
				dir[at + 12] = (byte)stored.Length;
				dir[at + 13] = (byte)(stored.Length >> 8);
				dir[at + 14] = (byte)(stored.Length >> 16);
				dir[at + 15] = (byte)(stored.Length >> 24);
				PutU64(dir, at + 16, SectionCheck(stored, 0, stored.Length));
				total += stored.Length;
			}
			if (total > 0x7FFFFFBF)
			{
				throw new IOException(string.Format("кадр {0} байт не помещается в запись v6", canonicalLen));
			}
			byte[] block = new byte[(int)total];
			PutI32(block, 0, canonicalLen);
			PutI32(block, 4, nsec);
			Buffer.BlockCopy(dir, 0, block, 8, dir.Length);
			PutU64(block, dir.Length + 8, Fnv1a(block, 0, dir.Length + 8));
			int at2 = dirBytes;
			for (int k = 0; k < sections.Length; k++)
			{
				Buffer.BlockCopy(sections[k], 0, block, at2, sections[k].Length);
				at2 += sections[k].Length;
			}
			Encoded e = new Encoded();
			e.version = 6;
			e.bytes = block;
			e.hash = canonicalHash;
			e.storedHash = StoredHash(block, 0, block.Length);
			e.canonicalLen = canonicalLen;
			return e;
		}
		finally
		{
			Pool.Give(work);
			Pool.Give(comp);
		}
	}

	public static ulong SectionCheck(byte[] b, int off, int len)
	{
		ulong h = (ulong)len ^ 11400714819323198485uL;
		int i = off;
		int lim = off + (len & -8);
		while (i < lim)
		{
			ulong v = (ulong)BitConverter.ToInt64(b, i);
			v = (v ^ h) * 18397679294719823053uL;
			h = v ^ (v >> 32);
			i += 8;
		}
		ulong tail = 0uL;
		int shift = 0;
		for (; i < off + len; i++)
		{
			tail |= (ulong)b[i] << shift;
			shift += 8;
		}
		ulong r = (tail ^ h) * 14181476777654086739uL;
		return r ^ (r >> 29);
	}

	public static ulong StoredHash(byte[] block, int off, int len)
	{
		ulong h = Fnv1a(BitConverter.GetBytes(len), 0, 4);
		h = Fnv1a(BitConverter.GetBytes(~len), 0, 4, h);
		h = Fnv1a(block, off, len, h);
		return h;
	}

	public static Encoded EncodeCanonicalV6(byte[] canonical, int len, int nb, int nf, Codec codec)
	{
		Raw[] s = Split(canonical, len, nb, nf);
		ulong hash = Fnv1a(BitConverter.GetBytes(len), 0, 4);
		hash = Fnv1a(canonical, 0, len, hash);
		return BuildStored(s, nb, nf, len, hash, codec);
	}

	public static void ValidateShape(Frame f, int bodyCount, int fieldCount)
	{
		if (f.bodies.Length != bodyCount || f.telemetry.Length != bodyCount)
		{
			throw new ArgumentException(string.Format("кадр {0}: тел {1}/телеметрий ", f.index, f.bodies.Length) + string.Format("{0}, шапка объявила {1}", f.telemetry.Length, bodyCount));
		}
		if (f.fieldMin.Length != fieldCount || f.fieldMax.Length != fieldCount)
		{
			throw new ArgumentException(string.Format("кадр {0}: границ полей {1}, ", f.index, f.fieldMin.Length) + string.Format("шапка объявила {0}", fieldCount));
		}
		for (int i = 0; i < f.bodies.Length; i++)
		{
			if (f.bodies[i].fields.Length != fieldCount || f.bodies[i].blockFields.Length != fieldCount)
			{
				throw new ArgumentException(string.Format("кадр {0}: каналов у тела ", f.index) + string.Format("{0}/{1}, ", f.bodies[i].fields.Length, f.bodies[i].blockFields.Length) + string.Format("шапка объявила {0}", fieldCount));
			}
		}
		if (f.debrisPos.Length != f.debrisCount || (f.debrisCount > 0 && f.debrisFields.Length != fieldCount))
		{
			throw new ArgumentException(string.Concat(new string[7]
			{
				"кадр ",
				f.index.ToString(),
				": обломков ",
				f.debrisPos.Length.ToString(),
				" при счётчике ",
				f.debrisCount.ToString(),
				" — форма кадра битая"
			}));
		}
		for (int i = 0; i < ((f.debrisCount > 0) ? fieldCount : 0); i++)
		{
			if (f.debrisFields[i] == null || f.debrisFields[i].Length != f.debrisCount)
			{
				throw new ArgumentException(string.Format("кадр {0}: канал обломков {1} длиной ", f.index, i) + string.Format("{0} при счётчике ", (f.debrisFields[i] != null) ? f.debrisFields[i].Length : (-1)) + string.Format("{0} — форма кадра битая", f.debrisCount));
			}
		}
	}

	public static Encoded EncodeV6(Frame f, int bodyCount, int fieldCount, Codec codec)
	{
		ValidateShape(f, bodyCount, fieldCount);
		int nb = bodyCount;
		int nf = fieldCount;
		Raw[] sections = new Raw[SectionCount(nb, nf)];
		List<byte[]> pooled = new List<byte[]>();
		try
		{
			byte[] frameHead = new byte[8];
			PutI32(frameHead, 0, f.index);
			PutF32(frameHead, 4, f.simTimeS);
			sections[SlotIndex(SectionKind.FrameHead, -1, -1, nb, nf)] = new Raw(frameHead, 0, 8);
			for (int b = 0; b < nb; b++)
			{
				BodyFrame body = f.bodies[b];
				byte[] head = new byte[BodyHeadBytes];
				int nv = body.vertices.Length;
				PutF32(head, 0, body.aabbLo.x);
				PutF32(head, 4, body.aabbLo.y);
				PutF32(head, 8, body.aabbLo.z);
				PutF32(head, 12, body.aabbHi.x);
				PutF32(head, 16, body.aabbHi.y);
				PutF32(head, 20, body.aabbHi.z);
				PutI32(head, 24, nv);
				PutI32(head, 28, body.triangles.Length / 3);
				head[BodyHeadA] = (byte)((nv <= 0xFFFF) ? 1 : 0);
				PutF32(head, 33, body.blockLo.x);
				PutF32(head, 37, body.blockLo.y);
				PutF32(head, 41, body.blockLo.z);
				PutF32(head, 45, body.blockCell);
				PutI32(head, 49, body.bnx);
				PutI32(head, 53, body.bny);
				PutI32(head, 57, body.bnz);
				sections[SlotIndex(SectionKind.BodyHead, b, -1, nb, nf)] = new Raw(head, 0, BodyHeadBytes);
				byte[] verts = Pooled(pooled, checked(nv * 6));
				float lox = body.aabbLo.x;
				float loy = body.aabbLo.y;
				float loz = body.aabbLo.z;
				for (int i = 0; i < nv; i++)
				{
					PutHalf(verts, 6 * i, SuperHalf.Encode(body.vertices[i].x - lox));
					PutHalf(verts, 6 * i + 2, SuperHalf.Encode(body.vertices[i].y - loy));
					PutHalf(verts, 6 * i + 4, SuperHalf.Encode(body.vertices[i].z - loz));
				}
				sections[SlotIndex(SectionKind.Vertices, b, -1, nb, nf)] = new Raw(verts, 0, nv * 6);
				for (int c = 0; c < nf; c++)
				{
					sections[SlotIndex(SectionKind.VertexField, b, c, nb, nf)] = HalfSection(pooled, body.fields[c]);
				}
				int ni3 = body.triangles.Length;
				int elem = ((nv <= 0xFFFF) ? 2 : 4);
				byte[] tris = Pooled(pooled, checked(elem * ni3));
				if (nv <= 0xFFFF)
				{
					for (int i = 0; i < ni3; i++)
					{
						PutI16(tris, 2 * i, body.triangles[i]);
					}
				}
				else
				{
					Buffer.BlockCopy(body.triangles, 0, tris, 0, ni3 * 4);
				}
				sections[SlotIndex(SectionKind.Triangles, b, -1, nb, nf)] = new Raw(tris, 0, elem * ni3);
				for (int c = 0; c < nf; c++)
				{
					sections[SlotIndex(SectionKind.BlockField, b, c, nb, nf)] = HalfSection(pooled, body.blockFields[c]);
				}
			}
			using (MemoryStream ms = new MemoryStream(nb * 36 + 64 + nf * 8))
			{
				BinaryWriter w = new BinaryWriter(ms);
				for (int b = 0; b < nb; b++)
				{
					w.Write(f.telemetry[b].kineticJ);
					w.Write(f.telemetry[b].momentum.x);
					w.Write(f.telemetry[b].momentum.y);
					w.Write(f.telemetry[b].momentum.z);
					w.Write(f.telemetry[b].massInDomainKg);
					w.Write(f.telemetry[b].particleVolumeM3);
				}
				w.Write(f.escapedMassKg);
				w.Write(f.verdictSoFar);
				w.Write(f.fieldMin.Length);
				for (int i = 0; i < f.fieldMin.Length; i++)
				{
					w.Write(f.fieldMin[i]);
					w.Write(f.fieldMax[i]);
				}
				w.Write(f.debrisCount);
				w.Flush();
				sections[SlotIndex(SectionKind.Tail, -1, -1, nb, nf)] = new Raw(ms.ToArray(), 0, (int)ms.Position);
			}
			int nd = f.debrisCount;
			byte[] pos = Pooled(pooled, checked(nd * 12));
			for (int i = 0; i < nd; i++)
			{
				PutF32(pos, 12 * i, f.debrisPos[i].x);
				PutF32(pos, 12 * i + 4, f.debrisPos[i].y);
				PutF32(pos, 12 * i + 8, f.debrisPos[i].z);
			}
			sections[SlotIndex(SectionKind.DebrisPos, -1, -1, nb, nf)] = new Raw(pos, 0, nd * 12);
			for (int c = 0; c < nf; c++)
			{
				sections[SlotIndex(SectionKind.DebrisField, -1, c, nb, nf)] = ((nd > 0) ? HalfSection(pooled, f.debrisFields[c]) : new Raw(new byte[0], 0, 0));
			}
			long canonicalLen = 0L;
			for (int k = 0; k < sections.Length; k++)
			{
				canonicalLen += sections[k].len;
			}
			if (canonicalLen > 0x7FFFFFFF)
			{
				throw new IOException(string.Format("кадр {0}: {1} байт — больше 2 ГБ, в ленту не пишется", f.index, canonicalLen));
			}
			ulong hash = HashCanonical(sections, nb, nf, (int)canonicalLen);
			return BuildStored(sections, nb, nf, (int)canonicalLen, hash, codec);
		}
		finally
		{
			foreach (byte[] buf in pooled)
			{
				Pool.Give(buf);
			}
		}
	}

	private static byte[] Pooled(List<byte[]> list, int n)
	{
		byte[] array = Pool.Take(Math.Max(1, n));
		list.Add(array);
		return array;
	}

	private static Raw HalfSection(List<byte[]> pooled, float[] a)
	{
		int n = checked(a.Length * 2);
		byte[] buf = Pooled(pooled, n);
		for (int i = 0; i < a.Length; i++)
		{
			PutHalf(buf, 2 * i, SuperHalf.Encode(a[i]));
		}
		return new Raw(buf, 0, n);
	}

	private static void PutHalf(byte[] b, int at, ushort h)
	{
		b[at] = (byte)h;
		b[at + 1] = (byte)(h >> 8);
	}

	private static void PutF32(byte[] b, int at, float v)
	{
		int bits = BitConverter.SingleToInt32Bits(v);
		b[at] = (byte)bits;
		b[at + 1] = (byte)(bits >> 8);
		b[at + 2] = (byte)(bits >> 16);
		b[at + 3] = (byte)(bits >> 24);
	}

	private static void PutI16(byte[] b, int at, int v)
	{
		b[at] = (byte)v;
		b[at + 1] = (byte)(v >> 8);
	}

	private static void PutI32(byte[] b, int at, int v)
	{
		b[at] = (byte)v;
		b[at + 1] = (byte)(v >> 8);
		b[at + 2] = (byte)(v >> 16);
		b[at + 3] = (byte)(v >> 24);
	}

	private static void PutU64(byte[] b, int at, ulong v)
	{
		b[at] = (byte)v;
		b[at + 1] = (byte)(v >> 8);
		b[at + 2] = (byte)(v >> 16);
		b[at + 3] = (byte)(v >> 24);
		b[at + 4] = (byte)(v >> 32);
		b[at + 5] = (byte)(v >> 40);
		b[at + 6] = (byte)(v >> 48);
		b[at + 7] = (byte)(v >> 56);
	}

	public static string CanonicalSha256(string path)
	{
		using (Reader r = new Reader(path))
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] header = r.CanonicalHeaderImage();
				sha.TransformBlock(header, 0, header.Length, null, 0);
				for (int i = 0; i < r.FrameCount; i++)
				{
					byte[] canonical = r.ReadCanonical(i, out int len);
					sha.TransformBlock(BitConverter.GetBytes(len), 0, 4, null, 0);
					sha.TransformBlock(canonical, 0, len, null, 0);
					sha.TransformBlock(BitConverter.GetBytes(r.CanonicalHashOf(i)), 0, 8, null, 0);
				}
				sha.TransformFinalBlock(new byte[0], 0, 0);
				StringBuilder sb = new StringBuilder(64);
				foreach (byte b in sha.Hash)
				{
					sb.Append(b.ToString("x2"));
				}
				return sb.ToString();
			}
		}
	}
}
