using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class CrewPanelText : MonoBehaviour
{
	private static readonly Dictionary<string, string[]> Rows = new Dictionary<string, string[]>
	{
		{
			"section",
			new string[5] { "Разрез", "Cutaway", "Кисем", "Schnittansicht", "Przekrój" }
		},
		{
			"organs",
			new string[5] { "Внутренние органы", "Internal organs", "Эчке әгъзалар", "Innere Organe", "Organy wewnętrzne" }
		},
		{
			"highlight",
			new string[5] { "Подсветка попаданий", "Highlight hits", "Эләгүләрне яктырту", "Treffer markieren", "Podświetl trafienia" }
		},
		{
			"organ_hits",
			new string[5] { "Задеты органы", "Organs hit", "Зыян күргән әгъзалар", "Getroffene Organe", "Trafione organy" }
		},
		{
			"none",
			new string[5] { "Нет попаданий в органы", "No organ hits", "Әгъзаларга эләгүләр юк", "Keine Organtreffer", "Brak trafień w organy" }
		},
		{
			"organs_hint",
			new string[5]
			{
				"Показ внутренних органов манекенов экипажа.",
				"Show internal organs of crew mannequins.",
				"Экипаж манекеннарының эчке әгъзаларын күрсәтү.",
				"Innere Organe der Besatzungsfiguren anzeigen.",
				"Pokaż organy wewnętrzne manekinów załogi."
			}
		},
		{
			"highlight_title",
			new string[5] { "Подсветка задетых органов", "Highlight organs hit", "Зыян күргән әгъзаларны яктырту", "Getroffene Organe hervorheben", "Podświetl trafione organy" }
		},
		{
			"highlight_hint",
			new string[5]
			{
				"Задетые органы краснеют. Учёт попаданий работает при любом положении переключателя.",
				"Organs turn red when hit. Hits are recorded with either setting.",
				"Зыян күргән әгъзалар кызыллана. Эләгүләр һәрвакыт исәпләнә.",
				"Getroffene Organe werden rot. Treffer werden bei beiden Einstellungen erfasst.",
				"Trafione organy stają się czerwone. Trafienia są rejestrowane przy obu ustawieniach."
			}
		}
	};

	private string key;

	private TMP_Text label;

	public static string T(string key)
	{
		if (Rows.TryGetValue(key, out string[] value))
		{
			int num = (int)Localization.Current;
			if (num < 0 || num >= value.Length)
			{
				num = 1;
			}
			return value[num];
		}
		throw new ArgumentException("Unknown crew UI label: " + key, "key");
	}

	public static void Bind(TMP_Text text, string key)
	{
		CrewPanelText crewPanelText = text.gameObject.AddComponent<CrewPanelText>();
		crewPanelText.label = text;
		crewPanelText.key = key;
		crewPanelText.Refresh();
	}

	private void OnEnable()
	{
		Localization.OnLanguageChanged += Refresh;
		Refresh();
	}

	private void OnDisable()
	{
		Localization.OnLanguageChanged -= Refresh;
	}

	private void Refresh()
	{
		if (label != null && key != null)
		{
			label.text = T(key);
		}
	}
}
