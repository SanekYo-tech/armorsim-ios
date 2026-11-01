// Восстановлены все методы. Формат файла (Write/Read): uint Magic, int Version, int headLen,
// byte[headLen] шапка (HeaderBytes/ParseHeader), ulong hash шапки, затем 9 блоков Block/ReadBlock
// (id=1..9: pos, vel, C, F, S, sigma, scal, dmg, cause — по d.particles элементов), ulong hash файла.
// Хэш: FNV-1a 14695981039346656037 по [4 байта headLen][head]  затем цепочка хэшей блоков (Block).
// S() дописана длина строки (w.Write(len); w.Write(bytes)) — по ISIL у S ровно два вызова Write.
// Инициализаторы полей Data/Scene восстановлены по Data..ctor (0x180689320): run="player_shot",
// device=deviceVersion=unityFirst="", scene=new Scene() (plates=new List, tapeTmpPath=tapeDir=
// tapeName="", drives=new DriveSpec[0]); состояние массивов state-полей — из FullState..ctor.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public static class SuperCheckpoint
{
	public struct DriveSpec
	{
		public int frontBody;

		public int rearBody;

		public int triggerBody;

		public float velocityMps;

		public float initiationDelayS;

		public float rampS;

		public float triggerDistanceM;

		public bool driveFront;

		public bool driveRear;

		public SuperEraDrive.State state;

		public int kind;

		public int targetPlateBody;

		public bool sabotageOpenBase;
	}

	public sealed class Scene
	{
		public int kind;

		public SuperRodSpec rod;

		public SuperJetSpec jet;

		public List<SuperPlateSpec> plates = new List<SuperPlateSpec>();

		public float spacing;

		public float durationS;

		public float marginM;

		public float cadenceS;

		public string tapeTmpPath = "";

		public string tapeDir = "";

		public string tapeName = "";

		public DriveSpec[] drives = new DriveSpec[0];

		public float probeEveryS;
	}

	public sealed class Data
	{
		public string run = "player_shot";

		public long slotSeq;

		public long utcTicks;

		public string device = "";

		public string deviceVersion = "";

		public ulong shaderHash;

		public Scene scene = new Scene();

		public int particles;

		public int steps;

		public float dtS;

		public int step;

		public int nextKf;

		public int unityErrors;

		public float simTimeS;

		public string unityFirst = "";

		public long tapeBytes;

		public int frames;

		public int maxVertsFrame;

		public int maxRawVertsFrame;

		public bool damageEnabled;

		public bool coherentFlyers;

		public float softening;

		public float coherentFloorPa;

		public float capN;

		public bool xiScatterOn;

		public float xiModulus;

		public uint xiSalt;

		public double s43Alpha;

		public double s43Beta;

		public double s43VRefMm3;

		public double s43HRefMm;

		public float xiCeiling;

		public SuperKernelGPU.FullState state;
	}

	public sealed class Probe
	{
		public bool usable;

		public string note;

		public string path;

		public string run;

		public long slotSeq;

		public long utcTicks;

		public float simTimeS;

		public float durationS;

		public int frames;

		public int step;

		public int steps;

		public string sceneLabel;

		public string tapeTmpPath;
	}

	public const uint Magic = 1347109715u;

	public const int Version = 10;

	public static float IntervalS;

	public static int EveryFrames;

	public const string DefaultRun = "player_shot";

	public static string ActiveRun;

	private static ulong? _shaderHash;

	private static readonly Dictionary<string, long> lastSeq;

	static SuperCheckpoint()
	{
		IntervalS = 120f;
		EveryFrames = 0;
		lastSeq = new Dictionary<string, long>();
	}

	private const int MaxProfileItems = 4096;

	private const int MaxMeshFiles = 256;

	public const int BytesPerParticle = 192;

	public static string Dir
	{
		get
		{
			return Path.Combine(SuperTapeRecorder.TapeDir, "checkpoints");
		}
	}

	public static string SlotPath(string run, char slot)
	{
		return Path.Combine(Dir, run + "." + slot.ToString() + ".ckpt");

	}

	public static ulong ShaderHash()
	{
		if (!_shaderHash.HasValue)
		{
			string path = Path.Combine(Application.dataPath, "Scripts", "Super", "Resources", "SuperSolver.compute");
			ulong hash = 0uL;
			if (File.Exists(path))
			{
				byte[] bytes = File.ReadAllBytes(path);
				hash = SuperTape.Fnv1a(bytes, 0, bytes.Length, 14695981039346656037uL);
			}
			_shaderHash = hash;
		}
		return _shaderHash.Value;
	}

	public static List<string> Runs()
	{
		List<string> runs = new List<string>();
		if (!Directory.Exists(Dir))
		{
			return runs;
		}
		string[] files = Directory.GetFiles(Dir, "*.ckpt");
		for (int i = 0; i < files.Length; i++)
		{
			string name = Path.GetFileName(files[i]);
			if (name.EndsWith(".A.ckpt") || name.EndsWith(".B.ckpt"))
			{
				string run = name.Substring(0, name.Length - ".A.ckpt".Length);
				if (!runs.Contains(run))
				{
					runs.Add(run);
				}
			}
		}
		return runs;
	}

	public static void KeepLatestRunOnly(string except)
	{
		List<string> runs = Runs();
		runs.Remove(except);
		if (runs.Count > 1)
		{
			runs.Sort((string a, string b) => NewestSlotTime(b).CompareTo(NewestSlotTime(a)));
			for (int i = 1; i < runs.Count; i++)
			{
				DiscardAll(runs[i]);
			}
		}
	}

	private static DateTime NewestSlotTime(string run)
	{
		DateTime newest = DateTime.MinValue;
		char[] slots = new char[2] { 'A', 'B' };
		char[] array = slots;
		for (int i = 0; i < array.Length; i++)
		{
			char slot = array[i];
			string path = SlotPath(run, slot);
			if (File.Exists(path))
			{
				DateTime t = File.GetLastWriteTimeUtc(path);
				if (t > newest)
				{
					newest = t;
				}
			}
		}
		return newest;
	}

	public static string Write(Data d)
	{
		Directory.CreateDirectory(Dir);
		long seq;
		if (!lastSeq.TryGetValue(d.run, out seq))
		{
			seq = Math.Max(SeqOf(SlotPath(d.run, 'A')), SeqOf(SlotPath(d.run, 'B')));
		}
		d.slotSeq = seq + 1;
		lastSeq[d.run] = d.slotSeq;
		char slot = ((d.slotSeq & 1) == 0) ? 'A' : 'B';
		string path = SlotPath(d.run, slot);
		string partPath = path + ".part";
		using (FileStream fs = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None, 1048576))
		{
			using (BinaryWriter w = new BinaryWriter(fs))
			{
				w.Write(Magic);
				w.Write(Version);
				byte[] head = HeaderBytes(d);
				w.Write(head.Length);
				w.Write(head);
				ulong fileHash = SuperTape.Fnv1a(BitConverter.GetBytes(head.Length), 0, 4, 14695981039346656037uL);
				fileHash = SuperTape.Fnv1a(head, 0, head.Length, fileHash);
				fileHash = Block(w, fs, 1, MemoryMarshal.AsBytes(new ReadOnlySpan<Vector3>(d.state.pos)), fileHash);
				fileHash = Block(w, fs, 2, MemoryMarshal.AsBytes(new ReadOnlySpan<Vector3>(d.state.vel)), fileHash);
				fileHash = Block(w, fs, 3, MemoryMarshal.AsBytes(new ReadOnlySpan<SuperMat3>(d.state.C)), fileHash);
				fileHash = Block(w, fs, 4, MemoryMarshal.AsBytes(new ReadOnlySpan<SuperMat3>(d.state.F)), fileHash);
				fileHash = Block(w, fs, 5, MemoryMarshal.AsBytes(new ReadOnlySpan<SuperMat3>(d.state.S)), fileHash);
				fileHash = Block(w, fs, 6, MemoryMarshal.AsBytes(new ReadOnlySpan<SuperMat3>(d.state.sigma)), fileHash);
				fileHash = Block(w, fs, 7, MemoryMarshal.AsBytes(new ReadOnlySpan<Vector4>(d.state.scal)), fileHash);
				fileHash = Block(w, fs, 8, MemoryMarshal.AsBytes(new ReadOnlySpan<float>(d.state.dmg)), fileHash);
				fileHash = Block(w, fs, 9, MemoryMarshal.AsBytes(new ReadOnlySpan<uint>(d.state.cause)), fileHash);
				w.Write(fileHash);
				w.Flush();
			}
			fs.Flush(flushToDisk: true);
		}
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		File.Move(partPath, path);
		return path;
	}

	private static ulong Block(BinaryWriter w, FileStream fs, int id, ReadOnlySpan<byte> bytes, ulong fileHash)
	{
		w.Write(id);
		long n = bytes.Length;
		w.Write(n);
		w.Flush();
		fs.Write(bytes);
		ulong h = SuperTape.Fnv1a(BitConverter.GetBytes(n), 0, 8, 14695981039346656037uL);
		h = SuperTape.Fnv1a(bytes, h);
		w.Write(h);
		return SuperTape.Fnv1a(BitConverter.GetBytes(h), 0, 8, fileHash);
	}

	private static byte[] HeaderBytes(Data d)
	{
		using (MemoryStream mem = new MemoryStream())
		{
			using (BinaryWriter w = new BinaryWriter(mem, Encoding.UTF8))
			{
				byte[] b = Encoding.UTF8.GetBytes(d.run ?? "");
				w.Write(b.Length);
				w.Write(b);
				w.Write(d.slotSeq);
				w.Write(d.utcTicks);
				b = Encoding.UTF8.GetBytes(d.device ?? "");
				w.Write(b.Length);
				w.Write(b);
				b = Encoding.UTF8.GetBytes(d.deviceVersion ?? "");
				w.Write(b.Length);
				w.Write(b);
				w.Write(d.shaderHash);
				Scene s = d.scene;
				w.Write(s.kind);
				w.Write(s.rod.diameterM);
				w.Write(s.rod.lengthM);
				w.Write(s.rod.velocityMps);
				w.Write((int)s.rod.nose);
				b = Encoding.UTF8.GetBytes(s.rod.material ?? "");
				w.Write(b.Length);
				w.Write(b);
				w.Write(s.rod.tipZM);
				w.Write(s.rod.spacingM);
				w.Write(s.rod.cavityFraction);
				WriteProfile(w, s.rod.hasProfile, s.rod.profile);
				w.Write(s.jet.lengthM);
				w.Write(s.jet.diameterM);
				w.Write(s.jet.vTipMps);
				w.Write(s.jet.vTailMps);
				w.Write(s.jet.tipZM);
				S(w, s.jet.material);
				w.Write(s.jet.spacingM);
				w.Write(s.plates.Count);
				foreach (SuperPlateSpec pl in s.plates)
				{
					w.Write(pl.thicknessM);
					w.Write(pl.angleXDeg);
					w.Write(pl.alongAxisM);
					w.Write(pl.sideM);
					b = Encoding.UTF8.GetBytes(pl.material ?? "");
					w.Write(b.Length);
					w.Write(b);
					w.Write(pl.clampedEdges);
					w.Write(pl.noFragmentation);
					w.Write(pl.spacingM);
					w.Write(pl.fineCoreRadiusM);
					w.Write(pl.offsetUM);
					w.Write(pl.offsetVM);
					w.Write(pl.hasPose);
					w.Write(pl.poseCentreM.x);
					w.Write(pl.poseCentreM.y);
					w.Write(pl.poseCentreM.z);
					w.Write(pl.poseU.x);
					w.Write(pl.poseU.y);
					w.Write(pl.poseU.z);
					w.Write(pl.poseV.x);
					w.Write(pl.poseV.y);
					w.Write(pl.poseV.z);
					w.Write(pl.poseN.x);
					w.Write(pl.poseN.y);
					w.Write(pl.poseN.z);
					w.Write(pl.widthM);
					w.Write(pl.heightM);
					WriteClipAndMesh(w, pl.clip, pl.mesh);
					w.Write(pl.roundU);
				}
				w.Write(s.spacing);
				w.Write(s.durationS);
				w.Write(s.marginM);
				w.Write(s.cadenceS);
				b = Encoding.UTF8.GetBytes(s.tapeTmpPath ?? "");
				w.Write(b.Length);
				w.Write(b);
				b = Encoding.UTF8.GetBytes(s.tapeDir ?? "");
				w.Write(b.Length);
				w.Write(b);
				b = Encoding.UTF8.GetBytes(s.tapeName ?? "");
				w.Write(b.Length);
				w.Write(b);
				w.Write(s.probeEveryS);
				w.Write(s.drives.Length);
				for (int j = 0; j < s.drives.Length; j++)
				{
					DriveSpec dr = s.drives[j];
					w.Write(dr.frontBody);
					w.Write(dr.rearBody);
					w.Write(dr.triggerBody);
					w.Write(dr.velocityMps);
					w.Write(dr.initiationDelayS);
					w.Write(dr.rampS);
					w.Write(dr.triggerDistanceM);
					w.Write(dr.driveFront);
					w.Write(dr.driveRear);
					w.Write(dr.state.fired);
					w.Write(dr.state.contactTimeS);
					w.Write(dr.state.fireTimeS);
					w.Write(dr.state.deliveredMps);
					w.Write(dr.state.nextProbeS);
					w.Write(dr.state.probes);
					w.Write(dr.kind);
					w.Write(dr.targetPlateBody);
					w.Write(dr.sabotageOpenBase);
				}
				w.Write(d.particles);
				w.Write(d.steps);
				w.Write(d.dtS);
				w.Write(d.step);
				w.Write(d.nextKf);
				w.Write(d.unityErrors);
				w.Write(d.simTimeS);
				b = Encoding.UTF8.GetBytes(d.unityFirst ?? "");
				w.Write(b.Length);
				w.Write(b);
				w.Write(d.tapeBytes);
				w.Write(d.frames);
				w.Write(d.maxVertsFrame);
				w.Write(d.maxRawVertsFrame);
				w.Write(d.damageEnabled);
				w.Write(d.coherentFlyers);
				w.Write(d.softening);
				w.Write(d.coherentFloorPa);
				w.Write(d.capN);
				w.Write(d.xiScatterOn);
				w.Write(d.xiModulus);
				w.Write(d.xiSalt);
				w.Write(d.s43Alpha);
				w.Write(d.s43Beta);
				w.Write(d.s43VRefMm3);
				w.Write(d.s43HRefMm);
				w.Write(d.xiCeiling);
				w.Write(d.state.fixedPointScale);
				for (int k = 0; k < 8; k++)
				{
					w.Write(d.state.fail[k]);
				}
				for (int m = 0; m < 4; m++)
				{
					w.Write(d.state.escaped[m]);
				}
				for (int n = 0; n < 4; n++)
				{
					w.Write(d.state.guard[n]);
				}
				w.Flush();
				return mem.ToArray();
			}
		}
	}

	private static void S(BinaryWriter w, string s)
	{
		byte[] b = Encoding.UTF8.GetBytes(s ?? "");
		w.Write(b.Length);
		w.Write(b);

	}

	private static string RS(BinaryReader r)
	{
		int len = r.ReadInt32();
		if (len > 1048576)
		{
			throw new IOException("строка длиной " + len.ToString() + " — точка битая");
		}
		return Encoding.UTF8.GetString(r.ReadBytes(len));

	}

	private static void WriteProfile(BinaryWriter w, bool has, SuperShellProfile p)
	{
		w.Write(has);
		w.Write(has && p.present);
		w.Write(has ? p.diameterM : 0f);
		w.Write(has ? (int)p.cavity : 0);
		S(w, has ? p.provenance : "");
		w.Write((has && p.outer != null) ? p.outer.Length : 0);
		if (has && p.outer != null)
		{
			for (int i = 0; i < p.outer.Length; i++)
			{
				w.Write(p.outer[i].aM);
				w.Write(p.outer[i].rM);
				w.Write((int)p.outer[i].law);
				w.Write((int)p.outer[i].noseShape);
				w.Write(p.outer[i].noseLenM);
				w.Write(p.outer[i].noseRefRM);
			}
		}
		w.Write((has && p.regions != null) ? p.regions.Length : 0);
		if (has && p.regions != null)
		{
			for (int i = 0; i < p.regions.Length; i++)
			{
				S(w, p.regions[i].materialId);
				S(w, p.regions[i].name);
				w.Write(p.regions[i].latSub);
				w.Write(p.regions[i].aFromM);
				w.Write(p.regions[i].aToM);
				w.Write(p.regions[i].rInnerM);
				w.Write(p.regions[i].rOuterM);
			}
		}
	}

	private static SuperShellProfile ReadProfile(BinaryReader r, out bool has)
	{
		SuperShellProfile p = default(SuperShellProfile);
		has = r.ReadBoolean();
		p.present = r.ReadBoolean();
		p.diameterM = r.ReadSingle();
		p.cavity = (SuperProfileCavity)r.ReadInt32();
		p.provenance = RS(r);
		int nOuter = r.ReadInt32();
		if (nOuter > MaxProfileItems)
		{
			throw new IOException("станций профиля " + nOuter + " — точка битая");
		}
		p.outer = new SuperProfileNode[nOuter];
		for (int i = 0; i < nOuter; i++)
		{
			p.outer[i].aM = r.ReadSingle();
			p.outer[i].rM = r.ReadSingle();
			p.outer[i].law = (SuperProfileLaw)r.ReadInt32();
			p.outer[i].noseShape = (NoseShape)r.ReadInt32();
			p.outer[i].noseLenM = r.ReadSingle();
			p.outer[i].noseRefRM = r.ReadSingle();
		}
		int nRegions = r.ReadInt32();
		if (nRegions > MaxProfileItems)
		{
			throw new IOException("областей профиля " + nRegions + " — точка битая");
		}
		p.regions = new SuperProfileRegion[nRegions];
		for (int i = 0; i < nRegions; i++)
		{
			p.regions[i].materialId = RS(r);
			p.regions[i].name = RS(r);
			p.regions[i].latSub = r.ReadInt32();
			p.regions[i].aFromM = r.ReadSingle();
			p.regions[i].aToM = r.ReadSingle();
			p.regions[i].rInnerM = r.ReadSingle();
			p.regions[i].rOuterM = r.ReadSingle();
		}
		return p;
	}

	private static void WriteClipAndMesh(BinaryWriter w, SuperClip c, SuperMeshRef m)
	{
		w.Write(c.present);
		w.Write(c.centre.x);
		w.Write(c.centre.y);
		w.Write(c.centre.z);
		w.Write(c.u.x);
		w.Write(c.u.y);
		w.Write(c.u.z);
		w.Write(c.v.x);
		w.Write(c.v.y);
		w.Write(c.v.z);
		w.Write(c.n.x);
		w.Write(c.n.y);
		w.Write(c.n.z);
		w.Write(c.hu);
		w.Write(c.hv);
		w.Write(c.hn);
		bool hasMesh = m != null && m.source != null;
		w.Write(hasMesh);
		w.Write(hasMesh ? m.source.files.Count : 0);
		if (hasMesh)
		{
			for (int i = 0; i < m.source.files.Count; i++)
			{
				S(w, m.source.files[i].path);
				S(w, m.source.files[i].sha256);
				w.Write(m.source.files[i].bytes);
			}
		}
		S(w, hasMesh ? m.source.contentSha : "");
		w.Write(hasMesh ? (int)m.source.settings.units : 0);
		w.Write(hasMesh ? (int)m.source.settings.up : 0);
		w.Write(hasMesh && m.source.settings.mirrorX);
		w.Write(hasMesh ? m.source.settings.Scale : 1f);
		w.Write(hasMesh ? m.source.partCount : 0);
		for (int i = 0; i < 16; i++)
		{
			w.Write(hasMesh ? m.modelToRegion[i] : 0f);
		}
		w.Write(hasMesh ? m.spacing : 0f);
		w.Write(hasMesh ? m.hu : 0f);
		w.Write(hasMesh ? m.hv : 0f);
		w.Write(hasMesh ? m.hn : 0f);
		w.Write(hasMesh ? m.part : 0);
		S(w, hasMesh ? m.partName : "");
	}

	private static void ReadClipAndMesh(BinaryReader r, out SuperClip c, out SuperMeshRef m)
	{
		c = default(SuperClip);
		c.present = r.ReadBoolean();
		c.centre.x = r.ReadSingle();
		c.centre.y = r.ReadSingle();
		c.centre.z = r.ReadSingle();
		c.u.x = r.ReadSingle();
		c.u.y = r.ReadSingle();
		c.u.z = r.ReadSingle();
		c.v.x = r.ReadSingle();
		c.v.y = r.ReadSingle();
		c.v.z = r.ReadSingle();
		c.n.x = r.ReadSingle();
		c.n.y = r.ReadSingle();
		c.n.z = r.ReadSingle();
		c.hu = r.ReadSingle();
		c.hv = r.ReadSingle();
		c.hn = r.ReadSingle();
		bool hasMesh = r.ReadBoolean();
		int nFiles = r.ReadInt32();
		if (nFiles > MaxMeshFiles)
		{
			throw new IOException("файлов модели " + nFiles + " — точка битая");
		}
		SuperMeshSource source = new SuperMeshSource();
		for (int i = 0; i < nFiles; i++)
		{
			ModelSourceFile f = default(ModelSourceFile);
			f.path = RS(r);
			f.sha256 = RS(r);
			f.bytes = r.ReadInt64();
			source.files.Add(f);
		}
		source.contentSha = RS(r);
		source.settings.units = (ModelUnits)r.ReadInt32();
		source.settings.up = (ModelUpAxis)r.ReadInt32();
		source.settings.mirrorX = r.ReadBoolean();
		source.settings.scale = r.ReadSingle();
		source.partCount = r.ReadInt32();
		SuperMeshRef mesh = new SuperMeshRef();
		mesh.source = source;
		for (int i = 0; i < 16; i++)
		{
			mesh.modelToRegion[i] = r.ReadSingle();
		}
		mesh.spacing = r.ReadSingle();
		mesh.hu = r.ReadSingle();
		mesh.hv = r.ReadSingle();
		mesh.hn = r.ReadSingle();
		mesh.part = r.ReadInt32();
		mesh.partName = RS(r);
		m = (hasMesh ? mesh : null);
	}

	private static void WV(BinaryWriter w, Vector3 v)
	{
		w.Write(v.x);
		w.Write(v.y);
		w.Write(v.z);

	}

	private static Vector3 RV(BinaryReader r)
	{
		return new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

	}

	private static long SeqOf(string path)
	{
		Data d = Read(path, headerOnly: true);
		if (d == null)
		{
			return -1L;
		}
		return d.slotSeq;

	}

	private static Data ParseHeader(byte[] head)
	{
		Data d = new Data();
		using (MemoryStream mem = new MemoryStream(head))
		{
			using (BinaryReader r = new BinaryReader(mem, Encoding.UTF8))
			{
				d.run = RS(r);
				d.slotSeq = r.ReadInt64();
				d.utcTicks = r.ReadInt64();
				d.device = RS(r);
				d.deviceVersion = RS(r);
				d.shaderHash = r.ReadUInt64();
				Scene s = d.scene;
				s.kind = r.ReadInt32();
				SuperRodSpec rod = default(SuperRodSpec);
				rod.diameterM = r.ReadSingle();
				rod.lengthM = r.ReadSingle();
				rod.velocityMps = r.ReadSingle();
				rod.nose = (NoseShape)r.ReadInt32();
				rod.material = RS(r);
				rod.tipZM = r.ReadSingle();
				rod.spacingM = r.ReadSingle();
				rod.cavityFraction = r.ReadSingle();
				bool hasProfile;
				rod.profile = ReadProfile(r, out hasProfile);
				rod.hasProfile = hasProfile;
				s.rod = rod;
				SuperJetSpec jet = default(SuperJetSpec);
				jet.lengthM = r.ReadSingle();
				jet.diameterM = r.ReadSingle();
				jet.vTipMps = r.ReadSingle();
				jet.vTailMps = r.ReadSingle();
				jet.tipZM = r.ReadSingle();
				jet.material = RS(r);
				jet.spacingM = r.ReadSingle();
				s.jet = jet;
				int n = r.ReadInt32();
				if (n < 0 || n > 1000)
				{
					throw new IOException("плит " + n.ToString() + " — точка битая");
				}
				for (int i = 0; i < n; i++)
				{
					SuperPlateSpec pl = default(SuperPlateSpec);
					pl.thicknessM = r.ReadSingle();
					pl.angleXDeg = r.ReadSingle();
					pl.alongAxisM = r.ReadSingle();
					pl.sideM = r.ReadSingle();
					pl.material = RS(r);
					pl.clampedEdges = r.ReadBoolean();
					pl.noFragmentation = r.ReadBoolean();
					pl.spacingM = r.ReadSingle();
					pl.fineCoreRadiusM = r.ReadSingle();
					pl.offsetUM = r.ReadSingle();
					pl.offsetVM = r.ReadSingle();
					pl.hasPose = r.ReadBoolean();
					pl.poseCentreM = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
					pl.poseU = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
					pl.poseV = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
					pl.poseN = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
					pl.widthM = r.ReadSingle();
					pl.heightM = r.ReadSingle();
					ReadClipAndMesh(r, out pl.clip, out pl.mesh);
					pl.roundU = r.ReadBoolean();
					s.plates.Add(pl);
				}
				s.spacing = r.ReadSingle();
				s.durationS = r.ReadSingle();
				s.marginM = r.ReadSingle();
				s.cadenceS = r.ReadSingle();
				s.tapeTmpPath = RS(r);
				s.tapeDir = RS(r);
				s.tapeName = RS(r);
				s.probeEveryS = r.ReadSingle();
				int nDrives = r.ReadInt32();
				if (nDrives < 0 || nDrives > 100)
				{
					throw new IOException("приводов " + nDrives.ToString() + " — точка битая");
				}
				s.drives = new DriveSpec[nDrives];
				for (int j = 0; j < nDrives; j++)
				{
					DriveSpec dr = default(DriveSpec);
					dr.frontBody = r.ReadInt32();
					dr.rearBody = r.ReadInt32();
					dr.triggerBody = r.ReadInt32();
					dr.velocityMps = r.ReadSingle();
					dr.initiationDelayS = r.ReadSingle();
					dr.rampS = r.ReadSingle();
					dr.triggerDistanceM = r.ReadSingle();
					dr.driveFront = r.ReadBoolean();
					dr.driveRear = r.ReadBoolean();
					dr.state.fired = r.ReadBoolean();
					dr.state.contactTimeS = r.ReadSingle();
					dr.state.fireTimeS = r.ReadSingle();
					dr.state.deliveredMps = r.ReadSingle();
					dr.state.nextProbeS = r.ReadSingle();
					dr.state.probes = r.ReadInt32();
					dr.kind = r.ReadInt32();
					dr.targetPlateBody = r.ReadInt32();
					dr.sabotageOpenBase = r.ReadBoolean();
					s.drives[j] = dr;
				}
				d.particles = r.ReadInt32();
				d.steps = r.ReadInt32();
				d.dtS = r.ReadSingle();
				d.step = r.ReadInt32();
				d.nextKf = r.ReadInt32();
				d.unityErrors = r.ReadInt32();
				d.simTimeS = r.ReadSingle();
				d.unityFirst = RS(r);
				d.tapeBytes = r.ReadInt64();
				d.frames = r.ReadInt32();
				d.maxVertsFrame = r.ReadInt32();
				d.maxRawVertsFrame = r.ReadInt32();
				d.damageEnabled = r.ReadBoolean();
				d.coherentFlyers = r.ReadBoolean();
				d.softening = r.ReadSingle();
				d.coherentFloorPa = r.ReadSingle();
				d.capN = r.ReadSingle();
				d.xiScatterOn = r.ReadBoolean();
				d.xiModulus = r.ReadSingle();
				d.xiSalt = r.ReadUInt32();
				d.s43Alpha = r.ReadDouble();
				d.s43Beta = r.ReadDouble();
				d.s43VRefMm3 = r.ReadDouble();
				d.s43HRefMm = r.ReadDouble();
				d.xiCeiling = r.ReadSingle();
				d.state = new SuperKernelGPU.FullState();
				d.state.count = d.particles;
				d.state.fixedPointScale = r.ReadSingle();
				for (int k = 0; k < 8; k++)
				{
					d.state.fail[k] = r.ReadUInt32();
				}
				for (int m = 0; m < 4; m++)
				{
					d.state.escaped[m] = r.ReadUInt32();
				}
				for (int q = 0; q < 4; q++)
				{
					d.state.guard[q] = r.ReadUInt32();
				}
				return d;
			}
		}
	}

	public static Data Read(string path, bool headerOnly = false)
	{
		using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1048576))
		{
			using (BinaryReader r = new BinaryReader(fs))
			{
				if (r.ReadUInt32() != Magic)
				{
					throw new IOException("не контрольная точка SUPER");
				}
				int version = r.ReadInt32();
				if (version != Version)
				{
					throw new IOException(string.Format("версия точки {0}, эта сборка читает {1}", version, Version));
				}
				int headLen = r.ReadInt32();
				if (headLen < 1 || headLen > 16777216)
				{
					throw new IOException("длина шапки " + headLen.ToString() + " — точка битая");
				}
				byte[] head = r.ReadBytes(headLen);
				ulong fileHash = r.ReadUInt64();
				ulong h = SuperTape.Fnv1a(BitConverter.GetBytes(headLen), 0, 4, 14695981039346656037uL);
				h = SuperTape.Fnv1a(head, 0, head.Length, h);
				if (fileHash != h)
				{
					throw new IOException("хэш шапки точки не сошёлся");
				}
				Data d = ParseHeader(head);
				if (headerOnly)
				{
					return d;
				}
				int n = d.particles;
				d.state.pos = new Vector3[n];
				d.state.vel = new Vector3[n];
				d.state.C = new SuperMat3[n];
				d.state.F = new SuperMat3[n];
				d.state.S = new SuperMat3[n];
				d.state.sigma = new SuperMat3[n];
				d.state.scal = new Vector4[n];
				d.state.dmg = new float[n];
				d.state.cause = new uint[n];
				fileHash = ReadBlock(r, fs, 1, MemoryMarshal.AsBytes(new Span<Vector3>(d.state.pos)), fileHash);
				fileHash = ReadBlock(r, fs, 2, MemoryMarshal.AsBytes(new Span<Vector3>(d.state.vel)), fileHash);
				fileHash = ReadBlock(r, fs, 3, MemoryMarshal.AsBytes(new Span<SuperMat3>(d.state.C)), fileHash);
				fileHash = ReadBlock(r, fs, 4, MemoryMarshal.AsBytes(new Span<SuperMat3>(d.state.F)), fileHash);
				fileHash = ReadBlock(r, fs, 5, MemoryMarshal.AsBytes(new Span<SuperMat3>(d.state.S)), fileHash);
				fileHash = ReadBlock(r, fs, 6, MemoryMarshal.AsBytes(new Span<SuperMat3>(d.state.sigma)), fileHash);
				fileHash = ReadBlock(r, fs, 7, MemoryMarshal.AsBytes(new Span<Vector4>(d.state.scal)), fileHash);
				fileHash = ReadBlock(r, fs, 8, MemoryMarshal.AsBytes(new Span<float>(d.state.dmg)), fileHash);
				fileHash = ReadBlock(r, fs, 9, MemoryMarshal.AsBytes(new Span<uint>(d.state.cause)), fileHash);
				if (r.ReadUInt64() != fileHash)
				{
					throw new IOException("хэш файла точки не сошёлся — запись оборвана");
				}
				return d;
			}
		}
	}

	private static ulong ReadBlock(BinaryReader r, FileStream fs, int id, Span<byte> into, ulong fileHash)
	{
		int blockId = r.ReadInt32();
		long len = r.ReadInt64();
		if (blockId != id || len != into.Length)
		{
			throw new IOException(string.Format("блок {0}: в файле блок {1} длиной {2} при ожидаемых {3}", id, blockId, len, into.Length));
		}
		int offset = 0;
		while (offset < into.Length)
		{
			int n = fs.Read(into.Slice(offset));
			if (n <= 0)
			{
				throw new IOException(string.Format("блок {0}: файл кончился на {1} из {2}", id, offset, into.Length));
			}
			offset += n;
		}
		ulong h = SuperTape.Fnv1a(BitConverter.GetBytes(len), 0, 8, 14695981039346656037uL);
		h = SuperTape.Fnv1a(into, h);
		ulong stored = r.ReadUInt64();
		if (stored != h)
		{
			throw new IOException(string.Format("блок {0}: хэш не сошёлся", id));
		}
		return SuperTape.Fnv1a(BitConverter.GetBytes(stored), 0, 8, fileHash);
	}

	public static Probe ProbeLatest()
	{
		StringBuilder sb = new StringBuilder();
		Probe best = null;
		foreach (string run in Runs())
		{
			Probe p = ProbeLatest(run);
			if (p == null)
			{
				continue;
			}
			if (!p.usable)
			{
				sb.Append(p.note + "; ");
				continue;
			}
			if (best != null && best.usable && p.utcTicks <= best.utcTicks)
			{
				continue;
			}
			best = p;
		}
		if (best != null)
		{
			return best;
		}
		string notes = sb.ToString();
		if (notes == null)
		{
			return null;
		}
		Probe probe = new Probe();
		probe.usable = false;
		probe.note = notes.TrimEnd(' ', ';');
		return probe;
	}

	public static Probe ProbeLatest(string run)
	{
		string pathA = SlotPath(run, 'A');
		string pathB = SlotPath(run, 'B');
		if (!File.Exists(pathA) && !File.Exists(pathB))
		{
			return null;
		}
		List<(long seq, string path)> slots = new List<(long seq, string path)>();
		string[] files = new string[2] { pathA, pathB };
		for (int i = 0; i < files.Length; i++)
		{
			if (!File.Exists(files[i]))
			{
				continue;
			}
			slots.Add((SeqOf(files[i]), files[i]));
		}
		slots.Sort((x, y) => y.seq.CompareTo(x.seq));
		StringBuilder sb = new StringBuilder();
		foreach ((long seq, string path) in slots)
		{
			try
			{
				Data d = Read(path, headerOnly: true);
				string gpu = SystemInfo.graphicsDeviceName ?? "";
				string gpuVersion = SystemInfo.graphicsDeviceVersion ?? "";
				if (d.device != gpu)
				{
					throw new IOException("точка снята на «" + d.device + "», сейчас «" + gpu + "» — побитовое продолжение не гарантировано");
				}
				if (d.deviceVersion != gpuVersion)
				{
					throw new IOException("точка снята на драйвере «" + d.deviceVersion + "», сейчас «" + gpuVersion + "» — побитовое продолжение не гарантировано");
				}
				if (d.shaderHash != 0 && ShaderHash() != 0 && d.shaderHash != ShaderHash())
				{
					throw new IOException("текст шейдера изменился с момента точки — продолжение было бы другим решателем");
				}
				if (!File.Exists(d.scene.tapeTmpPath) || new FileInfo(d.scene.tapeTmpPath).Length < d.tapeBytes)
				{
					throw new IOException("ленты прогона нет или она короче, чем знала точка");
				}
				string why;
				if (!VerifyBlocks(path, d, out why))
				{
					throw new IOException(why);
				}
				Probe p = new Probe();
				p.usable = true;
				p.path = path;
				p.run = run;
				p.slotSeq = d.slotSeq;
				p.utcTicks = d.utcTicks;
				p.simTimeS = d.simTimeS;
				p.durationS = d.scene.durationS;
				p.frames = d.frames;
				p.step = d.step;
				p.steps = d.steps;
				p.tapeTmpPath = d.scene.tapeTmpPath;
				p.sceneLabel = ((d.scene.kind == 2) ? "струя" : "стержень");
				p.note = Path.GetFileName(path) + ((d.shaderHash != 0 && ShaderHash() != 0) ? "" : " (шейдер не сверен: файла нет)");
				return p;
			}
			catch (IOException ex)
			{
				sb.Append(Path.GetFileName(path) + ": " + ex.Message + "; ");
			}
		}
		Probe probe = new Probe();
		probe.usable = false;
		probe.run = run;
		probe.note = sb.ToString().TrimEnd(' ', ';');
		return probe;
	}

	public static bool VerifyBlocks(string path, Data d, out string why)
	{
		why = null;
		int[] blockBytes = new int[9]
		{
			12, 12, 36, 36, 36, 36, 16, 4, 4
		};
		using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1048576))
		{
			using (BinaryReader r = new BinaryReader(fs))
			{
				r.ReadUInt32();
				r.ReadInt32();
				int headLen = r.ReadInt32();
				fs.Seek(headLen, SeekOrigin.Current);
				ulong fileHash = r.ReadUInt64();
				byte[] buf = new byte[1048576];
				for (int id = 1; id <= 9; id++)
				{
					int blockId = r.ReadInt32();
					long len = r.ReadInt64();
					long expected = (long)blockBytes[id - 1] * d.particles;
					if (blockId != id || len != expected)
					{
						why = string.Format("блок {0}: в файле блок {1} длиной {2} при ожидаемых {3}", id, blockId, len, expected);
						return false;
					}
					ulong h = SuperTape.Fnv1a(BitConverter.GetBytes(len), 0, 8, 14695981039346656037uL);
					while (len > 0)
					{
						int got = fs.Read(buf, 0, (int)Math.Min(buf.Length, len));
						if (got <= 0)
						{
							why = string.Format("блок {0}: файл кончился", id);
							return false;
						}
						h = SuperTape.Fnv1a(buf, 0, got, h);
						len -= got;
					}
					if (r.ReadUInt64() != h)
					{
						why = string.Format("блок {0}: хэш не сошёлся", id);
						return false;
					}
					fileHash = SuperTape.Fnv1a(BitConverter.GetBytes(h), 0, 8, fileHash);
				}
				if (r.ReadUInt64() != fileHash)
				{
					why = "хэш файла точки не сошёлся — запись оборвана";
					return false;
				}
				return true;
			}
		}
	}

	public static void DiscardAll(string run = "player_shot")
	{
		run = run ?? DefaultRun;
		char[] slots = new char[2] { 'A', 'B' };
		for (int i = 0; i < slots.Length; i++)
		{
			string p = SlotPath(run, slots[i]);
			if (File.Exists(p))
			{
				File.Delete(p);
			}
			if (File.Exists(p + ".part"))
			{
				File.Delete(p + ".part");
			}
		}

	}

	public static void DiscardEveryRun()
	{
		foreach (string run in Runs())
		{
			DiscardAll(run);
		}

	}

	public static Data Capture(SuperShot.KeyframeContext ctx, Scene scene, SuperEraDrive drive, long tapeBytes, int frames, int maxVertsFrame, int maxRawVertsFrame, string run = null)
	{
		Data d = new Data();
		d.run = run ?? ActiveRun ?? DefaultRun;
		d.utcTicks = DateTime.UtcNow.Ticks;
		d.device = SystemInfo.graphicsDeviceName ?? "";
		d.deviceVersion = SystemInfo.graphicsDeviceVersion ?? "";
		d.shaderHash = ShaderHash();
		d.scene = scene;
		d.particles = ctx.kernel.ParticleCount;
		d.steps = ctx.stepsTotal;
		d.dtS = ctx.dtS;
		d.step = ctx.step;
		d.nextKf = ctx.nextKf;
		d.unityErrors = ctx.unityErrors;
		d.simTimeS = ctx.simTimeS;
		d.unityFirst = ctx.unityFirst ?? "";
		d.tapeBytes = tapeBytes;
		d.frames = frames;
		d.maxVertsFrame = maxVertsFrame;
		d.maxRawVertsFrame = maxRawVertsFrame;
		d.damageEnabled = SuperDamage.Enabled;
		d.coherentFlyers = SuperEra.CoherentFlyerPlates;
		d.softening = SuperDamage.SofteningCoefficient;
		d.coherentFloorPa = SuperDamage.CoherentTensionFloorPa;
		d.capN = SuperDamage.PressureCapFactorN;
		d.xiScatterOn = SuperDamage.FailureStrainScatterOn;
		d.xiModulus = SuperDamage.FailureStrainScatterModulus;
		d.xiSalt = SuperDamage.FailureStrainScatterSalt;
		d.s43Alpha = SuperDamage.S43Alpha;
		d.s43Beta = SuperDamage.S43Beta;
		d.s43VRefMm3 = SuperDamage.S43VRefMm3;
		d.s43HRefMm = SuperDamage.S43HRefMm;
		d.xiCeiling = 10f;
		d.state = ctx.kernel.DownloadFull();
		List<DriveSpec> drives = new List<DriveSpec>();
		for (SuperEraDrive dr = drive; dr != null; dr = dr.next)
		{
			SuperShellDrive shell = dr as SuperShellDrive;
			DriveSpec spec = default(DriveSpec);
			spec.frontBody = dr.frontBody;
			spec.rearBody = dr.rearBody;
			spec.triggerBody = dr.triggerBody;
			spec.velocityMps = dr.velocityMps;
			spec.initiationDelayS = dr.initiationDelayS;
			spec.rampS = dr.rampS;
			spec.triggerDistanceM = dr.triggerDistanceM;
			spec.driveFront = dr.driveFront;
			spec.driveRear = dr.driveRear;
			spec.state = dr.Snapshot();
			spec.kind = ((shell != null) ? 1 : 0);
			spec.targetPlateBody = ((shell != null) ? shell.targetPlateBody : (-1));
			spec.sabotageOpenBase = shell != null && shell.SabotageOpenBase;
			drives.Add(spec);
		}
		scene.drives = drives.ToArray();
		scene.probeEveryS = SuperEraDrive.ProbeEveryS;
		return d;
	}

	public static SuperEraDrive BuildDrives(Scene sc)
	{
		SuperEraDrive head = null;
		SuperEraDrive tail = null;
		for (int i = 0; i < sc.drives.Length; i++)
		{
			DriveSpec spec = sc.drives[i];
			SuperEraDrive drive;
			if (spec.kind == 1)
			{
				SuperShellDrive shell = new SuperShellDrive();
				shell.targetPlateBody = spec.targetPlateBody;
				shell.SabotageOpenBase = spec.sabotageOpenBase;
				drive = shell;
			}
			else if (spec.kind != 0)
			{
				throw new IOException(string.Format("привод рода {0} этой сборке не известен — точка ", spec.kind) + "снята более новой версией, и продолжать её было бы продолжением другим механизмом");
			}
			else
			{
				drive = new SuperEraDrive();
			}
			drive.frontBody = spec.frontBody;
			drive.rearBody = spec.rearBody;
			drive.triggerBody = spec.triggerBody;
			drive.velocityMps = spec.velocityMps;
			drive.initiationDelayS = spec.initiationDelayS;
			drive.rampS = spec.rampS;
			drive.triggerDistanceM = spec.triggerDistanceM;
			drive.driveFront = spec.driveFront;
			drive.driveRear = spec.driveRear;
			if (head != null)
			{
				tail.next = drive;
			}
			else
			{
				head = drive;
			}
			tail = drive;
		}
		return head;
	}

	public static SuperShot.Resume ToResume(Data d)
	{
		SuperEraDrive.State[] states = new SuperEraDrive.State[d.scene.drives.Length];
		for (int i = 0; i < states.Length; i++)
		{
			states[i] = d.scene.drives[i].state;
		}
		SuperShot.Resume resume = new SuperShot.Resume();
		resume.state = d.state;
		resume.step = d.step;
		resume.nextKf = d.nextKf;
		resume.unityErrors = d.unityErrors;
		resume.unityFirst = (string.IsNullOrEmpty(d.unityFirst) ? null : d.unityFirst);
		resume.drives = states;
		resume.dtS = d.dtS;
		return resume;
	}
}
