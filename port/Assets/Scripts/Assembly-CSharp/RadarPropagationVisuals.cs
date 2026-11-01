using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class RadarPropagationVisuals : MonoBehaviour
{
	private sealed class DisplayPath
	{
		public Vector3[] Points;

		public double[] CumulativeMeters;

		public double TotalMeters;

		public double InitialDelayMeters;

		public float Power;
	}

	public const float WavefrontSpeedRadiiPerSecond = 1.3333334f;

	public const int MaximumInputPaths = 100000;

	public const int MaximumPatternSamples = 10000;

	public const int MaximumPointsPerPath = 6;

	public const int MaximumReflectedWavePatches = 256;

	private const int WavePatchSides = 16;

	private static readonly Color IncidentRayColor;

	private static readonly Color IncidentPulseColor;

	private static readonly Color WeakReturnColor;

	private static readonly Color FrontColor;

	private static readonly int[] PulseFaces;

	private readonly List<DisplayPath> displayed;

	private readonly List<int> reflectionCandidates;

	private readonly List<Vector3> vertices;

	private readonly List<Color> colors;

	private readonly List<int> triangles;

	private readonly List<Mesh> ownedMeshes;

	private readonly List<Material> ownedMaterials;

	private Vector3[][] sourcePaths;

	private float[] sourcePower;

	private Vector3[] patternDirections;

	private float[] patternPower;

	private int patternAzimuthSamples;

	private int patternElevationSamples;

	private float maximumPathPower;

	private float maximumPatternPower;

	private Vector3 modelCenter;

	private Vector3 u;

	private Vector3 h;

	private Vector3 v;

	private float rbs;

	private float wavelengthMeters;

	private bool geometryReady;

	private bool built;

	private bool showRays;

	private bool showWavefronts;

	private bool showPattern;

	private bool wavefrontOnlyPresentation;

	private float playbackSpeed;

	private float secondaryAlphaScale;

	private float secondaryWidthScale;

	private float patternFloorDb;

	private int pathStride;

	private int maxVisiblePaths;

	private int maxVisiblePulses;

	private int wavefrontCount;

	private double animationDistanceMeters;

	private double cycleMeters;

	private Mesh rayMesh;

	private Mesh pulseMesh;

	private Mesh wavefrontMesh;

	private Mesh reflectedWaveMesh;

	private Mesh patternMesh;

	private GameObject rayObject;

	private GameObject pulseObject;

	private GameObject wavefrontObject;

	private GameObject reflectedWaveObject;

	private GameObject patternObject;

	public bool WavefrontOnlyPresentation
	{
		get
		{
			return wavefrontOnlyPresentation;
		}
		set
		{
			if (wavefrontOnlyPresentation != value)
			{
				wavefrontOnlyPresentation = value;
				ApplyVisibility();
				RebuildAnimatedMeshes();
			}
		}
	}

	public bool ShowRays
	{
		get
		{
			return showRays;
		}
		set
		{
			showRays = value;
			ApplyVisibility();
		}
	}

	public bool ShowWavefronts
	{
		get
		{
			return showWavefronts;
		}
		set
		{
			showWavefronts = value;
			ApplyVisibility();
		}
	}

	public bool ShowPattern
	{
		get
		{
			return showPattern;
		}
		set
		{
			showPattern = value;
			ApplyVisibility();
		}
	}

	public float PlaybackSpeed
	{
		get
		{
			return playbackSpeed;
		}
		set
		{
			if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			playbackSpeed = value;
		}
	}

	public int PathStride
	{
		get
		{
			return pathStride;
		}
		set
		{
			if (value < 1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (pathStride != value)
			{
				pathStride = value;
				SelectPathsAndBuild();
			}
		}
	}

	public int MaxVisiblePaths
	{
		get
		{
			return maxVisiblePaths;
		}
		set
		{
			if (value < 1 || value > 10000)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (maxVisiblePaths != value)
			{
				maxVisiblePaths = value;
				SelectPathsAndBuild();
			}
		}
	}

	public int MaxVisiblePulses
	{
		get
		{
			return maxVisiblePulses;
		}
		set
		{
			if (value < 8 || value > 50000)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (maxVisiblePulses != value)
			{
				maxVisiblePulses = value;
				RebuildAnimatedMeshes();
			}
		}
	}

	public int WavefrontCount
	{
		get
		{
			return wavefrontCount;
		}
		set
		{
			if (value < 1 || value > 8)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			wavefrontCount = value;
			RecalculateTiming();
			RebuildAnimatedMeshes();
		}
	}

	public float SecondaryAlphaScale
	{
		get
		{
			return secondaryAlphaScale;
		}
		set
		{
			if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			secondaryAlphaScale = value;
			RebuildRayMesh();
		}
	}

	public float SecondaryWidthScale
	{
		get
		{
			return secondaryWidthScale;
		}
		set
		{
			if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f || value > 2f)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			secondaryWidthScale = value;
			RebuildRayMesh();
		}
	}

	public float PatternFloorDb
	{
		get
		{
			return patternFloorDb;
		}
		set
		{
			if (float.IsNaN(value) || float.IsInfinity(value) || value >= 0f || value < -120f)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			patternFloorDb = value;
			RebuildPatternMesh();
		}
	}

	public bool SecondaryScalingIsVisualOnly => secondaryAlphaScale == 1f && secondaryWidthScale == 1f;

	public int InputPathCount => (sourcePaths != null) ? sourcePaths.Length : 0;

	public int InputSegmentCount { get; private set; }

	public int DisplayedPathCount => displayed.Count;

	public int DisplayedSegmentCount { get; private set; }

	public int EffectivePathStride { get; private set; }

	public int EffectivePulsePathStride { get; private set; }

	public int VisiblePulseCount { get; private set; }

	public int VisibleWavefrontCount { get; private set; }

	public int VisibleReflectedWavePatchCount { get; private set; }

	public int PatternVertexCount => (patternDirections != null) ? patternDirections.Length : 0;

	public int PatternTriangleCount { get; private set; }

	public long EffectiveWavefrontPeriodStride { get; private set; }

	public double WavefrontSpacingMeters { get; private set; }

	public double AnimationDistanceMeters => animationDistanceMeters;

	public double MaxInitialDelayMeters { get; private set; }

	public float LastBuildMilliseconds { get; private set; }

	public float LastAnimationMilliseconds { get; private set; }

	public Vector3 PatternCenter => modelCenter + h * (rbs * 2.8f);

	public float GetDisplayedPathPower(int index)
	{
		if (index < 0 || index >= displayed.Count)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		return displayed[index].Power;
	}

	public void SetGeometry(Vector3 center, float radius, Vector3 toRadar, Vector3 horizontal, Vector3 vertical, float wavelength)
	{
		ValidateGeometry(center, radius, wavelength);
		ValidateBasis(toRadar, horizontal, vertical);
		EnsureBuilt();
		modelCenter = center;
		u = toRadar;
		h = horizontal;
		v = vertical;
		rbs = radius;
		wavelengthMeters = wavelength;
		geometryReady = true;
		RecalculateWavefrontSpacing();
		RecalculatePathDelays();
		RecalculateTiming();
		RebuildRayMesh();
		RebuildPatternMesh();
		RebuildAnimatedMeshes();
	}

	public void SetPaths(Vector3[][] paths, float[] relativePower, Vector3 center, float radius, float wavelength)
	{
		if (!geometryReady)
		{
			throw new InvalidOperationException("SetGeometry must precede SetPaths.");
		}
		ValidateGeometry(center, radius, wavelength);
		if (paths == null || relativePower == null || relativePower.Length != paths.Length)
		{
			throw new ArgumentException("Radar paths and relative power arrays must have equal lengths.");
		}
		if (paths.Length > MaximumInputPaths)
		{
			throw new ArgumentOutOfRangeException("paths", paths.Length, "Radar visual input exceeds the declared path limit.");
		}
		long start = Stopwatch.GetTimestamp();
		Vector3[][] copy = new Vector3[paths.Length][];
		float[] powerCopy = (float[])relativePower.Clone();
		float max = 0f;
		int segments = 0;
		for (int i = 0; i < paths.Length; i++)
		{
			Vector3[] path = paths[i];
			if (path == null || path.Length < 2 || path.Length > MaximumPointsPerPath)
			{
				throw new ArgumentException("Each visual path needs 2 to 6 points (at most four bounces).");
			}
			float power = relativePower[i];
			if (float.IsNaN(power) || float.IsInfinity(power) || power < 0f)
			{
				throw new ArgumentException("Path power must be finite and nonnegative.");
			}
			copy[i] = (Vector3[])path.Clone();
			for (int j = 0; j < path.Length; j++)
			{
				if (!Finite(path[j]))
				{
					throw new ArgumentException("Path contains nonfinite position.");
				}
				if (j > 0 && Distance(path[j - 1], path[j]) <= 0.0)
				{
					throw new ArgumentException("Path contains a zero-length segment.");
				}
			}
			segments += path.Length - 1;
			if (power > max)
			{
				max = power;
			}
		}
		sourcePaths = copy;
		sourcePower = powerCopy;
		maximumPathPower = max;
		modelCenter = center;
		InputSegmentCount = segments;
		RecalculateWavefrontSpacing();
		SelectPathsAndBuild();
		animationDistanceMeters = 0.0;
		RebuildPatternMesh();
		RebuildAnimatedMeshes();
		LastBuildMilliseconds = ElapsedMilliseconds(start);
	}

	public void SetPattern(Vector3[] directions, float[] relativePower, int azimuthSamples, int elevationSamples)
	{
		if (!geometryReady)
		{
			throw new InvalidOperationException("SetGeometry must precede SetPattern.");
		}
		if (directions == null || relativePower == null || azimuthSamples < 3 || elevationSamples < 3 || azimuthSamples * elevationSamples != directions.Length || relativePower.Length != directions.Length || directions.Length > MaximumPatternSamples)
		{
			throw new ArgumentException("Invalid structured Radar pattern grid.");
		}
		Vector3[] copy = (Vector3[])directions.Clone();
		float[] powerCopy = (float[])relativePower.Clone();
		float max = 0f;
		for (int i = 0; i < copy.Length; i++)
		{
			Vector3 direction = copy[i];
			if (!Finite(direction) || (direction.x * direction.x + direction.y * direction.y + direction.z * direction.z) <= 0f)
			{
				throw new ArgumentException("Pattern contains invalid direction or power.");
			}
			float power = relativePower[i];
			if (float.IsNaN(power) || float.IsInfinity(power) || power < 0f)
			{
				throw new ArgumentException("Pattern contains invalid direction or power.");
			}
			direction.Normalize();
			copy[i] = direction;
			if (power > max)
			{
				max = power;
			}
		}
		patternDirections = copy;
		patternPower = powerCopy;
		patternAzimuthSamples = azimuthSamples;
		maximumPatternPower = max;
		patternElevationSamples = elevationSamples;
		RebuildPatternMesh();
		ApplyVisibility();
	}

	public void ClearPaths()
	{
		sourcePaths = null;
		sourcePower = null;
		displayed.Clear();
		reflectionCandidates.Clear();
		maximumPathPower = 0f;
		VisibleReflectedWavePatchCount = 0;
		InputSegmentCount = 0;
		MaxInitialDelayMeters = 0.0;
		animationDistanceMeters = 0.0;
		EffectivePathStride = 1;
		EffectivePulsePathStride = 1;
		if (rayMesh != null)
		{
			rayMesh.Clear();
		}
		if (pulseMesh != null)
		{
			pulseMesh.Clear();
		}
		if (wavefrontMesh != null)
		{
			wavefrontMesh.Clear();
		}
		RecalculateTiming();
	}

	public void ClearPattern()
	{
		patternDirections = null;
		patternPower = null;
		maximumPatternPower = 0f;
		PatternTriangleCount = 0;
		if (patternMesh != null)
		{
			patternMesh.Clear();
		}
		ApplyVisibility();
	}

	public bool TryGetPulsePosition(int displayedPath, int frontIndex, out Vector3 position, out int segmentIndex)
	{
		position = default(Vector3);
		segmentIndex = -1;
		if (displayedPath < 0 || displayedPath >= displayed.Count || frontIndex < 0 || frontIndex >= wavefrontCount || cycleMeters <= 0.0)
		{
			return false;
		}
		DisplayPath path = displayed[displayedPath];
		double distance = PositiveMod(animationDistanceMeters - (double)frontIndex * WavefrontSpacingMeters, cycleMeters);
		distance -= path.InitialDelayMeters;
		if (path.Power <= 0f || distance < 0.0 || distance > path.TotalMeters)
		{
			return false;
		}
		double[] cumulative = path.CumulativeMeters;
		if (cumulative.Length <= 1)
		{
			return false;
		}
		int segment = 0;
		while (segment + 2 < cumulative.Length && cumulative[segment + 1] < distance)
		{
			segment++;
		}
		Vector3[] points = path.Points;
		if (segment + 1 >= points.Length)
		{
			return false;
		}
		double start = cumulative[segment];
		float t = (float)((distance - start) / (cumulative[segment + 1] - start));
		position = points[segment] + (points[segment + 1] - points[segment]) * t;
		segmentIndex = segment;
		return true;
	}

	private void Update()
	{
		if (geometryReady && built)
		{
			AdvanceAnimation(Time.unscaledDeltaTime);
		}
	}

	public void AdvanceAnimation(float unscaledSeconds)
	{
		if (float.IsNaN(unscaledSeconds) || float.IsInfinity(unscaledSeconds) || unscaledSeconds < 0f)
		{
			throw new ArgumentOutOfRangeException("unscaledSeconds");
		}
		if (!geometryReady)
		{
			throw new InvalidOperationException("SetGeometry must precede animation.");
		}
		long start = Stopwatch.GetTimestamp();
		animationDistanceMeters += (double)(unscaledSeconds * WavefrontSpeedRadiiPerSecond) * (double)rbs * (double)playbackSpeed;
		if (animationDistanceMeters > cycleMeters)
		{
			animationDistanceMeters = PositiveMod(animationDistanceMeters, cycleMeters);
		}
		RebuildAnimatedMeshes();
		LastAnimationMilliseconds = ElapsedMilliseconds(start);
	}

	private void EnsureBuilt()
	{
		if (built)
		{
			return;
		}
		Material source = Resources.Load<Material>("RadarPulse");
		if (source == null)
		{
			throw new InvalidOperationException(Localization.T("radar.error.shader"));
		}
		Material rayMaterial = new Material(source);
		rayMaterial.name = "Radar solved rays";
		Own(rayMaterial);
		Material pulseMaterial = new Material(source);
		pulseMaterial.name = "Radar travel pulses";
		Own(pulseMaterial);
		Material frontMaterial = new Material(source);
		frontMaterial.name = "Radar phase fronts";
		Own(frontMaterial);
		Material patchMaterial = new Material(source);
		patchMaterial.name = "Radar reflected phase patches";
		Own(patchMaterial);
		Material patternMaterial = new Material(source);
		patternMaterial.name = "Radar polar pattern";
		Own(patternMaterial);
		rayObject = MakeMeshObject("Radar solved paths", rayMaterial, out rayMesh);
		pulseObject = MakeMeshObject("Radar path pulses", pulseMaterial, out pulseMesh);
		wavefrontObject = MakeMeshObject("Radar wavefronts", frontMaterial, out wavefrontMesh);
		reflectedWaveObject = MakeMeshObject("Radar reflected wave patches", patchMaterial, out reflectedWaveMesh);
		patternObject = MakeMeshObject("Radar 3D directivity", patternMaterial, out patternMesh);
		built = true;
		ApplyVisibility();
	}

	private Material Own(Material value)
	{
		ownedMaterials.Add(value);
		return value;
	}

	private GameObject MakeMeshObject(string name, Material material, out Mesh mesh)
	{
		GameObject gameObject = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		mesh = new Mesh();
		mesh.name = name;
		mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.MarkDynamic();
		ownedMeshes.Add(mesh);
		gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
		gameObject.GetComponent<MeshRenderer>().sharedMaterial = material;
		return gameObject;
	}

	private void ApplyVisibility()
	{
		if (!built)
		{
			return;
		}
		bool rays = showRays && !wavefrontOnlyPresentation;
		rayObject.SetActive(rays);
		pulseObject.SetActive(rays);
		wavefrontObject.SetActive(showWavefronts);
		reflectedWaveObject.SetActive(showWavefronts && wavefrontOnlyPresentation);
		patternObject.SetActive(showPattern && patternDirections != null);
	}

	private void SelectPathsAndBuild()
	{
		if (!geometryReady)
		{
			return;
		}
		long start = Stopwatch.GetTimestamp();
		displayed.Clear();
		reflectionCandidates.Clear();
		DisplayedSegmentCount = 0;
		MaxInitialDelayMeters = 0.0;
		if (sourcePaths == null)
		{
			EffectivePathStride = pathStride;
		}
		else
		{
			int strideCount = (sourcePaths.Length - 1) / pathStride + 1;
			int limit = Math.Min(strideCount, maxVisiblePaths);
			EffectivePathStride = pathStride;
			int previous = 0;
			for (int i = 0; i < limit; i++)
			{
				int stride = (limit > 1) ? (((limit - 1) / 2 + (strideCount - 1) * i) / (limit - 1)) : 0;
				int index = stride * pathStride;
				if (i > 0)
				{
					EffectivePathStride = Math.Max(EffectivePathStride, index - previous);
				}
				previous = index;
				Vector3[] points = sourcePaths[index];
				double[] cumulative = new double[points.Length];
				for (int j = 1; j < points.Length; j++)
				{
					cumulative[j] = cumulative[j - 1] + Distance(points[j - 1], points[j]);
				}
				DisplayPath displayPath = new DisplayPath();
				displayPath.Points = points;
				displayPath.CumulativeMeters = cumulative;
				displayPath.TotalMeters = cumulative[cumulative.Length - 1];
				displayPath.Power = sourcePower[index];
				displayPath.InitialDelayMeters = InitialDelay(points[0]);
				MaxInitialDelayMeters = Math.Max(MaxInitialDelayMeters, displayPath.InitialDelayMeters);
				if (points.Length >= 3 && displayPath.Power > 0f)
				{
					reflectionCandidates.Add(displayed.Count);
				}
				displayed.Add(displayPath);
				DisplayedSegmentCount += points.Length - 1;
			}
		}
		RecalculateTiming();
		RebuildRayMesh();
		RebuildAnimatedMeshes();
		LastBuildMilliseconds = ElapsedMilliseconds(start);
	}

	private void RecalculatePathDelays()
	{
		MaxInitialDelayMeters = 0.0;
		foreach (DisplayPath path in displayed)
		{
			path.InitialDelayMeters = InitialDelay(path.Points[0]);
			MaxInitialDelayMeters = Math.Max(MaxInitialDelayMeters, path.InitialDelayMeters);
		}
	}

	private double InitialDelay(Vector3 origin)
	{
		Vector3 delta = origin - modelCenter;
		return Math.Max(0.0, 2.0 * (double)rbs - ((double)delta.x * (double)u.x + (double)delta.y * (double)u.y + (double)delta.z * (double)u.z));
	}

	private void RecalculateWavefrontSpacing()
	{
		if (!geometryReady)
		{
			return;
		}
		double waves = (double)rbs * 0.8 / (double)wavelengthMeters;
		if (double.IsNaN(waves) || double.IsInfinity(waves) || waves > 9.2233720368547758E+18)
		{
			throw new InvalidOperationException("Wavefront spacing is not representable.");
		}
		EffectiveWavefrontPeriodStride = Math.Max(1L, (long)Math.Floor(waves));
		WavefrontSpacingMeters = (double)wavelengthMeters * (double)EffectiveWavefrontPeriodStride;
		if (double.IsInfinity(WavefrontSpacingMeters))
		{
			throw new InvalidOperationException("Wavefront period stride is not representable.");
		}
	}

	private void RecalculateTiming()
	{
		if (!geometryReady)
		{
			return;
		}
		double travel = (double)rbs * 4.0;
		foreach (DisplayPath path in displayed)
		{
			travel = Math.Max(travel, path.TotalMeters + path.InitialDelayMeters);
		}
		cycleMeters = (double)wavefrontCount * WavefrontSpacingMeters + travel;
		if (cycleMeters <= 0.0 || double.IsInfinity(cycleMeters))
		{
			throw new InvalidOperationException("Radar display cycle is not representable.");
		}
		animationDistanceMeters = PositiveMod(animationDistanceMeters, cycleMeters);
	}

	private void RebuildRayMesh()
	{
		if (!built)
		{
			return;
		}
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		int count = displayed.Count;
		if (count < 1)
		{
			count = 1;
		}
		float density = Math.Min(1500f / (float)count, 1f);
		foreach (DisplayPath path in displayed)
		{
			if (path.Power <= 0f)
			{
				continue;
			}
			float strength = RelativeStrength(path.Power, maximumPathPower, -40f);
			Color tint = (path.Power >= 1f) ? IncidentRayColor : WeakReturnColor;
			for (int j = 0; j + 1 < path.Points.Length; j++)
			{
				float alpha = (strength * 0.085f + 0.015f) * ((j != 0) ? secondaryAlphaScale : 1f) * density;
				float width = rbs * 0.0006f * ((j != 0) ? secondaryWidthScale : 1f);
				AddTube(path.Points[j], path.Points[j + 1], width, new Color(tint.r, tint.g, tint.b, alpha));
			}
		}
		PutMesh(rayMesh);
	}

	private void RebuildAnimatedMeshes()
	{
		if (!built || !geometryReady)
		{
			return;
		}
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		VisibleWavefrontCount = 0;
		if (showWavefronts && wavefrontCount > 0)
		{
			for (int front = 0; front < wavefrontCount; front++)
			{
				double distance = PositiveMod(animationDistanceMeters - (double)front * WavefrontSpacingMeters, cycleMeters);
				if (distance <= (double)rbs * 4.0)
				{
					float ahead = (float)((double)(rbs + rbs) - distance);
					AddFront(modelCenter + u * ahead);
					VisibleWavefrontCount++;
				}
			}
		}
		wavefrontMesh.Clear();
		if (vertices.Count != 0)
		{
			wavefrontMesh.SetVertices(vertices);
			wavefrontMesh.SetColors(colors);
			wavefrontMesh.SetTriangles(triangles, 0);
			wavefrontMesh.RecalculateBounds();
		}
		EffectivePulsePathStride = Math.Max(1, (int)Math.Floor(displayed.Count / (double)(maxVisiblePulses / wavefrontCount)));
		if (showRays && !wavefrontOnlyPresentation)
		{
			VisiblePulseCount = 0;
			for (int i = 0; i < displayed.Count; i += EffectivePulsePathStride)
			{
				DisplayPath path = displayed[i];
				if (path.Power <= 0f)
				{
					continue;
				}
				float strength = RelativeStrength(path.Power, maximumPathPower, -40f);
				Color tint = (path.Power >= 1f) ? IncidentPulseColor : WeakReturnColor;
				for (int front2 = 0; front2 < wavefrontCount; front2++)
				{
					if (TryGetPulsePosition(i, front2, out var position, out var segment))
					{
						float alphaScale = ((segment != 0) ? secondaryAlphaScale : 1f);
						float widthScale = ((segment != 0) ? secondaryWidthScale : 1f);
						Vector3 a = path.Points[segment];
						Vector3 b = path.Points[segment + 1];
						float halfLength = Mathf.Min(Vector3.Angle(a, b) * 0.33f, rbs * 0.012f) * widthScale;
						float halfWidth = Mathf.Min(halfLength * 0.3f, rbs * 0.0018f) * widthScale;
						Vector3 direction = (b - a).normalized;
						AddPulse(position, direction, halfLength, halfWidth, new Color(tint.r, tint.g, tint.b, (strength * 0.65f + 0.2f) * alphaScale));
						VisiblePulseCount++;
					}
				}
			}
			pulseMesh.Clear();
			if (vertices.Count != 0)
			{
				pulseMesh.SetVertices(vertices);
				pulseMesh.SetColors(colors);
				pulseMesh.SetTriangles(triangles, 0);
				pulseMesh.RecalculateBounds();
			}
		}
		else
		{
			pulseMesh.Clear();
		}
		RebuildReflectedWaveMesh();
	}

	private void RebuildReflectedWaveMesh()
	{
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		VisibleReflectedWavePatchCount = 0;
		if (showWavefronts && wavefrontOnlyPresentation && reflectionCandidates != null && reflectionCandidates.Count > 0)
		{
			int count = reflectionCandidates.Count;
			int limit = Math.Min(count, MaximumReflectedWavePatches / wavefrontCount);
			float spread = Mathf.Sqrt((float)limit);
			float maxRadius = Mathf.Min(rbs * 0.5f / spread, rbs * 0.12f);
			for (int i = 0; i < limit; i++)
			{
				int index = (limit < 2) ? 0 : (((limit - 1) / 2 + (count - 1) * i) / (limit - 1));
				int candidate = reflectionCandidates[index];
				DisplayPath path = displayed[candidate];
				float strength = RelativeStrength(path.Power, maximumPathPower, -40f);
				Color tint = (path.Power >= 1f) ? IncidentPulseColor : WeakReturnColor;
				float alpha = (strength * 0.18f + 0.08f) * secondaryAlphaScale;
				for (int front = 0; front < wavefrontCount; front++)
				{
					Vector3 position;
					int segment;
					if (TryGetPulsePosition(candidate, front, out position, out segment) && segment != 0)
					{
						Vector3 a = path.Points[segment];
						Vector3 b = path.Points[segment + 1];
						Vector3 delta = b - a;
						float radius = Mathf.Min(delta.magnitude * 0.25f, maxRadius);
						Vector3 normal = delta.normalized;
						AddWavePatch(position, normal, radius, new Color(tint.r, tint.g, tint.b, alpha));
						VisibleReflectedWavePatchCount++;
					}
				}
			}
		}
		reflectedWaveMesh.Clear();
		if (vertices.Count != 0)
		{
			reflectedWaveMesh.SetVertices(vertices);
			reflectedWaveMesh.SetColors(colors);
			reflectedWaveMesh.SetTriangles(triangles, 0);
			reflectedWaveMesh.RecalculateBounds();
		}
	}

	private void AddWavePatch(Vector3 center, Vector3 normal, float radius, Color color)
	{
		Vector3 helper = (Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.9f) ? Vector3.right : Vector3.up;
		Vector3 side1 = Vector3.Cross(normal, helper).normalized;
		Vector3 side2 = Vector3.Cross(normal, side1).normalized;
		int baseIndex = vertices.Count;
		vertices.Add(center);
		colors.Add(new Color(color.r, color.g, color.b, color.a * 0.35f));
		for (int ring = 0; ring < 2; ring++)
		{
			float ringScale = ((ring == 0) ? 0.8f : 1f);
			float ringRadius = radius * ringScale;
			float alpha = ((ring == 0) ? color.a : 0f);
			for (int side = 0; side < WavePatchSides; side++)
			{
				float angle = (float)side * 6.28318548f * 0.0625f;
				float c = Mathf.Cos(angle);
				float s = Mathf.Sin(angle);
				vertices.Add(center + (side1 * c + side2 * s) * ringRadius);
				colors.Add(new Color(color.r, color.g, color.b, alpha));
			}
		}
		for (int side = 0; side < WavePatchSides; side++)
		{
			int next = (side + 1) % WavePatchSides;
			triangles.Add(baseIndex);
			triangles.Add(baseIndex + 1 + side);
			triangles.Add(baseIndex + 1 + next);
			triangles.Add(baseIndex + 1 + side);
			triangles.Add(baseIndex + WavePatchSides + 1 + side);
			triangles.Add(baseIndex + WavePatchSides + 1 + next);
			triangles.Add(baseIndex + 1 + side);
			triangles.Add(baseIndex + WavePatchSides + 1 + next);
			triangles.Add(baseIndex + 1 + next);
		}
	}

	private void AddFront(Vector3 center)
	{
		float span = rbs * 1.1f;
		Vector3 hh = h * span;
		Vector3 vv = v * span;
		int baseIndex = vertices.Count;
		vertices.Add(center - hh - vv);
		vertices.Add(center + hh - vv);
		vertices.Add(center + hh + vv);
		vertices.Add(center - hh + vv);
		float alpha = (wavefrontOnlyPresentation ? 0.11f : FrontColor.a);
		Color color = new Color(FrontColor.r, FrontColor.g, FrontColor.b, alpha);
		for (int i = 0; i < 4; i++)
		{
			colors.Add(color);
		}
		triangles.Add(baseIndex);
		triangles.Add(baseIndex + 1);
		triangles.Add(baseIndex + 2);
		triangles.Add(baseIndex);
		triangles.Add(baseIndex + 2);
		triangles.Add(baseIndex + 3);
	}

	private void AddTube(Vector3 a, Vector3 b, float width, Color color)
	{
		Vector3 axis = (b - a).normalized;
		Vector3 helper = (Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.9f) ? Vector3.right : Vector3.up;
		Vector3 side1 = Vector3.Cross(axis, helper).normalized * width;
		Vector3 side2 = Vector3.Cross(axis, side1).normalized * width;
		int baseIndex = vertices.Count;
		vertices.Add(a + side1);
		vertices.Add(a + side2);
		vertices.Add(a - side1);
		vertices.Add(a - side2);
		vertices.Add(b + side1);
		vertices.Add(b + side2);
		vertices.Add(b - side1);
		vertices.Add(b - side2);
		for (int i = 0; i < 8; i++)
		{
			colors.Add(color);
		}
		for (int j = 0; j < 4; j++)
		{
			int next = (j + 1) & 3;
			triangles.Add(baseIndex + j);
			triangles.Add(baseIndex + next);
			triangles.Add(baseIndex + 4 + next);
			triangles.Add(baseIndex + j);
			triangles.Add(baseIndex + 4 + next);
			triangles.Add(baseIndex + 4 + j);
		}
	}

	private void AddPulse(Vector3 center, Vector3 direction, float halfLength, float halfWidth, Color color)
	{
		Vector3 helper = (Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.9f) ? Vector3.right : Vector3.up;
		Vector3 side1 = Vector3.Cross(direction, helper).normalized;
		Vector3 side2 = Vector3.Cross(direction, side1).normalized;
		int baseIndex = vertices.Count;
		vertices.Add(center + direction * halfLength);
		vertices.Add(center - direction * halfLength);
		vertices.Add(center + side1 * halfWidth);
		vertices.Add(center - side1 * halfWidth);
		vertices.Add(center + side2 * halfWidth);
		vertices.Add(center - side2 * halfWidth);
		for (int i = 0; i < 6; i++)
		{
			colors.Add(color);
		}
		for (int j = 0; j < PulseFaces.Length; j++)
		{
			triangles.Add(baseIndex + PulseFaces[j]);
		}
	}

	private void RebuildPatternMesh()
	{
		if (!built)
		{
			return;
		}
		vertices.Clear();
		colors.Clear();
		triangles.Clear();
		PatternTriangleCount = 0;
		if (patternDirections == null)
		{
			patternMesh.Clear();
			ApplyVisibility();
			return;
		}
		Vector3 center = modelCenter;
		float scale = rbs * 2.8f;
		for (int i = 0; i < patternDirections.Length; i++)
		{
			float strength = RelativeStrength(patternPower[i], maximumPatternPower, patternFloorDb);
			float ring = (strength * 0.8f + 0.2f) * rbs * 0.8f;
			vertices.Add(center + h * scale + patternDirections[i] * ring);
			if (strength < 0f)
			{
				strength = 0f;
			}
			else if (strength > 1f)
			{
				strength = 1f;
			}
			float level = Mathf.Round(strength * 10f);
			float value = 1f - level / 10f;
			Color rgb = Color.HSVToRGB((value + value) / 3f, 0.9f, 0.95f, hdr: true);
			colors.Add(new Color(rgb.r, rgb.g, rgb.b, 0.56f));
		}
		if (patternElevationSamples > 1 && patternAzimuthSamples > 0)
		{
			for (int elevation = 1; elevation < patternElevationSamples; elevation++)
			{
				int row = elevation - 1;
				for (int azimuth = 0; azimuth < patternAzimuthSamples; azimuth++)
				{
					int next = (azimuth + 1) % patternAzimuthSamples;
					if (elevation != 1)
					{
						triangles.Add(row * patternAzimuthSamples + azimuth);
						triangles.Add(elevation * patternAzimuthSamples + azimuth);
						triangles.Add(row * patternAzimuthSamples + next);
					}
					if (elevation + 1 != patternElevationSamples)
					{
						triangles.Add(row * patternAzimuthSamples + next);
						triangles.Add(elevation * patternAzimuthSamples + azimuth);
						triangles.Add(elevation * patternAzimuthSamples + next);
					}
				}
			}
		}
		PatternTriangleCount = triangles.Count / 3;
		patternMesh.Clear();
		if (vertices.Count != 0)
		{
			patternMesh.SetVertices(vertices);
			patternMesh.SetColors(colors);
			patternMesh.SetTriangles(triangles, 0);
			patternMesh.RecalculateBounds();
		}
		ApplyVisibility();
	}

	private void PutMesh(Mesh mesh)
	{
		mesh.Clear();
		if (vertices.Count != 0)
		{
			mesh.SetVertices(vertices);
			mesh.SetColors(colors);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
		}
	}

	private static float RelativeStrength(float power, float maximum, float floorDb)
	{
		if (power <= 0f || maximum <= 0f)
		{
			return 0f;
		}
		float ratio = power / maximum;
		float value = (float)((Math.Log10((double)ratio) * 10.0 - (double)floorDb) / (double)(-floorDb));
		if (value < 0f)
		{
			return 0f;
		}
		if (value > 1f)
		{
			return 1f;
		}
		return value;
	}

	private static void ValidateGeometry(Vector3 center, float radius, float wavelength)
	{
		if (!Finite(center) || float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0f || float.IsNaN(wavelength) || float.IsInfinity(wavelength) || wavelength <= 0f)
		{
			throw new ArgumentException("Radar visual geometry needs finite center, positive Rbs and wavelength.");
		}
	}

	private static void ValidateBasis(Vector3 toRadar, Vector3 horizontal, Vector3 vertical)
	{
		if (!Finite(toRadar) || !Finite(horizontal) || !Finite(vertical) || Mathf.Abs(toRadar.magnitude - 1f) > 0.001f || Mathf.Abs(horizontal.magnitude - 1f) > 0.001f || Mathf.Abs(vertical.magnitude - 1f) > 0.001f || Mathf.Abs(Vector3.Dot(toRadar, horizontal)) > 0.001f || Mathf.Abs(Vector3.Dot(toRadar, vertical)) > 0.001f || (Vector3.Cross(horizontal, vertical) - toRadar).magnitude > 0.001f)
		{
			throw new ArgumentException("Radar visual direction basis is not orthonormal.");
		}
	}

	private static double Distance(Vector3 a, Vector3 b)
	{
		double dx = (double)(b.x - a.x);
		double dy = (double)(b.y - a.y);
		double dz = (double)(b.z - a.z);
		return Math.Sqrt(dx * dx + dy * dy + dz * dz);
	}

	private static double PositiveMod(double x, double modulus)
	{
		double r = Math.IEEERemainder(x, modulus);
		if (r < 0.0)
		{
			r += modulus;
		}
		return r;
	}

	private static bool Finite(float x)
	{
		if (!float.IsNaN(x))
		{
			return !float.IsInfinity(x);
		}
		return false;
	}

	private static bool Finite(Vector3 x)
	{
		if (!Finite(x.x) || !Finite(x.y))
		{
			return false;
		}
		return Finite(x.z);
	}

	private static float ElapsedMilliseconds(long start)
	{
		return (float)((double)(Stopwatch.GetTimestamp() - start) * 1000.0 / (double)Stopwatch.Frequency);
	}

	private void OnDestroy()
	{
		foreach (Mesh mesh in ownedMeshes)
		{
			Release(mesh);
		}
		foreach (Material material in ownedMaterials)
		{
			Release(material);
		}
	}

	private static void Release(Object value)
	{
		if (value == null)
		{
			return;
		}
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(value);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(value);
		}
	}

	public RadarPropagationVisuals()
	{
		displayed = new List<DisplayPath>();
		reflectionCandidates = new List<int>();
		vertices = new List<Vector3>();
		colors = new List<Color>();
		triangles = new List<int>();
		ownedMeshes = new List<Mesh>();
		ownedMaterials = new List<Material>();
		u = Vector3.forward;
		h = Vector3.right;
		v = Vector3.up;
		showRays = true;
		showWavefronts = true;
		playbackSpeed = 1f;
		secondaryAlphaScale = 0.65f;
		secondaryWidthScale = 0.75f;
		patternFloorDb = -40f;
		pathStride = 1;
		maxVisiblePaths = 3000;
		maxVisiblePulses = 600;
		wavefrontCount = 5;
		EffectivePathStride = 1;
		EffectivePulsePathStride = 1;
		EffectiveWavefrontPeriodStride = 1L;
	}

	static RadarPropagationVisuals()
	{
		IncidentRayColor = new Color(0.12f, 0.76f, 0.88f, 1f);
		IncidentPulseColor = new Color(0.58f, 0.91f, 0.98f, 1f);
		WeakReturnColor = new Color(0.5f, 0.12f, 0.13f, 1f);
		FrontColor = new Color(0.25f, 0.82f, 0.91f, 0.07f);
		PulseFaces = new int[6] { 0, 2, 4, 2, 1, 4 };
	}
}
