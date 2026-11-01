using System;
using UnityEngine;

public class SandboxSessionController : MonoBehaviour
{
	[Tooltip("Объект, реализующий ISimulationSnapshot. Пусто — найдётся автоматически в сцене.")]
	public MonoBehaviour snapshotProvider;

	[Tooltip("Возврат в меню по Esc. ВЫКЛЮЧЕНО: Esc уже снимает выделение (PlateSelector).")]
	public bool escapeReturnsToMenu;

	private ISimulationSnapshot _snapshot;

	public static SimMode ActiveMode => AppFlow.ActiveMode;

	private ISimulationSnapshot Snapshot()
	{
		if (_snapshot != null)
		{
			return _snapshot;
		}
		_snapshot = snapshotProvider as ISimulationSnapshot;
		if (_snapshot != null)
		{
			return _snapshot;
		}
		foreach (MonoBehaviour monoBehaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
		{
			_snapshot = monoBehaviour as ISimulationSnapshot;
			if (_snapshot != null)
			{
				return _snapshot;
			}
		}
		return _snapshot;
	}

	private void ApplyModeToProjectile()
	{
		Projectile projectile = UnityEngine.Object.FindFirstObjectByType<Projectile>();
		if (projectile == null)
		{
			return;
		}
		Projectile.QualityMode qualityMode = ((AppFlow.ActiveMode == SimMode.Advanced) ? Projectile.QualityMode.Продвинутое : Projectile.QualityMode.Обычное);
		if (projectile.quality != qualityMode)
		{
			projectile.quality = qualityMode;
			Debug.Log(string.Format("[SandboxSession] Режим расчёта из меню: {0} -> качество {1}. ", AppFlow.ActiveMode, qualityMode) + "ВНИМАНИЕ: продвинутый режим включает решётку ДЗ (EraRodLattice) и МЕНЯЕТ числа выстрела, а не только показ.");
		}
	}

	private void Awake()
	{
		ApplyModeToProjectile();
	}

	private void ApplyModeToFireButton()
	{
		if (AppFlow.ActiveMode == SimMode.Super)
		{
			return;
		}
		string text = "sandbox.fire";
		if (AppFlow.ActiveMode == SimMode.Ultra)
		{
			text = "sandbox.build_ultra";
		}
		if (FireButtonCaption.Apply(text) != 0)
		{
			return;
		}
		Debug.LogWarning("[Песочница] Кнопка выстрела в сцене не найдена — подпись «" + Localization.T(text) + "» не поставлена. Режим: " + AppFlow.ActiveMode);
	}

	private void Start()
	{
		ApplyModeToFireButton();
		_snapshot = Snapshot();
		SaveEntry saveEntry = AppFlow.PendingLoad;
		AppFlow.PendingLoad = null;
		if (saveEntry == null)
		{
			return;
		}
		try
		{
			if (_snapshot == null)
			{
				Debug.LogError("[SandboxSession] В сцене нет объекта с ISimulationSnapshot — загрузить сохранение «" + saveEntry.displayName + "» некуда.");
				return;
			}
			if (saveEntry.payloadVersion > 1)
			{
				Debug.LogError("[SandboxSession] Сохранение сделано более новой версией игры (payloadVersion " + saveEntry.payloadVersion + " > " + 1 + ").");
				return;
			}
			_snapshot.RestoreJson(saveEntry.payload);
		}
		catch (Exception ex)
		{
			Debug.LogError("[SandboxSession] Не удалось развернуть сохранение: " + ex.Message);
		}
	}

	private void Update()
	{
		if (escapeReturnsToMenu && Input.GetKeyDown(KeyCode.Escape) && !PlateGizmo.CancelOnEscape())
		{
			AppFlow.OpenMenu();
		}
	}

	public SaveEntry SaveCurrent(string displayName)
	{
		try
		{
			ISimulationSnapshot simulationSnapshot = Snapshot();
			if (simulationSnapshot == null)
			{
				Debug.LogError("[SandboxSession] Нечего сохранять: ISimulationSnapshot не найден.");
				return null;
			}
			string json = simulationSnapshot.CaptureJson();
			int objectCount = simulationSnapshot.ObjectCount;
			SaveEntry saveEntry = SaveSystem.Create(displayName, json, objectCount, AppFlow.ActiveSection, SimFamily.Analytic3D);
			Debug.Log("[SandboxSession] Сохранено: " + saveEntry.displayName + " (" + saveEntry.id + ")");
			if (!SavePreview.Capture(saveEntry.id, out var detail))
			{
				Debug.LogWarning("[SandboxSession] снимок сборки не снят (" + detail + "); выгрузка в мастерскую уедет с заглушкой вместо картинки.");
			}
			return saveEntry;
		}
		catch (Exception ex)
		{
			Debug.LogError("[SandboxSession] Снимок сцены не собрался: " + ex.Message);
			return null;
		}
	}

	public bool LoadEntry(SaveEntry entry, out string message)
	{
		if (entry == null)
		{
			message = "Нечего загружать.";
			return false;
		}
		try
		{
			ISimulationSnapshot simulationSnapshot = Snapshot();
			if (simulationSnapshot == null)
			{
				message = "В сцене нет объекта с ISimulationSnapshot — загружать некуда.";
				Debug.LogError("[SandboxSession] " + message);
				return false;
			}
			if (entry.payloadVersion > 1)
			{
				message = "Сохранение сделано более новой версией игры (payloadVersion " + string.Format("{0} > {1}).", entry.payloadVersion, 1);
				Debug.LogError("[SandboxSession] " + message);
				return false;
			}
			simulationSnapshot.RestoreJson(entry.payload);
			SceneSerializer sceneSerializer = simulationSnapshot as SceneSerializer;
			if (sceneSerializer != null && !string.IsNullOrEmpty(sceneSerializer.LastStatus))
			{
				message = sceneSerializer.LastStatus;
			}
			else
			{
				message = "Загружено: " + entry.displayName;
			}
			return true;
		}
		catch (Exception ex)
		{
			message = "Не удалось развернуть сохранение: " + ex.Message;
			Debug.LogError("[SandboxSession] " + message);
			return false;
		}
	}

	public void ReturnToMenu()
	{
		AppFlow.OpenMenu();
	}
}
