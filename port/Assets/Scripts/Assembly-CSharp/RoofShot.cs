using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

public static class RoofShot
{
	private const string KindKey = "armorsim.roof_spawn";

	private const string AngleKey = "armorsim.roof_angle";

	[CompilerGenerated]
	private static Action m_OnChanged;

	private static RoofSpawnKind _kind;

	private static float _angleDeg;

	private static bool _loaded;

	public static RoofSpawnKind Kind
	{
		get
		{
			Load();
			return _kind;
		}
	}

	public static float AngleDeg
	{
		get
		{
			Load();
			return _angleDeg;
		}
	}

	public static event Action OnChanged
	{
		[CompilerGenerated]
		add
		{
			Action comparand = m_OnChanged;
			Action previous;
			do
			{
				previous = comparand;
				Action combined = (Action)Delegate.Combine(previous, value);
				comparand = Interlocked.CompareExchange(ref m_OnChanged, combined, previous);
			}
			while (comparand != previous);
		}
		[CompilerGenerated]
		remove
		{
			Action comparand = m_OnChanged;
			Action previous;
			do
			{
				previous = comparand;
				Action removed = (Action)Delegate.Remove(previous, value);
				comparand = Interlocked.CompareExchange(ref m_OnChanged, removed, previous);
			}
			while (comparand != previous);
		}
	}

	private static void Load()
	{
		if (_loaded)
		{
			return;
		}
		_loaded = true;
		_kind = (RoofSpawnKind)PlayerPrefs.GetInt(KindKey, 0);
		_angleDeg = Mathf.Clamp(PlayerPrefs.GetFloat(AngleKey, SectionFrame.RoofAngleDefaultDeg), SectionFrame.RoofAngleMinDeg, SectionFrame.RoofAngleMaxDeg);
	}

	public static void SetKind(RoofSpawnKind k)
	{
		Load();
		if (_kind == k)
		{
			return;
		}
		_kind = k;
		PlayerPrefs.SetInt(KindKey, (int)k);
		PlayerPrefs.Save();
		Changed();
	}

	public static void SetAngle(float deg)
	{
		Load();
		deg = Mathf.Clamp(deg, SectionFrame.RoofAngleMinDeg, SectionFrame.RoofAngleMaxDeg);
		if (Mathf.Approximately(deg, _angleDeg))
		{
			return;
		}
		_angleDeg = deg;
		PlayerPrefs.SetFloat(AngleKey, deg);
		PlayerPrefs.Save();
		Changed();
	}

	private static void Changed()
	{
		ApplyToScene();
		m_OnChanged?.Invoke();
	}

	public static void ApplyToScene()
	{
		if (AppFlow.ActiveSection != TargetSection.Roof)
		{
			return;
		}
		Projectile pr = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (pr != null)
		{
			ApplyTo(pr);
		}
	}

	public static void ApplyTo(Projectile pr)
	{
		if (pr == null || AppFlow.ActiveSection != TargetSection.Roof)
		{
			return;
		}
		Vector3 position = Vector3.zero;
		Vector3 visualFrom = Vector3.zero;
		Vector3 direction;
		bool visualApproach = false;
		if (Kind == RoofSpawnKind.Atgm)
		{
			position = SectionFrame.AtgmOrigin(pr);
			direction = Vector3.down;
			float clearanceM = 0f;
			if (pr != null)
			{
				ProjectileDefinition definition = pr.GetDefinition();
				if (definition != null && definition.fuzeStandoffMM > 0f)
				{
					clearanceM = definition.fuzeStandoffMM / 1000f;
				}
			}
			visualFrom = position;
			visualFrom.y = SceneScale.FromM(Mathf.Max(0.1f, clearanceM) + 1f);
			visualFrom -= Vector3.forward * SceneScale.FromM(SceneDefaults.ApproachM);
			visualApproach = true;
		}
		else
		{
			float angle = AngleDeg;
			direction = SectionFrame.AngledDirection(angle);
			float approach = SceneScale.FromM(SceneDefaults.ApproachM);
			position = -direction * approach;
			position.y = SceneScale.FromM(1f) - SceneScale.RodDiameterWorld(1f) - direction.y * approach;
			direction = SectionFrame.AngledDirection(AngleDeg);
		}
		pr.direction = direction;
		pr.transform.position = position;
		Vector3 upwards = Mathf.Abs(direction.y) > 0.999f ? Vector3.forward : Vector3.up;
		pr.transform.rotation = Quaternion.LookRotation(direction, upwards);
		pr.AdoptCurrentTransformAsStart();
		pr.visualApproachFrom = visualFrom;
		pr.visualApproach = visualApproach;
	}

	public static string Describe(Projectile pr)
	{
		if (Kind != RoofSpawnKind.Atgm)
		{
			return string.Format("Точка старта: угловой подлёт {0:F0}° (дуга над крышей: 30° полого спереди, ", AngleDeg) + "90° отвесно, 180° горизонтально сзади).";
		}
		float clearanceM = 0f;
		if (pr != null)
		{
			ProjectileDefinition definition = pr.GetDefinition();
			if (definition != null && definition.fuzeStandoffMM > 0f)
			{
				clearanceM = definition.fuzeStandoffMM / 1000f;
			}
		}
		return string.Format("Точка старта: ПТУР — пролёт по горизонтали на {0:F0} мм ", Mathf.Max(0.1f, clearanceM) * 1000f) + "над крышей, ось поражения вертикально вниз.";
	}

	static RoofShot()
	{
		_kind = RoofSpawnKind.Angled;
		_angleDeg = SectionFrame.RoofAngleDefaultDeg;
	}
}
