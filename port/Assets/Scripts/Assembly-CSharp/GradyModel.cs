using System;
using UnityEngine;

public static class GradyModel
{
	public enum Branch
	{
		Ductile = 0,
		Brittle = 1
	}

	public struct FragMaterial
	{
		public string name;

		public float density;

		public float Cl;

		public float Kic;

		public float sigmaSp;

		public Branch branch;

		public bool alSwitch;

		public float gammaDuctile;
	}

	public static float StrainRate = 10000f;

	public static float AlBrittleStrainRate = 400000f;

	public static FragMaterial RHA()
	{
		return new FragMaterial
		{
			name = "RHA сталь",
			density = 7850f,
			Cl = 5860f,
			Kic = 1.15E+08f,
			sigmaSp = 4.5E+09f,
			gammaDuctile = 30000f
		};
	}

	public static FragMaterial HHA()
	{
		return new FragMaterial
		{
			name = "HHA сталь",
			density = 7850f,
			Cl = 5900f,
			Kic = 6E+07f,
			sigmaSp = 4E+09f,
			branch = Branch.Brittle,
			gammaDuctile = 20000f
		};
	}

	public static FragMaterial CastSteel()
	{
		return new FragMaterial
		{
			name = "Литая сталь",
			density = 7850f,
			Cl = 5860f,
			Kic = 8E+07f,
			sigmaSp = 2.9E+09f,
			gammaDuctile = 24000f
		};
	}

	public static FragMaterial Aluminum()
	{
		return new FragMaterial
		{
			name = "Алюминий",
			density = 2700f,
			Cl = 6300f,
			Kic = 3E+07f,
			sigmaSp = 1E+09f,
			alSwitch = true,
			gammaDuctile = 10000f
		};
	}

	public static FragMaterial Titanium()
	{
		return new FragMaterial
		{
			name = "Титан",
			density = 4430f,
			Cl = 6150f,
			Kic = 6.5E+07f,
			sigmaSp = 4.7E+09f,
			gammaDuctile = 16000f
		};
	}

	public static FragMaterial Ceramic()
	{
		return new FragMaterial
		{
			name = "Керамика SiC",
			density = 3200f,
			Cl = 11000f,
			Kic = 4000000f,
			sigmaSp = 1.5E+09f,
			branch = Branch.Brittle,
			gammaDuctile = 5000f
		};
	}

	public static FragMaterial Tungsten()
	{
		return new FragMaterial
		{
			name = "Вольфрам",
			density = 17300f,
			Cl = 5200f,
			Kic = 1E+08f,
			sigmaSp = 4E+09f,
			gammaDuctile = 40000f
		};
	}

	public static FragMaterial ForArmor(ArmorPlate.ArmorType type)
	{
		switch (type)
		{
		case ArmorPlate.ArmorType.HHA:
			return HHA();
		case ArmorPlate.ArmorType.Aluminum:
			return Aluminum();
		case ArmorPlate.ArmorType.Titanium:
			return Titanium();
		case ArmorPlate.ArmorType.Ceramic:
			return Ceramic();
		case ArmorPlate.ArmorType.Tungsten:
			return Tungsten();
		case ArmorPlate.ArmorType.CastSteel:
			return CastSteel();
		default:
			return RHA();
		}
	}

	public static Branch EffectiveBranch(FragMaterial m, float strainRate)
	{
		if (m.alSwitch && strainRate > AlBrittleStrainRate)
		{
			return Branch.Brittle;
		}
		return m.branch;
	}

	public static float CharacteristicSize(FragMaterial m, float strainRate)
	{
		strainRate = Mathf.Max(strainRate, 1f);
		if (EffectiveBranch(m, strainRate) == Branch.Brittle)
		{
			return Mathf.Pow(Mathf.Sqrt(20f) * m.Kic / Mathf.Max(m.Cl * m.density * strainRate, 1E-09f), 0.6666667f);
		}
		return Mathf.Pow(48f * m.gammaDuctile / Mathf.Max(strainRate * m.density * strainRate, 1E-09f), 0.33333334f);
	}

	public static float ChannelVolume(float channelRadiusM, float channelLengthM)
	{
		return Mathf.PI * channelRadiusM * channelRadiusM * Mathf.Max(channelLengthM, 0f);
	}

	public static int FragmentCountForVolume(float sizeM, float channelVolumeM3)
	{
		sizeM = Mathf.Max(sizeM, 1E-05f);
		float volume = Mathf.Max(channelVolumeM3, 0f);
		return Math.Max(0, (int)Math.Floor(volume / Mathf.Max(sizeM * sizeM * sizeM, 1E-15f)));
	}

	public static int FragmentCount(float sizeM, float channelRadiusM, float channelLengthM)
	{
		return FragmentCountForVolume(sizeM, ChannelVolume(channelRadiusM, channelLengthM));
	}
}
