using System;
using System.Collections.Generic;
using UnityEngine;

public static class Flat2DCrew
{
	private enum Kind
	{
		Ellipse = 0,
		RoundRect = 1
	}

	private struct Part
	{
		public string name;

		public Vector2 c;

		public Vector2 size;

		public Kind kind;

		public Color32 paint;
	}

	public const string SitId = "crew_sit";

	public const string StandId = "crew_stand";

	private static readonly Color32 Flesh = new Color32(244, 204, 170, 255);

	private static readonly Color32 Bone = new Color32(250, 246, 232, 255);

	private static readonly Color32 BrainC = new Color32(226, 158, 172, 255);

	private static readonly Color32 LungC = new Color32(236, 140, 150, 255);

	private static readonly Color32 HeartC = new Color32(178, 22, 34, 255);

	private static readonly Color32 LiverC = new Color32(118, 38, 28, 255);

	private static readonly Color32 StomachC = new Color32(248, 180, 110, 255);

	private static readonly Color32 GutC = new Color32(228, 138, 112, 255);

	private static readonly Color32 KidneyC = new Color32(150, 50, 60, 255);

	private static readonly Color32 BladderC = new Color32(248, 222, 100, 255);

	private static readonly Color32 TestisC = new Color32(214, 150, 176, 255);

	private static readonly Color32 PenisC = new Color32(222, 150, 140, 255);

	public static bool IsCrewDecal(string id)
	{
		return id == SitId || id == StandId;
	}

	private static Part P(string n, Vector2 c, Vector2 size, Kind k, Color32 col)
	{
		return new Part
		{
			name = n,
			c = c,
			size = size,
			kind = k,
			paint = col
		};
	}

	private static Vector2 Side(Vector3 v)
	{
		return new Vector2(v.z * 0.4f, v.y * 1.3f);
	}

	private static Part FromFigure(string name, string label, Color32 col)
	{
		for (int i = 0; i < CrewFigure.Parts.Length; i++)
		{
			CrewFigure.FigurePart p = CrewFigure.Parts[i];
			if (p.name == name)
			{
				return P(label, Side(p.pos), new Vector2(p.scale.z * 0.4f, p.scale.y * 1.3f), p.shape == CrewShape.Ellipsoid ? Kind.Ellipse : Kind.RoundRect, col);
			}
		}
		throw new InvalidOperationException("Flat2DCrew: в CrewFigure.Parts нет части " + name);
	}

	private static Part FromOrgan(string name, string label, Color32 col, Kind k)
	{
		for (int i = 0; i < CrewAnatomy.Organs.Length; i++)
		{
			CrewOrganBox p = CrewAnatomy.Organs[i];
			if (p.name == name)
			{
				return P(label, Side(p.pos), new Vector2(p.size.z * 0.4f, p.size.y * 1.3f), k, col);
			}
		}
		throw new InvalidOperationException("Flat2DCrew: в CrewAnatomy.Organs нет органа " + name);
	}

