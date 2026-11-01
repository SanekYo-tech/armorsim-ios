using System.Collections.Generic;
using UnityEngine;

public class EraFlyerFragments : MonoBehaviour
{
	public float threatDamagePerSweep = 8f;

	private float lastProgress;

	public float threatLoadPerSecond = 0.55f;

	public float driveLoadPerSecond = 0.65f;

	public float impactDamage = 0.5f;

	public float hitU = 0.5f;

	public float hitV;

	private EraFlyerFracture.Grid grid;

	private Transform[] cellTr;

	private Vector3[] cellHome;

	public const float CellEdgeJitter = 0.35f;

	private readonly HashSet<int> released = new HashSet<int>();

	private Vector3 velocityWU;

	private Collider ownPlateCollider;

	private ArmorPlate.ArmorType material;

	private float sinceLastCheck;

	private int brokenBonds;

	private Vector3 launchOrigin;

	private float sweepDistanceWU;

	private bool sweepDone;

	private static readonly List<GameObject> spawned = new List<GameObject>();

	public float DetachedFraction
	{
		get
		{
			if (grid == null)
			{
				return 0f;
			}
			if (grid.ny * grid.nx == 0)
			{
				return 0f;
			}
			EraFlyerFracture.BuildComponents(grid);
			int num = EraFlyerFracture.MainBodyRoot(grid);
			int num2 = 0;
			for (int i = 0; i < grid.ny * grid.nx; i++)
			{
				int num3 = i;
				while (grid.parent[num3] != num3)
				{
					grid.parent[num3] = grid.parent[grid.parent[num3]];
					num3 = grid.parent[num3];
				}
				if (num3 != num)
				{
					num2++;
				}
			}
			return (float)num2 / (float)(grid.ny * grid.nx);
		}
	}

	public int BrokenBonds => brokenBonds;

	public static void ClearAll()
	{
		foreach (GameObject item in spawned)
		{
			if (item != null)
			{
				UnityEngine.Object.Destroy(item);
			}
		}
		spawned.Clear();
	}

	public void Build(ArmorPlate.ArmorType type, Vector3 velocityWorld, Collider ownPlate)
	{
		velocityWU = velocityWorld;
		material = type;
		ownPlateCollider = ownPlate;
		Transform transform = base.transform;
		launchOrigin = transform.position;
		sweepDistanceWU = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
		grid = EraFlyerFracture.Create(16, 12, type);
		Renderer renderer = GetComponent<Renderer>();
		Color color = ((renderer == null) ? Color.gray : renderer.material.color);
		if (renderer != null)
		{
			renderer.enabled = false;
		}
		Collider collider = GetComponent<Collider>();
		if (collider != null)
		{
			collider.enabled = true;
		}
		cellTr = new Transform[grid.ny * grid.nx];
		cellHome = new Vector3[grid.ny * grid.nx];
		for (int i = 0; i < grid.ny; i++)
		{
			for (int j = 0; j < grid.nx; j++)
			{
				GameObject gameObject = new GameObject(string.Format("FlyerCell_{0}_{1}", j, i), typeof(MeshFilter), typeof(MeshRenderer));
				gameObject.transform.SetParent(transform, false);
				gameObject.transform.localScale = Vector3.one;
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				Mesh mesh = MeshShapes.PlateCell(j, i, grid.nx, grid.ny, CellEdgeJitter);
				gameObject.GetComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
				Material material2 = new Material(MaterialLibrary.Armor(type));
				meshRenderer.sharedMaterial = material2;
				meshRenderer.material.color = color;
				BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
				boxCollider.center = mesh.bounds.center;
				boxCollider.size = mesh.bounds.size;
				boxCollider.enabled = false;
				cellHome[i * grid.nx + j] = mesh.bounds.center;
				cellTr[i * grid.nx + j] = gameObject.transform;
			}
		}
	}

	private void Update()
	{
		if (grid == null)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		Transform transform = base.transform;
		Vector3 vector = transform.position - launchOrigin;
		float magnitude = vector.magnitude;
		float num = sweepDistanceWU;
		if (magnitude <= num)
		{
			if (num <= 1E-06f)
			{
				magnitude = 0f;
			}
			else
			{
				magnitude /= num;
			}
			Transform transform2 = base.transform;
			Vector3 vector2 = velocityWU;
			float magnitude2 = vector2.magnitude;
			Vector3 vector3 = ((magnitude2 <= 1E-05f) ? Vector3.zero : (velocityWU / magnitude2));
			Vector3 vector4 = transform2.TransformDirection(vector3);
			float num2 = Mathf.Clamp01(hitU - vector4.x * magnitude);
			float num3 = Mathf.Clamp01(hitV - vector4.y * magnitude);
			float num4 = magnitude - lastProgress;
			if (num4 <= 0f)
			{
				num4 = 0f;
			}
			lastProgress = magnitude;
			EraFlyerFracture.ApplyImpact(grid, num2, num3, num4 * threatDamagePerSweep);
		}
		else if (!sweepDone)
		{
			sweepDone = true;
			string[] array = new string[5]
			{
				"◈ Метаемая пластина [",
				ArmorPlate.DisplayName(material),
				"]: свип окончен ",
				string.Format("(прошла {0:F3} из {1:F3} мир.ед) — накопление урона остановлено. ", magnitude, sweepDistanceWU),
				"Дальше пластина летит как есть: разрушение — следствие ВСТРЕЧИ, а не возраста."
			};
			Debug.LogWarning(string.Concat(array));
		}
		float num5 = 0.05f;
		deltaTime += sinceLastCheck;
		sinceLastCheck = deltaTime;
		if (num5 <= sinceLastCheck)
		{
			sinceLastCheck = 0f;
			int num6 = EraFlyerFracture.BreakBonds(grid);
			if (0 < num6)
			{
				brokenBonds += num6;
				EraFlyerFracture.BuildComponents(grid);
				int num7 = EraFlyerFracture.MainBodyRoot(grid);
				for (int i = 0; i < grid.ny * grid.nx; i++)
				{
					if (released.Contains(i))
					{
						continue;
					}
					int num8 = i;
					while (grid.parent[num8] != num8)
					{
						grid.parent[num8] = grid.parent[grid.parent[num8]];
						num8 = grid.parent[num8];
					}
					if (num8 != num7)
					{
						Release(i);
					}
				}
				ApplyStrainPose();
				return;
			}
		}
	}

