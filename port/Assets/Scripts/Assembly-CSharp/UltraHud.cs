using UnityEngine;
using UnityEngine.UI;

public class UltraHud : MonoBehaviour
{
	[Header("Ход решения")]
	public Text fraction;

	[Header("Ход решения")]
	public Text elapsed;

	[Header("Ход решения")]
	public Text eta;

	[Header("Настройки")]
	public Text cacheSize;

	[Header("Настройки")]
	public Text rungs;

	[Header("Настройки")]
	public Text modeEntry;

	public Text gateSet;

	public Button cacheClear;

	[Header("Объявление проекции")]
	public Text declaration;

	public UltraShotDriver driver;

	[Header("Призрак построенной плиты")]
	public UltraBuiltGhost ghost;

	public KeyCode ghostKey = KeyCode.G;

	[Header("Поле")]
	public Text fieldSelector;

	[Header("Поле")]
	public Text fieldLegend;

	public UltraField field;

	[Header("Камера")]
	public UltraOrbitCamera orbit;

	[Header("Панель диагностики")]
	public GameObject panel;

	public GameObject progressPanel;

	public KeyCode panelKey = KeyCode.Tab;

	private UltraTimelineCache cache;

	private bool declarationMissingSaid;

	public bool PanelOpen
	{
		get
		{
			if (panel != null)
			{
				return panel.activeSelf;
			}
			return false;
		}
	}

	public void TogglePanel()
	{
		if (panel == null)
		{
			return;
		}
		panel.SetActive(!panel.activeSelf);
	}

	private void Awake()
	{
		cache = UltraTimelineCache.Shared;
		if (cacheClear != null)
		{
			cacheClear.onClick.AddListener(OnClearCache);
		}
	}

	private void OnClearCache()
	{
		if (cache == null)
		{
			return;
		}
		cache.Clear();
		Debug.Log("ULTRA кэш очищен игроком");
	}

	private void Update()
	{
		if (Input.GetKeyDown(panelKey) && panel != null)
		{
			panel.SetActive(!panel.activeSelf);
		}
		if (Input.GetKeyDown(ghostKey) && ghost != null)
		{
			ghost.show = !ghost.show;
		}
		UltraSolveQueue.Instance.Pump();
		UltraProgress progress = UltraSolveQueue.Instance.Progress;
		if (progressPanel != null)
		{
			bool running = progress.state == UltraJobState.Running;
			if (progressPanel.activeSelf != running)
			{
				progressPanel.SetActive(running);
			}
		}
		if (fraction != null)
		{
			fraction.text = UltraHudText.Fraction(progress);
		}
		if (elapsed != null)
		{
			elapsed.text = UltraHudText.Elapsed(progress);
		}
		if (eta != null)
		{
			eta.text = UltraHudText.Eta(progress);
		}
		if (panel != null && !panel.activeSelf)
		{
			return;
		}
		if (cacheSize != null && cache != null)
		{
			cacheSize.text = UltraHudText.CacheSize(cache.TotalBytes, cache.Count);
		}
		if (rungs != null)
		{
			rungs.text = UltraHudText.Rungs();
		}
		if (modeEntry != null)
		{
			ContinuumRouting.IsUltraUnlocked(null, out string reasonKey);
			modeEntry.text = UltraHudText.ModeEntry(reasonKey);
		}
		if (gateSet != null)
		{
			gateSet.text = UltraHudText.GateSet();
		}
		ProjectedScenario current = ((driver != null) ? driver.Current : null);
		if (declaration != null)
		{
			declaration.text = UltraHudText.DeclarationPanel(current) + UltraHudText.GhostSwitch(current, ghost != null && ghost.show, ghostKey.ToString());
		}
		else if (!declarationMissingSaid && current != null && (current.isLossy || current.layerCount > 1))
		{
			declarationMissingSaid = true;
			Debug.LogWarning("ULTRA: в сцене нет узла UltraHud/Panel/Declaration — полного текста объявления в панели не будет. Пересобери сцену просмотрщика: меню ArmorSim > Build ULTRA Viewer Scene.");
		}
		if (fieldSelector != null)
		{
			fieldSelector.text = UltraHudText.FieldName(field);
		}
		if (fieldLegend != null)
		{
			fieldLegend.text = UltraHudText.FieldLegend(field);
		}
	}

	public void CycleField()
	{
		field = (UltraField)(((int)field + 1) % 3);
	}

	public void ToggleViewMode()
	{
		if (orbit == null)
		{
			return;
		}
		orbit.viewMode = ((orbit.viewMode == UltraViewMode.Section) ? UltraViewMode.Exterior : UltraViewMode.Section);
		orbit.azimuthDeg = HalfLathe.ClampOrbitAzimuth(orbit.azimuthDeg, orbit.viewMode);
		orbit.Apply();
	}
}
