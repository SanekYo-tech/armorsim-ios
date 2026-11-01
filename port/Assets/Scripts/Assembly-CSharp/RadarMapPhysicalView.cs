using System.Globalization;
using UnityEngine;

public sealed class RadarMapPhysicalView
{
	public bool ObserverOpen;

	public RadarMapSession Session;

	private GUIStyle text;

	private GUIStyle button;

	private static string L(string key)
	{
		return RadarScenarioText.Get(key);
	}

	public Rect Draw(Rect area, double gameSeconds)
	{
		if (button == null)
		{
			text = new GUIStyle(GUI.skin.label);
			text.fontSize = 17;
			text.normal.textColor = new Color(0.62f, 0.85f, 0.66f, 1f);
			button = new GUIStyle(GUI.skin.button);
			button.fontSize = 18;
		}
		GUI.Label(new Rect(area.x + 20f, area.y + area.height - 34f, 400f, 28f), L("map.phys.time") + " " + gameSeconds.ToString("F1", CultureInfo.InvariantCulture) + " s", text);
		Color backgroundColor = GUI.backgroundColor;
		if (ObserverOpen)
		{
			GUI.backgroundColor = new Color(0.95f, 0.55f, 0.45f, 1f);
		}
		if (GUI.Button(new Rect(area.x + area.width - 300f, area.y + area.height - 50f, 280f, 42f), L("map.obs.title"), button))
		{
			ObserverOpen = !ObserverOpen;
		}
		GUI.backgroundColor = backgroundColor;
		return new Rect(area.x + area.width - 660f, area.y + 8f, 512f, area.height - 66f);
	}
}
