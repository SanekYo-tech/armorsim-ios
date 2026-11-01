using Unity.Mathematics;

public static class RadarRays
{
	public static RadarHit Closest(RadarScene scene, double3 origin, double3 direction, int excludedFace = -1, RadarCounters counters = null)
	{
		return scene.Bvh.Closest(origin, direction, excludedFace, double.PositiveInfinity, counters);
	}

	public static bool Occluded(RadarScene scene, double3 origin, double3 direction, int excludedFace = -1, RadarCounters counters = null)
	{
		return scene.Bvh.AnyHit(origin, direction, excludedFace, double.PositiveInfinity, counters);
	}
}
