using System;
using UnityEngine;

public sealed class AeroAccessRefresh : MonoBehaviour
{
	private bool observed;

	private Action changed;

	private float nextCheck;

	public void Configure(bool current, Action onChanged)
	{
		observed = current;
		changed = onChanged;
	}

	private void OnEnable()
	{
		CheckNow();
	}

	private void Update()
	{
		if (nextCheck > Time.unscaledTime)
		{
			return;
		}
		nextCheck = Time.unscaledTime + 0.25f;
		CheckNow();
	}

	public void CheckNow()
	{
		if (changed == null)
		{
			return;
		}
		bool activeNow = AeroFreeAccess.IsActiveNow;
		if (activeNow != observed)
		{
			observed = activeNow;
			changed();
		}
	}
}
