using System;
using System.Collections.Generic;
using UnityEngine;

public static class UltraProjection
{
	public struct ProjectedDisc
	{
		public float alongAxisM;

		public float rawThicknessMM;

		public float losThicknessMM;

		public float tiltDeg;

		public float leanSign;

		public ArmorPlate.ArmorType material;

		public bool materialSupported;
	}

	public const string KeyRicochet = "ultra.reject.ricochet";

	public const string KeySlide = "ultra.reject.slide";

	public const string KeyYaw = "ultra.reject.yaw";

	public const string KeyOffAxis = "ultra.reject.off_axis";

	public const string KeyCurved = "ultra.reject.curved";

	public const string KeyOverlap = "ultra.reject.overlap";

	public const string KeyMaterial = "ultra.reject.material";

	public const string KeyNoPlates = "ultra.reject.no_plates";

	public const string KeyThinFrontLayer = "ultra.reject.thin_front_layer";

	public const float ThinFrontLayerCalibres = 0.5f;

	public const string KeyNoJetModel = "ultra.reject.no_jet_model";

	public const string KeyNoHollowBodyModel = "ultra.reject.no_hollow_body_model";

	public const string DeclarationKey = "ultra.declaration.line_of_sight";

	public const float ThicknessQuantumMM = 0.001f;

	public const float WindowMinS = 6E-05f;

	public const float WindowMaxS = 0.0004f;

	public const float WindowMaxManualS = 0.0012f;

	public static readonly float[] OfferedWindowsS = new float[5] { 0f, 0.0001f, 0.0002f, 0.0004f, 0.0008f };

	public static float LineOfSightMM(float thicknessMM, float tiltDeg)
	{
		return thicknessMM / MathF.Max(MathF.Cos(MathF.Abs(tiltDeg) * ((float)Math.PI / 180f)), 0.001f);
	}

	public static float ClampManualWindow(float windowS)
	{
		if (windowS <= 0f)
		{
			return 0f;
		}
		if (windowS < 6E-05f)
		{
			windowS = 6E-05f;
		}
		if (windowS > 0.0012f)
		{
			windowS = 0.0012f;
		}
		return windowS;
	}

	public static float DisplayWindowS(ContinuumScenario s)
	{
		float rho0 = ContinuumMaterials.Get(s.rodMaterial).rho0;
		float num = ContinuumMaterials.Get(s.targetMaterial).rho0;
		float num2 = MathF.Sqrt(rho0 / MathF.Max(1f, num));
		float num3 = s.impactVelocityMps / (1f + 1f / MathF.Max(0.1f, num2));
		return MathF.Min(0.0004f, MathF.Max(6E-05f, s.TargetSpanM * 3f / MathF.Max(1f, num3)));
	}

	public static void ApplyDisplayWindow(ContinuumScenario s)
	{
		if (s.durationS <= 0f)
		{
			s.durationS = DisplayWindowS(s);
		}
	}

	public static float QuantiseMM(float mm)
	{
		return (float)Math.Round(mm / 0.001f, MidpointRounding.AwayFromZero) * 0.001f;
	}

	public static ProjectedDisc[] Layout(UltraShotEdit shot, IList<UltraPlateEdit> plates, out string reasonKey)
	{
		reasonKey = null;
		if (plates == null || plates.Count == 0)
		{
			reasonKey = "ultra.reject.no_plates";
			return null;
		}
		if (MathF.Abs(shot.yawDeg) > 0.0001f)
		{
			reasonKey = "ultra.reject.yaw";
			return null;
		}
		for (int i = 0; i < plates.Count; i++)
		{
			if (plates[i].curved)
			{
				reasonKey = "ultra.reject.curved";
				return null;
			}
			if (MathF.Abs(plates[i].lateralOffsetM) > 1E-06f)
			{
				reasonKey = "ultra.reject.off_axis";
				return null;
			}
		}
		ProjectedDisc[] array = new ProjectedDisc[plates.Count];
		for (int j = 0; j < plates.Count; j++)
		{
			UltraPlateEdit ultraPlateEdit = plates[j];
			array[j] = new ProjectedDisc
			{
				alongAxisM = ultraPlateEdit.alongAxisM,
				rawThicknessMM = ultraPlateEdit.thicknessMM,
				losThicknessMM = QuantiseMM(LineOfSightMM(ultraPlateEdit.thicknessMM, ultraPlateEdit.tiltDeg)),
				tiltDeg = ultraPlateEdit.tiltDeg,
				leanSign = ultraPlateEdit.leanSign,
				material = ultraPlateEdit.material,
				materialSupported = ContinuumGate.TryMapTarget(ultraPlateEdit.material, out _)
			};
		}
		Array.Sort(array, (ProjectedDisc a, ProjectedDisc b) => a.alongAxisM.CompareTo(b.alongAxisM));
		return array;
	}

