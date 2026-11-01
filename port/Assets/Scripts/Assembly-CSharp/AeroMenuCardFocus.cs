using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AeroMenuCardFocus : MonoBehaviour, ISelectHandler, IEventSystemHandler
{
	internal AeroMenuScroll owner;

	internal RectTransform card;

	public void OnSelect(BaseEventData eventData)
	{
	}
}
