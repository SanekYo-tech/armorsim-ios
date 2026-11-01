using System;
using System.Collections.Generic;
using UnityEngine;

public class ArmorPlate : MonoBehaviour
{
	public enum ArmorType
	{
		RHA = 0,
		HHA = 1,
		Aluminum = 2,
		Titanium = 3,
		Ceramic = 4,
		Tungsten = 5,
		ERA = 6,
		Textolite = 7,
		ERAHeavy = 8,
		BoronCarbide = 9,
		TitaniumDiboride = 10,
		DepletedUraniumArmour = 11,
		Alumina = 12,
		CastSteel = 13,
		RhaSetB = 14,
		Steel4340 = 15,
		Polyurethane = 16,
		Neoprene = 17,
		NaturalRubber = 18
	}

	public enum ObjectKind
	{
		Plate = 0,
		Screen = 1,
		Slat = 2,
		EngineBlock = 3,
		Era = 4,
		Nera = 5,
		SpallLiner = 6,
		Crew = 7,
		Model = 8,
		Rod = 9
	}

	[Header("Параметры этой пластины")]
	public ObjectKind kind;

	public ArmorType armorType;

	public const float DefaultThicknessMM = 50f;

	public float thicknessMM;

	public const float DefaultWidthM = 1f;

	public const float DefaultHeightM = 1f;

	public float widthM;

	public float heightM;

	public float angleY;

	public float angleX;

	public bool edgeClamp;

	[Header("ДЗ: параметры кассеты (единый набор для обеих ветвей)")]
	public float eraPlateThicknessMM;

	public float eraPlateVelocityMS;

	public float eraInterlayerGapMM;

	public const float EraSweepExtentMM = 130f;

	public const float EraLongSideMM = 250f;

	public float eraCassetteExtentMM;

	[Header("НДЗ: пакет пластин")]
	public float neraFrontThicknessMM;

	public float neraRearThicknessMM;

	public ArmorType neraFrontMaterial;

	public ArmorType neraRearMaterial;

	public float neraInterlayerMM;

	public InterlayerMaterial neraInterlayer;

	public float neraMountAngleDeg;

	public NeraBonding neraBonding;

	public float neraExtentMM;

	public float neraImpulsePerArea;

	public string presetId;

	[Header("ДЗ: состояние")]
	public bool cassetteExpended;

	[Header("Подбой (противоосколочный)")]
	public SpallLinerMaterial linerMaterial;

	[Header("Моторный отсек: повреждение осколками")]
	public int engineFragmentHits;

	public float engineDepositedJ;

	public float engineDamage;

	[Header("Экипаж: состояние")]
	public float crewHitPoints;

	public int crewFragmentHits;

	public float crewDepositedJ;

	public int crewDirectHits;

	public int linerDirectHits;

	[Header("Экипаж: по органам")]
	public float[] crewOrganJ;

	public int crewOrganHits;

	public int crewPassThroughs;

	public int crewBoxMisses;

	[Header("Решётчатый экран")]
	public float slatBarWidthMM;

	public float slatPitchMM;

	[Range(0f, 1f)]
	public float slatDudFraction;

	[Header("Моторный отсек")]
	public StandoffDefences.BlockMaterial blockMaterial;

	[Range(0f, 0.9f)]
	public float voidFraction;

	public float modelScale;

	public const float ModelScaleMin = 0.001f;

	public const float ModelScaleMax = 1000f;

	public const float NeraPlateThicknessMinMM = 1f;

	public const float NeraPlateThicknessMaxMM = 12f;

	public const float NeraInterlayerMinMM = 1f;

	public const float NeraInterlayerMaxMM = 40f;

	public const float NeraMountAngleMinDeg = 0f;

	public const float NeraMountAngleMaxDeg = 60f;

	public const float NeraExtentMinMM = 20f;

	public const float NeraExtentMaxMM = 500f;

	private Color baseColor;

	private Color verdictColor;

	private bool baseColorSet;

	private bool hasVerdict;

	public int CassetteCount => 1;

	public bool IsScreen => kind == ObjectKind.Screen;

	public bool IsSlat => kind == ObjectKind.Slat;

	public bool IsEngineBlock => kind == ObjectKind.EngineBlock;

	public bool IsEraPlate => kind == ObjectKind.Era;

	public bool IsNeraPlate => kind == ObjectKind.Nera;

