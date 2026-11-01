using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

public class AimPick : MonoBehaviour
{
	private static bool _armed;

	private static int _consumedFrame = -1;

	[CompilerGenerated]
	private static Action m_OnArmedChanged;

	public static bool IsArmed => _armed;

	public static bool BlocksSelection => _armed || _consumedFrame == Time.frameCount;

	public static AimPick Instance { get; private set; }

	public static event Action OnArmedChanged
	{
		[CompilerGenerated]
		add
		{
			Action action = m_OnArmedChanged;
			Action action2;
			do
			{
				action2 = action;
				Action value2 = (Action)Delegate.Combine(action2, value);
				action = Interlocked.CompareExchange(ref m_OnArmedChanged, value2, action2);
			}
			while (action != action2);
		}
		[CompilerGenerated]
		remove
		{
			Action action = m_OnArmedChanged;
			Action action2;
			do
			{
				action2 = action;
				Action value2 = (Action)Delegate.Remove(action2, value);
				action = Interlocked.CompareExchange(ref m_OnArmedChanged, value2, action2);
			}
			while (action != action2);
		}
	}

	public static void Bootstrap()
	{
		_armed = false;
		_consumedFrame = -1;
		if (!AimShot.Available || UnityEngine.Object.FindFirstObjectByType<AimPick>() != null)
		{
			return;
		}
		GameObject go = new GameObject("HullAimTools");
		go.AddComponent<AimLine>();
		go.AddComponent<AimPick>();
	}

	private void Awake()
	{
		Instance = this;
	}

	public static void Arm()
	{
		if (!AimShot.Available || _armed)
		{
			return;
		}
		_armed = true;
		m_OnArmedChanged?.Invoke();
	}

	public static void Cancel()
	{
		if (!_armed)
		{
			return;
		}
		_armed = false;
		m_OnArmedChanged?.Invoke();
	}

	public static void Toggle()
	{
		if (_armed)
		{
			Cancel();
		}
		else
		{
			Arm();
		}
	}

	public static bool TryAimAtScreenPoint(Camera cam, Vector2 screen)
	{
		RaycastHit hit = default;
		_consumedFrame = Time.frameCount;
		if (cam == null)
		{
			Cancel();
			return false;
		}
		Ray ray = cam.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
		ArmorPlate plate = null;
		if (Physics.Raycast(ray, out hit, 500f))
		{
			plate = hit.collider.GetComponentInParent<ArmorPlate>();
		}
		Cancel();
		if (plate == null)
		{
			Debug.Log("[Прицел] Щелчок мимо брони — точка прицела не изменилась.");
			return false;
		}
		Vector3 point = hit.point;
		Vector3 aim = new Vector3(SceneScale.ToM(point.x), SceneScale.ToM(point.y), SceneScale.ToM(point.z));
		AimShot.SetAimPoint(aim);
		Debug.Log(string.Format("[Прицел] Точка по щелчку: «{0}», {1} м.", plate.name, aim));
		return true;
	}

	private void Update()
	{
		if (!_armed || !TouchInputHelper.PrimaryDown)
		{
			return;
		}
		if (TouchInputHelper.PointerOverUI || PlateGizmo.IsBusy || PlateGizmo.PointerOverAxis)
		{
			return;
		}
		Vector2 mouse = TouchInputHelper.PrimaryPosition;
		TryAimAtScreenPoint(Camera.main, mouse);
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
		_armed = false;
	}
}
