using System;
using System.Text;

public struct SuperShellProfile
{
	public const string KeyOrder = "super.fail.profile_order";

	public const string KeyRadius = "super.fail.profile_radius";

	public const string KeyWider = "super.fail.profile_wider";

	public const string KeyMaterial = "super.fail.profile_material";

	public const string KeyRegions = "super.fail.profile_regions";

	public const int ProbeSamples = 4096;

	public const float NodeRadiusTolM = 1E-06f;

	public bool present;

	public float diameterM;

	public SuperProfileNode[] outer;

	public SuperProfileCavity cavity;

	public SuperProfileRegion[] regions;

	public string provenance;

	public float LengthM => outer[outer.Length - 1].aM;

	public int RegionCount => (regions == null) ? 0 : regions.Length;

	public int ForemostRegion
	{
		get
		{
			if (regions == null || regions.Length == 0)
			{
				return 0;
			}
			int best = 0;
			for (int i = 1; i < regions.Length; i++)
			{
				if (regions[best].aFromM > regions[i].aFromM || (regions[best].aFromM == regions[i].aFromM && regions[best].aToM > regions[i].aToM))
				{
					best = i;
				}
			}
			return best;
		}
	}

	public float OuterRadiusAt(float a)
	{
		int index = outer.Length - 2;
		for (int i = 0; i < outer.Length - 1; i++)
		{
			if (outer[i + 1].aM > a)
			{
				index = i;
				break;
			}
		}
		SuperProfileNode node = outer[index];
		if (node.law == SuperProfileLaw.ProjectNose)
		{
			return RodGeometry.NoseRadiusFactorAt(node.noseShape, a, node.noseLenM, node.noseRefRM) * node.noseRefRM;
		}
		SuperProfileNode next = outer[index + 1];
		float t = (a - node.aM) / (next.aM - node.aM);
		return node.rM + (next.rM - node.rM) * t;
	}

	public int OwnerCount(float a, float rho2)
	{
		int count = 0;
		for (int i = 0; i < regions.Length; i++)
		{
			SuperProfileRegion r = regions[i];
			if (r.aFromM <= a && a < r.aToM && (r.rInnerM <= 0f || rho2 > r.rInnerM * r.rInnerM) && (r.rOuterM == float.PositiveInfinity || rho2 < r.rOuterM * r.rOuterM))
			{
				count++;
			}
		}
		return count;
	}

	public double OuterVolumeM3(int samples = 4096)
	{
		double sum = 0.0;
		double da = LengthM / (double)samples;
		for (int i = 0; i < samples; i++)
		{
			float radius = OuterRadiusAt((float)((i + 0.5) * da));
			sum += (double)radius * Math.PI * radius * da;
		}
		return sum;
	}

	public static SuperShellProfile ForRod(in SuperRodSpec rod)
	{
		float radius = rod.diameterM * 0.5f;
		float noseLength = RodGeometry.NoseLengthM(rod.diameterM, rod.lengthM);
		return new SuperShellProfile
		{
			present = true,
			diameterM = rod.diameterM,
			outer = new SuperProfileNode[2]
			{
				new SuperProfileNode
				{
					aM = 0f,
					rM = RodGeometry.NoseRadiusFactorAt(rod.nose, 0f, noseLength, radius) * radius,
					law = SuperProfileLaw.ProjectNose,
					noseShape = rod.nose,
					noseLenM = noseLength,
					noseRefRM = radius
				},
				new SuperProfileNode
				{
					aM = rod.lengthM,
					rM = radius,
					law = SuperProfileLaw.Line
				}
			},
			cavity = ((rod.cavityFraction > 0f) ? SuperProfileCavity.VolumeFraction : SuperProfileCavity.None),
			regions = new SuperProfileRegion[1]
			{
				new SuperProfileRegion
				{
					materialId = rod.material,
					name = "стержень",
					latSub = 8,
					aFromM = 0f,
					aToM = float.PositiveInfinity,
					rInnerM = 0f,
					rOuterM = float.PositiveInfinity
				}
			},
			provenance = "выражение прежнего стержня: закон носа проекта (RodGeometry), полость — доля по объёму строки каталога; своих чисел профиль не вводит"
		};
	}

