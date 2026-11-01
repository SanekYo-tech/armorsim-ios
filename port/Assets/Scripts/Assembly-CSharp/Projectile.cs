using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(LineRenderer))]
public class Projectile : MonoBehaviour
{
	public enum QualityMode
	{
		Обычное = 0,
		Продвинутое = 1
	}

	public enum CoreType
	{
		WHA = 0,
		DU = 1,
		Steel = 2,
		WC = 3
	}

	[Header("Визуальный полёт")]
	public float flightSpeed;

	public Vector3 direction;

	[HideInInspector]
	public bool visualApproach;

	[HideInInspector]
	public Vector3 visualApproachFrom;

	[Header("Боевые параметры снаряда")]
	public float combatVelocity;

	public float rodLengthMM;

	public float diameter;

	public bool isLongRod;

	[Header("Материал сердечника")]
	public CoreType coreType;

	[Header("Качество симуляции")]
	public QualityMode quality;

	[Header("Тип снаряда (визуал + пресеты)")]
	public ProjectileDefinition definition;

	[Header("Дальность луча расчёта")]
	public float rayDistance;

	public const float WorldToMM = 999.99994f;

	private const int MaxSegments = 512;

	public const float ShatteredCoreOnset = 0.15f;

	private static readonly float SurfaceEps;

	private static readonly float BackEps;

	private Rigidbody rb;

	private LineRenderer line;

	private Vector3 startPosition;

	private Quaternion startRotation;

	private Vector3 startScale;

	private bool startCaptured;

	[NonSerialized]
	private float rayDistanceBase;

	[NonSerialized]
	private bool rayBaseCaptured;

	public static float FractureStrainWHA;

	public static float FractureStrainDU;

	public static float FractureStrainSteel;

	static Projectile()
	{
		SurfaceEps = SceneScale.RodDiameterWorld(0.2f);
		BackEps = SceneScale.RodDiameterWorld(0.1f);
		FractureStrainWHA = 0.2f;
		FractureStrainDU = 0.25f;
		FractureStrainSteel = 0.15f;
	}

	public Vector3 StartPositionWorld
	{
		get
		{
			if (startCaptured)
			{
				return startPosition;
			}
			return base.transform.position;
		}
	}

	public Quaternion StartRotationWorld
	{
		get
		{
			if (startCaptured)
			{
				return startRotation;
			}
			return base.transform.rotation;
		}
	}

	private void Start()
	{
		rb = GetComponent<Rigidbody>();
		line = GetComponent<LineRenderer>();
		SetupLine();
		startPosition = base.transform.position;
		startRotation = base.transform.rotation;
		startScale = base.transform.localScale;
		startCaptured = true;
		rb.isKinematic = true;
	}

	private void SetupLine()
	{
		line.startWidth = 0.08f;
		line.endWidth = 0.08f;
		Shader shader = Shader.Find("Sprites/Default");
		if (shader == null)
		{
			shader = Shader.Find("Unlit/Color");
		}
		Material material = new Material(shader);
		line.sharedMaterial = material;
		line.positionCount = 0;
	}

	public void Fire()
	{
		PenetrationPlayer player = PenetrationPlayer.FindOrCreate();
		player.StopPlayback();
		rb.isKinematic = true;
		base.transform.position = startPosition;
		base.transform.rotation = startRotation;
		base.transform.localScale = startScale;
		if (UltraShotRunner.TryBegin(this))
		{
			return;
		}
		UltraShotRunner.FindOrCreate().Restore();
		if (AppFlow.ActiveMode == SimMode.Super || !ModelRouting.SceneHasModel(this))
		{
			ClearPreviousShot();
			PrepareShotRange();
			player.Play(ComputeShotPlan());
		}
		else
		{
			string text = Localization.T(ModelRouting.Refuses(AppFlow.ActiveMode));
			Debug.LogWarning("[S40] " + text);
			if (ResultDisplay.Instance != null)
			{
				ResultDisplay.Instance.Show(text, new Color(1f, 0.72f, 0.3f, 1f));
			}
		}
	}

	public void ClearScene()
	{
		PenetrationPlayer player = PenetrationPlayer.FindOrCreate();
		if (player != null)
		{
			player.StopPlayback();
		}
		rb.isKinematic = true;
		base.transform.position = startPosition;
		base.transform.rotation = startRotation;
		base.transform.localScale = startScale;
		UltraShotRunner.FindOrCreate().Restore();
		ClearPreviousShot();
	}

