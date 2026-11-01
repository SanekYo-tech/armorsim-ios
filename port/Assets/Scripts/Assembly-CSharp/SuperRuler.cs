using System;
using System.Collections.Generic;
using UnityEngine;

public static class SuperRuler
{
	public const float CraterRadiusCalibres = 3f;

	public const float ControlRadiusCalibres = 6f;

	public const float VoidFraction = 0.5f;

	public const float ControlCeilingSteps = 0.5f;

	public static RulerReading CraterDepth(Vector3[] x, float[] V0, SuperBaker.Body plate, Vector3[] initial, float rodDiameterM, float throughFraction, float[] damage, out float alongTilt, out float alongSymmetric)
	{
		RulerReading r = default(RulerReading);
		r.value = float.NaN;
		r.status = RulerStatus.NotApplicable;
		r.why = "";
		alongTilt = 0f;
		alongSymmetric = 0f;
		int count = plate.count;
		if (initial == null || initial.Length < count || count == 0)
		{
			r.why = "нет исходных положений плиты — мерить нечем";
			return r;
		}
		float spacing = MathF.Pow(V0[plate.start], 0.33333334f);
		float craterRadius = rodDiameterM * VoidFraction * CraterRadiusCalibres;
		float controlRadius = rodDiameterM * VoidFraction * ControlRadiusCalibres;
		Vector3 tiltOrigin = plate.centre;
		float centreRadial = Mathf.Sqrt(plate.centre.x * plate.centre.x + plate.centre.y * plate.centre.y);
		if (centreRadial > spacing * 4f)
		{
			Vector3 n = plate.normal;
			n.Normalize();
			if (Math.Abs(n.z) < 0.15f)
			{
				r.why = "ось выстрела не входит в лицевую грань плиты внутри её пятна — линейка неприменима";
				return r;
			}
			float axisZ = Vector3.Dot(plate.faceCentre, n) / n.z - plate.centre.z;
			Vector3 axisLocal = new Vector3(-plate.centre.x, -plate.centre.y, axisZ);
			if (Math.Abs(Vector3.Dot(axisLocal, plate.axisU)) > plate.halfUM || Math.Abs(Vector3.Dot(axisLocal, plate.axisV)) > plate.halfVM)
			{
				r.why = "ось выстрела не входит в лицевую грань плиты внутри её пятна — линейка неприменима";
				return r;
			}
			tiltOrigin = new Vector3(0f, 0f, Vector3.Dot(plate.centre, n) / n.z);
		}
		RadarKahan sx = default(RadarKahan);
		RadarKahan sy = default(RadarKahan);
		RadarKahan sz = default(RadarKahan);
		for (int i = 0; i < count; i++)
		{
			Vector3 d = x[plate.start + i] - initial[i];
			sx.Add(d.x);
			sy.Add(d.y);
			sz.Add(d.z);
		}
		Vector3 mean = new Vector3((float)(sx.Value / count), (float)(sy.Value / count), (float)(sz.Value / count));
		r.rigidDrift = mean.magnitude;
		float legacyDepth = 0f;
		float witnessA = 0f;
		float control = 0f;
		int controlCount = 0;
		RadarKahan csx = default(RadarKahan);
		RadarKahan csy = default(RadarKahan);
		RadarKahan csz = default(RadarKahan);
		List<float> crater = new List<float>(1024);
		for (int j = 0; j < count; j++)
		{
			Vector3 disp = x[plate.start + j] - initial[j];
			float depth = -Vector3.Dot(disp - mean, plate.normal);
			if (legacyDepth < depth)
			{
				legacyDepth = depth;
			}
			float radial = Mathf.Sqrt(initial[j].x * initial[j].x + initial[j].y * initial[j].y);
			bool damaged = damage != null && damage[plate.start + j] >= 1f;
			if (radial <= craterRadius)
			{
				if (!damaged)
				{
					crater.Add(depth);
					if (witnessA < depth)
					{
						witnessA = depth;
					}
				}
			}
			else if (controlRadius < radial)
			{
				if (!damaged && control < depth)
				{
					control = depth;
				}
				csx.Add(disp.x);
				csy.Add(disp.y);
				csz.Add(disp.z);
				controlCount++;
			}
		}
		r.legacyDepthM = legacyDepth;
		r.control = control;
		if (crater.Count == 0)
		{
			r.why = "в радиусе каверны нет ни одной уцелевшей частицы плиты — свидетелю A мерить нечего";
			return r;
		}
		crater.Sort();
		r.witnessA = witnessA;
		int p95 = Math.Max(0, Math.Min(crater.Count - 1, (int)((float)(crater.Count - 1) * 0.95f)));
		r.witnessAp95 = crater[p95];
		Vector3 ctrlMean;
		if (controlCount < 1)
		{
			ctrlMean = mean;
		}
		else
		{
			ctrlMean = new Vector3((float)(csx.Value / controlCount), (float)(csy.Value / controlCount), (float)(csz.Value / controlCount));
		}
		Vector3[] pos = new Vector3[count];
		float[] v0 = new float[count];
		float[] dmg = (damage != null) ? new float[count] : null;
		for (int k = 0; k < count; k++)
		{
			pos[k] = x[plate.start + k];
			v0[k] = V0[plate.start + k];
			if (dmg != null)
			{
				dmg[k] = damage[plate.start + k];
			}
		}
		float marchLen = plate.thicknessM / Math.Abs(plate.normal.z);
		float zCross = Vector3.Dot(plate.faceCentre + ctrlMean, plate.normal) / plate.normal.z;
		Bounds clipBounds = default(Bounds);
		clipBounds.center = new Vector3(0f, 0f, zCross - marchLen * 0.5f);
		clipBounds.extents = new Vector3(0f, 0f, marchLen * 0.5f);
		clipBounds.Expand(8f * spacing);
		Bounds? clip = clipBounds;
		SuperReconstruct.DensityProbe probe = new SuperReconstruct.DensityProbe(pos, v0, spacing, dmg, clip);
		int steps = Math.Max(1, (int)Math.Ceiling(marchLen / (spacing * ControlCeilingSteps)));
		float witnessB = 0f;
		float spall = 0f;
		bool surfaceOpen = true;
		for (int step = 1; step <= steps; step++)
		{
			float z = Math.Min((float)step * ControlCeilingSteps * spacing, marchLen);
			float density = probe.At(new Vector3(0f, 0f, zCross - z));
			float depthNormal = Math.Abs(plate.normal.z) * z;
			bool empty = VoidFraction > density;
			if (surfaceOpen)
			{
				if (empty)
				{
					witnessB = depthNormal;
				}
				else
				{
					surfaceOpen = false;
				}
			}
			else if (empty && spall < depthNormal)
			{
				spall = depthNormal;
			}
		}
		r.witnessB = witnessB;
		r.spallCavityDepthM = spall;
		float maxDepth = Math.Max(r.witnessA, witnessB);
		r.tolerance = Math.Max(spacing + spacing, maxDepth * 0.1f);
		if (r.control <= spacing * 0.5f)
		{
			bool consistent;
			if (throughFraction > 0f)
			{
				consistent = r.witnessA >= plate.thicknessM - 0.5f * spacing;
			}
			else
			{
				consistent = r.witnessA < plate.thicknessM + 0.5f * spacing;
			}
			if (r.tolerance < Math.Abs(r.witnessA - r.witnessB))
			{
				r.status = RulerStatus.Disputed;
				r.why = string.Format("свидетели разошлись: A (вещество) {0:F2} мм против ", r.witnessA * 1000f) + string.Format("B (пустота) {0:F2} мм при допуске {1:F2} мм. ", r.witnessB * 1000f, r.tolerance * 1000f) + "Спор — результат: узкий канал и широкая вмятина — разные величины (для струи читать свидетеля B, разбор S6)";
			}
			else if (!consistent)
			{
				r.status = RulerStatus.Disputed;
				if (throughFraction <= 0f)
				{
					r.why = string.Format("C3: пробития нет, а глубина {0:F2} мм не меньше толщины ", r.witnessA * 1000f) + string.Format("{0:F1} мм — прибор меряет улетевшее вещество (класс S8)", plate.thicknessM * 1000f);
				}
				else
				{
					r.why = string.Format("C3: пробитие есть ({0:P1} массы за тыльной гранью), а глубина ", throughFraction) + string.Format("{0:F2} мм меньше толщины {1:F1} мм", r.witnessA * 1000f, plate.thicknessM * 1000f);
				}
			}
			else
			{
				r.value = r.witnessA;
				r.status = RulerStatus.Agreed;
			}
			float depthRef = r.witnessA;
			if (!float.IsNaN(r.value))
			{
				depthRef = r.value;
			}
			r.rigidDriftDominates = r.rigidDrift > depthRef;
			if (r.rigidDriftDominates)
			{
				string sep = (r.why.Length > 0) ? " " : "";
				r.why = r.why + sep + string.Format("⚠️ C2: жёсткий снос {0:F2} мм больше самой глубины — ", r.rigidDrift * 1000f) + "сноса не работает, всякая глубина этого прогона под подозрением";
			}
			if (controlCount == 0)
			{
				string sep2 = (r.why.Length > 0) ? " " : "";
				r.why = r.why + sep2 + "⚠️ кольца контроля нет (узкая плита) — снос для B взят общий.";
			}
			if (0f < legacyDepth * 0.1f)
			{
				RadarKahan ds = default(RadarKahan);
				RadarKahan dv = default(RadarKahan);
				RadarKahan du = default(RadarKahan);
				for (int i2 = 0; i2 < count; i2++)
				{
					Vector3 disp2 = x[plate.start + i2] - initial[i2];
					float depth2 = -Vector3.Dot(disp2 - mean, plate.normal);
					if (legacyDepth * 0.1f <= depth2)
					{
						Vector3 p = x[plate.start + i2];
						float ox = p.x - tiltOrigin.x;
						float oy = p.y - tiltOrigin.y;
						float oz = p.z - tiltOrigin.z;
						ds.Add(depth2);
						dv.Add((ox * plate.axisV.x + oy * plate.axisV.y + oz * plate.axisV.z) * (double)depth2);
						du.Add((ox * plate.axisU.x + oy * plate.axisU.y + oz * plate.axisU.z) * (double)depth2);
					}
				}
				if (1e-30 < ds.Value)
				{
					alongTilt = (float)(dv.Value / ds.Value);
					alongSymmetric = (float)(du.Value / ds.Value);
				}
			}
			return r;
		}
		r.status = RulerStatus.ControlFailed;
		r.why = string.Format("C1: нетронутое кольцо (r>6·r_снаряда) показывает {0:F3} мм ", r.control * 1000f) + string.Format("при потолке {0:F3} мм — вычитание жёсткого ", maxDepth * 1000f) + "сноса не работает, всякая глубина этого прогона под подозрением";
		return FinishCentroid(r, x, plate, initial, mean, legacyDepth, tiltOrigin, ref alongTilt, ref alongSymmetric);
	}

