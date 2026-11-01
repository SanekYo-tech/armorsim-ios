using UnityEngine;

public static class SuperOutcome
{
	public const float ThroughFraction = 0.05f;

	public const float DepartureFloorMps = 25f;

	public const float EmbeddedKeFraction = 0.02f;

	public const float ContactMarginCalibres = 1f;

	public static SuperOutcomeResult Classify(SuperParticles p, SuperBaker.Body rod, SuperBaker.Body plate, float rodDiameterM, double rodKe0, Vector3[] plateInitialPos, float[] damage = null)
	{
		Vector3[] x = new Vector3[p.count];
		Vector3[] vel = new Vector3[p.count];
		float[] massArr = new float[p.count];
		float[] V0 = new float[p.count];
		for (int i = 0; i < p.count; i++)
		{
			x[i] = p.x[i];
			vel[i] = p.v[i];
			massArr[i] = p.mass[i];
			V0[i] = p.V0[i];
		}
		return Classify(x, vel, massArr, V0, rod, plate, rodDiameterM, rodKe0, plateInitialPos, damage);
	}

	public static SuperOutcomeResult Classify(Vector3[] x, Vector3[] vel, float[] massArr, float[] V0, SuperBaker.Body rod, SuperBaker.Body plate, float rodDiameterM, double rodKe0, Vector3[] plateInitialPos, float[] damage = null)
	{
		SuperOutcomeResult result = default(SuperOutcomeResult);
		RadarKahan rodMass = new RadarKahan();
		RadarKahan rodMvx = new RadarKahan();
		RadarKahan rodMvy = new RadarKahan();
		RadarKahan rodMvz = new RadarKahan();
		RadarKahan rodKe = new RadarKahan();
		float minZ = float.MaxValue;
		for (int i = rod.start; i < rod.start + rod.count; i++)
		{
			float m = massArr[i];
			rodMass.Add(m);
			rodMvx.Add((double)vel[i].x * m);
			rodMvy.Add((double)vel[i].y * m);
			rodMvz.Add((double)vel[i].z * m);
			rodKe.Add(0.5 * m * ((double)vel[i].x * vel[i].x + (double)vel[i].y * vel[i].y + (double)vel[i].z * vel[i].z));
			if (minZ > x[i].z)
			{
				minZ = x[i].z;
			}
		}
		double rodDenom = ((rodMass.Value > 1e-30) ? rodMass.Value : 1.0);
		result.rodMassFractionRemaining = 1f;
		result.rodMeanVelocity = new Vector3((float)(rodMvx.Value / rodDenom), (float)(rodMvy.Value / rodDenom), (float)(rodMvz.Value / rodDenom));
		result.rodKineticJ = rodKe.Value;
		RadarKahan plateMass = new RadarKahan();
		RadarKahan plateMvx = new RadarKahan();
		RadarKahan plateMvy = new RadarKahan();
		RadarKahan plateMvz = new RadarKahan();
		for (int j = plate.start; j < plate.start + plate.count; j++)
		{
			float m2 = massArr[j];
			plateMass.Add(m2);
			plateMvx.Add((double)vel[j].x * m2);
			plateMvy.Add((double)vel[j].y * m2);
			plateMvz.Add((double)vel[j].z * m2);
		}
		double plateDenom = ((plateMass.Value > 1e-30) ? plateMass.Value : 1.0);
		result.plateMeanVelocity = new Vector3((float)(plateMvx.Value / plateDenom), (float)(plateMvy.Value / plateDenom), (float)(plateMvz.Value / plateDenom));
		result.departureSpeed = Vector3.Dot(result.rodMeanVelocity - result.plateMeanVelocity, plate.normal);
		float minDist = float.MaxValue;
		for (int k = rod.start; k < rod.start + rod.count; k++)
		{
			minDist = Mathf.Min(minDist, DistanceToSlab(x[k], plate));
		}
		bool contacted = rodDiameterM >= minDist;
		RadarKahan inFoot = new RadarKahan();
		RadarKahan outFoot = new RadarKahan();
		RadarKahan cvx = new RadarKahan();
		RadarKahan cvy = new RadarKahan();
		RadarKahan cvz = new RadarKahan();
		for (int l = rod.start; l < rod.start + rod.count; l++)
		{
			if (Vector3.Dot(x[l] - plate.faceCentre, plate.normal) > -plate.thicknessM)
			{
				continue;
			}
			if (Vector3.Dot(vel[l], plate.normal) >= 0f)
			{
				continue;
			}
			float m3 = massArr[l];
			if (WithinFootprint(x[l], plate))
			{
				inFoot.Add(m3);
			}
			else
			{
				outFoot.Add(m3);
			}
			cvx.Add((double)vel[l].x * m3);
			cvy.Add((double)vel[l].y * m3);
			cvz.Add((double)vel[l].z * m3);
		}
		result.throughFraction = (float)(inFoot.Value / rodDenom);
		result.bypassFraction = (float)(outFoot.Value / rodDenom);
		if (inFoot.Value > 1e-30)
		{
			result.residualVelocity = new Vector3((float)(cvx.Value / inFoot.Value), (float)(cvy.Value / inFoot.Value), (float)(cvz.Value / inFoot.Value));
		}
		result.craterReading = SuperRuler.CraterDepth(x, V0, plate, plateInitialPos, rodDiameterM, result.throughFraction, damage, out result.craterLateralOffsetM, out result.craterOffsetSymmetricAxisM);
		result.craterDepthM = result.craterReading.value;
		if (contacted)
		{
			if (result.throughFraction >= ThroughFraction)
			{
				result.verdict = SuperVerdict.Penetrate;
			}
			else if (result.departureSpeed >= DepartureFloorMps)
			{
				result.verdict = SuperVerdict.Ricochet;
			}
			else
			{
				if (rodKe0 > 0.0)
				{
					rodKe0 *= EmbeddedKeFraction;
				}
				result.verdict = SuperVerdict.Embedded;
			}
		}
		else
		{
			result.verdict = SuperVerdict.NoContact;
		}
		return result;
	}

	public static bool WithinFootprint(Vector3 q, SuperBaker.Body plate)
	{
		Vector3 d = q - plate.centre;
		return plate.halfUM >= Mathf.Abs(Vector3.Dot(d, plate.axisU)) && plate.halfVM >= Mathf.Abs(Vector3.Dot(d, plate.axisV));
	}

	public static float DistanceToSlab(Vector3 q, SuperBaker.Body plate)
	{
		Vector3 d = q - plate.centre;
		float u = Mathf.Max(0f, Mathf.Abs(Vector3.Dot(d, plate.axisU)) - plate.halfUM);
		float v = Mathf.Max(0f, Mathf.Abs(Vector3.Dot(d, plate.axisV)) - plate.halfVM);
		float w = Mathf.Max(0f, Mathf.Abs(Vector3.Dot(d, plate.normal)) - plate.thicknessM * 0.5f);
		return Mathf.Sqrt(u * u + v * v + w * w);
	}
}