	public bool PrecursorCanArmThis => ShotRouting.Fires(ShotBranch.PrecursorArms, kind, ThreatClass.TandemShapedCharge);

	public bool IsSpallLiner => kind == ObjectKind.SpallLiner;

	public bool IsCrew => kind == ObjectKind.Crew;

	public bool IsModel => kind == ObjectKind.Model;

	public bool IsRod => kind == ObjectKind.Rod;

	public float RodDiameterMM => thicknessMM;

	public bool IsBarrier => ShotRouting.Resists(kind);

	public bool RegistersDirectHit => ShotRouting.Registers(kind);

	public float LinerThicknessM => thicknessMM / 1000f;

	private bool PaintsChildren => kind == ObjectKind.Model || kind == ObjectKind.Rod;

	public ArmorPlate()
	{
		thicknessMM = 50f;
		widthM = 1f;
		heightM = 1f;
		edgeClamp = true;
		eraPlateThicknessMM = 2.5f;
		eraPlateVelocityMS = 700f;
		eraInterlayerGapMM = 20f;
		eraCassetteExtentMM = 130f;
		neraFrontThicknessMM = 3f;
		neraRearThicknessMM = 5f;
		neraInterlayerMM = 6f;
		neraExtentMM = 250f;
		neraImpulsePerArea = 4710f;
		presetId = "";
		crewHitPoints = 100f;
		crewOrganJ = new float[17];
		slatBarWidthMM = 12f;
		slatPitchMM = 100f;
		slatDudFraction = 0.5f;
		voidFraction = 0.55f;
		modelScale = 1f;
	}

	public int CassetteIndexAt(Vector3 worldPoint)
	{
		if (kind == ObjectKind.Era)
		{
			return 0;
		}
		return (kind == ObjectKind.Nera) ? 0 : (-1);
	}

	public bool IsCassetteSpent(int idx)
	{
		return cassetteExpended;
	}

	public void SpendCassette(int idx)
	{
		cassetteExpended = true;
	}

	public void ResetCassettes()
	{
		cassetteExpended = false;
	}

	public float ScreenStandoffMM(Vector3 shotDir)
	{
		if (shotDir.sqrMagnitude < 1e-09f)
		{
			shotDir = Vector3.right;
		}
		shotDir = shotDir.normalized;
		float num = float.MaxValue;
		RaycastHit[] array = Physics.RaycastAll(transform.position, shotDir, 500f);
		for (int i = 0; i < array.Length; i++)
		{
			ArmorPlate component = array[i].collider.GetComponent<ArmorPlate>();
			if (component == null || component == this || component.kind == ObjectKind.Screen || !ShotRouting.Resists(component.kind))
			{
				continue;
			}
			if (array[i].distance < num)
			{
				num = array[i].distance;
			}
		}
		if (num < float.MaxValue)
		{
			return SceneScale.ToMM(num);
		}
		return -1f;
	}

	public void SetScreenStandoffMM(float mm, Vector3 shotDir)
	{
		float num = ScreenStandoffMM(shotDir);
		if (num < 0f)
		{
			return;
		}
		if (shotDir.sqrMagnitude < 1e-09f)
		{
			shotDir = Vector3.right;
		}
		Vector3 position = transform.position;
		shotDir = shotDir.normalized;
		float num2 = SceneScale.FromMM(num - mm);
		transform.position = position + shotDir * num2;
	}

	public float SubstanceThicknessMM(float losThicknessMM)
	{
		if (kind != ObjectKind.EngineBlock)
		{
			return losThicknessMM;
		}
		return StandoffDefences.EffectiveThicknessMM(losThicknessMM, voidFraction);
	}

	public static bool IsEra(ArmorType t)
	{
		if (t != ArmorType.ERA)
		{
			return t == ArmorType.ERAHeavy;
		}
		return true;
	}

	public static bool PrecursorCanArm(ObjectKind k)
	{
		return ShotRouting.Fires(ShotBranch.PrecursorArms, k, ThreatClass.TandemShapedCharge);
	}

	public void SetModelScale(float s)
	{
		if (s > 0f && !float.IsInfinity(s))
		{
			modelScale = Mathf.Clamp(s, ModelScaleMin, ModelScaleMax);
			ApplyVisualThickness();
		}
	}

	public static bool IsBarrierKind(ObjectKind k)
	{
		return ShotRouting.Resists(k);
	}

