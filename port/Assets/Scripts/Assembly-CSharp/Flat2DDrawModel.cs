using System;
using System.Collections.Generic;
using UnityEngine;

public class Flat2DDrawModel
{
	[Serializable]
	public class Shape
	{
		public List<Vector2> pts = new List<Vector2>();

		public ContinuumMaterialId material = ContinuumMaterialId.RHA_MeyerKleponis;

		public Flat2DFill.Role role;

		public Vector2 velocity;

		public bool closed;

		public bool on = true;

		public bool locked;

		public string name = "";

		public int kind;

		public List<Vector2> path = new List<Vector2>();

		public float width;

		public int brushShape;

		public bool erase;

		public float angleDeg;

		public Vector2 pivot;

		public int seq;

		public Color32 paint;
	}

	[Serializable]
	private class Dto
	{
		public float dx;

		public List<ShapeDto> shapes = new List<ShapeDto>();

		public bool stabiliser;

		public bool bodiesSeparate;

		public bool erosion;

		public bool fracture;

		public bool restraint;
	}

	[Serializable]
	private class ShapeDto
	{
		public List<Vector2> pts;

		public int material;

		public int role;

		public Vector2 velocity;

		public bool off;

		public bool locked;

		public string name;

		public int kind;

		public List<Vector2> path;

		public float width;

		public int brushShape;

		public bool erase;

		public float angleDeg;

		public Vector2 pivot;

		public int seq;

		public Color32 paint;
	}

	public struct MaterialSlot
	{
		public string display;

		public ContinuumMaterialId id;

		public bool inRegistry;

		public bool usable;

		public string reason;
	}

	public enum Tool
	{
		Polygon = 0,
		Rect = 1,
		Circle = 2,
		Brush = 3,
		Eraser = 4
	}

	public enum Snap
	{
		Grid = 0,
		Angle15 = 1,
		Free = 2
	}

	public struct Decal
	{
		public string id;

		public string display;

		public string hint;
	}

	public struct DecalPart
	{
		public Vector2[] pts;

		public Color32 paint;

		public string name;
	}

	public struct Preset
	{
		public string id;

		public string display;

		public string hint;
	}

	public readonly List<Shape> shapes = new List<Shape>();

	public Shape open;

	public Shape openStroke;

	public float dx = 0.0005f;

	public bool stabiliser;

	public bool bodiesSeparate;

	public bool erosion;

	public bool fracture;

	public bool restraint;

	private int seqCounter;

	private int cachedCountVersion = -1;

	private int cachedCount;

	private int previewFillVersion = -1;

	private float previewFillDx;

	private Flat2DFill.Result previewFill;

	private readonly List<string> undoStack = new List<string>();

	private readonly List<string> redoStack = new List<string>();

	private const int UndoDepth = 64;

	public int Version { get; private set; }

	public static Decal[] Decals => new Decal[7]
	{
		new Decal
		{
			id = "plate",
			display = "ПЛИТА",
			hint = "прямоугольник t×h"
		},
		new Decal
		{
			id = "wedge",
			display = "КЛИН",
			hint = "треугольная носовая часть"
		},
		new Decal
		{
			id = "spaced",
			display = "РАЗНЕСЁННАЯ",
			hint = "две плиты с зазором"
		},
		new Decal
		{
			id = "corner",
			display = "УГОЛОК",
			hint = "две полки под 90°"
		},
		new Decal
		{
			id = "box",
			display = "КОРОБ",
			hint = "четыре стенки, полость внутри"
		},
		new Decal
		{
			id = "crew_sit",
			display = "МАНЕКЕН СИДЯ",
			hint = "экипаж в разрезе, органы раскрашены"
		},
		new Decal
		{
			id = "crew_stand",
			display = "МАНЕКЕН СТОЯ",
			hint = "экипаж в разрезе, органы раскрашены"
		}
	};

	public static Preset[] Presets => new Preset[3]
	{
		new Preset
		{
			id = "plate_bops",
			display = "ПЛИТА 100 мм + БОПС",
			hint = "RHA 100×120 мм, стержень WHA 8×60 мм, 1500 м/с, якорь по низу"
		},
		new Preset
		{
			id = "spaced",
			display = "РАЗНЕСЁННАЯ БРОНЯ",
			hint = "две плиты RHA 40 мм с зазором 60 мм, тот же стержень"
		},
		new Preset
		{
			id = "taylor",
			display = "ТЕЙЛОР",
			hint = "медный образец в жёсткую стену — поверка решателя, не сцена игрока"
		}
	};

	public int UndoDepthNow => undoStack.Count;

	public int RedoDepthNow => redoStack.Count;

	private void Bump()
	{
		Version++;
	}

	public void SetDx(float newDx)
	{
		Bump();
		dx = newDx;
	}

	public void BeginShape(ContinuumMaterialId mat, Flat2DFill.Role role)
	{
		open = new Shape
		{
			material = mat,
			role = role
		};
		Bump();
	}

