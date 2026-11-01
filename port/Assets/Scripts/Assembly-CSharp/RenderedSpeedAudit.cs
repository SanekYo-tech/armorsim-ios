using System;
using System.Collections.Generic;
using UnityEngine;

public class RenderedSpeedAudit : MonoBehaviour
{
	private class Tracked
	{
		public Renderer renderer;

		public Transform tr;

		public string kind;

		public Vector3 lastPos;

		public bool primed;

		public float birthRodSpeed;

		public float worstRatio;

		public float worstSpeed;
	}

	[Header("Аудит экранной скорости (инвариант шага 20/29)")]
	public bool enabledAudit;

	public int referenceWindowFrames;

	public bool logEveryViolation;

	private readonly List<Tracked> tracked;

	private readonly Queue<float> rodWindow;

	private Vector3 rodLastPos;

	private bool rodPrimed;

	private float rodSpeedNow;

	private int violations;

	private float worstRatioOverall;

	private string worstKind;

	private float worstMeasured;

	private float worstReference;

	public static RenderedSpeedAudit Instance { get; private set; }

	public RenderedSpeedAudit()
	{
		enabledAudit = true;
		referenceWindowFrames = 5;
		tracked = new List<Tracked>();
		rodWindow = new Queue<float>();
		worstKind = "—";
	}

	public static void Bootstrap()
	{
		if (UnityEngine.Object.FindFirstObjectByType<RenderedSpeedAudit>() != null)
		{
			return;
		}
		new GameObject("RenderedSpeedAudit").AddComponent<RenderedSpeedAudit>();
	}

	private void Awake()
	{
		Instance = this;
	}

	public static void Register(Transform tr, Renderer r, string kind)
	{
		if (Instance == null || !Instance.enabledAudit || tr == null)
		{
			return;
		}
		Tracked t = new Tracked
		{
			renderer = r,
			tr = tr,
			kind = kind,
			birthRodSpeed = RenderedSpeedRule.BirthReference(new List<float>(Instance.rodWindow)),
			primed = false
		};
		Instance.tracked.Add(t);
	}

	public void ResetShot()
	{
		tracked.Clear();
		rodWindow.Clear();
		rodPrimed = false;
		violations = 0;
		worstKind = "—";
		worstMeasured = 0f;
	}

	private static Vector3 DrawnCenter(Tracked t)
	{
		if (t.renderer != null)
		{
			return t.renderer.bounds.center;
		}
		return t.tr.position;
	}

	private void LateUpdate()
	{
		if (!enabledAudit)
		{
			return;
		}
		float dt = Time.deltaTime;
		if (dt < 1E-09f)
		{
			return;
		}
		Renderer rodRenderer = ((PenetrationPlayer.Instance != null) ? PenetrationPlayer.Instance.DrawnRodRenderer : null);
		if (rodRenderer == null)
		{
			rodSpeedNow = 0f;
		}
		else
		{
			Bounds b = rodRenderer.bounds;
			if (rodPrimed)
			{
				rodSpeedNow = ((dt > 1E-09f) ? (b.center - rodLastPos).magnitude / dt : 0f);
			}
			rodLastPos = b.center;
			rodPrimed = true;
		}
		rodWindow.Enqueue(rodSpeedNow);
		int maxCount = Math.Max(referenceWindowFrames, 1);
		while (rodWindow.Count > maxCount)
		{
			rodWindow.Dequeue();
		}
		for (int i = tracked.Count - 1; i >= 0; i--)
		{
			Tracked t = tracked[i];
			if (t.tr == null)
			{
				tracked.RemoveAt(i);
				continue;
			}
			t.birthRodSpeed = Math.Max(t.birthRodSpeed, rodSpeedNow);
			Vector3 pos = DrawnCenter(t);
			if (!t.primed)
			{
				t.lastPos = pos;
				t.primed = true;
				continue;
			}
			float speed = ((dt > 1E-09f) ? (pos - t.lastPos).magnitude / dt : 0f);
			t.lastPos = pos;
			float ratio = ((t.birthRodSpeed > 1E-09f) ? speed / t.birthRodSpeed : 0f);
			if (ratio > t.worstRatio)
			{
				t.worstRatio = ratio;
				t.worstSpeed = speed;
			}
			if (ratio > 1.05f)
			{
				violations++;
				if (ratio > worstRatioOverall)
				{
					worstRatioOverall = ratio;
					worstKind = t.kind;
					worstMeasured = speed;
					worstReference = t.birthRodSpeed;
				}
				if (logEveryViolation)
				{
					Debug.LogWarning(string.Format("[АУДИТ СКОРОСТИ] {0}: на экране {1:F3} мир.ед/с при опоре ", t.kind, speed) + string.Format("{0:F3} (×{1:F1}) — обогнал стержень.", t.birthRodSpeed, ratio));
				}
			}
		}
	}

	public void ReportShot()
	{
		if (!enabledAudit)
		{
			return;
		}
		if (violations > 0)
		{
			Debug.LogError(string.Format("[АУДИТ СКОРОСТИ] ✘ ИНВАРИАНТ НАРУШЕН: {0} замеров выше опоры. ", violations) + string.Format("Худший — «{0}»: {1:F3} мир.ед/с при опоре {2:F3} ", worstKind, worstMeasured, worstReference) + string.Format("(×{0:F1}). ⚠ Мерено по СДВИГУ РЕНДЕРЕРА: если поданные скорости ", worstRatioOverall) + "верны, смотри допуски физического движка (Fragment.ContactOffsetFor, maxDepenetrationVelocity) — дважды ломалось именно там.");
			return;
		}
		Debug.Log(string.Format("[АУДИТ СКОРОСТИ] ✔ инвариант держится: за показ отслежено {0} ", tracked.Count) + "нарисованных объектов, ни один не обогнал стержень (мерено по сдвигу рендерера).");
	}
}