	public void RegisterEngineFragment(float depositedJ)
	{
		if (kind == ObjectKind.EngineBlock && 0f < depositedJ)
		{
			engineFragmentHits++;
			engineDepositedJ = depositedJ + engineDepositedJ;
			EngineState engineState = EngineDamage.StateOf(engineDamage);
			engineDamage = EngineDamage.Accumulate(engineDamage, depositedJ);
			EngineState engineState2 = EngineDamage.StateOf(engineDamage);
			if (engineState2 != engineState)
			{
				Debug.Log("◈ " + EngineDamage.Report(engineFragmentHits, engineDepositedJ, engineDamage));
				hasVerdict = true;
				verdictColor = EngineDamage.StateColor(engineState2);
				ApplyDisplayColor();
			}
		}
	}

	public float CrewOrganEnergy(CrewOrgan o)
	{
		if (crewOrganJ != null && (int)o >= 0 && (int)o < crewOrganJ.Length)
		{
			return crewOrganJ[(int)o];
		}
		return 0f;
	}

	public bool CrewOrganWasHit(CrewOrgan o)
	{
		if (crewOrganJ != null && (int)o >= 0 && (int)o < crewOrganJ.Length)
		{
			return crewOrganJ[(int)o] > 0f;
		}
		return false;
	}

	private void EnsureOrganArray()
	{
		if (crewOrganJ == null || crewOrganJ.Length != 17)
		{
			crewOrganJ = new float[17];
		}
	}

	public void RegisterCrewTrace(CrewTraceResult r)
	{
		if (kind != ObjectKind.Crew)
		{
			return;
		}
		if (!r.hitBody)
		{
			crewBoxMisses++;
			return;
		}
		if (r.depositedJ <= 0f)
		{
			return;
		}
		EnsureOrganArray();
		CrewState crewState = CrewModel.StateOf(crewHitPoints);
		crewFragmentHits++;
		crewDepositedJ = r.depositedJ + crewDepositedJ;
		crewHitPoints -= r.damageHp;
		if (!r.stopped)
		{
			crewPassThroughs++;
		}
		bool flag = false;
		if (r.organJ != null)
		{
			for (int i = 1; i < r.organJ.Length && i < 17; i++)
			{
				if (r.organJ[i] > 0f)
				{
					crewOrganJ[i] += r.organJ[i];
					flag = true;
				}
			}
		}
		if (flag)
		{
			crewOrganHits++;
		}
		CrewState crewState2 = CrewModel.StateOf(crewHitPoints);
		string text;
		if (r.stopped)
		{
			text = string.Format("остановлен в «{0}» на {1:F0} мм пути", CrewAnatomy.OrganName(r.stopOrgan), r.pathMm);
		}
		else
		{
			text = string.Format("НАВЫЛЕТ через {0:F0} мм, остаточная {1:F0} м/с", r.pathMm, r.exitSpeedMps);
		}
		string text2 = CrewModel.OrganBreakdown(crewOrganJ);
		string[] array = new string[11];
		array[0] = "◈ Экипаж [";
		array[1] = base.name;
		array[2] = "]: ";
		array[3] = text;
		array[4] = "; трасса: ";
		array[5] = r.trail;
		array[6] = ". ";
		array[7] = string.Format("Осело {0:F1} Дж ⇒ запас {1:F0}/", r.depositedJ, Mathf.Max(crewHitPoints, 0f));
		array[8] = string.Format("{0:F0}", CrewModel.MaxHitPoints);
		array[9] = ((crewState2 != crewState) ? (" ⇒ «" + CrewModel.StateName(crewState2) + "»") : "");
		array[10] = ((text2.Length > 0) ? (". Накоплено по органам: " + text2) : "");
		Debug.Log(string.Concat(array));
		CrewFigure.RefreshColor(this);
	}

