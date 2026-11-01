using System;
using System.Collections.Generic;
using UnityEngine;

public static class Flat2DRounds
{
	public struct Entry
	{
		public string id;

		public string catalogueId;

		public string display;

		public string category;

		public Projectile.CoreType core;

		public ContinuumMaterialId mat;

		public bool hasMaterial;

		public float diameterM;

		public float lengthM;

		public float velocityMps;

		public NoseShape nose;

		public bool dimsDerived;

		public bool noseAssumed;

		public bool available;

		public string reason;

		public string tooltip;

		public string nameKey;

		public bool passport;
	}

	public const string IdPrefix = "round:";

	public const int NoseSamples = 16;

	public const string WhyCap = "колпачок — второе тело, силуэтом «Плоскости» не рисуется: вышел бы голый сердечник — соседняя строка под чужим именем";

	private static List<Entry> cache;

	private static Dictionary<string, int> index;

	private static Language builtFor;

	public static IList<Entry> All
	{
		get
		{
			Ensure();
			return cache;
		}
	}

	public static bool IsRoundId(string id)
	{
		return id != null && id.StartsWith(IdPrefix, StringComparison.Ordinal);
	}

	public static bool TryGet(string id, out Entry e)
	{
		e = default;
		if (!IsRoundId(id))
		{
			return false;
		}
		Ensure();
		if (!index.TryGetValue(id, out int i))
		{
			return false;
		}
		e = cache[i];
		return true;
	}

	private static void Ensure()
	{
		if (cache != null && builtFor == Localization.Current)
		{
			return;
		}
		builtFor = Localization.Current;
		List<Entry> list = new List<Entry>();
		Dictionary<string, int> map = new Dictionary<string, int>();
		Collect(list, map);
		index = map;
		cache = list;
	}

	private static void Collect(List<Entry> list, Dictionary<string, int> map)
	{
		List<Flat2DDrawModel.MaterialSlot> slots = Flat2DDrawModel.MaterialSlots();
		foreach (ProjectileCatalogEntry row in Catalog.Projectiles)
		{
			if (Catalog.IsShapedCategory(row.meta.category) || row.make == null)
			{
				continue;
			}
			ProjectileDefinition p = row.make();
			if (p == null || (p.kind != ProjectileClass.LongRod && p.kind != ProjectileClass.ArmorPiercing && p.kind != ProjectileClass.Kinetic))
			{
				continue;
			}
			Entry e = new Entry
			{
				id = IdPrefix + row.meta.id,
				catalogueId = row.meta.id,
				display = row.meta.displayName,
				category = CategoryFor(row.meta.category),
				core = p.coreType,
				diameterM = p.diameterMM * 0.001f,
				lengthM = p.rodLengthMM * 0.001f,
				velocityMps = p.velocityMps,
				nose = p.nose,
				nameKey = row.meta.nameKey
			};
			if (RoundCatalog.TryGet(row.meta.id, out RoundSpec spec))
			{
				e.dimsDerived = spec.geometrySource != RoundGeometrySource.Dimensions || !spec.coreDiameterMM.present || !spec.coreLengthMM.present;
				e.noseAssumed = !spec.nose.present;
			}
			string materialName = SuperShotRunner.MapCore(p.coreType);
			string verdict;
			if (!string.IsNullOrEmpty(materialName) && SuperBaker.TryMaterial(materialName, out ContinuumMaterialId material))
			{
				e.mat = material;
				e.hasMaterial = true;
				verdict = SlotVerdict(slots, true, material, null);
			}
			else
			{
				verdict = SlotVerdict(slots, false, default, OutOfRegistrySlot(p.coreType));
				if (verdict == null)
				{
					verdict = string.Format("нет строки {0} в реестре", p.coreType);
				}
			}
			if (RodPassports.Refusal(p, out string key, out object[] args))
			{
				verdict = Localization.T(key, args);
				e.passport = true;
			}
			else if (p.ballisticCap || p.capLengthFrac > 0f)
			{
				verdict = WhyCap;
			}
			else if (e.diameterM <= 0f || e.lengthM <= 0f)
			{
				verdict = "у пресета нет размеров — силуэт не строится";
			}
			e.reason = verdict;
			e.available = verdict == null;
			e.tooltip = Tooltip(in e);
			map[e.id] = list.Count;
			list.Add(e);
		}
	}

