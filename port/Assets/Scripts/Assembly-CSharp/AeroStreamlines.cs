using System;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class AeroStreamlines : MonoBehaviour
{
	private struct Particle
	{
		public Vector3 position;

		public Vector3 velocity;

		public Vector3 normal;

		public bool sliding;

		public bool detached;

		public float age;

		public float wakeDistance;

		public float detachC;
	}

	private const int Trail = 96;

	private const int MaximumCount = 2560;

	private const float Speed = 1.8f;

	private const float Offset = 0.008f;

	private Particle[] particles;

	private Vector3[] vertices;

	private Color32[] trailColors;

	private bool colorsDirty;

	private AeroRaycaster rays;

	private Mesh mesh;

	private Material material;

	private float accumulator;

	private float visualTime;

	private int count;

	private Vector3 bodyCenter;

	private Vector3 bodySize;

	private bool wakeVortices;

	private bool linesVisible;

	private bool cloudsVisible;

	private AeroCloudDisplay clouds;

	public int SurfaceContacts { get; private set; }

	public int Detachments { get; private set; }

	public int WakeSteps { get; private set; }

	public int ParticleCount => count;

	public bool WakeVortices => wakeVortices;

	public bool LinesVisible => linesVisible;

	public bool CloudsVisible => cloudsVisible;

	public double LastStepMilliseconds { get; private set; }

	public float SimulatedSeconds => visualTime;

	public void SetDensity(int requested)
	{
		if (requested < 1 || requested > MaximumCount)
		{
			throw new System.ArgumentOutOfRangeException("requested");
		}
		if (count != requested)
		{
			count = requested;
			if (mesh != null)
			{
				RebuildMesh();
				ResetPaths();
			}
		}
	}

	public void SetWakeVortices(bool enabled)
	{
		wakeVortices = enabled;
	}

	public void SetLinesVisible(bool visible)
	{
		linesVisible = visible;
		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			component.enabled = visible && rays != null;
		}
	}

	public void SetCloudsVisible(bool visible)
	{
		cloudsVisible = visible;
		if (visible && clouds == null)
		{
			GameObject gameObject = new GameObject("Cloud flow");
			gameObject.transform.SetParent(base.transform);
			clouds = gameObject.AddComponent<AeroCloudDisplay>();
		}
		if (clouds != null)
		{
			clouds.SetVisible(visible && rays != null);
			if (visible && rays != null && vertices != null)
			{
				clouds.Publish(vertices, trailColors, count, Trail);
			}
		}
	}

	private void PublishClouds()
	{
		if (clouds != null && cloudsVisible && rays != null)
		{
			clouds.SetVisible(true);
			clouds.Publish(vertices, trailColors, count, Trail);
		}
	}

	public void Configure(AeroRaycaster raycaster)
	{
		rays = raycaster;
		if (rays != null)
		{
			bodyCenter = (rays.BoundsMin + rays.BoundsMax) * 0.5f;
			bodySize = rays.BoundsMax - rays.BoundsMin;
		}
		if (mesh == null)
		{
			mesh = new Mesh();
			mesh.name = "Aero qualitative paths";
			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.MarkDynamic();
			gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
			Shader shader = Resources.Load<Shader>("AeroPaths");
			if (shader == null)
			{
				throw new AeroException("aero.error.shader", Array.Empty<object>());
			}
			material = new Material(shader);
			gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
			RebuildMesh();
		}
		ResetPaths();
		enabled = rays != null;
		GetComponent<Renderer>().enabled = linesVisible && rays != null;
	}

	private void RebuildMesh()
	{
		particles = new Particle[count];
		vertices = new Vector3[count * Trail];
		trailColors = new Color32[vertices.Length];
		int[] array = new int[count * 190];
		int num = 0;
		for (int i = 0; i < count; i++)
		{
			for (int j = 1; j < Trail; j++)
			{
				array[num] = j - 1 + i * Trail;
				array[num + 1] = j + i * Trail;
				num += 2;
			}
		}
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.colors32 = trailColors;
		mesh.SetIndices(array, MeshTopology.Lines, 0, false, 0);
	}

	private void ResetPaths()
	{
		Detachments = 0;
		SurfaceContacts = 0;
		accumulator = 0f;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				Seed(i);
			}
		}
		if (mesh != null)
		{
			mesh.vertices = vertices;
			mesh.colors32 = trailColors;
			colorsDirty = false;
			mesh.RecalculateBounds();
		}
		PublishClouds();
	}

	private void Seed(int i)
	{
		float num = Mathf.Max(bodySize.x + 0.8f, 1f);
		float num2 = Mathf.Max(bodySize.y + 0.8f, 1f);
		int num3 = Mathf.Clamp((int)Math.Ceiling(Mathf.Sqrt((float)count * num / num2)), 1, count);
		int num4 = (int)Math.Ceiling((float)count / (float)num3);
		Vector3 point = new Vector3((((float)(i % num3) + 0.5f) / (float)num3 - 0.5f) * num + bodyCenter.x, (((float)(i / num3) + 0.5f) / (float)num4 - 0.5f) * num2 + bodyCenter.y, bodyCenter.z - bodySize.z * 0.5f - Speed - (float)(i % 4) * 0.15f);
		particles[i] = new Particle
		{
			position = point,
			velocity = Vector3.forward * Speed
		};
		bool flag = rays != null && rays.FirstHit(point, Vector3.forward, bodySize.z + 6f, out AeroRayHit hit);
		float num5 = Mathf.Clamp(Mathf.Sqrt(320f / (float)count), 0.5f, 1.2f) * (flag ? 0.3f : 0.055f);
		for (int j = 0; j < Trail; j++)
		{
			vertices[i * Trail + j] = point;
			trailColors[i * Trail + j] = new Color32((byte)Mathf.RoundToInt(58.65f), (byte)Mathf.RoundToInt(89.25f), (byte)Mathf.RoundToInt(99.45f), (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.Lerp(0.2f, 1f, (float)j / 95f) * num5) * 255f));
		}
		colorsDirty = true;
	}

	public void Clear()
	{
		rays = null;
		base.enabled = false;
		if (clouds != null)
		{
			clouds.Clear();
			Renderer component = GetComponent<Renderer>();
			if (component != null)
			{
				component.enabled = false;
			}
		}
	}

	private void Update()
	{
		if (rays == null)
		{
			return;
		}
		long timestamp = Stopwatch.GetTimestamp();
		accumulator += Mathf.Min(Time.deltaTime, 0.1f);
		float num = 0.0333333351f;
		while (num <= accumulator)
		{
			accumulator -= num;
			visualTime += num;
			for (int i = 0; i < count; i++)
			{
				Step(i, num);
			}
		}
		mesh.vertices = vertices;
		if (colorsDirty)
		{
			mesh.colors32 = trailColors;
			colorsDirty = false;
		}
		mesh.RecalculateBounds();
		PublishClouds();
		LastStepMilliseconds = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private Vector3 TangentVelocity(Vector3 point, Vector3 normal)
	{
		Vector3 vector = Vector3.forward * Speed;
		float num = normal.x * normal.x + normal.y * normal.y + normal.z * normal.z;
		if (num >= Mathf.Epsilon)
		{
			float num2 = (normal.x * vector.x + normal.y * vector.y + normal.z * vector.z) / num;
			vector = new Vector3(vector.x - normal.x * num2, vector.y - normal.y * num2, vector.z - normal.z * num2);
		}
		if (vector.x * vector.x + vector.y * vector.y + vector.z * vector.z > 1E-08f)
		{
			return vector;
		}
		vector = point - bodyCenter;
		num = normal.x * normal.x + normal.y * normal.y + normal.z * normal.z;
		if (num >= Mathf.Epsilon)
		{
			float num3 = (normal.x * vector.x + normal.y * vector.y + normal.z * vector.z) / num;
			vector = new Vector3(vector.x - normal.x * num3, vector.y - normal.y * num3, vector.z - normal.z * num3);
		}
		if (vector.sqrMagnitude < 1E-08f)
		{
			vector = Vector3.Cross(normal, Vector3.right);
		}
		if (vector.sqrMagnitude < 1E-08f)
		{
			vector = Vector3.Cross(normal, Vector3.up);
		}
		float magnitude = vector.magnitude;
		if (magnitude <= 1E-05f)
		{
			return Vector3.zero;
		}
		return vector / magnitude * 0.9f;
	}

	private void Detach(ref Particle p)
	{
		p.sliding = false;
		p.detached = true;
		p.wakeDistance = 0f;
		Detachments++;
	}

	private void Step(int i, float dt)
	{
		Particle particle = particles[i];
		float age = particle.age + dt;
		Vector3 vector = Vector3.forward * Speed;
		if (particle.detached)
		{
			particle.velocity += (vector - particle.velocity) * Mathf.Clamp01(1f - Mathf.Exp(dt * -1.4f));
		}
		Vector3 vector2 = particle.velocity * dt;
		if (particle.detached)
		{
			float num = Mathf.Exp(0f - particle.wakeDistance * 0.7f) * particle.detachC * 0.1f;
			vector2 += new Vector3(Mathf.Sin((float)i + age * 8f), Mathf.Sin((float)i * 0.7f + age * 6f), 0f) * num * dt;
			if (wakeVortices)
			{
				Vector3 vector3 = AeroWake.Velocity(particle.position, visualTime, 1f, bodyCenter, bodySize);
				vector2 += vector3 * dt;
				if (vector3.sqrMagnitude > 0.01f)
				{
					WakeSteps++;
				}
			}
			particle.wakeDistance += vector2.magnitude;
		}
		float num2 = vector2.magnitude;
		if (num2 > 1E-07f && rays.FirstHit(particle.position, vector2 / num2, num2, out AeroRayHit hit))
		{
			particle.position = hit.Point + hit.Normal * Offset;
			particle.normal = hit.Normal;
			particle.velocity = TangentVelocity(particle.position, hit.Normal);
			particle.sliding = true;
			particle.detached = false;
			particle.detachC = Mathf.Clamp01(0f - Vector3.Dot(hit.Normal, Vector3.forward));
			SurfaceContacts++;
		}
		else
		{
			particle.position += vector2;
			if (particle.sliding)
			{
				if (rays.FirstHit(particle.position + particle.normal * Offset, -particle.normal, 0.04f, out AeroRayHit hit2) && Vector3.Dot(hit2.Normal, Vector3.forward) < 0f)
				{
					particle.position = hit2.Point + hit2.Normal * Offset;
					particle.normal = hit2.Normal;
					particle.velocity = TangentVelocity(particle.position, hit2.Normal);
					particle.detachC = Mathf.Clamp01(0f - Vector3.Dot(hit2.Normal, Vector3.forward));
				}
				else
				{
					Detach(ref particle);
				}
			}
		}
		particle.age = age;
		if (particle.position.z - bodyCenter.z > bodySize.z * 0.5f + 9f || particle.position.z - bodyCenter.z < 0f - bodySize.z * 0.5f - 3f || Mathf.Abs(particle.position.x - bodyCenter.x) > bodySize.x * 0.5f + 4f || Mathf.Abs(particle.position.y - bodyCenter.y) > bodySize.y * 0.5f + 4f || age > 24f)
		{
			Seed(i);
			return;
		}
		particles[i] = particle;
		Array.Copy(vertices, i * Trail, vertices, i * Trail + 1, Trail - 1);
		vertices[i * Trail + Trail - 1] = particle.position;
	}

	private void OnDestroy()
	{
		if (mesh != null)
		{
			Object.Destroy(mesh);
		}
		if (material != null)
		{
			Object.Destroy(material);
		}
	}
}