	public void RegisterDirectPenetratorHit(ThreatClass threatClass, string threatName, float energyJ, Vector3 worldPoint)
	{
		if (!ShotRouting.Registers(kind))
		{
			return;
		}
		if (ShotRouting.Fires(ShotBranch.CrewRegistration, kind, threatClass))
		{
			Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
			CrewZone zone = CrewModel.ZoneAtHeightFraction(Mathf.Clamp(localPoint.y + 0.5f, 0f, 1f));
			crewDirectHits++;
			if (energyJ > 0f)
			{
				crewDepositedJ = energyJ + crewDepositedJ;
			}
			crewHitPoints = 0f;
			string text = (energyJ < 0f) ? "энергия не оценивается" : string.Format("~{0:F0} кДж", energyJ / 1000f);
			string[] array = new string[11];
			array[0] = "◈ ЭКИПАЖ [";
			array[1] = base.name;
			array[2] = "]: ПРЯМОЕ ПОПАДАНИЕ (";
			array[3] = threatName;
			array[4] = ") в ";
			array[5] = CrewModel.ZoneName(zone);
			array[6] = ", ";
			array[7] = text;
			array[8] = " ⇒ «";
			array[9] = CrewModel.StateName(CrewState.Incapacitated);
			array[10] = "». ⚠ Снаряд при этом НЕ ЗАМЕДЛЕН: фигура сопротивления не оказывает (R_t = 0). ⚠ Это РЕГИСТРАЦИЯ факта, а не модель поражения — раневой баллистики в проекте нет.";
			Debug.Log(string.Concat(array));
			CrewFigure.RefreshColor(this);
		}
		else if (ShotRouting.Fires(ShotBranch.LinerRegistration, kind, threatClass))
		{
			linerDirectHits++;
			string[] array = new string[5];
			array[0] = "◈ ПОДБОЙ [";
			array[1] = base.name;
			array[2] = "]: прямое попадание (";
			array[3] = threatName;
			array[4] = ") зарегистрировано — пробит насквозь без сопротивления. ⚠ Подбой рассчитан на ОСКОЛКИ; против пенетратора он не работает, и модель этого не изображает.";
			Debug.Log(string.Concat(array));
		}
	}

	public CrewZone CrewZoneAt(Vector3 worldPoint)
	{
		Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
		return CrewModel.ZoneAtHeightFraction(Mathf.Clamp(localPoint.y + 0.5f, 0f, 1f));
	}

	public void ResetBehindArmour()
	{
		crewHitPoints = 100f;
		crewFragmentHits = 0;
		engineFragmentHits = 0;
		engineDepositedJ = 0f;
		engineDamage = 0f;
		crewDepositedJ = 0f;
		crewDirectHits = 0;
		linerDirectHits = 0;
		crewOrganHits = 0;
		crewPassThroughs = 0;
		crewBoxMisses = 0;
		EnsureOrganArray();
		for (int i = 0; i < crewOrganJ.Length; i++)
		{
			crewOrganJ[i] = 0f;
		}
		CrewFigure.RefreshColor(this);
	}

	public void ApplyCassetteDefaults(ArmorType t)
	{
		if (t == ArmorType.ERAHeavy)
		{
			eraPlateThicknessMM = 15f;
			eraPlateVelocityMS = 500f;
			eraCassetteExtentMM = 150f;
			eraInterlayerGapMM = 20f;
		}
		else if (t == ArmorType.ERA)
		{
			eraPlateThicknessMM = 2.5f;
			eraPlateVelocityMS = 700f;
			eraCassetteExtentMM = 150f;
			eraInterlayerGapMM = 20f;
		}
	}

	public void SetEraPlateThickness(float mm)
	{
		eraPlateThicknessMM = Mathf.Max(mm, 0.1f);
		ApplyKindGeometry();
	}

	public void SetEraPlateVelocity(float mps)
	{
		eraPlateVelocityMS = Mathf.Max(mps, 0f);
		RefreshComposition();
	}

	public void SetEraInterlayerGap(float mm)
	{
		eraInterlayerGapMM = Mathf.Max(mm, 0.5f);
		ApplyKindGeometry();
	}

	public void SetEraCassetteExtent(float mm)
	{
		eraCassetteExtentMM = Mathf.Max(mm, 10f);
		ApplyKindGeometry();
	}

	public ReactiveArmorSpec GetJetEraSpec()
	{
		ReactiveArmorSpec spec = ReactiveArmor.DemoPlaceholder();
		spec.enabled = kind == ObjectKind.Era;
		spec.plateThicknessM = eraPlateThicknessMM / 1000f;
		spec.plateVelocityMS = eraPlateVelocityMS;
		spec.plateExtentM = eraCassetteExtentMM / 1000f;
		spec.cassetteExpended = cassetteExpended;
		return spec;
	}

	public ReactiveArmorRodSpec GetRodEraSpec()
	{
		ReactiveArmorRodSpec spec = ReactiveArmor.DemoRodPlaceholder();
		spec.enabled = kind == ObjectKind.Era;
		spec.plateThicknessM = eraPlateThicknessMM / 1000f;
		spec.plateVelocityMS = eraPlateVelocityMS;
		spec.interlayerGapM = eraInterlayerGapMM / 1000f;
		spec.plateExtentM = eraCassetteExtentMM / 1000f;
		spec.cassetteExpended = cassetteExpended;
		return spec;
	}

