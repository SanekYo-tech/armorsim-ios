#if !UNITY_IOS && UNITY_STANDALONE_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Steamworks;
using UnityEngine;
using System.Threading;

public sealed class SteamWorkshopBackend : IWorkshopBackend
{
	private const int TitleMax = 128;

	private const int DescriptionMax = 8000;

	public static string StagingRootOverride = "";

	public static bool SendItemTags = true;

	public static bool SendPreview = true;

	private Callback<DownloadItemResult_t> _downloadCallback;

	private CallResult<CreateItemResult_t> _createResult;

	private CallResult<SubmitItemUpdateResult_t> _submitResult;

	private WorkshopPublishRequest _pending;

	private Action<WorkshopPublishResult> _pendingDone;

	private string _pendingFolder = "";

	private string _pendingItemId = "";

	[CompilerGenerated]
	private Action m_ItemsChanged;

	public SteamWorkshopBackend()
	{
		if (SteamBoot.Initialized)
		{
			_downloadCallback = Callback<DownloadItemResult_t>.Create(OnDownloadFinished);
			_createResult = CallResult<CreateItemResult_t>.Create(OnItemCreated);
			_submitResult = CallResult<SubmitItemUpdateResult_t>.Create(OnItemSubmitted);
		}
	}

	public string Name => "Steam Workshop";

	public bool CanPublish => SteamBoot.Initialized;

	public string AuthorName
	{
		get
		{
			if (!SteamBoot.Initialized)
			{
				return "";
			}
			return SteamFriends.GetPersonaName();
		}
	}

	public event Action ItemsChanged
	{
		[CompilerGenerated]
		add
		{
			Action action2 = m_ItemsChanged;
			Action action3;
			do
			{
				action3 = action2;
				Action value2 = (Action)Delegate.Combine(action3, value);
				action2 = Interlocked.CompareExchange(ref m_ItemsChanged, value2, action3);
			}
			while (action2 != action3);
		}
		[CompilerGenerated]
		remove
		{
			Action action2 = m_ItemsChanged;
			Action action3;
			do
			{
				action3 = action2;
				Action value2 = (Action)Delegate.Remove(action3, value);
				action2 = Interlocked.CompareExchange(ref m_ItemsChanged, value2, action3);
			}
			while (action2 != action3);
		}
	}

	public void ListInstalled(Action<WorkshopListResult> done)
	{
		done = done ?? delegate
		{
		};
		WorkshopListResult workshopListResult = new WorkshopListResult();
		workshopListResult.where = "Steam Workshop";
		workshopListResult.emptyHintKey = "workshop.empty";
		if (SteamBoot.Initialized)
		{
			workshopListResult.ok = true;
			uint num = SteamUGC.GetNumSubscribedItems();
			if (num != 0)
			{
				PublishedFileId_t[] array = new PublishedFileId_t[num];
				uint num2 = SteamUGC.GetSubscribedItems(array, num, bIncludeLocallyDisabled: false);
				if (num2 != 0)
				{
					for (uint num3 = 0u; num3 < num2; num3++)
					{
						PublishedFileId_t publishedFileId = array[num3];
						WorkshopItem workshopItem = new WorkshopItem();
						workshopItem.publishedFileId = publishedFileId.m_PublishedFileId.ToString();
						uint num4 = SteamUGC.GetItemState(publishedFileId);
						workshopItem.installed = (num4 & 4) != 0;
						workshopItem.downloading = (num4 & 16) != 0 || (num4 & 32) != 0;
						if ((num4 & 8) != 0 || (!workshopItem.installed && !workshopItem.downloading))
						{
							if (SteamUGC.DownloadItem(publishedFileId, false))
							{
								workshopItem.downloading = true;
							}
							else if (!workshopItem.installed)
							{
								workshopItem.reasonKey = "workshop.reject.no_files";
								workshopItem.detail = "DownloadItem вернул false";
								workshopListResult.items.Add(workshopItem);
								continue;
							}
						}
						if (workshopItem.installed)
						{
							ulong num5;
							string text;
							uint num6;
							if (SteamUGC.GetItemInstallInfo(publishedFileId, out num5, out text, 1024u, out num6))
							{
								workshopItem.installFolder = text;
								workshopItem.sizeBytes = num5;
								WorkshopReadResult workshopReadResult = WorkshopPackage.ReadFolder(workshopItem.installFolder);
								if (workshopReadResult.Ok)
								{
									workshopItem.manifest = workshopReadResult.manifest;
								}
								else
								{
									workshopItem.reasonKey = workshopReadResult.reasonKey;
									workshopItem.detail = workshopReadResult.detail;
								}
							}
							else
							{
								workshopItem.installed = false;
								workshopItem.reasonKey = "workshop.reject.no_files";
								workshopItem.detail = "GetItemInstallInfo вернул false для " + workshopItem.publishedFileId;
								Debug.LogError("[Мастерская] " + workshopItem.detail);
							}
						}
						workshopListResult.items.Add(workshopItem);
					}
				}
			}
		}
		else
		{
			workshopListResult.ok = false;
			workshopListResult.reasonKey = "workshop.reject.no_steam";
			workshopListResult.detail = "SteamBoot.Initialized = false";
		}
		done(workshopListResult);
	}

