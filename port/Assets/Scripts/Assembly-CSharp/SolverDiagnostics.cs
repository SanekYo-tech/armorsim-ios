using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class SolverDiagnostics
{
	public struct ElementRemoval
	{
		public float timeS;

		public float zM;

		public float rM;

		public float massKg;

		public float pressurePa;

		public float epsPAtDeath;

		public byte causeMask;

		public byte body;
	}

	public int steps;

	public int elementsInitial;

	public int nodesInitial;

	public int elementsAlive;

	public int elementsEroded;

	public float simulatedTimeS;

	public float minTimeStepS;

	public float lastTimeStepS;

	public double wallClockMs;

	public double kineticInitial;

	public double internalInitial;

	public double kinetic;

	public double internalEnergy;

	public double erodedInternal;

	public double hourglassWork;

	public double contactWork;

	public double viscosityWork;

	public float erodedMassKg;

	public double erosionLedgerJump;

	public int orphanNodes;

	public float orphanMaxSpeed;

	public double debrisKinetic;

	public double debrisMomentumZ;

	public double debrisMomentumR;

	public float debrisMassKg;

	public int debrisNodes;

	public readonly int[] deletionPressureBins;

	public readonly int[] deletionsByCause;

	public readonly float[] deletionCauseMaxPa;

	public readonly float[] deletionCauseMinPa;

	public float deletionPressureMaxPa;

	public double deletionPressureSumPa;

	public const int RemovalLogCap = 400000;

	public readonly List<ElementRemoval> removals;

	public int removalsDropped;

	public int multiCauseRemovals;

	public float targetTensionProbePa;

	public int targetElemsBelowRodFloor;

	public float targetMassBelowRodFloorKg;

	public int deletionsInTension;

	public float deletionTensionMaxPa;

	public double deletionTensionSumPa;

	public readonly int[] deletionTensionBins;

	public const int TensionZBins = 40;

	public readonly int[] tensionZRod;

	public readonly int[] tensionZTarget;

	public float tensionZMinM;

	public float tensionZBinM;

	public float peakTensionPa;

	public float peakTensionR;

	public float peakTensionZ;

	public float peakTensionTimeS;

	public int peakTensionElem;

	public readonly int[] tensionElemsBelow;

	public static readonly float[] TensionThresholdsPa;

	public float peakRodCompressionPa;

	public float peakCompressionPa;

	public float peakCompressionR;

	public float peakCompressionZ;

	public float peakCompressionTimeS;

	public int peakCompressionElem;

	public float impactPressurePa;

	public int strengthlessHeld;

	public float effectiveTargetResistancePa;

	public float interfaceVelocityMps;

	public string resistanceNotApplicable;

	public const float SemiInfiniteMaxDepthFraction = 0.75f;

	public string semiInfiniteViolated;

	public float meshAspectMin;

	public float meshAspectMax;

	public float meshSkewMaxDeg;

	public float meshMinDtAtStartS;

	public float tipFlatMinAdmissibleOverD;

	public int elementsTrimmed;

	public float initialMassKg;

	public float erodedMassRodKg;

	public float erodedMassTargetKg;

	public float initialMassRodKg;

	public float initialMassTargetKg;

	public int dtFloorHits;

	public double hourglassDistortionMean;

	public double hourglassDistortionMax;

	public int hourglassDistortedElements;

	public const double CheckerboardMean = 0.1;

	public readonly List<string> warnings;

	public readonly List<string> provenanceLog;

	private readonly HashSet<string> warnedOnce;

	public double RamRatio => impactPressurePa > 1f ? (double)(peakCompressionPa / impactPressurePa) : 0.0;

	public double TotalNow => kinetic + internalEnergy + erodedInternal + debrisKinetic + hourglassWork + contactWork;

	public double TotalInitial => kineticInitial + internalInitial;

	public double EnergyImbalance => TotalNow - TotalInitial;

	public double EnergyImbalanceFraction => kineticInitial > 1E-12 ? Math.Abs(EnergyImbalance) / kineticInitial : 0.0;

	public double HourglassFraction => internalEnergy + erodedInternal > 1E-12 ? hourglassWork / (internalEnergy + erodedInternal) : 0.0;

	public double ErodedMassFraction => initialMassKg > 1E-12f ? (double)(erodedMassKg / initialMassKg) : 0.0;

	public double ErodedRodFraction => initialMassRodKg > 1E-12f ? (double)(erodedMassRodKg / initialMassRodKg) : 0.0;

	public double ErodedTargetFraction => initialMassTargetKg > 1E-12f ? (double)(erodedMassTargetKg / initialMassTargetKg) : 0.0;

	public static int PrimaryCause(byte mask)
	{
		if ((mask & 1) != 0)
		{
			return 0;
		}
		if ((mask & 2) != 0)
		{
			return 1;
		}
		if ((mask & 4) != 0)
		{
			return 2;
		}
		if ((mask & 8) != 0)
		{
			return 3;
		}
		if ((mask & 16) != 0)
		{
			return 4;
		}
		return (mask & 32) != 0 ? 5 : -1;
	}

	public void LogRemoval(float timeS, float zM, float rM, float massKg, float pressurePa, float epsPAtDeath, byte causeMask, byte body)
	{
		if (Bits(causeMask) > 1)
		{
			multiCauseRemovals++;
		}
		if (removals.Count < RemovalLogCap)
		{
			removals.Add(new ElementRemoval
			{
				timeS = timeS,
				zM = zM,
				rM = rM,
				massKg = massKg,
				pressurePa = pressurePa,
				epsPAtDeath = epsPAtDeath,
				causeMask = causeMask,
				body = body
			});
		}
		else
		{
			removalsDropped++;
		}
	}

	private static int Bits(byte b)
	{
		int count = 0;
		while (b != 0)
		{
			count += b & 1;
			b >>= 1;
		}
		return count;
	}

	public void AddTensionZ(float z, byte body)
	{
		if (tensionZBinM > 0f)
		{
			int bin = Mathf.Clamp((int)Math.Floor((z - tensionZMinM) / tensionZBinM), 0, TensionZBins - 1);
			if (body == 0)
			{
				tensionZRod[bin]++;
			}
			else
			{
				tensionZTarget[bin]++;
			}
		}
	}

	public static string OrNA(bool applicable, string reason, string formatted)
	{
		return applicable ? formatted : "N/A (" + reason + ")";
	}

	public void Warn(string text)
	{
		warnings.Add(text);
	}

	public void WarnOnce(string key, string text)
	{
		if (warnedOnce.Add(key))
		{
			warnings.Add(text);
		}
	}

	public void LogMaterialProvenance(params ContinuumMaterialId[] inUse)
	{
		provenanceLog.Clear();
		provenanceLog.Add("ULTRA / L1: ни одна строка таблицы материалов не помечена MEASURED целиком.");
		provenanceLog.AddRange(ContinuumMaterials.NonMeasuredReport(inUse));
		provenanceLog.Add("Поведение по выбиванию пробки — КАЛИБРОВКА (ε_ASB измерена, Δε_fade и m подогнаны), предсказанием физики не является.");
	}

	public string DeletionHistogram()
	{
		StringBuilder sb = new StringBuilder();
		float[] edges = ErosionPass.PressureBinsPa;
		for (int i = 0; i < deletionPressureBins.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}
			string range;
			if (i == 0)
			{
				range = string.Format("<{0:0.##}", edges[0] / 1E+09f);
			}
			else if (i < edges.Length)
			{
				range = string.Format("{0:0.##}–{1:0.##}", edges[i - 1] / 1E+09f, edges[i] / 1E+09f);
			}
			else
			{
				range = string.Format(">{0:0.##}", edges[edges.Length - 1] / 1E+09f);
			}
			sb.Append(string.Format("{0} ГПа: {1}", range, deletionPressureBins[i]));
		}
		int total = 0;
		for (int i = 0; i < deletionPressureBins.Length; i++)
		{
			total += deletionPressureBins[i];
		}
		sb.Append(string.Format(" | сжатых всего {0}, максимум {1:F2} ГПа, ", total, deletionPressureMaxPa / 1E+09f));
		sb.Append(string.Format("среднее {0:F3} ГПа", total > 0 ? deletionPressureSumPa / total / 1000000000.0 : 0.0));
		return sb.ToString();
	}

	public string TensionHistogram()
	{
		StringBuilder sb = new StringBuilder();
		float[] edges = ErosionPass.PressureBinsPa;
		for (int i = 0; i < deletionTensionBins.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}
			string range;
			if (i == 0)
			{
				range = string.Format("<{0:0.##}", edges[0] / 1E+09f);
			}
			else if (i < edges.Length)
			{
				range = string.Format("{0:0.##}–{1:0.##}", edges[i - 1] / 1E+09f, edges[i] / 1E+09f);
			}
			else
			{
				range = string.Format(">{0:0.##}", edges[edges.Length - 1] / 1E+09f);
			}
			sb.Append(string.Format("−{0} ГПа: {1}", range, deletionTensionBins[i]));
		}
		sb.Append(string.Format(" | всего {0}, максимум −{1:F2} ГПа, ", deletionsInTension, deletionTensionMaxPa / 1E+09f));
		sb.Append(string.Format("среднее −{0:F3} ГПа", deletionsInTension > 0 ? deletionTensionSumPa / deletionsInTension / 1000000000.0 : 0.0));
		return sb.ToString();
	}

	public string TensionZProfile()
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < TensionZBins; i++)
		{
			int rod = tensionZRod[i];
			int target = tensionZTarget[i];
			if (rod != 0 || target != 0)
			{
				sb.Append(string.Format("[{0:F0}мм: с{1}/п{2}] ", ((float)i * tensionZBinM + tensionZMinM) * 1000f, rod, target));
			}
		}
		return sb.Length == 0 ? "(нет)" : sb.ToString();
	}

	public string DeletionCauses()
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < deletionsByCause.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(", ");
			}
			sb.Append(string.Format("{0} {1} ", ElementKill.CauseName(i), deletionsByCause[i]) + string.Format("(P от {0:F2} до {1:F2} ГПа)", deletionCauseMinPa[i] / 1E+09f, deletionCauseMaxPa[i] / 1E+09f));
		}
		return sb.ToString();
	}

	public string Report()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("── ULTRA / решатель сплошной среды: отчёт ──");
		sb.AppendLine(string.Format("  шагов {0}, модельное время {1:F2} мкс, стена {2:F0} мс", steps, simulatedTimeS * 1000000f, wallClockMs));
		sb.AppendLine(string.Format("  элементов {0}/{1} живых, эродировано {2} ", elementsAlive, elementsInitial, elementsEroded) + string.Format("({0:F2} г), узлов {1}", erodedMassKg * 1000f, nodesInitial) + (dtFloorHits > 0 ? string.Format(", ⚠ пол Δt задет {0} раз", dtFloorHits) : ""));
		string rodWear = string.Format("{0:F1} г из {1:F1} г = ", erodedMassRodKg * 1000f, initialMassRodKg * 1000f) + string.Format("{0:F2} %", ErodedRodFraction * 100.0);
		rodWear = OrNA(initialMassRodKg > 1E-12f, "стержня в постановке нет", rodWear);
		string targetWear = string.Format("{0:F1} г из {1:F1} г = ", erodedMassTargetKg * 1000f, initialMassTargetKg * 1000f) + string.Format("{0:F2} %", ErodedTargetFraction * 100.0);
		targetWear = OrNA(initialMassTargetKg > 1E-12f, "преграды в постановке нет", targetWear);
		sb.AppendLine("    износ СТЕРЖНЯ: " + rodWear + " | выедание ПРЕГРАДЫ: " + targetWear);
		sb.AppendLine(string.Format("  шаг по времени: последний {0:F3} нс, минимальный {1:F3} нс", lastTimeStepS * 1E+09f, minTimeStepS * 1E+09f));
		sb.AppendLine(string.Format("  энергия: KE {0:E4} Дж (было {1:E4}), IE {2:E4} Дж", kinetic, kineticInitial, internalEnergy));
		sb.AppendLine(string.Format("           эрозия {0:E4}, обломки {1:E4}, ", erodedInternal, debrisKinetic) + string.Format("песочные часы {0:E4}, контакт {1:E4} Дж", hourglassWork, contactWork));
		sb.AppendLine(string.Format("           справочно вязкость {0:E4} Дж (УЖЕ внутри IE, в баланс не входит)", viscosityWork));
		sb.AppendLine(string.Format("  НЕВЯЗКА БАЛАНСА {0:E4} Дж = {1:F3} % от KE₀ ", EnergyImbalance, EnergyImbalanceFraction * 100.0) + "(порог T5: 2 %)");
		sb.AppendLine(string.Format("  песочные часы: ЧИСЛИТЕЛЬ {0:F0} Дж / знаменатель ", hourglassWork) + string.Format("{0:F0} Дж = {1:F3} % ", internalEnergy + erodedInternal, HourglassFraction * 100.0) + "⚠ ПОТОЛОК 5 % ПРИОСТАНОВЛЕН как ворота, см. итерацию 9");
		sb.AppendLine(string.Format("  обломков СПИСАНО {0} узлов ({1:F2} г), ", debrisNodes, debrisMassKg * 1000f) + string.Format("быстрейший на момент списания {0:F0} м/с; ", orphanMaxSpeed) + string.Format("E_обломков {0:E4} Дж, p_z {1:E3} кг·м/с", debrisKinetic, debrisMomentumZ));
		sb.AppendLine(string.Format("  ворота по давлению удержали элемент без прочности {0} раз (шагов·элементов)", strengthlessHeld));
		sb.AppendLine("  удаления по P+q в момент смерти, СЖАТИЕ: " + DeletionHistogram());
		sb.AppendLine("  удаления по P+q в момент смерти, РАСТЯЖЕНИЕ (откол): " + TensionHistogram());
		sb.AppendLine("  откол по глубине (с — стержень, п — преграда): " + TensionZProfile());
		sb.AppendLine("  удаления по причинам: " + DeletionCauses());
		sb.AppendLine(string.Concat(new string[]
		{
			string.Format("  пик РАСТЯЖЕНИЯ за прогон −{0:F2} ГПа ", peakTensionPa / 1E+09f),
			string.Format("(элемент {0}, r={1:F1}, z={2:F1} мм, ", peakTensionElem, peakTensionR * 1000f, peakTensionZ * 1000f),
			string.Format("t={0:F1} мкс); разных элементов ниже ", peakTensionTimeS * 1000000f),
			string.Format("−1/−2/−4/−8 ГПа: {0}/{1}/", tensionElemsBelow[0], tensionElemsBelow[1]),
			string.Format("{0}/{1}", tensionElemsBelow[2], tensionElemsBelow[3])
		}));
		sb.AppendLine(string.Format("  пик СЖАТИЯ: стержень {0:F2} ГПа, ", peakRodCompressionPa / 1E+09f) + string.Format("всё вещество {0:F2} ГПа (элемент {1}, ", peakCompressionPa / 1E+09f, peakCompressionElem) + string.Format("r={0:F1}, z={1:F1} мм, ", peakCompressionR * 1000f, peakCompressionZ * 1000f) + string.Format("t={0:F1} мкс)", peakCompressionTimeS * 1000000f));
		string ram = string.Format("P_удара (DERIVED) {0:F2} ГПа ⇒ ", impactPressurePa / 1E+09f) + string.Format("ram = max(P+q)/P_удара = {0:F2} ", RamRatio) + "(порога НЕТ сознательно: одного набора мало, чтобы назначить допуск)";
		sb.AppendLine("  ⭐ ТАРАН: " + OrNA(impactPressurePa > 0f, "нет преграды — давление соударения не определено", ram));
		if (semiInfiniteViolated != null)
		{
			sb.AppendLine("  ⛔ " + semiInfiniteViolated);
		}
		string resistance = string.Format("u = {0:F0} м/с ⇒ R_t = ", interfaceVelocityMps) + string.Format("{0:F2} ГПа ", effectiveTargetResistancePa / 1E+09f) + "(аналитика подогнана на 5.3 ГПа — следить в фазе 4)";
		sb.AppendLine("  скорость проникания u и эффективное R_t: " + OrNA(resistanceNotApplicable == null, resistanceNotApplicable ?? "", resistance));
		sb.AppendLine(string.Format("  разрыв леджера на эрозии {0:E4} Дж ", erosionLedgerJump) + string.Format("({0:F2} Дж/элемент, обязан быть нулём)", elementsEroded > 0 ? erosionLedgerJump / elementsEroded : 0.0));
		if (meshAspectMax > 0f)
		{
			sb.AppendLine(string.Format("  качество исходной сетки: отношение сторон {0:F2}…{1:F2}, ", meshAspectMin, meshAspectMax) + string.Format("максимальный перекос {0:F1}°, min Δt_e при t=0 ", meshSkewMaxDeg) + string.Format("{0:F3} нс", meshMinDtAtStartS * 1E+09f) + (tipFlatMinAdmissibleOverD > 0f ? string.Format("; допустимый минимум притупления носка {0:F4}·D", tipFlatMinAdmissibleOverD) : ""));
		}
		sb.AppendLine(string.Format("  искажение песочных часов |Σh·x|/h: среднее {0:F4}, ", hourglassDistortionMean) + string.Format("макс {0:F3}, выше 0.5 элементов {1} ", hourglassDistortionMax, hourglassDistortedElements) + string.Format("(шахматка начинается около {0:F2})", CheckerboardMean));
		if (provenanceLog.Count > 0)
		{
			sb.AppendLine("  ── провенанс констант в работе ──");
			foreach (string line in provenanceLog)
			{
				sb.AppendLine("    " + line);
			}
		}
		if (warnings.Count > 0)
		{
			sb.AppendLine(string.Format("  ── предупреждения ({0}) ──", warnings.Count));
			int shown = 0;
			foreach (string warning in warnings)
			{
				if (shown >= 20)
				{
					break;
				}
				sb.AppendLine("    ⚠ " + warning);
				shown++;
			}
			if (shown == 20)
			{
				sb.AppendLine(string.Format("    … ещё {0}", warnings.Count - shown));
			}
		}
		return sb.ToString();
	}

	public SolverDiagnostics()
	{
		minTimeStepS = float.MaxValue;
		deletionPressureBins = new int[8];
		deletionsByCause = new int[6];
		deletionCauseMaxPa = new float[6];
		deletionCauseMinPa = new float[6];
		removals = new List<ElementRemoval>();
		deletionTensionBins = new int[8];
		tensionZRod = new int[TensionZBins];
		tensionZTarget = new int[TensionZBins];
		tensionZBinM = 1f;
		peakTensionElem = -1;
		tensionElemsBelow = new int[4];
		peakCompressionElem = -1;
		meshAspectMin = float.MaxValue;
		warnings = new List<string>();
		provenanceLog = new List<string>();
		warnedOnce = new HashSet<string>();
	}

	static SolverDiagnostics()
	{
		TensionThresholdsPa = new float[4] { -1E+09f, -2E+09f, -4E+09f, -8E+09f };
	}
}
