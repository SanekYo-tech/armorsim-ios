using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class T7_RoutingTable : MonoBehaviour
{
	private static readonly string[] ForbiddenRoots = new string[16]
	{
		"penetrat", "perforat", "residual", "ballisticlimit", "outcome", "verdict", "defeated", "stopped",
		"ricochet", "erosionfraction", "result", "pass", "fail", "пробит", "исход", "остаточн"
	};

	[ContextMenu("T7 — таблица маршрутизации")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T7", "таблица маршрутизации", () => Check(), ref pass, ref total);
	}

	public static bool Check()
	{
		return CheckRouting() & CheckNoOutcomeField() & CheckGateMatrix() & CheckReasonKeys();
	}

	private static bool CheckRouting()
	{
		Dictionary<SimMode, SolverBackendKind> table = new Dictionary<SimMode, SolverBackendKind>();
		table.Add(SimMode.Normal, SolverBackendKind.Analytic);
		table.Add(SimMode.Advanced, SolverBackendKind.Analytic);
		table.Add(SimMode.Ultra, SolverBackendKind.Continuum);
		table.Add(SimMode.Super, SolverBackendKind.Super);
		SimMode[] modes = (SimMode[])Enum.GetValues(typeof(SimMode));
		bool ok = CT.True(string.Format("перебор покрывает все {0} значений SimMode", modes.Length), table.Count == modes.Length);
		int continuumCount = 0;
		for (int i = 0; i < modes.Length; i++)
		{
			SimMode m = modes[i];
			SolverBackendKind got = ContinuumRouting.BackendFor(m);
			SolverBackendKind want = table.TryGetValue(m, out SolverBackendKind w) ? w : SolverBackendKind.Analytic;
			ok &= CT.True(string.Format("{0} → {1} (ожидалось {2})", m, got, want), got == want);
			ok &= CT.True(string.Format("{0}: власть над исходом остаётся у аналитики", m), ContinuumRouting.OutcomeAuthority(m) == SolverBackendKind.Analytic);
		}
		for (int j = 0; j < modes.Length; j++)
		{
			if (ContinuumRouting.BackendFor(modes[j]) == SolverBackendKind.Continuum)
			{
				continuumCount++;
			}
		}
		return CT.True("решатель сплошной среды включается ровно в ОДНОМ режиме", continuumCount == 1) && ok;
	}

	private static bool CheckNoOutcomeField()
	{
		bool clean = true;
		Type[] roots = new Type[3] { typeof(GeometryTimeline), typeof(GeometryFrame), typeof(SolverDiagnostics) };
		for (int i = 0; i < roots.Length; i++)
		{
			Type t = roots[i];
			MemberInfo[] members = t.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			for (int j = 0; j < members.Length; j++)
			{
				MemberInfo m = members[j];
				if (!(m is FieldInfo) && !(m is PropertyInfo))
				{
					continue;
				}
				string lower = m.Name.ToLowerInvariant();
				for (int k = 0; k < ForbiddenRoots.Length; k++)
				{
					if (lower.Contains(ForbiddenRoots[k]))
					{
						Debug.Log("    ЗАПРЕЩЁННОЕ ПОЛЕ ИСХОДА: " + t.Name + "." + m.Name + " (корень «" + ForbiddenRoots[k] + "»)");
						clean = false;
					}
				}
			}
		}
		bool ok = CT.True("в GeometryTimeline / GeometryFrame / SolverDiagnostics нет полей исхода", clean);
		bool caught = false;
		for (int l = 0; l < ForbiddenRoots.Length; l++)
		{
			if ("residualVelocity".ToLowerInvariant().Contains(ForbiddenRoots[l]))
			{
				caught = true;
			}
		}
		bool sentinel = CT.True("проверка ловит подлог (поле residualVelocity было бы отвергнуто)", caught);
		bool implements = CT.True("ContinuumBackend реализует ISolverBackend", typeof(ISolverBackend).IsAssignableFrom(typeof(ContinuumBackend)));
		MethodInfo solve = typeof(ContinuumBackend).GetMethod("Solve");
		bool returnsTimeline = CT.True("Solve возвращает именно GeometryTimeline", solve.ReturnType == typeof(GeometryTimeline));
		return returnsTimeline && implements && sentinel && clean && ok;
	}

	private static bool CheckGateMatrix()
	{
		string reasonKey = null;
		bool ok = CT.True("нормальный удар WHA по RHA — ULTRA доступна", ContinuumGate.IsUltraAvailable(Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, ProjectileClass.LongRod, 0f), out reasonKey) && reasonKey == "ultra.ok");
		ok &= Reject("плита под 30°", Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, ProjectileClass.LongRod, 30f), "ultra.reject.angle");
		ok &= CT.True("наклон 0.4° внутри допуска ±0.5° — не отказ", ContinuumGate.IsUltraAvailable(Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, ProjectileClass.LongRod, 0.4f), out reasonKey));
		foreach (ArmorPlate.ObjectKind kind in Enum.GetValues(typeof(ArmorPlate.ObjectKind)))
		{
			bool avail = ContinuumGate.IsUltraAvailable(Scene(kind, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, ProjectileClass.LongRod, 0f), out reasonKey);
			ok &= CT.True(string.Format("вид объекта {0}: {1}", kind, avail ? "доступно" : "отказ «" + reasonKey + "»"), avail != (kind == ArmorPlate.ObjectKind.Slat || kind == ArmorPlate.ObjectKind.Crew));
		}
		foreach (ArmorPlate.ArmorType mat in Enum.GetValues(typeof(ArmorPlate.ArmorType)))
		{
			bool avail = ContinuumGate.IsUltraAvailable(Scene(ArmorPlate.ObjectKind.Plate, mat, Projectile.CoreType.WHA, ProjectileClass.LongRod, 0f), out reasonKey);
			bool mapped = ContinuumGate.TryMapTarget(mat, out ContinuumMaterialId id);
			ok &= CT.True(string.Format("материал преграды {0}: {1} ", mat, avail ? "доступно" : "отказ") + "(в таблице L1 " + (mapped ? "есть" : "НЕТ") + ")", avail == mapped);
		}
		foreach (Projectile.CoreType core in Enum.GetValues(typeof(Projectile.CoreType)))
		{
			bool avail = ContinuumGate.IsUltraAvailable(Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, core, ProjectileClass.LongRod, 0f), out reasonKey);
			bool mapped = ContinuumGate.TryMapCore(core, out ContinuumMaterialId id);
			ok &= CT.True(string.Format("сердечник {0}: {1} ", core, avail ? "доступно" : "отказ") + "(в таблице L1 " + (mapped ? "есть" : "НЕТ") + ")", avail == mapped);
		}
		ok &= CT.True("DU закрыт (подстановка WHA заставила бы уран ГРИБИТЬ)", !ContinuumGate.TryMapCore(Projectile.CoreType.DU, out ContinuumMaterialId du));
		ok &= CT.True("WC закрыт (упруго-хрупкий: Джонсон–Кук — неверная рамка)", !ContinuumGate.TryMapCore(Projectile.CoreType.WC, out ContinuumMaterialId wc));
		ok &= CT.True("HHA закрыт (возвращённый набор — сталь 4340 под другим именем)", !ContinuumGate.TryMapTarget(ArmorPlate.ArmorType.HHA, out ContinuumMaterialId hha));
		Dictionary<ProjectileClass, string> expected = new Dictionary<ProjectileClass, string>();
		expected.Add(ProjectileClass.LongRod, null);
		expected.Add(ProjectileClass.ArmorPiercing, null);
		expected.Add(ProjectileClass.Kinetic, null);
		expected.Add(ProjectileClass.ShapedCharge, "ultra.reject.material_jet_liner");
		expected.Add(ProjectileClass.HollowBody, "ultra.reject.no_hollow_body_model");
		ProjectileClass[] classes = (ProjectileClass[])Enum.GetValues(typeof(ProjectileClass));
		ok &= CT.True(string.Format("таблица классов покрывает все {0} значения ProjectileClass", classes.Length), expected.Count == classes.Length);
		for (int i = 0; i < classes.Length; i++)
		{
			ProjectileClass cls = classes[i];
			bool avail = ContinuumGate.IsUltraAvailable(Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, cls, 0f), out reasonKey);
			if (!expected.TryGetValue(cls, out string want))
			{
				ok &= CT.True(string.Format("класс {0} в таблице ожиданий НЕ НАЗВАН ", cls), ok: false);
				continue;
			}
			ok &= CT.True(string.Format("класс {0}: {1} ", cls, avail ? "доступно" : "отказ «" + reasonKey + "»") + "при ожидании " + (want ?? "доступно"), want == null ? avail : (!avail && reasonKey == want));
			ok &= CT.True(string.Format("класс {0}: доступность ({1}) согласна со сборкой ({2})", cls, avail, UltraProjection.CanRepresent(cls)), avail == UltraProjection.CanRepresent(cls));
		}
		SceneFile offsetScene = Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, ProjectileClass.LongRod, 0f);
		offsetScene.projectile.startXM += 0.05f;
		ok &= Reject("старт в 50 мм от оси", offsetScene, "ultra.reject.lateral");
		SceneFile frozen = Scene(ArmorPlate.ObjectKind.Plate, ArmorPlate.ArmorType.RHA, Projectile.CoreType.WHA, ProjectileClass.LongRod, 0f);
		frozen.objects[0].thicknessMM = 50f;
		frozen.projectile.rodLengthMM = 300f;
		frozen.projectile.diameterMM = 30f;
		frozen.projectile.velocityMps = 1100f;
		ok &= CT.True("замороженный эталон (WHA 300 мм @ 1100 → RHA 50 мм) проходит гейт", ContinuumGate.IsUltraAvailable(frozen, out reasonKey));
		return ok;
	}

	private static bool CheckReasonKeys()
	{
		bool ok = true;
		string[] allKeys = ContinuumGateStrings.AllKeys;
		for (int i = 0; i < allKeys.Length; i++)
		{
			string key = allKeys[i];
			bool has = ContinuumGateStrings.Table.ContainsKey(key);
			ok &= CT.True("ключ «" + key + "» есть в таблице переводов", has);
			if (has)
			{
				string[] row = ContinuumGateStrings.Table[key];
				bool allTranslated = row.Length == Localization.All.Length;
				for (int j = 0; j < row.Length && allTranslated; j++)
				{
					allTranslated &= !string.IsNullOrEmpty(row[j]);
				}
				ok &= CT.True(string.Format("ключ «{0}» переведён на все {1} языков", key, Localization.All.Length), allTranslated);
				ok &= CT.True("«" + key + "» выглядит как ключ, а не как готовая строка", key.StartsWith("ultra.") && !key.Contains(" "));
			}
		}
		foreach (ProjectileClass cls in Enum.GetValues(typeof(ProjectileClass)))
		{
			string key = UltraProjection.ClassRefusalKey(cls);
			if (key != null)
			{
				ok &= CT.True(string.Format("ключ отказа класса {0} («{1}») есть в таблице локализации", cls, key), Localization.HasKey(key));
			}
		}
		return ok;
	}

	private static bool Reject(string what, SceneFile sc, string wantKey)
	{
		bool ok = ContinuumGate.IsUltraAvailable(sc, out string gotKey);
		return CT.True(what + ": отказ с ключом «" + wantKey + "» (получено «" + gotKey + "»)", !ok && gotKey == wantKey);
	}

	private static SceneFile Scene(ArmorPlate.ObjectKind kind, ArmorPlate.ArmorType mat, Projectile.CoreType core, ProjectileClass cls, float angleDeg)
	{
		SceneFile sc = new SceneFile();
		sc.formatVersion = 5;
		sc.name = "T7";
		sc.objects = new List<SavedObject>();
		SavedObject plate = new SavedObject();
		plate.kind = kind.ToString();
		plate.material = mat.ToString();
		plate.posXM = 0f;
		plate.posYM = 0f;
		plate.posZM = 0.5f;
		plate.angleYDeg = angleDeg;
		plate.widthM = 1f;
		plate.heightM = 1f;
		plate.thicknessMM = 50f;
		plate.neraFrontMaterial = "RHA";
		plate.neraRearMaterial = "RHA";
		sc.objects.Add(plate);
		SavedProjectile pr = new SavedProjectile();
		pr.projectileClass = cls.ToString();
		pr.coreType = core.ToString();
		pr.rodLengthMM = 300f;
		pr.diameterMM = 30f;
		pr.velocityMps = 1100f;
		pr.startXM = 0f;
		pr.startYM = 0f;
		pr.startZM = -2f;
		sc.projectile = pr;
		return sc;
	}
}
