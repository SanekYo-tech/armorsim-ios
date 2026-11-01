using System;
using System.Globalization;
using UnityEngine;

public static class CT
{
	public static float CflSafetyOverride;

	public static int lastDriftedDebris;

	public static int Failures { get; private set; }

	public static int Runs { get; private set; }

	public static void Reset()
	{
		Failures = 0;
		Runs = 0;
	}

	public static void Run(string id, string title, Func<bool> body, ref int pass, ref int total)
	{
		total++;
		Runs++;
		Debug.Log("── " + id + ": " + title + " ──");
		try
		{
			if (body())
			{
				pass++;
				Debug.Log("    " + id + " PASS");
			}
			else
			{
				Failures++;
				Debug.LogError("    " + id + " FAIL");
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("ИСКЛЮЧЕНИЕ: {0}", ex));
			Failures++;
			Debug.LogError("    " + id + " FAIL");
		}
	}

	public static void Measure(string id, string title, Action body, ref int measured)
	{
		measured++;
		Debug.Log("── " + id + ": " + title + " ──");
		try
		{
			body();
		}
		catch (Exception ex)
		{
			Debug.LogError(string.Format("ИСКЛЮЧЕНИЕ: {0}", ex));
		}
		Debug.Log("    " + id + " MEASURED (порога нет по построению)");
	}

	public static bool Near(string what, double got, double want, double relTol)
	{
		double denominator = Math.Abs(want) <= 1E-30 ? 1.0 : Math.Abs(want);
		double relative = Math.Abs(got - want) / denominator;
		bool ok = relative <= relTol;
		Debug.Log(string.Format("    {0}: получено {1:G7}, ожидалось {2:G7}, ", what, got, want) + string.Format("отклонение {0:F3} % (допуск {1:F2} %) — {2}", relative * 100.0, relTol * 100.0, ok ? "ok" : "МИМО"));
		return ok;
	}

	public static bool InRange(string what, double got, double lo, double hi)
	{
		bool ok = got >= lo && got <= hi;
		Debug.Log(string.Format("    {0}: получено {1:G7}, вилка [{2:G7}; {3:G7}] — {4}", what, got, lo, hi, ok ? "ok" : "МИМО"));
		return ok;
	}

	public static bool True(string what, bool ok)
	{
		Debug.Log("    " + what + " — " + (ok ? "ok" : "МИМО"));
		return ok;
	}

	public static void Info(string what)
	{
		Debug.Log("    " + what);
	}

	public static void ApplyCflOverride(ContinuumScenario s)
	{
		if (s != null && !float.IsNaN(CflSafetyOverride))
		{
			s.cflSafety = CflSafetyOverride;
		}
	}

	public static ContinuumProperties[] BaseTable()
	{
		int count = Enum.GetValues(typeof(ContinuumMaterialId)).Length;
		ContinuumProperties[] table = new ContinuumProperties[count];
		for (int i = 0; i < count; i++)
		{
			table[i] = ContinuumMaterials.Get((ContinuumMaterialId)i);
		}
		return table;
	}

	public static void MakeStrengthless(ContinuumProperties[] t, ContinuumMaterialId id)
	{
		t[(int)id].JC_A = 0f;
		t[(int)id].JC_B = 0f;
		t[(int)id].hasASB = 0;
		t[(int)id].hasDamage = 0;
	}

	public static void MakeRigidPlastic(ContinuumProperties[] t, ContinuumMaterialId id)
	{
		t[(int)id].JC_A = 1E+15f;
		t[(int)id].JC_B = 0f;
		t[(int)id].hasASB = 0;
		t[(int)id].hasDamage = 0;
	}

