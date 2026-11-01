using UnityEngine;

/// <summary>
/// Утилита для работы с safe area (notch / Dynamic Island на iPhone).
/// UI, прижатый к краям экрана, должен смещаться на SafeAreaHelper.SafeArea.
/// </summary>
public static class SafeAreaHelper
{
	private static Rect? _cached;

	private static int _cachedWidth = -1;

	private static int _cachedHeight = -1;

	public static Rect SafeArea
	{
		get
		{
			if (_cached.HasValue && _cachedWidth == Screen.width && _cachedHeight == Screen.height)
			{
				return _cached.Value;
			}
#if UNITY_IOS
			_cached = Screen.safeArea;
#else
			_cached = new Rect(0f, 0f, Screen.width, Screen.height);
#endif
			_cachedWidth = Screen.width;
			_cachedHeight = Screen.height;
			return _cached.Value;
		}
	}

	/// <summary>
	/// Возвращает отступ (в пикселях экрана) левого нижнего угла safe area.
	/// Для RectTransform в Screen Space Overlay канвасе со ScaleWithScreenSize
	/// используйте offsetMin = SafeAreaHelper.CanvasSafeOffset(canvasScaler, canvasRect).
	/// </summary>
	public static Vector2 ApplySafeAreaOffset(RectTransform rt)
	{
		if (rt == null)
		{
			return Vector2.zero;
		}
		Rect safe = SafeArea;
		return new Vector2(safe.x, safe.y);
	}

	/// <summary>
	/// Переводит экранный safe area в координаты канваса (Reference Units),
	/// учитывая scaleFactor канваса — удобно для offsetMin/offsetMax корневых RectTransform.
	/// </summary>
	public static Vector2 CanvasSafeOffset(Canvas canvas)
	{
		if (canvas == null)
		{
			return Vector2.zero;
		}
		Rect safe = SafeArea;
		float scaleFactor = canvas.scaleFactor;
		if (scaleFactor <= 0f)
		{
			return Vector2.zero;
		}
		return new Vector2(safe.x / scaleFactor, safe.y / scaleFactor);
	}
}
