using TMPro;
using UnityEngine;

public sealed class RadarScenarioEntryCaption : MonoBehaviour
{
	public TMP_Text Label;

	private void OnEnable()
	{
		Localization.OnLanguageChanged += Refresh;
		if (Label != null)
		{
			Label.text = RadarScenarioText.Get("scenario.entry");
		}
	}

	private void OnDisable()
	{
		Localization.OnLanguageChanged -= Refresh;
	}

	public void Refresh()
	{
		if (Label != null)
		{
			Label.text = RadarScenarioText.Get("scenario.entry");
		}
	}
}