	public NeraSpec GetNeraSpec()
	{
		NeraSpec spec = NeraArmor.DemoReference();
		spec.name = "НДЗ";
		spec.enabled = kind == ObjectKind.Nera;
		spec.frontThicknessM = neraFrontThicknessMM / 1000f;
		spec.rearThicknessM = neraRearThicknessMM / 1000f;
		spec.frontDensity = NeraArmor.PlateDensity(neraFrontMaterial);
		spec.rearDensity = NeraArmor.PlateDensity(neraRearMaterial);
		spec.interlayerThicknessM = neraInterlayerMM / 1000f;
		spec.plateExtentM = neraExtentMM / 1000f;
		spec.elastomerImpulsePerArea = neraImpulsePerArea;
		spec.mountAngleRad = neraMountAngleDeg * Mathf.Deg2Rad;
		spec.blockExpended = cassetteExpended;
		return spec;
	}

	public NeraSpec GetNeraSpecAt(Vector3 worldHit)
	{
		NeraSpec spec = GetNeraSpec();
		spec.blockExpended = cassetteExpended;
		return spec;
	}

	public float NeraFrontVelocityMS()
	{
		return NeraArmor.FrontVelocityMS(GetNeraSpec());
	}

	public float NeraRearVelocityMS()
	{
		return NeraArmor.RearVelocityMS(GetNeraSpec());
	}

	public void SetNeraFrontThickness(float mm)
	{
		neraFrontThicknessMM = Mathf.Clamp(mm, NeraPlateThicknessMinMM, NeraPlateThicknessMaxMM);
		ApplyKindGeometry();
	}

	public void SetNeraRearThickness(float mm)
	{
		neraRearThicknessMM = Mathf.Clamp(mm, NeraPlateThicknessMinMM, NeraPlateThicknessMaxMM);
		ApplyKindGeometry();
	}

	public void SetNeraInterlayer(float mm)
	{
		neraInterlayerMM = Mathf.Clamp(mm, NeraInterlayerMinMM, NeraInterlayerMaxMM);
		ApplyKindGeometry();
	}

	public void SetNeraMountAngle(float deg)
	{
		neraMountAngleDeg = Mathf.Clamp(deg, NeraMountAngleMinDeg, NeraMountAngleMaxDeg);
		NeraBlock.Refresh(this);
	}

	public void SetNeraFrontMaterial(ArmorType t)
	{
		neraFrontMaterial = NeraArmor.ClampPlateMaterial(t);
		NeraBlock.Refresh(this);
	}

	public void SetNeraRearMaterial(ArmorType t)
	{
		neraRearMaterial = NeraArmor.ClampPlateMaterial(t);
		NeraBlock.Refresh(this);
	}

	public void SetNeraInterlayerMaterial(InterlayerMaterial m)
	{
		neraInterlayer = m;
	}

	public void SetNeraExtent(float mm)
	{
		neraExtentMM = Mathf.Clamp(mm, NeraExtentMinMM, NeraExtentMaxMM);
		ApplyKindGeometry();
	}

	public void SetNeraBonding(NeraBonding b)
	{
		if (!Enum.IsDefined(typeof(NeraBonding), b))
		{
			b = NeraBonding.Unknown;
		}
		neraBonding = b;
		NeraBlock.Refresh(this);
	}

	private void ApplyNeraGeometry()
	{
		ApplyKindGeometry();
	}

	private void ApplyKindGeometry()
	{
		if (ScaleAudit.TryKindSize(kind, KindGeometryInput.From(this), out PlacedSize s))
		{
			if (s.overridesPlan)
			{
				widthM = s.widthM;
				heightM = s.heightM;
			}
			if (s.overridesThickness)
			{
				thicknessMM = s.thicknessMM;
			}
		}
		ApplyVisualThickness();
		RefreshComposition();
	}

	public ReactiveArmorSpec GetJetEraSpecAt(Vector3 worldHit)
	{
		ReactiveArmorSpec spec = ReactiveArmor.DemoPlaceholder();
		spec.enabled = kind == ObjectKind.Era;
		spec.plateThicknessM = eraPlateThicknessMM / 1000f;
		spec.plateVelocityMS = eraPlateVelocityMS;
		spec.plateExtentM = eraCassetteExtentMM / 1000f;
		spec.cassetteExpended = cassetteExpended;
		return spec;
	}

