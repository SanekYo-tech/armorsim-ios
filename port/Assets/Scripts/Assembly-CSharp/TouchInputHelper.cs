using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Единая точка доступа к «основному» указателю для мыши и тач-экрана.
/// На устройствах с тачем приоритет у пальцев, на десктопе всё падает обратно в мышь,
/// поэтому существующая мышиная логика не меняется — меняется только источник координат.
/// </summary>
public static class TouchInputHelper
{
	/// <summary>Пикселей движения пальца, эквивалентных одной единице оси «Mouse X/Y» (чувствительность 0.1).</summary>
	public const float PixelsPerAxisUnit = 10f;

	/// <summary>Пикселей изменения дистанции между пальцами, эквивалентных одному щелчку колеса мыши.</summary>
	public const float PinchPixelsPerNotch = 120f;

	/// <summary>Устройство с тач-вводом (iOS/Android и т.п.).</summary>
	public static bool IsTouchDevice => Input.touchSupported;

	/// <summary>В этом кадре есть активные пальцы — тач-жесты имеют приоритет над мышью.</summary>
	public static bool TouchActive => IsTouchDevice && Input.touchCount > 0;

	/// <summary>Нажатие основного указателя: касание началось ИЛИ левая кнопка мыши нажата.</summary>
	public static bool PrimaryDown => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);

	/// <summary>Указатель удерживается: палец на экране (движется/стоит) ИЛИ левая кнопка мыши зажата.</summary>
	public static bool PrimaryHeld => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(0).phase == TouchPhase.Stationary : Input.GetMouseButton(0);

	/// <summary>Указатель отпущен: касание закончилось ИЛИ левая кнопка мыши отпущена.</summary>
	public static bool PrimaryUp => Input.touchCount > 0 ? Input.GetTouch(0).phase == TouchPhase.Ended : Input.GetMouseButtonUp(0);

	/// <summary>Экранная позиция основного указателя (пиксели).</summary>
	public static Vector2 PrimaryPosition => Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

	/// <summary>Дельта основного указателя за кадр: для тача — пиксели, для мыши — оси «Mouse X/Y».</summary>
	public static Vector2 PrimaryDelta => Input.touchCount > 0 ? Input.GetTouch(0).deltaPosition : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

	/// <summary>Дельта основного указателя, нормированная как мышиные оси (1 единица ≈ 10 пикселей) — одинаковый масштаб для мыши и тача.</summary>
	public static Vector2 PrimaryAxisDelta => Input.touchCount > 0 ? PrimaryDelta / PixelsPerAxisUnit : PrimaryDelta;

	/// <summary>Изменение дистанции между двумя пальцами за кадр (пиксели). 0 — если пальцев не два.</summary>
	public static float PinchDelta
	{
		get
		{
			if (Input.touchCount != 2)
			{
				return 0f;
			}
			Touch t0 = Input.GetTouch(0);
			Touch t1 = Input.GetTouch(1);
			float prevDist = Vector2.Distance(t0.position - t0.deltaPosition, t1.position - t1.deltaPosition);
			float currDist = Vector2.Distance(t0.position, t1.position);
			return currDist - prevDist;
		}
	}

	/// <summary>Пинч, выраженный в «щелчках колеса мыши» — можно подставлять в существующую мыиную логику зума.</summary>
	public static float PinchNotches => PinchDelta / PinchPixelsPerNotch;

	/// <summary>Средняя дельта двух пальцев за кадр (пиксели) — жест «двумя пальцами» (пан).</summary>
	public static Vector2 TwoFingerDelta
	{
		get
		{
			if (Input.touchCount < 2)
			{
				return Vector2.zero;
			}
			Touch t0 = Input.GetTouch(0);
			Touch t1 = Input.GetTouch(1);
			return (t0.deltaPosition + t1.deltaPosition) * 0.5f;
		}
	}

	/// <summary>Средняя дельта двух пальцев, нормированная как мышиные оси (1 единица ≈ 10 пикселей).</summary>
	public static Vector2 TwoFingerAxisDelta => TwoFingerDelta / PixelsPerAxisUnit;

	/// <summary>Указатель сейчас над UI: для тача — проверка по fingerId, для мыши — обычная проверка pointerId -1.</summary>
	public static bool PointerOverUI
	{
		get
		{
			if (EventSystem.current == null)
			{
				return false;
			}
			if (Input.touchCount > 0)
			{
				return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
			}
			return EventSystem.current.IsPointerOverGameObject();
		}
	}
}
