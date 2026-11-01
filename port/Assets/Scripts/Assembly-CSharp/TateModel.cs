using UnityEngine;

public static class TateModel
{
	public struct Penetrator
	{
		public string name;

		public float density;

		public float Yp;

		public float confinement;
	}

	public struct Target
	{
		public string name;

		public float density;

		public float Rt;

		public bool RtIsFitted;

		public float Rrigid;
	}

	public static Penetrator WHA()
	{
		return new Penetrator
		{
			name = "Вольфрам. сплав",
			density = 17300f,
			Yp = 2E+09f
		};
	}

	public static Penetrator DU()
	{
		return new Penetrator
		{
			name = "Обедн. уран",
			density = 18600f,
			Yp = 1.1E+09f
		};
	}

	public static Penetrator Steel()
	{
		return new Penetrator
		{
			name = "Сталь бронебойная",
			density = 7850f,
			Yp = 2.5E+09f
		};
	}

	public static Penetrator WC()
	{
		return new Penetrator
		{
			name = "Карбид W",
			density = 15000f,
			Yp = 5E+09f,
			confinement = 1.22f
		};
	}

	public static Target RHA()
	{
		return new Target
		{
			name = "RHA сталь",
			density = 7850f,
			Rt = 5.3E+09f,
			RtIsFitted = true,
			Rrigid = 3.737E+09f
		};
	}

	public static Target HHA()
	{
		return new Target
		{
			name = "Высокотв. HHA",
			density = 7850f,
			Rt = 7E+09f,
			RtIsFitted = true,
			Rrigid = 5.615E+09f
		};
	}

	public static Target Aluminum()
	{
		return new Target
		{
			name = "Алюминий",
			density = 2700f,
			Rt = 1.5E+09f,
			RtIsFitted = true,
			Rrigid = 1.454E+09f
		};
	}

	public static Target Titanium()
	{
		return new Target
		{
			name = "Титан",
			density = 4430f,
			Rt = 4.2E+09f,
			RtIsFitted = true,
			Rrigid = 3.636E+09f
		};
	}

	public static Target Ceramic()
	{
		return new Target
		{
			name = "Керамика SiC",
			density = 3200f,
			Rt = 7E+09f,
			RtIsFitted = true
		};
	}

	public static Target Tungsten()
	{
		return new Target
		{
			name = "Вольфрам",
			density = 17300f,
			Rt = 9E+09f,
			RtIsFitted = true
		};
	}

	public static Target CastSteel()
	{
		return new Target
		{
			name = "Литая сталь",
			density = 7850f,
			Rt = 5.3E+09f,
			RtIsFitted = true,
			Rrigid = 3.737E+09f
		};
	}

	public static Target BoronCarbide()
	{
		return new Target
		{
			name = "Карбид бора B₄C",
			density = 2520f,
			Rt = 6E+09f,
			RtIsFitted = true
		};
	}

	public static Target TitaniumDiboride()
	{
		return new Target
		{
			name = "Диборид титана TiB₂",
			density = 4500f,
			Rt = 1.07E+10f,
			RtIsFitted = true
		};
	}

	public static Target DepletedUraniumArmour()
	{
		return new Target
		{
			name = "Обеднённый уран (броня)",
			density = 18600f,
			Rt = 5.8E+09f,
			RtIsFitted = true
		};
	}

	public static Target Alumina()
	{
		return new Target
		{
			name = "Оксид алюминия Al₂O₃",
			density = 3900f,
			Rt = 5.8E+09f,
			RtIsFitted = true
		};
	}

	public static Target Textolite()
	{
		return new Target
		{
			name = "Текстолит",
			density = 1850f,
			Rt = 1.2E+09f,
			RtIsFitted = true
		};
	}

	public static Target Elastomer(InterlayerMaterial m)
	{
		var phases = NeraArmor.ShockPhases(m);
		return new Target
		{
			name = NeraArmor.InterlayerName(m),
			density = phases.Count > 0 ? phases[0].density : 0f
		};
	}

	public static float PenetrationVelocity(Penetrator p, Target t, float V)
	{
		if (Mathf.Abs(p.density - t.density) < 1f)
		{
			return Mathf.Clamp(V * 0.5f + (p.Yp - t.Rt) / (p.density * V), 0f, V);
		}
		float a = (p.density - t.density) * 0.5f;
		float b = -p.density * V;
		float c = p.density * 0.5f * V * V + p.Yp - t.Rt;
		float discriminant = b * b - 4f * a * c;
		if (discriminant < 0f)
		{
			return Mathf.Clamp(t.density, 0f, V);
		}
		float root = Mathf.Sqrt(discriminant);
		float u1 = (-b + root) / (2f * a);
		float u2 = (-b - root) / (2f * a);
		float u;
		if (u1 > 0f && u1 < V)
		{
			u = u1;
		}
		else if (u2 > 0f && u2 < V)
		{
			u = u2;
		}
		else
		{
			u = Mathf.Max(0f, Mathf.Min(u1, u2));
		}
		return Mathf.Clamp(u, 0f, V);
	}
}