	public ReactiveArmorRodSpec GetRodEraSpecAt(Vector3 worldHit)
	{
		ReactiveArmorRodSpec spec = ReactiveArmor.DemoRodPlaceholder();
		spec.enabled = kind == ObjectKind.Era;
		spec.plateThicknessM = eraPlateThicknessMM / 1000f;
		spec.plateVelocityMS = eraPlateVelocityMS;
		spec.interlayerGapM = eraInterlayerGapMM / 1000f;
		spec.plateExtentM = eraCassetteExtentMM / 1000f;
		spec.cassetteExpended = cassetteExpended;
		return spec;
	}

	private void Start()
	{
		ApplyVisualThickness();
		ApplyRotation();
		ResetColor();
		RefreshComposition();
	}

	public void RefreshComposition()
	{
		EraCassette.Refresh(this);
		NeraBlock.Refresh(this);
		CrewFigure.Refresh(this);
		RodBody.Refresh(this);
	}

	public TateModel.Target GetTarget()
	{
		if (kind == ObjectKind.Screen)
		{
			return StandoffDefences.RubberScreen();
		}
		if (kind == ObjectKind.EngineBlock)
		{
			return StandoffDefences.EngineBlockTarget(blockMaterial);
		}
		if (kind == ObjectKind.Nera)
		{
			return NeraArmor.PlateTarget(neraFrontMaterial);
		}
		if (kind == ObjectKind.SpallLiner)
		{
			return new TateModel.Target
			{
				name = "Подбой (" + SpallLinerModel.Name(linerMaterial) + ")",
				density = SpallLinerModel.Density(linerMaterial)
			};
		}
		if (kind == ObjectKind.Crew)
		{
			return new TateModel.Target
			{
				name = "Фигура экипажа (не преграда)",
				density = 1000f
			};
		}
		switch (armorType)
		{
		case ArmorType.HHA:
			return TateModel.HHA();
		case ArmorType.Aluminum:
			return TateModel.Aluminum();
		case ArmorType.Titanium:
			return TateModel.Titanium();
		case ArmorType.Ceramic:
			return TateModel.Ceramic();
		case ArmorType.Tungsten:
			return TateModel.Tungsten();
		case ArmorType.Textolite:
			return TateModel.Textolite();
		case ArmorType.BoronCarbide:
			return TateModel.BoronCarbide();
		case ArmorType.TitaniumDiboride:
			return TateModel.TitaniumDiboride();
		case ArmorType.DepletedUraniumArmour:
			return TateModel.DepletedUraniumArmour();
		case ArmorType.Alumina:
			return TateModel.Alumina();
		case ArmorType.CastSteel:
			return TateModel.CastSteel();
		case ArmorType.Steel4340:
		{
			Debug.LogError("Сталь 4340 не имеет аналитической мишени (R_t): у проекта есть только пенетратор TateModel.Steel(). Плита посчитана как RHA, и это названо в подписи результата. Этот материал считается в Super.");
			TateModel.Target target = TateModel.RHA();
			target.name = "Сталь 4340 (АНАЛИТИКИ НЕТ — посчитано как RHA)";
			return target;
		}
		case ArmorType.Polyurethane:
			return TateModel.Elastomer(InterlayerMaterial.Polyurethane);
		case ArmorType.Neoprene:
			return TateModel.Elastomer(InterlayerMaterial.Neoprene);
		case ArmorType.NaturalRubber:
			return TateModel.Elastomer(InterlayerMaterial.NaturalRubber);
		default:
			return TateModel.RHA();
		}
	}

	public static string KindName(ObjectKind k)
	{
		switch (k)
		{
		case ObjectKind.Screen:
			return "Экран";
		case ObjectKind.Slat:
			return "Решётка";
		case ObjectKind.EngineBlock:
			return "Моторный отсек";
		case ObjectKind.Era:
			return "Кассета ДЗ";
		case ObjectKind.Nera:
			return "Блок НДЗ";
		case ObjectKind.SpallLiner:
			return "Подбой";
		case ObjectKind.Crew:
			return "Экипаж";
		case ObjectKind.Model:
			return "Модель";
		case ObjectKind.Rod:
			return "Стержень";
		default:
			return "Преграда";
		}
	}

