using UnityEngine;
using UnityEngine.Rendering;

public class SceneVisuals : MonoBehaviour
{
	public bool applyOnStart = true;

	[Header("Фон (светло-серый — референс ANSYS/Autodyn)")]
	public Color backgroundColor = ResultPalette.Background;

	[Header("Ambient — ПЛОСКИЙ, чуть ТЕМНЕЕ фона (объект сидит в поле, а не парит над ним)")]
	public Color ambientFlat = new Color(0.5f, 0.5f, 0.51f, 1f);

	[Header("Ключевой свет")]
	public float keyIntensity = 0.9f;

	public Color keyColor = new Color(1f, 0.99f, 0.97f, 1f);

	[Header("Заполняющий (противоположный, теневые грани читаемы, а не чёрные)")]
	public float fillIntensity = 0.3f;

	public Color fillColor = new Color(0.94f, 0.96f, 1f, 1f);

	[Header("Контровой — на светлом поле почти не нужен")]
	public float rimIntensity = 0.1f;

	[Header("Тени — мягкие и СЛАБЫЕ: притеняют, а не пробивают дыры в светлом поле")]
	[Range(0f, 1f)]
	public float shadowStrength = 0.35f;

	public static void Bootstrap()
	{
		if (Object.FindFirstObjectByType<SceneVisuals>() != null)
		{
			return;
		}
		new GameObject("SceneVisuals").AddComponent<SceneVisuals>();
	}

	private void Start()
	{
		if (applyOnStart)
		{
			Apply();
		}
	}

	public void Apply()
	{
		RenderSettings.ambientMode = AmbientMode.Flat;
		RenderSettings.ambientLight = ambientFlat;
		RenderSettings.ambientIntensity = 1f;
		RenderSettings.skybox = null;
		Light light = null;
		Light[] array = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			Light light2 = array[i];
			if (light2.type == LightType.Directional)
			{
				light = light2;
				break;
			}
		}
		if (light == null)
		{
			GameObject gameObject = new GameObject("Key Light");
			light = gameObject.AddComponent<Light>();
			light.type = LightType.Directional;
			gameObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
		}
		light.intensity = keyIntensity;
		light.color = keyColor;
		light.shadows = LightShadows.Soft;
		light.shadowStrength = shadowStrength;
		light.shadowResolution = LightShadowResolution.VeryHigh;
		QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
		QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 60f);
		Light light3 = EnsureChildLight("Fill Light", light.transform.rotation * Quaternion.Euler(0f, 160f, 0f));
		light3.intensity = fillIntensity;
		light3.color = fillColor;
		light3.shadows = LightShadows.None;
		Light light4 = EnsureChildLight("Rim Light", Quaternion.Euler(-20f, 150f, 0f));
		light4.intensity = rimIntensity;
		light4.color = new Color(0.95f, 0.96f, 1f, 1f);
		light4.shadows = LightShadows.None;
		Camera[] array2 = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
		for (int j = 0; j < array2.Length; j++)
		{
			Camera camera = array2[j];
			if (camera.clearFlags != CameraClearFlags.Depth && camera.clearFlags != CameraClearFlags.Nothing)
			{
				camera.clearFlags = CameraClearFlags.SolidColor;
				camera.backgroundColor = backgroundColor;
			}
		}
		Debug.Log("[SceneVisuals] Риг под СВЕТЛОЕ поле: фон " + ColorUtility.ToHtmlStringRGB(backgroundColor) + ", " + string.Format("ambient ПЛОСКИЙ {0}, key {1:F2}, ", ColorUtility.ToHtmlStringRGB(ambientFlat), keyIntensity) + string.Format("fill {0:F2}, rim {1:F2}, тени мягкие {2:F2}. ", fillIntensity, rimIntensity, shadowStrength) + "Блума/AO/grading/виньетки нет и не добавляется — ровный вид и есть цель.");
	}

	private Light EnsureChildLight(string name, Quaternion rot)
	{
		Transform transform = base.transform.Find(name);
		GameObject gameObject;
		if (transform != null)
		{
			gameObject = transform.gameObject;
		}
		else
		{
			gameObject = new GameObject(name);
			gameObject.transform.SetParent(base.transform, false);
		}
		gameObject.transform.rotation = rot;
		Light light = gameObject.GetComponent<Light>();
		if (light == null)
		{
			light = gameObject.AddComponent<Light>();
		}
		light.type = LightType.Directional;
		return light;
	}
}