	public string AddVertex(Vector2 p)
	{
		if (open == null)
		{
			open = new Shape();
			open.material = ContinuumMaterialId.RHA_MeyerKleponis;
			Bump();
		}
		open.pts.Add(p);
		Bump();
		if (open.pts.Count < 3)
		{
			return null;
		}
		return CeilingWarning();
	}

	public bool CloseShape()
	{
		if (open == null || open.pts.Count < 3)
		{
			return false;
		}
		open.closed = true;
		seqCounter++;
		open.seq = seqCounter;
		open.pivot = CentreOf(open);
		shapes.Insert(0, open);
		open = null;
		Bump();
		return true;
	}

	public void BeginStroke(ContinuumMaterialId mat, Flat2DFill.Role role, Vector2 at, float width, int brushShape, bool erase, Vector2 velocity = default(Vector2))
	{
		Shape shape = new Shape();
		shape.kind = 1;
		shape.material = mat;
		shape.role = (erase ? Flat2DFill.Role.Armour : role);
		shape.velocity = velocity;
		shape.brushShape = brushShape;
		shape.erase = erase;
		shape.closed = true;
		if (width < 1e-06f)
		{
			width = 1e-06f;
		}
		shape.width = width;
		openStroke = shape;
		openStroke.path.Add(at);
		Bump();
	}

	public string ExtendStroke(Vector2 p)
	{
		if (openStroke == null)
		{
			return null;
		}
		Vector2 vector = openStroke.path[openStroke.path.Count - 1];
		float num = Mathf.Max(openStroke.width * 0.25f, dx * 0.5f);
		if ((p - vector).magnitude < num)
		{
			return null;
		}
		openStroke.path.Add(p);
		Bump();
		return CeilingWarning();
	}

	public Shape EndStroke()
	{
		if (openStroke == null)
		{
			return null;
		}
		Shape shape = openStroke;
		openStroke = null;
		if (shape.path.Count == 0)
		{
			Bump();
			return null;
		}
		Checkpoint();
		seqCounter++;
		shape.seq = seqCounter;
		shape.pivot = CentreOf(shape);
		shapes.Insert(0, shape);
		Bump();
		return shape;
	}

	public void CancelStroke()
	{
		if (openStroke != null)
		{
			openStroke = null;
			Bump();
		}
	}

	public void Undo()
	{
		if (open != null && open.pts.Count > 0)
		{
			open.pts.RemoveAt(open.pts.Count - 1);
			if (open.pts.Count == 0)
			{
				open = null;
				Bump();
			}
		}
	}

	public void DeleteShape(int index)
	{
		if (index >= 0 && index < shapes.Count)
		{
			shapes.RemoveAt(index);
			Bump();
		}
	}

	public void Clear()
	{
		shapes.Clear();
		open = null;
		openStroke = null;
		Bump();
	}

	public static Vector2 Rotate(Vector2 p, Vector2 pivot, float angleDeg)
	{
		if (angleDeg == 0f)
		{
			return p;
		}
		float f = angleDeg * 0.0174532924f;
		float num = Mathf.Cos(f);
		float num2 = Mathf.Sin(f);
		Vector2 vector = p - pivot;
		return new Vector2(vector.x * num - vector.y * num2 + pivot.x, vector.x * num2 + vector.y * num + pivot.y);
	}

