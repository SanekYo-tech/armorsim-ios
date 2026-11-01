using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public static class AimShot
{
	private struct LineSnap
	{
		public bool valid;

		public Vector3 pos;

		public Quaternion rot;

		public Vector3 dir;
	}

	private const string ModeKey = "armorsim.aim_mode";

	private const string AzKey = "armorsim.aim_az";

	private const string ElKey = "armorsim.aim_el";

	private const string AimXKey = "armorsim.aim_x";

	private const string AimYKey = "armorsim.aim_y";

	private const string AimZKey = "armorsim.aim_z";

	public const float AzimuthMinDeg = -180f;

	public const float AzimuthMaxDeg = 180f;

	public const float ElevationMinDeg = -15f;

	public const float ElevationMaxDeg = 90f;

	[CompilerGenerated]
	private static Action m_OnChanged;

	private static AimMode _mode;

	private static float _az;

	private static float _el;

	private static Vector3 _aimM;

	private static bool _loaded;

	private static LineSnap _line;

	private static string _lastInside;

	public static bool SabotageWorldAxis;

	public static AimMode Mode
	{
		get
		{
			Load();
			return _mode;
		}
	}

	public static float AzimuthDeg
	{
		get
		{
			Load();
			return _az;
		}
	}

	public static float ElevationDeg
	{
		get
		{
			Load();
			return _el;
		}
	}

	public static Vector3 AimPointM
	{
		get
		{
			Load();
			return _aimM;
		}
	}

	public static bool Available => !SectionFrame.IsRoof && (AppFlow.ActiveMode == SimMode.Normal || AppFlow.ActiveMode == SimMode.Advanced);

	public static bool IsFree => Available && Mode == AimMode.Free;

	public static Vector3 Direction => DirectionOf(AzimuthDeg, ElevationDeg);

	public static Vector3 AimPointWorld => ToWorld(AimPointM);

	public static Vector3 FreeStartWorld => StartWorldOf(AimPointWorld, Direction);

	public static event Action OnChanged
	{
		[CompilerGenerated]
		add
		{
			Action action = m_OnChanged;
			Action action2;
			do
			{
				action2 = action;
				Action value2 = (Action)Delegate.Combine(action2, value);
				action = Interlocked.CompareExchange(ref m_OnChanged, value2, action2);
			}
			while (action != action2);
		}
		[CompilerGenerated]
		remove
		{
			Action action = m_OnChanged;
			Action action2;
			do
			{
				action2 = action;
				Action value2 = (Action)Delegate.Remove(action2, value);
				action = Interlocked.CompareExchange(ref m_OnChanged, value2, action2);
			}
			while (action != action2);
		}
	}

	private static void Load()
	{
		if (_loaded)
		{
			return;
		}
		_loaded = true;
		_mode = ((PlayerPrefs.GetInt(ModeKey, 0) == 1) ? AimMode.Free : AimMode.Line);
		_az = WrapAzimuth(Finite(PlayerPrefs.GetFloat(AzKey, 0f), 0f));
		_el = ClampElevation(Finite(PlayerPrefs.GetFloat(ElKey, 0f), 0f));
		_aimM = new Vector3(Finite(PlayerPrefs.GetFloat(AimXKey, 0f), 0f), Finite(PlayerPrefs.GetFloat(AimYKey, 1f), 1f), Finite(PlayerPrefs.GetFloat(AimZKey, SceneDefaults.ApproachM), SceneDefaults.ApproachM));
	}

	private static float Finite(float v, float fallback)
	{
		return (float.IsNaN(v) || float.IsInfinity(v)) ? fallback : v;
	}

	private static void PersistAll()
	{
		PlayerPrefs.SetInt(ModeKey, (int)_mode);
		PlayerPrefs.SetFloat(AzKey, _az);
		PlayerPrefs.SetFloat(ElKey, _el);
		PlayerPrefs.SetFloat(AimXKey, _aimM.x);
		PlayerPrefs.SetFloat(AimYKey, _aimM.y);
		PlayerPrefs.SetFloat(AimZKey, _aimM.z);
		PlayerPrefs.Save();
	}

	public static float WrapAzimuth(float deg)
	{
		return Mathf.Clamp(Mathf.Repeat(deg + 180f, 360f), 0f, 360f) - 180f;
	}

	public static float ClampElevation(float deg)
	{
		return Mathf.Clamp(deg, ElevationMinDeg, ElevationMaxDeg);
	}

	public static Vector3 DirectionOf(float azimuthDeg, float elevationDeg)
	{
		return Quaternion.Euler(ClampElevation(elevationDeg), WrapAzimuth(azimuthDeg), 0f) * Vector3.forward;
	}

	public static Vector3 StartWorldOf(Vector3 aimWorld, Vector3 dir)
	{
		return aimWorld - dir * SceneScale.FromM(SceneDefaults.ApproachM);
	}

	private static Vector3 ToWorld(Vector3 m)
	{
		return new Vector3(SceneScale.FromM(m.x), SceneScale.FromM(m.y), SceneScale.FromM(m.z));
	}

	private static Vector3 ToM(Vector3 w)
	{
		return new Vector3(SceneScale.ToM(w.x), SceneScale.ToM(w.y), SceneScale.ToM(w.z));
	}

	private static Projectile FindProjectile()
	{
		return UnityEngine.Object.FindFirstObjectByType<Projectile>();
	}

	public static void SetMode(AimMode m)
	{
		Load();
		if (m == _mode)
		{
			return;
		}
		if (m == AimMode.Free)
		{
			EnterFree();
			return;
		}
		_mode = AimMode.Line;
		PlayerPrefs.SetInt(ModeKey, (int)_mode);
		PlayerPrefs.Save();
		RestoreLine(FindProjectile());
		_lastInside = null;
		Debug.Log("[Прицел] " + Describe());
		m_OnChanged?.Invoke();
	}

	private static bool EnterFree()
	{
		Vector3 point = default;
		if (!Available)
		{
			Debug.LogWarning("[Прицел] Свободный прицел недоступен: он есть только в корпусе режимов " + string.Format("«Нормальный» и «Продвинутый» (сейчас режим {0}, раздел {1}).", AppFlow.ActiveMode, AppFlow.ActiveSection));
			return false;
		}
		if (_mode != AimMode.Free)
		{
			Projectile pr = FindProjectile();
			_az = 0f;
			_el = 0f;
			if (pr != null)
			{
				if (!_line.valid)
				{
					SnapshotFromStart(pr);
				}
				if (!pr.TryFirstEntryPoint(out point))
				{
					point = pr.StartPositionWorld + pr.direction.normalized * SceneScale.FromM(SceneDefaults.ApproachM);
				}
				_aimM = ToM(point);
			}
			_mode = AimMode.Free;
			PersistAll();
			Debug.Log("[Прицел] " + Describe());
			Changed();
		}
		return true;
	}

	public static void SetAzimuth(float deg)
	{
		Load();
		if (float.IsNaN(deg) || float.IsInfinity(deg) || (_mode == AimMode.Line && !EnterFree()))
		{
			return;
		}
		float value = WrapAzimuth(deg);
		if (!Mathf.Approximately(value, _az))
		{
			_az = value;
			PlayerPrefs.SetFloat(AzKey, _az);
			PlayerPrefs.Save();
			Changed();
		}
	}

	public static void SetElevation(float deg)
	{
		Load();
		if (float.IsNaN(deg) || float.IsInfinity(deg) || (_mode == AimMode.Line && !EnterFree()))
		{
			return;
		}
		float value = ClampElevation(deg);
		if (!Mathf.Approximately(value, _el))
		{
			_el = value;
			PlayerPrefs.SetFloat(ElKey, _el);
			PlayerPrefs.Save();
			Changed();
		}
	}

	public static void SetAimPoint(Vector3 worldM)
	{
		Load();
		if (!IsFinite(worldM))
		{
			Debug.LogWarning(string.Format("[Прицел] Точка прицела {0} не число — отклонена.", worldM));
			return;
		}
		if (_mode == AimMode.Line && !EnterFree())
		{
			return;
		}
		_aimM = worldM;
		PersistAll();
		Changed();
	}

	public static void SetDirectionFromView(Vector3 viewForward)
	{
		Load();
		if (!IsFinite(viewForward) || viewForward.sqrMagnitude < 1E-12f || (_mode == AimMode.Line && !EnterFree()))
		{
			return;
		}
		Vector3 d = viewForward.normalized;
		_az = WrapAzimuth(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg);
		_el = ClampElevation(Mathf.Asin(Mathf.Clamp(0f - d.y, -1f, 1f)) * Mathf.Rad2Deg);
		PersistAll();
		Changed();
	}

	public static bool AimAtSelectionCentre()
	{
		PlateSelector selector = UnityEngine.Object.FindFirstObjectByType<PlateSelector>();
		if (selector == null || selector.SelectedPlates == null || selector.SelectedPlates.Count == 0)
		{
			return false;
		}
		Bounds bounds = default;
		bool any = false;
		foreach (ArmorPlate plate in selector.SelectedPlates)
		{
			if (plate == null)
			{
				continue;
			}
			Renderer[] renderers = plate.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in renderers)
			{
				if (renderer == null || !renderer.enabled)
				{
					continue;
				}
				if (any)
				{
					bounds.Encapsulate(renderer.bounds);
				}
				else
				{
					bounds = renderer.bounds;
					any = true;
				}
			}
		}
		if (!any)
		{
			return false;
		}
		SetAimPoint(ToM(bounds.center));
		return true;
	}

	private static bool IsFinite(Vector3 v)
	{
		return !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
	}

	private static void Changed()
	{
		if (Available)
		{
			Projectile pr = FindProjectile();
			if (pr != null)
			{
				ApplyTo(pr);
			}
		}
		m_OnChanged?.Invoke();
	}

	public static void ApplyToScene()
	{
		if (!Available)
		{
			return;
		}
		Projectile pr = FindProjectile();
		if (pr != null)
		{
			ApplyTo(pr);
		}
	}

	public static void ApplyTo(Projectile pr)
	{
		if (pr == null || !Available)
		{
			return;
		}
		Load();
		if (_mode == AimMode.Line)
		{
			return;
		}
		if (!_line.valid)
		{
			SnapshotFromStart(pr);
			Debug.LogWarning("[Прицел] Снимок «по линии» снят с текущего старта снаряда: сцена его не дала.");
		}
		Vector3 dir = (SabotageWorldAxis ? Vector3.forward : Direction);
		Vector3 start = AimPointWorld - dir * SceneScale.FromM(SceneDefaults.ApproachM);
		pr.direction = dir;
		pr.transform.position = start;
		Vector3 from = ((_line.dir.sqrMagnitude > 1E-12f) ? _line.dir.normalized : Vector3.forward);
		pr.transform.rotation = Quaternion.FromToRotation(from, dir) * _line.rot;
		pr.AdoptCurrentTransformAsStart();
		pr.visualApproach = false;
		string inside = StartInsideObjectName(pr);
		if (inside != _lastInside)
		{
			_lastInside = inside;
			if (inside != null)
			{
				Debug.LogWarning("[Прицел] Старт стоит ВНУТРИ объекта «" + inside + "»: луч, начатый внутри меша, его не видит, и объект выпадет из выстрела. Отодвинь точку прицела или смени направление.");
			}
		}
	}

	public static void OnSceneBoot()
	{
		_line = default;
		_lastInside = null;
		if (SectionFrame.IsRoof)
		{
			return;
		}
		Projectile pr = FindProjectile();
		if (pr == null)
		{
			return;
		}
		_line = new LineSnap
		{
			valid = true,
			pos = pr.transform.position,
			rot = pr.transform.rotation,
			dir = pr.direction
		};
		if (Available && Mode == AimMode.Free)
		{
			ApplyTo(pr);
		}
	}

	public static void OnSceneLoaded()
	{
		if (SectionFrame.IsRoof)
		{
			return;
		}
		Projectile pr = FindProjectile();
		if (pr == null)
		{
			return;
		}
		if (!_line.valid)
		{
			_line.rot = pr.StartRotationWorld;
			_line.dir = pr.direction;
			if (IsFree)
			{
				Debug.LogWarning("[Прицел] Загрузка без снимка сцены при свободном прицеле: поворот и направление «по линии» сняты с текущего снаряда.");
			}
		}
		_line.pos = pr.StartPositionWorld;
		_line.valid = true;
		if (Available && Mode == AimMode.Free)
		{
			ApplyTo(pr);
		}
	}

	private static void SnapshotFromStart(Projectile pr)
	{
		_line = new LineSnap
		{
			valid = true,
			pos = pr.StartPositionWorld,
			rot = pr.StartRotationWorld,
			dir = pr.direction
		};
	}

	private static void RestoreLine(Projectile pr)
	{
		if (pr == null || !_line.valid)
		{
			return;
		}
		pr.direction = _line.dir;
		pr.transform.position = _line.pos;
		pr.transform.rotation = _line.rot;
		pr.AdoptCurrentTransformAsStart();
		pr.visualApproach = false;
	}

	public static Vector3 SavedStartWorld(Projectile pr)
	{
		if (Available && Mode == AimMode.Free && _line.valid)
		{
			return _line.pos;
		}
		return (pr != null) ? pr.StartPositionWorld : Vector3.zero;
	}

	public static string StartInsideObjectName(Projectile pr)
	{
		if (pr == null)
		{
			return null;
		}
		Vector3 start = pr.StartPositionWorld;
		SandboxModel[] models = UnityEngine.Object.FindObjectsByType<SandboxModel>(FindObjectsSortMode.None);
		foreach (SandboxModel model in models)
		{
			if (model == null)
			{
				continue;
			}
			MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>();
			foreach (MeshFilter filter in filters)
			{
				if (filter == null || filter.sharedMesh == null)
				{
					continue;
				}
				Bounds bounds = filter.sharedMesh.bounds;
				if (bounds.Contains(filter.transform.InverseTransformPoint(start)))
				{
					return model.name;
				}
			}
		}
		return null;
	}

	public static string Describe()
	{
		if (!Available)
		{
			return string.Format("Прицел: недоступен (режим {0}, раздел {1}) — выстрел по линии.", AppFlow.ActiveMode, AppFlow.ActiveSection);
		}
		Load();
		if (_mode == AimMode.Line)
		{
			return "Прицел: ПО ЛИНИИ — старт из сцены/файла, направление +Z (как до V1).";
		}
		Vector3 d = Direction;
		return string.Format("Прицел: СВОБОДНО — азимут {0:F1}°, возвышение {1:F1}°, точка {2} м, ", AzimuthDeg, ElevationDeg, AimPointM) + string.Format("направление ({0:F4}, {1:F4}, {2:F4}).", d.x, d.y, d.z);
	}
}
