using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;

public static class RadarFacetPO
{
	private struct MonostaticKahan
	{
		public double Sum;

		public double Correction;

		public void Add(double value)
		{
			double y = value - Correction;
			double t = Sum + y;
			Correction = t - Sum - y;
			Sum = t;
		}

		public void AddZeros(int count)
		{
			for (int i = 0; i < count; i++)
			{
				Add(0.0);
			}
		}
	}

	private struct MonostaticKahanVector
	{
		private MonostaticKahan xr;

		private MonostaticKahan xi;

		private MonostaticKahan yr;

		private MonostaticKahan yi;

		private MonostaticKahan zr;

		private MonostaticKahan zi;

		public RadarCVector Value => new RadarCVector(new Complex64(xr.Sum, xi.Sum), new Complex64(yr.Sum, yi.Sum), new Complex64(zr.Sum, zi.Sum));

		public void Add(RadarCVector v)
		{
			xr.Add(v.X.Real);
			xi.Add(v.X.Imaginary);
			yr.Add(v.Y.Real);
			yi.Add(v.Y.Imaginary);
			zr.Add(v.Z.Real);
			zi.Add(v.Z.Imaginary);
		}

		public void AddZeros(int count)
		{
			if (count > 0)
			{
				xr.AddZeros(count);
				xi.AddZeros(count);
				yr.AddZeros(count);
				yi.AddZeros(count);
				zr.AddZeros(count);
				zi.AddZeros(count);
			}
		}
	}

	private const double A1 = 0.05971587178977;

	private const double B1 = 0.470142064105115;

	private const double A2 = 0.797426985353087;

	private const double B2 = 0.101286507323456;

