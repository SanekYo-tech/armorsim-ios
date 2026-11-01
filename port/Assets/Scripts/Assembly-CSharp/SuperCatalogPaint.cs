using System;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public sealed class SuperCatalogPaint : MonoBehaviour
{
	private static SuperCatalogPaint _instance;

	private int _spacingBits;

	private int _durationBits;

	private int _lang;

	private int _gridsHash;

	private bool _wasSuper;

	private Projectile _liveBody;

	private int _bodyBits;

	public static int Version { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Install()
	{
		if (_instance != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("SuperCatalogPaint");
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(gameObject);
		}
		_instance = gameObject.AddComponent<SuperCatalogPaint>();
	}

	public static void Ensure()
	{
		if (_instance != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("SuperCatalogPaint");
		gameObject.hideFlags = HideFlags.HideAndDontSave;
		if (Application.isPlaying)
		{
			Object.DontDestroyOnLoad(gameObject);
		}
		_instance = gameObject.AddComponent<SuperCatalogPaint>();
	}

	private void Update()
	{
		Repaint(force: false);
	}

	public static void RepaintNow()
	{
		if (_instance != null)
		{
			_instance.Repaint(force: true);
		}
	}

	private void Repaint(bool force)
	{
		bool wasSuper = AppFlow.ActiveMode == SimMode.Super;
		int mode = (int)AppFlow.ActiveMode;
		IReadOnlyList<CatalogGrid> live = CatalogGrid.Live;
		int gridsHash = live.Count;
		foreach (CatalogGrid g in live)
		{
			gridsHash = gridsHash * 31 + RuntimeHelpers.GetHashCode(g);
		}
		int spacingBits = BitConverter.SingleToInt32Bits(SuperPreset.SpacingM);
		int durationBits = BitConverter.SingleToInt32Bits(SuperPreset.DurationS);
		int lang = (int)Localization.Current;
		int bodyBits = 0;
		if (mode == (int)SimMode.Super && live.Count > 0)
		{
			if (_liveBody == null)
			{
				_liveBody = Object.FindFirstObjectByType<Projectile>();
			}
			if (_liveBody != null)
			{
				bodyBits = (((BitConverter.SingleToInt32Bits(_liveBody.diameter) * 31 + BitConverter.SingleToInt32Bits(_liveBody.rodLengthMM)) * 31 + BitConverter.SingleToInt32Bits(_liveBody.combatVelocity)) * 31 + BitConverter.SingleToInt32Bits(SuperPreset.TriggerTimeS)) * 31 + (int)_liveBody.coreType;
			}
		}
		if (!force && wasSuper == _wasSuper && spacingBits == _spacingBits && durationBits == _durationBits && lang == _lang && gridsHash == _gridsHash && bodyBits == _bodyBits)
		{
			return;
		}
		_wasSuper = wasSuper;
		_spacingBits = spacingBits;
		_durationBits = durationBits;
		_lang = lang;
		_gridsHash = gridsHash;
		_bodyBits = bodyBits;
		if (live.Count == 0)
		{
			return;
		}
		foreach (CatalogGrid g in live)
		{
			foreach (string id in new List<string>(g.Ids))
			{
				if (mode == (int)SimMode.Super)
				{
					SuperAvailability.Verdict verdict = SuperAvailability.ForId(id);
					g.SetRefused(id, (!verdict.ok) ? verdict.text : null);
					g.SetNote(id, verdict.ok ? verdict.note : null);
				}
				else
				{
					g.SetRefused(id, null);
					g.SetNote(id, null);
				}
			}
		}
		Version++;
	}

	public static List<KeyValuePair<string, string>> Report()
	{
		List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
		foreach (CatalogGrid g in CatalogGrid.Live)
		{
			foreach (string id in g.Ids)
			{
				SuperAvailability.Verdict verdict = SuperAvailability.ForId(id);
				result.Add(new KeyValuePair<string, string>(id, verdict.ok ? "" : verdict.text));
			}
		}
		return result;
	}

	public SuperCatalogPaint()
	{
		_spacingBits = int.MinValue;
		_durationBits = int.MinValue;
		_lang = -1;
		_bodyBits = int.MinValue;
	}
}