	private void OnCollisionEnter(Collision c)
	{
		if (grid == null)
		{
			return;
		}
		if (c.contactCount == 0)
		{
			return;
		}
		Transform transform = base.transform;
		ContactPoint contactPoint = c.GetContact(0);
		Vector3 vector = transform.InverseTransformPoint(contactPoint.point);
		float num = Mathf.Clamp01(vector.x + 0.5f);
		float num2 = Mathf.Clamp01(vector.y + 0.5f);
		Vector3 relativeVelocity = c.relativeVelocity;
		float num3 = relativeVelocity.magnitude / Mathf.Max(velocityWU.magnitude, 0.001f);
		num3 = Mathf.Clamp01(num3);
		EraFlyerFracture.ApplyImpact(grid, num, num2, num3 * impactDamage);
	}

	private void ApplyStrainPose()
	{
		if (cellTr == null)
		{
			return;
		}
		for (int i = 0; i < grid.ny; i++)
		{
			for (int j = 0; j < grid.nx; j++)
			{
				int num = i * grid.nx + j;
				if (released.Contains(num))
				{
					continue;
				}
				Transform transform = cellTr[num];
				if (transform == null)
				{
					continue;
				}
				float num2 = EraFlyerFracture.StrainOf(grid, j, i);
				if (num2 <= 0f)
				{
					continue;
				}
				Vector2 vector = EraFlyerFracture.PullDirection(grid, j, i);
				if (1E-09f <= vector.x * vector.x + vector.y * vector.y)
				{
					transform.localPosition = new Vector3(num2 * 0.5f * vector.x / (float)grid.nx, num2 * 0.5f * vector.y / (float)grid.ny, 0f);
					transform.localRotation = Quaternion.Euler(new Vector3((0f - vector.y) * 25f * num2 * Mathf.Deg2Rad, vector.x * 25f * num2 * Mathf.Deg2Rad, 0f));
				}
			}
		}
	}

	private void Release(int cellIndex)
	{
		Transform transform = cellTr[cellIndex];
		if (transform == null)
		{
			return;
		}
		if (released == null)
		{
			return;
		}
		released.Add(cellIndex);
		transform.SetParent(null, worldPositionStays: true);
		Collider collider = transform.GetComponent<Collider>();
		if (collider != null)
		{
			collider.enabled = true;
			collider.contactOffset = Fragment.ContactOffsetFor(Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) * 0.5f);
		}
		Rigidbody rigidbody = transform.gameObject.GetComponent<Rigidbody>();
		if (rigidbody != null)
		{
			rigidbody.useGravity = false;
			rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			Vector3 vector = transform.position - base.transform.position;
			Vector3 vector2 = ((vector.magnitude <= 1E-05f) ? Vector3.zero : (vector / vector.magnitude));
			float num = velocityWU.magnitude * 0.01f;
			rigidbody.linearVelocity = new Vector3(num * vector2.x + velocityWU.x, num * vector2.y + velocityWU.y, num * vector2.z + velocityWU.z);
			rigidbody.angularVelocity = UnityEngine.Random.onUnitSphere * 6f;
			rigidbody.maxDepenetrationVelocity = Mathf.Max(rigidbody.linearVelocity.magnitude, 0.001f);
		}
		if (collider != null && ownPlateCollider != null)
		{
			Physics.IgnoreCollision(collider, ownPlateCollider, true);
		}
		spawned.Add(transform.gameObject);
	}

	private void OnDestroy()
	{
		if (grid == null)
		{
			return;
		}
		string text = string.Format("◈ Метаемая пластина [{0}]: связей порвано {1}, ", ArmorPlate.DisplayName(material), brokenBonds);
		float num;
		if (grid.ny * grid.nx == 0)
		{
			num = 0f;
		}
		else
		{
			EraFlyerFracture.BuildComponents(grid);
			int num2 = EraFlyerFracture.MainBodyRoot(grid);
			int num3 = 0;
			for (int i = 0; i < grid.ny * grid.nx; i++)
			{
				int num4 = i;
				while (grid.parent[num4] != num4)
				{
					grid.parent[num4] = grid.parent[grid.parent[num4]];
					num4 = grid.parent[num4];
				}
				if (num4 != num2)
				{
					num3++;
				}
			}
			num = (float)num3 / (float)(grid.ny * grid.nx);
		}
		string text2 = string.Format("отделилось {0:F0}% площади ", num * 100f);
		string text3 = string.Format("(порог {0:F2} = вязкость K_Ic относительно RHA). ", grid.threshold);
		Debug.LogWarning(text + text2 + text3 + "Основное тело шло с V_p до конца. ⏸ ПРОБЕЛ: разрушение метаемой пластины ПОКАЗАНО, но в расчёт НЕ ВХОДИТ — скважность (ReactiveArmor) по-прежнему считает пластину целой. Связать их = правка валидированной модели, и она требует своего шага со своей приёмкой.");
	}
}