	private static List<Part> Build(bool standing)
	{
		Part head = FromFigure("Crew_Head", "голова", Flesh);
		Part neck = FromFigure("Crew_Neck", "шея", Flesh);
		Part chest = FromFigure("Crew_Chest", "грудь", Flesh);
		Part abdomen = FromFigure("Crew_Abdomen", "живот", Flesh);
		Part pelvis = FromFigure("Crew_Pelvis", "таз", Flesh);
		Part shoulder = FromFigure("Crew_ShoulderL", "плечо", Flesh);
		Part upperArm = FromFigure("Crew_UpperArmL", "плечевой отдел руки", Flesh);
		Part elbow = FromFigure("Crew_ElbowL", "локоть", Flesh);
		Part forearm = FromFigure("Crew_ForearmL", "предплечье", Flesh);
		Part hand = FromFigure("Crew_HandL", "кисть руки", Flesh);
		Part thigh = FromFigure("Crew_ThighL", "бедро", Flesh);
		Part knee = FromFigure("Crew_KneeL", "колено", Flesh);
		Part shin = FromFigure("Crew_ShinL", "голень", Flesh);
		Part ankle = FromFigure("Crew_AnkleL", "лодыжка", Flesh);
		Part foot = FromFigure("Crew_FootL", "стопа", Flesh);
		if (standing)
		{
			float x = pelvis.c.x + pelvis.size.x * 0.25f * 0.5f;
			float kneeY = pelvis.c.y - pelvis.size.y * 0.5f - thigh.size.x + 0.04f;
			thigh.c = new Vector2(x, pelvis.c.y - pelvis.size.y * 0.5f - thigh.size.x * 0.5f + 0.04f);
			knee.c = new Vector2(x, kneeY);
			shin.c = new Vector2(x, kneeY - shin.size.y * 0.5f);
			ankle.c = new Vector2(x, kneeY - shin.size.y);
			foot.c = new Vector2(x + foot.size.x * 0.5f - ankle.size.x * 0.5f, ankle.c.y - foot.size.y * 0.5f + 0.01f);
			forearm.c = new Vector2(elbow.c.x, elbow.c.y - forearm.size.x * 0.5f);
			hand.c = new Vector2(elbow.c.x, elbow.c.y - forearm.size.x - hand.size.x * 0.4f);
			thigh.size = new Vector2(thigh.size.y, thigh.size.x);
			forearm.size = new Vector2(forearm.size.y, forearm.size.x);
			hand.size = new Vector2(hand.size.y, hand.size.x);
		}

		List<Part> shell = new List<Part>();
		shell.AddRange(new Part[15]
		{
			shoulder,
			upperArm,
			elbow,
			forearm,
			hand,
			thigh,
			knee,
			shin,
			ankle,
			foot,
			pelvis,
			abdomen,
			chest,
			neck,
			head
		});
		List<Part> bones = new List<Part>
		{
			P("бедренная кость", thigh.c, new Vector2(thigh.size.x * (thigh.size.x < thigh.size.y ? 0.28f : 0.86f), thigh.size.y * (thigh.size.x < thigh.size.y ? 0.86f : 0.28f)), Kind.RoundRect, Bone),
			P("большеберцовая кость", shin.c, new Vector2(shin.size.x * 0.28f, shin.size.y * 0.86f), Kind.RoundRect, Bone),
			P("череп", head.c, head.size * 0.9f, Kind.Ellipse, Bone)
		};
		List<Part> organs = new List<Part>
		{
			Spine(chest, abdomen, pelvis),
			P("почка", new Vector2(abdomen.c.x + abdomen.size.x * -0.3f, abdomen.c.y + abdomen.size.y * 0.05f), new Vector2(0.045f, 0.075f), Kind.Ellipse, KidneyC),
			P("кишечник", new Vector2(abdomen.c.x + abdomen.size.x * 0.1f, abdomen.c.y + abdomen.size.y * -0.4f), new Vector2(0.15f, 0.11f), Kind.Ellipse, GutC),
			P("мочевой пузырь", new Vector2(pelvis.c.x + pelvis.size.x * 0.22f, pelvis.c.y + 0.02f), new Vector2(0.06f, 0.05f), Kind.Ellipse, BladderC),
			P("желудок", new Vector2(abdomen.c.x + abdomen.size.x * 0.24f, abdomen.c.y + abdomen.size.y * 0.12f), new Vector2(0.08f, 0.06f), Kind.Ellipse, StomachC),
			FromOrgan("Organ_Liver", "печень", LiverC, Kind.Ellipse),
			FromOrgan("Organ_LungL", "лёгкое", LungC, Kind.Ellipse),
			FromOrgan("Organ_Heart", "сердце", HeartC, Kind.Ellipse),
			FromOrgan("Organ_Brain", "мозг", BrainC, Kind.Ellipse)
		};
		AddGenitals(organs, pelvis, thigh, standing);
		List<Part> result = new List<Part>(shell);
		result.AddRange(bones);
		result.AddRange(organs);
		return result;
	}

	private static Part Spine(Part chest, Part abd, Part pelvis)
	{
		float width = 0f;
		for (int i = 0; i < CrewAnatomy.Organs.Length; i++)
		{
			if (CrewAnatomy.Organs[i].name == "Organ_Spine")
			{
				width = CrewAnatomy.Organs[i].size.z * 0.4f;
			}
		}
		if (width <= 0f)
		{
			throw new InvalidOperationException("Flat2DCrew: в CrewAnatomy.Organs нет Organ_Spine");
		}
		float left = Mathf.Min(chest.c.x - chest.size.x * 0.5f, Mathf.Min(abd.c.x - abd.size.x * 0.5f, pelvis.c.x - pelvis.size.x * 0.5f));
		float top = chest.c.y + chest.size.y * 0.5f - chest.size.y * 0.12f;
		return P("позвоночник", new Vector2(left + chest.size.x * 0.1f + width * 0.5f, (top + pelvis.c.y) * 0.5f), new Vector2(width, top - pelvis.c.y), Kind.RoundRect, Bone);
	}

