using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public static class ViewerFixtures
{
	public const float EtalonImpactVelocity = 1100f;

	private static GeometryTimeline cached;

	private static float maxStepS;

	public static float[] rodMaxVz;

	public static float[] rodMaxVzR;

	public static float[] rodMaxVzZ;

	public static float[] tipZ;

	public static float EtalonMaxStepS => maxStepS;

	public static ContinuumScenario Scenario()
	{
		ContinuumScenario continuumScenario = ContinuumScenario.Etalon();
		continuumScenario.elementsPerDiameter = 16f;
		continuumScenario.durationS = 0.0002f;
		continuumScenario.outputFrames = 200;
		return continuumScenario;
	}

	public static GeometryTimeline EtalonTimeline()
	{
		if (cached != null)
		{
			return cached;
		}
		ContinuumScenario scenario = Scenario();
		ContinuumBackend.Run run = new ContinuumBackend.Run(scenario);
		AxiMesh mesh = run.mesh;
		maxStepS = 0f;
		List<float> vz = new List<float>(1000000);
		List<float> vr0 = new List<float>(1000000);
		List<float> vz0 = new List<float>(1000000);
		List<float> tz = new List<float>(1000000);
		float num = 0f;
		float num2 = scenario.durationS / scenario.outputFrames;
		while (true)
		{
			SampleRod(mesh, run, vz, vr0, vz0, tz);
			while (run.time < num + num2)
			{
				if (run.Finished)
				{
					cached = run.Finish();
					rodMaxVz = vz.ToArray();
					rodMaxVzR = vr0.ToArray();
					rodMaxVzZ = vz0.ToArray();
					tipZ = tz.ToArray();
					run.ReleaseWorkingSet();
					return cached;
				}
				run.Step();
				if (maxStepS < run.diag.lastTimeStepS)
				{
					maxStepS = run.diag.lastTimeStepS;
				}
			}
			num += num2;
		}
	}

	private static void SampleRod(AxiMesh m, ContinuumBackend.Run run, List<float> vz, List<float> vr0, List<float> vz0, List<float> tz)
	{
		int num = 0;
		float num2 = float.MinValue;
		for (int i = 0; i < m.nodeCount; i++)
		{
			if (m.nodeBody[i] == 0 && num2 < m.nodeVz[i])
			{
				num2 = m.nodeVz[i];
				num = i;
			}
		}
		float num3 = float.MinValue;
		for (int j = run.Surface.projNodeStart; j < run.Surface.projNodeEnd; j++)
		{
			int num4 = run.Surface.surfNodes[j];
			if (num3 < m.nodeZ[num4])
			{
				num3 = m.nodeZ[num4];
			}
		}
		vz.Add(num2);
		vr0.Add(m.nodeR[num]);
		vz0.Add(m.nodeZ[num]);
		tz.Add(num3);
	}

	public static float MinZ(NativeArray<Vector2> profile)
	{
		if (!profile.IsCreated || profile.Length == 0)
		{
			return 0f;
		}
		float num = float.MaxValue;
		for (int i = 0; i < profile.Length; i++)
		{
			if (profile[i].y < num)
			{
				num = profile[i].y;
			}
		}
		return num;
	}

	public static string Checksum(GeometryTimeline tl)
	{
		ulong num = 0x14650FB0739D0383uL;
		Mix(ref num, (ulong)BitConverter.SingleToInt32Bits(tl.dtOutput));
		Mix(ref num, (ulong)tl.frames.Count);
		for (int i = 0; i < tl.frames.Count; i++)
		{
			GeometryFrame frame = tl.frames[i];
			Mix(ref num, (ulong)BitConverter.SingleToInt32Bits(frame.time));
			Mix(ref num, (ulong)BitConverter.SingleToInt32Bits(frame.erodedMassKg));
			Mix(ref num, (ulong)BitConverter.SingleToInt32Bits(frame.tipAxialVelocity));
			MixProfile(ref num, frame.projectileProfile);
			MixProfile(ref num, frame.targetProfile);
			MixSeeds(ref num, frame.fragmentSeeds);
			Mix(ref num, (ulong)frame.projectileLoopCount);
			Mix(ref num, (ulong)frame.targetLoopCount);
		}
		return num.ToString("X16");
	}

	private static void MixSeeds(ref ulong h, NativeArray<ContinuumFragmentSeed> s)
	{
		Mix(ref h, (ulong)s.Length);
		for (int i = 0; i < s.Length; i++)
		{
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(s[i].r));
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(s[i].z));
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(s[i].vr));
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(s[i].vz));
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(s[i].massKg));
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(s[i].timeS));
			Mix(ref h, (ulong)s[i].body);
		}
	}

	private static void MixProfile(ref ulong h, NativeArray<Vector2> p)
	{
		Mix(ref h, (ulong)p.Length);
		for (int i = 0; i < p.Length; i++)
		{
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(p[i].x));
			Mix(ref h, (ulong)BitConverter.SingleToInt32Bits(p[i].y));
		}
	}

	private static void Mix(ref ulong h, ulong v)
	{
		h = (((((((((v & 0xFF) ^ h) * 1099511628211 ^ ((v >> 8) & 0xFF)) * 1099511628211 ^ ((v >> 16) & 0xFF)) * 1099511628211 ^ ((v >> 24) & 0xFF)) * 1099511628211 ^ ((v >> 32) & 0xFF)) * 1099511628211 ^ ((v >> 40) & 0xFF)) * 1099511628211 ^ ((v >> 48) & 0xFF)) * 1099511628211 ^ (v >> 56)) * 1099511628211;
	}
}
