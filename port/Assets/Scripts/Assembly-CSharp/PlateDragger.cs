using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlateDragger : MonoBehaviour
{
	public PlateSelector selector;

	private Camera cam;

	private bool dragging;

	private float dragDepth;

	private Vector3 grabOffset;

	private ArmorPlate anchorAtGrab;

	private Vector3 anchorStartPos;

	private readonly List<ArmorPlate> groupAtGrab = new List<ArmorPlate>();

	private readonly List<Vector3> groupStartPos = new List<Vector3>();

	private void Start()
	{
		cam = Camera.main;
	}

	private void Update()
	{
		if (selector == null)
		{
			return;
		}
		if (PlateGizmo.IsBusy)
		{
			dragging = false;
			return;
		}
		if (TouchInputHelper.PrimaryDown)
		{
			if (TouchInputHelper.PointerOverUI)
			{
				return;
			}
			if (PlateGizmo.PointerOverAxis)
			{
				return;
			}
			ArmorPlate armorPlate = selector.SelectedPlate;
			if (armorPlate == null)
			{
				return;
			}
			Ray ray = cam.ScreenPointToRay(TouchInputHelper.PrimaryPosition);
			if (Physics.Raycast(ray, out RaycastHit hitInfo, 500f))
			{
				ArmorPlate armorPlate2 = hitInfo.collider.GetComponentInParent<ArmorPlate>();
				if (armorPlate2 == armorPlate)
				{
					dragging = true;
					dragDepth = cam.WorldToScreenPoint(armorPlate.transform.position).z;
					grabOffset = armorPlate.transform.position - ScreenToWorld(TouchInputHelper.PrimaryPosition, dragDepth);
					anchorAtGrab = armorPlate;
					anchorStartPos = armorPlate.transform.position;
					groupAtGrab.Clear();
					groupStartPos.Clear();
					foreach (ArmorPlate plate in selector.SelectedPlates)
					{
						if (plate == null || plate == armorPlate)
						{
							continue;
						}
						groupAtGrab.Add(plate);
						groupStartPos.Add(plate.transform.position);
					}
				}
			}
		}
		if (dragging && TouchInputHelper.PrimaryHeld)
		{
			if (anchorAtGrab == null)
			{
				dragging = false;
				return;
			}
			Vector3 vector = grabOffset + ScreenToWorld(TouchInputHelper.PrimaryPosition, dragDepth);
			anchorAtGrab.transform.position = vector;
			Vector3 vector2 = vector - anchorStartPos;
			for (int i = 0; i < groupAtGrab.Count; i++)
			{
				ArmorPlate armorPlate3 = groupAtGrab[i];
				if (armorPlate3 == null)
				{
					continue;
				}
				armorPlate3.transform.position = groupStartPos[i] + vector2;
			}
		}
		if (TouchInputHelper.PrimaryUp)
		{
			dragging = false;
			anchorAtGrab = null;
			groupAtGrab.Clear();
			groupStartPos.Clear();
		}
	}

	private Vector3 ScreenToWorld(Vector3 screenPos, float depth)
	{
		screenPos.z = depth;
		return cam.ScreenToWorldPoint(screenPos);
	}
}
