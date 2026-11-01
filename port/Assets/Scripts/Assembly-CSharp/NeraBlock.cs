using System.Collections.Generic;
using UnityEngine;

public class NeraBlock : MonoBehaviour
{
	private class Cell
	{
		public Renderer front;

		public Renderer rear;

		public Renderer elastomer;

		public Renderer frameBottom;

		public readonly List<Renderer> frame;

		public bool fired;

		public Cell()
		{
			frame = new List<Renderer>();
		}
	}

	public const string RootName = "NeraBlockParts";

	private const float PartLateralFit = 0.98f;

	private const float WallFrac = 0.06f;

	private Transform partsRoot;

	private readonly List<Cell> cells = new List<Cell>();

	private static readonly List<GameObject> spawnedParts = new List<GameObject>();

	public static void Refresh(ArmorPlate plate)
	{
		if (plate == null)
		{
			return;
		}
		NeraBlock block = plate.GetComponent<NeraBlock>();
		if (plate.IsNeraPlate)
		{
			if (block == null)
			{
				block = plate.gameObject.AddComponent<NeraBlock>();
			}
			block.Build(plate);
		}
		else if (block != null)
		{
			block.Clear();
		}
	}

	private void Clear()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
		{
			Transform child = transform.GetChild(i);
			if (child == null)
			{
				continue;
			}
			if (child.name != RootName)
			{
				continue;
			}
			child.gameObject.SetActive(false);
			child.SetParent(null);
			if (Application.isPlaying)
			{
				Object.Destroy(child.gameObject);
			}
			else
			{
				Object.DestroyImmediate(child.gameObject);
			}
		}
		partsRoot = null;
		cells.Clear();
		GetComponent<Renderer>().enabled = true;
	}

	private void Build(ArmorPlate plate)
	{
		Clear();
		float thickness = Mathf.Max(plate.thicknessMM, ArmorPlate.ModelScaleMin);
		float front = Mathf.Max(plate.neraFrontThicknessMM, 0.1f);
		float rear = Mathf.Max(plate.neraRearThicknessMM, 0.1f);
		float interlayer = Mathf.Max(plate.neraInterlayerMM, 0.1f);
		float sum = rear + front + interlayer;
		if (thickness < sum)
		{
			Debug.LogWarning(string.Format("НДЗ [{0}]: пакет {1:F1} + {2:F1} + {3:F1} = {4:F1} мм не влезает ", plate.name, front, interlayer, rear, sum) + string.Format("в раму {0:F1} мм — части ужаты. Это ПОКАЗАНО, не скрыто.", thickness));
		}
		partsRoot = new GameObject(RootName).transform;
		partsRoot.SetParent(base.transform, false);
		partsRoot.localPosition = Vector3.zero;
		partsRoot.localRotation = (Vector3.Dot(SectionFrame.PlateBase() * Vector3.forward, SectionFrame.StackAxis()) <= 0f) ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
		partsRoot.localScale = Vector3.one;
		Cell cell = new Cell();
		cells.Add(cell);
		if (plate.cassetteExpended)
		{
			cell.fired = true;
		}
		float mount = Mathf.Clamp(plate.neraMountAngleDeg, ArmorPlate.NeraMountAngleMinDeg, ArmorPlate.NeraMountAngleMaxDeg);
		float plateFit = PartLateralFit - WallFrac * 2f;
		float rise = Mathf.Cos(mount * Mathf.Deg2Rad) * plateFit;
		Quaternion tilt = Quaternion.Euler(plate.neraMountAngleDeg, 0f, 0f);
		float span = 1f - WallFrac * 2f;
		float rearN = rear * span / sum;
		float interlayerN = interlayer * span / sum;
		float frontN = front * span / sum;
		float packetHalf = span * 0.5f;
		float sideOffset = (PartLateralFit - WallFrac) * 0.5f;
		cell.frameBottom = MakePart("NERA_Frame_Bottom", new Vector3(PartLateralFit, PartLateralFit, WallFrac), Vector3.zero + new Vector3(0f, 0f, -(packetHalf + WallFrac * 0.5f)), FrameColor());
		cell.frame.Add(MakePart("NERA_Frame_SideX+", new Vector3(WallFrac, PartLateralFit, span), Vector3.zero + new Vector3(sideOffset, 0f, 0f), FrameColor()));
		cell.frame.Add(MakePart("NERA_Frame_SideX-", new Vector3(WallFrac, PartLateralFit, span), Vector3.zero + new Vector3(0f - sideOffset, 0f, 0f), FrameColor()));
		cell.frame.Add(MakePart("NERA_Frame_SideY+", new Vector3(plateFit, WallFrac, span), Vector3.zero + new Vector3(0f, sideOffset, 0f), FrameColor()));
		cell.frame.Add(MakePart("NERA_Frame_SideY-", new Vector3(plateFit, WallFrac, span), Vector3.zero + new Vector3(0f, 0f - sideOffset, 0f), FrameColor()));
		if (!cell.fired)
		{
			cell.rear = MakePart("NERA_RearPlate", new Vector3(plateFit, rise, rearN), Vector3.zero + new Vector3(0f, 0f, rearN * 0.5f - packetHalf), ArmorPlate.CanonicalColor(plate.neraRearMaterial), tilt);
			cell.elastomer = MakePart("NERA_Elastomer", new Vector3(plateFit, rise, interlayerN), Vector3.zero + new Vector3(0f, 0f, rearN - packetHalf + interlayerN * 0.5f), ElastomerColor(), tilt);
			cell.front = MakePart("NERA_FrontPlate", new Vector3(plateFit, rise, frontN), Vector3.zero + new Vector3(0f, 0f, rearN - packetHalf + interlayerN + frontN * 0.5f), ArmorPlate.CanonicalColor(plate.neraFrontMaterial), tilt);
		}
		Renderer plateRenderer = plate.GetComponent<Renderer>();
		if (plateRenderer != null)
		{
			plateRenderer.enabled = false;
		}
		NeraSpec spec = plate.GetNeraSpec();
		string[] parts = new string[10]
		{
			string.Format("НДЗ [{0}]: пакет {1:F1}/{2:F1}/{3:F1} мм ", plate.name, front, interlayer, rear),
			"(",
			ArmorPlate.DisplayName(plate.neraFrontMaterial),
			" / ",
			ArmorPlate.DisplayName(plate.neraRearMaterial),
			"), ",
			string.Format("наклон {0:F0}° (ВХОДИТ В РАСЧЁТ) | ", plate.neraMountAngleDeg),
			string.Format("V_перед = {0:F0} м/с, V_зад = {1:F0} м/с ", NeraArmor.FrontVelocityMS(spec), NeraArmor.RearVelocityMS(spec)),
			string.Format("— ВЫВЕДЕНЫ из импульса {0:F0} Н·с/м² (ASSUMED), отношение ", spec.elastomerImpulsePerArea),
			string.Format("{0:F3}", NeraArmor.VelocityRatio(spec))
		};
		Debug.Log(string.Concat(parts));
	}

	private Renderer MakePart(string name, Vector3 localScale, Vector3 localPos, Color color, Quaternion localRot = default(Quaternion))
	{
		GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
		part.name = name;
		part.transform.SetParent(partsRoot, false);
		part.transform.localPosition = localPos;
		part.transform.localRotation = localRot;
		part.transform.localScale = localScale;
		Renderer renderer = part.GetComponent<Renderer>();
		renderer.material = new Material(MaterialLibrary.Armor(ArmorPlate.ArmorType.RHA));
		renderer.material.color = color;
		return renderer;
	}

	private static float RimAmp(ArmorPlate.ArmorType material, float partThicknessMM, float threatDiameterMM)
	{
		if (CraterModel.RaggedRimApplies(material, partThicknessMM, threatDiameterMM))
		{
			return CraterModel.RaggedRimAmplitude(CraterModel.RimClassOf(material));
		}
		return 0f;
	}

	private static Color FrameColor()
	{
		return new Color(0.30f, 0.31f, 0.33f, 1f);
	}

	private static Color ElastomerColor()
	{
		return new Color(0.13f, 0.12f, 0.14f, 1f);
	}

	public static void Fire(ArmorPlate plate, Vector3 threatDir, float worldPerMeter, float timeDilation, Vector3? hitPoint = null, float holeRadiusWorld = 0f)
	{
		if (plate == null)
		{
			return;
		}
		if (!plate.IsNeraPlate)
		{
			return;
		}
		int index = (hitPoint.HasValue ? plate.CassetteIndexAt(hitPoint.Value) : 0);
		if (!plate.cassetteExpended)
		{
			NeraBlock block = plate.GetComponent<NeraBlock>();
			NeraSpec spec = plate.GetNeraSpec();
			float frontVelocity = NeraArmor.FrontVelocityMS(spec);
			float rearVelocity = NeraArmor.RearVelocityMS(spec);
			if (block != null && index >= 0)
			{
				Cell cell = block.cells[index];
				if (!cell.fired)
				{
					cell.fired = true;
					float pushFront = frontVelocity * worldPerMeter * NeraBulge.InteractionSeconds;
					float pushRear = rearVelocity * worldPerMeter * NeraBulge.InteractionSeconds;
					float holeMM = ((worldPerMeter > 1E-06f) ? ((holeRadiusWorld / worldPerMeter) * 2000f) : 0f);
					float rimFront = RimAmp(plate.neraFrontMaterial, plate.neraFrontThicknessMM, holeMM);
					float rimRear = RimAmp(plate.neraRearMaterial, plate.neraRearThicknessMM, holeMM);
					float rimFrame = RimAmp(ArmorPlate.ArmorType.RHA, plate.thicknessMM * WallFrac, holeMM);
					NeraBulge.Begin(cell.front, pushFront, pushFront, threatDir, timeDilation, hitPoint, holeRadiusWorld, rimFront);
					NeraBulge.Begin(cell.rear, 0f - pushRear, 0f - pushRear, threatDir, timeDilation, hitPoint, holeRadiusWorld, rimRear);
					NeraBulge.Begin(cell.elastomer, pushFront, 0f - pushRear, threatDir, timeDilation, hitPoint, holeRadiusWorld, 0f);
					NeraBulge.Begin(cell.frameBottom, 0f - pushRear, 0f - pushRear, threatDir, timeDilation, hitPoint, holeRadiusWorld, rimFrame);
					string[] parts = new string[9]
					{
						"◈ НДЗ [",
						plate.name,
						"]: пробиты ВСЕ части на линии выстрела — передняя, ",
						string.Format("прослойка, задняя, дно рамы; отверстие ⌀{0:F1} мм = ПОПЕРЕЧНИК УГРОЗЫ, ", holeMM),
						string.Format("кромка рваная на ±{0:F0} % (передняя) / ±{1:F0} % (задняя) ", rimFront * 100f, rimRear * 100f),
						"по классу материала из CraterModel. ",
						string.Format("Выгиб передней {0:F1} мм, задней {1:F1} мм ", pushFront / worldPerMeter * 1000f, pushRear / worldPerMeter * 1000f),
						string.Format("— ИСТИННЫЕ V·t, экранного множителя больше нет (VisualGain = {0:F1}). ", NeraBulge.VisualGain),
						"Пластины расходятся ОТ ЦЕНТРА ПАКЕТА, прослойка растягивается между ними, дно идёт за тыльной пластиной как её опора."
					};
					Debug.Log(string.Concat(parts));
				}
			}
			plate.SpendCassette(index);
			Debug.Log(string.Format("◈ НДЗ СРАБОТАЛА [{0}]: передняя {1:F1} мм ", plate.name, plate.neraFrontThicknessMM) + string.Format("ВЫПУЧИЛАСЬ навстречу угрозе (V = {0:F0} м/с), задняя {1:F1} мм — ", frontVelocity, plate.neraRearThicknessMM) + string.Format("от неё (V = {0:F0} м/с), отношение {1:F2}. ", rearVelocity, NeraArmor.VelocityRatio(spec)) + "Пластины ОСТАЮТСЯ в пакете: разлёт — механизм ДЗ, здесь взрываться нечему. Соседние блоки — ДРУГИЕ объекты и не тронуты.");
		}
		else
		{
			Debug.Log("◈ НДЗ [" + plate.name + "] не сработала: этот элемент уже отработал в этом выстреле");
		}
	}

	public static void ResetAll()
	{
		int rebuilt = 0;
		NeraBlock[] blocks = Object.FindObjectsByType<NeraBlock>(FindObjectsSortMode.None);
		for (int i = 0; i < blocks.Length; i++)
		{
			NeraBlock block = blocks[i];
			if (block == null)
			{
				continue;
			}
			ArmorPlate plate = block.GetComponent<ArmorPlate>();
			if (plate == null || !plate.IsNeraPlate)
			{
				block.Clear();
			}
			else
			{
				block.Build(plate);
				rebuilt++;
			}
		}
		if (rebuilt > 0)
		{
			Debug.Log(string.Format("◇ НДЗ: к новому выстрелу пересобрано блоков {0} ", rebuilt) + "(пробоины и выпучивание прошлого показа сняты).");
		}
	}

	public static void CollectCapBodies(List<Renderer> into)
	{
		NeraBlock[] blocks = Object.FindObjectsByType<NeraBlock>(FindObjectsSortMode.None);
		for (int i = 0; i < blocks.Length; i++)
		{
			NeraBlock block = blocks[i];
			if (block == null)
			{
				continue;
			}
			foreach (Cell cell in block.cells)
			{
				AddIfAny(into, cell.front);
				AddIfAny(into, cell.rear);
				AddIfAny(into, cell.elastomer);
				if (cell.frame != null)
				{
					foreach (Renderer r in cell.frame)
					{
						AddIfAny(into, r);
					}
				}
			}
		}
	}

	private static void AddIfAny(List<Renderer> into, Renderer r)
	{
		if (r != null)
		{
			into.Add(r);
		}
	}

	public static void ClearAll()
	{
		foreach (GameObject go in spawnedParts)
		{
			if (go != null)
			{
				Object.Destroy(go);
			}
		}
		spawnedParts.Clear();
	}
}
