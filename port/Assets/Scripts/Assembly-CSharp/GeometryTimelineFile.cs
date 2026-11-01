using System;
using System.IO;
using Unity.Collections;
using UnityEngine;

public static class GeometryTimelineFile
{
	public const uint Magic = 1431065601u;

	public const int Version = 4;

	public const string Extension = ".ultra.bin";

	public static void Save(GeometryTimeline tl, string path)
	{
		using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
		{
			using (BinaryWriter w = new BinaryWriter(fs))
			{
				w.Write(Magic);
				w.Write(Version);
				w.Write(tl.dtOutput);
				w.Write(tl.fieldGridR);
				w.Write(tl.fieldGridZ);
				w.Write(tl.fieldBounds.rMaxM);
				w.Write(tl.fieldBounds.zMinM);
				w.Write(tl.fieldBounds.zMaxM);
				w.Write(tl.frames.Count);
				for (int i = 0; i < tl.frames.Count; i++)
				{
					GeometryFrame f = tl.frames[i];
					w.Write(f.time);
					w.Write(f.erodedMassKg);
					w.Write(f.tipAxialVelocity);
					w.Write(f.projectileLoopCount);
					w.Write(f.targetLoopCount);
					WriteProfile(w, f.projectileProfile);
					WriteProfile(w, f.targetProfile);
					WriteSeeds(w, f.fragmentSeeds);
					WriteField(w, f.fieldPlastic);
					WriteBody(w, f.fieldBody);
				}
			}
		}
	}

	private static void WriteField(BinaryWriter w, NativeArray<float> f)
	{
		int n = f.IsCreated ? f.Length : 0;
		w.Write(n);
		for (int i = 0; i < n; i++)
		{
			w.Write(f[i]);
		}
	}

	private static void WriteBody(BinaryWriter w, NativeArray<byte> b)
	{
		int n = b.IsCreated ? b.Length : 0;
		w.Write(n);
		for (int i = 0; i < n; i++)
		{
			w.Write(b[i]);
		}
	}

	private static NativeArray<byte> ReadBody(BinaryReader r)
	{
		int n = r.ReadInt32();
		NativeArray<byte> b = ((n > 0) ? new NativeArray<byte>(n, Allocator.Persistent) : default(NativeArray<byte>));
		for (int i = 0; i < n; i++)
		{
			b[i] = r.ReadByte();
		}
		return b;
	}

	private static NativeArray<float> ReadField(BinaryReader r)
	{
		int n = r.ReadInt32();
		NativeArray<float> f = ((n > 0) ? new NativeArray<float>(n, Allocator.Persistent) : default(NativeArray<float>));
		for (int i = 0; i < n; i++)
		{
			f[i] = r.ReadSingle();
		}
		return f;
	}

	private static void WriteProfile(BinaryWriter w, NativeArray<Vector2> p)
	{
		int n = p.IsCreated ? p.Length : 0;
		w.Write(n);
		for (int i = 0; i < n; i++)
		{
			w.Write(p[i].x);
			w.Write(p[i].y);
		}
	}

	private static void WriteSeeds(BinaryWriter w, NativeArray<ContinuumFragmentSeed> s)
	{
		int n = s.IsCreated ? s.Length : 0;
		w.Write(n);
		for (int i = 0; i < n; i++)
		{
			ContinuumFragmentSeed seed = s[i];
			w.Write(seed.r);
			w.Write(seed.z);
			w.Write(seed.vr);
			w.Write(seed.vz);
			w.Write(seed.massKg);
			w.Write(seed.timeS);
			w.Write(seed.body);
		}
	}

	public static GeometryTimeline Load(string path, out string error)
	{
		error = null;
		if (!File.Exists(path))
		{
			error = "файла нет: " + path;
			return null;
		}
		GeometryTimeline result;
		using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
		{
			result = Load(fs, out error);
		}
		return result;
	}

	public static GeometryTimeline Load(Stream stream, out string error)
	{
		error = null;
		GeometryTimeline tl = new GeometryTimeline();
		tl.frames = new System.Collections.Generic.List<GeometryFrame>();
		tl.diagnostics = new SolverDiagnostics();
		using (BinaryReader r = new BinaryReader(stream))
		{
			uint magic = r.ReadUInt32();
			if (magic != Magic)
			{
				error = "не запись прогона ULTRA";
				return null;
			}
			int version = r.ReadInt32();
			if (version != Version)
			{
				error = string.Format("версия формата {0}, эта сборка читает {1}", version, Version);
				return null;
			}
			tl.dtOutput = r.ReadSingle();
			tl.fieldGridR = r.ReadInt32();
			tl.fieldGridZ = r.ReadInt32();
			UltraFieldGrid.Bounds b = default(UltraFieldGrid.Bounds);
			b.rMaxM = r.ReadSingle();
			b.zMinM = r.ReadSingle();
			b.zMaxM = r.ReadSingle();
			tl.fieldBounds = b;
			int count = r.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				GeometryFrame f = default(GeometryFrame);
				f.time = r.ReadSingle();
				f.erodedMassKg = r.ReadSingle();
				f.tipAxialVelocity = r.ReadSingle();
				f.projectileLoopCount = r.ReadInt32();
				f.targetLoopCount = r.ReadInt32();
				f.projectileProfile = ReadProfile(r);
				f.targetProfile = ReadProfile(r);
				f.fragmentSeeds = ReadSeeds(r);
				f.fieldPlastic = ReadField(r);
				f.fieldBody = ReadBody(r);
				tl.frames.Add(f);
			}
		}
		tl.Completed = true;
		return tl;
	}

	private static NativeArray<Vector2> ReadProfile(BinaryReader r)
	{
		int n = r.ReadInt32();
		NativeArray<Vector2> p = ((n > 0) ? new NativeArray<Vector2>(n, Allocator.Persistent) : default(NativeArray<Vector2>));
		for (int i = 0; i < n; i++)
		{
			Vector2 v = default(Vector2);
			v.x = r.ReadSingle();
			v.y = r.ReadSingle();
			p[i] = v;
		}
		return p;
	}

	private static NativeArray<ContinuumFragmentSeed> ReadSeeds(BinaryReader r)
	{
		int n = r.ReadInt32();
		NativeArray<ContinuumFragmentSeed> s = ((n > 0) ? new NativeArray<ContinuumFragmentSeed>(n, Allocator.Persistent) : default(NativeArray<ContinuumFragmentSeed>));
		for (int i = 0; i < n; i++)
		{
			ContinuumFragmentSeed seed = default(ContinuumFragmentSeed);
			seed.r = r.ReadSingle();
			seed.z = r.ReadSingle();
			seed.vr = r.ReadSingle();
			seed.vz = r.ReadSingle();
			seed.massKg = r.ReadSingle();
			seed.timeS = r.ReadSingle();
			seed.body = r.ReadInt32();
			s[i] = seed;
		}
		return s;
	}
}
