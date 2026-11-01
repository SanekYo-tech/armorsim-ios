using System.Collections.Generic;
using UnityEngine;

public sealed class RegionBox
{
	public Vector3 centre;

	public Vector3 right;

	public Vector3 up;

	public Vector3 forward;

	public float widthM;

	public float heightM;

	public float depthM;

	public bool auto;

	public static RegionBox Of(ComputeRegion r)
	{
		RegionBox regionBox = new RegionBox();
		regionBox.centre = r.transform.position;
		regionBox.right = r.transform.right;
		regionBox.up = r.transform.up;
		regionBox.forward = r.transform.forward;
		regionBox.widthM = r.widthM;
		regionBox.heightM = r.heightM;
		regionBox.depthM = r.depthM;
		return regionBox;
	}

	public SuperClip ClipIn(SuperScenePose.BakerFrame f)
	{
		Vector3 vector = f.Point(centre);
		Vector3 vector2 = f.Dir(right);
		Vector3 vector3 = f.Dir(up);
		Vector3 vector4 = f.Dir(forward);
		if (vector4.z < 0f)
		{
			vector2 = -vector2;
			vector4 = -vector4;
		}
		return SuperClip.Box(vector, vector2, vector3, vector4, widthM, heightM, depthM);
	}

	public void WorldAxes(SuperScenePose.BakerFrame f, out Vector3 u, out Vector3 v, out Vector3 n)
	{
		u = right;
		v = up;
		n = forward;
		if (f.Dir(forward).z < 0f)
		{
			u = -u;
			n = -n;
		}
	}

	public static RegionBox Auto(SuperScenePose.BakerFrame f, IEnumerable<Component> objects, float padM)
	{
		Vector3 min = Vector3.zero;
		Vector3 max = Vector3.zero;
		bool flag = false;
		foreach (Component item in objects)
		{
			if (item == null)
			{
				continue;
			}
			Renderer[] componentsInChildren = item.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Renderer renderer = componentsInChildren[i];
				if (renderer == null)
				{
					continue;
				}
				Bounds bounds = renderer.bounds;
				for (int j = 0; j < 8; j++)
				{
					Vector3 vector = new Vector3(((j & 1) == 0) ? bounds.min.x : bounds.max.x, ((j & 2) == 0) ? bounds.min.y : bounds.max.y, ((j & 4) == 0) ? bounds.min.z : bounds.max.z);
					Vector3 vector2 = f.Point(vector);
					if (flag)
					{
						min.x = Mathf.Min(min.x, vector2.x);
						min.y = Mathf.Min(min.y, vector2.y);
						min.z = Mathf.Min(min.z, vector2.z);
						max.x = Mathf.Max(max.x, vector2.x);
						max.y = Mathf.Max(max.y, vector2.y);
						max.z = Mathf.Max(max.z, vector2.z);
					}
					else
					{
						min = vector2;
						max = vector2;
						flag = true;
					}
				}
			}
		}
		if (!flag)
		{
			return null;
		}
		min.x = Mathf.Min(min.x, 0f);
		min.y = Mathf.Min(min.y, 0f);
		max.x = Mathf.Max(max.x, 0f);
		max.y = Mathf.Max(max.y, 0f);
		min -= Vector3.one * padM;
		max += Vector3.one * padM;
		Vector3 vector3 = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, (min.z + max.z) * 0.5f - f.zShift);
		RegionBox regionBox = new RegionBox();
		regionBox.centre = f.origin + f.x * vector3.x + f.y * vector3.y + f.z * vector3.z;
		regionBox.right = f.x;
		regionBox.up = f.y;
		regionBox.forward = f.z;
		regionBox.widthM = max.x - min.x;
		regionBox.heightM = max.y - min.y;
		regionBox.depthM = max.z - min.z;
		regionBox.auto = true;
		return regionBox;
	}
}