	public static RadarPoPrepared Prepare(RadarScene scene, RadarDirectionBasis incident, double surfaceOffsetMeters, RadarCounters counters, CancellationToken token)
	{
		if (scene == null)
		{
			throw new ArgumentNullException("scene");
		}
		if (counters == null)
		{
			throw new ArgumentNullException("counters");
		}
		List<RadarPoTriangle> output = new List<RadarPoTriangle>(scene.TriangleCount);
		bool[] lit = new bool[scene.TriangleCount];
		for (int i = 0; i < scene.TriangleCount; i++)
		{
			if ((i & 0x7F) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			double3 n = scene.Normals[i];
			double dot = incident.ToRadar.x * n.x + incident.ToRadar.y * n.y + incident.ToRadar.z * n.z;
			if (Math.Abs(dot) < 1e-09)
			{
				counters.EdgeOnFacets++;
				continue;
			}
			double3 normal = ((dot > 0.0) ? n : (-n));
			scene.GetTriangle(i, out var a, out var b, out var c);
			AddVisible(scene, i, a, b, c, normal, incident.ToRadar, surfaceOffsetMeters, 0, output, lit, counters, token);
		}
		return new RadarPoPrepared(output.ToArray(), lit);
	}

	private static void AddVisible(RadarScene scene, int face, double3 a, double3 b, double3 c, double3 normal, double3 toRadar, double epsilon, int depth, List<RadarPoTriangle> output, bool[] lit, RadarCounters counters, CancellationToken token)
	{
		token.ThrowIfCancellationRequested();
		double3 centroid = (a + b + c) / 3.0;
		if (depth == 3)
		{
			counters.ShadowRays++;
			double3 origin = centroid + normal * epsilon;
			if (scene.Bvh.AnyHit(origin, toRadar, face, 0.0, counters))
			{
				return;
			}
			Append(face, a, b, c, normal, output, lit, counters);
			return;
		}
		counters.ShadowRays++;
		double3 pa = a * 0.99 + centroid * 0.01 + normal * epsilon;
		bool visA = !scene.Bvh.AnyHit(pa, toRadar, face, 0.0, counters);
		counters.ShadowRays++;
		double3 pb = b * 0.99 + centroid * 0.01 + normal * epsilon;
		bool visB = !scene.Bvh.AnyHit(pb, toRadar, face, 0.0, counters);
		counters.ShadowRays++;
		double3 pc = c * 0.99 + centroid * 0.01 + normal * epsilon;
		bool visC = !scene.Bvh.AnyHit(pc, toRadar, face, 0.0, counters);
		counters.ShadowRays++;
		double3 pm = centroid + normal * epsilon;
		bool visM = !scene.Bvh.AnyHit(pm, toRadar, face, 0.0, counters);
		int visibleCount = (visA ? 1 : 0) + (visB ? 1 : 0) + (visC ? 1 : 0) + (visM ? 1 : 0);
		if (visibleCount == 4)
		{
			Append(face, a, b, c, normal, output, lit, counters);
			return;
		}
		if (visibleCount == 0)
		{
			return;
		}
		double3 ab = (a + b) * 0.5;
		double3 bc = (b + c) * 0.5;
		double3 ac = (a + c) * 0.5;
		AddVisible(scene, face, a, ab, ac, normal, toRadar, epsilon, depth + 1, output, lit, counters, token);
		AddVisible(scene, face, ab, b, bc, normal, toRadar, epsilon, depth + 1, output, lit, counters, token);
		AddVisible(scene, face, ac, bc, c, normal, toRadar, epsilon, depth + 1, output, lit, counters, token);
		AddVisible(scene, face, ab, bc, ac, normal, toRadar, epsilon, depth + 1, output, lit, counters, token);
	}

	private static void Append(int face, double3 a, double3 b, double3 c, double3 normal, List<RadarPoTriangle> output, bool[] lit, RadarCounters counters)
	{
		output.Add(new RadarPoTriangle(face, a, b, c, normal));
		lit[face] = true;
		counters.VisibleFacetSubtriangles++;
	}

	private static bool Visible(RadarScene scene, int face, double3 point, double3 normal, double3 toRadar, double epsilon, RadarCounters counters)
	{
		counters.ShadowRays++;
		double3 origin = point + normal * epsilon;
		return !scene.Bvh.AnyHit(origin, toRadar, face, 0.0, counters);
	}

	public static RadarCVector[,] Evaluate(RadarScene scene, RadarPoPrepared prepared, RadarDirectionBasis incident, RadarDirectionBasis receiver, double wavenumber, CancellationToken token)
	{
		if (scene == null || prepared == null)
		{
			throw new ArgumentNullException();
		}
		double3 w = (receiver.ToRadar - incident.Incident) * wavenumber;
		int faceCount = scene.TriangleCount;
		RadarKahanVector[,] kahan = new RadarKahanVector[faceCount, 2];
		for (int i = 0; i < prepared.VisibleTriangles.Length; i++)
		{
			if ((i & 0x7F) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			RadarPoTriangle tri = prepared.VisibleTriangles[i];
			Complex64 integral = TriangleIntegral(tri.A, tri.B, tri.C, w);
			double3 v0 = math.cross(incident.Incident, incident.Horizontal);
			double3 v1 = math.cross(incident.Incident, incident.Vertical);
			double3 t0 = math.cross(tri.EffectiveNormal, v0) * 2.0;
			double3 t1 = math.cross(tri.EffectiveNormal, v1) * 2.0;
			kahan[tri.Face, 0].Add(RadarCVector.FromReal(t0) * integral);
			kahan[tri.Face, 1].Add(RadarCVector.FromReal(t1) * integral);
		}
		RadarCVector[,] result = new RadarCVector[faceCount, 2];
		for (int face = 0; face < scene.TriangleCount; face++)
		{
			result[face, 0] = kahan[face, 0].Value;
			result[face, 1] = kahan[face, 1].Value;
		}
		return result;
	}

	public static Complex64 EvaluateSelected(RadarScene scene, RadarPoPrepared prepared, RadarDirectionBasis incident, RadarDirectionBasis receiver, double wavenumber, int receivePolarization, int transmitPolarization, double surfaceOffsetMeters, CancellationToken token)
	{
		if (scene == null || prepared == null)
		{
			throw new ArgumentNullException();
		}
		if (receivePolarization < 0 || receivePolarization > 1 || transmitPolarization < 0 || transmitPolarization > 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		double3 w = (receiver.ToRadar - incident.Incident) * wavenumber;
		double3 transmitBasis = ((transmitPolarization == 0) ? incident.Horizontal : incident.Vertical);
		double3 receiveBasis = ((receivePolarization == 0) ? receiver.Horizontal : receiver.Vertical);
		RadarKahanComplex sum = default(RadarKahanComplex);
		for (int i = 0; i < prepared.VisibleTriangles.Length; i++)
		{
			if ((i & 0xFF) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			RadarPoTriangle tri = prepared.VisibleTriangles[i];
			if (0.0 < math.dot(tri.EffectiveNormal, receiver.ToRadar))
			{
				double3 shadowOrigin = (tri.A + tri.B + tri.C) / 3.0 + tri.EffectiveNormal * surfaceOffsetMeters;
				if (!scene.Bvh.AnyHit(shadowOrigin, receiver.ToRadar, tri.Face, double.PositiveInfinity, null))
				{
					Complex64 integral = TriangleIntegral(tri.A, tri.B, tri.C, w);
					double3 v = math.cross(incident.Incident, transmitBasis);
					double3 current = math.cross(tri.EffectiveNormal, v) * 2.0;
					RadarCVector electric = RadarCVector.FromReal(current) * integral;
					RadarCVector scattered = RadarFarField.Scatter(electric, RadarCVector.Zero, receiver.ToRadar, wavenumber);
					sum.Add(scattered.Dot(receiveBasis));
				}
			}
		}
		return sum.Value;
	}

	public static Complex64 TriangleIntegral(double3 a, double3 b, double3 c, double3 w)
	{
		double3 e0 = b - a;
		double3 e1 = c - b;
		double3 e2 = a - c;
		double3 n = math.cross(e0, c - a);
		double twoArea = Math.Sqrt(n.x * n.x + n.y * n.y + n.z * n.z);
		if (!(twoArea > 0.0))
		{
			return Complex64.Zero;
		}
		double3 unit = n / twoArea;
		double wDotN = math.dot(w, unit);
		double3 wPerp = w - unit * wDotN;
		double wPerp2 = wPerp.x * wPerp.x + wPerp.y * wPerp.y + wPerp.z * wPerp.z;
		double l0 = Math.Sqrt(e0.x * e0.x + e0.y * e0.y + e0.z * e0.z);
		double l1 = Math.Sqrt(e1.x * e1.x + e1.y * e1.y + e1.z * e1.z);
		double l2 = Math.Sqrt(e2.x * e2.x + e2.y * e2.y + e2.z * e2.z);
		double longestEdge = Math.Max(l0, Math.Max(l1, l2));
		double wPerpLength = Math.Sqrt(wPerp2);
		if (0.1 > longestEdge * wPerpLength)
		{
			double phase = math.dot((a + b + c) / 3.0, w);
			Complex64 orbit1 = DunavantOrbit(a, b, c, w, A1, B1, 0.132394152788506);
			Complex64 orbit2 = DunavantOrbit(a, b, c, w, A2, B2, 0.125939180544827);
			double area = twoArea * 0.5;
			return new Complex64((Math.Cos(phase) * 0.225 + orbit1.Real + orbit2.Real) * area, (Math.Sin(phase) * 0.225 + orbit1.Imaginary + orbit2.Imaginary) * area);
		}
		Complex64 edgeSum = EdgeTerm(a, b, e0, unit, wPerp, w) + EdgeTerm(b, c, e1, unit, wPerp, w) + EdgeTerm(c, a, e2, unit, wPerp, w);
		return new Complex64(edgeSum.Imaginary, -edgeSum.Real) / wPerp2;
	}

	private static Complex64 DunavantOrbit(double3 a, double3 b, double3 c, double3 w, double unique, double repeated, double weight)
	{
		double phase1 = math.dot(a * unique + b * repeated + c * repeated, w);
		double phase2 = math.dot(a * repeated + b * unique + c * repeated, w);
		double phase3 = math.dot(a * repeated + b * repeated + c * unique, w);
		return new Complex64((Math.Cos(phase1) + Math.Cos(phase2) + Math.Cos(phase3)) * weight, (Math.Sin(phase1) + Math.Sin(phase2) + Math.Sin(phase3)) * weight);
	}

	private static Complex64 EdgeTerm(double3 a, double3 b, double3 edge, double3 normal, double3 tangent, double3 w)
	{
		double phase = math.dot((a + b) * 0.5, w);
		double amplitude = Sinc(math.dot(edge, w) * 0.5) * math.dot(math.cross(edge, normal), tangent);
		return new Complex64(Math.Cos(phase) * amplitude, Math.Sin(phase) * amplitude);
	}

	public static double Sinc(double x)
	{
		if (1e-05 > Math.Abs(x))
		{
			double x2 = x * x;
			return 1.0 - x2 / 6.0 + x2 * x2 / 120.0 - x2 * x2 * x2 / 5040.0;
		}
		return Math.Sin(x) / x;
	}

	public static void EvaluateMonostaticS(RadarScene scene, RadarPoPrepared prepared, RadarDirectionBasis dir, double k, CancellationToken token, out Complex64 shh, out Complex64 shv, out Complex64 svh, out Complex64 svv)
	{
		if (scene == null || prepared == null)
		{
			throw new ArgumentNullException();
		}
		double3 w = (dir.ToRadar - dir.Incident) * k;
		double3 v0 = math.cross(dir.Incident, dir.Horizontal);
		double3 v1 = math.cross(dir.Incident, dir.Vertical);
		MonostaticKahanVector kahanH = default(MonostaticKahanVector);
		MonostaticKahanVector kahanV = default(MonostaticKahanVector);
		MonostaticKahanVector faceH = default(MonostaticKahanVector);
		MonostaticKahanVector faceV = default(MonostaticKahanVector);
		int face = -1;
		int flushedFace = 0;
		for (int i = 0; i < prepared.VisibleTriangles.Length; i++)
		{
			if ((i & 0x7F) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			RadarPoTriangle tri = prepared.VisibleTriangles[i];
			if (tri.Face != face)
			{
				if (face >= 0)
				{
					kahanH.AddZeros(face - flushedFace);
					kahanV.AddZeros(face - flushedFace);
					kahanH.Add(faceH.Value);
					kahanV.Add(faceV.Value);
					flushedFace = face + 1;
				}
				faceH = default(MonostaticKahanVector);
				faceV = default(MonostaticKahanVector);
				face = tri.Face;
			}
			Complex64 integral = TriangleIntegral(tri.A, tri.B, tri.C, w);
			double3 t0 = math.cross(tri.EffectiveNormal, v0) * 2.0;
			double3 t1 = math.cross(tri.EffectiveNormal, v1) * 2.0;
			faceH.Add(RadarCVector.FromReal(t0) * integral);
			faceV.Add(RadarCVector.FromReal(t1) * integral);
		}
		if (face >= 0)
		{
			kahanH.AddZeros(face - flushedFace);
			kahanV.AddZeros(face - flushedFace);
			kahanH.Add(faceH.Value);
			kahanV.Add(faceV.Value);
			flushedFace = face + 1;
		}
		kahanH.AddZeros(scene.TriangleCount - flushedFace);
		kahanV.AddZeros(scene.TriangleCount - flushedFace);
		shh = RadarFarField.Project(kahanH.Value, RadarCVector.Zero, dir.ToRadar, dir.Horizontal, k);
		shv = RadarFarField.Project(kahanH.Value, RadarCVector.Zero, dir.ToRadar, dir.Vertical, k);
		svh = RadarFarField.Project(kahanV.Value, RadarCVector.Zero, dir.ToRadar, dir.Horizontal, k);
		svv = RadarFarField.Project(kahanV.Value, RadarCVector.Zero, dir.ToRadar, dir.Vertical, k);
	}

	private static void CoatedCurrents(RadarPoTriangle tri, double3 incident, double3 polarization, Complex64 gammaTE, Complex64 gammaTM, Complex64 integral, out RadarCVector electric, out RadarCVector magnetic)
	{
		double3 normal = tri.EffectiveNormal;
		double3 reflected = incident - normal * (2.0 * math.dot(incident, normal));
		RadarCVector reflectedField = RadarCoating.ReflectWith(gammaTE, gammaTM, RadarCVector.FromReal(polarization), incident, normal, reflected);
		RadarCVector a = RadarCVector.Cross(reflected, reflectedField) + RadarCVector.FromReal(math.cross(incident, polarization));
		RadarCVector b = reflectedField + RadarCVector.FromReal(polarization);
		electric = RadarCVector.Cross(normal, a) * integral;
		magnetic = RadarCVector.Cross(normal, b) * -1.0 * integral;
	}

	public static void EvaluateCoated(RadarScene scene, RadarPoPrepared prepared, RadarDirectionBasis incident, RadarDirectionBasis receiver, double wavenumber, RadarCoating coating, CancellationToken token, out RadarCVector[,] electric, out RadarCVector[,] magnetic)
	{
		if (scene == null || prepared == null || coating == null)
		{
			throw new ArgumentNullException();
		}
		double3 w = (receiver.ToRadar - incident.Incident) * wavenumber;
		int faceCount = scene.TriangleCount;
		RadarKahanVector[,] kahanElectric = new RadarKahanVector[faceCount, 2];
		RadarKahanVector[,] kahanMagnetic = new RadarKahanVector[faceCount, 2];
		for (int i = 0; i < prepared.VisibleTriangles.Length; i++)
		{
			if ((i & 0x7F) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			RadarPoTriangle tri = prepared.VisibleTriangles[i];
			Complex64 integral = TriangleIntegral(tri.A, tri.B, tri.C, w);
			coating.Gamma(wavenumber, math.dot(tri.EffectiveNormal, incident.ToRadar), out Complex64 te, out Complex64 tm);
			CoatedCurrents(tri, incident.Incident, incident.Horizontal, te, tm, integral, out RadarCVector electric0, out RadarCVector magnetic0);
			CoatedCurrents(tri, incident.Incident, incident.Vertical, te, tm, integral, out RadarCVector electric1, out RadarCVector magnetic1);
			kahanElectric[tri.Face, 0].Add(electric0);
			kahanElectric[tri.Face, 1].Add(electric1);
			kahanMagnetic[tri.Face, 0].Add(magnetic0);
			kahanMagnetic[tri.Face, 1].Add(magnetic1);
		}
		electric = new RadarCVector[faceCount, 2];
		magnetic = new RadarCVector[faceCount, 2];
		for (int face = 0; face < scene.TriangleCount; face++)
		{
			for (int polarization = 0; polarization < 2; polarization++)
			{
				electric[face, polarization] = kahanElectric[face, polarization].Value;
				magnetic[face, polarization] = kahanMagnetic[face, polarization].Value;
			}
		}
	}

	public static Complex64 EvaluateSelectedCoated(RadarScene scene, RadarPoPrepared prepared, RadarDirectionBasis incident, RadarDirectionBasis receiver, double wavenumber, int receivePolarization, int transmitPolarization, double surfaceOffsetMeters, RadarCoating coating, CancellationToken token)
	{
		if (scene == null || prepared == null || coating == null)
		{
			throw new ArgumentNullException();
		}
		if (receivePolarization < 0 || receivePolarization > 1 || transmitPolarization < 0 || transmitPolarization > 1)
		{
			throw new ArgumentOutOfRangeException();
		}
		double3 w = (receiver.ToRadar - incident.Incident) * wavenumber;
		double3 transmitBasis = ((transmitPolarization == 0) ? incident.Horizontal : incident.Vertical);
		double3 receiveBasis = ((receivePolarization == 0) ? receiver.Horizontal : receiver.Vertical);
		RadarKahanComplex sum = default(RadarKahanComplex);
		for (int i = 0; i < prepared.VisibleTriangles.Length; i++)
		{
			if ((i & 0xFF) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			RadarPoTriangle tri = prepared.VisibleTriangles[i];
			if (0.0 < math.dot(tri.EffectiveNormal, receiver.ToRadar))
			{
				double3 shadowOrigin = (tri.A + tri.B + tri.C) / 3.0 + tri.EffectiveNormal * surfaceOffsetMeters;
				if (!scene.Bvh.AnyHit(shadowOrigin, receiver.ToRadar, tri.Face, double.PositiveInfinity, null))
				{
					Complex64 integral = TriangleIntegral(tri.A, tri.B, tri.C, w);
					coating.Gamma(wavenumber, math.dot(tri.EffectiveNormal, incident.ToRadar), out Complex64 te, out Complex64 tm);
					CoatedCurrents(tri, incident.Incident, transmitBasis, te, tm, integral, out RadarCVector electric, out RadarCVector magnetic);
					RadarCVector scattered = RadarFarField.Scatter(electric, magnetic, receiver.ToRadar, wavenumber);
					sum.Add(scattered.Dot(receiveBasis));
				}
			}
		}
		return sum.Value;
	}

	public static void EvaluateMonostaticSCoated(RadarScene scene, RadarPoPrepared prepared, RadarDirectionBasis dir, double k, RadarCoating coating, CancellationToken token, out Complex64 shh, out Complex64 shv, out Complex64 svh, out Complex64 svv)
	{
		if (scene == null || prepared == null || coating == null)
		{
			throw new ArgumentNullException();
		}
		double3 w = (dir.ToRadar - dir.Incident) * k;
		MonostaticKahanVector electricH = default(MonostaticKahanVector);
		MonostaticKahanVector electricV = default(MonostaticKahanVector);
		MonostaticKahanVector magneticH = default(MonostaticKahanVector);
		MonostaticKahanVector magneticV = default(MonostaticKahanVector);
		MonostaticKahanVector faceElectricH = default(MonostaticKahanVector);
		MonostaticKahanVector faceElectricV = default(MonostaticKahanVector);
		MonostaticKahanVector faceMagneticH = default(MonostaticKahanVector);
		MonostaticKahanVector faceMagneticV = default(MonostaticKahanVector);
		int face = -1;
		int flushedFace = 0;
		for (int i = 0; i < prepared.VisibleTriangles.Length; i++)
		{
			if ((i & 0x7F) == 0)
			{
				token.ThrowIfCancellationRequested();
			}
			RadarPoTriangle tri = prepared.VisibleTriangles[i];
			if (tri.Face != face)
			{
				if (face >= 0)
				{
					electricH.AddZeros(face - flushedFace);
					electricV.AddZeros(face - flushedFace);
					magneticH.AddZeros(face - flushedFace);
					magneticV.AddZeros(face - flushedFace);
					electricH.Add(faceElectricH.Value);
					electricV.Add(faceElectricV.Value);
					magneticH.Add(faceMagneticH.Value);
					magneticV.Add(faceMagneticV.Value);
					flushedFace = face + 1;
				}
				faceElectricH = default(MonostaticKahanVector);
				faceElectricV = default(MonostaticKahanVector);
				faceMagneticH = default(MonostaticKahanVector);
				faceMagneticV = default(MonostaticKahanVector);
				face = tri.Face;
			}
			Complex64 integral = TriangleIntegral(tri.A, tri.B, tri.C, w);
			coating.Gamma(k, math.dot(tri.EffectiveNormal, dir.ToRadar), out Complex64 te, out Complex64 tm);
			CoatedCurrents(tri, dir.Incident, dir.Horizontal, te, tm, integral, out RadarCVector electric0, out RadarCVector magnetic0);
			CoatedCurrents(tri, dir.Incident, dir.Vertical, te, tm, integral, out RadarCVector electric1, out RadarCVector magnetic1);
			faceElectricH.Add(electric0);
			faceElectricV.Add(electric1);
			faceMagneticH.Add(magnetic0);
			faceMagneticV.Add(magnetic1);
		}
		if (face >= 0)
		{
			int gap = face - flushedFace;
			if (gap > 0)
			{
				electricH.AddZeros(gap);
				electricV.AddZeros(gap);
				magneticH.AddZeros(gap);
				magneticV.AddZeros(gap);
			}
			electricH.Add(faceElectricH.Value);
			electricV.Add(faceElectricV.Value);
			magneticH.Add(faceMagneticH.Value);
			magneticV.Add(faceMagneticV.Value);
			flushedFace = face + 1;
		}
		int tail = scene.TriangleCount - flushedFace;
		if (tail > 0)
		{
			electricH.AddZeros(tail);
			electricV.AddZeros(tail);
			magneticH.AddZeros(tail);
			magneticV.AddZeros(tail);
		}
		shh = RadarFarField.Project(electricH.Value, magneticH.Value, dir.ToRadar, dir.Horizontal, k);
		shv = RadarFarField.Project(electricH.Value, magneticH.Value, dir.ToRadar, dir.Vertical, k);
		svh = RadarFarField.Project(electricV.Value, magneticV.Value, dir.ToRadar, dir.Horizontal, k);
		svv = RadarFarField.Project(electricV.Value, magneticV.Value, dir.ToRadar, dir.Vertical, k);
	}
}
