public static class SuperShellPreset
{
	public const float PlaceholderFragmentVMS = 1000f;

	public const float PlaceholderCavityFraction = 0.2f;

	public const string ReferenceScene = "эталонная сцена строки — приёмка S28 §4 (NightS28, набор S28FIRE): полое тело ⌀60 × 120 мм, встреча 800 м/с, мишень RHA 8 мм по нормали, шаг частиц 2 мм";

	public const float PlaceholderFuzeDelayS = 0.00018887f;

	public const string Provenance = "Конфигурация-заглушка S27 части А. Все три числа — PLACEHOLDER проекта, в том же смысле, в каком PLACEHOLDER'ом помечены скорости пластин в EraCatalog: V_разл = 1000 м/с (опубликованной скорости разлёта корпуса у проекта нет ни одной), доля полости 0.20 (выбрана под ворота толщины стенки, а не по снаряжению), задержка взрывателя 188.87 мкс (ВЫВЕДЕНА ИЗ ЗАМЕРА на эталонной сцене строки: эталонная сцена строки — приёмка S28 §4 (NightS28, набор S28FIRE): полое тело ⌀60 × 120 мм, встреча 800 м/с, мишень RHA 8 мм по нормали, шаг частиц 2 мм — там она даёт импульс в 47.83 мм ЗА тыльной плоскостью; настоящие донные взрыватели работают на миллисекундах, поэтому пометка PLACEHOLDER остаётся). Рампа заимствована у ДЗ (SuperEra.RampS = 5 мкс, ASSUMED_BY_SUBSTITUTION). Ни одно из этих чисел не является паспортом изделия: с S28 §1.3 они отгружаются игроку ОДНОЙ строкой каталога полых тел, и отгружаются С ЯРЛЫКОМ — и подпись строки, и её подсказка называют себя заглушкой проекта. Именованных полых тел по-прежнему нет.";

	public static SuperShellDrive Drive(float fuzeDelayS, float triggerDistanceM)
	{
		SuperShellDrive drive = new SuperShellDrive();
		drive.initiationDelayS = fuzeDelayS;
		drive.triggerDistanceM = triggerDistanceM;
		drive.velocityMps = 1000f;
		return drive;
	}
}
