using UnityEngine;
using UnityEngine.EventSystems;

public class DrawSurface : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IDragHandler, IScrollHandler, IPointerDownHandler, IPointerUpHandler
{
	public Flat2DDrawScreen screen;

	public void OnPointerDown(PointerEventData e)
	{
		if (e.button == PointerEventData.InputButton.Left && screen != null)
		{
			screen.SurfacePress(e.position, e.pressEventCamera);
		}
	}

	public void OnPointerUp(PointerEventData e)
	{
		if (e.button == PointerEventData.InputButton.Left && screen != null)
		{
			screen.SurfaceRelease();
		}
	}

	public void OnPointerClick(PointerEventData e)
	{
		if (e.button == PointerEventData.InputButton.Left && !e.dragging && screen != null)
		{
			screen.SurfaceClick(e.position, e.pressEventCamera);
		}
	}

	public void OnDrag(PointerEventData e)
	{
		if (e.button == PointerEventData.InputButton.Left && screen != null && screen.SurfaceDragPaint(e.position, e.pressEventCamera))
		{
			return;
		}
		if (screen != null)
		{
			screen.SurfacePan(e.delta);
		}
	}

	public void OnScroll(PointerEventData e)
	{
		if (screen != null)
		{
			screen.SurfaceZoom(Flat2DDrawScreen.WheelNotches(e.scrollDelta.y), e.position, e.pressEventCamera);
		}
	}
}