	public static SuperShellProfile ForShell(in SuperShellSpec s)
	{
		float radius = s.diameterM * 0.5f;
		float noseLength = RodGeometry.NoseLengthM(s.diameterM, s.lengthM);
		float coreRadius = radius * Math.Min(Math.Max(s.coreDiameterFrac, 0f), 1f);
		float capLength = s.capLengthFrac * s.lengthM;
		bool hasCap = s.capLengthFrac > 0.0001f && !string.IsNullOrEmpty(s.capMaterial);
		bool hasJacket = s.coreDiameterFrac < 0.999f && !string.IsNullOrEmpty(s.jacketMaterial);
		System.Collections.Generic.List<SuperProfileRegion> regions = new System.Collections.Generic.List<SuperProfileRegion>(3);
		regions.Add(new SuperProfileRegion
		{
			materialId = s.coreMaterial,
			name = "сердечник",
			latSub = 0,
			aFromM = (hasCap ? capLength : 0f),
			aToM = float.PositiveInfinity,
			rInnerM = 0f,
			rOuterM = (hasJacket ? coreRadius : float.PositiveInfinity)
		});
		if (hasJacket)
		{
			regions.Add(new SuperProfileRegion
			{
				materialId = s.jacketMaterial,
				name = "рубашка",
				latSub = 1,
				aFromM = (hasCap ? capLength : 0f),
				aToM = float.PositiveInfinity,
				rInnerM = coreRadius,
				rOuterM = float.PositiveInfinity
			});
		}
		if (hasCap)
		{
			regions.Add(new SuperProfileRegion
			{
				materialId = s.capMaterial,
				name = "колпак",
				latSub = 2,
				aFromM = 0f,
				aToM = capLength,
				rInnerM = 0f,
				rOuterM = float.PositiveInfinity
			});
		}
		return new SuperShellProfile
		{
			present = true,
			diameterM = s.diameterM,
			outer = new SuperProfileNode[2]
			{
				new SuperProfileNode
				{
					aM = 0f,
					rM = RodGeometry.NoseRadiusFactorAt(s.nose, 0f, noseLength, radius) * radius,
					law = SuperProfileLaw.ProjectNose,
					noseShape = s.nose,
					noseLenM = noseLength,
					noseRefRM = radius
				},
				new SuperProfileNode
				{
					aM = s.lengthM,
					rM = radius,
					law = SuperProfileLaw.Line
				}
			},
			cavity = SuperProfileCavity.None,
			regions = regions.ToArray(),
			provenance = "выражение прежнего составного снаряда: закон носа проекта, доли сердечника и колпака — числа спецификации; своих чисел профиль не вводит"
		};
	}

