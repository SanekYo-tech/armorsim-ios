using System;
using System.Collections.Generic;
using UnityEngine;

public static class HybridRodLoft
{
	public const int SegPerRing = 16;

	public static int Build(Vector3 anchor, Quaternion rot, float scale, Vector3[] centersM, bool[] alive, float[] restCoordM, Func<float, float> radiusAtW, Vector3 planePointM, Vector3 planeNormalM, float[] ringDropM, float rodRadiusM, float erodedM, List<Vector3> verts, List<int> tris)
	{
		verts.Clear();
		tris.Clear();
		if (centersM == null || alive == null || restCoordM == null)
		{
			return 0;
		}
		List<int> live = new List<int>(centersM.Length);
		for (int i = 0; i < centersM.Length; i++)
		{
			if (alive[i] && erodedM <= restCoordM[i])
			{
				live.Add(i);
			}
		}
		if (live.Count < 2)
		{
			live.Clear();
			for (int j = 0; j < centersM.Length; j++)
			{
				if (alive[j])
				{
					live.Add(j);
				}
			}
			if (live.Count < 2)
			{
				return 0;
			}
		}
		int last = live[live.Count - 1];
		bool hasCut = false;
		int k = 0;
		for (k = last; k > 0; k--)
		{
			if (!alive[k])
			{
				continue;
			}
			if (restCoordM[k] < erodedM && erodedM != restCoordM[k] && erodedM - restCoordM[live[0]] > 1e-09f)
			{
				hasCut = true;
				break;
			}
			break;
		}
		int n = live.Count + (hasCut ? 1 : 0);
		Vector3[] centers = new Vector3[n];
		float[] radii = new float[n];
		float[] drops = new float[n];
		if (hasCut)
		{
			float cutT = (erodedM - restCoordM[k]) / (restCoordM[live[0]] - restCoordM[k]);
			cutT = Mathf.Clamp01(cutT);
			centers[0] = Vector3.Lerp(centersM[k], centersM[live[0]], cutT);
			radii[0] = radiusAtW(erodedM);
			drops[0] = ((ringDropM != null) ? Mathf.Lerp(ringDropM[k], ringDropM[live[0]], cutT) : 0f);
			for (int m = 0; m < live.Count; m++)
			{
				int idx = live[m];
				centers[m + 1] = centersM[idx];
				radii[m + 1] = radiusAtW(restCoordM[idx]);
				drops[m + 1] = ((ringDropM != null) ? ringDropM[idx] : 0f);
			}
		}
		else
		{
			for (int m2 = 0; m2 < live.Count; m2++)
			{
				int idx2 = live[m2];
				centers[m2] = centersM[idx2];
				radii[m2] = radiusAtW(restCoordM[idx2]);
				drops[m2] = ((ringDropM != null) ? ringDropM[idx2] : 0f);
			}
		}
		if (planeNormalM.sqrMagnitude > 1e-06f && scale > 1e-06f && ringDropM != null && rodRadiusM > 1e-09f)
		{
			Vector3 n2 = ((planeNormalM.magnitude > 1e-09f) ? (planeNormalM / planeNormalM.magnitude) : Vector3.zero);
			for (int i2 = 0; i2 < n; i2++)
			{
				float slack = drops[i2] / scale * drops[0] / rodRadiusM;
				float dot = (centers[i2] - planePointM).x * n2.x + (centers[i2] - planePointM).y * n2.y + (centers[i2] - planePointM).z * n2.z;
				slack = Mathf.Clamp(slack, 0f, slack);
				centers[i2] += n2 * slack;
			}
		}
		Vector3[] world = new Vector3[n];
		for (int i3 = 0; i3 < n; i3++)
		{
			world[i3] = rot * (centers[i3] * scale) + anchor;
		}
		Vector3[] normals = new Vector3[n];
		float maxDist = scale * rodRadiusM;
		if (maxDist < 1e-05f)
		{
			maxDist = 1e-05f;
		}
		for (int i4 = 0; i4 < n; i4++)
		{
			int ahead = i4;
			while (ahead < n - 1 && (world[ahead + 1] - world[i4]).magnitude <= maxDist)
			{
				ahead++;
			}
			int behind = i4;
			while (behind > 0 && (world[behind - 1] - world[i4]).magnitude <= maxDist)
			{
				behind--;
			}
			Vector3 dir;
			if (behind < ahead)
			{
				dir = world[ahead] - world[behind];
			}
			else if (i4 == 0)
			{
				int end = n - 1;
				ahead = ((end < 2) ? end : 1);
				behind = ((end < 2) ? (n - 2) : 0);
				dir = world[0] - world[ahead];
			}
			else
			{
				ahead = i4 - 1;
				behind = ((ahead < 1) ? 0 : (i4 - 1));
				dir = world[behind] - world[i4];
			}
			normals[i4] = ((dir.sqrMagnitude > 1e-12f) ? (dir / dir.magnitude) : Vector3.forward);
		}
		Vector3 axis = Vector3.forward;
		if (world.Length != 0 && Vector3.Dot(world[0], axis) * 0.899999976f > 0f)
		{
			axis = Vector3.back;
		}
		for (int i5 = 0; i5 < n; i5++)
		{
			Vector3 radial = world[i5] - Vector3.Dot(world[i5], normals[i5]) * normals[i5];
			Vector3 tangent = axis - Vector3.Dot(axis, normals[i5]) * normals[i5];
			if (tangent.sqrMagnitude <= 1e-10f)
			{
				tangent = Vector3.Cross(normals[i5], Vector3.up);
			}
			Vector3 binormal = Vector3.Cross(normals[i5], tangent);
			Vector3 rN = ((radial.magnitude > 1e-05f) ? (radial / radial.magnitude) : Vector3.zero);
			Vector3 tN = ((tangent.magnitude > 1e-05f) ? (tangent / tangent.magnitude) : Vector3.zero);
			Vector3 bN = ((binormal.magnitude > 1e-05f) ? (binormal / binormal.magnitude) : Vector3.zero);
			float radius = radii[i5] * scale;
			for (int seg = 0; seg < SegPerRing; seg++)
			{
				float ang = (float)seg * (1f / (float)SegPerRing) * (float)Math.PI * 2f;
				float ca = Mathf.Cos(ang);
				float sa = Mathf.Sin(ang);
				verts.Add(world[i5] + (rN * sa + tN * ca + bN * ca) * radius * drops[i5]);
			}
		}
		if (n > 1)
		{
			for (int ring = 1; ring < n; ring++)
			{
				for (int seg2 = 0; seg2 < SegPerRing; seg2++)
				{
					int next = (seg2 + 1) % SegPerRing;
					tris.Add((ring - 1) * SegPerRing + seg2);
					tris.Add((ring - 1) * SegPerRing + next);
					tris.Add(ring * SegPerRing + seg2);
					tris.Add(ring * SegPerRing + seg2);
					tris.Add((ring - 1) * SegPerRing + next);
					tris.Add(ring * SegPerRing + next);
				}
			}
			Vector3 capFront = world[0] + normals[0] * (radii[0] * scale * 0.001f);
			verts.Add(capFront);
			for (int seg3 = 0; seg3 < SegPerRing; seg3++)
			{
				int next2 = (seg3 + 1) % SegPerRing;
				tris.Add(n * SegPerRing);
				tris.Add(seg3);
				tris.Add(next2);
			}
			Vector3 capBack = world[n - 1] - normals[n - 1] * (radii[n - 1] * scale * 0.001f);
			verts.Add(capBack);
			int backBase = n * SegPerRing + 1;
			for (int seg4 = 0; seg4 < SegPerRing; seg4++)
			{
				int next3 = (seg4 + 1) % SegPerRing;
				tris.Add(backBase);
				tris.Add((n - 1) * SegPerRing + seg4);
				tris.Add((n - 1) * SegPerRing + next3);
			}
		}
		return n;
	}
}
