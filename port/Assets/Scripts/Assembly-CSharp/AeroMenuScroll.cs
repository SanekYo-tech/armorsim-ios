using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class AeroMenuScroll : MonoBehaviour
{
	private ScrollRect scroll;

	private readonly List<Button> buttons;

	private readonly List<UnityAction> actions;

	private readonly List<AeroMenuCardFocus> focusHandlers;

	public AeroMenuScroll()
	{
		buttons = new List<Button>();
		actions = new List<UnityAction>();
		focusHandlers = new List<AeroMenuCardFocus>();
	}

	public void Configure(ScrollRect target)
	{
	}

	public void Reveal(RectTransform card)
	{
	}

	private void Unbind()
	{
	}

	private void OnDestroy()
	{
		Unbind();
	}
}
