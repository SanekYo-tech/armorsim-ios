using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlateSelector : MonoBehaviour
{
	[Header("Цвет выделения")]
	public Color selectedColor;

	public Color anchorColor;

	private readonly List<ArmorPlate> selectedPlates;

	private Camera cam;

	public ArmorPlate SelectedPlate
	{
		get
		{
			if (selectedPlates.Count < 1)
			{
				return null;
			}
			return selectedPlates[selectedPlates.Count - 1];
		}
	}

	public IReadOnlyList<ArmorPlate> SelectedPlates => selectedPlates;

	private void Start()
	{
		cam = Camera.main;
	}

	private void Update()
	{
		if (!TouchInputHelper.PrimaryDown)
		{
			return;
		}
		if (TouchInputHelper.PointerOverUI)
		{
			return;
		}
		if (!PlateGizmo.IsBusy && !PlateGizmo.PointerOverAxis && !AimPick.BlocksSelection)
		{
			TrySelect(ControlBindings.Held(ControlModifier.SelectAdd));
		}
	}

	private void TrySelect(bool shift)
	{
		Ray ray = cam.ScreenPointToRay(TouchInputHelper.PrimaryPosition);
		if (!Physics.Raycast(ray, out RaycastHit hitInfo, 500f))
		{
			ClearSelection();
			return;
		}
		ArmorPlate armorPlate = hitInfo.collider.GetComponentInParent<ArmorPlate>();
		if (armorPlate != null)
		{
			if (!shift)
			{
				SelectSingle(armorPlate);
			}
			else
			{
				ToggleInGroup(armorPlate);
			}
		}
		else
		{
			ClearSelection();
		}
	}

	private void SelectSingle(ArmorPlate plate)
	{
		ClearSelection();
		AddToGroup(plate);
	}

	private void ToggleInGroup(ArmorPlate plate)
	{
		if (selectedPlates.Contains(plate))
		{
			RemoveFromGroup(plate);
		}
		else
		{
			AddToGroup(plate);
		}
	}

	private void AddToGroup(ArmorPlate plate)
	{
		if (selectedPlates.Contains(plate))
		{
			return;
		}
		selectedPlates.Add(plate);
		RefreshHighlights();
		Debug.Log(string.Format("Выделено плит: {0} | якорь: {1} ", selectedPlates.Count, plate.name) + string.Format("{0} {1}мм", plate.GetTarget().name, plate.thicknessMM));
	}

	private void RemoveFromGroup(ArmorPlate plate)
	{
		if (selectedPlates.Remove(plate))
		{
			plate.ApplyDisplayColor();
			RefreshHighlights();
		}
	}

	public void SetSelection(IList<ArmorPlate> newPlates)
	{
		ClearSelection();
		if (newPlates != null)
		{
			foreach (ArmorPlate plate in newPlates)
			{
				if (plate != null)
				{
					AddToGroup(plate);
				}
			}
		}
	}

	public void ClearSelection()
	{
		foreach (ArmorPlate plate in selectedPlates)
		{
			if (plate == null)
			{
				continue;
			}
			plate.ApplyDisplayColor();
		}
		selectedPlates.Clear();
	}

	private void RefreshHighlights()
	{
		for (int i = 0; i < selectedPlates.Count; i++)
		{
			ArmorPlate armorPlate = selectedPlates[i];
			if (armorPlate == null)
			{
				continue;
			}
			armorPlate.SetHighlight((i == selectedPlates.Count - 1) ? anchorColor : selectedColor);
		}
	}

	public PlateSelector()
	{
		selectedColor = new Color(0.3f, 0.6f, 1f);
		anchorColor = new Color(0.55f, 0.85f, 1f);
		selectedPlates = new List<ArmorPlate>();
	}
}
