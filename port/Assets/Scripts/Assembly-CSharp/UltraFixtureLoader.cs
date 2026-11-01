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
	}

	private void OnDestroy()
	{
	}

	public void LoadSynthetic()
	{
	}

	public void ShowEmpty()
	{
	}

	public void LoadSolved(GeometryTimeline tl, string fingerprint)
	{
	}
}
