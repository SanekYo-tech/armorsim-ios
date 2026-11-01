using System;
using TMPro;
using UnityEngine;

public sealed class AeroReadout : MonoBehaviour
{
	private string key;

	private object[] args = new object[0];

	private TMP_Text label;

	public void Set(string value, params object[] values)
	{
		key = value;
		args = values;
		if (label == null)
		{
			label = GetComponent<TMP_Text>();
		}
		if (label != null && key != null)
		{
			label.text = Localization.T(key, args);
		}
	}

	private void OnEnable()
	{
		Localization.OnLanguageChanged += Apply;
	}

	private void OnDisable()
	{
		Localization.OnLanguageChanged -= Apply;
	}

	private void Apply()
	{
		if (label == null)
		{
			label = GetComponent<TMP_Text>();
		}
		if (label != null && key != null)
		{
			label.text = Localization.T(key, args);
		}
	}

	public static AeroReadout Attach(TextMeshProUGUI label, string key)
	{
		AeroReadout component = label.GetComponent<AeroReadout>();
		if (component != null)
		{
			component.enabled = false;
		}
		AeroReadout aeroReadout = label.gameObject.AddComponent<AeroReadout>();
		aeroReadout.Set(key, Array.Empty<object>());
		return aeroReadout;
	}
}
