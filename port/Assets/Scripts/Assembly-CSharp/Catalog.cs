using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class Catalog
{
	public const string Kinetic = "Настраиваемые";

	public const string Shaped = "Кумулятивные";

	public const string HeavyAlloys = "Тяжёлые сплавы";

	public const string SteelsAndCarbides = "Стали и карбиды";

	public const string Steels = "Стали";

	public const string LightAlloys = "Лёгкие сплавы";

	public const string Ceramics = "Керамика";

	public const string Composites = "Композиты";

	public const string HeavyMetals = "Тяжёлые металлы";

	public const string Elastomers = "Эластомеры";

	public const string Reactive = "Активная защита";

	public const string NonExplosive = "Выпучивающаяся броня";

	public const string BehindArmour = "Заброневое";

	public const string Plates = "Преграды";

	public const string Screens = "Экраны";

	public const string Modules = "Модули";

	private static readonly ProjectileCatalogEntry[] parametric;

	private static readonly ProjectileCatalogEntry[] projectiles;

	private static readonly CoreCatalogEntry[] cores;

	private static readonly MaterialCatalogEntry[] materials;

	private static readonly HashSet<ArmorPlate.ArmorType> deprecatedMaterials;

	private static readonly HashSet<ArmorPlate.ArmorType> superOnlyMaterials;

	private static readonly HashSet<ProjectileClass> superOnlyClasses;

	private static readonly ObjectCatalogEntry[] singleKinds;

	private static readonly ObjectCatalogEntry[] objects;

	private static readonly HashSet<ArmorPlate.ObjectKind> multiPresetKinds;

	private static readonly HashSet<ArmorPlate.ObjectKind> deprecatedKinds;

	private static readonly HashSet<ArmorPlate.ObjectKind> panelOnlyKinds;

	public static int ParametricCount => parametric.Length;

	public static int ProjectileRowCount => parametric.Length + RoundCatalog.All.Count + RodPassports.Refused.Count + WarheadCatalog.All.Count + HollowBodyCatalog.All.Count;

	public static string ProjectileRowBreakdown => string.Format("{0} настраиваемых + {1} каталожных + ", parametric.Length, RoundCatalog.All.Count) + string.Format("{0} отклонённых паспортов + ", RodPassports.Refused.Count) + string.Format("{0} кумулятивных + {1} полых тел", WarheadCatalog.All.Count, HollowBodyCatalog.All.Count);

	public static int DeprecatedMaterialCount => deprecatedMaterials.Count;

	public static int SuperOnlyMaterialCount => superOnlyMaterials.Count;

	public static int SuperOnlyClassCount => superOnlyClasses.Count;

	public static int MultiPresetKindCount => multiPresetKinds.Count;

	public static int DeprecatedKindCount => deprecatedKinds.Count;

	public static int PanelOnlyKindCount => panelOnlyKinds.Count;

	public static IList<ProjectileCatalogEntry> Projectiles { get; }

	public static IList<CoreCatalogEntry> Cores { get; }

	public static IList<MaterialCatalogEntry> Materials { get; }

	public static IList<ObjectCatalogEntry> Objects { get; }

	public static bool IsShapedCategory(string category)
	{
		return category == Shaped;
	}

	private static ProjectileCatalogEntry[] BuildProjectiles()
	{
		List<ProjectileCatalogEntry> result = new List<ProjectileCatalogEntry>(parametric);
		UnityEngine.Debug.Log(string.Format("◇ Каталог выстрелов: считаю пробитие для {0} строк ", RoundCatalog.All.Count) + "(полубесконечная RHA, дульная скорость) — следующие строки о баллистическом пределе относятся к ней.");
		foreach (RoundSpec spec in RoundCatalog.All)
		{
			RoundSpec captured = spec;
			result.Add(Proj(captured.id, captured.displayName, captured.category, RoundCatalog.Tooltip(captured), () => RoundCatalog.MakeDefinition(captured)));
		}
		result.AddRange(PassportTiles(RodPassports.Refused));
		foreach (WarheadSpec spec in WarheadCatalog.All)
		{
			WarheadSpec captured = spec;
			result.Add(Proj(captured.id, captured.displayName, captured.category, WarheadCatalog.Tooltip(captured), () => WarheadCatalog.MakeDefinition(captured)));
		}
		foreach (HollowBodySpec spec in HollowBodyCatalog.All)
		{
			HollowBodySpec captured = spec;
			result.Add(Proj(captured.id, captured.displayName, captured.category, HollowBodyCatalog.Tooltip(captured), () => HollowBodyCatalog.MakeDefinition(captured)));
		}
		return result.ToArray();
	}

	internal static List<ProjectileCatalogEntry> PassportTiles(IEnumerable<RodPassport> refused)
	{
		List<ProjectileCatalogEntry> result = new List<ProjectileCatalogEntry>();
		foreach (RodPassport pass in refused)
		{
			RodPassport captured = pass;
			result.Add(Proj(captured.id, captured.displayName, RoundCatalog.AC, RodPassports.Tooltip(captured), () => RodPassports.MakeDefinition(captured)));
		}
		return result;
	}

	public static bool IsMaterialAnalytic(ArmorPlate.ArmorType type)
	{
		return !superOnlyMaterials.Contains(type);
	}

	public static bool IsProjectileAnalytic(ProjectileClass kind)
	{
		return !superOnlyClasses.Contains(kind);
	}

	private static ObjectCatalogEntry[] BuildObjects()
	{
		List<ObjectCatalogEntry> result = new List<ObjectCatalogEntry>(singleKinds);
		foreach (EraPreset spec in EraCatalog.All)
		{
			EraPreset captured = spec;
			result.Add(new ObjectCatalogEntry
			{
				meta = Meta("object", captured.id, captured.displayName, captured.category, EraCatalog.Tooltip(captured)),
				kind = ArmorPlate.ObjectKind.Era,
				apply = delegate(ArmorPlate plate)
				{
					plate.presetId = captured.id;
					plate.armorType = captured.armor;
					plate.eraPlateThicknessMM = captured.plateMM;
					plate.eraInterlayerGapMM = captured.gapMM;
					plate.eraPlateVelocityMS = captured.velocityMS;
					plate.eraCassetteExtentMM = captured.extentMM;
				}
			});
		}
		foreach (NeraPreset spec in NeraCatalog.All)
		{
			NeraPreset captured = spec;
			result.Add(new ObjectCatalogEntry
			{
				meta = Meta("object", captured.id, captured.displayName, captured.category, NeraCatalog.Tooltip(captured)),
				kind = ArmorPlate.ObjectKind.Nera,
				apply = delegate(ArmorPlate plate)
				{
					plate.presetId = captured.id;
					plate.neraFrontThicknessMM = captured.frontMM;
					plate.neraRearThicknessMM = captured.rearMM;
					plate.neraInterlayerMM = captured.interlayerMM;
					plate.neraFrontMaterial = captured.frontMaterial;
					plate.neraRearMaterial = captured.rearMaterial;
					plate.neraInterlayer = captured.interlayer;
					plate.neraMountAngleDeg = captured.mountAngleDeg;
					plate.neraExtentMM = captured.extentMM;
					plate.neraBonding = captured.bonding;
				}
			});
		}
		return result.ToArray();
	}

	public static bool IsMultiPresetKind(ArmorPlate.ObjectKind kind)
	{
		return multiPresetKinds.Contains(kind);
	}

	public static bool IsPanelOnlyKind(ArmorPlate.ObjectKind kind)
	{
		return panelOnlyKinds.Contains(kind);
	}

	public static bool IsKindAvailable(ArmorPlate.ObjectKind kind)
	{
		return !deprecatedKinds.Contains(kind);
	}

	public static bool IsMaterialAvailable(ArmorPlate.ArmorType type)
	{
		return !deprecatedMaterials.Contains(type);
	}

	public static string[] ProjectileCategories()
	{
		return CategoriesOf(ProjectileMeta());
	}

	public static string[] CoreCategories()
	{
		return CategoriesOf(CoreMeta());
	}

	public static string[] MaterialCategories()
	{
		return CategoriesOf(MaterialMeta());
	}

	public static string[] ObjectCategories()
	{
		return CategoriesOf(ObjectMeta());
	}

	public static CatalogEntry[] ProjectileMeta()
	{
		return Metas(projectiles.Length, i => projectiles[i].meta);
	}

	public static CatalogEntry[] CoreMeta()
	{
		return Metas(cores.Length, i => cores[i].meta);
	}

	public static CatalogEntry[] MaterialMeta()
	{
		return Metas(materials.Length, i => materials[i].meta);
	}

	public static CatalogEntry[] ObjectMeta()
	{
		return Metas(objects.Length, i => objects[i].meta);
	}

	public static bool TryObjectKind(string id, out ArmorPlate.ObjectKind kind)
	{
		foreach (ObjectCatalogEntry row in objects)
		{
			if (row.meta.id == id)
			{
				kind = row.kind;
				return true;
			}
		}
		kind = default;
		return false;
	}

	public static string MaterialName(ArmorPlate.ArmorType type)
	{
		foreach (MaterialCatalogEntry row in materials)
		{
			if (row.type == type)
			{
				return row.meta.Name;
			}
		}
		return ArmorPlate.DisplayName(type);
	}

	public static string MaterialSection(ArmorPlate.ArmorType type)
	{
		foreach (MaterialCatalogEntry row in materials)
		{
			if (row.type == type)
			{
				return row.meta.category;
			}
		}
		return "";
	}

	public static string ObjectName(ArmorPlate.ObjectKind kind)
	{
		foreach (ObjectCatalogEntry row in objects)
		{
			if (row.kind == kind)
			{
				return row.meta.Name;
			}
		}
		return ArmorPlate.KindName(kind);
	}

	public static string ObjectIdFor(ArmorPlate.ObjectKind kind)
	{
		foreach (ObjectCatalogEntry row in objects)
		{
			if (row.kind == kind)
			{
				return row.meta.id;
			}
		}
		return null;
	}

	public static string ObjectIdFor(ArmorPlate plate)
	{
		if (plate == null)
		{
			return null;
		}
		if (!multiPresetKinds.Contains(plate.kind))
		{
			return ObjectIdFor(plate.kind);
		}
		if (plate.kind == ArmorPlate.ObjectKind.Era)
		{
			return EraCatalog.MatchingId(plate);
		}
		return NeraCatalog.MatchingId(plate);
	}

	public static bool ApplyObjectPreset(string id, ArmorPlate plate)
	{
		foreach (ObjectCatalogEntry row in objects)
		{
			if (row.meta.id == id)
			{
				row.apply?.Invoke(plate);
				return true;
			}
		}
		return false;
	}

	public static ProjectileDefinition MakeProjectile(string id)
	{
		foreach (ProjectileCatalogEntry row in projectiles)
		{
			if (row.meta.id == id)
			{
				return row.make?.Invoke();
			}
		}
		return null;
	}

	public static string ProjectileIdFor(ProjectileDefinition def)
	{
		if (def != null)
		{
			foreach (ProjectileCatalogEntry row in projectiles)
			{
				ProjectileDefinition made = row.make();
				if (made.name == def.name)
				{
					return row.meta.id;
				}
			}
		}
		return projectiles.Length != 0 ? projectiles[0].meta.id : null;
	}

	public static bool TryCoreType(string id, out Projectile.CoreType type)
	{
		foreach (CoreCatalogEntry row in cores)
		{
			if (row.meta.id == id)
			{
				type = row.type;
				return true;
			}
		}
		type = default;
		return false;
	}

	public static string CoreIdFor(Projectile.CoreType type)
	{
		foreach (CoreCatalogEntry row in cores)
		{
			if (row.type == type)
			{
				return row.meta.id;
			}
		}
		return null;
	}

	public static bool TryMaterialType(string id, out ArmorPlate.ArmorType type)
	{
		foreach (MaterialCatalogEntry row in materials)
		{
			if (row.meta.id == id)
			{
				type = row.type;
				return true;
			}
		}
		type = default;
		return false;
	}

	public static string MaterialIdFor(ArmorPlate.ArmorType type)
	{
		foreach (MaterialCatalogEntry row in materials)
		{
			if (row.type == type)
			{
				return row.meta.id;
			}
		}
		return null;
	}

	public static string TooltipFor(CatalogEntry[] entries, string id)
	{
		foreach (CatalogEntry row in entries)
		{
			if (row.id == id)
			{
				return row.Tip;
			}
		}
		return null;
	}

	public static List<string> Validate()
	{
		List<string> problems = new List<string>();
		ValidateMaterials(materials, problems);
		ValidateCores(cores, problems);
		ValidateProjectiles(projectiles, problems);
		ValidateObjects(objects, problems);
		NeraCatalog.Validate(problems);
		EraCatalog.Validate(problems);
		WarheadCatalog.Validate(problems);
		HollowBodyCatalog.Validate(problems);
		RoundCatalog.Validate(problems);
		return problems;
	}

	public static void ValidateObjects(IList<ObjectCatalogEntry> rows, List<string> problems)
	{
		Dictionary<ArmorPlate.ObjectKind, int> counts = new Dictionary<ArmorPlate.ObjectKind, int>();
		Dictionary<ArmorPlate.ObjectKind, int> withoutPresets = new Dictionary<ArmorPlate.ObjectKind, int>();
		foreach (ObjectCatalogEntry row in rows)
		{
			CheckMeta(row.meta, "вид объекта", problems);
			if (!Enum.IsDefined(typeof(ArmorPlate.ObjectKind), row.kind))
			{
				problems.Add(string.Format("вид объекта '{0}': {1} не является значением ArmorPlate.ObjectKind", row.meta.id, row.kind));
				continue;
			}
			if (row.apply == null)
			{
				if (row.meta.id != row.kind.ToString())
				{
					problems.Add(string.Format("вид объекта '{0}': ключ не совпадает с ObjectKind.{1}", row.meta.id, row.kind));
				}
				withoutPresets.TryGetValue(row.kind, out int presetCount);
				withoutPresets[row.kind] = presetCount + 1;
			}
			else if (!multiPresetKinds.Contains(row.kind))
			{
				problems.Add(string.Format("вид объекта '{0}': строка-пресет для ObjectKind.{1}, ", row.meta.id, row.kind) + "которого нет в multiPresetKinds");
			}
			counts.TryGetValue(row.kind, out int count);
			counts[row.kind] = count + 1;
		}
		foreach (ArmorPlate.ObjectKind kind in Enum.GetValues(typeof(ArmorPlate.ObjectKind)))
		{
			counts.TryGetValue(kind, out int count);
			bool multi = multiPresetKinds.Contains(kind);
			if (count == 0)
			{
				if (!multi && !deprecatedKinds.Contains(kind) && !panelOnlyKinds.Contains(kind))
				{
					problems.Add(string.Format("ObjectKind.{0} отсутствует в реестре видов объекта", kind));
				}
			}
			else
			{
				if (deprecatedKinds.Contains(kind))
				{
					problems.Add(string.Format("ObjectKind.{0} изъят из выбора, но строка в реестре есть", kind));
				}
				if (panelOnlyKinds.Contains(kind))
				{
					problems.Add(string.Format("ObjectKind.{0} ставится своей панелью, но строка в реестре видов есть", kind));
				}
				if (count > 1 && !multi)
				{
					problems.Add(string.Format("ObjectKind.{0} встречается в реестре видов объекта {1} раз (нужна ровно одна строка)", kind, count));
				}
			}
			withoutPresets.TryGetValue(kind, out int presetCount);
			if (multi && presetCount > 0)
			{
				problems.Add(string.Format("ObjectKind.{0} объявлен многовариантным, но {1} его строк(и) без пресета", kind, presetCount));
			}
		}
		DuplicateIds(Metas(rows.Count, i => rows[i].meta), "видов объекта", problems);
	}

	public static void ValidateCores(IList<CoreCatalogEntry> rows, List<string> problems)
	{
		Dictionary<Projectile.CoreType, int> counts = new Dictionary<Projectile.CoreType, int>();
		foreach (CoreCatalogEntry row in rows)
		{
			CheckMeta(row.meta, "сердечник", problems);
			if (!Enum.IsDefined(typeof(Projectile.CoreType), row.type))
			{
				problems.Add(string.Format("сердечник '{0}': {1} не является значением Projectile.CoreType", row.meta.id, row.type));
				continue;
			}
			if (row.meta.id != row.type.ToString())
			{
				problems.Add(string.Format("сердечник '{0}': ключ не совпадает с CoreType.{1}", row.meta.id, row.type));
			}
			counts.TryGetValue(row.type, out int count);
			counts[row.type] = count + 1;
		}
		foreach (Projectile.CoreType type in Enum.GetValues(typeof(Projectile.CoreType)))
		{
			counts.TryGetValue(type, out int count);
			if (count == 0)
			{
				problems.Add(string.Format("CoreType.{0} отсутствует в реестре сердечников", type));
			}
			else if (count > 1)
			{
				problems.Add(string.Format("CoreType.{0} встречается в реестре сердечников {1} раз (нужна ровно одна строка)", type, count));
			}
		}
		DuplicateIds(Metas(rows.Count, i => rows[i].meta), "сердечников", problems);
	}

	public static void ValidateMaterials(IList<MaterialCatalogEntry> rows, List<string> problems)
	{
		Dictionary<ArmorPlate.ArmorType, int> counts = new Dictionary<ArmorPlate.ArmorType, int>();
		foreach (MaterialCatalogEntry row in rows)
		{
			CheckMeta(row.meta, "материал", problems);
			if (!Enum.IsDefined(typeof(ArmorPlate.ArmorType), row.type))
			{
				problems.Add(string.Format("материал '{0}': {1} не является значением ArmorPlate.ArmorType", row.meta.id, row.type));
				continue;
			}
			if (row.meta.id != row.type.ToString())
			{
				problems.Add(string.Format("материал '{0}': ключ не совпадает с ArmorType.{1}", row.meta.id, row.type));
			}
			counts.TryGetValue(row.type, out int count);
			counts[row.type] = count + 1;
		}
		foreach (ArmorPlate.ArmorType type in Enum.GetValues(typeof(ArmorPlate.ArmorType)))
		{
			counts.TryGetValue(type, out int count);
			if (count == 0)
			{
				if (!deprecatedMaterials.Contains(type))
				{
					problems.Add(string.Format("ArmorType.{0} отсутствует в реестре материалов", type));
				}
			}
			else if (deprecatedMaterials.Contains(type))
			{
				problems.Add(string.Format("ArmorType.{0} изъят из выбора, но строка в реестре есть", type));
			}
			if (count > 1)
			{
				problems.Add(string.Format("ArmorType.{0} встречается в реестре материалов {1} раз (нужна ровно одна строка)", type, count));
			}
		}
		DuplicateIds(Metas(rows.Count, i => rows[i].meta), "материалов", problems);
	}

	public static void ValidateProjectiles(IList<ProjectileCatalogEntry> rows, List<string> problems)
	{
		Dictionary<Projectile.CoreType, int> counts = new Dictionary<Projectile.CoreType, int>();
		foreach (ProjectileCatalogEntry row in rows)
		{
			CheckMeta(row.meta, "снаряд", problems);
			ProjectileDefinition made = row.make?.Invoke();
			if (made == null)
			{
				problems.Add("снаряд '" + row.meta.id + "': пресет не задан");
				continue;
			}
			if (!Enum.IsDefined(typeof(Projectile.CoreType), made.coreType))
			{
				problems.Add(string.Format("снаряд '{0}': {1} не является значением Projectile.CoreType", row.meta.id, made.coreType));
				continue;
			}
			if (IsShapedCategory(row.meta.category) != made.IsShapedCharge)
			{
				problems.Add(string.Format("снаряд '{0}': категория '{1}' не соответствует классу {2}", row.meta.id, row.meta.category, made.kind));
			}
			if (CoreIdFor(made.coreType) == null)
			{
				problems.Add(string.Format("снаряд '{0}': сердечник {1} отсутствует в реестре сердечников", row.meta.id, made.coreType));
			}
			if (!made.IsShapedCharge && IsProjectileAnalytic(made.kind))
			{
				counts.TryGetValue(made.coreType, out int count);
				counts[made.coreType] = count + 1;
			}
		}
		foreach (Projectile.CoreType type in Enum.GetValues(typeof(Projectile.CoreType)))
		{
			counts.TryGetValue(type, out int count);
			if (count == 0)
			{
				problems.Add(string.Format("CoreType.{0} не задаётся ни одним кинетическим пресетом", type));
			}
		}
		DuplicateIds(Metas(rows.Count, i => rows[i].meta), "снарядов", problems);
	}

	public static void AssertComplete()
	{
		List<string> problems = Validate();
		if (problems.Count != 0)
		{
			foreach (string problem in problems)
			{
				UnityEngine.Debug.LogError("Catalog: " + problem);
			}
			throw new InvalidOperationException(string.Format("Catalog: реестр неполон ({0} проблем) — подробности выше.", problems.Count));
		}
	}

	private static CatalogEntry Meta(string prefix, string id, string displayName, string category, string tooltip)
	{
		return new CatalogEntry
		{
			id = id,
			displayName = displayName,
			category = category,
			tooltip = tooltip,
			nameKey = prefix + "." + id.ToLowerInvariant(),
			tipKey = prefix + "." + id.ToLowerInvariant() + ".desc",
			categoryKey = CategoryKey(category)
		};
	}

	private static string CategoryKey(string category)
	{
		switch (category)
		{
		case Reactive: return "cat.reactive";
		case Screens: return "cat.screens";
		case Modules: return "cat.modules";
		case "ПТУР и гранатомёты": return "cat.atgm";
		case HeavyAlloys: return "cat.heavy_alloys";
		case Kinetic: return "cat.kinetic";
		case "Германия (DM)": return "cat.rounds_de";
		case HeavyMetals: return "cat.heavy_metals";
		case SteelsAndCarbides: return "cat.steels_carbides";
		case Shaped: return "cat.shaped";
		case "Полые тела": return "cat.hollow_bodies";
		case Steels: return "cat.steels";
		case Composites: return "cat.composites";
		case BehindArmour: return "cat.behind_armour";
		case RoundCatalog.AC: return "cat.rounds_autocannon";
		case NonExplosive: return "cat.non_explosive";
		case RoundCatalog.RU: return "cat.rounds_ru";
		case Plates: return "cat.plates";
		case RoundCatalog.US: return "cat.rounds_us";
		case Ceramics: return "cat.ceramics";
		case LightAlloys: return "cat.light_alloys";
		default: return null;
		}
	}

	private static ProjectileCatalogEntry Proj(string id, string name, string category, string tooltip, Func<ProjectileDefinition> make)
	{
		return new ProjectileCatalogEntry { meta = Meta("proj", id, name, category, tooltip), make = make };
	}

	private static CoreCatalogEntry Core(Projectile.CoreType type, string category, string tooltip, Func<TateModel.Penetrator> penetrator)
	{
		return new CoreCatalogEntry { meta = Meta("core", type.ToString(), penetrator().name, category, tooltip), type = type };
	}

	private static ObjectCatalogEntry Obj(ArmorPlate.ObjectKind kind, string category, string tooltip)
	{
		return new ObjectCatalogEntry { meta = Meta("object", kind.ToString(), ArmorPlate.KindName(kind), category, tooltip), kind = kind };
	}

	private static MaterialCatalogEntry Mat(ArmorPlate.ArmorType type, string category, string tooltip)
	{
		return new MaterialCatalogEntry { meta = Meta("material", type.ToString(), ArmorPlate.DisplayName(type), category, tooltip), type = type };
	}

	private static CatalogEntry[] Metas(int count, Func<int, CatalogEntry> pick)
	{
		CatalogEntry[] result = new CatalogEntry[count];
		for (int i = 0; i < count; i++)
		{
			result[i] = pick(i);
		}
		return result;
	}

	private static string[] CategoriesOf(CatalogEntry[] entries)
	{
		List<string> result = new List<string>();
		foreach (CatalogEntry row in entries)
		{
			if (!result.Contains(row.category))
			{
				result.Add(row.category);
			}
		}
		return result.ToArray();
	}

	private static void CheckMeta(CatalogEntry m, string what, List<string> problems)
	{
		if (string.IsNullOrEmpty(m.id)) problems.Add(what + ": пустой ключ");
		if (string.IsNullOrEmpty(m.displayName)) problems.Add(what + " '" + m.id + "': пустая подпись");
		if (string.IsNullOrEmpty(m.category)) problems.Add(what + " '" + m.id + "': пустая категория");
		if (string.IsNullOrEmpty(m.tooltip)) problems.Add(what + " '" + m.id + "': пустая подсказка");
	}

	private static void DuplicateIds(CatalogEntry[] entries, string what, List<string> problems)
	{
		for (int i = 0; i < entries.Length; i++)
		{
			for (int j = i + 1; j < entries.Length; j++)
			{
				if (entries[i].id == entries[j].id)
				{
					problems.Add("реестр " + what + ": ключ '" + entries[i].id + "' повторяется");
				}
			}
		}
	}

	static Catalog()
	{
		parametric = new ProjectileCatalogEntry[8]
		{
			Proj("wha_rod", "WHA стержень", Kinetic, "Вольфрамовый стержень, L/D = 10. Расчёт по Тейту.", ProjectileLibrary.WHALongRod),
			Proj("du_rod", "DU стержень", Kinetic, "Урановый стержень, L/D = 15, самозатачивается. Тейт.", ProjectileLibrary.DULongRod),
			Proj("steel_ap", "Бронебойный", Kinetic, "Короткий, притупленная головка, калибр 40 мм. Жёсткая ветка Понселе, не Тейт.", ProjectileLibrary.SteelAP),
			Proj("steel_apc", "Бронебойный с колпачком", Kinetic, "Тот же сердечник, но под баллистическим колпаком. Колпак размазывает ударный импульс: порог раскола по RHA сдвигается с ~800 до ~1095 м/с. Отличается от предыдущего ОДНИМ полем.", ProjectileLibrary.SteelAPC),
			Proj("wc_core", "Твёрдый сердечник", Kinetic, "Короткий сердечник, коническое остриё, калибр 25 мм. Тейт.", ProjectileLibrary.WCCore),
			Proj("heat_85", "Кумулятивный", Shaped, "Струя, калибр 85 мм, стандоф 2.0 CD. Другое ядро, не Тейт. ⚠ Марки реального боеприпаса имя НЕ несёт намеренно: это параметрический архетип. Названные изделия — в каталоге кумулятивных (WarheadCatalog). ◈ Корпус до подрыва рисуется моделью Resources/Models/defaulttled. ⚠ Меш в файле назван 9М14 «Малютка» — на экране узнаваемое изделие, тогда как строка остаётся безымянным архетипом. На числа это не влияет: путь к модели в расчёт не входит.", ProjectileLibrary.ShapedCharge85),
			Proj("heat_tandem", "Тандемный", Shaped, "Предзаряд снимает кассету ДЗ, основной заряд идёт по уже инертной. ⚠ Калибр предзаряда и задержка между зарядами — PLACEHOLDER. Предзаряд НЕ считается отдельным пробитием: его работа — инициировать кассету. ◈ Корпус — ТА ЖЕ модель, что у моноблока (Resources/Models/defaulttled): тандемной головы на экране НЕ ВИДНО, отличие тандема — в расчёте.", ProjectileLibrary.TandemShapedCharge),
			Proj("etalon_rod", "Эталонный стержень", Kinetic, "WHA L/D = 10, ⌀ 30 мм, 1100 м/с, ПЛОСКИЙ торец. Та самая постановка, на которой сняты отпечаток решателя и все замеры этого проекта: ворота набора идут именно на плоском носе. Считается как любой другой стержень, по Тейту.", ProjectileLibrary.EtalonFlatRod)
		};
		projectiles = BuildProjectiles();
		cores = new CoreCatalogEntry[4]
		{
			Core(Projectile.CoreType.WHA, HeavyAlloys, "ρ = 17300, Y_p = 2.0 ГПа. Классический БОПС.", TateModel.WHA),
			Core(Projectile.CoreType.DU, HeavyAlloys, "ρ = 18600, Y_p = 1.1 ГПа. Самозатачивается при эрозии.", TateModel.DU),
			Core(Projectile.CoreType.Steel, SteelsAndCarbides, "ρ = 7850, Y_p = 2.5 ГПа (закалённая). Втрое легче вольфрама.", TateModel.Steel),
			Core(Projectile.CoreType.WC, SteelsAndCarbides, "ρ = 15000, Y_p = 5.0 ГПа. Хрупкий: колется, а не гнётся.", TateModel.WC)
		};
		materials = new MaterialCatalogEntry[16]
		{
			Mat(ArmorPlate.ArmorType.RHA, Steels, "Катаная гомогенная броня, ρ = 7850. R_t = 5.3 ГПа, подгонка по Hauver."),
			Mat(ArmorPlate.ArmorType.HHA, Steels, "Высокотвёрдая сталь, ρ = 7850. R_t = 7.0 ГПа, подгонка."),
			Mat(ArmorPlate.ArmorType.Aluminum, LightAlloys, "Алюминиевый сплав, ρ = 2700. R_t = 1.5 ГПа, подгонка."),
			Mat(ArmorPlate.ArmorType.Titanium, LightAlloys, "Ti-6Al-4V, ρ = 4430. R_t = 4.2 ГПа, подгонка."),
			Mat(ArmorPlate.ArmorType.Ceramic, Ceramics, "Карбид кремния, ρ = 3200. R_t = 7.0 ГПа, подгонка по Orphal & Franzen."),
			Mat(ArmorPlate.ArmorType.Textolite, Composites, "Стеклотекстолит, ρ = 1850. ⚠⚠ ЗДЕСЬ МОДЕЛЬ НЕПРИМЕНИМА, А НЕ ПРОСТО НЕТОЧНА. Исследование №10 (§C3) отвечает прямо: Алексеевский–Тейт выводится из уравнения Бернулли для стационарного течения, а композит НЕ ТЕЧЁТ — он разрушается разрывом волокон и расслоением, после которого граничные условия стационарности перестают существовать. Сопротивление нестационарно: пик в первую микросекунду, дальше падение на порядки. ⚠ R_t = 1.2 ГПа — ASSUMED-ESTIMATED; подогнанного значения для стеклопластика в литературе НЕТ (NOT FOUND) и не может быть по построению. Число даёт устойчивую арифметику, а не физику."),
			Mat(ArmorPlate.ArmorType.Tungsten, HeavyMetals, "Вольфрам, ρ = 17300. R_t = 9.0 ГПа, оценка cavity-expansion."),
			Mat(ArmorPlate.ArmorType.CastSteel, Steels, "Литая броневая сталь, ρ = 7850 (MEASURED — состав тот же, что у катаной). ⭐ R_t = 5.3 ГПа, КАК У RHA. Ходовой коэффициент «литьё держит 0.85–0.9 от проката» проверен исследованием №10 (§B1) и оказался ИНЖЕНЕРНЫМ ФОЛЬКЛОРОМ без первоисточника — того же рода, что опровергнутое «R_t ≈ 3.5·Y»; спецификации требуют отстрела, а не задают модификатор. ⭐ Отличие литья от проката НЕ В СОПРОТИВЛЕНИИ, А В РАЗРУШЕНИИ: микропористость 0.10–0.27 % и удлинение при разрыве 12.8–19.6 % против 22.0 % у проката (MEASURED) ⇒ откол наступает раньше и осколки крупнее. ⚠ Порог этого перехода в модели Грэди НЕ НАЙДЕН (NOT FOUND) и потому НЕ смоделирован — литьё пока дробится как катаная сталь."),
			Mat(ArmorPlate.ArmorType.BoronCarbide, Ceramics, "Карбид бора B₄C, ρ = 2520 (MEASURED). R_t = 6.0 ГПа — FITTED, Orphal/Franzen 1997 (40 опытов, WHA L/D 20). ⭐ ЕДИНСТВЕННЫЙ материал, чьё сопротивление ПАДАЕТ с ростом давления: выше ~40 ГПа (DERIVED, излом Гюгонио) структура аморфизуется и прочность обваливается. Против стержней порог НЕ достигается никогда, против головной части струи — достигается. ⚠ R_деград — ASSUMED, БЕЗ ИСТОЧНИКА. ⚠ HEL 14-19 ГПа — ДРУГАЯ величина, не R_t."),
			Mat(ArmorPlate.ArmorType.TitaniumDiboride, Ceramics, "Диборид титана TiB₂, ρ = 4500 (MEASURED). ⭐ R_t СПАДАЕТ ПО ГЛУБИНЕ: 10.7 → 5.8 ГПа по мере измельчения керамики (обе точки FITTED, Hauver/Gooch 1992, WHA 1500 м/с). ⚠ 8.4 ГПа из того же источника — ПУТЕВОЕ СРЕДНЕЕ, а не третья стадия, и в модели не используется. ⚠ Длина спада — ASSUMED, БЕЗ ИСТОЧНИКА. ⚠ Вязкость разрушения входит ТОЛЬКО как скорость спада; многократное попадание вне модели."),
			Mat(ArmorPlate.ArmorType.Alumina, Ceramics, "Оксид алюминия AD995, ρ = 3900 (MEASURED). R_t = 5.8 ГПа — FITTED, Hohler/Stilp/Weber 1995 (критическая скорость: 5.4/6.1/4.8 ГПа на 1.7/2.5/3.0 км/с). Источник отмечает отсутствие заметной скоростной зависимости — поэтому строка постоянная по ЕГО утверждению, а не по нашему упрощению."),
			Mat(ArmorPlate.ArmorType.DepletedUraniumArmour, HeavyMetals, "Обеднённый уран как БРОНЯ, ρ = 18600 (MEASURED). R_t = 5.8 ГПа — DERIVED: R_t = Y·[2/3 + ln(2E/3Y)] (Hill 1950, принято Тейтом 1986) при Y = 1.1 ГПа, E = 208 ГПа, с поверкой метода на RHA (даёт 5.56 против измеренных 5.3, +5%). ⚠⚠ ИЗМЕРЕНИЙ R_t ДЛЯ УРАНА-МИШЕНИ НЕТ ВОВСЕ. ⚠ Его защита — это ПЛОТНОСТЬ, которая работает отдельным инерционным членом ½·ρ·u²; выводить из неё ещё и высокое R_t = считать дважды."),
			Mat(ArmorPlate.ArmorType.Steel4340, Steels, "Сталь 4340, ρ = 7830. ⭐ ЕДИНСТВЕННАЯ строка реестра Super с полным и внутренне согласованным набором «течение + повреждаемость + УС» — на ней гоняются проверки решателя. ⚠⚠ ВЫБИРАЕТСЯ ТОЛЬКО В SUPER. В обычном режиме эта плита НЕ СТРЕЛЯЕТ, и это отсутствие данных, названное вслух: аналитическому ядру нужна мишень (R_t), а у проекта есть только пенетратор (TateModel.Steel(), Y_p = 2.5 ГПа). R_t этой стали не измерен, не выведен и подставлять его от RHA нельзя — вышел бы результат RHA под вывеской 4340."),
			Mat(ArmorPlate.ArmorType.Polyurethane, Elastomers, "Плотный полиуретан. ⭐ УДАРНАЯ АДИАБАТА — СТРОКА РЕЕСТРА SUPER (с 22.09): ρ0 = 1264 кг/м³, C0 = 2660 м/с, S = 1.48 — Carter & Marsh, плотный полиуретан, MEASURED; Grüneisen, T_melt и C_p не поставлены (ASSUMED). Этой же строкой Super считает натуральную резину — объявленная подстановка; у неопрена с S30 строка СВОЯ. ⚠ Таблица эластомеров аналитики (иссл. №10 §A5) — УТОЧНЯЕТСЯ: источника на диске нет, её числа не показываются, в счёт выстрела она не входит. ⚠⚠ ЭТО НЕ БРОНЯ, И ЭТО НЕ ОГОВОРКА О ТОЧНОСТИ: прочности у прослойки нет вовсе (G = 0, A = 0 — это МОДЕЛЬ отрасли, а не пробел), против стержня её R_t ≈ 0, и работает она средой передачи давления, а не преградой. ⚠⚠ ВЫБИРАЕТСЯ ТОЛЬКО В SUPER. Обычный режим ОТКАЗЫВАЕТ ПО ИМЕНИ: у Алексеевского–Тейта мишени для жидкости без прочности нет, и «пробивается насквозь чем угодно» было бы враньём на низких скоростях, где резина ведёт себя упруго."),
			Mat(ArmorPlate.ArmorType.Neoprene, Elastomers, "Полихлоропрен (неопрен). ⭐ УДАРНАЯ АДИАБАТА — СТРОКА РЕЕСТРА SUPER (с 21.09): ρ0 = 1230 кг/м³, C0 = 1420 м/с, S = 1.75 — Millett, Bourne & Akhavan, J. Appl. Phys. 95, 4722 (2004), коммерческий полихлоропрен без заметного сажевого наполнения; фит до ≈ 12 ГПа, выше — экстраполяция; Grüneisen и C_p подставлены от полиуретана (не найдены); числа — повтор до сверки со статьёй. ⚠ Таблица эластомеров аналитики (иссл. №10 §A5) — УТОЧНЯЕТСЯ: источника на диске нет, её числа не показываются, в счёт выстрела она не входит. ⚠⚠ Не броня и в обычном режиме не стреляет — по той же причине, что полиуретан."),
			Mat(ArmorPlate.ArmorType.NaturalRubber, Elastomers, "Натуральная резина. ⛔⛔ СВОИХ ЧИСЕЛ У НЕЁ НЕТ НИ ОДНОГО: ни плотности, ни ударной адиабаты — не добыто (бриф 33 послан и не вернулся). Строка существует потому, что ИЗМЕРЕННЫЕ пакеты брифа 24 (ISL/Fras, KFRRCA) собраны именно на резине, и записать их «полиуретаном» значило бы солгать о самом пакете. ⚠⚠ ПОКА БРИФ НЕ ВЕРНУЛСЯ, ЭТА ПЛИТА ЧИСЛЕННО НЕОТЛИЧИМА ОТ ПОЛИУРЕТАНОВОЙ — и это объявленная ПОДСТАНОВКА, а не совпадение и не «примерно одно и то же». Подставить ей выдуманные c₀ и s «чтобы материалы отличались» прямо запрещено: правдоподобное различие хуже честного его отсутствия. ⚠⚠ Не броня и в обычном режиме не стреляет — по той же причине, что полиуретан.")
		};
		deprecatedMaterials = new HashSet<ArmorPlate.ArmorType>
		{
			ArmorPlate.ArmorType.ERA,
			ArmorPlate.ArmorType.ERAHeavy,
			ArmorPlate.ArmorType.RhaSetB
		};
		superOnlyMaterials = new HashSet<ArmorPlate.ArmorType>
		{
			ArmorPlate.ArmorType.Steel4340,
			ArmorPlate.ArmorType.Polyurethane,
			ArmorPlate.ArmorType.Neoprene,
			ArmorPlate.ArmorType.NaturalRubber
		};
		superOnlyClasses = new HashSet<ProjectileClass> { ProjectileClass.HollowBody };
		singleKinds = new ObjectCatalogEntry[5]
		{
			Obj(ArmorPlate.ObjectKind.Plate, Plates, "Обычная преграда: материал и толщина задаются отдельно."),
			Obj(ArmorPlate.ObjectKind.Rod, Plates, "Стержень: круглый пруток как броня. Толщина — диаметр, ширина — длина; материал задаётся отдельно. Путь в металле — хорда круга: по центру это диаметр, ближе к краю — меньше, а удар идёт всё косее."),
			Obj(ArmorPlate.ObjectKind.SpallLiner, BehindArmour, "Противоосколочный подбой: ткань (арамид/СВМПЭ), которая ЛОВИТ ОСКОЛКИ растяжением волокон. ⭐ НЕ материал брони и не преграда: снаряд по линии визирования он не задерживает (модели его сопротивления лому нет), его работа начинается ПОСЛЕ пробития. Управляющая величина — поверхностная плотность ρ·t, поэтому толщина входит в расчёт напрямую. ⚠ Порог поглощения (2·10⁵ Дж/кг у арамида) — ASSUMED, БЕЗ ИСТОЧНИКА. ⚠ СВМПЭ лучше на единицу массы — это установлено качественно, но отношение ×1.5 ПРЕДПОЛОЖЕНО."),
			Obj(ArmorPlate.ObjectKind.Crew, BehindArmour, "Сидящий манекен экипажа с внутренними органами. O — показать органы; C — сечение. Детальная геометрия — визуальное представление (ASSUMED). Вход в тело определяется оболочкой, попадания в органы — упрощёнными объёмами. Рёбра и сосуды отдельно не рассчитываются. Красный цвет обозначает задетый орган, а не тяжесть травмы. ⚠ МОДЕЛЬ СЧИТАЕТ ПОПАДАНИЯ И ЭНЕРГИЮ И НЕ ПРЕДСКАЗЫВАЕТ ПОРАЖЕНИЕ: раневой баллистики в проекте нет, ВСЕ пороги ASSUMED без источника. Прямое попадание снаряда регистрируется; сопротивление фигуры снаряду не рассчитывается."),
			Obj(ArmorPlate.ObjectKind.EngineBlock, Modules, "Моторный отсек: объём материала с пустотами. Пустотность снимает ТОЛЬШИНУ (т_эфф = (1−f)·t), а материал остаётся собой — полные ρ и R_t. ⚠ ОДНОРОДНОЕ ПРИБЛИЖЕНИЕ: настоящий путь зависит от точки входа (цилиндр или полость), модель даёт СРЕДНЕЕ. ⚠ Пустотность 0.55 — ASSUMED. ⚠ Сталь берётся строкой RHA — единственной стальной с источником.")
		};
		objects = BuildObjects();
		multiPresetKinds = new HashSet<ArmorPlate.ObjectKind>
		{
			ArmorPlate.ObjectKind.Era,
			ArmorPlate.ObjectKind.Nera
		};
		deprecatedKinds = new HashSet<ArmorPlate.ObjectKind>
		{
			ArmorPlate.ObjectKind.Screen,
			ArmorPlate.ObjectKind.Slat
		};
		panelOnlyKinds = new HashSet<ArmorPlate.ObjectKind> { ArmorPlate.ObjectKind.Model };
		Projectiles = new ReadOnlyCollection<ProjectileCatalogEntry>(projectiles);
		Cores = new ReadOnlyCollection<CoreCatalogEntry>(cores);
		Materials = new ReadOnlyCollection<MaterialCatalogEntry>(materials);
		Objects = new ReadOnlyCollection<ObjectCatalogEntry>(objects);
	}
}
