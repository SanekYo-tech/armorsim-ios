using System;
using System.Collections.Generic;
using UnityEngine;

public static class SuperJet
{
	public const string Material = "COPPER";

	public static SuperJetSpec FromCharge(in ShapedChargeSpec spec, float standoffM, float tipZM)
	{
		float vTipMps = Math.Max(1f, spec.vTip);
		float vTailMps = Mathf.Clamp(spec.vTail, 1f, vTipMps - 1f);
		float lengthM = Math.Max(0.0001f, (1f - vTailMps / vTipMps) * standoffM);
		float jetMass = ShapedChargeCore.JetMass(spec);
		ContinuumProperties copper = ContinuumMaterials.Get(SuperBaker.Material(Material));
		float diameterM = 2f * Mathf.Sqrt(jetMass / (copper.rho0 * Mathf.PI * lengthM));
		return new SuperJetSpec
		{
			lengthM = lengthM,
			diameterM = diameterM,
			vTipMps = vTipMps,
			vTailMps = vTailMps,
			tipZM = tipZM,
			material = Material
		};
	}

	public static SuperJetSpec WithLength(in ShapedChargeSpec spec, float lengthM, float tipZM)
	{
		float vTipMps = Math.Max(1f, spec.vTip);
		float vTailMps = Mathf.Clamp(spec.vTail, 1f, vTipMps - 1f);
		float jetMass = ShapedChargeCore.JetMass(spec);
		ContinuumProperties copper = ContinuumMaterials.Get(SuperBaker.Material(Material));
		float diameterM = 2f * Mathf.Sqrt(jetMass / (copper.rho0 * Mathf.PI * Math.Max(0.0001f, lengthM)));
		return new SuperJetSpec
		{
			lengthM = lengthM,
			diameterM = diameterM,
			vTipMps = vTipMps,
			vTailMps = vTailMps,
			tipZM = tipZM,
			material = Material
		};
	}

	public static float StandoffForLength(in ShapedChargeSpec spec, float lengthM)
	{
		float vTipMps = Math.Max(1f, spec.vTip);
		float vTailMps = Mathf.Clamp(spec.vTail, 1f, vTipMps - 1f);
		return lengthM / (1f - vTailMps / vTipMps);
	}

	public static float BreakupTimeS(in ShapedChargeSpec spec)
	{
		return ShapedChargeCore.BreakupTime(spec);
	}

	public static float HydroDepthM(float lengthM, float rhoJet, float rhoTarget)
	{
		return lengthM * Mathf.Sqrt(rhoJet / Math.Max(1f, rhoTarget));
	}

	public static float JetReachM(Vector3[] pos, SuperBaker.Body jetBody, Vector3 faceCentre, Vector3 faceNormal, float tailFrac = 0.01f)
	{
		int count = jetBody.count;
		if (count <= 0)
		{
			return 0f;
		}
		float[] d = new float[count];
		Vector3 n = faceNormal;
		n.Normalize();
		float proj = faceCentre.x * n.x + faceCentre.y * n.y + faceCentre.z * n.z;
		for (int i = 0; i < count; i++)
		{
			Vector3 p = pos[jetBody.start + i];
			d[i] = proj - (p.x * n.x + p.y * n.y + p.z * n.z);
		}
		Array.Sort(d);
		int tailSkip = (int)Math.Floor(count * tailFrac);
		int idx = Math.Max(0, Math.Min(count - 1, count - tailSkip - 1));
		return Math.Max(0f, d[idx]);
	}

	public static float ChannelDepthM(Vector3[] pos, Vector3[] initial, SuperBaker.Body plate, Vector3 faceCentre, Vector3 faceNormal, float jetDiameterM, float radialFracOfJetD = 0.5f, float tailFrac = 0.001f)
	{
		int count = plate.count;
		if (count <= 0 || initial == null || initial.Length < count)
		{
			return 0f;
		}
		Vector3 n = faceNormal.normalized;
		float proj = faceCentre.x * n.x + faceCentre.y * n.y + faceCentre.z * n.z;
		float radMax = jetDiameterM * radialFracOfJetD;
		List<float> depths = new List<float>(1024);
		for (int i = 0; i < count; i++)
		{
			Vector3 disp = pos[plate.start + i] - initial[i];
			float along = disp.x * n.x + disp.y * n.y + disp.z * n.z;
			Vector3 radial = disp - along * n;
			float rad = Mathf.Sqrt(radial.x * radial.x + radial.y * radial.y + radial.z * radial.z);
			if (rad < radMax)
			{
				continue;
			}
			Vector3 p = pos[plate.start + i];
			depths.Add(proj - (p.x * n.x + p.y * n.y + p.z * n.z));
		}
		if (depths.Count == 0)
		{
			return 0f;
		}
		depths.Sort();
		int tailSkip = (int)Math.Floor(depths.Count * tailFrac);
		int idx = Math.Max(0, Math.Min(depths.Count - 1, depths.Count - tailSkip - 1));
		return Math.Max(0f, depths[idx]);
	}
}