	private static string SlotVerdict(List<Flat2DDrawModel.MaterialSlot> slots, bool inRegistry, ContinuumMaterialId id, string outName)
	{
		foreach (Flat2DDrawModel.MaterialSlot slot in slots)
		{
			bool match;
			if (inRegistry)
			{
				match = slot.inRegistry && slot.id == id;
			}
			else
			{
				match = !slot.inRegistry && outName != null && slot.display == outName;
			}
			if (match)
			{
				return slot.usable ? null : slot.reason;
			}
		}
		return inRegistry ? "вещества нет в пикере «Плоскости»" : null;
	}

	private static string OutOfRegistrySlot(Projectile.CoreType c)
	{
		return c == Projectile.CoreType.WC ? "WC-Co" : null;
	}

	private static string CategoryFor(string c)
	{
		if (c == "Настраиваемые")
		{
			return "НАСТРАИВАЕМЫЕ (СИЛУЭТ)";
		}
		if (c == "СССР/Россия (3BM)")
		{
			return "СССР / РОССИЯ";
		}
		if (c == "США (M829)")
		{
			return "США";
		}
		if (c == "Германия (DM)")
		{
			return "ГЕРМАНИЯ";
		}
		if (c == "Автопушки 25–30 мм")
		{
			return "АВТОПУШКИ 25–30 мм";
		}
		if (string.IsNullOrEmpty(c))
		{
			return "ПРОЧИЕ";
		}
		return c.ToUpperInvariant();
	}

	public static string NoseRu(NoseShape n)
	{
		switch (n)
		{
		case NoseShape.Flat:
			return "плоский торец";
		case NoseShape.Conical:
			return "конус";
		case NoseShape.Ogive:
			return "оживало";
		case NoseShape.BluntOgive:
			return "притуплённое оживало";
		default:
			return n.ToString();
		}
	}

	private static string Tooltip(in Entry e)
	{
		return string.Concat(new string[7]
		{
			Flat2DText.F("Ø {0:F1} мм × {1:F0} мм", e.diameterM * 1000f, e.lengthM * 1000f),
			e.dimsDerived ? Flat2DText.Tr(" (выведено из массы и L/D)") : "",
			Flat2DText.F(", нос {0}", Flat2DText.Tr(NoseRu(e.nose))),
			e.noseAssumed ? Flat2DText.Tr(" (умолчание каталога)") : "",
			Flat2DText.F(", v0 {0:F0} м/с, вещество ", e.velocityMps),
			e.hasMaterial ? Flat2DText.Tr(ContinuumMaterials.DisplayName(e.mat)) : Flat2DText.F("— (строки {0} нет)", e.core),
			Flat2DText.Tr("; силуэт без оперения и поддона")
		});
	}

	public static Vector2[] Silhouette(Entry e)
	{
		if (e.diameterM <= 0f || e.lengthM <= 0f)
		{
			return Array.Empty<Vector2>();
		}
		float radius = e.diameterM * 0.5f;
		float halfLength = e.lengthM * 0.5f;
		float baseY = e.lengthM * -0.5f;
		float noseLength = RodGeometry.NoseLengthM(e.diameterM, e.lengthM);
		List<Vector2> right = new List<Vector2>(NoseSamples + 1);
		for (int i = 0; i <= NoseSamples; i++)
		{
			float x = (float)i * noseLength * 0.0625f;
			float factor = RodGeometry.NoseRadiusFactorAt(e.nose, x, noseLength, radius);
			right.Add(new Vector2(factor * radius, halfLength - x));
			if (factor >= 1f)
			{
				break;
			}
		}
		List<Vector2> outline = new List<Vector2>(right.Count * 2 + 2);
		outline.AddRange(right);
		outline.Add(new Vector2(radius, baseY));
		outline.Add(new Vector2(0f - radius, baseY));
		for (int i = right.Count - 1; i >= 0; i--)
		{
			Vector2 p = right[i];
			outline.Add(new Vector2(0f - p.x, p.y));
		}
		return outline.ToArray();
	}
}