	public bool Validate(float spacingM, out string failKey, out object[] failArgs, out string prose)
	{
		failKey = null;
		failArgs = null;
		prose = null;
		if (!present)
		{
			return true;
		}
		if (spacingM <= 0f)
		{
			return Refuse("super.fail.profile_order", (outer == null) ? 0 : outer.Length, string.Format("шаг частиц {0:F3} мм не положителен: по нему считаются и ", spacingM * 1000f) + "число слоёв, и обход покрытия областей — проверить профиль нечем", out failKey, out failArgs, out prose);
		}
		if (outer == null || outer.Length < 2)
		{
			return Refuse("super.fail.profile_order", (outer == null) ? 0 : outer.Length, "одной точкой тело вращения не задано: нужно не меньше двух станций", out failKey, out failArgs, out prose);
		}
		if (outer[0].aM != 0f)
		{
			return Refuse("super.fail.profile_order", outer.Length, string.Format("первая станция стоит на {0:F3} мм от кончика, а обязана ", outer[0].aM * 1000f) + "стоять на нуле: кончик — начало отсчёта профиля", out failKey, out failArgs, out prose);
		}
		for (int i = 1; i < outer.Length; i++)
		{
			if (outer[i].aM <= outer[i - 1].aM)
			{
				string reason = string.Format("станции {0} и {1} стоят на {2:F3} и ", i - 1, i, outer[i - 1].aM * 1000f) + string.Format("{0:F3} мм — расстояние от кончика обязано строго ", outer[i].aM * 1000f) + "возрастать, иначе у звена нет длины и закон радиуса не определён";
				return Refuse("super.fail.profile_order", outer.Length, reason, out failKey, out failArgs, out prose);
			}
		}
		if (!IsFinite(LengthM) || LengthM <= 0f || !IsFinite(diameterM) || diameterM <= 0f)
		{
			return Refuse("super.fail.profile_order", outer.Length, string.Format("габарит профиля {0:F2} × {1:F2} мм не годится ", LengthM * 1000f, diameterM * 1000f) + "в растр: число слоёв и число узлов поперёк выводятся из него", out failKey, out failArgs, out prose);
		}
		for (int i = 0; i < outer.Length; i++)
		{
			float declared = outer[i].rM;
			float actual = OuterRadiusAt(outer[i].aM);
			if (!IsFinite(declared) || Math.Abs(declared - actual) > 1E-06f)
			{
				string reason = string.Format("станция {0} на {1:F3} мм объявляет радиус ", i, outer[i].aM * 1000f) + string.Format("{0:F3} мм, а закон звена даёт там {1:F3} мм ", declared * 1000f, actual * 1000f) + string.Format("(допуск {0:F0} мкм): радиус станции обязан ", 1f) + "согласовываться с законом, иначе он данные без читателя";
				return Refuse("super.fail.profile_order", outer.Length, reason, out failKey, out failArgs, out prose);
			}
		}
		float maxRadius = 0f;
		for (int i = 0; i < 4096; i++)
		{
			float a = (float)((double)LengthM * 0.000244140625 * (i + 0.5));
			float radius = OuterRadiusAt(a);
			if (!IsFinite(radius) || radius <= 0f)
			{
				failKey = "super.fail.profile_radius";
				failArgs = new object[2] { (double)(a * 1000f), (double)(radius * 1000f) };
				prose = string.Format("Профиль тела не выражается: на станции {0:F3} мм радиус вышел ", (double)(a * 1000f)) + string.Format("{0:F3} мм — поверхность тела вращения проходит через ось и сама ", (double)(radius * 1000f)) + "себя пересекает.";
				return false;
			}
			if (radius > maxRadius)
			{
				maxRadius = radius;
			}
		}
		if (diameterM * 0.5f * 1.000001f < maxRadius)
		{
			failKey = "super.fail.profile_wider";
			failArgs = new object[2] { (double)(maxRadius * 2000f), (double)(diameterM * 1000f) };
			prose = string.Format("Профиль тела не выражается: наибольший поперечник профиля {0:F3} мм ", (double)(maxRadius * 2000f)) + string.Format("против объявленных {0:F3} мм — решётка обрезала бы тело молча, ", (double)(diameterM * 1000f)) + "потому что число узлов поперёк берётся из объявленного поперечника.";
			return false;
		}
		int regionCount = RegionCount;
		if (regionCount <= 0)
		{
			return Refuse("super.fail.profile_regions", regionCount, "областей веществ ноль: каждой частице нужно вещество, и брать его не откуда", out failKey, out failArgs, out prose);
		}
		for (int i = 0; i < regionCount; i++)
		{
			if (!SuperBaker.TryMaterial(regions[i].materialId, out _))
			{
				failKey = "super.fail.profile_material";
				failArgs = new object[2] { regions[i].materialId ?? "", regions[i].name ?? "" };
				prose = "Профиль тела не выражается: область «" + (regions[i].name ?? "") + "» называет веществом «" + (regions[i].materialId ?? "") + "», а строки с таким именем запекание не знает. Реестр несёт провенанс, и подставлять похожее вместо неизвестного запрещено.";
				return false;
			}
			if (string.IsNullOrEmpty(regions[i].name))
			{
				return Refuse("super.fail.profile_regions", regionCount, string.Format("у области {0} пустое имя: по именам тел судят исход, шапка ленты и ", i) + "просмотрщик", out failKey, out failArgs, out prose);
			}
			if (regions[i].latSub < 0 || regions[i].latSub > 15)
			{
				string reason = string.Format("{0} вне 0..15: ключ решётки наложился бы на соседний", regions[i].latSub) + "у области «" + regions[i].name + "» номер подъячейки ключа " + regions[i].latSub;
				return Refuse("super.fail.profile_regions", regionCount, reason, out failKey, out failArgs, out prose);
			}
		}
		int axialSamples = Math.Max(1, UnityEngine.Mathf.CeilToInt(LengthM / spacingM));
		int radialSamples = Math.Max(1, UnityEngine.Mathf.RoundToInt(diameterM / spacingM));
		for (int axial = 0; axial < axialSamples; axial++)
		{
			float a = (axial + 0.5f) * spacingM;
			for (int y = 0; y < radialSamples; y++)
			{
				for (int x = 0; x < radialSamples; x++)
				{
					float centre = (radialSamples - 1) * 0.5f;
					float dy = (y - centre) * spacingM;
					float dx = (x - centre) * spacingM;
					float rho2 = dx * dx + dy * dy;
					int owners = OwnerCount(a, rho2);
					if (owners != 1)
					{
						string reason = string.Format("узел решётки (слой {0}, ряд {1}, столбец {2}; станция ", axial, y, x) + string.Format("{0:F3} мм, радиус {1:F3} мм при ", a * 1000f, Math.Sqrt(rho2) * 1000f) + string.Format("шаге {0:F3} мм) принадлежит {1} областям ", spacingM * 1000f, owners) + "вместо одной: коробки областей обязаны не пересекаться и покрывать тело";
						return Refuse("super.fail.profile_regions", regionCount, reason, out failKey, out failArgs, out prose);
					}
				}
			}
		}
		return true;
	}

