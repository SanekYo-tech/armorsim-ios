using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LocalizedLabel : MonoBehaviour
{
	[Tooltip("Ключ из таблицы Localization, например menu.saves")]
	public string key;

	[Tooltip("Необязательный текст до/после перевода")]
	public string prefix = "";

	public string suffix = "";

	[Tooltip("Если true — текст переводится в ВЕРХНИЙ РЕГИСТР после подстановки")]
	public bool upperCase;

	private TMP_Text _tmp;

	private Text _legacy;

	private bool _cached;

	private void CacheTarget()
	{
		if (_cached)
		{
			return;
		}
		_cached = true;
		_tmp = GetComponent<TMP_Text>();
		if (_tmp == null)
		{
			_legacy = GetComponent<Text>();
		}
	}

	private void OnEnable()
	{
		Localization.OnLanguageChanged += Apply;
		Apply();
	}

	private void OnDisable()
	{
		Localization.OnLanguageChanged -= Apply;
	}

	public void SetKey(string newKey)
	{
		key = newKey;
		Apply();
	}

	public void Apply()
	{
		CacheTarget();
		string text = prefix + Localization.T(key) + suffix;
		if (upperCase)
		{
			text = text.ToUpperInvariant();
		}
		if (_tmp != null)
		{
			_tmp.text = text;
		}
		else if (_legacy != null)
		{
			_legacy.text = text;
		}
	}
}