	public static float[] Spacings(ProjectedDisc[] discs)
	{
		if (discs == null || discs.Length < 2)
		{
			return new float[0];
		}
		float[] array = new float[discs.Length - 1];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = discs[i + 1].alongAxisM - discs[i].alongAxisM;
		}
		return array;
	}

	public static ProjectedScenario Project(UltraShotEdit shot, IList<UltraPlateEdit> plates, UltraRung rung, out string reasonKey, float windowOverrideS = 0f)
	{
		reasonKey = null;
		if (plates == null || plates.Count == 0)
		{
			reasonKey = "ultra.reject.no_plates";
			return null;
		}
		if (!CanRepresent(shot.kind))
		{
			reasonKey = ClassRefusalKey(shot.kind);
			return null;
		}
		ProjectedDisc[] array = Layout(shot, plates, out reasonKey);
		if (array == null || array.Length == 0)
		{
			if (string.IsNullOrEmpty(reasonKey))
			{
				reasonKey = "ultra.reject.no_plates";
			}
			return null;
		}
		if (!ContinuumGate.TryMapCore(shot.core, out ContinuumMaterialId id))
		{
			reasonKey = "ultra.reject.material";
			return null;
		}
		ContinuumLayer[] array2 = new ContinuumLayer[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			if (!ContinuumGate.TryMapTarget(array[i].material, out ContinuumMaterialId id2))
			{
				reasonKey = "ultra.reject.material";
				return null;
			}
			array2[i].thicknessM = array[i].losThicknessMM / 1000f;
			array2[i].material = id2;
		}
		for (int j = 1; j < array.Length; j++)
		{
			float num = QuantiseMM((array[j].alongAxisM - array[j - 1].alongAxisM) * 1000f - (array[j].rawThicknessMM + array[j - 1].losThicknessMM) * 0.5f);
			if (num < 0f)
			{
				reasonKey = "ultra.reject.overlap";
				return null;
			}
			array2[j].gapBeforeM = num / 1000f;
		}
		if (array2.Length >= 2 && array2[1].gapBeforeM <= 0f && shot.calibreMM * 0.5f > array[0].losThicknessMM)
		{
			reasonKey = "ultra.reject.thin_front_layer";
			return null;
		}
		ProjectedDisc projectedDisc = array[0];
		TrajectoryModel.ObliqueDecision obliqueDecision = TrajectoryModel.Classify(CorePenetrator(shot), TargetOf(projectedDisc.material), shot.velocityMps, MathF.Abs(projectedDisc.tiltDeg), projectedDisc.losThicknessMM, shot.calibreMM, shot.analyticNose, shot.ballisticCap, shot.calibreMM / 1000f * MathF.Max(0.1f, shot.lengthOverDiameter));
		if (obliqueDecision.evt == TrajectoryModel.ObliqueEvent.Ricochet || obliqueDecision.evt == TrajectoryModel.ObliqueEvent.CriticalRicochet)
		{
			reasonKey = "ultra.reject.ricochet";
			return null;
		}
		if (obliqueDecision.evt == TrajectoryModel.ObliqueEvent.Slide)
		{
			reasonKey = "ultra.reject.slide";
			return null;
		}
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		for (int k = 0; k < array.Length; k++)
		{
			num2 += array[k].rawThicknessMM;
			num3 += array[k].losThicknessMM;
			num4 = MathF.Max(num4, MathF.Abs(array[k].tiltDeg));
		}
		bool flag = array.Length > 1;
		ContinuumScenario continuumScenario = new ContinuumScenario();
		string text = string.Format("ULTRA: {0} L/D {1:F1} @ {2:F0} → ", shot.core, shot.lengthOverDiameter, shot.velocityMps);
		if (flag)
		{
			continuumScenario.name = text + string.Format("пакет из {0} слоёв, {1:F0} мм по линии визирования", array.Length, num3);
		}
		else
		{
			continuumScenario.name = text + string.Format("{0} {1:F0} мм под {2:F0}°", projectedDisc.material, projectedDisc.rawThicknessMM, projectedDisc.tiltDeg);
		}
		continuumScenario.rodDiameterM = shot.calibreMM / 1000f;
		continuumScenario.rodLengthM = MathF.Max(0.1f, shot.lengthOverDiameter) * continuumScenario.rodDiameterM;
		continuumScenario.impactVelocityMps = shot.velocityMps;
		continuumScenario.nose = shot.nose;
		continuumScenario.noseHalfAngleDeg = shot.noseHalfAngleDeg;
		continuumScenario.rodMaterial = id;
		continuumScenario.targetKind = (ContinuumTargetKind)0;
		continuumScenario.targetThicknessM = num3 / 1000f;
		continuumScenario.targetMaterial = array2[0].material;
		continuumScenario.layers = (flag ? array2 : null);
		continuumScenario.elementsPerDiameter = rung.elementsPerDiameter;
		if (windowOverrideS > 0f)
		{
			float num5 = ClampManualWindow(windowOverrideS);
			if (num5 > 0f)
			{
				continuumScenario.durationS = num5;
			}
		}
		ApplyDisplayWindow(continuumScenario);
		bool flag2 = num4 > 0.0001f || flag;
		return new ProjectedScenario(continuumScenario, flag2, flag2 ? "ultra.declaration.line_of_sight" : null, num4, num2, num3, array.Length, array);
	}

	public static string EquivalenceLine(ProjectedScenario p)
	{
		if (p == null)
		{
			return "";
		}
		if (p.layerCount <= 1)
		{
			if (!p.isLossy)
			{
				return string.Format("{0:F0} мм по нормали — проекция без потерь", p.rawThicknessMM);
			}
			return string.Format("{0:F0} мм под {1:F0}° = {2:F0} мм ", p.rawThicknessMM, p.maxTiltDeg, p.losThicknessMM) + "по линии визирования при нормальном падении";
		}
		if (!p.isLossy)
		{
			return string.Format("пакет из {0} слоёв: {1:F0} мм суммарно по нормали; ", p.layerCount, p.rawThicknessMM) + "зазоры отсчитаны по ходу выстрела";
		}
		return string.Format("пакет из {0} слоёв: {1:F0} мм суммарно, наклон до ", p.layerCount, p.rawThicknessMM) + string.Format("{0:F0}° ⇒ {1:F0} мм по линии визирования; ", p.maxTiltDeg, p.losThicknessMM) + "зазоры отсчитаны по ходу выстрела";
	}

	public static bool CanRepresent(ProjectileClass kind)
	{
		return kind == ProjectileClass.LongRod || kind == ProjectileClass.ArmorPiercing || kind == ProjectileClass.Kinetic;
	}

	public static string ClassRefusalKey(ProjectileClass kind)
	{
		switch (kind)
		{
		case ProjectileClass.LongRod:
		case ProjectileClass.ArmorPiercing:
		case ProjectileClass.Kinetic:
			return null;
		case ProjectileClass.ShapedCharge:
			return "ultra.reject.no_jet_model";
		case ProjectileClass.HollowBody:
			return "ultra.reject.no_hollow_body_model";
		default:
			Debug.LogError(string.Format("[UltraProjection] класс {0} не назван ни представимым, ни отказным — ", kind) + "отказываю общим ключом струи. Допиши строку в ClassRefusalKey И в CanRepresent.");
			return "ultra.reject.no_jet_model";
		}
	}

	public static TateModel.Penetrator CorePenetrator(UltraShotEdit s)
	{
		switch (s.core)
		{
		case Projectile.CoreType.DU:
			return TateModel.DU();
		case Projectile.CoreType.Steel:
			return TateModel.Steel();
		case Projectile.CoreType.WC:
			return TateModel.WC();
		default:
			return TateModel.WHA();
		}
	}

	private static TateModel.Target TargetOf(ArmorPlate.ArmorType t)
	{
		return NeraArmor.PlateTarget(t);
	}
}
