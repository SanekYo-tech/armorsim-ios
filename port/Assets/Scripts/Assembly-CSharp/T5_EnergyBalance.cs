using System;
using UnityEngine;

public class T5_EnergyBalance : MonoBehaviour
{
	[ContextMenu("T5 — баланс энергии")]
	public void RunOne()
	{
		int pass = 0;
		int total = 0;
		CT.Run("T5", "баланс энергии", Check, ref pass, ref total);
	}

	public static bool Check()
	{
		return Check(ContinuumNose.Flat, 16f);
	}

	public static bool Check(ContinuumNose nose, float elementsPerDiameter)
	{
		ContinuumScenario s = new ContinuumScenario
		{
			name = string.Format("T5: 4340 → 4340, нос {0}, D/{1:F0}", nose, elementsPerDiameter),
			elementsPerDiameter = elementsPerDiameter,
			rodDiameterM = 0.02f,
			rodLengthM = 0.1f,
			impactVelocityMps = 1200f,
			nose = nose,
			rodMaterial = ContinuumMaterialId.Steel4340,
			targetKind = ContinuumTargetKind.Plate,
			targetThicknessM = 0.03f,
			targetMaterial = ContinuumMaterialId.Steel4340,
			outputFrames = 40
		};
		CT.ApplyCflOverride(s);
		ContinuumBackend.Run run = CT.RunToEnd(s);
		SolverDiagnostics d = run.diag;
		string line = string.Format("элементов {0}, узлов {1}, шагов {2}, ", d.elementsInitial, d.nodesInitial, d.steps);
		line += string.Format("стена {0:F0} мс", d.wallClockMs);
		Debug.Log("    " + line);
		Debug.Log("    " + string.Format("KE {0:E4} → {1:E4} Дж; IE {2:E4} → {3:E4} Дж", new object[]
		{
			d.kineticInitial,
			d.kinetic,
			d.internalInitial,
			d.internalEnergy
		}));
		string line2 = string.Format("эрозия {0:E4} Дж ({1} элементов, {2:F2} г), ", d.erodedInternal, d.elementsEroded, d.erodedMassKg * 1000f);
		line2 += string.Format("песочные часы {0:E4} Дж, контакт {1:E4} Дж", d.hourglassWork, d.contactWork);
		Debug.Log("    " + line2);
		string line3 = string.Format("обломки {0:E4} Дж ({1} списанных узлов, ", d.debrisKinetic, d.debrisNodes);
		line3 += string.Format("{0:F2} г, p_z {1:E3} кг·м/с)", d.debrisMassKg * 1000f, d.debrisMomentumZ);
		Debug.Log("    " + line3);
		Debug.Log("    " + string.Format("справочно, вязкость {0:E4} Дж — УЖЕ внутри IE, в сумму НЕ входит", d.viscosityWork));
		Debug.Log("    " + string.Format("Δt: минимальный {0:F3} нс, последний {1:F3} нс", d.minTimeStepS * 1e9f, d.lastTimeStepS * 1e9f));
		bool imbalanceOk = CT.True(string.Format("невязка баланса {0:F3} % от KE₀ (порог 2 %)", d.EnergyImbalanceFraction * 100.0), d.EnergyImbalanceFraction <= 0.02);
		bool hourglassOk = CT.True(string.Format("песочные часы съели {0:F3} % внутренней энергии (порог 5 %)", d.HourglassFraction * 100.0), d.HourglassFraction <= 0.05);
		bool nanOk = CT.True("NaN не появилось", !CT.AnyNaN(run.mesh));
		string note = "без статей эрозии/часов/контакта невязка была бы " + string.Format("{0:F2} % — то есть статьи не декоративны", Math.Abs(d.internalEnergy + d.kinetic - d.TotalInitial) / d.kineticInitial * 100.0);
		Debug.Log("    " + note);
		run.ReleaseWorkingSet();
		return nanOk && hourglassOk && imbalanceOk;
	}
}
