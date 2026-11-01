# ArmorSim → iOS: статус реверса

Дата: 2026-10-30

## Что скачано
- Источник: `https://ts.bzzhr.to/d/542b1p38yebn?v=...` (Buzzheavier) — сработало.
- Первая ссылка UploadHaven: `403` (привязка к IP). Вторая (`net.geo.dwnstd.com`): hCaptcha + левый `DS-Setup.exe`, не игру — не использовать.
- Архив: `ArmorSim-SteamRIP.com.rar` (132 МБ, RAR5). Системный `7z 16.02` его НЕ берёт — распакован `/root/opencode-projects/tools/...` свежим `7zz 25.01` (в `/tmp/opencode/7znew/7zz`).
- Распаковка: `extracted/ArmorSim` (222 МБ).

## Что это за сборка
- Unity **6000.3.11f1**, IL2CPP Release, Windows x64. Версия игры 2.2.0.
- Сцены: MainMenu, SampleScene, UltraViewer, SuperViewer, Flat2D, AeroSim, RadarSim.
- Плагины: Steamworks.NET, Burst. ComputeShader x3, Shader x22.
- `GameAssembly.dll` 42 МБ, `global-metadata.dat` 8.9 МБ, **metadata version 39**.

## Инструменты (в /root/opencode-projects/tools)
- `dotnet/` — .NET 8 runtime (local install, не системный).
- `Il2CppDumper/` — v6.7.46. **НЕ работает**: `Metadata file supplied is not a supported version[39]`.
- `AssetRipper/` — 2.0.0 linux x64. **Работает**, включая IL2CPP metadata v39 (встроенный Cpp2IL).
  - Запуск: `./AssetRipper.GUI.Free --headless --port 17865 --log-path /tmp/opencode/assetripper.log`
  - API: `POST /LoadFolder` (form field `Path`), `POST /Export/UnityProject` (form field `Path`), `GET /openapi.json`.

## Результат реверса: reversed/UnityProject (120 МБ)
- `ExportedProject/Assets` — сцenes (.unity), Resources 104 МБ (модели/текстуры/звук/шейдеры), Materials, MonoBehaviour, ProjectSettings, Packages/manifest.json.
- `ExportedProject/Assets/Scripts/Assembly-CSharp` — **810 .cs файлов, ~6693 метода**, 59k строк.
- **ВСЕ тела методов — заглушки** (`return 0f;`, `return null;`). Сохранены имена классов/полей/констант и сигнатуры. Это ограничение IL2CPP: логика живёт только в нативном x64 коде `GameAssembly.dll`.
- `AuxiliaryFiles/GameAssemblies/*.dll` — managed-обёртки (тоже без тел).
- Ключевые классы по именам: Flat2DSolver, SuperShotRunner, SuperSolveJob, SuperKernelGPU, PenetrationPlayer, ArmorPlate, RodGeometry, RadarBenchController, MainMenuUI, UltraOrbitCamera и т.д.

## Этап 2 (2026-10-30): точное восстановление логики
- `Cpp2IL 2022.1.0-pre-release.21` ПОДДЕРЖИВАЕТ metadata v39. Дампы:
  - `reversed/cpp2il/isil/IsilDump/Assembly-CSharp` — 9682 метода: Disassembly + ISIL (71 МБ).
  - `reversed/cpp2il/diffcs/DiffableCs` — сигнатуры + offsets полей.
  - `reversed/cpp2il/isil_addr` + `method_map.csv` (63808 методов с адресами) — собрано патченным Cpp2IL из `tools/Cpp2IL-src` (в IsilDumpOutputFormat добавлена строка Address).
