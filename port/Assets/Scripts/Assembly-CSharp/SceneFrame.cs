using UnityEngine;

public class SceneFrame : MonoBehaviour
{
	private const int FrameDelayLateUpdates = 3;

	private const float LimitsPeriodS = 0.5f;

	private int framesLeft = -1;

	private float nextLimitsAt;

	public static SceneFrame Instance { get; private set; }

	public static void Bootstrap()
	{
		if (SectionFrame.IsRoof)
		{
			return;
		}
		if (Object.FindFirstObjectByType<SceneFrame>() != null)
		{
			return;
		}
		new GameObject("SceneFrame").AddComponent<SceneFrame>();
		RequestFrame();
	}

	public static void RequestFrame()
	{
		SceneFrame sceneFrame = Instance;
		if (sceneFrame == null)
		{
			sceneFrame = Object.FindFirstObjectByType<SceneFrame>();
		}
		if (sceneFrame == null)
		{
			Debug.LogWarning("[SceneFrame] Кадр запрошен, а объекта SceneFrame в сцене нет — SceneFrame.Bootstrap не звали (раздел «крыша»?). Кадр не выполнен.");
			return;
		}
		sceneFrame.framesLeft = FrameDelayLateUpdates;
	}

	private void Awake()
	{
		Instance = this;
	}

	private static void HalfAngles(Camera cam, out float vertRad, out float horzRad)
	{
		float num = SceneDefaults.DefaultFovDeg;
		if (cam != null && cam.fieldOfView > 0f)
		{
			num = cam.fieldOfView;
		}
		float num2 = ((cam != null) ? cam.aspect : 0f);
		if (!(num2 > 0f) || float.IsInfinity(num2))
		{
			num2 = 16f / 9f;
		}
		vertRad = num * 0.5f * Mathf.Deg2Rad;
		horzRad = Mathf.Atan(Mathf.Tan(vertRad) * num2);
	}

	public static float FitDistance(Bounds b, Camera cam)
	{
		HalfAngles(cam, out float vertRad, out float horzRad);
		float magnitude = Mathf.Max(b.extents.magnitude, SceneScale.MinRenderWorld);
		float num = Mathf.Min(vertRad, horzRad);
		return magnitude / (Mathf.Max(Mathf.Tan(num), 0.001f) * SceneDefaults.PlateFrameFill);
	}

	public static int GridHalfCellsFor(Bounds b, float cellM)
	{
		float num = Mathf.Max(cellM, 0.001f);
		float num2 = Mathf.Max(Mathf.Abs(b.min.x), Mathf.Abs(b.max.x));
		float num3 = Mathf.Max(Mathf.Abs(b.min.z), Mathf.Abs(b.max.z));
		return Mathf.CeilToInt(Mathf.Max(num2, num3) / num);
	}

	public static bool PointInFrustum(Vector3 p, Pose camPose, Camera cam)
	{
		HalfAngles(cam, out float vertRad, out float horzRad);
		float num = ((cam != null) ? cam.nearClipPlane : 0f);
		Vector3 vector = Quaternion.Inverse(camPose.rotation) * (p - camPose.position);
		if (vector.z <= num)
		{
			return false;
		}
		return Mathf.Abs(vector.x) <= Mathf.Tan(horzRad) * vector.z && Mathf.Abs(vector.y) <= Mathf.Tan(vertRad) * vector.z;
	}

	public static bool FitsFrame(Bounds b, Pose camPose, Camera cam)
	{
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector = new Vector3(((i & 1) == 0) ? b.min.x : b.max.x, ((i & 2) == 0) ? b.min.y : b.max.y, ((i & 4) == 0) ? b.min.z : b.max.z);
			if (!PointInFrustum(vector, camPose, cam))
			{
				return false;
			}
		}
		return true;
	}

	private static Camera CameraOf(OrbitCamera oc)
	{
		Camera camera = ((oc != null) ? oc.GetComponent<Camera>() : null);
		if (camera != null)
		{
			return camera;
		}
		return Camera.main;
	}

	private void RefreshLimits(Projectile pr, OrbitCamera oc)
	{
		Bounds sceneBounds = default(Bounds);
		bool flag = SceneBounds.TryScene(pr, out sceneBounds);
		if (oc != null)
		{
			oc.EnsureMaxDistance((flag ? FitDistance(sceneBounds, CameraOf(oc)) * 2f : 0f));
		}
		ScaleGrid scaleGrid = ScaleGrid.Instance;
		if (scaleGrid == null)
		{
			return;
		}
		int num = Mathf.Max(SceneDefaults.GridHalfCells, flag ? GridHalfCellsFor(sceneBounds, scaleGrid.cellM) : 0);
		if (scaleGrid.halfCells != num)
		{
			scaleGrid.halfCells = num;
		}
	}

	private static bool FitsToday(Bounds armour, Projectile pr, Pose pose, Camera cam)
	{
		for (int i = 0; i < 8; i++)
		{
			Vector3 vector = new Vector3(((i & 1) == 0) ? armour.min.x : armour.max.x, ((i & 2) == 0) ? armour.min.y : armour.max.y, ((i & 4) == 0) ? armour.min.z : armour.max.z);
			if (!PointInFrustum(vector, pose, cam))
			{
				return false;
			}
		}
		if (pr == null)
		{
			return true;
		}
		return PointInFrustum(pr.StartPositionWorld, pose, cam);
	}

	private void DoFrame(Projectile pr, OrbitCamera oc)
	{
		RefreshLimits(pr, oc);
		if (oc == null)
		{
			Debug.LogWarning("[SceneFrame] Кадр по сцене не выполнен: OrbitCamera в сцене нет.");
			return;
		}
		if (!SceneBounds.TryArmour(out Bounds armour))
		{
			Debug.Log("[SceneFrame] Кадр по сцене: брони нет — камера остаётся, где была.");
			return;
		}
		Camera camera = CameraOf(oc);
		Pose pose = oc.SteadyPose();
		if (FitsToday(armour, pr, pose, camera))
		{
			Debug.Log(string.Format("[SceneFrame] Сцена помещается в сегодняшний кадр (цель {0}, ", oc.PivotTarget) + string.Format("дистанция {0:F3}) — камера не тронута.", oc.DistanceTarget));
			return;
		}
		SceneBounds.TryScene(pr, out Bounds sceneBounds);
		oc.FrameSceneBounds(sceneBounds);
		Debug.Log(string.Format("[SceneFrame] Кадр по сцене: габарит {0} мир.ед., центр {1}, ", sceneBounds.size, sceneBounds.center) + string.Format("дистанция {0:F3} (предел зума {1:F2}).", oc.DistanceTarget, oc.maxDistance));
	}

	private void LateUpdate()
	{
		bool flag = framesLeft > 0 && --framesLeft == 0;
		float num = Time.unscaledTime;
		if (!flag && nextLimitsAt > num)
		{
			return;
		}
		nextLimitsAt = num + LimitsPeriodS;
		Projectile pr = Object.FindFirstObjectByType<Projectile>();
		OrbitCamera oc = OrbitCamera.Instance;
		if (flag)
		{
			DoFrame(pr, oc);
		}
		else
		{
			RefreshLimits(pr, oc);
		}
	}
}