	public static Vector2[] EffectivePts(Shape s)
	{
		Vector2[] array = new Vector2[s.pts.Count];
		if (s.angleDeg != 0f)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Rotate(s.pts[i], s.pivot, s.angleDeg);
			}
		}
		else
		{
			s.pts.CopyTo(array);
		}
		return array;
	}

	public static Vector2[] EffectivePath(Shape s)
	{
		Vector2[] array = new Vector2[s.path.Count];
		if (s.angleDeg != 0f)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = Rotate(s.path[i], s.pivot, s.angleDeg);
			}
		}
		else
		{
			s.path.CopyTo(array);
		}
		return array;
	}

	public static Vector2 CentreOf(Shape s)
	{
		List<Vector2> list = (s.kind == 1) ? s.path : s.pts;
		if (list.Count == 0)
		{
			return Vector2.zero;
		}
		Vector2 zero = Vector2.zero;
		foreach (Vector2 item in list)
		{
			zero += item;
		}
		return new Vector2(zero.x / list.Count, zero.y / list.Count);
	}

	public bool RotateShape(int i, float deltaDeg, bool snap15)
	{
		if (i < 0 || i >= shapes.Count || shapes[i].locked)
		{
			return false;
		}
		Shape shape = shapes[i];
		shape.angleDeg += deltaDeg;
		if (snap15)
		{
			shape.angleDeg = Mathf.Round(shape.angleDeg / 15f) * 15f;
		}
		float num = shape.angleDeg % 360f;
		if (num > 180f)
		{
			num -= 360f;
		}
		else if (num <= -180f)
		{
			num += 360f;
		}
		shape.angleDeg = num;
		Bump();
		return true;
	}

	private static Flat2DFill.Poly ToPoly(Shape s)
	{
		Flat2DFill.Poly result = default(Flat2DFill.Poly);
		if (s.kind == 1)
		{
			result.path = EffectivePath(s);
			result.halfWidth = s.width * 0.5f;
			result.square = s.brushShape == 1;
			result.erase = s.erase;
			result.material = (uint)s.material;
			result.role = s.role;
		}
		else
		{
			result.pts = EffectivePts(s);
			result.material = (uint)s.material;
			result.role = s.role;
			result.erase = s.erase;
		}
		return result;
	}

	public List<Flat2DFill.Poly> Polys(bool includeOpenProvisional)
	{
		return Polys(includeOpenProvisional, null, null);
	}

	private List<Flat2DFill.Poly> Polys(bool includeOpenProvisional, List<Shape> owners, List<Color32> paints = null)
	{
		List<Flat2DFill.Poly> list = new List<Flat2DFill.Poly>();
		foreach (Shape s in shapes)
		{
			if (!s.on)
			{
				continue;
			}
			if (s.kind == 1)
			{
				if (s.path.Count < 1)
				{
					continue;
				}
			}
			else if (s.pts.Count < 3)
			{
				continue;
			}
			Flat2DFill.Poly item = ToPoly(s);
			list.Add(item);
			if (owners != null)
			{
				owners.Add(s);
			}
			if (paints != null)
			{
				paints.Add(s.paint);
			}
		}
		if (includeOpenProvisional)
		{
			if (open != null && open.pts.Count >= 3)
			{
				list.Insert(0, new Flat2DFill.Poly
				{
					pts = open.pts.ToArray(),
					material = (uint)open.material,
					role = open.role
				});
				if (owners != null)
				{
					owners.Insert(0, open);
				}
				if (paints != null)
				{
					paints.Insert(0, open.paint);
				}
			}
			if (openStroke != null && openStroke.path.Count >= 1)
			{
				list.Insert(0, ToPoly(openStroke));
				if (owners != null)
				{
					owners.Insert(0, openStroke);
				}
				if (paints != null)
				{
					paints.Insert(0, openStroke.paint);
				}
			}
		}
		return list;
	}

	public int LiveCount()
	{
		if (cachedCountVersion != Version)
		{
			if (previewFillVersion != Version || previewFillDx != dx)
			{
				previewFill = BuildFill(true);
				previewFillVersion = Version;
				previewFillDx = dx;
			}
			cachedCountVersion = Version;
			cachedCount = previewFill.count;
		}
		return cachedCount;
	}

	public Flat2DFill.Result PreviewFill()
	{
		if (previewFillVersion != Version || previewFillDx != dx)
		{
			previewFill = BuildFill(true);
			previewFillVersion = Version;
			previewFillDx = dx;
		}
		return previewFill;
	}

	public int LastProjectileIndex()
	{
		int result = -1;
		for (int i = 0; i < shapes.Count; i++)
		{
			if (shapes[i].role == Flat2DFill.Role.Projectile && !shapes[i].erase)
			{
				if (result >= 0 && shapes[i].seq < shapes[result].seq)
				{
					continue;
				}
				result = i;
			}
		}
		return result;
	}

	public Vector2? LastProjectileVelocity()
	{
		int num = LastProjectileIndex();
		if (num < 0)
		{
			return null;
		}
		return shapes[num].velocity;
	}

	public string CeilingWarning()
	{
		int count = LiveCount();
		if (count > Flat2DFill.Ceiling)
		{
			return Flat2DText.F("ВНИМАНИЕ: не менее {0} частиц ПРИ ПОТОЛКЕ {1} (счёт остановлен) — фигура будет ОТКАЗАНА; уменьшите её или укрупните Δx", count, Flat2DFill.Ceiling);
		}
		return null;
	}

	public ContinuumMaterialId[] FireMaterialIds()
	{
		SortedSet<int> sortedSet = new SortedSet<int>();
		foreach (Shape shape in shapes)
		{
			if (!shape.on || shape.role == Flat2DFill.Role.Anchor || shape.erase)
			{
				continue;
			}
			sortedSet.Add((int)shape.material);
		}
		if (sortedSet.Count == 0)
		{
			sortedSet.Add((int)ContinuumMaterialId.RHA_MeyerKleponis);
		}
		List<ContinuumMaterialId> list = new List<ContinuumMaterialId>();
		foreach (int item in sortedSet)
		{
			list.Add((ContinuumMaterialId)item);
		}
		return list.ToArray();
	}

	public Flat2DFill.Result BuildFireFill(out Flat2DMaterialBlock[] mats)
	{
		Flat2DFill.Result result = BuildFill(true);
		ContinuumMaterialId[] array = FireMaterialIds();
		Dictionary<uint, uint> dictionary = new Dictionary<uint, uint>();
		for (int i = 0; i < array.Length; i++)
		{
			dictionary[(uint)array[i]] = (uint)i;
		}
		mats = Flat2DMaterials.Build(array);
		if (result.ok && result.count > 0)
		{
			for (int j = 0; j < result.count; j++)
			{
				result.mat[j] = dictionary[result.mat[j]];
			}
		}
		return result;
	}

	public Flat2DFill.Result BuildFill()
	{
		return BuildFill(false);
	}

	public Flat2DFill.Result BuildFill(bool includeOpenProvisional)
	{
		List<Shape> owners = new List<Shape>();
		List<Color32> paints = new List<Color32>();
		List<Flat2DFill.Poly> polys = Polys(includeOpenProvisional, owners, paints);
		Flat2DFill.Result result = Flat2DFill.Build(polys, dx);
		if (result.ok)
		{
			bool flag = false;
			foreach (Color32 paint in paints)
			{
				if (paint.a != 0)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				result.paint = new uint[result.count];
				for (int i = 0; i < result.count; i++)
				{
					result.paint[i] = Pack(paints[result.ownerPoly[i]]);
				}
			}
			result.ownerRole = new byte[result.count];
			for (int j = 0; j < result.count; j++)
			{
				Shape shape = owners[result.ownerPoly[j]];
				if (shape != null)
				{
					if (shape.role == Flat2DFill.Role.Projectile)
					{
						result.vel[j] = shape.velocity;
					}
					result.ownerRole[j] = (byte)((shape.role == Flat2DFill.Role.Projectile) ? 1 : 0);
				}
				else
				{
					result.ownerRole[j] = byte.MaxValue;
				}
			}
			if (!includeOpenProvisional)
			{
				UnifyProjectileVelocity(result, owners);
			}
		}
		return result;
	}

	private void UnifyProjectileVelocity(Flat2DFill.Result r, List<Shape> owners)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < r.count; i++)
		{
			if (r.ownerRole[i] == 1)
			{
				list.Add(i);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Vector2[] array = new Vector2[list.Count];
		for (int j = 0; j < list.Count; j++)
		{
			array[j] = r.pos[list[j]];
		}
		int num = Flat2DBodies.Label(array, dx * 1.3f * 2.0, out int[] rank, null);
		Dictionary<Shape, int> dictionary = new Dictionary<Shape, int>();
		for (int k = 0; k < shapes.Count; k++)
		{
			dictionary[shapes[k]] = k;
		}
		Shape[] array2 = new Shape[num];
		int[] array3 = new int[num];
		for (int l = 0; l < list.Count; l++)
		{
			Shape shape = owners[r.ownerPoly[list[l]]];
			int num2 = dictionary[shape];
			int num3 = rank[l];
			if (array2[num3] == null || shape.seq > array2[num3].seq || (shape.seq == array2[num3].seq && array3[num3] > num2))
			{
				array2[num3] = shape;
				array3[num3] = num2;
			}
		}
		for (int m = 0; m < list.Count; m++)
		{
			r.vel[list[m]] = array2[rank[m]].velocity;
		}
	}

	public static string EstimateShotTime(int plannedSteps, int probeSteps, double probeSeconds, out double seconds)
	{
		int num = Math.Max(probeSteps, 1);
		seconds = probeSeconds / (double)num * (double)plannedSteps;
		return Flat2DText.F("≈{0:F1} с = {1} шагов × ({2:F1} мс / {3} зондовых шагов)", seconds, plannedSteps, probeSeconds * 1000.0, probeSteps);
	}

	public static List<MaterialSlot> MaterialSlots()
	{
		List<MaterialSlot> list = new List<MaterialSlot>();
		foreach (ContinuumMaterialId id in Enum.GetValues(typeof(ContinuumMaterialId)))
		{
			ContinuumProperties continuumProperties = ContinuumMaterials.Get(id);
			string reason = null;
			if (continuumProperties.JC_A <= 0f)
			{
				reason = "нет модели прочности: блок Джонсона–Кука пуст (жидкость с УС) — твёрдым телом не считается";
			}
			else if (continuumProperties.C0 <= 0f || continuumProperties.S <= 0f)
			{
				reason = "нет ударной адиабаты (C0/S) — давление не считается";
			}
			else if (id == ContinuumMaterialId.OFHC_Copper)
			{
				reason = "только для тестов (T3b, струя S6): в песочнице не выбирается — запись реестра";
			}
			else if (id == ContinuumMaterialId.DepletedUranium)
			{
				reason = "строка в реестре есть (S31, для Super), в «Плоскости» приёмки не было; блока разрушения D1–D5 у урана нет";
			}
			else if (id == ContinuumMaterialId.CastArmourSteel)
			{
				reason = "строка в реестре есть, приёмка не пройдена — исход немонотонен по диапазону деформации разрушения";
			}
			list.Add(new MaterialSlot
			{
				display = ContinuumMaterials.DisplayName(id),
				id = id,
				inRegistry = true,
				usable = reason == null,
				reason = reason
			});
		}
		Ext("IRHA", "нет параметров разрушения и скоростной чувствительности (research_13)");
		Ext("Эластомеры", "нет блока Джонсона–Кука вовсе (research_13)");
		Ext("WC-Co", "требуется модель JH-2 (research_13)");
		Ext("Композиты", "требуется ортотропная модель (research_13)");
		return list;

		void Ext(string name, string why)
		{
			list.Add(new MaterialSlot
			{
				display = name,
				id = ContinuumMaterialId.WHA,
				inRegistry = false,
				usable = false,
				reason = why
			});
		}
	}

	public Shape AddShape(IList<Vector2> pts, ContinuumMaterialId mat, Flat2DFill.Role role, Vector2 vel, string name, Color32 paint = default(Color32))
	{
		if (pts == null || pts.Count < 3)
		{
			return null;
		}
		Shape shape = new Shape();
		shape.pts = new List<Vector2>(pts);
		shape.velocity = vel;
		shape.material = mat;
		shape.role = role;
		shape.closed = true;
		shape.name = name ?? "";
		seqCounter++;
		shape.seq = seqCounter;
		shape.paint = paint;
		shape.pivot = CentreOf(shape);
		shapes.Insert(0, shape);
		Bump();
		return shape;
	}

	public bool SetShapeMaterial(int i, ContinuumMaterialId m)
	{
		if (i < 0 || i >= shapes.Count || shapes[i].locked)
		{
			return false;
		}
		shapes[i].material = m;
		Bump();
		return true;
	}

	public bool SetShapeRole(int i, Flat2DFill.Role r)
	{
		if (i < 0 || i >= shapes.Count || shapes[i].locked)
		{
			return false;
		}
		shapes[i].role = r;
		Bump();
		return true;
	}

	public bool ToggleShape(int i)
	{
		if (i < 0 || i >= shapes.Count)
		{
			return false;
		}
		shapes[i].on = !shapes[i].on;
		Bump();
		return true;
	}

	public bool ToggleLock(int i)
	{
		if (i < 0 || i >= shapes.Count)
		{
			return false;
		}
		shapes[i].locked = !shapes[i].locked;
		return true;
	}

	public int CountShape(int i)
	{
		if (i < 0 || i >= shapes.Count)
		{
			return 0;
		}
		Shape shape = shapes[i];
		if (shape.kind == 1)
		{
			if (shape.path.Count < 1)
			{
				return 0;
			}
		}
		else if (shape.pts.Count < 3)
		{
			return 0;
		}
		List<Flat2DFill.Poly> list = new List<Flat2DFill.Poly>();
		list.Add(ToPoly(shape));
		return Flat2DFill.CountOnly(list, dx, Flat2DFill.Ceiling);
	}

	public static bool ShapeCovers(Shape s, Vector2 p)
	{
		if (s.kind == 1)
		{
			if (s.path.Count < 1)
			{
				return false;
			}
		}
		else if (s.pts.Count < 3)
		{
			return false;
		}
		return Flat2DFill.Covers(ToPoly(s), p);
	}

	public static string RoleName(Flat2DFill.Role r)
	{
		string ru = (r != 0) ? ((r == Flat2DFill.Role.Projectile) ? "снаряд" : "якорь") : "броня";
		return Flat2DText.Tr(ru);
	}

	public static Vector2[] RectPts(Vector2 a, Vector2 b)
	{
		return new Vector2[4]
		{
			new Vector2(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y)),
			new Vector2(Mathf.Max(a.x, b.x), Mathf.Min(a.y, b.y)),
			new Vector2(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y)),
			new Vector2(Mathf.Min(a.x, b.x), Mathf.Max(a.y, b.y))
		};
	}

	public static Vector2[] CirclePts(Vector2 c, float r, int seg = 24)
	{
		seg = Mathf.Clamp(seg, 8, 128);
		Vector2[] array = new Vector2[seg];
		for (int i = 0; i < seg; i++)
		{
			float f = (float)i * (Mathf.PI * 2f) / (float)seg;
			array[i] = new Vector2(c.x + Mathf.Cos(f) * r, c.y + Mathf.Sin(f) * r);
		}
		return array;
	}

	public static Vector2[] StrokePts(Vector2 a, Vector2 b, float width)
	{
		Vector2 vector = b - a;
		if (vector.sqrMagnitude < 1e-12f)
		{
			vector = Vector2.right;
		}
		Vector2 vector2 = Vector2.Normalize(new Vector2(0f - vector.y, vector.x));
		if (width < 1e-06f)
		{
			width = 1e-06f;
		}
		float num = width * 0.5f;
		return new Vector2[4]
		{
			new Vector2(a.x + vector2.x * num, a.y + vector2.y * num),
			new Vector2(b.x + vector2.x * num, b.y + vector2.y * num),
			new Vector2(b.x - vector2.x * num, b.y - vector2.y * num),
			new Vector2(a.x - vector2.x * num, a.y - vector2.y * num)
		};
	}

	public static Vector2[] MirrorX(IList<Vector2> pts, float axis)
	{
		Vector2[] array = new Vector2[pts.Count];
		for (int i = 0; i < pts.Count; i++)
		{
			array[i] = new Vector2(axis * 2f - pts[i].x, pts[i].y);
		}
		return array;
	}

	public static Vector2 SnapPoint(Vector2 p, Snap mode, float dx, Vector2? prev)
	{
		if (mode == Snap.Grid)
		{
			int num = Mathf.RoundToInt(p.x / dx - 0.5f);
			int num2 = Mathf.RoundToInt(p.y / dx - 0.5f);
			return new Vector2((num + 0.5f) * dx, (num2 + 0.5f) * dx);
		}
		if (mode == Snap.Angle15 && prev.HasValue && (p - prev.Value).sqrMagnitude >= 1e-14f)
		{
			Vector2 value = prev.Value;
			Vector2 vector = p - value;
			float num3 = Mathf.Round(Mathf.Atan2(vector.y, vector.x) / (Mathf.PI / 12f)) * (Mathf.PI / 12f);
			float magnitude = vector.magnitude;
			return new Vector2(value.x + Mathf.Cos(num3) * magnitude, value.y + Mathf.Sin(num3) * magnitude);
		}
		return p;
	}

	public static List<Vector2[]> DecalPolys(string id, Vector2 at, float t, float h)
	{
		List<Vector2[]> list = new List<Vector2[]>();
		switch (id)
		{
		case "plate":
			list.Add(RectPts(at, new Vector2(at.x + t, at.y + h)));
			break;
		case "wedge":
			list.Add(new Vector2[3]
			{
				at,
				new Vector2(at.x + t, at.y + h * 0.5f),
				new Vector2(at.x, at.y + h)
			});
			break;
		case "spaced":
			list.Add(RectPts(at, new Vector2(at.x + t, at.y + h)));
			list.Add(RectPts(new Vector2(at.x + 2f * t, at.y), new Vector2(at.x + 3f * t, at.y + h)));
			break;
		case "corner":
			list.Add(RectPts(at, new Vector2(at.x + t, at.y + h)));
			list.Add(RectPts(new Vector2(at.x + t, at.y), new Vector2(at.x + h, at.y + t)));
			break;
		case "box":
			list.Add(RectPts(at, new Vector2(at.x + h, at.y + t)));
			list.Add(RectPts(new Vector2(at.x, at.y + h - t), new Vector2(at.x + h, at.y + h)));
			list.Add(RectPts(new Vector2(at.x, at.y + t), new Vector2(at.x + t, at.y + h - t)));
			list.Add(RectPts(new Vector2(at.x + h - t, at.y + t), new Vector2(at.x + h, at.y + h - t)));
			break;
		case "crew_sit":
		case "crew_stand":
			foreach (DecalPart part in Flat2DCrew.Parts(id, at))
			{
				list.Add(part.pts);
			}
			break;
		default:
			if (Flat2DRounds.TryGet(id, out Flat2DRounds.Entry e))
			{
				Vector2[] silhouette = Flat2DRounds.Silhouette(e);
				if (silhouette.Length >= 3)
				{
					float s = Flat2DModelScale.S;
					for (int i = 0; i < silhouette.Length; i++)
					{
						silhouette[i] = new Vector2(silhouette[i].x * s, silhouette[i].y * s);
					}
					float num = float.MaxValue;
					float num2 = float.MaxValue;
					for (int j = 0; j < silhouette.Length; j++)
					{
						num = Mathf.Min(num, silhouette[j].x);
						num2 = Mathf.Min(num2, silhouette[j].y);
					}
					for (int k = 0; k < silhouette.Length; k++)
					{
						silhouette[k] = new Vector2(at.x - num + silhouette[k].x, at.y - num2 + silhouette[k].y);
					}
					list.Add(silhouette);
				}
			}
			break;
		}
		return list;
	}

	public static List<Vector2[]> DecalPolysCentred(string id, Vector2 centre, float t, float h)
	{
		List<DecalPart> list = DecalPartsCentred(id, centre, t, h);
		List<Vector2[]> list2 = new List<Vector2[]>(list.Count);
		foreach (DecalPart part in list)
		{
			list2.Add(part.pts);
		}
		return list2;
	}

	public static List<DecalPart> DecalParts(string id, Vector2 at, float t, float h)
	{
		if (Flat2DCrew.IsCrewDecal(id))
		{
			return Flat2DCrew.Parts(id, at);
		}
		List<DecalPart> list = new List<DecalPart>();
		foreach (Vector2[] item in DecalPolys(id, at, t, h))
		{
			list.Add(new DecalPart
			{
				pts = item
			});
		}
		return list;
	}

	public static List<DecalPart> DecalPartsCentred(string id, Vector2 centre, float t, float h)
	{
		List<DecalPart> list = DecalParts(id, Vector2.zero, t, h);
		if (list.Count == 0)
		{
			return list;
		}
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = 0f - float.MaxValue;
		float num4 = 0f - float.MaxValue;
		foreach (DecalPart part in list)
		{
			for (int i = 0; i < part.pts.Length; i++)
			{
				num = Mathf.Min(num, part.pts[i].x);
				num2 = Mathf.Min(num2, part.pts[i].y);
				num3 = Mathf.Max(num3, part.pts[i].x);
				num4 = Mathf.Max(num4, part.pts[i].y);
			}
		}
		float x = centre.x - (num + num3) * 0.5f;
		float y = centre.y - (num2 + num4) * 0.5f;
		foreach (DecalPart part2 in list)
		{
			for (int j = 0; j < part2.pts.Length; j++)
			{
				part2.pts[j] = new Vector2(x + part2.pts[j].x, y + part2.pts[j].y);
			}
		}
		return list;
	}

	public static uint Pack(Color32 c)
	{
		return (uint)((c.a << 24) | (c.b << 16) | (c.g << 8) | c.r);
	}

	public int PlaceDecal(string id, Vector2 centre, float t, float h, float angleDeg, ContinuumMaterialId mat, Flat2DFill.Role role, Vector2 vel, string name, Color32 paint = default(Color32))
	{
		List<DecalPart> list = DecalPartsCentred(id, centre, t, h);
		if (list.Count == 0)
		{
			return 0;
		}
		if (Flat2DCrew.IsCrewDecal(id))
		{
			mat = ContinuumMaterialId.CrewTissue;
			role = Flat2DFill.Role.Armour;
			vel = Vector2.zero;
		}
		Checkpoint();
		int num = seqCounter + 1;
		foreach (DecalPart part in list)
		{
			Shape shape = AddShape(part.pts, mat, role, vel, (part.name != null) ? (name + ": " + part.name) : name, (part.paint.a != 0) ? part.paint : paint);
			if (shape != null)
			{
				shape.angleDeg = angleDeg;
				shape.pivot = centre;
				shape.seq = num;
			}
		}
		seqCounter = num;
		Bump();
		return list.Count;
	}

	public int SetLastProjectileVelocity(Vector2 v)
	{
		int num = LastProjectileIndex();
		if (num < 0)
		{
			return 0;
		}
		int seq = shapes[num].seq;
		int num2 = 0;
		foreach (Shape shape in shapes)
		{
			if (shape.role == Flat2DFill.Role.Projectile && !shape.erase && shape.seq == seq)
			{
				shape.velocity = v;
				num2++;
			}
		}
		return num2;
	}

	public void LoadPreset(string id)
	{
		Clear();
		if (id == "plate_bops")
		{
			AddShape(RectPts(new Vector2(0.04f, 0f), new Vector2(0.14f, 0.12f)), ContinuumMaterialId.RHA_Grey, Flat2DFill.Role.Armour, Vector2.zero, "плита RHA 100 мм");
			AddShape(RectPts(new Vector2(-0.06f, 0.056f), new Vector2(0f, 0.064f)), ContinuumMaterialId.WHA, Flat2DFill.Role.Projectile, new Vector2(1500f, 0f), "стержень WHA");
			AddShape(RectPts(new Vector2(0.04f, 0f), new Vector2(0.14f, 0.004f)), ContinuumMaterialId.RHA_Grey, Flat2DFill.Role.Anchor, Vector2.zero, "якорь: опора плиты");
		}
		else if (id == "spaced")
		{
			AddShape(RectPts(new Vector2(0.04f, 0f), new Vector2(0.08f, 0.12f)), ContinuumMaterialId.RHA_Grey, Flat2DFill.Role.Armour, Vector2.zero, "экран RHA 40 мм");
			AddShape(RectPts(new Vector2(0.14f, 0f), new Vector2(0.18f, 0.12f)), ContinuumMaterialId.RHA_Grey, Flat2DFill.Role.Armour, Vector2.zero, "основная RHA 40 мм");
			AddShape(RectPts(new Vector2(-0.06f, 0.056f), new Vector2(0f, 0.064f)), ContinuumMaterialId.WHA, Flat2DFill.Role.Projectile, new Vector2(1500f, 0f), "стержень WHA");
			AddShape(RectPts(new Vector2(0.14f, 0f), new Vector2(0.18f, 0.004f)), ContinuumMaterialId.RHA_Grey, Flat2DFill.Role.Anchor, Vector2.zero, "якорь: опора основной");
		}
		else if (id == "taylor")
		{
			AddShape(RectPts(new Vector2(-0.0384f, 0f), new Vector2(0f, 0.0038f)), ContinuumMaterialId.OFHC_Copper, Flat2DFill.Role.Projectile, new Vector2(190f, 0f), "образец Тейлора (медь)");
			AddShape(RectPts(new Vector2(0f, -0.01f), new Vector2(0.008f, 0.014f)), ContinuumMaterialId.Steel4340, Flat2DFill.Role.Armour, Vector2.zero, "жёсткая стена 4340");
			AddShape(RectPts(new Vector2(0.004f, -0.01f), new Vector2(0.008f, 0.014f)), ContinuumMaterialId.Steel4340, Flat2DFill.Role.Anchor, Vector2.zero, "якорь: тыл стены");
		}
	}

	public void Checkpoint()
	{
		string item = ToPayload();
		undoStack.Add(item);
		if (undoStack.Count > 64)
		{
			undoStack.RemoveAt(0);
		}
		redoStack.Clear();
	}

	public bool UndoStep()
	{
		if (undoStack.Count == 0)
		{
			return false;
		}
		redoStack.Add(ToPayload());
		string payload = undoStack[undoStack.Count - 1];
		undoStack.RemoveAt(undoStack.Count - 1);
		FromPayload(payload, keepSettings: true);
		return true;
	}

	public bool RedoStep()
	{
		if (redoStack.Count == 0)
		{
			return false;
		}
		undoStack.Add(ToPayload());
		string payload = redoStack[redoStack.Count - 1];
		redoStack.RemoveAt(redoStack.Count - 1);
		FromPayload(payload, keepSettings: true);
		return true;
	}

	public string ToPayload()
	{
		Dto dto = new Dto();
		dto.shapes = new List<ShapeDto>();
		dto.dx = dx;
		dto.stabiliser = stabiliser;
		dto.bodiesSeparate = bodiesSeparate;
		dto.erosion = erosion;
		dto.fracture = fracture;
		dto.restraint = restraint;
		foreach (Shape shape in shapes)
		{
			dto.shapes.Add(new ShapeDto
			{
				pts = shape.pts,
				material = (int)shape.material,
				role = (int)shape.role,
				velocity = shape.velocity,
				off = !shape.on,
				locked = shape.locked,
				name = shape.name,
				kind = shape.kind,
				path = shape.path,
				width = shape.width,
				brushShape = shape.brushShape,
				erase = shape.erase,
				angleDeg = shape.angleDeg,
				pivot = shape.pivot,
				seq = shape.seq,
				paint = shape.paint
			});
		}
		return JsonUtility.ToJson(dto);
	}

	public void FromPayload(string payload)
	{
		FromPayload(payload, keepSettings: false);
	}

	public void FromPayload(string payload, bool keepSettings)
	{
		Clear();
		Dto dto = JsonUtility.FromJson<Dto>(payload);
		dx = dto.dx;
		if (!keepSettings)
		{
			stabiliser = dto.stabiliser;
			bodiesSeparate = dto.bodiesSeparate;
			erosion = dto.erosion;
			fracture = dto.fracture;
			restraint = dto.restraint;
		}
		seqCounter = 0;
		foreach (ShapeDto shapeDto in dto.shapes)
		{
			Shape shape = new Shape();
			shape.pts = shapeDto.pts;
			shape.material = (ContinuumMaterialId)shapeDto.material;
			shape.role = (Flat2DFill.Role)shapeDto.role;
			shape.velocity = shapeDto.velocity;
			shape.closed = true;
			shape.on = !shapeDto.off;
			shape.locked = shapeDto.locked;
			shape.name = shapeDto.name ?? "";
			shape.kind = shapeDto.kind;
			shape.path = shapeDto.path ?? new List<Vector2>();
			shape.width = shapeDto.width;
			shape.brushShape = shapeDto.brushShape;
			shape.erase = shapeDto.erase;
			shape.angleDeg = shapeDto.angleDeg;
			shape.pivot = shapeDto.pivot;
			shape.seq = shapeDto.seq;
			shape.paint = shapeDto.paint;
			if (shape.angleDeg == 0f)
			{
				shape.pivot = CentreOf(shape);
			}
			shapes.Add(shape);
			seqCounter = Math.Max(seqCounter, shapeDto.seq);
		}
	}

	public SaveEntry Save(string displayName)
	{
		return SaveSystem.Create(displayName, ToPayload(), shapes.Count, TargetSection.Hull, SimFamily.Flat2D);
	}

	public bool Load(string id)
	{
		foreach (SaveEntry item in SaveSystem.LoadUserSaves())
		{
			if (item.id == id)
			{
				FromPayload(item.payload);
				return true;
			}
		}
		return false;
	}
}