- `tools_decomp/isil.py` — просмотрщик: `python3 tools_decomp/isil.py Type [method] [--asm]`; подставляет float/int/double по адресам, имена полей по offsets, имена методов в Call 0x...
- `port/` — рабочий Unity-проект (копия ExportedProject), в нём восстанавливаем тела.
- `build/Assembly-CSharp.csproj` — проверка компиляции без Unity (references на AuxiliaryFiles/GameAssemblies): `cd build && dotnet build -v q` (dotnet из /root/opencode-projects/tools/dotnet).
- `docs/PORTING.md` — правила точного перевода ISIL→C# для агентов.
- Ghidra 12.2 + JDK25 в tools, проект `reversed/ghidra`, скрипты `tools_decomp/ghidra` (ImportMethods/DecompileMethods).
- Уже восстановлены вручную: AppFlow, UltraSceneRoute, Flat2DRoute, ContinuumRouting, UltraQualityLadder, Flat2DMaterialBlock.
- Ghidra: полный анализ завершён, 9642 функции названы. ВАЖНО: в DecompInterface обязательно `toggleCCode(true); toggleSyntaxTree(true)`, иначе пустой DECOMPILE FAILED. C-псевдокод: `reversed/ghidra/decompiled_all` — ЗАВЕРШЕНО, все 9642 функции.
- `build/refs` — reference DLL для компиляции; `tools/RefPatch` добавляет в dummy `UnityEngine.Mathf` отсутствующие const PI/Deg2Rad/Rad2Deg (IL2CPP их выкинул). csproj ссылается на build/refs.
- Первый агентский батч DONE: RodGeometry, TateModel, GradyModel, RigidPenetration, FragmentBallistics, TrajectoryModel, CraterModel, CraterProfile, NeraArmor, ReactiveArmor, ShapedChargeCore, RodLengthSeries, AxiSolverConst, Complex64.

- Полная C# компиляция всех 810 файлов `port` ПРОХОДИТ (`build/Assembly-CSharp.csproj`, 0 errors). Это не означает отсутствие заглушек: значительная часть методов ещё в работе.
- Каталоги/настройки/локализация — блок из 15 файлов ПОЛНОСТЬЮ DONE (Catalog, RoundCatalog, RodPassports, ScenePresets, SceneDefaults, GameSettings, Localization, Flat2DText, Flat2DGlowPalette, AdvancedMaterials, ContinuumMaterials/Id/Properties, SuperLimits, SuperPreset).
- Flat2D-поддержка: 12 файлов DONE (включая Flat2DFloor — Anchors/Balance/HalfStep/Refine/N4 закрыты, Determinism дополнен вторым Squeeze+Domain по бинарнику).
- MainMenuUI: 120/120 методов, подтверждено агентом (ScrollbarVisibility = AutoHide по нативу).
- MenuTheme: восстановлен целиком вручную (обе палитры, все UI-фабрики, MakeScrollList).
- Активные агенты: PenetrationPlayer (целиком), 3D-ядро (Projectile/RodDynamicsSolver/DeformableMesh/FragmentSpawner/AxiMesh), ArmorPlate.
- `build/refs` дополнен compile-only заглушками `SystemInfo.deviceName/deviceUniqueIdentifier` (реальный Unity их имеет, dummy-сборка — нет). Игровой код не меняется.
- Подготовлены iOS pipeline: `.github/workflows/ios.yml` (macos-15, GameCI Unity export → xcodebuild archive/export → IPA artifact), `port/Assets/Editor/ArmorSimBuild.cs`, инструкция `docs/IOS.md` с нужными secrets. Pipeline пока не прогонялся.

## Вывод
Готового «порта» не будет: нужно восстанавливать ~6.7k методов. Пути:
1. Ghidra + декомпиляция нативных методов по адресам из IL2CPP, ручная переписка на C# — самый точный, очень долгий.
2. Переписать логику заново по скелету/константам (быстрее, но это переизобретение, не 1:1).
Сборка .ipa: только после рабочего Unity-проекта, GitHub Actions macos-раннер + Unity iOS Build Support + Xcode (нужны сертификат/профиль для подписи).
