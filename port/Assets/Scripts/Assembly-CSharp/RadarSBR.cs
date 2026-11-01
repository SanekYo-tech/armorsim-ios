using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;

public static class RadarSBR
{
	private const int MaxVisualPaths = 6000;

	private const int MaxFirstOrderPaths = 8000;

	internal static RadarSbrOutput Trace(RadarScene scene, RadarDirectionBasis incident, RadarDirectionBasis receiver, double wavelength, int gridSide, double spacing, int maxBounces, double epsilon, RadarCounters counters, CancellationToken token)
	{
		int faceCount = scene.TriangleCount;
		RadarSbrOutput output = new RadarSbrOutput(faceCount);
		RadarKahanVector[,] kahanByOrder = new RadarKahanVector[4, 2];
		RadarKahanComplex[,,] kahanByFacet = new RadarKahanComplex[2, 2, faceCount];
		List<RadarPath> paths = new List<RadarPath>(MaxVisualPaths);
		List<RadarPath> firstOrderPaths = new List<RadarPath>(MaxFirstOrderPaths);
		long pathCount = 0L;
		long firstOrderCount = 0L;
		double k = 6.283185307179586 / wavelength;
		double3 center = scene.Center;
		double radius = scene.BoundingSphereRadius;
		double3 cellH = incident.Horizontal * spacing;
		double3 cellV = incident.Vertical * spacing;
		double half = (double)(gridSide - 1) * 0.5;
		counters.LaunchedRays = gridSide * gridSide;
		for (int row = 0; row < gridSide; row++)
		{
			token.ThrowIfCancellationRequested();
			double rowOffset = ((double)row - half) * spacing;
			for (int col = 0; col < gridSide; col++)
			{
				if ((col & 0xFF) == 0)
				{
					token.ThrowIfCancellationRequested();
				}
				double colOffset = ((double)col - half) * spacing;
				double3 origin = center + incident.Horizontal * rowOffset + incident.Vertical * colOffset + incident.ToRadar * (radius * 1.05);
				Complex64 planeWave = Complex64.Phase(math.dot(incident.Incident, origin) * -k);
				RadarCVector fieldH = RadarCVector.FromReal(incident.Horizontal) * planeWave;
				RadarCVector fieldV = RadarCVector.FromReal(incident.Vertical) * planeWave;
				double3 position = origin;
				double3 direction = incident.Incident;
				int lastFace = -1;
				int face1 = -1;
				int face2 = -1;
				int face3 = -1;
				int face4 = -1;
				double3 point1 = default(double3);
				double3 point2 = default(double3);
				double3 point3 = default(double3);
				double3 point4 = default(double3);
				double3 posAfter1 = default(double3);
				double3 dirAfter1 = default(double3);
				bool escaped = false;
				bool grazing = false;
				bool truncated = false;
				double amplitude = 0.0;
				int bounce = 0;
				while (bounce < maxBounces)
				{
					RadarHit hit = scene.Bvh.Closest(position, direction, lastFace, double.PositiveInfinity, counters);
					if (!hit.Found)
					{
						escaped = true;
						break;
					}
					bounce++;
					lastFace = hit.Face;
					if (bounce == 1)
					{
						counters.HitRays++;
						counters.SurfaceHits++;
						face1 = hit.Face;
						point1 = hit.Point;
					}
					else
					{
						counters.SurfaceHits++;
						if (bounce < 5)
						{
							if (bounce == 2)
							{
								face2 = hit.Face;
								point2 = hit.Point;
							}
							else if (bounce == 3)
							{
								face3 = hit.Face;
								point3 = hit.Point;
							}
							else if (bounce == 4)
							{
								face4 = hit.Face;
								point4 = hit.Point;
							}
						}
					}
					if (output.MaxBounceByFacet[hit.Face] < bounce)
					{
						output.MaxBounceByFacet[hit.Face] = bounce;
					}
					double3 normal = scene.Normals[hit.Face];
					if (0.0 < math.dot(direction, normal))
					{
						normal = -normal;
					}
					double ndot = math.dot(direction, normal);
					if (ndot > -0.001)
					{
						counters.GrazingTerminations++;
						grazing = true;
						truncated = bounce == 1;
						break;
					}
					Complex64 propagated = Complex64.Phase(hit.Distance * -k);
					fieldH *= propagated;
					fieldV *= propagated;
					double3 u = cellH - (math.dot(normal, cellH) / ndot) * direction;
					double3 v = cellV - (math.dot(normal, cellV) / ndot) * direction;
					double area = math.length(math.cross(u, v));
					if (bounce > 1 && 0.0 < math.dot(receiver.ToRadar, normal))
					{
						counters.ShadowRays++;
						if (!scene.Bvh.AnyHit(hit.Point + normal * epsilon, receiver.ToRadar, hit.Face, double.PositiveInfinity, counters))
						{
							double3 w = (receiver.ToRadar - direction) * k;
							double scale = RadarFacetPO.Sinc(math.dot(u, w) * 0.5) * RadarFacetPO.Sinc(math.dot(v, w) * 0.5) * area;
							Complex64 scatterPhase = Complex64.Phase(math.dot(receiver.ToRadar, hit.Point) * k) * scale;
							RadarCVector electricH = RadarCVector.Cross(normal, RadarCVector.Cross(direction, fieldH)) * 2.0 * scatterPhase;
							RadarCVector electricV = RadarCVector.Cross(normal, RadarCVector.Cross(direction, fieldV)) * 2.0 * scatterPhase;
							int order = Math.Min(bounce - 1, 3);
							kahanByOrder[order, 0].Add(electricH);
							kahanByOrder[order, 1].Add(electricV);
							Complex64 hh = RadarFarField.Project(electricH, RadarCVector.Zero, receiver.ToRadar, receiver.Horizontal, k);
							Complex64 hv = RadarFarField.Project(electricH, RadarCVector.Zero, receiver.ToRadar, receiver.Vertical, k);
							Complex64 vh = RadarFarField.Project(electricV, RadarCVector.Zero, receiver.ToRadar, receiver.Horizontal, k);
							Complex64 vv = RadarFarField.Project(electricV, RadarCVector.Zero, receiver.ToRadar, receiver.Vertical, k);
							kahanByFacet[0, 0, hit.Face].Add(hh);
							kahanByFacet[1, 0, hit.Face].Add(hv);
							kahanByFacet[0, 1, hit.Face].Add(vh);
							kahanByFacet[1, 1, hit.Face].Add(vv);
							amplitude += vv.Magnitude;
						}
					}
					fieldH = new RadarCVector(Complex64.Zero - fieldH.X, Complex64.Zero - fieldH.Y, Complex64.Zero - fieldH.Z) + RadarCVector.FromReal(normal) * (2.0 * fieldH.Dot(normal));
					fieldV = new RadarCVector(Complex64.Zero - fieldV.X, Complex64.Zero - fieldV.Y, Complex64.Zero - fieldV.Z) + RadarCVector.FromReal(normal) * (2.0 * fieldV.Dot(normal));
					cellH = cellH - normal * (2.0 * math.dot(normal, cellH));
					cellV = cellV - normal * (2.0 * math.dot(normal, cellV));
					direction = direction - normal * (2.0 * ndot);
					position = hit.Point + normal * epsilon;
					if (bounce == 1)
					{
						posAfter1 = position;
						dirAfter1 = direction;
					}
				}
				if (face1 >= 0 && !truncated)
				{
					firstOrderCount++;
					int index;
					if (firstOrderCount <= 8000)
					{
						index = (int)firstOrderCount - 1;
					}
					else
					{
						ulong r = Mix((ulong)firstOrderCount ^ 0x56C4C3D2A1B0E9F8uL);
						index = ((r % (ulong)firstOrderCount < 8000) ? ((int)(r % (ulong)firstOrderCount)) : (-1));
					}
					if (index >= 0)
					{
						bool firstEscapes = bounce == 1 && escaped;
						double3 endPoint;
						if (bounce < 2)
						{
							if (!firstEscapes)
							{
								endPoint = posAfter1;
							}
							else
							{
								endPoint = ExitOnSphere(posAfter1, dirAfter1, center, radius + radius);
							}
						}
						else
						{
							endPoint = point2;
						}
						RadarPath path = new RadarPath(new double3[3] { origin, point1, endPoint }, new int[1] { face1 }, 1, 1.0, firstEscapes, !firstEscapes);
						if (index == firstOrderPaths.Count)
						{
							firstOrderPaths.Add(path);
						}
						else
						{
							firstOrderPaths[index] = path;
						}
					}
				}
				if (bounce > 1)
				{
					counters.MultiBounceRays++;
					pathCount++;
					int index;
					if (pathCount <= 6000)
					{
						index = (int)pathCount - 1;
					}
					else
					{
						ulong r = Mix((ulong)pathCount);
						index = ((r % (ulong)pathCount < 6000) ? ((int)(r % (ulong)pathCount)) : (-1));
					}
					if (index >= 0)
					{
						int bounceCount = Math.Min(bounce, 4);
						if (!escaped && !grazing && bounce == maxBounces)
						{
							RadarHit endHit = scene.Bvh.Closest(position, direction, lastFace, double.PositiveInfinity, null);
							escaped = !endHit.Found;
						}
						bool escapes = bounceCount == bounce && escaped && !grazing;
						double3[] points = new double3[1 + bounceCount + (escapes ? 1 : 0)];
						int[] faces = new int[bounceCount];
						points[0] = origin;
						if (bounceCount > 0)
						{
							points[1] = point1;
							faces[0] = face1;
						}
						if (bounceCount > 1)
						{
							points[2] = point2;
							faces[1] = face2;
						}
						if (bounceCount > 2)
						{
							points[3] = point3;
							faces[2] = face3;
						}
						if (bounceCount > 3)
						{
							points[4] = point4;
							faces[3] = face4;
						}
						if (escapes)
						{
							points[bounceCount + 1] = ExitOnSphere(position, direction, center, radius + radius);
						}
						RadarPath path2 = new RadarPath(points, faces, bounceCount, amplitude, escapes, !escapes);
						if (index == paths.Count)
						{
							paths.Add(path2);
						}
						else
						{
							paths[index] = path2;
						}
					}
				}
			}
		}
		counters.QualifiedPaths = pathCount;
		output.Paths = paths.ToArray();
		output.FirstOrderPaths = firstOrderPaths.ToArray();
		for (int order2 = 1; order2 < 4; order2++)
		{
			for (int polarization = 0; polarization < 2; polarization++)
			{
				output.ByOrder[order2, polarization] = kahanByOrder[order2, polarization].Value;
			}
		}
		for (int face = 0; face < faceCount; face++)
		{
			for (int receive = 0; receive < 2; receive++)
			{
				for (int transmit = 0; transmit < 2; transmit++)
				{
					output.ByFacet[receive, transmit, face] = kahanByFacet[receive, transmit, face].Value;
				}
			}
		}
		return output;
	}

	private static ulong Mix(ulong x)
	{
		x += 11400714819323198485uL;
		x ^= x >> 30;
		x *= 13787848793156543929uL;
		x ^= x >> 27;
		x *= 10723151780598845931uL;
		x ^= x >> 31;
		return x;
	}

	internal static double3 ExitOnSphere(double3 origin, double3 direction, double3 center, double radius)
	{
		double3 oc = origin - center;
		double b = oc.x * direction.x + oc.y * direction.y + oc.z * direction.z;
		double c = oc.x * oc.x + oc.y * oc.y + oc.z * oc.z - radius * radius;
		double disc = b * b - c;
		double t = ((disc >= 0.0) ? (-b + Math.Sqrt(disc)) : 0.0);
		return origin + direction * Math.Max(0.0, t);
	}
}
