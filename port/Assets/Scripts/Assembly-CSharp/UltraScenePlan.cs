using System.Collections.Generic;

public static class UltraScenePlan
{
	public const string SceneAssetPath = "Assets/Scenes/UltraViewer.unity";

	public const string MaterialFolder = "Assets/Materials/Ultra";

	public const string FixtureBadgeText = "SYNTHETIC — похоже на нынешний вывод решателя, не физическая истина";

	public static List<UltraNode> Nodes()
	{
		List<UltraNode> list = new List<UltraNode>();
		Add(list, "UltraViewer", UltraNodeKind.Empty, "корень сцены; всё живёт под ним");
		Add(list, "UltraViewer/CameraPivot", UltraNodeKind.Empty, "поворотная точка в точке удара");
		Add(list, "UltraViewer/CameraPivot/Camera", UltraNodeKind.Camera, "камера на плече");
		Add(list, "UltraViewer/Lighting", UltraNodeKind.Empty, "");
		Add(list, "UltraViewer/Lighting/Key", UltraNodeKind.KeyLight, "скользит по стенке кратера");
		Add(list, "UltraViewer/Lighting/Fill", UltraNodeKind.FillLight, "крышка сечения не чёрная");
		Add(list, "UltraViewer/Bodies", UltraNodeKind.Empty, "");
		Add(list, "UltraViewer/Bodies/Projectile", UltraNodeKind.ProjectileMesh, "");
		Add(list, "UltraViewer/Bodies/Projectile/Fins", UltraNodeKind.FinRoot, "оперение; отделяется через FragmentSpawner по событию срыва");
		Add(list, "UltraViewer/Bodies/Target", UltraNodeKind.TargetMesh, "");
		Add(list, "UltraViewer/Bodies/SectionCap", UltraNodeKind.CapMesh, "меридиональная крышка; СВОЙ неосвещаемый материал — см. ниже");
		Add(list, "UltraViewer/Rig", UltraNodeKind.Empty, "");
		Add(list, "UltraViewer/Rig/Viewer", UltraNodeKind.Viewer, "ContinuumViewer + транспорт 5a (UltraViewerUI, БЕЗ кнопки «Огонь» — она слайс 2)");
		Add(list, "UltraViewer/Rig/Viewer/Ghost", UltraNodeKind.BuiltGhost, "контур «как построено» поверх посчитанного; посчитанный меш НЕ поворачивается");
		Add(list, "UltraViewer/Rig/Fixture", UltraNodeKind.FixtureLoader, "провенанс показа и подмена таймлайна; оснастку САМ НЕ ГРУЗИТ");
		Add(list, "UltraViewer/Rig/Driver", UltraNodeKind.ShotDriver, "кнопка расчёта: кэш, очередь, подмена таймлайна; владеет решением");
		Add(list, "UltraViewer/Rig/Exit", UltraNodeKind.Exit, "возврат в меню: Esc и кнопка, с подтверждением поверх идущего расчёта");
		Add(list, "UltraHud", UltraNodeKind.Hud, "Canvas");
		Add(list, "UltraHud/Panel", UltraNodeKind.HudPanel, "контейнер диагностики; ЗАКРЫТ по умолчанию, открывается клавишей");
		Add(list, "UltraHud/Panel/Settings", UltraNodeKind.HudPanel, "");
		Add(list, "UltraHud/Panel/Settings/CacheSize", UltraNodeKind.HudLabel, "");
		Add(list, "UltraHud/Panel/Settings/CacheClear", UltraNodeKind.HudButton, "");
		Add(list, "UltraHud/Panel/Settings/Rungs", UltraNodeKind.HudLabel, "только ступени, помеченные зелёными ВРУЧНУЮ; конусных сегодня нет ни одной");
		Add(list, "UltraHud/Panel/Settings/ModeEntry", UltraNodeKind.HudLabel, "пункт ULTRA: виден и ВЫКЛЮЧЕН с причиной ворот");
		Add(list, "UltraHud/Panel/Settings/GateSet", UltraNodeKind.HudLabel, "именованный набор ворот и три объявленных исключения с причинами");
		Add(list, "UltraHud/Panel/Declaration", UltraNodeKind.HudLabel, "полный текст объявления проекции; пусто при проекции БЕЗ потерь");
		Add(list, "UltraHud/Panel/Field", UltraNodeKind.HudPanel, "");
		Add(list, "UltraHud/Panel/Field/Selector", UltraNodeKind.HudLabel, "пластика/температура/давление");
		Add(list, "UltraHud/Panel/Field/Legend", UltraNodeKind.HudLabel, "диапазон ЭТОГО кадра; абсолютных значений НЕТ (фаза 5b §2)");
		Add(list, "UltraHud/Progress", UltraNodeKind.HudPanel, "доля, прошло, оценка остатка");
		Add(list, "UltraHud/Progress/Fraction", UltraNodeKind.HudLabel, "");
		Add(list, "UltraHud/Progress/Elapsed", UltraNodeKind.HudLabel, "");
		Add(list, "UltraHud/Progress/Eta", UltraNodeKind.HudLabel, "«—» до 200 шагов");
		Add(list, "UltraHud/FixtureBadge", UltraNodeKind.HudLabel, "метка провенанса — обязана быть видна всегда");
		return list;
	}

	private static void Add(List<UltraNode> list, string path, UltraNodeKind kind, string note)
	{
		list.Add(new UltraNode
		{
			path = path,
			kind = kind,
			note = note
		});
	}

	public static bool PathsUnique(List<UltraNode> nodes, out string duplicate)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (UltraNode node in nodes)
		{
			if (!hashSet.Add(node.path))
			{
				duplicate = node.path;
				return false;
			}
		}
		duplicate = null;
		return true;
	}

	public static bool ParentsPrecede(List<UltraNode> nodes, out string orphan)
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (UltraNode node in nodes)
		{
			if (node.ParentPath != null && !hashSet.Contains(node.ParentPath))
			{
				orphan = node.path;
				return false;
			}
			hashSet.Add(node.path);
		}
		orphan = null;
		return true;
	}
}