	private static void AddGenitals(List<Part> organs, Part pelvis, Part thigh, bool standing)
	{
		float x = pelvis.c.x + pelvis.size.x * 0.5f;
		float y = pelvis.c.y - pelvis.size.y * 0.5f;
		if (standing)
		{
			organs.Add(P("яички", new Vector2(x - 0.01f, y - 0.012f), new Vector2(0.042f, 0.046f), Kind.Ellipse, TestisC));
			organs.Add(P("половой член", new Vector2(x + 0.004f, y - 0.03f), new Vector2(0.03f, 0.085f), Kind.RoundRect, PenisC));
		}
		else
		{
			float top = thigh.c.y + thigh.size.y * 0.5f;
			organs.Add(P("яички", new Vector2(x + 0.004f, top + 0.008f), new Vector2(0.046f, 0.04f), Kind.Ellipse, TestisC));
			organs.Add(P("половой член", new Vector2(x + 0.034f, top + 0.01f), new Vector2(0.085f, 0.03f), Kind.RoundRect, PenisC));
		}
	}

	public static float NaturalHeightM(bool standing)
	{
		float min = float.MaxValue;
		float max = float.MinValue;
		foreach (Part p in Build(standing))
		{
			min = Mathf.Min(min, p.c.y - p.size.y * 0.5f);
			max = Mathf.Max(max, p.c.y + p.size.y * 0.5f);
		}
		return max - min;
	}

	public static List<Flat2DDrawModel.DecalPart> Parts(string id, Vector2 at)
	{
		return Parts(id, at, Flat2DModelScale.S);
	}

	public static List<Flat2DDrawModel.DecalPart> Parts(string id, Vector2 at, float scale)
	{
		List<Part> parts = Build(id == StandId);
		for (int i = 0; i < parts.Count; i++)
		{
			Part p = parts[i];
			p.c.x = 0f - p.c.x;
			parts[i] = p;
		}
		float minY = float.MaxValue;
		float minX = float.MaxValue;
		foreach (Part p in parts)
		{
			minY = Mathf.Min(minY, p.c.y - p.size.y * 0.5f);
			minX = Mathf.Min(minX, p.c.x - p.size.x * 0.5f);
		}
		scale = Mathf.Max(scale, 1E-06f);
		List<Flat2DDrawModel.DecalPart> result = new List<Flat2DDrawModel.DecalPart>(parts.Count);
		foreach (Part p in parts)
		{
			Vector2 c = new Vector2(at.x + (p.c.x - minX) * scale, at.y + (p.c.y - minY) * scale);
			Vector2 half = p.size * scale * 0.5f;
			result.Add(new Flat2DDrawModel.DecalPart
			{
				pts = p.kind == Kind.Ellipse ? Ellipse(c, half, 28) : RoundRect(c, half, 0.35f),
				paint = p.paint,
				name = p.name
			});
		}
		return result;
	}

	private static Vector2[] Ellipse(Vector2 c, Vector2 half, int seg)
	{
		Vector2[] pts = new Vector2[seg];
		for (int i = 0; i < seg; i++)
		{
			float a = (float)i * (Mathf.PI * 2f) / (float)seg;
			pts[i] = new Vector2(c.x + Mathf.Cos(a) * half.x, c.y + Mathf.Sin(a) * half.y);
		}
		return pts;
	}

	private static Vector2[] RoundRect(Vector2 c, Vector2 half, float roundShare)
	{
		float r = Mathf.Min(half.x, half.y) * roundShare;
		List<Vector2> pts = new List<Vector2>(24);
		Vector2[] centres = new Vector2[4]
		{
			new Vector2(half.x - r, half.y - r),
			new Vector2(0f - half.x + r, half.y - r),
			new Vector2(0f - half.x + r, 0f - half.y + r),
			new Vector2(half.x - r, 0f - half.y + r)
		};
		for (int corner = 0; corner < 4; corner++)
		{
			for (int i = 0; i < 6; i++)
			{
				float a = ((float)i / 5f + (float)corner) * (Mathf.PI * 0.5f);
				pts.Add(c + centres[corner] + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
			}
		}
		return pts.ToArray();
	}
}