	private void OnDownloadFinished(DownloadItemResult_t cb)
	{
		if (cb.m_unAppID != SteamUtils.GetAppID())
		{
			return;
		}
		if (cb.m_eResult == EResult.k_EResultOK)
		{
			Debug.Log("[Мастерская] предмет " + cb.m_nPublishedFileId.m_PublishedFileId.ToString() + " скачан.");
			m_ItemsChanged?.Invoke();
		}
		else
		{
			Debug.LogError("[Мастерская] предмет " + cb.m_nPublishedFileId.m_PublishedFileId.ToString() + " не скачался: " + cb.m_eResult);
		}
	}

	public void Publish(WorkshopPublishRequest request, Action<WorkshopPublishResult> done)
	{
		done = done ?? delegate
		{
		};
		if (!SteamBoot.Initialized)
		{
			done(Fail("workshop.reject.no_steam", "SteamBoot.Initialized = false"));
			return;
		}
		if (request == null || request.entry == null)
		{
			done(Fail("workshop.reject.nothing", "заявка пуста"));
			return;
		}
		if (_pending != null)
		{
			done(Fail("workshop.reject.busy", "идёт другая выгрузка"));
			return;
		}
		string text = Normalize(Path.Combine(StagingRoot(), string.IsNullOrEmpty(request.entry.id) ? "item" : request.entry.id));
		WorkshopManifest manifest = WorkshopPackage.ManifestFor(request.entry, request.title, request.description, AuthorName);
		string detail;
		if (!WorkshopPackage.WriteFolder(text, request.entry, manifest, out detail))
		{
			done(Fail("workshop.reject.write_failed", detail));
			return;
		}
		WarnIfNonAscii(text);
		_pending = request;
		_pendingDone = done;
		_pendingFolder = text;
		if (!string.IsNullOrEmpty(request.publishedFileId))
		{
			ulong num;
			if (ulong.TryParse(request.publishedFileId, out num))
			{
				SubmitContent(new PublishedFileId_t(num));
				return;
			}
			Finish(Fail("workshop.reject.failed", "нечитаемый id предмета: " + request.publishedFileId));
			return;
		}
		SteamAPICall_t hAPICall = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
		_createResult.Set(hAPICall);
		Debug.Log("[Мастерская] создаю новый предмет для «" + manifest.title + "»…");
	}

	private void OnItemCreated(CreateItemResult_t cb, bool ioFailure)
	{
		if (ioFailure)
		{
			Finish(Fail("workshop.reject.failed", "CreateItem: обрыв связи со Steam"));
			return;
		}
		if (cb.m_eResult != EResult.k_EResultOK)
		{
			Finish(Fail("workshop.reject.failed", "CreateItem: " + cb.m_eResult + ((cb.m_eResult == EResult.k_EResultAccessDenied) ? " — Steam не считает мастерскую действующей. Проверь в Steamworks: App Admin > вкладка «Мастерская» > «Общие» (мастерская настроена) И вкладку «Опубликовать» — НЕОПУБЛИКОВАННЫЕ настройки для Steam не существуют." : " (ответ Steam; код ни при чём не виноват заранее — сверься с документацией по EResult)")));
			return;
		}
		if (cb.m_bUserNeedsToAcceptWorkshopLegalAgreement)
		{
			Debug.LogWarning("[Мастерская] Steam требует принять соглашение мастерской — предмет не будет виден, пока оно не принято.");
		}
		SubmitContent(cb.m_nPublishedFileId);
	}