	private static bool Refuse(string key, int count, string reason, out string failKey, out object[] failArgs, out string prose)
	{
		failKey = key;
		failArgs = new object[2] { count, reason };
		prose = "Профиль тела не выражается: " + reason + ".";
		return false;
	}

	private static bool IsFinite(float x)
	{
		return !float.IsNaN(x) && !float.IsInfinity(x);
	}

	public string Describe()
	{
		if (!present)
		{
			return "профиль: нет (прежние скаляры)";
		}
		StringBuilder sb = new StringBuilder();
		sb.Append(string.Format("профиль: {0} станций, {1} областей, габарит ", outer.Length, regions.Length) + string.Format("{0:F1} × {1:F1} мм; звенья ", LengthM * 1000f, diameterM * 1000f));
		for (int i = 0; i < outer.Length - 1; i++)
		{
			if (i > 0)
			{
				sb.Append(" | ");
			}
			sb.Append(string.Format("{0:F1}…{1:F1} мм {2}", outer[i].aM * 1000f, outer[i + 1].aM * 1000f, outer[i].law));
			if (outer[i].law == SuperProfileLaw.ProjectNose)
			{
				sb.Append(string.Format("({0}, довод носа {1:F1} мм", outer[i].noseShape, outer[i].noseLenM * 1000f));
				string actual = ((outer[i].noseShape == NoseShape.BluntOgive) ? string.Format(" ⇒ настоящий {0:F1} мм", outer[i].noseLenM * 0.6f * 1000f) : "");
				sb.Append(actual + ", " + string.Format("опора {0:F2} мм)", outer[i].noseRefRM * 1000f));
			}
		}
		sb.Append("; области ");
		for (int i = 0; i < regions.Length; i++)
		{
			if (i > 0)
			{
				sb.Append(" | ");
			}
			sb.Append(regions[i].name + "=" + regions[i].materialId);
		}
		sb.Append(string.Format("; полость {0}", cavity));
		if (!string.IsNullOrEmpty(provenance))
		{
			sb.Append("; ПРОВЕНАНС: " + provenance);
		}
		return sb.ToString();
	}
}
