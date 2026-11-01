using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SceneSave
{
	public const int CurrentVersion = 5;

	public const int MinReadableVersion = 1;

	public const string Extension = ".armorsim.json";

	public const string FolderName = "Scenes";

	public static string Folder => Path.Combine(Application.persistentDataPath, FolderName);

	public static string PathFor(string sceneName)
	{
		return Path.Combine(Folder, SanitizeFileName(sceneName) + Extension);
	}

	public static string SanitizeFileName(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "scene";
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length);
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (!char.IsLetterOrDigit(c) && c != ' ' && c != '_')
			{
				c = '_';
			}
			stringBuilder.Append(c);
		}
		string text = stringBuilder.ToString().Trim();
		if (text.Length == 0)
		{
			return "scene";
		}
		return text;
	}

	public static SceneFile Capture(string sceneName)
	{
		SceneFile sceneFile = new SceneFile();
		sceneFile.formatVersion = CurrentVersion;
		sceneFile.section = AppFlow.ActiveSection.ToString();
		sceneFile.family = SimFamily.Analytic3D.ToString();
		if (string.IsNullOrEmpty(sceneName))
		{
			sceneName = "Без имени";
		}
		sceneFile.name = sceneName;
		sceneFile.savedUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss'Z'", CultureInfo.InvariantCulture);
		sceneFile.appVersion = Application.version;
		sceneFile.projectile = null;
		ArmorPlate[] array = Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			ArmorPlate armorPlate = array[i];
			if (armorPlate != null && !armorPlate.IsModel)
			{
				sceneFile.objects.Add(CaptureObject(armorPlate));
			}
		}
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile != null)
		{
			sceneFile.projectile = CaptureProjectile(projectile);
		}
		foreach (SandboxModel item in SandboxModel.All())
		{
			if (item == null || item.source == null)
			{
				continue;
			}
			sceneFile.models.Add(CaptureModel(item));
		}
		List<ComputeRegion> list = ComputeRegion.All();
		if (list.Count > 0)
		{
			sceneFile.region = CaptureRegion(list[0]);
		}
		return sceneFile;
	}

	public static SavedModel CaptureModel(SandboxModel sm)
	{
		Vector3 position = sm.transform.position;
		SavedModel savedModel = new SavedModel();
		savedModel.files = new List<ModelSourceFile>(sm.source.files);
		savedModel.contentSha = sm.source.contentSha;
		savedModel.units = sm.source.settings.units.ToString();
		savedModel.up = sm.source.settings.up.ToString();
		savedModel.mirrorX = sm.source.settings.mirrorX;
		savedModel.scale = sm.source.settings.Scale;
		savedModel.posXM = position.x;
		savedModel.posYM = position.y;
		savedModel.posZM = position.z;
		savedModel.angleXDeg = sm.angleX;
		savedModel.angleYDeg = sm.angleY;
		savedModel.liveScale = sm.Scale;
		savedModel.edgeClamp = sm.Plate == null || sm.Plate.edgeClamp;
		sm.SyncPartMaterials();
		foreach (ArmorPlate.ArmorType item in sm.partMaterials)
		{
			savedModel.partMaterials.Add(item.ToString());
		}
		return savedModel;
	}

	public static SavedRegion CaptureRegion(ComputeRegion r)
	{
		Vector3 position = r.transform.position;
		SavedRegion savedRegion = new SavedRegion();
		savedRegion.posXM = position.x;
		savedRegion.posYM = position.y;
		savedRegion.posZM = position.z;
		savedRegion.present = true;
		savedRegion.angleXDeg = r.angleX;
		savedRegion.angleYDeg = r.angleY;
		savedRegion.widthM = r.widthM;
		savedRegion.heightM = r.heightM;
		savedRegion.depthM = r.depthM;
		return savedRegion;
	}

	public static SuperMeshSource SourceOf(SavedModel m)
	{
		ModelImportSettings settings = ModelImportSettings.Default;
		if (Enum.TryParse(m.units, out ModelUnits units))
		{
			settings.units = units;
		}
		if (Enum.TryParse(m.up, out ModelUpAxis up))
		{
			settings.up = up;
		}
		settings.mirrorX = m.mirrorX;
		settings.scale = m.scale;
		SuperMeshSource superMeshSource = new SuperMeshSource();
		superMeshSource.files = new List<ModelSourceFile>(m.files);
		superMeshSource.contentSha = m.contentSha;
		superMeshSource.settings = settings;
		superMeshSource.partCount = m.partMaterials.Count;
		return superMeshSource;
	}

	public static bool ModelsAvailable(SceneFile f, out string why)
	{
		why = null;
		if (f == null || f.models == null)
		{
			return true;
		}
		foreach (SavedModel model in f.models)
		{
			try
			{
				SuperMeshStore.Get(SourceOf(model));
			}
			catch (ModelImportException ex)
			{
				why = ex.Message;
				return false;
			}
			catch (Exception ex2)
			{
				why = "модель не читается: " + ex2.Message;
				return false;
			}
		}
		return true;
	}

	public static SavedObject CaptureObject(ArmorPlate p)
	{
		Vector3 position = p.transform.position;
		SavedObject savedObject = new SavedObject();
		savedObject.kind = p.kind.ToString();
		savedObject.material = p.armorType.ToString();
		savedObject.posXM = position.x;
		savedObject.posYM = position.y;
		savedObject.posZM = position.z;
		savedObject.angleXDeg = p.angleX;
		savedObject.angleYDeg = p.angleY;
		savedObject.widthM = p.widthM;
		savedObject.heightM = p.heightM;
		savedObject.thicknessMM = p.thicknessMM;
		savedObject.eraPlateThicknessMM = p.eraPlateThicknessMM;
		savedObject.eraPlateVelocityMS = p.eraPlateVelocityMS;
		savedObject.eraInterlayerGapMM = p.eraInterlayerGapMM;
		savedObject.eraCassetteExtentMM = p.eraCassetteExtentMM;
		savedObject.neraFrontThicknessMM = p.neraFrontThicknessMM;
		savedObject.neraRearThicknessMM = p.neraRearThicknessMM;
		savedObject.neraInterlayerMM = p.neraInterlayerMM;
		savedObject.neraMountAngleDeg = p.neraMountAngleDeg;
		savedObject.neraExtentMM = p.neraExtentMM;
		savedObject.neraImpulsePerArea = p.neraImpulsePerArea;
		savedObject.neraFrontMaterial = p.neraFrontMaterial.ToString();
		savedObject.neraRearMaterial = p.neraRearMaterial.ToString();
		savedObject.neraInterlayer = p.neraInterlayer.ToString();
		savedObject.neraBonding = p.neraBonding.ToString();
		savedObject.slatBarWidthMM = p.slatBarWidthMM;
		savedObject.slatPitchMM = p.slatPitchMM;
		savedObject.slatDudFraction = p.slatDudFraction;
		savedObject.blockMaterial = p.blockMaterial.ToString();
		savedObject.voidFraction = p.voidFraction;
		savedObject.linerMaterial = p.linerMaterial.ToString();
		savedObject.presetId = p.presetId ?? "";
		savedObject.edgeClamp = p.edgeClamp;
		return savedObject;
	}

	public static SavedProjectile CaptureProjectile(Projectile pr)
	{
		ProjectileDefinition projectileDefinition = pr.GetDefinition();
		Vector3 start = AimShot.SavedStartWorld(pr);
		SavedProjectile savedProjectile = new SavedProjectile();
		savedProjectile.catalogId = Catalog.ProjectileIdFor(projectileDefinition) ?? "";
		savedProjectile.roundId = RoundIdFor(projectileDefinition);
		savedProjectile.displayName = projectileDefinition.name ?? "";
		savedProjectile.projectileClass = projectileDefinition.kind.ToString();
		savedProjectile.coreType = pr.coreType.ToString();
		savedProjectile.rodLengthMM = pr.rodLengthMM;
		savedProjectile.diameterMM = pr.diameter;
		savedProjectile.velocityMps = pr.flightSpeed;
		savedProjectile.coneDiameterMM = projectileDefinition.coneDiameterMM;
		savedProjectile.fuzeStandoffMM = projectileDefinition.fuzeStandoffMM;
		savedProjectile.isTandem = projectileDefinition.isTandem;
		savedProjectile.precursorConeDiameterMM = projectileDefinition.precursorConeDiameterMM;
		savedProjectile.nose = projectileDefinition.nose.ToString();
		savedProjectile.segments = projectileDefinition.segments;
		savedProjectile.strands = projectileDefinition.strands;
		savedProjectile.finBlades = projectileDefinition.finBlades;
		savedProjectile.noFins = projectileDefinition.noFins;
		savedProjectile.gunCalibreMM = projectileDefinition.gunCalibreMM;
		savedProjectile.startXM = start.x;
		savedProjectile.startYM = start.y;
		savedProjectile.startZM = start.z;
		return savedProjectile;
	}

	public static string RoundIdFor(ProjectileDefinition d)
	{
		if (d == null || string.IsNullOrEmpty(d.name))
		{
			return "";
		}
		foreach (RoundSpec round in RoundCatalog.All)
		{
			if (d.name == round.displayName)
			{
				return round.id;
			}
		}
		return "";
	}

	public static string ToJson(SceneFile f)
	{
		return JsonUtility.ToJson(f, true);
	}

	public static SceneLoadResult Parse(string json)
	{
		if (string.IsNullOrEmpty(json))
		{
			return Refuse(SceneLoadStatus.RefusedMalformed, "Пустой файл.");
		}
		try
		{
			SceneFile sceneFile = JsonUtility.FromJson<SceneFile>(json);
			if (sceneFile != null)
			{
				return Accept(sceneFile);
			}
			return Refuse(SceneLoadStatus.RefusedMalformed, "Не разбирается как сцена ArmorSim.");
		}
		catch (Exception ex)
		{
			return Refuse(SceneLoadStatus.RefusedMalformed, "Не разбирается как JSON: " + ex.Message);
		}
	}

	public static SceneLoadResult Accept(SceneFile f)
	{
		if (f == null)
		{
			return Refuse(SceneLoadStatus.RefusedMalformed, "Пустая сцена.");
		}
		if (f.formatVersion <= 0)
		{
			return Refuse(SceneLoadStatus.RefusedNoVersion, "В файле нет номера формата. Загружать нечем: без номера неизвестно, что означают записанные числа (умолчания проекта уже переезжали — плита 3 м → 1 м, зазор 50 → 100 мм).");
		}
		if (f.formatVersion > CurrentVersion)
		{
			return Refuse(SceneLoadStatus.RefusedNewer, string.Format("Файл формата {0}, эта сборка знает до {1}. ", f.formatVersion, CurrentVersion) + "Загрузка отменена — читать новый формат старыми правилами нельзя.");
		}
		if (f.formatVersion < MinReadableVersion)
		{
			return Refuse(SceneLoadStatus.RefusedNoMigration, string.Format("Формат {0} ниже минимально читаемого ({1}). ", f.formatVersion, MinReadableVersion) + "Правил подъёма для него нет.");
		}
		List<string> list = new List<string>();
		bool flag = false;
		while (f.formatVersion < CurrentVersion)
		{
			if (!MigrateStep(f, f.formatVersion, out string what))
			{
				return Refuse(SceneLoadStatus.RefusedNoMigration, string.Format("Формат {0} → {1}: правило подъёма не описано. Загрузка отменена — ", f.formatVersion, f.formatVersion + 1) + "загружать «как получится» запрещено (§4.5 ТЗ).");
			}
			f.formatVersion++;
			flag = true;
			list.Add(what);
		}
		string text = Validate(f);
		if (text != null)
		{
			return Refuse(SceneLoadStatus.RefusedMalformed, text);
		}
		return new SceneLoadResult
		{
			status = (flag ? SceneLoadStatus.Migrated : SceneLoadStatus.Ok),
			file = f,
			message = (flag ? string.Format("Файл поднят с формата {0} до {1}: ", CurrentVersion - list.Count, CurrentVersion) + string.Join("; ", list.ToArray()) : "")
		};
	}

	private static bool MigrateStep(SceneFile f, int from, out string what)
	{
		what = "";
		switch (from)
		{
		case 1:
			f.section = TargetSection.Hull.ToString();
			what = "раздел цели не записан ⇒ корпус (крыши в формате 1 не было)";
			return true;
		case 2:
			f.family = SimFamily.Analytic3D.ToString();
			what = "семейство не записано ⇒ Analytic3D («Плоскости» в формате 2 не было)";
			return true;
		case 3:
		{
			int num = 0;
			int num2 = 0;
			string text = ArmorPlate.ObjectKind.Nera.ToString();
			foreach (SavedObject obj in f.objects)
			{
				if (obj == null)
				{
					continue;
				}
				if (obj.kind != text)
				{
					continue;
				}
				num2++;
				if (string.IsNullOrEmpty(obj.neraBonding))
				{
					continue;
				}
				if (!NeraCatalog.TryGet(obj.neraBonding, out NeraPreset preset))
				{
					continue;
				}
				obj.neraBonding = preset.bonding.ToString();
				num++;
			}
			what = "связанность пакетов НДЗ не записана ⇒ восстановлена из строки каталога " + string.Format("({0} из {1}; без строки — «не названа», как и было в памяти)", num, num2);
			return true;
		}
		case 4:
		{
			int num3 = 0;
			foreach (SavedObject obj2 in f.objects)
			{
				if (obj2 == null)
				{
					continue;
				}
				obj2.edgeClamp = true;
				num3++;
			}
			if (f.models == null)
			{
				f.models = new List<SavedModel>();
			}
			f.models.Clear();
			f.region = new SavedRegion();
			f.region.present = false;
			what = string.Format("закрепление краёв не записано ⇒ закреплены все ({0}; до S40 Super закреплял каждую плиту); ", num3) + "моделей и области счёта в формате 4 не было";
			return true;
		}
		default:
			return false;
		}
	}

	public static TargetSection SectionOf(SceneFile f)
	{
		if (f != null && Enum.TryParse(f.section, out TargetSection result))
		{
			return result;
		}
		return TargetSection.Hull;
	}

	public static SimFamily FamilyOf(SceneFile f)
	{
		if (f != null && Enum.TryParse(f.family, out SimFamily result))
		{
			return result;
		}
		return SimFamily.Analytic3D;
	}

	public static string Validate(SceneFile f)
	{
		if (f.objects == null)
		{
			return "В файле нет списка объектов.";
		}
		for (int i = 0; i < f.objects.Count; i++)
		{
			SavedObject savedObject = f.objects[i];
			if (savedObject == null)
			{
				return string.Format("Объект #{0}: пустая запись.", i);
			}
			if (!Enum.TryParse(savedObject.kind, out ArmorPlate.ObjectKind kind))
			{
				return string.Format("Объект #{0}: неизвестный вид «{1}».", i, savedObject.kind);
			}
			if (!Enum.TryParse(savedObject.material, out ArmorPlate.ArmorType type))
			{
				return string.Format("Объект #{0}: неизвестный материал «{1}».", i, savedObject.material);
			}
			if (savedObject.widthM <= 0f || savedObject.heightM <= 0f)
			{
				return string.Format("Объект #{0}: грань {1}×{2} м — размер обязан быть записан явно.", i, savedObject.widthM, savedObject.heightM);
			}
			if (savedObject.thicknessMM <= 0f)
			{
				return string.Format("Объект #{0}: толщина {1} мм — размер обязан быть записан явно.", i, savedObject.thicknessMM);
			}
		}
		if (f.models != null)
		{
			for (int j = 0; j < f.models.Count; j++)
			{
				SavedModel savedModel = f.models[j];
				if (savedModel == null)
				{
					return string.Format("Модель #{0}: пустая запись.", j);
				}
				if (savedModel.files == null || savedModel.files.Count == 0)
				{
					return string.Format("Модель #{0}: не записано ни одного файла.", j);
				}
				if (string.IsNullOrEmpty(savedModel.contentSha))
				{
					return string.Format("Модель #{0}: не записан отпечаток содержимого.", j);
				}
				if (!Enum.TryParse(savedModel.units, out ModelUnits units))
				{
					return string.Format("Модель #{0}: неизвестные единицы «{1}».", j, savedModel.units);
				}
				if (!Enum.TryParse(savedModel.up, out ModelUpAxis up))
				{
					return string.Format("Модель #{0}: неизвестная ось «{1}».", j, savedModel.up);
				}
				if (float.IsNaN(savedModel.scale) || float.IsInfinity(savedModel.scale) || savedModel.scale < 0f || (savedModel.scale > 0f && (savedModel.scale < 0.001f || savedModel.scale > 1000f)))
				{
					return string.Format("Модель #{0}: масштаб {1} вне пределов {2}…{3}.", j, savedModel.scale, 0.001f, 1000f);
				}
				if (float.IsNaN(savedModel.liveScale) || float.IsInfinity(savedModel.liveScale) || savedModel.liveScale < 0f || (savedModel.liveScale > 0f && (savedModel.liveScale < 0.001f || savedModel.liveScale > 1000f)))
				{
					return string.Format("Модель #{0}: живой масштаб {1} вне пределов {2}…{3}.", j, savedModel.liveScale, 0.001f, 1000f);
				}
				foreach (string partMaterial in savedModel.partMaterials)
				{
					if (!Enum.TryParse(partMaterial, out ArmorPlate.ArmorType partType))
					{
						return string.Format("Модель #{0}: неизвестное вещество части «{1}».", j, partMaterial);
					}
				}
			}
		}
		if (f.region != null && f.region.present && (f.region.widthM <= 0f || f.region.heightM <= 0f || f.region.depthM <= 0f))
		{
			return string.Format("Область счёта {0}×{1}×{2} м — размер обязан быть записан явно.", f.region.widthM, f.region.heightM, f.region.depthM);
		}
		SavedProjectile savedProjectile = f.projectile;
		if (savedProjectile == null)
		{
			return "В файле нет снаряда.";
		}
		if (!Enum.TryParse(savedProjectile.coreType, out Projectile.CoreType coreType))
		{
			return "Снаряд: неизвестный материал сердечника «" + savedProjectile.coreType + "».";
		}
		if (!Enum.TryParse(savedProjectile.projectileClass, out ProjectileClass projectileClass))
		{
			return "Снаряд: неизвестный класс «" + savedProjectile.projectileClass + "».";
		}
		if (!Enum.TryParse(savedProjectile.nose, out NoseShape nose))
		{
			return "Снаряд: неизвестная форма носа «" + savedProjectile.nose + "».";
		}
		if (savedProjectile.rodLengthMM <= 0f || savedProjectile.diameterMM <= 0f || savedProjectile.velocityMps <= 0f)
		{
			return string.Format("Снаряд: L={0} мм, ⌀={1} мм, v={2} м/с — ", savedProjectile.rodLengthMM, savedProjectile.diameterMM, savedProjectile.velocityMps) + "все три обязаны быть записаны явно.";
		}
		return null;
	}

	private static SceneLoadResult Refuse(SceneLoadStatus st, string msg)
	{
		return new SceneLoadResult
		{
			status = st,
			file = null,
			message = msg
		};
	}

	public static Vector3 WorldFromM(float xM, float yM, float zM)
	{
		return new Vector3(xM, yM, zM);
	}

	public static void ApplyObject(SavedObject s, ArmorPlate p)
	{
		if (s == null || p == null)
		{
			return;
		}
		if (Enum.TryParse(s.kind, out ArmorPlate.ObjectKind kind))
		{
			p.SetKind(kind);
		}
		if (Enum.TryParse(s.material, out ArmorPlate.ArmorType type))
		{
			p.SetArmorType(type);
		}
		p.SetWidth(s.widthM);
		p.SetHeight(s.heightM);
		p.SetThickness(s.thicknessMM);
		p.SetAngleX(s.angleXDeg);
		p.SetAngleY(s.angleYDeg);
		p.transform.position = WorldFromM(s.posXM, s.posYM, s.posZM);
		if (p.IsEraPlate)
		{
			p.SetEraPlateThickness(s.eraPlateThicknessMM);
			p.SetEraPlateVelocity(s.eraPlateVelocityMS);
			p.SetEraInterlayerGap(s.eraInterlayerGapMM);
			p.SetEraCassetteExtent(s.eraCassetteExtentMM);
		}
		if (p.IsNeraPlate)
		{
			p.SetNeraFrontThickness(s.neraFrontThicknessMM);
			p.SetNeraRearThickness(s.neraRearThicknessMM);
			p.SetNeraInterlayer(s.neraInterlayerMM);
			p.SetNeraMountAngle(s.neraMountAngleDeg);
			p.SetNeraExtent(s.neraExtentMM);
			if (s.neraImpulsePerArea > 0f)
			{
				p.neraImpulsePerArea = s.neraImpulsePerArea;
			}
			if (Enum.TryParse(s.neraFrontMaterial, out ArmorPlate.ArmorType frontMaterial))
			{
				p.SetNeraFrontMaterial(frontMaterial);
			}
			if (Enum.TryParse(s.neraRearMaterial, out ArmorPlate.ArmorType rearMaterial))
			{
				p.SetNeraRearMaterial(rearMaterial);
			}
			if (Enum.TryParse(s.neraInterlayer, out InterlayerMaterial interlayer))
			{
				p.neraInterlayer = interlayer;
			}
			if (Enum.TryParse(s.neraBonding, out NeraBonding bonding))
			{
				p.SetNeraBonding(bonding);
			}
		}
		if ((p.IsEraPlate || p.IsNeraPlate) && !string.IsNullOrEmpty(s.presetId))
		{
			p.presetId = s.presetId;
		}
		if (p.IsSlat)
		{
			if (s.slatBarWidthMM > 0f)
			{
				p.slatBarWidthMM = s.slatBarWidthMM;
			}
			if (s.slatPitchMM > 0f)
			{
				p.slatPitchMM = s.slatPitchMM;
			}
			p.slatDudFraction = s.slatDudFraction;
		}
		if (p.IsEngineBlock)
		{
			if (Enum.TryParse(s.blockMaterial, out StandoffDefences.BlockMaterial blockMaterial))
			{
				p.blockMaterial = blockMaterial;
			}
			p.voidFraction = s.voidFraction;
		}
		if (p.IsSpallLiner)
		{
			if (Enum.TryParse(s.linerMaterial, out SpallLinerMaterial linerMaterial))
			{
				p.linerMaterial = linerMaterial;
			}
		}
		p.edgeClamp = s.edgeClamp;
	}

	public static void ApplyProjectile(SavedProjectile s, Projectile pr)
	{
		if (s == null || pr == null)
		{
			return;
		}
		ProjectileDefinition projectileDefinition = null;
		if (!string.IsNullOrEmpty(s.roundId) && RoundCatalog.TryGet(s.roundId, out RoundSpec spec))
		{
			projectileDefinition = RoundCatalog.MakeDefinition(spec);
		}
		if (projectileDefinition == null && !string.IsNullOrEmpty(s.catalogId))
		{
			projectileDefinition = Catalog.MakeProjectile(s.catalogId);
		}
		if (projectileDefinition == null)
		{
			projectileDefinition = ProjectileLibrary.Default();
		}
		ProjectileDefinition projectileDefinition2 = projectileDefinition.Clone();
		if (!string.IsNullOrEmpty(s.displayName))
		{
			projectileDefinition2.name = s.displayName;
		}
		if (Enum.TryParse(s.projectileClass, out ProjectileClass projectileClass))
		{
			projectileDefinition2.kind = projectileClass;
		}
		if (Enum.TryParse(s.coreType, out Projectile.CoreType coreType))
		{
			projectileDefinition2.coreType = coreType;
		}
		if (Enum.TryParse(s.nose, out NoseShape nose))
		{
			projectileDefinition2.nose = nose;
		}
		projectileDefinition2.rodLengthMM = s.rodLengthMM;
		projectileDefinition2.diameterMM = s.diameterMM;
		projectileDefinition2.velocityMps = s.velocityMps;
		if (s.coneDiameterMM > 0f)
		{
			projectileDefinition2.coneDiameterMM = s.coneDiameterMM;
		}
		if (s.fuzeStandoffMM > 0f)
		{
			projectileDefinition2.fuzeStandoffMM = s.fuzeStandoffMM;
		}
		projectileDefinition2.isTandem = s.isTandem;
		if (s.precursorConeDiameterMM > 0f)
		{
			projectileDefinition2.precursorConeDiameterMM = s.precursorConeDiameterMM;
		}
		if (s.segments > 0)
		{
			projectileDefinition2.segments = s.segments;
		}
		if (s.strands > 0)
		{
			projectileDefinition2.strands = s.strands;
		}
		if (s.finBlades > 0)
		{
			projectileDefinition2.finBlades = s.finBlades;
		}
		projectileDefinition2.noFins = s.noFins;
		projectileDefinition2.gunCalibreMM = s.gunCalibreMM;
		pr.ApplyDefinition(projectileDefinition2);
		pr.transform.position = WorldFromM(s.startXM, s.startYM, s.startZM);
	}

	public static int ApplyToScene(SceneFile f)
	{
		if (f == null || f.objects == null)
		{
			return 0;
		}
		ArmorBuilder armorBuilder = Object.FindFirstObjectByType<ArmorBuilder>();
		if (armorBuilder == null)
		{
			Debug.LogWarning("SceneSave: в сцене нет ArmorBuilder — загружать нечем.");
			return 0;
		}
		armorBuilder.ClearForLoad();
		int num = 0;
		foreach (SavedObject obj in f.objects)
		{
			if (obj == null || !Enum.TryParse(obj.kind, out ArmorPlate.ObjectKind kind))
			{
				continue;
			}
			ArmorPlate armorPlate = armorBuilder.AddObjectAt(kind, WorldFromM(obj.posXM, obj.posYM, obj.posZM));
			if (armorPlate == null)
			{
				continue;
			}
			ApplyObject(obj, armorPlate);
			num++;
		}
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile != null && f.projectile != null)
		{
			ApplyProjectile(f.projectile, projectile);
		}
		foreach (SandboxModel item in SandboxModel.All())
		{
			if (item == null)
			{
				continue;
			}
			item.gameObject.SetActive(false);
			Object.Destroy(item.gameObject);
		}
		foreach (ComputeRegion item2 in ComputeRegion.All())
		{
			if (item2 == null)
			{
				continue;
			}
			item2.gameObject.SetActive(false);
			Object.Destroy(item2.gameObject);
		}
		if (f.models != null)
		{
			foreach (SavedModel model in f.models)
			{
				PreparedModel prepared = SuperMeshStore.Get(SourceOf(model));
				SandboxModel sandboxModel = SandboxModel.Create(prepared, WorldFromM(model.posXM, model.posYM, model.posZM));
				sandboxModel.SetAngles(model.angleXDeg, model.angleYDeg);
				for (int i = 0; i < model.partMaterials.Count && i < sandboxModel.partMaterials.Count; i++)
				{
					if (Enum.TryParse(model.partMaterials[i], out ArmorPlate.ArmorType type))
					{
						sandboxModel.SetPartMaterial(i, type);
					}
				}
				if (sandboxModel.Plate != null)
				{
					sandboxModel.Plate.SetModelScale((model.liveScale > 0f) ? model.liveScale : 1f);
					sandboxModel.Plate.edgeClamp = model.edgeClamp;
				}
			}
		}
		if (f.region != null && f.region.present)
		{
			ComputeRegion computeRegion = ComputeRegion.Create(WorldFromM(f.region.posXM, f.region.posYM, f.region.posZM));
			computeRegion.SetSize(f.region.widthM, f.region.heightM, f.region.depthM);
			computeRegion.SetAngles(f.region.angleXDeg, f.region.angleYDeg);
		}
		Debug.Log(string.Format("[SceneSave] Загружено «{0}»: объектов {1}", f.name, num) + (string.IsNullOrEmpty(f.provenanceNote) ? "." : ". " + f.provenanceNote));
		return num;
	}

	private static void EnsureFolder()
	{
		if (!Directory.Exists(Folder))
		{
			Directory.CreateDirectory(Folder);
		}
	}

	public static List<string> ListUserScenes()
	{
		List<string> list = new List<string>();
		if (!Directory.Exists(Folder))
		{
			Directory.CreateDirectory(Folder);
		}
		string[] array = Directory.GetFiles(Folder, "*" + Extension);
		try
		{
			string[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				string path = array2[i];
				string fileName = Path.GetFileName(path);
				list.Add(fileName.Substring(0, fileName.Length - Extension.Length));
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("SceneSave: каталог сцен недоступен: " + ex.Message);
		}
		list.Sort(StringComparer.CurrentCultureIgnoreCase);
		return list;
	}

	public static bool WriteToDisk(SceneFile f, out string message)
	{
		if (f == null)
		{
			message = "Нечего сохранять.";
			return false;
		}
		try
		{
			f.readOnly = false;
			f.formatVersion = CurrentVersion;
			if (!Directory.Exists(Folder))
			{
				Directory.CreateDirectory(Folder);
			}
			string path = PathFor(f.name);
			string contents = JsonUtility.ToJson(f, true);
			File.WriteAllText(path, contents, Encoding.UTF8);
			message = string.Format("Сохранено: {0} (формат {1}, объектов {2}).", f.name, f.formatVersion, f.objects.Count);
			Debug.Log("[SceneSave] " + message);
			return true;
		}
		catch (Exception ex)
		{
			message = "Не удалось сохранить: " + ex.Message;
			return false;
		}
	}

	public static SceneLoadResult ReadFromDisk(string sceneName)
	{
		string path = PathFor(sceneName);
		if (!File.Exists(path))
		{
			return Refuse(SceneLoadStatus.RefusedMalformed, "Файла «" + sceneName + "» нет.");
		}
		try
		{
			return Parse(File.ReadAllText(path, Encoding.UTF8));
		}
		catch (Exception ex)
		{
			return Refuse(SceneLoadStatus.RefusedMalformed, "Не удалось прочитать: " + ex.Message);
		}
	}

	public static bool DeleteScene(string sceneName)
	{
		try
		{
			string path = PathFor(sceneName);
			if (!File.Exists(path))
			{
				return false;
			}
			File.Delete(path);
			Debug.Log("[SceneSave] Удалено: " + sceneName);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("SceneSave: не удалось удалить: " + ex.Message);
			return false;
		}
	}

	public static bool RenameScene(string oldName, string newName, out string message)
	{
		if (string.IsNullOrEmpty(newName))
		{
			message = "Пустое имя.";
			return false;
		}
		SceneLoadResult sceneLoadResult = ReadFromDisk(oldName);
		if (sceneLoadResult.status > SceneLoadStatus.Migrated)
		{
			message = sceneLoadResult.message;
			return false;
		}
		sceneLoadResult.file.name = newName;
		if (!WriteToDisk(sceneLoadResult.file, out message))
		{
			return false;
		}
		if (SanitizeFileName(oldName) != SanitizeFileName(newName))
		{
			DeleteScene(oldName);
		}
		message = "Переименовано: " + oldName + " → " + newName + ".";
		return true;
	}
}
