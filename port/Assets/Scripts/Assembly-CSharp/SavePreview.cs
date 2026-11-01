using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SavePreview
{
	public const int Side = 512;

	public const int MaxBytes = 921600;

	private static readonly Color Background = new Color(0.93f, 0.93f, 0.93f, 1f);

	public static string PathFor(string saveId)
	{
		if (string.IsNullOrEmpty(saveId))
		{
			return "";
		}
		return Path.Combine(SaveSystem.SavesDir, saveId + ".png");
	}

	public static bool Exists(string saveId)
	{
		string text = PathFor(saveId);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		return File.Exists(text);
	}

	public static string ForPublish(string saveId)
	{
		string text = PathFor(saveId);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return "";
		}
		return PathFor(saveId);
	}

	public static void Delete(string saveId)
	{
		string text = PathFor(saveId);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return;
		}
		try
		{
			File.Delete(text);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Снимок] не удалось удалить снимок сохранения " + saveId + ": " + ex.Message);
		}
	}

	public static bool Capture(string saveId, out string detail)
	{
		detail = "";
		if (string.IsNullOrEmpty(saveId))
		{
			detail = "нет id сохранения";
			return false;
		}
		if (!SceneBounds.TryArmour(out Bounds bounds))
		{
			detail = "в сцене нет видимой брони";
			return false;
		}
		Vector3 vector = ViewDirection(bounds.center);
		float num = Mathf.Max(bounds.extents.magnitude, 0.05f);
		float num2 = num / Mathf.Sin(35f * 0.5f * Mathf.Deg2Rad) * SceneDefaults.CameraBackMargin;
		Vector3 vector2 = bounds.center + vector * num2;
		GameObject gameObject = new GameObject("SavePreviewCamera");
		Texture2D texture2D = null;
		RenderTexture renderTexture = null;
		try
		{
			gameObject.hideFlags = HideFlags.HideAndDontSave;
			Camera camera = gameObject.AddComponent<Camera>();
			camera.cullingMask = ((Camera.main != null) ? Camera.main.cullingMask : -1);
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = Background;
			camera.orthographic = false;
			camera.fieldOfView = 35f;
			camera.nearClipPlane = Mathf.Max(0.01f, num2 * 0.001f);
			camera.farClipPlane = num * 4f + num2 * 4f;
			gameObject.transform.position = vector2;
			gameObject.transform.LookAt(bounds.center);
			renderTexture = new RenderTexture(Side, Side, 24);
			renderTexture.antiAliasing = 4;
			camera.targetTexture = renderTexture;
			camera.Render();
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			texture2D = new Texture2D(Side, Side, TextureFormat.RGB24, false);
			texture2D.ReadPixels(new Rect(0f, 0f, Side, Side), 0, 0);
			texture2D.Apply(false);
			RenderTexture.active = active;
			camera.targetTexture = null;
			byte[] array = texture2D.EncodeToPNG();
			string text = "PNG";
			if (array != null && array.Length > MaxBytes)
			{
				byte[] array2 = texture2D.EncodeToJPG(85);
				if (array2 != null && (array == null || array2.Length < array.Length))
				{
					array = array2;
					text = "JPEG (PNG не уложился в " + MaxBytes + " байт)";
				}
			}
			if (array == null || array.Length == 0)
			{
				detail = "кодирование вернуло пусто";
				return false;
			}
			string text2 = PathFor(saveId);
			string directoryName = Path.GetDirectoryName(text2);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			File.WriteAllBytes(text2, array);
			detail = text2 + " (" + text + ", " + array.Length + " байт)";
			Debug.Log("[Снимок] сборка снята: " + detail);
			return true;
		}
		catch (Exception ex)
		{
			detail = "съёмка не удалась: " + ex.Message;
			Debug.LogWarning("[Снимок] " + detail);
			return false;
		}
		finally
		{
			if (texture2D != null)
			{
				Object.Destroy(texture2D);
			}
			if (renderTexture != null)
			{
				renderTexture.Release();
				Object.Destroy(renderTexture);
			}
			if (gameObject != null)
			{
				Object.Destroy(gameObject);
			}
		}
	}

	public static bool ComputeView(out Bounds bounds, out Vector3 camPos, out float fovDeg, out float distance, out float radius)
	{
		camPos = Vector3.zero;
		fovDeg = 35f;
		distance = 0f;
		radius = 0f;
		if (!SceneBounds.TryArmour(out bounds))
		{
			return false;
		}
		Vector3 vector = ViewDirection(bounds.center);
		radius = Mathf.Max(bounds.extents.magnitude, 0.05f);
		distance = radius / Mathf.Sin(fovDeg * 0.5f * Mathf.Deg2Rad) * SceneDefaults.CameraBackMargin;
		camPos = bounds.center + vector * distance;
		return true;
	}

	private static Vector3 ViewDirection(Vector3 center)
	{
		Vector3 vector = Vector3.back;
		Projectile projectile = Object.FindFirstObjectByType<Projectile>();
		if (projectile != null)
		{
			Vector3 vector2 = new Vector3(projectile.StartPositionWorld.x - center.x, 0f, projectile.StartPositionWorld.z - center.z);
			if (vector2.sqrMagnitude > 1e-06f)
			{
				vector = vector2.normalized;
			}
		}
		vector = Quaternion.AngleAxis(30f, Vector3.up) * vector + Vector3.up * 0.45f;
		return vector.normalized;
	}
}
