using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class FireButtonCaption
{
	public const string ObjectName = "FireButton";

	public static List<Button> Find()
	{
		List<Button> result = new List<Button>();
		Button[] array = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
		foreach (Button button in array)
		{
			int count = button.onClick.GetPersistentEventCount();
			bool fire = false;
			for (int i = 0; i < count; i++)
			{
				if (button.onClick.GetPersistentMethodName(i) == "Fire" && button.onClick.GetPersistentTarget(i) is Projectile)
				{
					fire = true;
					break;
				}
			}
			if (fire || button.name == ObjectName)
			{
				result.Add(button);
			}
		}
		return result;
	}

	public static int Apply(string key)
	{
		List<Button> list = Find();
		if (list.Count == 0)
		{
			return 0;
		}
		string text = Localization.T(key);
		int count = 0;
		foreach (Button button in list)
		{
			LocalizedLabel label = button.GetComponentInChildren<LocalizedLabel>();
			if (label != null)
			{
				label.key = key;
				label.Apply();
				count++;
			}
			else
			{
				TMP_Text tmp = button.GetComponentInChildren<TMP_Text>();
				if (tmp != null)
				{
					tmp.text = text;
					count++;
				}
				else
				{
					Text legacy = button.GetComponentInChildren<Text>();
					if (legacy != null)
					{
						legacy.text = text;
						count++;
					}
				}
			}
		}
		return count;
	}
}
