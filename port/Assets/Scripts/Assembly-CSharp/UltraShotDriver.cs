using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class UltraShotDriver : MonoBehaviour, IUltraTimelineSink
{
	public ContinuumViewer viewer;

	public UltraFixtureLoader fixture;

	private GeometryTimeline owned;

	private string ownedKey;

	private string pendingKey;

	private ProjectedScenario pendingAtStart;

	private static readonly int SeedBytes = Marshal.SizeOf(typeof(ContinuumFragmentSeed));

	public float ElementsPerDiameter
	{
		get
		{
			if (Current != null && Current.scenario != null)
			{
				return Current.scenario.elementsPerDiameter;
			}
			return AppFlow.SelectedElementsPerDiameter;
		}
	}

	public bool Solving { get; private set; }

	public ProjectedScenario Current { get; private set; }

	private UltraRung EtalonRung()
	{
		if (UltraQualityLadder.TrySelected(AppFlow.SelectedElementsPerDiameter, out UltraRung rung))
		{
			return rung;
		}
		if (UltraQualityLadder.TryCoarsest(out UltraRung coarsest))
		{
			Debug.LogWarning(string.Format("ULTRA: ступени D/{0:F0} в лестнице ", AppFlow.SelectedElementsPerDiameter) + string.Format("нет — эталон считается на самой грубой D/{0:F0}.", coarsest.elementsPerDiameter));
			return coarsest;
		}
		Debug.LogError("ULTRA: лестница качества ПУСТА — эталон считать не на чем.");
		return UltraQualityLadder.RowFor(AppFlow.SelectedElementsPerDiameter);
	}

	private void Awake()
	{
		UltraSolveQueue.Instance.sink = this;
		pendingAtStart = AppFlow.PendingUltraBuild;
		AppFlow.PendingUltraBuild = null;
		if (UltraSolveQueue.Instance.jobFactory == null)
		{
			UltraSolveQueue.Instance.jobFactory = (ContinuumScenario s) => new ContinuumSolveJob(s);
		}
	}

	private void Start()
	{
		if (pendingAtStart != null)
		{
			Solve(pendingAtStart);
		}
		pendingAtStart = null;
	}

	private void OnDestroy()
	{
		if (owned != null)
		{
			owned.Dispose();
		}
		owned = null;
		ownedKey = null;
	}

	public ContinuumScenario Scenario()
	{
		ContinuumScenario scenario = ContinuumScenario.Etalon();
		UltraRung rung = EtalonRung();
		scenario.elementsPerDiameter = rung.elementsPerDiameter;
		if (scenario.durationS <= 0f)
		{
			scenario.durationS = UltraProjection.DisplayWindowS(scenario);
		}
		return scenario;
	}

	public ProjectedScenario EtalonProjected()
	{
		ContinuumScenario scenario = ContinuumScenario.Etalon();
		UltraRung rung = EtalonRung();
		scenario.elementsPerDiameter = rung.elementsPerDiameter;
		if (scenario.durationS <= 0f)
		{
			scenario.durationS = UltraProjection.DisplayWindowS(scenario);
		}
		float losMM = scenario.targetThicknessM * 1000f;
		return new ProjectedScenario(scenario, isLossy: false, null, 0f, losMM, losMM);
	}

	public void Solve()
	{
		Solve(EtalonProjected());
	}

	public void Solve(ProjectedScenario projected)
	{
		if (projected == null)
		{
			Debug.LogError("ULTRA: постановка не спроецирована.");
			return;
		}
		Current = projected;
		string text = UltraCacheKey.Compute(projected.scenario);
		if (text == null)
		{
			Debug.LogError("ULTRA: сценарий не кэшируется, ключ не вычислен. Расчёт не запущен.");
			return;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		if (UltraTimelineCache.Shared.TryLoad(text, out GeometryTimeline geometryTimeline))
		{
			stopwatch.Stop();
			Debug.Log(string.Format("ULTRA: попадание в кэш [{0}] за {1:F0} мс, ", text, stopwatch.Elapsed.TotalMilliseconds) + string.Format("ступень D/{0:F0}, решатель не запускался", projected.scenario.elementsPerDiameter));
			Adopt(text, geometryTimeline);
			return;
		}
		UltraSolveQueue.Submission submission = UltraSolveQueue.Instance.Submit(projected.scenario, out pendingKey);
		if (submission == UltraSolveQueue.Submission.NotCacheable)
		{
			Debug.LogError("ULTRA: очередь отказала, ключ не вычислен");
			pendingKey = null;
		}
		else
		{
			Solving = true;
			Debug.Log(string.Format("ULTRA: расчёт в очереди, ключ [{0}], ступень D/{1:F0}, ", pendingKey, projected.scenario.elementsPerDiameter) + string.Format("шагов оценочно {0} ({1})", UltraSolveQueue.EstimateSteps(projected.scenario), submission));
		}
	}

	public UltraBuildResult Build(UltraShotEdit shot, IList<UltraPlateEdit> plates, UltraRung rung, float windowOverrideS = 0f)
	{
		UltraBuildResult ultraBuildResult = UltraSceneBuild.Prepare(shot, plates, rung, UltraTimelineCache.Shared.Has, windowOverrideS);
		if (ultraBuildResult.status == UltraBuildStatus.Refused)
		{
			Debug.LogWarning("ULTRA сборка отказана: " + Localization.T(ultraBuildResult.reasonKey));
			return ultraBuildResult;
		}
		if (UltraSceneBuild.NeedsEquivalenceNote(ultraBuildResult))
		{
			Debug.Log("ULTRA: " + Localization.T("ultra.cache.equivalent") + "  " + ultraBuildResult.equivalenceLine);
		}
		if (UltraSceneBuild.NeedsDeclaration(ultraBuildResult))
		{
			Debug.Log("ULTRA объявление: " + Localization.T(ultraBuildResult.declarationKey) + "  " + ultraBuildResult.equivalenceLine);
		}
		Solve(ultraBuildResult.projected);
		return ultraBuildResult;
	}

	public void Cancel()
	{
		if (Solving)
		{
			UltraSolveQueue.Instance.RequestCancel();
			if (pendingKey != null)
			{
				UltraTimelineCache.Shared.Discard(pendingKey);
			}
			pendingKey = null;
			Solving = false;
			Debug.Log("ULTRA: расчёт отменён игроком, показ очищен");
			ClearView();
		}
	}

	public void Accept(string key, GeometryTimeline tl)
	{
		Solving = false;
		pendingKey = null;
		if (tl != null)
		{
			if (tl.Completed && !tl.Cancelled)
			{
				UltraTimelineCache.Shared.Accept(key, tl);
				Adopt(key, tl);
				return;
			}
			tl.Dispose();
		}
		ClearView();
	}

	private void Adopt(string key, GeometryTimeline tl)
	{
		if (tl == null || tl.frames == null || tl.frames.Count == 0)
		{
			Debug.LogError("ULTRA: решение [" + key + "] пришло БЕЗ КАДРОВ — показывать нечего. Это не «идёт расчёт»: работа завершена и вернула пустой результат. Показ возвращается к оснастке.");
			if (tl != null)
			{
				tl.Dispose();
			}
			ClearView();
			return;
		}
		int num = VertexCount();
		GeometryTimeline geometryTimeline = owned;
		owned = tl;
		ownedKey = key;
		if (fixture != null)
		{
			fixture.LoadSolved(tl, UltraCacheKey.SolverFingerprint);
		}
		else if (viewer != null)
		{
			viewer.SetTimeline(tl);
			viewer.frame = 0;
			viewer.playing = true;
		}
		if (geometryTimeline != null)
		{
			geometryTimeline.Dispose();
		}
		int num2 = VertexCount();
		if (num > 0 && num2 > 0 && num != num2)
		{
			Debug.LogWarning(string.Format("ULTRA подмена: число вершин изменилось {0} -> {1}. ", num, num2) + "Токарка пересобрала топологию, проверьте profilePoints/segments.");
		}
		ReportSize(tl);
	}

	private void ClearView()
	{
		Current = null;
		if (viewer != null)
		{
			viewer.SetTimeline(null);
		}
		if (fixture != null)
		{
			fixture.enabled = true;
		}
	}


	private void ReleaseOwned()
	{
		if (owned != null)
		{
			owned.Dispose();
		}
		owned = null;
		ownedKey = null;
	}

	private int VertexCount()
	{
		if (viewer == null)
		{
			return 0;
		}
		MeshFilter meshFilter = viewer.GetComponentInChildren<MeshFilter>();
		if (meshFilter != null && meshFilter.sharedMesh != null)
		{
			return meshFilter.sharedMesh.vertexCount;
		}
		return 0;
	}

	private void ReportSize(GeometryTimeline tl)
	{
		if (tl == null || tl.frames == null || tl.frames.Count == 0)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < tl.frames.Count; i++)
		{
			GeometryFrame geometryFrame = tl.frames[i];
			if (geometryFrame.projectileProfile.IsCreated)
			{
				num += geometryFrame.projectileProfile.Length;
			}
			if (geometryFrame.targetProfile.IsCreated)
			{
				num += geometryFrame.targetProfile.Length;
			}
			if (geometryFrame.fragmentSeeds.IsCreated)
			{
				num2 += geometryFrame.fragmentSeeds.Length;
			}
			if (geometryFrame.fieldPlastic.IsCreated)
			{
				num3 += geometryFrame.fieldPlastic.Length;
			}
		}
		int num4 = SeedBytes * num2 + (num3 + num * 2) * 4;
		Debug.Log(string.Format("ULTRA память: кадров {0}, буферы кадров ", tl.frames.Count) + string.Format("{0:F1} МБ = {1:F1} КБ/кадр, ", num4 / 1048576.0, num4 / (double)tl.frames.Count / 1024.0) + string.Format("из них ПОЛЕ {0:F1} МБ ({1:F0} КБ/кадр, ", num3 * 4 / 1048576.0, tl.fieldGridR * tl.fieldGridZ * 4 / 1024.0) + string.Format("сетка {0}×{1} — от ступени НЕ зависит). ", tl.fieldGridR, tl.fieldGridZ) + string.Format("Ступень D/{0:F0}.", ElementsPerDiameter));
	}
}
