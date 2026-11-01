using UnityEngine;

public static class SceneScale
{
	public const float WorldPerMeter = 1f;

	public const float WorldPerMM = 0.001f;

	public const float MMPerWorld = 999.99994f;

	public const float MetersPerWorld = 1f;

	public const float MinRenderWorld = 0.0001f;

	public static float FromMM(float mm)
	{
		return mm * WorldPerMM;
	}

	public static float FromM(float m)
	{
		return m;
	}

	public static float ToMM(float worldUnits)
	{
		return worldUnits * MMPerWorld;
	}

	public static float ToM(float worldUnits)
	{
		return worldUnits;
	}

	public static Vector3 PlateLocalScale(float widthM, float heightM, float thicknessMM)
	{
		return new Vector3(Mathf.Max(MinRenderWorld, widthM), Mathf.Max(MinRenderWorld, heightM), Mathf.Max(MinRenderWorld, thicknessMM * WorldPerMM));
	}

	public static float RodLengthWorld(float rodLengthMM)
	{
		return rodLengthMM * WorldPerMM;
	}

	public static float RodDiameterWorld(float diameterMM)
	{
		return diameterMM * WorldPerMM;
	}

	public static string Describe()
	{
		return string.Format("Масштаб сцены: 1 мировая единица = {0:F3} м ({1:F0} мм). ", MetersPerWorld, MMPerWorld) + "Габарит нарисованного объекта равен физическому (инвариант шага 26).";
	}
}
