using System.Collections.Generic;
using UnityEngine;

public class EraCassette : MonoBehaviour
{
	private class Cell
	{
		public Renderer flyer;

		public Renderer back;

		public Renderer explosive;

		public Renderer lid;

		public Renderer mount;

		public readonly List<Renderer> housing;

		public readonly List<Renderer> mounts;

		public bool fired;

		public Cell()
		{
			housing = new List<Renderer>();
			mounts = new List<Renderer>();
		}
	}

	public const string RootName = "EraCassetteParts";

	private const float FlyerInclineDeg = 18f;

	private const float PartLateralFit = 0.98f;

	private const float WallFrac = 0.06f;

	private const float MountFrac = 0.04f;

	private const float HousingVpFraction = 0.35f;

	private Transform partsRoot;

	private readonly List<Cell> cells = new List<Cell>();

	private static readonly List<GameObject> spawnedParts = new List<GameObject>();

	public static void Refresh(ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		EraCassette eraCassette = plate.GetComponent<EraCassette>();
		if (!plate.IsEraPlate)
		{
			if (eraCassette == null)
			{
				return;
			}
			eraCassette.Clear();
			return;
		}
		if (eraCassette == null)
		{
			eraCassette = plate.gameObject.AddComponent<EraCassette>();
		}
		eraCassette.Build(plate);
	}

