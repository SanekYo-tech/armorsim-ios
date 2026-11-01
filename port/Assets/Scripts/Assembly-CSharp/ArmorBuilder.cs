using System.Collections.Generic;
using UnityEngine;

public class ArmorBuilder : MonoBehaviour
{
	[Header("Шаблон пластины")]
	public GameObject platePrefab;

	[Header("Куда ставить пластины")]
	public const float LineOfFireY = 1f;

	public Vector3 firstPlatePosition;

	public float gapM;

	private List<GameObject> plates;

	public ArmorBuilder()
	{
		firstPlatePosition = new Vector3(0f, LineOfFireY, SceneDefaults.ApproachM);
		gapM = 0.1f;
		plates = new List<GameObject>();
	}

	public Vector3 FirstPosition()
	{
		if (!SectionFrame.IsRoof)
		{
			return firstPlatePosition;
		}
		return SectionFrame.FirstPlateCentre(PrefabThicknessMM());
	}

	private float PrefabThicknessMM()
	{
		ArmorPlate p = platePrefab != null ? platePrefab.GetComponent<ArmorPlate>() : null;
		return p != null ? p.thicknessMM : ArmorPlate.DefaultThicknessMM;
	}

	private static float HalfDepth(GameObject go)
	{
		ArmorPlate p = go != null ? go.GetComponent<ArmorPlate>() : null;
		if (p != null)
		{
			return Mathf.Max(0.0001f, SceneScale.RodDiameterWorld(p.thicknessMM)) * 0.5f;
		}
		return SceneScale.RodDiameterWorld(ArmorPlate.DefaultThicknessMM) * 0.5f;
	}

	public void AddPlate()
	{
		if (platePrefab == null)
		{
			Debug.LogWarning("Не задан префаб пластины!");
			return;
		}
		GameObject go = SpawnAt(NextPosition());
		if (go == null)
		{
			return;
		}
		go.name = "Plate_" + plates.Count;
		ApplyModeDefaultPlan(go.GetComponent<ArmorPlate>());
		Debug.Log(string.Format("Добавлена пластина #{0} на Z={1:F2} ", plates.Count, go.transform.position.z) + string.Format("(зазор {0:F2} м до предыдущей)", gapM));
	}

	private Vector3 NextPosition()
	{
		Vector3 start = FirstPosition();
		GameObject last = plates.Count > 0 ? plates[plates.Count - 1] : null;
		if (last == null)
		{
			return start;
		}
		Vector3 axis = SectionFrame.StackAxis();
		Vector3 lastPos = last.transform.position;
		float startAlong = Vector3.Dot(start, axis);
		float along = HalfDepth(platePrefab) + Vector3.Dot(lastPos, axis) + HalfDepth(last) + gapM;
		return start - axis * startAlong + axis * along;
	}

	private static void ApplyModeDefaultPlan(ArmorPlate p)
	{
		if (p == null || AppFlow.ActiveMode != SimMode.Super || p.kind != ArmorPlate.ObjectKind.Plate)
		{
			return;
		}
		p.widthM = 0.2f;
		p.ApplyVisualThickness();
		p.RefreshComposition();
		if (p.kind == ArmorPlate.ObjectKind.Rod)
		{
			p.thicknessMM = 200f;
		}
		p.heightM = 0.2f;
		p.ApplyVisualThickness();
		p.RefreshComposition();
	}

	private GameObject SpawnAt(Vector3 pos)
	{
		if (platePrefab == null)
		{
			Debug.LogWarning("Не задан префаб пластины!");
			return null;
		}
		GameObject go = Object.Instantiate(platePrefab, pos, SectionFrame.PlateBase());
		plates.Add(go);
		return go;
	}

	public ArmorPlate AddObject(ArmorPlate.ObjectKind kind)
	{
		GameObject go = SpawnAt(NextPosition());
		if (go == null)
		{
			return null;
		}
		ArmorPlate p = go.GetComponent<ArmorPlate>();
		if (p != null)
		{
			p.SetKind(kind);
			go.name = string.Format("{0}_{1}", ArmorPlate.KindName(kind), plates.Count);
			ApplyModeDefaultPlan(p);
		}
		string dims = p != null ? string.Format("({0:F2}×{1:F2} м, {2:F1} мм)", p.widthM, p.heightM, p.thicknessMM) : "";
		Debug.Log("Добавлен объект: " + ArmorPlate.KindName(kind) + " " + dims);
		return p;
	}

	public ArmorPlate AddObjectAt(ArmorPlate.ObjectKind kind, Vector3 worldPos)
	{
		GameObject go = SpawnAt(worldPos);
		if (go == null)
		{
			return null;
		}
		ArmorPlate p = go.GetComponent<ArmorPlate>();
		if (p != null)
		{
			p.SetKind(kind);
			go.name = string.Format("{0}_{1}", ArmorPlate.KindName(kind), plates.Count);
			p.transform.position = worldPos;
		}
		return p;
	}

	public void ClearForLoad()
	{
		ArmorPlate[] all = Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		foreach (ArmorPlate p in all)
		{
			if (p != null)
			{
				Object.Destroy(p.gameObject);
			}
		}
		plates.Clear();
		Debug.Log(string.Format("Сцена очищена перед загрузкой: снято объектов {0}.", all.Length));
	}

	public void RemoveLastPlate()
	{
		if (plates.Count == 0)
		{
			return;
		}
		GameObject go = plates[plates.Count - 1];
		plates.RemoveAt(plates.Count - 1);
		Object.Destroy(go);
		Debug.Log(string.Format("Удалена последняя пластина. Осталось: {0}", plates.Count));
	}

	public ArmorPlate DuplicatePlate(ArmorPlate source, Vector3 worldOffset)
	{
		if (source == null)
		{
			return null;
		}
		if (source.kind == ArmorPlate.ObjectKind.Model)
		{
			SandboxModel model = SandboxModel.Duplicate(source.GetComponent<SandboxModel>(), worldOffset);
			if (model == null)
			{
				return null;
			}
			Debug.Log("Скопирована модель → " + model.name);
			return model.Plate;
		}
		GameObject go = Object.Instantiate(source.gameObject, source.transform.position + worldOffset, source.transform.rotation);
		go.name = "Plate_" + (plates.Count + 1);
		plates.Add(go);
		ArmorPlate p = go.GetComponent<ArmorPlate>();
		if (p != null)
		{
			p.ResetColor();
		}
		Debug.Log(string.Format("Скопирована пластина → {0} (всего {1})", go.name, plates.Count));
		return p;
	}

	public void RemovePlate(GameObject plate)
	{
		if (plate == null)
		{
			return;
		}
		plates.Remove(plate);
		Object.Destroy(plate);
		Debug.Log(string.Format("Удалена выбранная пластина. Осталось: {0}", plates.Count));
	}

	public void ClearAll()
	{
		foreach (GameObject go in plates)
		{
			if (go != null)
			{
				Object.Destroy(go);
			}
		}
		plates.Clear();
		foreach (SandboxModel model in SandboxModel.All())
		{
			if (model != null)
			{
				model.gameObject.SetActive(false);
				Object.Destroy(model.gameObject);
			}
		}
		Debug.Log("Все пластины удалены.");
	}
}
