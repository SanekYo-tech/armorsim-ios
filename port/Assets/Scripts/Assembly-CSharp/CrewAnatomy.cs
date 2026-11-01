using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class CrewAnatomy
{
	public struct ShellLayer
	{
		public CrewTissue tissue;

		public float thicknessMm;

		public ShellLayer(CrewTissue t, float mm)
		{
			tissue = t;
			thicknessMm = mm;
		}
	}

	private struct Piece
	{
		public float t0;

		public float t1;

		public Vector3 n0;

		public CrewRegion region;
	}

	public const float Eps = 1E-06f;

	public const int OrganCount = 17;

	public static readonly CrewOrganBox[] Organs = new CrewOrganBox[6]
	{
		O("Organ_Brain", CrewOrgan.Brain, CrewTissue.Brain, new Vector3(0.233f, 0.1f, 0.388f), new Vector3(0f, 0.419f, -0.05f), new Color(0.72f, 0.7f, 0.74f, 1f)),
		O("Organ_Heart", CrewOrgan.Heart, CrewTissue.Viscera, new Vector3(0.15f, 0.085f, 0.225f), new Vector3(-0.02f, 0.146f, -0.113f), new Color(0.6f, 0.34f, 0.34f, 1f)),
		O("Organ_LungL", CrewOrgan.Lung, CrewTissue.Lung, new Vector3(0.14f, 0.15f, 0.375f), new Vector3(-0.165f, 0.185f, -0.2f), new Color(0.52f, 0.56f, 0.6f, 1f)),
		O("Organ_LungR", CrewOrgan.Lung, CrewTissue.Lung, new Vector3(0.14f, 0.15f, 0.375f), new Vector3(0.165f, 0.185f, -0.2f), new Color(0.52f, 0.56f, 0.6f, 1f)),
		O("Organ_Liver", CrewOrgan.Liver, CrewTissue.Viscera, new Vector3(0.23f, 0.085f, 0.32f), new Vector3(0.13f, 0.055f, -0.11f), new Color(0.55f, 0.42f, 0.36f, 1f)),
		O("Organ_Spine", CrewOrgan.Spine, CrewTissue.Bone, new Vector3(0.06f, 0.446f, 0.138f), new Vector3(0f, 0.085f, -0.35f), new Color(0.78f, 0.77f, 0.72f, 1f))
	};

	public const float RibPitchMm = 32f;

	public const float RibWidthMm = 16f;

	public const float RibThicknessMm = 12f;

	private static readonly List<Piece> _pieces = new List<Piece>(32);

	private static readonly List<CrewSpan> _spans = new List<CrewSpan>(64);

	private static readonly ShellLayer[] _layers = new ShellLayer[4];

	private static readonly float[] _organJ = new float[OrganCount];

	private static readonly bool[] _organTouched = new bool[OrganCount];

	private static readonly List<CrewImpactHoles.BodyInterval> _surfaceIntervals = new List<CrewImpactHoles.BodyInterval>(32);

	private static CrewOrganBox O(string n, CrewOrgan org, CrewTissue t, Vector3 size, Vector3 pos, Color c)
	{
		return new CrewOrganBox
		{
			name = n,
			organ = org,
			tissue = t,
			size = size,
			pos = pos,
			color = c
		};
	}

	public static CrewOrganBox NormalizedOrgan(int i)
	{
		CrewOrganBox crewOrganBox = Organs[i];
		crewOrganBox.pos.z *= CrewFigure.ZNormFactor;
		crewOrganBox.size.z *= CrewFigure.ZNormFactor;
		return crewOrganBox;
	}

	public static float Criticality(CrewOrgan o, CrewTissue t)
	{
		switch (o)
		{
		case CrewOrgan.Brain:
		case CrewOrgan.Heart:
			return 4f;
		case CrewOrgan.Lung:
			return 1.5f;
		case CrewOrgan.Liver:
			return 2f;
		case CrewOrgan.Spine:
			return 3f;
		default:
			if (t > CrewTissue.Fat)
			{
				if (t == CrewTissue.Bone)
				{
					return 0.3f;
				}
				return 0.4f;
			}
			return 0.2f;
		}
	}

	public static string OrganName(CrewOrgan o)
	{
		switch (o)
		{
		case CrewOrgan.Brain:
			return "мозг";
		case CrewOrgan.Heart:
			return "сердце";
		case CrewOrgan.Lung:
			return "лёгкое";
		case CrewOrgan.Liver:
			return "печень";
		case CrewOrgan.Spine:
			return "позвоночник";
		case CrewOrgan.Stomach:
			return "желудок";
		case CrewOrgan.Pancreas:
			return "поджелудочная железа";
		case CrewOrgan.Gallbladder:
			return "желчный пузырь";
		case CrewOrgan.SmallIntestine:
			return "тонкая кишка";
		case CrewOrgan.LargeIntestine:
			return "толстая кишка";
		case CrewOrgan.KidneyL:
			return "левая почка";
		case CrewOrgan.KidneyR:
			return "правая почка";
		case CrewOrgan.Bladder:
			return "мочевой пузырь";
		case CrewOrgan.Spleen:
			return "селезёнка";
		case CrewOrgan.TestisL:
			return "левое яичко";
		case CrewOrgan.TestisR:
			return "правое яичко";
		default:
			return "вне органов";
		}
	}

	public static bool RibAt(float yMm)
	{
		return yMm - Mathf.Floor(yMm / RibPitchMm) * RibPitchMm < RibWidthMm;
	}

	public static int ShellLayers(CrewRegion r, float yMm, ShellLayer[] into)
	{
		switch (r)
		{
		case CrewRegion.Head:
			into[0] = new ShellLayer(CrewTissue.Skin, 5f);
			into[1] = new ShellLayer(CrewTissue.Bone, 7f);
			return 2;
		case CrewRegion.Neck:
			into[0] = new ShellLayer(CrewTissue.Skin, 2f);
			into[1] = new ShellLayer(CrewTissue.Fat, 5f);
			into[2] = new ShellLayer(CrewTissue.Muscle, 22f);
			return 3;
		case CrewRegion.Thorax:
			into[0] = new ShellLayer(CrewTissue.Skin, 2f);
			into[1] = new ShellLayer(CrewTissue.Fat, 12f);
			into[2] = new ShellLayer(CrewTissue.Muscle, 12f);
			if (RibAt(yMm))
			{
				into[3] = new ShellLayer(CrewTissue.Bone, RibThicknessMm);
				return 4;
			}
			return 3;
		case CrewRegion.Abdomen:
			into[0] = new ShellLayer(CrewTissue.Skin, 2f);
			into[1] = new ShellLayer(CrewTissue.Fat, 20f);
			into[2] = new ShellLayer(CrewTissue.Muscle, 14f);
			return 3;
		case CrewRegion.Pelvis:
			into[0] = new ShellLayer(CrewTissue.Skin, 2f);
			into[1] = new ShellLayer(CrewTissue.Fat, 14f);
			into[2] = new ShellLayer(CrewTissue.Muscle, 22f);
			into[3] = new ShellLayer(CrewTissue.Bone, 10f);
			return 4;
		default:
			into[0] = new ShellLayer(CrewTissue.Skin, 2f);
			into[1] = new ShellLayer(CrewTissue.Fat, 9f);
			into[2] = new ShellLayer(CrewTissue.Muscle, 25f);
			return 3;
		}
	}

	public static bool HitBox(Vector3 o, Vector3 d, Vector3 c, Vector3 h, out float t0, out float t1, out Vector3 n0)
	{
		t0 = float.NegativeInfinity;
		t1 = float.PositiveInfinity;
		int num = 2;
		float num2 = -1f;
		for (int i = 0; i < 3; i++)
		{
			float num3 = d[i];
			if (Mathf.Abs(num3) < Eps)
			{
				if (Mathf.Abs(o[i] - c[i]) > h[i])
				{
					n0 = Vector3.forward;
					return false;
				}
				continue;
			}
			float num4 = 1f / num3;
			float num5 = (c[i] - h[i] - o[i]) * num4;
			float num6 = (c[i] + h[i] - o[i]) * num4;
			if (num6 < num5)
			{
				float num7 = num5;
				num5 = num6;
				num6 = num7;
			}
			if (num5 > t0)
			{
				t0 = num5;
				num = i;
				num2 = ((num3 <= 0f) ? 1f : (-1f));
			}
			if (num6 < t1)
			{
				t1 = num6;
			}
			if (t0 > t1)
			{
				n0 = Vector3.forward;
				return false;
			}
		}
		if (t0 == float.NegativeInfinity)
		{
			n0 = Vector3.forward;
			return false;
		}
		n0 = Vector3.zero;
		n0[num] = num2;
		return true;
	}

	public static bool HitEllipsoid(Vector3 o, Vector3 d, Vector3 c, Vector3 h, out float t0, out float t1, out Vector3 n0)
	{
		t0 = 0f;
		t1 = 0f;
		n0 = Vector3.forward;
		if (h.x <= Eps || h.y <= Eps || h.z <= Eps)
		{
			return false;
		}
		float num = d.x / h.x;
		float num2 = d.y / h.y;
		float num3 = d.z / h.z;
		float num4 = (o.x - c.x) / h.x;
		float num5 = (o.y - c.y) / h.y;
		float num6 = (o.z - c.z) / h.z;
		float num7 = num * num + num2 * num2 + num3 * num3;
		if (num7 < Eps)
		{
			return false;
		}
		float num8 = 2f * (num4 * num + num5 * num2 + num6 * num3);
		float num9 = num4 * num4 + num5 * num5 + num6 * num6 - 1f;
		float num10 = num8 * num8 - 4f * num7 * num9;
		if (num10 <= 0f)
		{
			return false;
		}
		float num11 = Mathf.Sqrt(num10);
		t0 = (0f - num8 - num11) / (2f * num7);
		t1 = (0f - num8 + num11) / (2f * num7);
		Vector3 vector = new Vector3((num4 + t0 * num) / h.x, (num5 + t0 * num2) / h.y, (num6 + t0 * num3) / h.z);
		float magnitude = vector.magnitude;
		if (magnitude > Eps)
		{
			n0 = vector / magnitude;
			return true;
		}
		n0 = Vector3.forward;
		return true;
	}

	public static CrewTraceResult Trace(Vector3 localPoint, Vector3 localDir, float widthM, float heightM, float depthMM, float massKg, float speedMps, float areaM2, CrewImpactHoles surface = null)
	{
		CrewTraceResult result = default(CrewTraceResult);
		for (int i = 0; i < OrganCount; i++)
		{
			_organJ[i] = 0f;
			_organTouched[i] = false;
		}
		result.organJ = _organJ;
		result.organTouched = _organTouched;
		result.exitSpeedMps = speedMps;
		result.endLocal = localPoint;
		result.stopOrgan = CrewOrgan.None;
		result.trail = "";
		if (massKg <= 0f || speedMps <= 0f || areaM2 <= 0f)
		{
			return result;
		}
		float wmm = Mathf.Max(widthM, 0.0001f) * 1000f;
		float hmm = Mathf.Max(heightM, 0.0001f) * 1000f;
		float dmm = Mathf.Max(depthMM, 0.001f);
		Vector3 o = new Vector3((0f - localPoint.x) * wmm, hmm * localPoint.y, (0f - localPoint.z) * dmm);
		Vector3 d = new Vector3((0f - localDir.x) * wmm, hmm * localDir.y, (0f - localDir.z) * dmm);
		float num = d.magnitude;
		if (num < Eps)
		{
			return result;
		}
		d /= num;
		_pieces.Clear();
		if (surface != null)
		{
			surface.BodyIntervals(o, d, new Vector3(wmm, hmm, dmm), _surfaceIntervals);
			foreach (CrewImpactHoles.BodyInterval item in _surfaceIntervals)
			{
				_pieces.Add(new Piece
				{
					t0 = item.enterMm,
					t1 = item.exitMm,
					n0 = item.normal,
					region = item.region
				});
			}
		}
		else
		{
			for (int j = 0; j < CrewFigure.Parts.Length; j++)
			{
				CrewFigure.FigurePart figurePart = CrewFigure.NormalizedPart(j);
				Vector3 c = new Vector3(figurePart.pos.x * wmm, figurePart.pos.y * hmm, figurePart.pos.z * dmm);
				Vector3 h = new Vector3(figurePart.scale.x * 0.5f * wmm, figurePart.scale.y * 0.5f * hmm, figurePart.scale.z * 0.5f * dmm);
				bool hit;
				float t0;
				float t1;
				Vector3 n0;
				if (figurePart.shape == CrewShape.Ellipsoid)
				{
					hit = HitEllipsoid(o, d, c, h, out t0, out t1, out n0);
				}
				else
				{
					hit = HitBox(o, d, c, h, out t0, out t1, out n0);
				}
				if (hit && t1 > 0f)
				{
					if (t0 < 0f)
					{
						t0 = 0f;
						n0 = -d;
					}
					_pieces.Add(new Piece
					{
						t0 = t0,
						t1 = t1,
						n0 = n0,
						region = figurePart.region
					});
				}
			}
		}
		if (_pieces.Count == 0)
		{
			return result;
		}
		_pieces.Sort(ComparePieces);
		result.hitBody = true;
		Piece piece = _pieces[0];
		result.entryLocal = new Vector3((0f - (d.x * piece.t0 + o.x)) / wmm, (d.y * piece.t0 + o.y) / hmm, (0f - (d.z * piece.t0 + o.z)) / dmm);
		_spans.Clear();
		int num2 = 0;
		while (num2 < _pieces.Count)
		{
			Piece piece2 = _pieces[num2];
			float num3 = piece2.t1;
			CrewRegion region = piece2.region;
			int num4 = num2 + 1;
			while (num4 < _pieces.Count && num3 + Eps >= _pieces[num4].t0)
			{
				if (_pieces[num4].t1 > num3)
				{
					num3 = _pieces[num4].t1;
					region = _pieces[num4].region;
				}
				num4++;
			}
			BuildSpansForInterval(o, d, piece2.t0, num3, piece2.n0, piece2.region, region, wmm, hmm, dmm);
			num2 = num4;
		}
		if (_spans.Count == 0)
		{
			return result;
		}
		float num5 = areaM2 / massKg;
		StringBuilder stringBuilder = new StringBuilder(160);
		float num6 = 0f;
		float num7 = 0f;
		float num8 = speedMps;
		float num9 = 0f;
		CrewSpan crewSpan = _spans[_spans.Count - 1];
		float num10 = crewSpan.exitMm;
		bool stopped = false;
		for (int k = 0; k < _spans.Count && !stopped; k++)
		{
			CrewSpan crewSpan2 = _spans[k];
			float num11 = (crewSpan2.exitMm - crewSpan2.enterMm) * 0.001f;
			if (num11 <= 0f)
			{
				continue;
			}
			float num12 = CrewTissueModel.DensityKgM3(crewSpan2.tissue) * num5;
			float num13 = CrewTissueModel.StrengthPa(crewSpan2.tissue) * 2f * num5;
			float num14 = num8 * num8;
			float num15;
			if (num13 <= Eps || num12 <= Eps)
			{
				num15 = float.PositiveInfinity;
			}
			else
			{
				num15 = Mathf.Sqrt(num14 * num12 / num13 + 1f) / num12;
			}
			float num16 = num11;
			if (num11 >= num15)
			{
				num16 = num15;
				stopped = true;
			}
			float num17;
			if (num12 > Eps)
			{
				num17 = Mathf.Exp((0f - num12) * num16) * (num13 / num12 + num14) - num13 / num12;
			}
			else
			{
				num17 = num14 - 2f * num13 * num16;
			}
			if (num17 < 0f)
			{
				num17 = 0f;
			}
			num8 = Mathf.Sqrt(num17);
			float num18 = (num14 - num17) * massKg * 0.5f;
			if (num18 < 0f)
			{
				num18 = 0f;
			}
			num6 += num18;
			num9 += CrewModel.DamageFromOrgan(num18, crewSpan2.organ, crewSpan2.tissue);
			num7 += num16 * 1000f;
			_organJ[(int)crewSpan2.organ] += num18;
			if (num18 > 0f)
			{
				_organTouched[(int)crewSpan2.organ] = true;
			}
			float num19 = num16 * 1000f;
			if (num19 >= 0.5f)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(" → ");
				}
				string text = ((crewSpan2.organ != 0) ? OrganName(crewSpan2.organ) : CrewTissueModel.Name(crewSpan2.tissue));
				stringBuilder.Append(text).Append(' ').Append(num19.ToString("F0")).Append(" мм");
			}
			if (stopped)
			{
				num10 = crewSpan2.enterMm + num19;
				result.stopOrgan = crewSpan2.organ;
			}
		}
		result.stopped = stopped;
		result.exitSpeedMps = (stopped ? 0f : num8);
		result.depositedJ = num6;
		result.damageHp = num9;
		result.pathMm = num7;
		result.trail = stringBuilder.ToString();
		result.endLocal = new Vector3((0f - (d.x * num10 + o.x)) / wmm, (d.y * num10 + o.y) / hmm, (0f - (d.z * num10 + o.z)) / dmm);
		return result;
	}

	private static int ComparePieces(Piece a, Piece b)
	{
		if (a.t0 < b.t0)
		{
			return -1;
		}
		return (a.t0 > b.t0) ? 1 : 0;
	}

	private static void BuildSpansForInterval(Vector3 o, Vector3 d, float start, float end, Vector3 nIn, CrewRegion regIn, CrewRegion regOut, float wmm, float hmm, float dmm)
	{
		if (end - start <= Eps)
		{
			return;
		}
		float num = Mathf.Max(Mathf.Abs(Vector3.Dot(nIn, d)), 0.05f);
		int num2 = ShellLayers(regIn, d.y * start + o.y, _layers);
		for (int i = 0; i < num2; i++)
		{
			if (start >= end)
			{
				break;
			}
			float num3 = Mathf.Min(start + _layers[i].thicknessMm / num, end);
			AddSpan(start, num3, _layers[i].tissue, CrewOrgan.None);
			start = num3;
		}
		int num4 = ShellLayers(regOut, d.y * end + o.y, _layers);
		float num5 = end;
		for (int j = 0; j < num4; j++)
		{
			if (num5 <= start)
			{
				break;
			}
			num5 -= _layers[j].thicknessMm / num;
			num5 = Mathf.Max(num5, start);
		}
		FillInterior(o, d, start, num5, wmm, hmm, dmm, InteriorTissue(regIn));
		for (int num6 = num4 - 1; num6 >= 0; num6--)
		{
			if (num5 >= end)
			{
				break;
			}
			float num7 = Mathf.Min(num5 + _layers[num6].thicknessMm / num, end);
			AddSpan(num5, num7, _layers[num6].tissue, CrewOrgan.None);
			num5 = num7;
		}
		if (end - Eps > num5)
		{
			AddSpan(num5, end, CrewTissue.Skin, CrewOrgan.None);
		}
	}

	private static CrewTissue InteriorTissue(CrewRegion r)
	{
		if (r == CrewRegion.Head)
		{
			return CrewTissue.Brain;
		}
		return CrewTissue.Muscle;
	}

	private static void FillInterior(Vector3 o, Vector3 d, float from, float to, float wmm, float hmm, float dmm, CrewTissue filler)
	{
		if (to - from <= Eps)
		{
			return;
		}
		float num = to - Eps;
		float num2 = from;
		for (int i = 0; i < 32; i++)
		{
			if (num <= num2)
			{
				break;
			}
			float num3 = float.PositiveInfinity;
			float num4 = to;
			int num5 = -1;
			for (int j = 0; j < Organs.Length; j++)
			{
				CrewOrganBox crewOrganBox = NormalizedOrgan(j);
				Vector3 h = new Vector3(crewOrganBox.size.x * 0.5f * wmm, crewOrganBox.size.y * 0.5f * hmm, crewOrganBox.size.z * 0.5f * dmm);
				Vector3 c = new Vector3(crewOrganBox.pos.x * wmm, crewOrganBox.pos.y * hmm, crewOrganBox.pos.z * dmm);
				bool hit = HitBox(o, d, c, h, out float t0, out float t1, out Vector3 n0);
				if (!hit || t1 <= num2 + Eps || num <= t0)
				{
					continue;
				}
				float num6 = Mathf.Max(t0, num2);
				if (num6 < num3)
				{
					num3 = num6;
					num5 = j;
					num4 = ((to <= t1) ? to : t1);
				}
			}
			if (num5 < 0)
			{
				AddSpan(num2, to, filler, CrewOrgan.None);
				return;
			}
			if (num2 + Eps < num3)
			{
				AddSpan(num2, num3, filler, CrewOrgan.None);
			}
			AddSpan(num3, num4, Organs[num5].tissue, Organs[num5].organ);
			num2 = num4;
		}
		if (num2 < to - Eps)
		{
			AddSpan(num2, to, filler, CrewOrgan.None);
		}
	}

	private static void AddSpan(float a, float b, CrewTissue t, CrewOrgan o)
	{
		if (b - a <= Eps)
		{
			return;
		}
		_spans.Add(new CrewSpan
		{
			enterMm = a,
			exitMm = b,
			tissue = t,
			organ = o
		});
	}
}