	private void Clear()
	{
		if (partsRoot != null)
		{
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(partsRoot.gameObject);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(partsRoot.gameObject);
			}
		}
		partsRoot = null;
		cells.Clear();
		Renderer component = GetComponent<Renderer>();
		if (component != null)
		{
			component.enabled = true;
		}
	}

	private void Build(ArmorPlate plate)
	{
		Clear();
		if (plate == null)
		{
			return;
		}
		float num = Mathf.Max(plate.thicknessMM, 0.001f);
		float num2 = plate.eraPlateThicknessMM;
		float num3 = plate.eraInterlayerGapMM;
		float num4 = num2 + num2 + num3;
		if (num4 > num)
		{
			Debug.LogWarning(string.Concat(string.Format("ДЗ [{0}]: набивка 2·{1:F1} + {2:F1} = {3:F1} мм не влезает ", plate.name, num2, num3, num4), string.Format("в короб {0:F1} мм — части ужаты ×{1:F2}. Это ПОКАЗАНО, не скрыто: ", num, num / num4), "увеличьте толщину плиты или уменьшите t_p/зазор."));
		}
		GameObject gameObject = new GameObject("EraCassetteParts");
		partsRoot = gameObject.transform;
		partsRoot.SetParent(base.transform, worldPositionStays: false);
		partsRoot.localPosition = Vector3.zero;
		partsRoot.localRotation = ((0f < Vector3.Dot(SectionFrame.PlateBase() * Vector3.forward, SectionFrame.StackAxis())) ? Quaternion.Euler(0f, 270f, 0f) : Quaternion.identity);
		partsRoot.localScale = Vector3.one;
		float num5 = num2 * 0.88f / num4;
		float num6 = num3 * 0.88f / num4;
		float num7 = num2 * 0.88f / num4;
		float num8 = num5 * 0.5f - 0.44f;
		float num9 = (num5 - 0.44f) + num6 * 0.5f;
		float num10 = (num5 - 0.44f) + num6 + num7 * 0.5f;
		float num11 = Mathf.Sin(FlyerInclineDeg * Mathf.Deg2Rad);
		int num12 = 0;
		for (int i = 0; i < 1; i++)
		{
			for (int j = 0; j < 1; j++)
			{
				Cell cell = new Cell();
				cells.Add(cell);
				if (!plate.cassetteExpended)
				{
					cell.housing.Add(MakePart(string.Format("ERA{0}_Housing_Bottom", num12), new Vector3(0.98f, 0.98f, 0.06f), new Vector3(j, i, -0.47f), HousingColor(plate)));
					cell.housing.Add(MakePart(string.Format("ERA{0}_Housing_SideX+", num12), new Vector3(0.06f, 0.98f, 0.88f), new Vector3(j + 0.46f, i, 0f), HousingColor(plate)));
					cell.housing.Add(MakePart(string.Format("ERA{0}_Housing_SideX-", num12), new Vector3(0.06f, 0.98f, 0.88f), new Vector3(j - 0.46f, i, 0f), HousingColor(plate)));
					cell.housing.Add(MakePart(string.Format("ERA{0}_Housing_SideY+", num12), new Vector3(0.86f, 0.06f, 0.88f), new Vector3(j, i + 0.46f, 0f), HousingColor(plate)));
					cell.housing.Add(MakePart(string.Format("ERA{0}_Housing_SideY-", num12), new Vector3(0.86f, 0.06f, 0.88f), new Vector3(j, i - 0.46f, 0f), HousingColor(plate)));
					cell.lid = MakePart(string.Format("ERA{0}_Housing_Lid", num12), new Vector3(0.98f, 0.98f, 0.06f), new Vector3(j, i, 0.47f), HousingColor(plate));
					cell.back = MakePart(string.Format("ERA{0}_BackPlate", num12), new Vector3(0.86f, 0.86f, num5), new Vector3(j, i, num8), PlateColor(plate));
					cell.explosive = MakePart(string.Format("ERA{0}_Explosive", num12), new Vector3(0.86f, 0.86f, num6), new Vector3(j, i, num9), ExplosiveColor(plate));
					cell.flyer = MakePart(string.Format("ERA{0}_FlyerPlate", num12), new Vector3(0.86f, num11 * 0.86f, num7), new Vector3(j, i, num10), PlateColor(plate), Quaternion.Euler(FlyerInclineDeg, 0f, 0f));
					for (int k = -1; k < 2; k += 2)
					{
						for (int l = -1; l < 2; l += 2)
						{
							Renderer renderer = MakePart(string.Format("ERA{0}_Mount_{1}{2}", num12, (k > 0) ? "X+" : "X-", (l > 0) ? "Y+" : "Y-"), new Vector3(0.1764f, 0.1764f, 0.1f), new Vector3(j + k * 0.4018f, i + l * 0.4018f, -0.55f), HousingColor(plate));
							cell.mounts.Add(renderer);
							if (cell.mount == null)
							{
								cell.mount = renderer;
							}
						}
					}
				}
				else
				{
					cell.fired = true;
				}
				num12++;
			}
		}
		Renderer component = plate.GetComponent<Renderer>();
		if (component != null)
		{
			component.enabled = false;
		}
		string[] array = new string[5];
		array[0] = string.Format("ДЗ [{0}]: массив {1}×{2} = {3} кассет ", ArmorPlate.DisplayName(plate.armorType), 1, 1, 1);
		array[1] = string.Format("по W = {0:F0} мм на плите {1:F2}×{2:F2} м | ", plate.eraCassetteExtentMM, plate.widthM, plate.heightM);
		array[2] = string.Format("метаемая/опорная {0:F1} мм, ВВ {1:F1} мм, V_p = {2:F0} м/с | ", num2, num3, plate.eraPlateVelocityMS);
		array[3] = string.Format("наклон метаемой {0:F0}° — ВИЗУАЛЬНЫЙ, в расчёт не входит | ", FlyerInclineDeg);
		array[4] = "собственный меш плиты СКРЫТ: ДЗ это массив кассет, а не преграда";
		Debug.Log(string.Concat(array));
	}

	private Renderer MakePart(string name, Vector3 localScale, Vector3 localPos, Color color, Quaternion localRot = default(Quaternion))
	{
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
		gameObject.name = name;
		gameObject.transform.SetParent(partsRoot, false);
		gameObject.transform.localPosition = localPos;
		gameObject.transform.localRotation = localRot;
		gameObject.transform.localScale = localScale;
		MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
		Material material = new Material(MaterialLibrary.Armor(ArmorPlate.ArmorType.RHA));
		meshRenderer.sharedMaterial = material;
		material.color = color;
		return meshRenderer;
	}

	private static Color PlateColor(ArmorPlate p)
	{
		return new Color(0.55f, 0.55f, 0.6f, 1f);
	}

	private static Color HousingColor(ArmorPlate p)
	{
		return new Color(0.34f, 0.36f, 0.34f, 1f);
	}

	private static Color ExplosiveColor(ArmorPlate p)
	{
		return new Color(0.2f, 0.19f, 0.18f, 1f);
	}

	public static void Detonate(ArmorPlate plate, Vector3 threatDir, float worldPerMeter, float timeDilation, bool firedByPrecursor = false, Vector3? hitPoint = null)
	{
		if (plate == null)
		{
			return;
		}
		if (!plate.IsEraPlate)
		{
			return;
		}
		if (hitPoint.HasValue)
		{
			plate.CassetteIndexAt(hitPoint.Value);
		}
		if (!plate.cassetteExpended)
		{
			if (firedByPrecursor)
			{
				Debug.Log("◈ ДЗ [" + plate.name + "] сработала ОТ ПРЕДЗАРЯДА тандема — к приходу основного заряда инертна");
			}
		}
		else if (!firedByPrecursor)
		{
			Debug.Log("◈ ДЗ [" + plate.name + "] не сработала: кассета уже израсходована в этом выстреле");
			return;
		}
		else
		{
			Debug.Log("◈ ДЗ [" + plate.name + "] сработала ОТ ПРЕДЗАРЯДА тандема — к приходу основного заряда инертна");
		}
		EraCassette component = plate.GetComponent<EraCassette>();
		if (0f < Vector3.Dot(threatDir, plate.transform.forward))
		{
			threatDir = -threatDir;
		}
		float num = plate.eraPlateVelocityMS;
		Collider component2 = plate.GetComponent<Collider>();
		int num2 = hitPoint.HasValue ? plate.CassetteIndexAt(hitPoint.Value) : 0;
		if (component != null && num2 >= 0)
		{
			List<Cell> list = component.cells;
			Cell cell = (num2 < list.Count) ? list[num2] : null;
			if (cell != null && !cell.fired)
			{
				cell.fired = true;
				if (cell.explosive != null)
				{
					cell.explosive.enabled = false;
				}
				LaunchPart(cell.lid, threatDir, num * 0.6f, worldPerMeter, timeDilation, 120f, dynamic: true, component2);
				LaunchPart(cell.flyer, threatDir, num, worldPerMeter, timeDilation, 0f, dynamic: true, component2);
				if (EraFlyerFracture.EnabledInPlayback)
				{
					AttachFragmentation(cell.lid, plate.armorType, component2, hitPoint, threatDir * (num * 0.6f));
					AttachFragmentation(cell.flyer, plate.armorType, component2, hitPoint, threatDir * num);
				}
				LaunchPart(cell.back, -threatDir, num, worldPerMeter, timeDilation, 0f, dynamic: true, component2);
				Vector3 vector = ((cell.mount != null) ? new Vector3(cell.mount.transform.localPosition.x, cell.mount.transform.localPosition.y, 0f) : Vector3.zero);
				Vector3 vector2 = default(Vector3);
				bool flag = false;
				if (hitPoint.HasValue && component != null && component.partsRoot != null)
				{
					vector2 = component.partsRoot.InverseTransformPoint(hitPoint.Value);
					flag = true;
				}
				int num3 = 0;
				int num4 = 0;
				for (int num5 = cell.housing.Count - 1; num5 >= 0; num5--)
				{
					Renderer renderer = cell.housing[num5];
					if (renderer == null)
					{
						cell.housing.RemoveAt(num5);
						continue;
					}
					bool flag2 = false;
					if (flag)
					{
						Vector3 localPosition = renderer.transform.localPosition;
						Vector3 localScale = renderer.transform.localScale;
						flag2 = Mathf.Abs(vector2.x - localPosition.x) <= localScale.x * 0.5f && Mathf.Abs(vector2.y - localPosition.y) <= localScale.y * 0.5f;
						if (!flag2)
						{
							num4++;
							continue;
						}
					}
					Transform transform = (component != null && component.partsRoot != null) ? component.partsRoot : plate.transform;
					Vector3 vector3 = renderer.transform.localPosition - vector;
					Vector3 vector4;
					if (vector3.sqrMagnitude <= 1E-08f)
					{
						vector4 = -threatDir;
					}
					else
					{
						float magnitude = vector3.magnitude;
						vector4 = transform.TransformDirection((magnitude <= 1E-05f) ? Vector3.zero : (vector3 / magnitude));
					}
					LaunchPart(renderer, vector4, num * 0.35f, worldPerMeter, timeDilation, 90f, dynamic: true, component2);
					cell.housing.RemoveAt(num5);
					num3++;
				}
				Debug.Log(string.Format("  ◈ корпус: сорвано {0} част(и), осталось на кронштейнах {1} — ", num3, num4) + (flag ? "ломается только задетое угрозой (§2)" : "⚠ точки попадания нет, снесён весь короб (совместимость со старым вызовом)"));
			}
		}
		if (hitPoint.HasValue)
		{
			plate.SpendCassette(plate.CassetteIndexAt(hitPoint.Value));
		}
		else
		{
			plate.cassetteExpended = true;
		}
		Renderer component3 = plate.GetComponent<Renderer>();
		if (component3 != null)
		{
			component3.enabled = false;
		}
		string[] array = new string[5];
		array[0] = "◈ ДЗ СРАБОТАЛА [";
		array[1] = ArmorPlate.DisplayName(plate.armorType);
		array[2] = "]: крышка сорвана, ";
		array[3] = string.Format("метаемая пластина {0:F1} мм пошла НАВСТРЕЧУ угрозе ", plate.eraPlateThicknessMM);
		array[4] = string.Format("на {0:F0} м/с, опорная — от неё; корпус остался на месте, осколков нет", num);
		Debug.Log(string.Concat(array));
	}

	private static void AttachFragmentation(Renderer part, ArmorPlate.ArmorType material, Collider ownPlate, Vector3? hitPoint, Vector3 fallbackVelocity)
	{
		if (part == null)
		{
			return;
		}
		EraFlyerFragments eraFlyerFragments = part.gameObject.AddComponent<EraFlyerFragments>();
		EraPlateMotion component = part.GetComponent<EraPlateMotion>();
		Vector3 vector = ((component == null) ? fallbackVelocity : component.VelocityWorld);
		if (!hitPoint.HasValue)
		{
			eraFlyerFragments.Build(material, vector, ownPlate);
			return;
		}
		Transform transform = part.transform;
		Vector3 vector2 = transform.InverseTransformPoint(hitPoint.Value);
		eraFlyerFragments.hitU = Mathf.Clamp01(vector2.x + 0.5f);
		eraFlyerFragments.hitV = Mathf.Clamp01(vector2.y + 0.5f);
		eraFlyerFragments.Build(material, vector, ownPlate);
	}

	private static void LaunchPart(Renderer part, Vector3 dir, float vpMps, float worldPerMeter, float timeDilation, float spinDegS, bool dynamic = false, Collider ownPlate = null)
	{
		if (part == null)
		{
			return;
		}
		part.transform.SetParent(null, worldPositionStays: true);
		EraPlateMotion eraPlateMotion = part.gameObject.AddComponent<EraPlateMotion>();
		eraPlateMotion.spinDegS = spinDegS;
		eraPlateMotion.dynamicBody = dynamic;
		eraPlateMotion.ownPlateCollider = ownPlate;
		float num = Mathf.Max(timeDilation, 1E-06f);
		Vector3 vector = dir * (vpMps * worldPerMeter / num);
		eraPlateMotion.Launch(vector, dir);
		spawnedParts.Add(part.gameObject);
	}

	public static void ClearAll()
	{
		foreach (GameObject item in spawnedParts)
		{
			if (item != null)
			{
				UnityEngine.Object.Destroy(item);
			}
		}
		spawnedParts.Clear();
	}
}