	public void SetKind(ObjectKind k)
	{
		if (kind != k)
		{
			kind = k;
			presetId = "";
			if (ScaleAudit.TryKindSize(k, KindGeometryInput.From(this), out PlacedSize s))
			{
				if (s.overridesPlan)
				{
					widthM = s.widthM;
					heightM = s.heightM;
				}
				if (s.overridesThickness)
				{
					thicknessMM = s.thicknessMM;
				}
			}
			else
			{
				Debug.LogError("ArmorPlate.SetKind: вид «" + KindName(k) + "» неизвестен таблице ScaleAudit.TryKindSize — габарит не назначен. Добавь строку в ScaleAudit, иначе объект будет нарисован размером листа брони.");
			}
			ApplyVisualThickness();
			ResetColor();
			RefreshComposition();
		}
	}

	public static string DisplayName(ArmorType type)
	{
		switch (type)
		{
		case ArmorType.RHA:
			return "RHA сталь";
		case ArmorType.HHA:
			return "HHA сталь";
		case ArmorType.Aluminum:
			return "Алюминий";
		case ArmorType.Titanium:
			return "Титан";
		case ArmorType.Ceramic:
			return "Керамика SiC";
		case ArmorType.Tungsten:
			return "Вольфрам";
		case ArmorType.ERA:
			return "ДЗ лёгкая кассета";
		case ArmorType.Textolite:
			return "Текстолит";
		case ArmorType.ERAHeavy:
			return "ДЗ тяжёлая кассета";
		case ArmorType.BoronCarbide:
			return "Карбид бора B₄C";
		case ArmorType.TitaniumDiboride:
			return "Диборид титана TiB₂";
		case ArmorType.DepletedUraniumArmour:
			return "Обеднённый уран";
		case ArmorType.Alumina:
			return "Оксид алюминия";
		case ArmorType.CastSteel:
			return "Литая сталь";
		case ArmorType.RhaSetB:
			return "RHA (набор Б)";
		case ArmorType.Steel4340:
			return "Сталь 4340";
		case ArmorType.Polyurethane:
			return "Полиуретан";
		case ArmorType.Neoprene:
			return "Неопрен";
		case ArmorType.NaturalRubber:
			return "Натуральная резина";
		default:
			return type.ToString();
		}
	}

	public void SetArmorType(ArmorType type)
	{
		ArmorType armorType2 = armorType;
		armorType = type;
		if (kind == ObjectKind.Era && armorType2 != type)
		{
			ApplyCassetteDefaults(type);
		}
		ResetColor();
		RefreshComposition();
	}

	public void SetThickness(float mm)
	{
		thicknessMM = mm;
		if (kind == ObjectKind.Rod)
		{
			heightM = mm / 1000f;
		}
		ApplyVisualThickness();
		RefreshComposition();
	}

	public void SetWidth(float m)
	{
		widthM = m;
		ApplyVisualThickness();
		RefreshComposition();
	}

	public void SetHeight(float m)
	{
		if (kind == ObjectKind.Rod)
		{
			thicknessMM = m * 1000f;
			m = thicknessMM / 1000f;
		}
		heightM = m;
		ApplyVisualThickness();
		RefreshComposition();
	}

	public void SetAngleY(float deg)
	{
		angleY = deg;
		ApplyRotation();
	}

	public void SetAngleX(float deg)
	{
		angleX = deg;
		ApplyRotation();
	}

	public void SetEdgeClamp(bool on)
	{
		edgeClamp = on;
	}

	private void ApplyRotation()
	{
		transform.rotation = SectionFrame.PlateBase() * Quaternion.Euler(new Vector3(angleX, angleY, 0f));
	}

