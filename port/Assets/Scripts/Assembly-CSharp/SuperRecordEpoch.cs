using System.Text;

public static class SuperRecordEpoch
{
	public enum Instrument
	{
		LadderStep = 0,
		Bisection = 1,
		SingleWitnessDepth = 2,
		RulerN1 = 3,
		Sigma1Withdrawn = 4,
		BeforeSigma1 = 5,
		InfinitePlaneThrough = 6
	}

	public struct Entry
	{
		public string id;

		public Instrument instrument;

		public string note;
	}

	public static readonly Entry[] Records = new Entry[11]
	{
		new Entry
		{
			id = "R11-ladder",
			instrument = Instrument.LadderStep,
			note = "лестница 10° 30.08: T(1100) = T(1500) = «40°» — это ступень, на которой сменился вердикт, а не угол перехода; эффект 13.75° прятался за шагом"
		},
		new Entry
		{
			id = "R11-bisection",
			instrument = Instrument.Bisection,
			note = "01.09, разрешение ⩽2°: T(1100) = 23.13°, T(1500) = 36.88°, скобки разъединены, направление Розенберга; снято ДО σ₁ и после его отзыва в силе"
		},
		new Entry
		{
			id = "R11-floor-sweep",
			instrument = Instrument.BeforeSigma1,
			note = "02.09 ночь: T(1500) от порога ухода {5/10/25/50} = 35.94/35.94/36.56/37.81°; перепрогнано после отзыва σ₁ — совпало бит-в-бит"
		},
		new Entry
		{
			id = "S4-acceptance",
			instrument = Instrument.BeforeSigma1,
			note = "31.08: модель разрушения, откол и разделение; ветвь σ₁ тогда не существовала, после отзыва порога записи снова действительны сами собой"
		},
		new Entry
		{
			id = "S4v-owner-scene",
			instrument = Instrument.SingleWitnessDepth,
			note = "31.08: глубины сняты прежним craterDepthM (один свидетель); с N1 такие числа читаются только вместе с их статусом"
		},
		new Entry
		{
			id = "S10-A2-rod",
			instrument = Instrument.Sigma1Withdrawn,
			note = "02.09: 86 % частиц по σ₁ и Embedded на 0°/1500 сняты ПРИ ПОРОГЕ 2.0 ГПа, который отозван в тот же день; как измерение физики не действительны"
		},
		new Entry
		{
			id = "S10-A3-sandwich",
			instrument = Instrument.Sigma1Withdrawn,
			note = "02.09: срез стержня пластиной ДЗ (6921 частица) — ДОКАЗАТЕЛЬСТВО МЕХАНИЗМА при отозванном пороге; величины не действительны, существование среза — да"
		},
		new Entry
		{
			id = "S5-era-numbers",
			instrument = Instrument.BeforeSigma1,
			note = "01.09: сняты при V_p = 500 м/с (заглушка) и броне тоньше суммы метаемых; сняты с учёта решением S11, замена — таблица S11 §4 при 1078 м/с"
		},
		new Entry
		{
			id = "S9-A4-jet-scaling",
			instrument = Instrument.RulerN1,
			note = "02.09: P/L распадающейся струи против записей S6; канал читается свидетелями S6, линейка N1 на струевых сценах отказывает по C1"
		},
		new Entry
		{
			id = "R11-bisection-vs-criterion",
			instrument = Instrument.Bisection,
			note = "08.09 (S23 §1.1): сравнение записей R11-bisection с аналитическим критерием ОТОЗВАНО — бриф 30 публикует, что обе классические модели (Тейт, Розенберг) полностью теряют применимость для эродирующих стержней на плитах конечной и полубесконечной толщины, расходятся между собой на ~140 м/с на ОДНОЙ постановке и обе далеки от опыта. Тейт предполагает жёсткий поворот всего уцелевшего стержня (зависит от L/D и предела текучести), Розенберг — локальный сдвиг контактного элемента (геометрия исчезает, правит R мишени): это не две оценки одной величины. ⚠️ САМИ ЧИСЛА 23.13° и 36.88° ОСТАЮТСЯ В СИЛЕ и не перемеряются: они были верными замерами МОДЕЛИ. Неверным было сравнение"
		},
		new Entry
		{
			id = "money-witness",
			instrument = Instrument.InfinitePlaneThrough,
			note = "02.09 (money4/money5): «свидетель ПРОБИТ, 6.3 и 7.7 % массы» сняты прибором, складывавшим проход СКВОЗЬ плиту и облёт МИМО её кромки. После разделения (решение §7) те же прогоны дают Embedded: сквозь 2.8 % без ДЗ и 0.4 % с ДЗ, мимо кромки 3.5 и 4.9 %. Ленты и mp4 не переписаны — они протокол того прибора"
		}
	};

	public static bool TryGet(string id, out Entry e)
	{
		for (int i = 0; i < Records.Length; i++)
		{
			if (Records[i].id == id)
			{
				e = Records[i];
				return true;
			}
		}
		e = default(Entry);
		return false;
	}

	public static bool WellFormed(out string problem)
	{
		for (int i = 0; i < Records.Length; i++)
		{
			if (string.IsNullOrEmpty(Records[i].id))
			{
				problem = "запись без имени";
				return false;
			}
			if (string.IsNullOrEmpty(Records[i].note))
			{
				problem = "запись «" + Records[i].id + "» без причины";
				return false;
			}
			for (int j = i + 1; j < Records.Length; j++)
			{
				if (Records[j].id == Records[i].id)
				{
					problem = "имя «" + Records[i].id + "» повторяется";
					return false;
				}
			}
		}
		problem = null;
		return true;
	}

	public static string Describe()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append("эпохи приборов у записей (аннотируем, не переписываем):");
		for (int i = 0; i < Records.Length; i++)
		{
			sb.Append(string.Format("\n    {0} — {1}: {2}", Records[i].id, Records[i].instrument, Records[i].note));
		}
		return sb.ToString();
	}
}
