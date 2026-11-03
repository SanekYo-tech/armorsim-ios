using UnityEngine;
using UnityEngine.UI;

public class UltraFixtureLoader : MonoBehaviour
{
	public ContinuumViewer viewer;

	public Text badge;

	public bool loadOnStart;

	public bool autoPlay;

	public static string LoadedStamp;

	private GeometryTimeline ownedFixture;

	private void Start()
	{
		Debug.Log("ULTRA " + UltraGatingSet.Describe());
		if (loadOnStart)
		{
			LoadSynthetic();
		}
	}

	private void OnDestroy()
	{
		if (ownedFixture != null)
		{
			ownedFixture.Dispose();
			ownedFixture = null;
		}
	}

	public void LoadSynthetic()
	{
		Debug.LogError("СБОРКА: СИНТЕТИЧЕСКАЯ оснастка загружена в сборке для игрока. Это дефект конфигурации сборки: оснастка предназначена только для редактора. Показ остановлен.");
	}

	public void ShowEmpty()
	{
		if (ownedFixture != null)
		{
			ownedFixture.Dispose();
			ownedFixture = null;
		}
		LoadedStamp = null;
		if (badge != null)
		{
			badge.text = UltraProvenance.Describe(null);
			badge.enabled = true;
		}
	}

	public void LoadSolved(GeometryTimeline tl, string fingerprint)
	{
		if (ownedFixture != null)
		{
			ownedFixture.Dispose();
			ownedFixture = null;
		}
		LoadedStamp = fingerprint;
		if (viewer != null)
		{
			viewer.SetTimeline(tl);
			if (autoPlay)
			{
				viewer.frame = 0;
				viewer.playing = true;
			}
		}
		if (badge != null)
		{
			bool num = fingerprint == UltraCacheKey.SolverFingerprint;
			badge.text = UltraProvenance.Describe(fingerprint);
			badge.enabled = !num;
		}
	}
}