	private static RulerReading FinishCentroid(RulerReading r, Vector3[] x, SuperBaker.Body plate, Vector3[] initial, Vector3 mean, float legacyDepth, Vector3 origin, ref float alongTilt, ref float alongSymmetric)
	{
		float threshold = legacyDepth * 0.1f;
		if (0f < threshold)
		{
			RadarKahan ds = default(RadarKahan);
			RadarKahan dv = default(RadarKahan);
			RadarKahan du = default(RadarKahan);
			int count = plate.count;
			for (int i = 0; i < count; i++)
			{
				Vector3 disp = x[plate.start + i] - initial[i];
				float depth = -Vector3.Dot(disp - mean, plate.normal);
				if (threshold <= depth)
				{
					Vector3 p = x[plate.start + i];
					float ox = p.x - origin.x;
					float oy = p.y - origin.y;
					float oz = p.z - origin.z;
					ds.Add(depth);
					dv.Add((ox * plate.axisV.x + oy * plate.axisV.y + oz * plate.axisV.z) * (double)depth);
					du.Add((ox * plate.axisU.x + oy * plate.axisU.y + oz * plate.axisU.z) * (double)depth);
				}
			}
			if (1e-30 < ds.Value)
			{
				alongTilt = (float)(dv.Value / ds.Value);
				alongSymmetric = (float)(du.Value / ds.Value);
			}
		}
		return r;
	}

	public static string Describe(in RulerReading r)
	{
		string valueText = (r.status == RulerStatus.Agreed) ? string.Format("{0:F2} мм", r.value * 1000f) : "—";
		string head = string.Format("{0}: value {1}; A {2:F2} (p95 {3:F2}) / ", r.status, valueText, r.witnessA * 1000f, r.witnessAp95 * 1000f);
		string tail = string.Format("B {0:F2} мм; допуск {1:F2} мм; ", r.witnessB * 1000f, r.tolerance * 1000f);
		string control = string.Format("C1 {0:F3} мм; снос {1:F2} мм", r.control * 1000f, r.rigidDrift * 1000f);
		string spall = (r.spallCavityDepthM <= 0f) ? "" : string.Format("; отдельная полость {0:F2} мм", r.spallCavityDepthM * 1000f);
		string legacy = string.Format("; прежний прибор {0:F2} мм", r.legacyDepthM * 1000f);
		string why = (r.why.Length < 1) ? "" : "; " + r.why;
		return head + tail + control + spall + legacy + why;
	}
}
