using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Debug = UnityEngine.Debug;
using UnityEngine;

public class UltraShotRunner : MonoBehaviour
{
	[CompilerGenerated]
	private sealed class _003CPlay_003Ed__17 : IEnumerator<object>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private object _003C_003E2__current;

		public UltraShotRunner _003C_003E4__this;

		private int _003Cn_003E5__2;

		private float _003Cper_003E5__3;

		private int _003Ci_003E5__4;

		object IEnumerator<object>.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return _003C_003E2__current;
			}
		}

		[DebuggerHidden]
		public _003CPlay_003Ed__17(int _003C_003E1__state)
		{
			this._003C_003E1__state = _003C_003E1__state;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
		}

		private bool MoveNext()
		{
			int num = _003C_003E1__state;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				_003C_003E1__state = -1;
				_003Ci_003E5__4++;
			}
			else
			{
				_003C_003E1__state = -1;
				_003Cn_003E5__2 = _003C_003E4__this.timeline.frames.Count;
				_003Cper_003E5__3 = _003C_003E4__this.playbackSeconds / (float)_003Cn_003E5__2;
				_003Ci_003E5__4 = 0;
			}
			if (_003Ci_003E5__4 >= _003Cn_003E5__2)
			{
				return false;
			}
			_003C_003E4__this.binder.Apply(_003C_003E4__this.timeline.frames[_003Ci_003E5__4]);
			_003C_003E2__current = new WaitForSeconds(_003Cper_003E5__3);
			_003C_003E1__state = 1;
			return true;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
	}

	public float playbackSeconds;

	public float elementsPerDiameter;

	private static UltraShotRunner instance;

	private ContinuumMeshBinder binder;

	private GameObject rodGo;

	private GameObject targetGo;

	private GeometryTimeline timeline;

	private Coroutine playing;

	private readonly List<Renderer> hidden;

	public UltraShotRunner()
	{
		playbackSeconds = 4f;
		elementsPerDiameter = 16f;
		hidden = new List<Renderer>();
	}

	public static UltraShotRunner FindOrCreate()
	{
		if (instance != null)
		{
			return instance;
		}
		instance = UnityEngine.Object.FindFirstObjectByType<UltraShotRunner>();
		if (instance == null)
		{
			GameObject gameObject = new GameObject("ULTRA: показ выстрела");
			instance = gameObject.AddComponent<UltraShotRunner>();
		}
		return instance;
	}

	public static bool TryBegin(Projectile proj)
	{
		if (AppFlow.ActiveMode != SimMode.Ultra)
		{
			return false;
		}
		if (proj == null)
		{
			return false;
		}
		if (ModelRouting.SceneHasModel(proj))
		{
			Say(Localization.T(ModelRouting.Refuses(SimMode.Ultra)), warn: true);
			return true;
		}
		ProjectileDefinition definition = proj.GetDefinition();
		if (RodPassports.Refusal(definition, out string key, out object[] args))
		{
			return false;
		}
		if (UltraEditorState.FromScene(proj, out UltraShotEdit shot, out List<UltraPlateEdit> plates, out string reasonKey))
		{
			UltraRung rung = UltraQualityLadder.RowFor(AppFlow.SelectedElementsPerDiameter);
			UltraBuildResult ultraBuildResult = UltraBuildAction.Run(shot, plates, rung, AppFlow.SelectedDisplayWindowS);
			if (ultraBuildResult.status == UltraBuildStatus.Refused)
			{
				Say("ULTRA не собрал симуляцию: " + Localization.T(ultraBuildResult.reasonKey), warn: true);
			}
			else
			{
				Say("ULTRA: " + ultraBuildResult.equivalenceLine, warn: false);
			}
			return true;
		}
		string text = null;
		if (ContinuumGateStrings.Table.TryGetValue(reasonKey, out string[] array))
		{
			text = array[(int)Localization.Current];
		}
		Say("ULTRA НЕ ВЗЯЛ выстрел: " + text + " Идём обычным путём.", warn: true);
		return false;
	}

	internal static bool PassportHandsOff(ProjectileDefinition d)
	{
		return RodPassports.Refusal(d, out string key, out object[] args);
	}

	private static void Say(string text, bool warn)
	{
		if (warn)
		{
			Debug.LogWarning(text);
		}
		else
		{
			Debug.Log(text);
		}
		if (ResultDisplay.Instance != null)
		{
			ResultDisplay.Instance.Show(text, warn ? new Color(1f, 0.72f, 0.3f, 1f) : new Color(0.62f, 0.86f, 1f, 1f));
		}
	}

	private static string Explain(string key)
	{
		if (ContinuumGateStrings.Table.TryGetValue(key, out string[] value))
		{
			return value[(int)Localization.Current];
		}
		return key;
	}

	private static ContinuumScenario BuildScenario(Projectile proj)
	{
		ProjectileDefinition definition = proj.GetDefinition();
		float num = float.MaxValue;
		ArmorPlate armorPlate = null;
		ArmorPlate[] array = UnityEngine.Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			ArmorPlate armorPlate2 = array[i];
			if (armorPlate2.kind == ArmorPlate.ObjectKind.Plate && proj.transform.position.y < armorPlate2.transform.position.y && armorPlate2.transform.position.y < num)
			{
				num = armorPlate2.transform.position.y;
				armorPlate = armorPlate2;
			}
		}
		if (armorPlate == null)
		{
			return null;
		}
		if (!ContinuumGate.TryMapCore(definition.coreType, out ContinuumMaterialId core))
		{
			return null;
		}
		if (!ContinuumGate.TryMapTarget(armorPlate.armorType, out ContinuumMaterialId target))
		{
			return null;
		}
		float num2 = armorPlate.thicknessMM / 1000f;
		ContinuumScenario ContinuumScenario = new ContinuumScenario();
		ContinuumScenario.name = "ULTRA: выстрел из песочницы";
		ContinuumScenario.rodDiameterM = Math.Max(definition.diameterMM / 1000f, 0.001f);
		ContinuumScenario.rodLengthM = Math.Max(definition.rodLengthMM / 1000f, 0.001f);
		ContinuumScenario.impactVelocityMps = Math.Max(definition.velocityMps, 1f);
		ContinuumScenario.nose = MapNose(definition.nose);
		ContinuumScenario.rodMaterial = core;
		ContinuumScenario.targetKind = ContinuumTargetKind.Plate;
		ContinuumScenario.targetThicknessM = Math.Max(num2, 0.001f);
		ContinuumScenario.targetMaterial = target;
		ContinuumScenario.elementsPerDiameter = FindOrCreate().elementsPerDiameter;
		ContinuumScenario.outputFrames = 200;
		float rho0 = ContinuumMaterials.Get(core).rho0;
		float num3 = Math.Max(ContinuumMaterials.Get(target).rho0, 1f);
		float num4 = Math.Max(0.1f, Mathf.Sqrt(rho0 / num3));
		float num5 = Math.Max(1f, ContinuumScenario.impactVelocityMps / (1f + 1f / num4));
		ContinuumScenario.durationS = Math.Clamp(num2 * 3f / num5, 6e-05f, 0.0004f);
		return ContinuumScenario;
	}

	public static ContinuumNose MapNose(NoseShape n)
	{
		if (n == NoseShape.Flat)
		{
			return ContinuumNose.Flat;
		}
		if (n == NoseShape.Ogive)
		{
			return ContinuumNose.Ogive;
		}
		return ContinuumNose.Conical;
	}

	private void Begin(Projectile proj, ContinuumScenario s)
	{
		Restore();
		if (playing != null)
		{
			StopCoroutine(playing);
			playing = null;
		}
		ArmorPlate armorPlate = null;
		float num = float.MaxValue;
		ArmorPlate[] array = UnityEngine.Object.FindObjectsByType<ArmorPlate>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			ArmorPlate armorPlate2 = array[i];
			if (armorPlate2.kind == ArmorPlate.ObjectKind.Plate && proj.transform.position.y < armorPlate2.transform.position.y && armorPlate2.transform.position.y < num)
			{
				num = armorPlate2.transform.position.y;
				armorPlate = armorPlate2;
			}
		}
		if (armorPlate == null)
		{
			return;
		}
		float num2 = armorPlate.thicknessMM * 0.5f / 1000f;
		base.transform.position = armorPlate.transform.position - new Vector3(0f, 0f, num2);
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
		Hide(armorPlate.GetComponentsInChildren<Renderer>());
		Hide(proj.GetComponentsInChildren<Renderer>());
		ContinuumBackend ContinuumBackend = new ContinuumBackend();
		timeline = ContinuumBackend.Solve(s, null, default(CancellationToken));
		Debug.Log(timeline.diagnostics.Report());
		Debug.Log(string.Format("ULTRA: {0} кадров за {1:F0} мкс модельного ", timeline.frames.Count, s.durationS * 1000000f) + string.Format("времени, показ {0:F1} с. ⚠ Это ФОРМА, а не приговор: ", playbackSeconds) + "пробитие и остаточную скорость по-прежнему говорит аналитическое ядро.");
		if (binder == null)
		{
			binder = new ContinuumMeshBinder(256, 48);
		}
		if (rodGo == null)
		{
			rodGo = MakeBody("ULTRA стержень", binder.ProjectileMesh, new Color(0.3f, 0.3f, 0.33f, 1f));
		}
		if (targetGo == null)
		{
			targetGo = MakeBody("ULTRA преграда", binder.TargetMesh, new Color(0.38f, 0.38f, 0.42f, 1f));
		}
		playing = StartCoroutine(Play());
	}

	private IEnumerator Play()
	{
		_003CPlay_003Ed__17 _003CPlay_003Ed__ = new _003CPlay_003Ed__17(0);
		_003CPlay_003Ed__._003C_003E4__this = this;
		return _003CPlay_003Ed__;
	}

	[System.Runtime.CompilerServices.IteratorStateMachine(typeof(_003CPlay_003Ed__17))]

	private GameObject MakeBody(string name, Mesh mesh, Color albedo)
	{
		GameObject gameObject = new GameObject(name);
		gameObject.transform.SetParent(base.transform, worldPositionStays: false);
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
		Material material = new Material(Shader.Find("Standard"));
		material.color = albedo;
		material.SetFloat("_Metallic", 0.85f);
		material.SetFloat("_Glossiness", 0.45f);
		meshRenderer.sharedMaterial = material;
		return gameObject;
	}

	private void Hide(Renderer[] rs)
	{
		for (int i = 0; i < rs.Length; i++)
		{
			Renderer renderer = rs[i];
			if (!(renderer == null) && renderer.enabled)
			{
				renderer.enabled = false;
				hidden.Add(renderer);
			}
		}
	}

	public void Restore()
	{
		foreach (Renderer item in hidden)
		{
			if (item != null)
			{
				item.enabled = true;
			}
		}
		hidden.Clear();
		if (timeline != null)
		{
			timeline.Dispose();
			timeline = null;
		}
	}

	private void OnDisable()
	{
		Restore();
	}

	private void OnDestroy()
	{
		Restore();
		if (binder != null)
		{
			binder.Dispose();
			binder = null;
		}
	}
}