	public static void LiveExtentZ(AxiMesh m, byte body, out float zMin, out float zMax)
	{
		zMin = float.MaxValue;
		zMax = float.MinValue;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] != 0 && m.elemBody[e] == body)
			{
				for (int corner = 0; corner < 4; corner++)
				{
					float z = m.nodeZ[m.elemNodes[e * 4 + corner]];
					zMin = Mathf.Min(zMin, z);
					zMax = Mathf.Max(zMax, z);
				}
			}
		}
	}

	public static float MaxRadiusInWindow(AxiMesh m, byte body, float z0, float z1)
	{
		float radius = 0f;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] != 0 && m.elemBody[e] == body)
			{
				for (int corner = 0; corner < 4; corner++)
				{
					int node = m.elemNodes[e * 4 + corner];
					float z = m.nodeZ[node];
					if (z >= z0 && z <= z1 && m.nodeR[node] > radius)
					{
						radius = m.nodeR[node];
					}
				}
			}
		}
		return radius;
	}

	public static float CraterDepth(AxiMesh m, float axialRadius, float targetExtentM)
	{
		int drifted = 0;
		float depth = 0f;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemBody[e] == 1 && m.elemAlive[e] == 0 && m.elemRc[e] <= axialRadius)
			{
				float z = 0f;
				for (int corner = 0; corner < 4; corner++)
				{
					z += m.nodeZ[m.elemNodes[e * 4 + corner]];
				}
				z *= 0.25f;
				if (z < 0f || z > targetExtentM)
				{
					drifted++;
				}
				else if (z > depth)
				{
					depth = z;
				}
			}
		}
		lastDriftedDebris = drifted;
		return depth;
	}

	public static float TipAdvance(AxiMesh m, float sofar)
	{
		float tip = float.MinValue;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] != 0 && m.elemBody[e] == 0)
			{
				for (int corner = 0; corner < 4; corner++)
				{
					tip = Mathf.Max(tip, m.nodeZ[m.elemNodes[e * 4 + corner]]);
				}
			}
		}
		return sofar < tip && tip < 1E+09f ? tip : sofar;
	}

	public static bool AnyNaN(AxiMesh m)
	{
		for (int i = 0; i < m.nodeCount; i++)
		{
			if (float.IsNaN(m.nodeR[i]) || float.IsNaN(m.nodeZ[i]) || float.IsNaN(m.nodeVr[i]) || float.IsNaN(m.nodeVz[i]))
			{
				return true;
			}
		}
		for (int e = 0; e < m.elemCount; e++)
		{
			if (float.IsNaN(m.elemP[e]) || float.IsNaN(m.elemSrr[e]) || float.IsNaN(m.elemEnergy[e]) || float.IsNaN(m.elemTemp[e]))
			{
				return true;
			}
		}
		return false;
	}

	public static ContinuumScenario FingerprintScenario()
	{
		return new ContinuumScenario
		{
			name = "отпечаток",
			rodDiameterM = 0.03f,
			rodLengthM = 0.3f,
			impactVelocityMps = 1100f,
			rodMaterial = ContinuumMaterialId.WHA,
			targetKind = ContinuumTargetKind.SemiInfinite,
			targetThicknessM = 0.2f,
			targetMaterial = ContinuumMaterials.ActiveRhaSet,
			elementsPerDiameter = 16f,
			durationS = 0.0001f,
			outputFrames = 40
		};
	}

	public static string FingerprintOf(ContinuumBackend.Run run, float rodDiameterM)
	{
		float tip = float.MinValue;
		AxiMesh m = run.mesh;
		for (int e = 0; e < m.elemCount; e++)
		{
			if (m.elemAlive[e] != 0 && m.elemBody[e] == 0)
			{
				for (int corner = 0; corner < 4; corner++)
				{
					tip = Mathf.Max(tip, m.nodeZ[m.elemNodes[e * 4 + corner]]);
				}
			}
		}
		float radius = MaxRadiusInWindow(m, 0, tip - rodDiameterM * 0.25f, tip + 1f);
		return string.Format(CultureInfo.InvariantCulture, "R/r₀={0:F6} шагов={1} эродировано={2}", radius / (rodDiameterM * 0.5f), run.StepIndex, run.diag.elementsEroded);
	}

	public static string SolverFingerprint()
	{
		ContinuumScenario s = FingerprintScenario();
		ContinuumBackend.Run run = RunToEnd(s);
		string fingerprint = FingerprintOf(run, s.rodDiameterM);
		run.ReleaseWorkingSet();
		return fingerprint;
	}

	public static ContinuumBackend.Run RunToEnd(ContinuumScenario s, AxiMesh prebuilt = null)
	{
		ContinuumBackend.Run run = new ContinuumBackend.Run(s, prebuilt);
		while (!run.Finished)
		{
			run.Step();
		}
		run.Finish();
		return run;
	}

	static CT()
	{
		CflSafetyOverride = float.NaN;
	}
}
