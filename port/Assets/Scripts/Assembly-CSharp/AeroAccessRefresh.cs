using System;
using UnityEngine;

public sealed class AeroAccessRefresh : MonoBehaviour
{
	private bool observed;

	private Action changed;

	private float nextCheck;

	public void Configure(bool current, Action onChanged)
	{
	}

	private void OnEnable()
	{
	}

	private void Update()
	{
	}

	public void CheckNow()
	{
	}
}