	private void SubmitContent(PublishedFileId_t id)
	{
		UGCUpdateHandle_t uGCUpdateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), id);
		if (uGCUpdateHandle == UGCUpdateHandle_t.Invalid)
		{
			Finish(Fail("workshop.reject.failed", "StartItemUpdate вернул НЕДЕЙСТВИТЕЛЬНЫЙ дескриптор для предмета " + id.m_PublishedFileId.ToString() + " — заявку на обновление Steam не открыл, и все последующие поля ушли бы в никуда"));
			return;
		}
		Debug.Log("[Мастерская] заявка открыта, дескриптор " + uGCUpdateHandle.m_UGCUpdateHandle.ToString());
		_pendingItemId = id.m_PublishedFileId.ToString();
		string text = Clamp(_pending.title, TitleMax, "заголовок");
		string text2 = Clamp(_pending.description, DescriptionMax, "описание");
		if (string.IsNullOrWhiteSpace(text))
		{
			Finish(Fail("workshop.reject.nothing", "у предмета пустой заголовок — отправка отменена (Steam отверг бы её кодом, который не называет поле)"));
			return;
		}
		if (!SteamUGC.SetItemTitle(uGCUpdateHandle, text))
		{
			Debug.LogError("[Мастерская] SetItemTitle вернул false.");
		}
		if (!string.IsNullOrEmpty(text2))
		{
			if (!SteamUGC.SetItemDescription(uGCUpdateHandle, text2))
			{
				Debug.LogError("[Мастерская] SetItemDescription вернул false.");
			}
		}
		Debug.Log("[Мастерская] отправляю: заголовок «" + text + "» (" + text.Length + " симв.), описание " + (string.IsNullOrEmpty(text2) ? "НЕ ЗАДАНО (вызов пропущен)" : (text2.Length + " симв.")) + ", метка " + (SendItemTags ? "«Armor layout»" : "не шлётся"));
		Debug.Log("[Мастерская] площадка выгрузки: " + _pendingFolder);
		if (!SteamUGC.SetItemContent(uGCUpdateHandle, _pendingFolder))
		{
			Finish(Fail("workshop.reject.write_failed", "SetItemContent отверг папку " + _pendingFolder + " — отправка отменена, чтобы отказ назывался там, где он возник"));
			return;
		}
		if (!SendItemTags)
		{
			Debug.LogWarning("[Мастерская] метка «Armor layout» НЕ отправляется (SendItemTags = false). Предмет уедет без метки и не найдётся поиском по ней. Завести метку: Steamworks > App Admin > Мастерская > Метки, затем вернуть SendItemTags = true.");
		}
		else
		{
			List<string> list = new List<string>();
			list.Add(WorkshopPackage.TagArmorLayout);
			if (!SteamUGC.SetItemTags(uGCUpdateHandle, list))
			{
				Debug.LogError("[Мастерская] SetItemTags вернул false.");
			}
		}
		string text3 = "";
		bool flag = false;
		if (SendPreview)
		{
			text3 = _pending.previewImagePath;
			if (!string.IsNullOrEmpty(text3))
			{
				flag = File.Exists(text3);
			}
			if (!flag)
			{
				text3 = MakePlaceholderPreview(id.m_PublishedFileId.ToString());
			}
		}
		else
		{
			Debug.LogWarning("[Мастерская] превью НЕ отправляется (SendPreview = false) — опыт: превью хранится в облаке игрока, и если выгрузка пройдёт БЕЗ него, виновата незаданная квота Steam Cloud, а не заявка.");
		}
		if (string.IsNullOrEmpty(text3))
		{
			if (SendPreview)
			{
				Debug.LogWarning("[Мастерская] превью не собралось — картинки у предмета не будет.");
			}
		}
		else
		{
			FileInfo fileInfo = new FileInfo(text3);
			Debug.Log("[Мастерская] превью: " + (flag ? "СНИМОК СБОРКИ" : "ЗАГЛУШКА (снимка нет — сохранение сделано до появления съёмки; пересохрани сборку)") + " — " + text3 + " (" + fileInfo.Length + " байт)");
			if (!SteamUGC.SetItemPreview(uGCUpdateHandle, text3))
			{
				Debug.LogError("[Мастерская] SetItemPreview вернул false для " + text3);
			}
		}
		if (string.IsNullOrEmpty(_pending.publishedFileId))
		{
			if (!SteamUGC.SetItemVisibility(uGCUpdateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic))
			{
				Debug.LogError("[Мастерская] SetItemVisibility вернул false.");
			}
			Debug.Log("[Мастерская] СОЗДАНИЕ (видимость выставлена «для всех»)");
		}
		else
		{
			Debug.Log("[Мастерская] ОБНОВЛЕНИЕ (видимость не трогаем)");
		}
		SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(uGCUpdateHandle, string.IsNullOrEmpty(_pending.changeNote) ? ("ArmorSim " + Application.version) : _pending.changeNote);
		_submitResult.Set(hAPICall);
		Debug.Log("[Мастерская] отправляю содержимое предмета " + id.m_PublishedFileId.ToString() + "…");
		ulong num;
		ulong num2;
		EItemUpdateStatus eItemUpdateStatus = SteamUGC.GetItemUpdateProgress(uGCUpdateHandle, out num, out num2);
		Debug.Log("[Мастерская] шаг сразу после подачи: " + eItemUpdateStatus + " (" + num + " из " + num2 + " байт)");
	}

	private void OnItemSubmitted(SubmitItemUpdateResult_t cb, bool ioFailure)
	{
		if (ioFailure)
		{
			Finish(Fail("workshop.reject.failed", "SubmitItemUpdate: обрыв связи со Steam"));
			return;
		}
		if (cb.m_eResult != EResult.k_EResultOK)
		{
			Finish(Fail("workshop.reject.failed", "SubmitItemUpdate: " + cb.m_eResult + "\n⚠️ КОД ОШИБКИ ПРИЧИНЫ НЕ НАЗЫВАЕТ. Настоящая причина — в журнале клиента Steam: <папка Steam>\\logs\\workshop_log.txt, последние строки «Upload workshop item … failed (…)». Там она написана словами."));
			return;
		}
		WorkshopPublishResult workshopPublishResult = new WorkshopPublishResult
		{
			ok = true,
			publishedFileId = cb.m_nPublishedFileId.m_PublishedFileId.ToString(),
			detail = "выложено",
			needsLegalAgreement = cb.m_bUserNeedsToAcceptWorkshopLegalAgreement
		};
		if (workshopPublishResult.needsLegalAgreement)
		{
			SteamFriends.ActivateGameOverlayToWebPage(WorkshopUrls.LegalAgreement());
			Debug.LogWarning("[Мастерская] предмет " + workshopPublishResult.publishedFileId + " создан, но НЕ ВИДЕН: не принято соглашение мастерской. Открыл страницу соглашения.");
		}
		else
		{
			Debug.Log("[Мастерская] предмет " + workshopPublishResult.publishedFileId + " выложен.");
		}
		Finish(workshopPublishResult);
	}

	public void OpenItemPage(string publishedFileId)
	{
		if (string.IsNullOrEmpty(publishedFileId))
		{
			return;
		}
		SteamFriends.ActivateGameOverlayToWebPage(WorkshopUrls.ItemPage(publishedFileId));
	}

	public void OpenHub()
	{
		SteamFriends.ActivateGameOverlayToWebPage(WorkshopUrls.Hub());
	}

	private static string Normalize(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}
		try
		{
			return Path.GetFullPath(path);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Мастерская] путь не приводится к полному виду (" + path + "): " + ex.Message);
			return path;
		}
	}

	private static string StagingRoot()
	{
		if (!string.IsNullOrEmpty(StagingRootOverride))
		{
			return Normalize(StagingRootOverride);
		}
		string text = Path.Combine(Application.persistentDataPath, "ArmorSim", "WorkshopUpload");
		if (!IsAscii(text))
		{
			string text2 = Path.Combine(Path.GetPathRoot(Application.persistentDataPath) ?? "C:\\", "ArmorSimWorkshop");
			Debug.LogWarning("[Мастерская] обычная площадка выгрузки содержит не-ASCII символы:\n  " + text + "\nПеренесена на " + text2 + " — steamcmd на таких путях ОТКАЗЫВАЕТ, про SteamUGC это не измерено, а цена ошибки выше цены переноса. Вернуть прежнее место можно полем SteamWorkshopBackend.StagingRootOverride.");
			return Normalize(text2);
		}
		return Normalize(text);
	}

	private static string MakePlaceholderPreview(string itemId)
	{
		Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
		try
		{
			Color32[] array = new Color32[262144];
			for (int i = 0; i < 512; i++)
			{
				for (int j = 0; j < 512; j++)
				{
					array[i * 512 + j] = ((j < 10 || i < 10 || j >= 502 || i >= 502) ? new Color32(60, 60, 64, byte.MaxValue) : new Color32(237, 237, 237, byte.MaxValue));
				}
			}
			texture.SetPixels32(array);
			texture.Apply();
			byte[] array2 = texture.EncodeToPNG();
			if (array2 == null || array2.Length == 0)
			{
				Debug.LogError("[Мастерская] EncodeToPNG вернул пусто — превью не собрано.");
				return "";
			}
			string text = StagingRoot();
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			string text2 = Normalize(Path.Combine(text, itemId + ".preview.png"));
			File.WriteAllBytes(text2, array2);
			return text2;
		}
		catch (Exception ex)
		{
			Debug.LogError("[Мастерская] превью не собралось: " + ex.Message);
			return "";
		}
		finally
		{
			if (texture != null)
			{
				UnityEngine.Object.Destroy(texture);
			}
		}
	}

	private static bool IsAscii(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return true;
		}
		for (int i = 0; i < s.Length; i++)
		{
			if (s[i] > 127)
			{
				return false;
			}
		}
		return true;
	}

	private static string StagingFolderFor(SaveEntry entry)
	{
		string text = (entry == null || string.IsNullOrEmpty(entry.id)) ? "item" : entry.id;
		return Normalize(Path.Combine(StagingRoot(), text));
	}

	private static void WarnIfNonAscii(string path)
	{
		if (!IsAscii(path))
		{
			Debug.LogWarning("[Мастерская] заданная вручную площадка выгрузки содержит не-ASCII символы: " + path + ". steamcmd на таких путях ОТКАЗЫВАЕТ (см. Steam/scripts/app_build_5159810.vdf); про SteamUGC это не измерено. Если выгрузка отказывает — убери StagingRootOverride, и папка уедет на путь из одной латиницы сама.");
		}
	}

	private static string Clamp(string s, int max, string what)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s ?? "";
		}
		if (s.Length <= max)
		{
			return s;
		}
		Debug.LogWarning("[Мастерская] " + what + " обрезан(о) до " + max + " символов (было " + s.Length + ").");
		return s.Substring(0, max);
	}

	private static WorkshopPublishResult Fail(string reasonKey, string detail)
	{
		Debug.LogError("[Мастерская] выгрузка не удалась: " + detail);
		return new WorkshopPublishResult
		{
			ok = false,
			reasonKey = reasonKey,
			detail = detail
		};
	}

	private void Finish(WorkshopPublishResult result)
	{
		if (result != null && string.IsNullOrEmpty(result.publishedFileId) && !string.IsNullOrEmpty(_pendingItemId))
		{
			result.publishedFileId = _pendingItemId;
			if (!result.ok)
			{
				Debug.LogWarning("[Мастерская] предмет " + _pendingItemId + " создан, но не заполнен. Связь сохранена: следующая попытка ДОПОЛНИТ его, а не создаст новый.");
			}
		}
		Action<WorkshopPublishResult> action = _pendingDone;
		_pending = null;
		_pendingFolder = "";
		_pendingItemId = "";
		action?.Invoke(result);
	}
}
#endif