	public float GetEffectiveAngle()
	{
		Vector3 vector;
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (projectile != null && projectile.direction.sqrMagnitude > 1e-09f)
		{
			vector = projectile.direction.normalized;
		}
		else
		{
			vector = SectionFrame.IsRoof ? Vector3.back : Vector3.right;
		}
		if (kind == ObjectKind.Rod)
		{
			Vector3 vector2 = vector.normalized;
			Vector3 right = transform.right.normalized;
			return Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector3.Dot(right, vector2)))) * Mathf.Rad2Deg;
		}
		float num = Vector3.Angle(vector, transform.forward);
		if (num > 90f)
		{
			num = 180f - num;
		}
		return num;
	}

	public void ApplyVisualThickness()
	{
		if (kind == ObjectKind.Model)
		{
			transform.localScale = Vector3.one * modelScale;
		}
		else
		{
			transform.localScale = SceneScale.PlateLocalScale(widthM, heightM, thicknessMM);
		}
	}

	public static Color CanonicalColor(ArmorType type)
	{
		switch (type)
		{
		case ArmorType.RHA:
		case ArmorType.RhaSetB:
			return new Color(0.55f, 0.55f, 0.6f);
		case ArmorType.HHA:
			return new Color(0.4f, 0.4f, 0.45f);
		case ArmorType.Aluminum:
			return new Color(0.75f, 0.76f, 0.78f);
		case ArmorType.Titanium:
			return new Color(0.5f, 0.52f, 0.55f);
		case ArmorType.Ceramic:
			return new Color(0.8f, 0.75f, 0.65f);
		case ArmorType.Tungsten:
			return new Color(0.35f, 0.35f, 0.38f);
		case ArmorType.ERA:
			return new Color(0.32f, 0.42f, 0.3f);
		case ArmorType.Textolite:
			return new Color(0.576f, 0.484f, 0.227f);
		case ArmorType.ERAHeavy:
			return new Color(0.26f, 0.33f, 0.26f);
		case ArmorType.BoronCarbide:
		case ArmorType.Neoprene:
			return new Color(0.22f, 0.22f, 0.24f);
		case ArmorType.TitaniumDiboride:
			return new Color(0.45f, 0.47f, 0.5f);
		case ArmorType.DepletedUraniumArmour:
			return new Color(0.24f, 0.23f, 0.21f);
		case ArmorType.Alumina:
			return new Color(0.88f, 0.88f, 0.86f);
		case ArmorType.CastSteel:
			return new Color(0.48f, 0.47f, 0.47f);
		case ArmorType.Steel4340:
			return new Color(0.5f, 0.5f, 0.54f);
		case ArmorType.Polyurethane:
			return new Color(0.3f, 0.28f, 0.33f);
		case ArmorType.NaturalRubber:
			return new Color(0.26f, 0.24f, 0.22f);
		default:
			return new Color(0.6f, 0.6f, 0.65f);
		}
	}

	public void SetDisplayColor(Color c)
	{
		hasVerdict = true;
		verdictColor = c;
		ApplyDisplayColor();
	}

	public void ApplyDisplayColor()
	{
		Color c;
		if (!baseColorSet)
		{
			ResetColor();
			return;
		}
		if (hasVerdict && GameSettings.ResultTint)
		{
			c = verdictColor;
		}
		else
		{
			c = baseColor;
		}
		if (kind == ObjectKind.Model || kind == ObjectKind.Rod)
		{
			foreach (Renderer renderer in ChildBodyRenderers())
			{
				renderer.material.color = c;
			}
		}
		else
		{
			Renderer renderer = GetComponent<Renderer>();
			if (renderer == null)
			{
				return;
			}
			renderer.material.color = c;
		}
	}

	public void SetHighlight(Color c)
	{
		if (kind == ObjectKind.Model || kind == ObjectKind.Rod)
		{
			foreach (Renderer renderer in ChildBodyRenderers())
			{
				renderer.material.color = c;
			}
		}
		else
		{
			Renderer renderer = GetComponent<Renderer>();
			if (renderer != null)
			{
				renderer.material.color = c;
			}
		}
	}

	private List<Renderer> ChildBodyRenderers()
	{
		List<Renderer> list = new List<Renderer>();
		Renderer[] array = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < array.Length; i++)
		{
			Renderer renderer = (array[i] != null) ? array[i].GetComponent<Renderer>() : null;
			if (renderer != null)
			{
				list.Add(renderer);
			}
		}
		return list;
	}

	public void ResetColor()
	{
		if (kind == ObjectKind.Model || kind == ObjectKind.Rod)
		{
			foreach (Renderer renderer in ChildBodyRenderers())
			{
				renderer.sharedMaterial = new Material(MaterialLibrary.Armor(armorType));
			}
		}
		else
		{
			Renderer renderer = GetComponent<Renderer>();
			if (renderer == null)
			{
				return;
			}
			Material[] array = new Material[2];
			array[0] = new Material(MaterialLibrary.Armor(armorType));
			array[1] = MaterialLibrary.ChannelInterior();
			renderer.sharedMaterials = array;
		}
		baseColor = MaterialLibrary.Armor(armorType).color;
		baseColorSet = true;
	}
}