	private void ClearPreviousShot()
	{
		WarheadDetonation.RestoreBody();
		WarheadDetonation.ClearAll();
		line.positionCount = 0;
		ResetAllPlates();
		if (FragmentSpawner.Instance != null)
		{
			FragmentSpawner.Instance.ClearAll();
		}
		EraCassette.ClearAll();
		NeraBlock.ClearAll();
		EraFlyerFragments.ClearAll();
		MicroSpall.ClearAll();
		if (ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Hide();
		}
		ArmorPlate[] array = FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			DeformableMesh deformableMesh = array[i].GetComponent<DeformableMesh>();
			if (deformableMesh != null)
			{
				deformableMesh.ResetDeformation();
			}
		}
	}

	private void ResetAllPlates()
	{
		EraCassette.ClearAll();
		NeraBlock.ClearAll();
		ArmorPlate[] array = FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			ArmorPlate armorPlate = array[i];
			armorPlate.ResetColor();
			if (armorPlate.IsEraPlate || armorPlate.IsNeraPlate)
			{
				armorPlate.ResetCassettes();
			}
			armorPlate.ResetBehindArmour();
			EraCassette.Refresh(armorPlate);
			CrewFigure.Refresh(armorPlate);
		}
		NeraBlock.ResetAll();
	}

	private static string NoAnalyticModelKey(ProjectileClass kind)
	{
		if (kind == ProjectileClass.HollowBody)
		{
			return "verdict.no_model_hollow_body";
		}
		Debug.LogError(string.Format("[Projectile] класс {0} объявлен без аналитической модели ", kind) + "(Catalog.IsProjectileAnalytic), а своего текста отказа у него нет — игрок получит ОБЩИЙ текст. Допиши ключ сюда.");
		return "verdict.no_model";
	}

	private ShotPlan ComputeShotPlan()
	{
		Vector3 origin = startPosition;
		Vector3 dir = direction.normalized;
		ShotPlan plan = new ShotPlan();
		plan.origin = origin;
		plan.direction = dir;
		plan.hasApproach = visualApproach;
		plan.approachFrom = visualApproachFrom;
		plan.L0M = rodLengthMM / 1000f;
		plan.diameterMM = diameter;
		plan.rayDistance = rayDistance;
		plan.projectile = this;
		plan.tracePoints.Add(origin);
		if (!Catalog.IsProjectileAnalytic(GetDefinition().kind))
		{
			plan.lineEnd = origin;
			plan.verdict = ShotVerdict.NoModel;
			plan.verdictMessage = Localization.T(NoAnalyticModelKey(GetDefinition().kind));
			plan.verdictColor = ResultPalette.Miss;
			plan.lineColor = ResultPalette.Miss;
			Debug.LogWarning(string.Format("=== ВЫСТРЕЛ НЕ ПОСЧИТАН: класс {0} аналитического ядра ", GetDefinition().kind) + "не имеет, отказ по имени (S28 §1.3) ===");
			return plan;
		}
		string refusalKey;
		object[] refusalArgs;
		if (RodPassports.Refusal(GetDefinition(), out refusalKey, out refusalArgs))
		{
			plan.lineEnd = origin;
			plan.verdict = ShotVerdict.NoModel;
			plan.verdictMessage = Localization.T(refusalKey, refusalArgs);
			plan.verdictColor = ResultPalette.Miss;
			plan.lineColor = ResultPalette.Miss;
			Debug.LogWarning(string.Concat("=== ВЫСТРЕЛ НЕ ПОСЧИТАН: паспорт «", GetDefinition().passportId, "» отклонён (", refusalKey, "), отказ по имени (S44 §4) ==="));
			return plan;
		}
		if (GetDefinition().IsShapedCharge)
		{
			StandoffDefences.SlatOutcome slatOutcome;
			ArmorPlate slat;
			Vector3 slatHit;
			if (ResolveSlatArray(origin, dir, GetDefinition().coneDiameterMM, GetDefinition().isTandem, out slatOutcome, out slat, out slatHit))
			{
				if (slatOutcome == StandoffDefences.SlatOutcome.Dud)
				{
					plan.tracePoints.Add(slatHit);
					plan.lineEnd = slatHit;
					plan.verdict = ShotVerdict.Stopped;
					plan.verdictMessage = Localization.T("verdict.dud_slat");
					plan.verdictColor = ResultPalette.Stopped;
					plan.lineColor = ResultPalette.Stopped;
					return plan;
				}
				if (slatOutcome == StandoffDefences.SlatOutcome.DetonatedAtSlat)
				{
					return BuildShapedChargePlan(plan, origin, dir, rayDistance, slatHit);
				}
			}
			return BuildShapedChargePlan(plan, origin, dir, rayDistance);
		}
		TateModel.Penetrator core = GetCore(coreType);
		float v = combatVelocity;
		float deadLenM = Mathf.Max(plan.L0M * 0.02f, 0.001f);
		float yawDeg = 0f;
		RodPostEraState postEra = new RodPostEraState
		{
			effectiveLengthM = plan.L0M
		};
		Debug.Log(string.Format("=== ВЫСТРЕЛ: {0} {1}мм, {2:F0} м/с | L0={3:F1}мм, L/D={4:F2} ===", core.name, diameter, combatVelocity, rodLengthMM, rodLengthMM / diameter));
		if (rodLengthMM / diameter < 1f)
		{
			Debug.LogWarning("Короткий стержень: модель Тейта рассчитана на длинные стержни");
		}
		int segment = 0;
		while (true)
		{
			RaycastHit bestHit;
			ArmorPlate plate;
			if (!FindNearestPlate(origin, dir, out bestHit, out plate))
			{
				break;
			}
			RegisterPassThroughHits(origin, dir, bestHit.distance, ThreatClass.KineticRod, "стержень", RodKineticEnergyJ(core, postEra.effectiveLengthM, v));
			int layerIndex = plan.layers.Count + 1;
			Vector3 normal;
			int axis;
			EntryFaceNormal(plate, origin, dir, out normal, out axis);
			if (Vector3.Dot(normal, dir) > 0f)
			{
				normal = -normal;
			}
			float betaDeg = Vector3.Angle(-dir, normal);
			float cosBeta = Mathf.Cos(betaDeg * Mathf.Deg2Rad);
			Vector3 hitPoint = bestHit.point;
			if (axis != 2)
			{
				Vector3 forward = plate.transform.forward;
				if (Vector3.Dot(dir, forward) > 0f)
				{
					forward = -forward;
				}
				Debug.Log(string.Concat(string.Format("  ▣ ТОРЦЕВОЕ ПОПАДАНИЕ в слой {0}: {1} — вход через ", layerIndex, plate.name), (axis == 0) ? "боковую" : "верхнюю/нижнюю", " грань. ", string.Format("β к задетой грани {0:F1}° (к лицевой было бы {1:F1}°). ", betaDeg, Vector3.Angle(-dir, forward)), "Считаем по задетой — она и сопротивляется."));
			}
			if (!Catalog.IsMaterialAnalytic(plate.armorType))
			{
				plan.layers.Clear();
				plan.tracePoints.Clear();
				plan.tracePoints.Add(plan.origin);
				plan.lineEnd = plan.origin;
				plan.verdict = ShotVerdict.NoModel;
				plan.verdictMessage = Localization.T("verdict.no_model_material", Catalog.MaterialName(plate.armorType));
				plan.verdictColor = ResultPalette.Miss;
				plan.lineColor = ResultPalette.Miss;
				Debug.LogWarning(string.Format("=== ВЫСТРЕЛ НЕ ПОСЧИТАН: у вещества {0} ", plate.armorType) + "(плита «" + plate.name + "») нет аналитической мишени — отказ по имени, Catalog.superOnlyMaterials. Этот материал считается в Super. ===");
				return plan;
			}
			bool degraded;
			float pressurePa;
			TateModel.Target target = AdvancedMaterials.ResolveForImpact(plate.armorType, plate.GetTarget(), core, v, out degraded, out pressurePa);
			if (plate.armorType == ArmorPlate.ArmorType.BoronCarbide)
			{
				string text = ((!degraded) ? string.Format("материал цел, R_t = {0:F1} ГПа", target.Rt / 1E+09f) : ("АМОРФИЗАЦИЯ, R_t обвалился до " + string.Format("{0:F1} ГПа (ASSUMED)", 1.2f)));
				Debug.Log(string.Format("  ◈ B₄C слой {0}: давление торможения {1:F1} ГПа против порога ", layerIndex, pressurePa / 1E+09f) + string.Format("{0:F0} ГПа ⇒ ", 40f) + text);
			}
			float substanceMM = plate.SubstanceThicknessMM(MeasureLosPath(plate, origin, dir, bestHit.distance, betaDeg, out bool fullFaceHit));
			TrajectoryModel.ObliqueDecision decision = TrajectoryModel.Classify(core, target, v, betaDeg, substanceMM, diameter, GetDefinition().nose, GetDefinition().ballisticCap, postEra.effectiveLengthM);
			if (decision.thinPlate && betaDeg > 60f)
			{
				Debug.Log(string.Format("  тонкая плита — аналитика рикошета неприменима ({0}: {1:F0} мм по нормали < 1 калибра {2:F0} мм) — пробитие/прорыв с отклонением", plate.name, substanceMM * cosBeta, diameter));
			}
			if (decision.evt == TrajectoryModel.ObliqueEvent.Slide)
			{
				Vector3 slideDir = Vector3.ProjectOnPlane(dir, normal).normalized;
				Vector3 grooveOrigin = hitPoint - normal * (plate.thicknessMM / WorldToMM * 0.3f);
				float slideTrack = RayExitDistance(plate, grooveOrigin, dir);
				if (slideTrack <= 0f)
				{
					slideTrack = 1f;
				}
				Vector3 slideExit = grooveOrigin + slideDir * slideTrack;
				float vOut = v * TrajectoryModel.SlideSpeedFactor;
				float lenAfter = postEra.effectiveLengthM - plan.L0M * TrajectoryModel.SlideErosionOfL0;
				if (lenAfter <= 0f)
				{
					lenAfter = 0f;
				}
				Debug.Log(string.Format("  ↷ СКОЛЬЖЕНИЕ по слою {0}: {1} | β={2:F0}° > {3:F0}°, v={4:F0} < v_c={5:F0} м/с | v → {6:F0} м/с, износ {7:F0}% L0 | уход ({8:F2}, {9:F2}, {10:F2})", layerIndex, target.name, betaDeg, TrajectoryModel.SlideMinAngleDeg, v, decision.slideVc, vOut, TrajectoryModel.SlideErosionOfL0 * 100f, slideDir.x, slideDir.y, slideDir.z));
				LayerPlaybackData layer = new LayerPlaybackData();
				layer.plate = plate;
				layer.entryPoint = hitPoint;
				layer.direction = dir;
				layer.eventKind = LayerEventKind.Slide;
				layer.outcome = SynthOutcome(vOut, lenAfter, postEra.effectiveLengthM, plan.L0M);
				layer.tEffMM = substanceMM;
				layer.entryVelocity = v;
				layer.entryLengthM = postEra.effectiveLengthM;
				layer.exitDirection = slideDir;
				layer.exitPoint = slideExit;
				layer.surfaceNormal = normal;
				layer.exitVelocity = vOut;
				layer.exitLengthM = lenAfter;
				layer.grooveDepthMM = TrajectoryModel.SlideGrooveDepthCalibers * diameter;
				layer.slideTrackWorld = slideTrack;
				layer.yawDeg = yawDeg;
				layer.kYaw = 1f;
				plan.layers.Add(layer);
				plan.tracePoints.Add(hitPoint);
				plan.tracePoints.Add(slideExit);
				if (lenAfter <= 0f)
				{
					postEra = default(RodPostEraState);
				}
				else
				{
					postEra.effectiveLengthM = lenAfter;
				}
				if (postEra.effectiveLengthM <= deadLenM || vOut < 60f)
				{
					Debug.Log(string.Format("  ✕ стержень мёртв после скольжения (слой {0})", layerIndex));
					plan.verdict = ShotVerdict.Slide;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T("verdict.slide_worn");
					plan.verdictColor = ResultPalette.Slide;
					plan.lineColor = ResultPalette.Slide;
					plan.lineEnd = slideExit;
					return plan;
				}
				dir = slideDir;
				origin = slideExit + slideDir * SurfaceEps + normal * BackEps;
				v = vOut;
			}
			else if (decision.evt != TrajectoryModel.ObliqueEvent.Ricochet && decision.evt != TrajectoryModel.ObliqueEvent.CriticalRicochet)
			{
				if (ShotRouting.Fires(ShotBranch.EraRod, plate.kind, ThreatClass.KineticRod) || ShotRouting.Fires(ShotBranch.NeraRod, plate.kind, ThreatClass.KineticRod))
				{
					ReactiveArmorRodSpec spec;
					NeraRodOutcome neraOutcome = default(NeraRodOutcome);
					if (!plate.IsNeraPlate)
					{
						spec = plate.GetRodEraSpecAt(hitPoint);
					}
					else
					{
						NeraSpec neraSpec = plate.GetNeraSpecAt(hitPoint);
						neraOutcome = NeraArmor.ApplyToRod(neraSpec, betaDeg * Mathf.Deg2Rad, postEra.effectiveLengthM, diameter / 1000f, core.density, v, 0.5f);
						Debug.Log(string.Format("  ◈ НДЗ слой {0} против стержня: F_перед {1:F3} МН, ", layerIndex, neraOutcome.front.forceN / 1E+06f) + string.Format("F_зад {0:F3} МН — это ×{1:F3} от кассеты ДЗ ", neraOutcome.rear.forceN / 1E+06f, neraOutcome.forceRatioToEra) + "на той же встрече (V_p ниже в квадрате И пластина тоньше). " + string.Format("ψ_перед {0:F3}°", neraOutcome.front.yawAngleDeg));
						spec = NeraArmor.PlateRodSpec(neraSpec, front: true, 0.5f);
					}
					RodEraOutcome era = ReactiveArmor.ApplyToRod(spec, betaDeg * Mathf.Deg2Rad, postEra.effectiveLengthM, diameter / 1000f, core.density, v);
					if (era.forceN <= 0f)
					{
						if (spec.enabled)
						{
							string text2 = ((spec.plateVelocityMS > 0f) ? "(β≈0 — пластины едут вдоль оси)" : (plate.IsNeraPlate ? "(блок уже отработал — пластины улетели)" : "(кассета израсходована)"));
							Debug.Log(string.Format("  ◈ {0} слой {1}: поперечного импульса нет ", plate.IsNeraPlate ? "НДЗ" : "ДЗ", layerIndex) + text2);
						}
					}
					else
					{
						Debug.Log(string.Format("  ◈ ДЗ слой {0}: F={1:F2} МН, момент пары {2:F1} кг·м²/с, ", layerIndex, era.forceN / 1E+06f, era.angularImpulse) + string.Format("v_station={0:F0} м/с", era.stationVelocityMS));
						RodPostEraState eraResult;
						if (quality == QualityMode.Продвинутое)
						{
							eraResult = EraRodLattice.Run(spec, betaDeg * Mathf.Deg2Rad, coreType, postEra.effectiveLengthM, diameter / 1000f, core.density, v);
						}
						else
						{
							eraResult = ReactiveArmor.AnalyticPostEraState(era, postEra.effectiveLengthM);
							Debug.Log("     (обычный режим: решётка не запускалась, рыскание аналитическое; укорочение стержня даёт только продвинутый режим)");
						}
						if (0f < eraResult.effectiveLengthM && eraResult.effectiveLengthM < postEra.effectiveLengthM)
						{
							Debug.Log(string.Format("     L: {0:F0} → {1:F0} мм", postEra.effectiveLengthM * 1000f, eraResult.effectiveLengthM * 1000f) + ((eraResult.fragmentCount < 2) ? "" : string.Format(" (обломков {0}, считаем ГОЛОВНОЙ)", eraResult.fragmentCount)));
							postEra = eraResult;
						}
						float psiDeg = eraResult.rigidYawRad * 57.29578f;
						if (psiDeg > 0f)
						{
							yawDeg += psiDeg;
							Debug.Log(string.Format("     ψ=+{0:F2}° → γ={1:F1}°", psiDeg, yawDeg) + ((psiDeg > 5f) ? "  ⚠ЭКСТРАПОЛЯЦИЯ за проверенный диапазон Бьерке" : "") + ((!eraResult.fromLattice) ? "  (аналитика: решётка не дала результата)" : ""));
						}
						if (postEra.effectiveLengthM <= deadLenM)
						{
							Debug.Log(string.Format("  ✕ стержень разрушен кассетой на слое {0}", layerIndex));
							plan.verdict = ShotVerdict.Stopped;
							plan.verdictLayerIndex = plan.layers.Count - 1;
							plan.verdictMessage = Localization.T("verdict.rod_killed_era");
							plan.verdictColor = ResultPalette.Stopped;
							plan.lineColor = ResultPalette.Stopped;
							plan.lineEnd = hitPoint;
							return plan;
						}
					}
				}
				float kYaw = 1f;
				bool tailBrokeFromYaw = false;
				float tailBreakLengthM = 0f;
				if (yawDeg > 0f)
				{
					float criticalYawDeg = TrajectoryModel.CriticalYawDeg(v, postEra.effectiveLengthM, diameter / 1000f);
					if (criticalYawDeg > 0f)
					{
						float gammaDeg = yawDeg;
						if (criticalYawDeg * TrajectoryModel.TailBreakYawRatio <= yawDeg)
						{
							float keptLengthM = postEra.effectiveLengthM * (1f - TrajectoryModel.TailBreakLengthCut);
							tailBreakLengthM = postEra.effectiveLengthM - keptLengthM;
							postEra.effectiveLengthM = keptLengthM;
							Debug.Log(string.Format("  ✂ излом хвоста от рыскания: γ={0:F1}° ≥ {1:F0}·γ_кр={2:F2}° | L → {3:F0} мм", gammaDeg, TrajectoryModel.TailBreakYawRatio, criticalYawDeg, keptLengthM * 1000f));
							tailBrokeFromYaw = true;
						}
					}
					kYaw = TrajectoryModel.YawPenalty(yawDeg, criticalYawDeg);
					Debug.Log(string.Format("  yaw γ={0:F1}° (γ_кр={1:F2}°) → штраф k={2:F3}", yawDeg, criticalYawDeg, kYaw));
				}
				float thicknessMM = (!fullFaceHit) ? (substanceMM * cosBeta) : plate.thicknessMM;
				PenetrationOutcome outcome;
				if (!AdvancedMaterials.HasDepthDecay(plate.armorType))
				{
					outcome = PenetrationCore.Calculate(core, target, v, betaDeg, thicknessMM, postEra.effectiveLengthM, plan.L0M, collectSteps: true, kYaw, GetDefinition().nose, GetDefinition().ballisticCap, diameter);
				}
				else
				{
					outcome = AdvancedMaterials.CalculateWithDepthDecay(core, target, AdvancedMaterials.DepthLaw(plate.armorType), v, betaDeg, thicknessMM, postEra.effectiveLengthM, plan.L0M, kYaw, 24);
					Debug.Log(string.Format("  ◈ TiB₂ слой {0}: спад R_t {1:F1} → ", layerIndex, 10.7f) + string.Format("{0:F1} ГПа (λ = ", 5.8f) + string.Format("{0:F0} мм, ASSUMED), ", 40f) + string.Format("{0} подслоёв", 24));
				}
				string betaNote = ((betaDeg <= 0.05f) ? "" : ((decision.tan2Threshold == float.PositiveInfinity) ? string.Format(" | β={0:F1}°", betaDeg) : string.Format(" | β={0:F1}° (β_кр={1:F1}°)", betaDeg, decision.betaCritDeg)));
				string limitNote = ((outcome.ballisticLimit == float.PositiveInfinity) ? ">3000 (непробиваемо)" : string.Format("{0:F0} м/с", outcome.ballisticLimit));
				float rigidLimit = RigidPenetration.RigidLimitVelocity(core, target, GetDefinition().ballisticCap);
				string rigidNote = ((!RigidPenetration.HasRigidBranch(target)) ? "жёсткой ветки у материала нет" : ((rigidLimit <= 0f) ? "порог жёсткости 0 м/с (сопротивление выше стеснённой прочности уже в покое)" : (string.Format("порог жёсткости {0:F0} м/с", rigidLimit) + (GetDefinition().ballisticCap ? " (с колпаком)" : ""))));
				string modeNote = ((outcome.erosionOnsetMax <= 0f) ? "жёсткий" : ((outcome.erosionOnsetMax < 1f) ? string.Format("переходный (s = {0:F2})", outcome.erosionOnsetMax) : "эрозионный"));
				Debug.Log(string.Format("  ◇ режим слоя {0}: {1} | {2}", layerIndex, modeNote, rigidNote));
				LayerPlaybackData layer2 = new LayerPlaybackData();
				layer2.plate = plate;
				layer2.entryPoint = hitPoint;
				layer2.direction = dir;
				layer2.eventKind = ((outcome.result != 0) ? LayerEventKind.Stopped : LayerEventKind.Penetration);
				layer2.outcome = outcome;
				layer2.tEffMM = substanceMM;
				layer2.entryVelocity = v;
				layer2.entryLengthM = postEra.effectiveLengthM;
				layer2.exitDirection = dir;
				layer2.surfaceNormal = normal;
				layer2.exitVelocity = outcome.residualVelocity;
				layer2.exitLengthM = outcome.residualLengthM;
				layer2.yawDeg = yawDeg;
				layer2.kYaw = kYaw;
				layer2.tailBrokeFromYaw = tailBrokeFromYaw;
				layer2.tailBreakLengthM = tailBreakLengthM;
				plan.layers.Add(layer2);
				if (outcome.result != 0)
				{
					Vector3 stopPoint = hitPoint + dir * (outcome.penetrationDepthMM / WorldToMM);
					layer2.exitPoint = stopPoint;
					plan.tracePoints.Add(hitPoint);
					plan.tracePoints.Add(stopPoint);
					Debug.Log(string.Format("  ■ ОСТАНОВИЛА слой {0}: {1} {2}мм{3} | глубина {4:F0} из {5:F0} мм | износ {6:F0}% | предел {7}", layerIndex, target.name, plate.thicknessMM, betaNote, outcome.penetrationDepthMM, substanceMM, outcome.erosionFraction * 100f, limitNote));
					plan.verdict = ShotVerdict.Stopped;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T((outcome.erosionOnsetMax <= ShatteredCoreOnset) ? "verdict.stuck_in" : "verdict.stuck_shattered");
					plan.verdictColor = ResultPalette.Stopped;
					plan.lineColor = ResultPalette.Stopped;
					plan.lineEnd = stopPoint;
					return plan;
				}
				Vector3 exitPoint = hitPoint + dir * (substanceMM / WorldToMM);
				layer2.exitPoint = exitPoint;
				plan.tracePoints.Add(hitPoint);
				plan.tracePoints.Add(exitPoint);
				Debug.Log(string.Format("  → ПРОБИЛ слой {0}: {1} {2}мм{3} | {4:F0} → {5:F0} м/с | износ {6:F0}% | предел {7}", layerIndex, target.name, plate.thicknessMM, betaNote, v, outcome.residualVelocity, outcome.erosionFraction * 100f, limitNote));
				float deflectionDeg = TrajectoryModel.DeflectionDeg(core, target, v, betaDeg, substanceMM, plan.L0M);
				if (deflectionDeg > 0.001f)
				{
					Vector3 deflectAxis;
					bool towardNormal;
					if (!TrajectoryModel.DeflectTowardNormal)
					{
						Vector3 planar = Vector3.ProjectOnPlane(dir, normal);
						if (planar.sqrMagnitude < 1E-10f)
						{
							deflectAxis = -normal;
							towardNormal = true;
						}
						else
						{
							deflectAxis = planar.normalized;
							towardNormal = false;
						}
					}
					else
					{
						deflectAxis = -normal;
						towardNormal = true;
					}
					Vector3 deflectedDir = Vector3.RotateTowards(dir, deflectAxis, deflectionDeg * Mathf.Deg2Rad, 0f).normalized;
					yawDeg += deflectionDeg;
					layer2.exitDirection = deflectedDir;
					float betaAfter;
					string deflectionNote;
					if (towardNormal)
					{
						betaAfter = betaDeg - deflectionDeg;
						if (betaAfter <= 0f)
						{
							betaAfter = 0f;
						}
						deflectionNote = "к нормали";
					}
					else
					{
						betaAfter = deflectionDeg + betaDeg;
						if (90f <= betaAfter)
						{
							betaAfter = 90f;
						}
						deflectionNote = "вдоль плиты (от нормали)";
					}
					Debug.Log(string.Format("  ↳ отклонение +{0:F1}° {1} (β {2:F1}° → {3:F1}°) | уход ({4:F2}, {5:F2}, {6:F2}) | γ={7:F1}°", deflectionDeg, deflectionNote, betaDeg, betaAfter, deflectedDir.x, deflectedDir.y, deflectedDir.z, yawDeg));
				}
				layer2.exitVelocity = outcome.residualVelocity;
				layer2.exitLengthM = outcome.residualLengthM;
				if (outcome.residualVelocity < 60f)
				{
					Debug.Log(string.Format("  ✕ ВЫДОХСЯ после слоя {0}", layerIndex));
					plan.verdict = ShotVerdict.Exhausted;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T("verdict.exhausted");
					plan.verdictColor = ResultPalette.Exhausted;
					plan.lineColor = ResultPalette.Exhausted;
					plan.lineEnd = exitPoint;
					return plan;
				}
				dir = layer2.exitDirection;
				origin = exitPoint - dir * BackEps;
				postEra.effectiveLengthM = outcome.residualLengthM;
				v = outcome.residualVelocity;
			}
			else
			{
				bool critical = decision.evt == TrajectoryModel.ObliqueEvent.CriticalRicochet;
				Vector3 ricochetDir = (dir - normal * (2f * Vector3.Dot(normal, dir))).normalized;
				float speedFactor = (critical ? TrajectoryModel.CriticalRicochetSpeedFactor : TrajectoryModel.RicochetSpeedFactor);
				float erosionOfL0 = (critical ? TrajectoryModel.CriticalRicochetErosionOfL0 : TrajectoryModel.RicochetErosionOfL0);
				float vOut2 = speedFactor * v;
				float erosionFraction = decision.erosionOnset * erosionOfL0;
				float lenBefore = postEra.effectiveLengthM;
				float lenAfter2 = lenBefore - erosionFraction * plan.L0M;
				if (lenAfter2 <= 0f)
				{
					lenAfter2 = 0f;
				}
				yawDeg += TrajectoryModel.RicochetInducedYawDeg;
				float grooveDepthMM = 0f;
				if (critical)
				{
					grooveDepthMM = substanceMM * cosBeta / 3f;
					Debug.Log(string.Format("  ⟲ КРИТИЧЕСКИЙ РИКОШЕТ от слоя {0} (переходный режим): {1} | β={2:F1}° ≈ β_кр={3:F1}° (tan²β={4:F2} в ±10% порога {5:F2}) | борозда {6:F0} мм (≈T/3) | v → {7:F0} м/с, износ {8:F0}% L0, γ={9:F1}° | уход ({10:F2}, {11:F2}, {12:F2})", layerIndex, target.name, betaDeg, decision.betaCritDeg, decision.tan2Beta, decision.tan2Threshold, grooveDepthMM, vOut2, erosionFraction * 100f, yawDeg, ricochetDir.x, ricochetDir.y, ricochetDir.z));
				}
				else
				{
					Debug.Log(string.Format("  ⟲ РИКОШЕТ от слоя {0}: {1} | β={2:F1}° > β_кр={3:F1}° (tan²β={4:F2} > порог {5:F2}) | v → {6:F0} м/с, износ {7:F0}% L0, γ={8:F1}° | уход ({9:F2}, {10:F2}, {11:F2})", layerIndex, target.name, betaDeg, decision.betaCritDeg, decision.tan2Beta, decision.tan2Threshold, vOut2, erosionFraction * 100f, yawDeg, ricochetDir.x, ricochetDir.y, ricochetDir.z));
				}
				LayerPlaybackData layer3 = new LayerPlaybackData();
				layer3.plate = plate;
				layer3.entryPoint = hitPoint;
				layer3.direction = dir;
				layer3.eventKind = (critical ? LayerEventKind.CriticalRicochet : LayerEventKind.Ricochet);
				layer3.outcome = SynthOutcome(vOut2, lenAfter2, lenBefore, plan.L0M);
				layer3.tEffMM = substanceMM;
				layer3.entryVelocity = v;
				layer3.entryLengthM = lenBefore;
				layer3.exitDirection = ricochetDir;
				layer3.exitPoint = hitPoint;
				layer3.surfaceNormal = normal;
				layer3.exitVelocity = vOut2;
				layer3.exitLengthM = lenAfter2;
				layer3.grooveDepthMM = grooveDepthMM;
				layer3.yawDeg = yawDeg;
				layer3.kYaw = 1f;
				plan.layers.Add(layer3);
				plan.tracePoints.Add(hitPoint);
				if (lenAfter2 <= 0f)
				{
					postEra = default(RodPostEraState);
				}
				else
				{
					postEra.effectiveLengthM = lenAfter2;
				}
				if (postEra.effectiveLengthM <= deadLenM || vOut2 < 60f)
				{
					Debug.Log(string.Format("  ✕ стержень мёртв после рикошета (слой {0})", layerIndex));
					plan.verdict = ShotVerdict.Ricochet;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T("verdict.ricochet_worn");
					plan.verdictColor = ResultPalette.Ricochet;
					plan.lineColor = ResultPalette.Ricochet;
					plan.lineEnd = hitPoint;
					return plan;
				}
				dir = ricochetDir;
				origin = hitPoint + ricochetDir * SurfaceEps + normal * BackEps;
				v = vOut2;
			}
			segment++;
			if (segment >= MaxSegments)
			{
				Debug.LogWarning(string.Format("Достигнут предел {0} сегментов на выстрел — расчёт остановлен принудительно.", MaxSegments));
				plan.tracePoints.Add(origin);
				plan.lineEnd = origin;
				return FinalizeFlyoff(plan, v);
			}
		}
		RegisterPassThroughHits(origin, dir, rayDistance, ThreatClass.KineticRod, "стержень", RodKineticEnergyJ(core, postEra.effectiveLengthM, v));
		Vector3 lineEnd = origin + dir * rayDistance;
		plan.tracePoints.Add(lineEnd);
		plan.lineEnd = lineEnd;
		return FinalizeFlyoff(plan, v);
	}

	private ShotPlan BuildShapedChargePlan(ShotPlan plan, Vector3 origin, Vector3 dir, float rayDistance)
	{
		return BuildShapedChargePlan(plan, origin, dir, rayDistance, null);
	}

	private ShotPlan BuildShapedChargePlan(ShotPlan plan, Vector3 origin, Vector3 dir, float rayDistance, Vector3? forcedDetonation)
	{
		ProjectileDefinition def = GetDefinition();
		ShapedChargeSpec spec = def.GetShapedChargeSpec();
		int elements = 300;
		float fuzeStandoffM = Mathf.Max(def.fuzeStandoffMM / 1000f, 0.001f);
		Vector3 detonationPoint = forcedDetonation ?? Vector3.zero;
		bool detonationSet = forcedDetonation.HasValue;
		if (detonationSet)
		{
			plan.detonationPoint = detonationPoint;
			plan.coneDiameterM = spec.coneDiameterM;
			plan.isShapedCharge = true;
			plan.breakupTimeS = ShapedChargeCore.BreakupTime(spec);
			plan.jetTipVelocityMS = spec.vTip;
			plan.jetElementsAtDetonation = elements;
			plan.elementDvMS = (spec.vTip - spec.vTail) / elements;
		}
		Debug.Log(string.Concat(string.Format("=== ВЫСТРЕЛ: кумулятивная струя, CD={0:F0} мм ", spec.coneDiameterM * 1000f), string.Format("(V_tip={0:F0}, V_tail={1:F0} м/с), ", spec.vTip, spec.vTail), string.Format("взрывательный стандоф {0:F0} мм = {1:F2} CD ===", def.fuzeStandoffMM, fuzeStandoffM / spec.coneDiameterM)));
		ThreatClass threatClass = ShotRouting.ClassOf(def);
		if (def.isTandem)
		{
			RunPrecursorPass(plan, origin, dir, def, fuzeStandoffM, forcedDetonation);
		}
		int segment = 0;
		int retiredTotal = 0;
		RaycastHit bestHit;
		ArmorPlate plate;
		while (true)
		{
			if (!FindNearestPlate(origin, dir, out bestHit, out plate))
			{
				break;
			}
			RegisterPassThroughHits(origin, dir, bestHit.distance, threatClass, "кумулятивная струя", -1f);
			int layerIndex = plan.layers.Count + 1;
			Vector3 hitPoint = bestHit.point;
			Vector3 normal;
			int axis;
			EntryFaceNormal(plate, origin, dir, out normal, out axis);
			if (Vector3.Dot(normal, dir) > 0f)
			{
				normal = -normal;
			}
			float betaDeg = Vector3.Angle(-normal, dir);
			float substanceMM = plate.SubstanceThicknessMM(MeasureLosPath(plate, origin, dir, bestHit.distance, betaDeg, out bool fullFaceHit));
			if (axis != 2)
			{
				Debug.Log(string.Concat(string.Format("  ▣ ТОРЦЕВОЕ ПОПАДАНИЕ (струя) в слой {0}: {1} — ", layerIndex, plate.name), string.Format("β к задетой грани {0:F1}°, путь по веществу {1:F0} мм", betaDeg, substanceMM)));
			}
			if (!detonationSet)
			{
				if (ShotRouting.Fires(ShotBranch.ScreenFuze, plate.kind, threatClass))
				{
					detonationPoint = hitPoint;
					Debug.Log(string.Concat("  ⌗ ЭКРАН сработал взрывателем: подрыв НА ЭКРАНЕ вместо ", string.Format("{0:F0} мм перед преградой — дальше работает ", def.fuzeStandoffMM), "расстояние «экран → броня»"));
				}
				else
				{
					detonationPoint = hitPoint - dir * fuzeStandoffM;
				}
				plan.detonationPoint = detonationPoint;
				plan.coneDiameterM = spec.coneDiameterM;
				plan.isShapedCharge = true;
				plan.breakupTimeS = ShapedChargeCore.BreakupTime(spec);
				plan.jetTipVelocityMS = spec.vTip;
				plan.jetElementsAtDetonation = elements;
				plan.elementDvMS = (spec.vTip - spec.vTail) / elements;
				detonationSet = true;
			}
			float standoffM = Mathf.Max(Vector3.Distance(detonationPoint, hitPoint), 0.001f);
			TateModel.Target target = plate.GetTarget();
			string betaNote = ((betaDeg <= 0.5f) ? "" : string.Format(" под {0:F0}°", betaDeg));
			int retired = 0;
			if (ShotRouting.Fires(ShotBranch.EraJet, plate.kind, threatClass) || ShotRouting.Fires(ShotBranch.NeraJet, plate.kind, threatClass))
			{
				float betaRad = betaDeg * Mathf.Deg2Rad;
				ShapedChargeSpec survivingJet;
				int survivingCount;
				bool fullyDefeated;
				int retiredCount;
				float consumedLengthM;
				if (plate.IsNeraPlate)
				{
					NeraSpec neraSpec = plate.GetNeraSpecAt(hitPoint);
					NeraOutcome nera = NeraArmor.Apply(spec, neraSpec, betaRad, standoffM, elements);
					Debug.Log(string.Concat(string.Format("  ◈ НДЗ слой {0}{1}: θ_эфф {2:F0}° ", layerIndex, betaNote, nera.effectiveObliquityRad * 57.29578f), string.Format("(наклон пакета {0:F0}°), V_перед {1:F0} / ", plate.neraMountAngleDeg, nera.frontVelocityMS), string.Format("V_зад {0:F0} м/с (ВЫВЕДЕНЫ, ×{1:F2}) | ", nera.rearVelocityMS, nera.velocityRatio), string.Format("скважность {0:F3}/{1:F3}, съедено ", nera.front.eraDutyCycle, nera.rear.eraDutyCycle), string.Format("{0:F1}+{1:F1} = ", nera.front.eraConsumedLengthM * 1000f, nera.rear.eraConsumedLengthM * 1000f), string.Format("{0:F1} мм струи, снято {1}/{2} элементов", nera.consumedLengthM * 1000f, nera.elementsRetired, elements), neraSpec.blockExpended ? " [блок уже отработал — пластины улетели]" : ""));
					survivingJet = nera.survivingJet;
					survivingCount = nera.survivingElementCount;
					fullyDefeated = nera.fullyDefeated;
					retiredCount = nera.elementsRetired;
					consumedLengthM = nera.consumedLengthM;
				}
				else
				{
					ReactiveArmorSpec eraSpec = plate.GetJetEraSpecAt(hitPoint);
					EraOutcome era = ReactiveArmor.Apply(spec, eraSpec, betaRad, standoffM, elements);
					Debug.Log(string.Concat(string.Format("  ◈ ДЗ слой {0}{1}: скважность {2:F3}, съедено ", layerIndex, betaNote, era.eraDutyCycle), string.Format("{0:F1} мм струи, снято {1}/{2} элементов", era.eraConsumedLengthM * 1000f, era.eraElementsRetired, elements), eraSpec.cassetteExpended ? " [кассета израсходована — инертна]" : ""));
					survivingJet = era.survivingJet;
					survivingCount = era.survivingElementCount;
					fullyDefeated = era.eraFullyDefeated;
					retiredCount = era.eraElementsRetired;
					consumedLengthM = era.eraConsumedLengthM;
				}
				if (plan.jetTimeline == null)
				{
					JetPenetrationOutcome timelineOutcome = ShapedChargeCore.Compute(spec, standoffM, 0f, target.density, target.Rt, elements, craterFeedback: true, velocityDependentRt: true);
					plan.jetTimeline = timelineOutcome.timeline;
				}
				LayerPlaybackData layer = new LayerPlaybackData();
				layer.plate = plate;
				layer.entryPoint = hitPoint;
				layer.direction = dir;
				layer.eventKind = (fullyDefeated ? LayerEventKind.Stopped : LayerEventKind.Penetration);
				layer.outcome = new PenetrationOutcome
				{
					result = PenetrationResult.Stopped,
					residualVelocity = spec.vTip
				};
				layer.exitDirection = dir;
				layer.exitPoint = hitPoint;
				layer.surfaceNormal = normal;
				layer.tEffMM = substanceMM;
				layer.entryVelocity = spec.vTip;
				layer.exitVelocity = spec.vTip;
				layer.entryLengthM = 0f;
				layer.exitLengthM = 0f;
				layer.yawDeg = 0f;
				layer.kYaw = 1f;
				layer.jetElementsBefore = retiredTotal;
				layer.jetElementsRetired = Mathf.Clamp(retiredCount, 0, elements);
				retired = Mathf.Clamp(retiredCount, 0, elements);
				plan.layers.Add(layer);
				plan.tracePoints.Add(hitPoint);
				if (fullyDefeated)
				{
					Debug.Log(string.Concat(string.Format("  ■ {0} СЪЕЛА СТРУЮ ЦЕЛИКОМ на слое {1} ({2:F1} мм, ", plate.IsNeraPlate ? "НДЗ" : "ДЗ", layerIndex, consumedLengthM * 1000f), string.Format("{0} элементов)", retiredCount)));
					plan.verdict = ShotVerdict.Stopped;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T(plate.IsNeraPlate ? "verdict.jet_killed_nera" : "verdict.jet_killed_era", layerIndex);
					plan.verdictColor = ResultPalette.Stopped;
					plan.lineEnd = hitPoint;
					plan.lineColor = ResultPalette.Stopped;
					return plan;
				}
				elements = Mathf.Max(survivingCount, 1);
				spec = survivingJet;
				origin = hitPoint + dir * SurfaceEps;
			}
			else
			{
				JetPenetrationOutcome outcome = ShapedChargeCore.Compute(spec, standoffM, substanceMM / 1000f, target.density, target.Rt, elements, craterFeedback: true, velocityDependentRt: true);
				if (plan.jetTimeline == null)
				{
					plan.jetTimeline = outcome.timeline;
				}
				LayerPlaybackData layer = new LayerPlaybackData();
				layer.plate = plate;
				layer.entryPoint = hitPoint;
				layer.direction = dir;
				layer.eventKind = (outcome.perforated ? LayerEventKind.Penetration : LayerEventKind.Stopped);
				layer.outcome = new PenetrationOutcome
				{
					result = PenetrationResult.Stopped,
					residualVelocity = spec.vTip
				};
				layer.exitDirection = dir;
				layer.surfaceNormal = normal;
				layer.tEffMM = substanceMM;
				layer.entryVelocity = spec.vTip;
				layer.exitVelocity = spec.vTip;
				layer.entryLengthM = 0f;
				layer.exitLengthM = 0f;
				layer.yawDeg = 0f;
				layer.kYaw = 1f;
				layer.jetTimeline = outcome.timeline;
				layer.jetElementsBefore = retiredTotal;
				float penMM = outcome.totalPenetrationM * 1000f;
				layer.jetDepthMargin = penMM / Mathf.Max(substanceMM, 0.001f);
				layer.outcome.penetrationDepthMM = Mathf.Min(penMM, substanceMM);
				plan.layers.Add(layer);
				int elementsSpent = ShapedChargeCore.ElementsSpentOn(outcome.timeline, substanceMM / 1000f);
				layer.jetElementsRetired = elementsSpent;
				Debug.Log(string.Concat(string.Format("  {0} слой {1}: {2} ", outcome.perforated ? "→ ПРОБИЛА" : "■ ОСТАНОВИЛА", layerIndex, target.name), string.Format("{0}мм{1} | стандоф {2:F0} мм ", plate.thicknessMM, betaNote, standoffM * 1000f), string.Format("({0:F1} CD) | глубина {1:F0} из {2:F0} мм | ", standoffM / spec.coneDiameterM, penMM, substanceMM), string.Format("отсечка {0:F0} м/с | израсходовано {1}/{2} элементов ", outcome.geometricCutoffVelocity, elementsSpent, elements), string.Format("(запас по глубине ×{0:F1})", penMM / Mathf.Max(substanceMM, 0.001f))));
				if (!outcome.perforated)
				{
					Vector3 stopPoint = hitPoint + dir * (layer.outcome.penetrationDepthMM / WorldToMM);
					layer.exitPoint = stopPoint;
					plan.tracePoints.Add(hitPoint);
					plan.tracePoints.Add(stopPoint);
					plan.verdict = ShotVerdict.Stopped;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T("verdict.jet_stuck", target.name, layerIndex);
					plan.verdictColor = ResultPalette.Stopped;
					plan.lineColor = ResultPalette.Stopped;
					plan.lineEnd = stopPoint;
					return plan;
				}
				Vector3 exitPoint = hitPoint + dir * (substanceMM / WorldToMM);
				layer.exitPoint = exitPoint;
				plan.tracePoints.Add(hitPoint);
				plan.tracePoints.Add(exitPoint);
				retired = Mathf.Clamp(elementsSpent, 1, elements - 1);
				spec = ShapedChargeCore.RetireFromTip(spec, elements, retired, out int surviving);
				layer.jetElementsRetired = retired;
				if (surviving < 2 || spec.vTail >= spec.vTip)
				{
					Debug.Log(string.Format("  ✕ струя израсходована после слоя {0}", layerIndex));
					plan.verdict = ShotVerdict.Exhausted;
					plan.verdictLayerIndex = plan.layers.Count - 1;
					plan.verdictMessage = Localization.T("verdict.jet_spent", layerIndex);
					plan.verdictColor = ResultPalette.Exhausted;
					plan.lineColor = ResultPalette.Exhausted;
					plan.lineEnd = exitPoint;
					return plan;
				}
				origin = exitPoint + dir * SurfaceEps;
			}
			segment++;
			if (segment >= MaxSegments)
			{
				plan.tracePoints.Add(origin);
				plan.lineEnd = origin;
				float remaining = -1f;
				if (detonationSet)
				{
					remaining = RemainingJetPenetrationMM(spec, elements, Vector3.Distance(detonationPoint, origin));
				}
				return FinalizeFlyoff(plan, spec.vTip, remaining);
			}
			retiredTotal += retired;
		}
		RegisterPassThroughHits(origin, dir, rayDistance, threatClass, "кумулятивная струя", -1f);
		Vector3 lineEnd = origin + dir * rayDistance;
		plan.tracePoints.Add(lineEnd);
		plan.lineEnd = lineEnd;
		float remainingJetMM = -1f;
		if (detonationSet)
		{
			remainingJetMM = RemainingJetPenetrationMM(spec, elements, Vector3.Distance(detonationPoint, origin));
		}
		return FinalizeFlyoff(plan, spec.vTip, remainingJetMM);
	}

	private float RemainingJetPenetrationMM(ShapedChargeSpec jet, int elements, float standoffM)
	{
		if (elements <= 1 || jet.vTail >= jet.vTip)
		{
			return 0f;
		}
		standoffM = Mathf.Max(standoffM, 0.001f);
		TateModel.Target target = TateModel.RHA();
		return ShapedChargeCore.Compute(jet, standoffM, 1f, target.density, target.Rt, elements, craterFeedback: true, velocityDependentRt: true).totalPenetrationM * 1000f;
	}

	private ShotPlan FinalizeFlyoff(ShotPlan plan, float v, float remainingJetMM = -1f)
	{
		if (plan.layers == null || plan.layers.Count == 0)
		{
			Debug.Log("Луч не нашёл брони на траектории — ПРОМАХ.");
			plan.verdict = ShotVerdict.NoTarget;
			plan.verdictMessage = Localization.T("verdict.miss");
			plan.verdictColor = ResultPalette.Miss;
			plan.lineColor = ResultPalette.Miss;
			return plan;
		}
		LayerPlaybackData layerPlaybackData = plan.layers[plan.layers.Count - 1];
		string text = ((layerPlaybackData.plate != null) ? layerPlaybackData.plate.GetTarget().name : "брони");
		plan.verdictLayerIndex = plan.layers.Count - 1;
		if (layerPlaybackData.eventKind == LayerEventKind.Ricochet || layerPlaybackData.eventKind == LayerEventKind.CriticalRicochet)
		{
			plan.verdict = ShotVerdict.Ricochet;
			plan.verdictMessage = Localization.T("verdict.ricochet", text, plan.layers.Count);
			plan.verdictColor = ResultPalette.Ricochet;
			plan.lineColor = ResultPalette.Ricochet;
		}
		else if (layerPlaybackData.eventKind == LayerEventKind.Slide)
		{
			plan.verdict = ShotVerdict.Slide;
			plan.verdictMessage = Localization.T("verdict.slide", text, plan.layers.Count);
			plan.verdictColor = ResultPalette.Slide;
			plan.lineColor = ResultPalette.Slide;
		}
		else
		{
			plan.finalVelocity = v;
			plan.verdict = ShotVerdict.PassedThrough;
			if (remainingJetMM >= 0f)
			{
				Debug.Log(string.Concat("  ⊳ СТРУЯ ПРОШЛА ВСЮ БРОНЮ НАВЫЛЕТ | остаток пробьёт ещё ", string.Format("{0:F0} мм RHA (v_tip уцелевшего элемента {1:F0} м/с — ", remainingJetMM, v), "НЕ «остаточная скорость»: струя не тормозит, она расходуется с головы)"));
				plan.verdictMessage = Localization.T("verdict.through_jet", string.Format("{0:F0}", remainingJetMM));
			}
			else
			{
				Debug.Log(string.Format("  ⊳ ПРОШЁЛ ВСЮ БРОНЮ НАВЫЛЕТ | остаточная {0:F0} м/с", v));
				plan.verdictMessage = Localization.T("verdict.through_rod", string.Format("{0:F0}", v));
			}
			plan.verdictColor = ResultPalette.Perforated;
			plan.lineColor = ResultPalette.Perforated;
		}
		return plan;
	}

	private static PenetrationOutcome SynthOutcome(float vAfter, float lAfter, float lBefore, float L0)
	{
		PenetrationOutcome outcome = default(PenetrationOutcome);
		outcome.result = PenetrationResult.Stopped;
		outcome.residualVelocity = vAfter;
		outcome.residualLengthM = lAfter;
		float num = L0;
		if (num < 1E-04f)
		{
			num = 1E-04f;
		}
		outcome.erosionFraction = Mathf.Clamp01((lBefore - lAfter) / num);
		return outcome;
	}

	private void RunPrecursorPass(ShotPlan plan, Vector3 origin, Vector3 dir, ProjectileDefinition def, float fuzeStandoffWU, Vector3? forcedDetonation)
	{
		if (!forcedDetonation.HasValue)
		{
			BuildPrecursorCharge(plan, origin, dir, def, fuzeStandoffWU);
		}
		else
		{
			Debug.Log("  ⌗ ПРЕДЗАРЯД инициирован решёткой — отдельной струи ему здесь не строится (его подрыв уже описан исходом решётки); дальше идёт основной заряд");
		}
		RaycastHit[] array = Physics.RaycastAll(new Ray(origin, dir), rayDistance);
		int num = 0;
		foreach (RaycastHit raycastHit in array)
		{
			ArmorPlate armorPlate = raycastHit.collider.GetComponent<ArmorPlate>();
			if (armorPlate != null && armorPlate.PrecursorCanArmThis)
			{
				int num2 = armorPlate.CassetteIndexAt(raycastHit.point);
				if (!armorPlate.cassetteExpended)
				{
					armorPlate.SpendCassette(num2);
					plan.precursorArmedPlates.Add(armorPlate);
					num++;
					Debug.Log(string.Concat(string.Format("  ⌗ ПРЕДЗАРЯД снял кассету #{0} на «{1}» ", num2, armorPlate.name), string.Format("(из {0}) — для основного заряда ОНА инертна (V_p = 0), соседние заряжены", armorPlate.CassetteCount)));
				}
			}
		}
		if (plan != null)
		{
			plan.tandemPrecursorArmed = num;
			if (num != 0)
			{
				Debug.Log(string.Concat(string.Format("  ⌗ ПРЕДЗАРЯД снял кассет: {0} | задержка до основного ", num), string.Format("{0:F0} мкс (PLACEHOLDER)", def.interChargeDelayS * 1000000f)));
			}
			else
			{
				Debug.Log(string.Concat(string.Format("  ⌗ ПРЕДЗАРЯД (CD={0:F0} мм, PLACEHOLDER): кассет ", def.precursorConeDiameterMM), "на трассе нет — снимать нечего, основной заряд идёт как моноблок"));
			}
		}
	}

	private void BuildPrecursorCharge(ShotPlan plan, Vector3 origin, Vector3 dir, ProjectileDefinition def, float fuzeStandoffWU)
	{
		RaycastHit bestHit;
		ArmorPlate plate;
		if (!FindNearestPlate(origin, dir, out bestHit, out plate))
		{
			return;
		}
		ShapedChargeSpec spec = def.GetShapedChargeSpec();
		float coneDiameterM = def.precursorConeDiameterMM / 1000f;
		if (coneDiameterM <= 0.001f)
		{
			coneDiameterM = 0.001f;
		}
		spec.coneDiameterM = coneDiameterM;
		Vector3 hitPoint = bestHit.point;
		float minStandoffWU = fuzeStandoffWU * 0.1f;
		float rawStandoffWU = ((!def.precursorInNoseProbe) ? (fuzeStandoffWU - coneDiameterM) : coneDiameterM);
		float standoffWU = rawStandoffWU;
		if (rawStandoffWU < minStandoffWU)
		{
			standoffWU = minStandoffWU;
		}
		Vector3 detonationPoint = hitPoint - dir * standoffWU;
		float standoffM = standoffWU;
		if (standoffWU <= 0.001f)
		{
			standoffM = 0.001f;
		}
		if (plate != null)
		{
			TateModel.Target target = plate.GetTarget();
			JetPenetrationOutcome outcome = ShapedChargeCore.Compute(spec, standoffM, 0f, target.density, target.Rt, 300, craterFeedback: true, velocityDependentRt: true);
			PrecursorCharge precursor = new PrecursorCharge();
			precursor.detonationPoint = detonationPoint;
			precursor.delayS = 0f;
			precursor.coneDiameterM = coneDiameterM;
			precursor.breakupTimeS = ShapedChargeCore.BreakupTime(spec);
			precursor.elementDvMS = (spec.vTip - spec.vTail) / 300f;
			precursor.tipVelocityMS = spec.vTip;
			precursor.reachM = standoffM;
			precursor.timeline = outcome.timeline;
			plan.precursor = precursor;
			Debug.Log(string.Concat(string.Format("  ⌗ ПРЕДЗАРЯД: CD={0:F0} мм (PLACEHOLDER), компоновка — ", coneDiameterM * 1000f), def.precursorInNoseProbe ? "ВЫНОСНОЙ ЗОНД (подрыв ПРИ КАСАНИИ зонда)" : "в теле боеприпаса", string.Format(", подрыв ОДНОВРЕМЕННО с основным, на {0:F0} мм от преграды ", standoffM * 1000f), string.Format("({0:F2} своих CD) против {1:F0} мм ", standoffM / coneDiameterM, fuzeStandoffWU * 1000f), string.Format("у основного — заряды разнесены НА {0:F0} мм ПО МЕСТУ, а не по времени. ", (fuzeStandoffWU - standoffWU) * 1000f), "Предзаряд приходит к цели первым потому, что он ближе.", (rawStandoffWU < minStandoffWU) ? " ⚠ стандоф предзаряда ужат до 1/10 взрывательного: иначе его virtual origin ушёл бы в саму плиту" : "", string.Format(" | заявленная задержка {0:F0} мкс (PLACEHOLDER) в подрыв ", def.interChargeDelayS * 1000000f), "НЕ входит: расход кассеты считается не по времени, а флагом cassetteExpended"));
		}
	}

	private bool ResolveSlatArray(Vector3 origin, Vector3 dir, float warheadDiameterMM, bool tandem, out StandoffDefences.SlatOutcome outcome, out ArmorPlate slat, out Vector3 hitPoint)
	{
		outcome = StandoffDefences.SlatOutcome.PassedBetween;
		slat = null;
		hitPoint = Vector3.zero;
		RaycastHit[] array = Physics.RaycastAll(new Ray(origin, dir), rayDistance);
		float num = float.MaxValue;
		foreach (RaycastHit raycastHit in array)
		{
			if (raycastHit.distance > 1E-05f)
			{
				ArmorPlate armorPlate = raycastHit.collider.GetComponent<ArmorPlate>();
				if (armorPlate != null && ShotRouting.Fires(ShotBranch.SlatResolution, armorPlate.kind, tandem ? ThreatClass.TandemShapedCharge : ThreatClass.ShapedCharge) && num > raycastHit.distance)
				{
					num = raycastHit.distance;
					hitPoint = raycastHit.point;
					slat = armorPlate;
				}
			}
		}
		if (slat == null)
		{
			return false;
		}
		float num2 = StandoffDefences.SlatHitProbability(slat.slatBarWidthMM / 1000f, warheadDiameterMM / 1000f, slat.slatPitchMM / 1000f);
		if (tandem)
		{
			outcome = StandoffDefences.ResolveSlatTandem(num2, Random.value);
		}
		else
		{
			outcome = StandoffDefences.ResolveSlat(num2, slat.slatDudFraction, Random.value, Random.value);
		}
		Debug.Log(string.Concat(string.Format("  ⌗ РЕШЁТКА: пруток {0:F0} мм, шаг {1:F0} мм, ", slat.slatBarWidthMM, slat.slatPitchMM), string.Format("БЧ {0:F0} мм ⇒ P_задеть = {1:F3} (ВЫВЕДЕНО: (пруток+калибр)/шаг)", warheadDiameterMM, num2)));
		string text = ((outcome == StandoffDefences.SlatOutcome.Dud) ? "БОЛВАНКА — взрыватель замкнут, струя не формируется" : ((outcome == StandoffDefences.SlatOutcome.DetonatedAtSlat) ? "ПОДРЫВ НА РЕШЁТКЕ — струя пошла с решёточного стандофа" : "прошёл между прутками — решётка не сработала"));
		string text2 = "";
		if (outcome != 0)
		{
			text2 = (tandem ? " | ТАНДЕМ: «болванки» не бывает — задев инициирует предзаряд, основной идёт дальше" : string.Format(" | разделение болванка/подрыв {0:F2} — ASSUMED, БЕЗ ИСТОЧНИКА", slat.slatDudFraction));
		}
		Debug.Log(string.Concat("    исход: ", text, text2));
		return true;
	}

	private static string SlatOutcomeName(StandoffDefences.SlatOutcome o)
	{
		if (o == StandoffDefences.SlatOutcome.Dud)
		{
			return "БОЛВАНКА — взрыватель замкнут, струя не формируется";
		}
		string text = "прошёл между прутками — решётка не сработала";
		if (o == StandoffDefences.SlatOutcome.DetonatedAtSlat)
		{
			text = "ПОДРЫВ НА РЕШЁТКЕ — струя пошла с решёточного стандофа";
		}
		return text;
	}

	private bool FindNearestPlate(Vector3 pos, Vector3 dir, out RaycastHit bestHit, out ArmorPlate plate)
	{
		bestHit = default(RaycastHit);
		plate = null;
		RaycastHit[] hits = Physics.RaycastAll(pos, dir, rayDistance);
		float num = float.MaxValue;
		for (int i = 0; i < hits.Length; i++)
		{
			RaycastHit raycastHit = hits[i];
			if (raycastHit.distance <= 1E-05f)
			{
				continue;
			}
			ArmorPlate armorPlate = raycastHit.collider.GetComponent<ArmorPlate>();
			if (armorPlate == null)
			{
				continue;
			}
			if (!ShotRouting.Resists(armorPlate.kind))
			{
				continue;
			}
			if (num <= raycastHit.distance)
			{
				continue;
			}
			bestHit = raycastHit;
			plate = armorPlate;
			num = raycastHit.distance;
		}
		return plate != null;
	}

	private void RegisterPassThroughHits(Vector3 from, Vector3 dir, float maxDistWU, ThreatClass threatClass, string threatName, float energyJ)
	{
		if (maxDistWU <= 0f)
		{
			return;
		}
		RaycastHit[] hits = Physics.RaycastAll(from, dir, maxDistWU);
		for (int i = 0; i < hits.Length; i++)
		{
			RaycastHit raycastHit = hits[i];
			if (raycastHit.distance <= 1E-05f)
			{
				continue;
			}
			ArmorPlate armorPlate = raycastHit.collider.GetComponent<ArmorPlate>();
			if (armorPlate == null)
			{
				continue;
			}
			if (ShotRouting.Resists(armorPlate.kind))
			{
				continue;
			}
			if (!ShotRouting.Registers(armorPlate.kind))
			{
				continue;
			}
			armorPlate.RegisterDirectPenetratorHit(threatClass, threatName, energyJ, raycastHit.point);
		}
	}

	private float RodKineticEnergyJ(TateModel.Penetrator core, float rodLenM, float velocityMS)
	{
		float num = diameter * 0.5f / 1000f;
		float num2 = core.density * 3.14159274f * num * num;
		float num3 = rodLenM;
		if (num3 < 0f)
		{
			num3 = 0f;
		}
		return num2 * num3 * 0.5f * velocityMS * velocityMS;
	}

	private float MeasureLosPath(ArmorPlate plate, Vector3 segOrigin, Vector3 dir, float entryDist, float betaDeg, out bool fullFaceHit)
	{
		float num = Mathf.Max(Mathf.Cos(betaDeg * 0.0174532924f), 1E-04f);
		float num2 = plate.thicknessMM / num;
		RaycastHit[] array = Physics.RaycastAll(new Ray(segOrigin + dir * rayDistance, -dir), rayDistance);
		float num3 = -1f;
		foreach (RaycastHit raycastHit in array)
		{
			if (raycastHit.collider.GetComponent<ArmorPlate>() == plate)
			{
				float num4 = rayDistance - raycastHit.distance;
				if (num3 < num4)
				{
					num3 = num4;
				}
			}
		}
		float num5;
		if (num3 > entryDist)
		{
			num5 = (num3 - entryDist) * 1000f;
		}
		else
		{
			num5 = RayExitDistance(plate, segOrigin, dir);
			if (num5 > entryDist)
			{
				num5 = (num5 - entryDist) * 1000f;
			}
			else
			{
				num5 = plate.thicknessMM * 0.1f;
			}
		}
		float num6 = Mathf.Max(0.05f, num2 * 0.001f);
		fullFaceHit = Mathf.Abs(num5 - num2) <= num6;
		if (fullFaceHit)
		{
			num5 = num2;
		}
		else if (num2 > num5)
		{
			Debug.Log(string.Format("  ⚠ Краевое попадание (прорыв кромки): {0} — путь в материале {1:F0} мм вместо {2:F0} мм по полной грани", plate.name, num5, num2));
		}
		if (num5 < 0.01f)
		{
			num5 = 0.01f;
		}
		return num5;
	}

	public static void EntryFaceNormal(ArmorPlate plate, Vector3 worldOrigin, Vector3 worldDir, out Vector3 nOut, out int axis)
	{
		Transform transform = plate.transform;
		nOut = transform.forward;
		axis = 2;
		if (plate.IsRod && RodEntryNormal(plate, worldOrigin, worldDir, out nOut, out axis))
		{
			return;
		}
		nOut = transform.forward;
		axis = 2;
		Vector3 vector = transform.InverseTransformPoint(worldOrigin);
		Vector3 vector2 = transform.InverseTransformPoint(worldOrigin + worldDir);
		Vector3 vector3 = vector2 - vector;
		float num = float.NegativeInfinity;
		float num2 = float.PositiveInfinity;
		int num3 = -1;
		float num4 = -1f;
		for (int i = 0; i < 3; i++)
		{
			float num5 = vector3[i];
			if (1E-09f > Mathf.Abs(num5))
			{
				if (vector[i] < -0.5f || vector[i] > 0.5f)
				{
					return;
				}
				continue;
			}
			float num6 = (-0.5f - vector[i]) / num5;
			float num7 = (0.5f - vector[i]) / num5;
			float num8 = -1f;
			if (num6 > num7)
			{
				float num9 = num6;
				num6 = num7;
				num7 = num9;
				num8 = 1f;
			}
			if (num6 > num)
			{
				num = num6;
				num3 = i;
				num4 = num8;
			}
			if (num7 < num2)
			{
				num2 = num7;
			}
			if (num > num2)
			{
				return;
			}
		}
		if (num3 < 0)
		{
			return;
		}
		Vector3 vector4 = Vector3.zero;
		vector4[num3] = num4;
		nOut = transform.rotation * vector4;
		axis = num3;
	}

	public static bool RodEntryNormal(ArmorPlate plate, Vector3 worldOrigin, Vector3 worldDir, out Vector3 nOut, out int axis)
	{
		Transform transform = plate.transform;
		nOut = transform.forward;
		axis = 2;
		Vector3 vector = transform.right.normalized;
		Vector3 vector2 = ((worldDir.sqrMagnitude > 1E-12f) ? worldDir.normalized : Vector3.forward);
		float num = SceneScale.RodDiameterWorld(plate.thicknessMM) * 0.5f;
		float num2 = SceneScale.FromM(plate.widthM) * 0.5f;
		Vector3 vector3 = worldOrigin - transform.position;
		Vector3 vector4 = vector3 - Vector3.Dot(vector3, vector) * vector;
		Vector3 vector5 = vector2 - Vector3.Dot(vector2, vector) * vector;
		float num3 = vector5.sqrMagnitude;
		float num4 = Vector3.Dot(vector4, vector5);
		num4 += num4;
		float num5 = float.PositiveInfinity;
		if (1E-12f < num3)
		{
			float num6 = num4 * num4 - (vector4.sqrMagnitude - num * num) * num3 * 4f;
			if (num6 >= 0f)
			{
				float num7 = (0f - num4 - Mathf.Sqrt(num6)) / (num3 + num3);
				if (num7 >= 0f && Mathf.Abs(Vector3.Dot(vector3 + num7 * vector2, vector)) <= num2)
				{
					num5 = num7;
				}
			}
		}
		float num8 = float.PositiveInfinity;
		int num9 = 0;
		float num10 = Vector3.Dot(vector2, vector);
		if (Mathf.Abs(num10) > 1E-09f)
		{
			for (int i = -1; i <= 1; i += 2)
			{
				float num11 = ((float)i * num2 - Vector3.Dot(vector3, vector)) / num10;
				if (num11 >= 0f && num11 < num8)
				{
					Vector3 vector6 = vector3 + num11 * vector2;
					Vector3 vector7 = vector6 - Vector3.Dot(vector6, vector) * vector;
					if (vector7.sqrMagnitude <= num * num)
					{
						num8 = num11;
						num9 = i;
					}
				}
			}
		}
		if (num5 == float.PositiveInfinity && num8 == float.PositiveInfinity)
		{
			return false;
		}
		if (num5 <= num8)
		{
			Vector3 vector8 = vector3 + num5 * vector2;
			Vector3 vector9 = vector8 - Vector3.Dot(vector8, vector) * vector;
			if (vector9.sqrMagnitude <= 1E-12f)
			{
				nOut = transform.forward;
				axis = 2;
				return true;
			}
			nOut = vector9.normalized;
			axis = 2;
			return true;
		}
		nOut = vector * num9;
		axis = 0;
		return true;
	}

	private static float RayExitDistance(ArmorPlate plate, Vector3 worldOrigin, Vector3 worldDir)
	{
		Transform transform = plate.transform;
		Vector3 vector = transform.InverseTransformPoint(worldOrigin);
		Vector3 vector2 = transform.InverseTransformPoint(worldOrigin + worldDir);
		Vector3 vector3 = vector2 - vector;
		float num = float.NegativeInfinity;
		float num2 = float.PositiveInfinity;
		for (int i = 0; i < 3; i++)
		{
			float num3 = vector3[i];
			if (1E-09f > Mathf.Abs(num3))
			{
				if (vector[i] < -0.5f || vector[i] > 0.5f)
				{
					return -1f;
				}
				continue;
			}
			float num4 = (-0.5f - vector[i]) / num3;
			float num5 = (0.5f - vector[i]) / num3;
			if (num4 > num5)
			{
				float num6 = num4;
				num4 = num5;
				num5 = num6;
			}
			if (num < num4)
			{
				num = num4;
			}
			if (num5 < num2)
			{
				num2 = num5;
			}
			if (num > num2)
			{
				return -1f;
			}
		}
		return num2;
	}

	public void DrawTracePath(List<Vector3> points, Color c)
	{
		if (GameSettings.TracePath)
		{
			if (points != null && points.Count >= 2)
			{
				line.positionCount = points.Count;
				for (int i = 0; i < points.Count; i++)
				{
					line.SetPosition(i, points[i]);
				}
				line.startColor = c;
				line.endColor = c;
			}
		}
		else
		{
			line.positionCount = 0;
		}
	}

	public void ClearTrace()
	{
		line.positionCount = 0;
	}

	public void AdoptCurrentTransformAsStart()
	{
		startPosition = base.transform.position;
		startRotation = base.transform.rotation;
		startScale = base.transform.localScale;
		startCaptured = true;
	}

	public void PrepareShotRange()
	{
		if (!rayBaseCaptured)
		{
			rayDistanceBase = rayDistance;
			rayBaseCaptured = true;
		}
		rayDistance = ShotRange.Derive(this, rayDistanceBase);
	}

	public bool TryFirstEntryPoint(out Vector3 point)
	{
		PrepareShotRange();
		Vector3 origin = StartPositionWorld;
		Vector3 vector = direction.normalized;
		if (!FindNearestPlate(origin, vector, out RaycastHit bestHit, out ArmorPlate plate))
		{
			point = Vector3.zero;
			return false;
		}
		point = bestHit.point;
		return true;
	}

	public void ResetTransformToStart()
	{
		base.transform.position = startPosition;
		base.transform.rotation = startRotation;
		base.transform.localScale = startScale;
	}

	public float ComputeMassKg()
	{
		float num = diameter / 1000f;
		float num2 = num * 0.785398185f * num;
		TateModel.Penetrator core = GetCore(coreType);
		float num3 = rodLengthMM / 1000f;
		float num4 = num3 * (core.density * num2);
		if (definition == null)
		{
			definition = ProjectileLibrary.Default();
		}
		if (!definition.IsHollowBody)
		{
			return num4;
		}
		return (1f - Mathf.Clamp01(definition.cavityFrac)) * num4;
	}

	public float CoreDensity()
	{
		return GetCore(coreType).density;
	}

	public float CoreYieldPa()
	{
		return GetCore(coreType).Yp;
	}

	public TateModel.Penetrator GetCorePenetrator()
	{
		return GetCore(coreType);
	}

	public ProjectileDefinition GetDefinition()
	{
		if (definition == null)
		{
			definition = ProjectileLibrary.Default();
		}
		return definition;
	}

	public void ApplyDefinition(ProjectileDefinition def)
	{
		if (def == null)
		{
			return;
		}
		definition = def.Clone();
		coreType = def.coreType;
		rodLengthMM = def.rodLengthMM;
		diameter = def.diameterMM;
		combatVelocity = def.velocityMps;
		isLongRod = def.kind == ProjectileClass.LongRod;
		PenetrationPlayer.ReleaseLodgedIfAny();
	}

	public static bool IsBrittleCore(CoreType t)
	{
		return t == CoreType.WC;
	}

	public static float CoreFractureStrain(CoreType t)
	{
		if (t == CoreType.WHA)
		{
			return FractureStrainWHA;
		}
		if (t == CoreType.DU)
		{
			return FractureStrainDU;
		}
		if (t == CoreType.Steel)
		{
			return FractureStrainSteel;
		}
		return 0f;
	}

	public static Color CoreColor(CoreType t)
	{
		if (t == CoreType.WHA)
		{
			return new Color(0.42f, 0.42f, 0.46f, 1f);
		}
		if (t == CoreType.DU)
		{
			return new Color(0.4f, 0.42f, 0.37f, 1f);
		}
		if (t == CoreType.Steel)
		{
			return new Color(0.56f, 0.58f, 0.63f, 1f);
		}
		if (t == CoreType.WC)
		{
			return new Color(0.34f, 0.34f, 0.38f, 1f);
		}
		return new Color(0.5f, 0.5f, 0.55f, 1f);
	}

	private TateModel.Penetrator GetCore(CoreType t)
	{
		if (t == CoreType.DU)
		{
			return TateModel.DU();
		}
		if (t == CoreType.Steel)
		{
			return TateModel.Steel();
		}
		if (t == CoreType.WC)
		{
			return TateModel.WC();
		}
		return TateModel.WHA();
	}

	public Projectile()
	{
		flightSpeed = 20f;
		direction = Vector3.forward;
		combatVelocity = 1100f;
		rodLengthMM = 300f;
		diameter = 30f;
		definition = ProjectileLibrary.Default();
		rayDistance = 20f;
	}
}
