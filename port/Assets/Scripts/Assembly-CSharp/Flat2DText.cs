using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class Flat2DText
{
	private struct Pattern
	{
		public Regex re;

		public string ru;

		public string[] tr;

		public int[] order;
	}

	private static readonly Dictionary<string, string[]> D;

	private static readonly List<Pattern> Patterns;

	private static readonly List<string> duplicates;

	private static readonly Regex Placeholder;

	public static Language? AuditLanguage;

	private static Language Lang => AuditLanguage ?? Localization.Current;

	private static CultureInfo Culture => AuditLanguage.HasValue ? CultureInfo.InvariantCulture : Localization.Culture;

	public static bool ShowsRussian => Lang == Language.RU || Lang == Language.TT;

	public static IReadOnlyList<string> Duplicates => duplicates;

	public static int Count => D.Count + Patterns.Count;

	private static void AddCatalogue()
	{
		R("НАСТРАИВАЕМЫЕ", "CONFIGURABLE", "KONFIGURIERBAR", "KONFIGUROWALNE");
		R("КУМУЛЯТИВНЫЕ", "SHAPED CHARGE", "HOHLLADUNG", "KUMULACYJNE");
		R("СТАЛИ", "STEELS", "STÄHLE", "STALE");
		R("ЛЁГКИЕ СПЛАВЫ", "LIGHT ALLOYS", "LEICHTMETALLE", "STOPY LEKKIE");
		R("ТЯЖЁЛЫЕ МЕТАЛЛЫ", "HEAVY METALS", "SCHWERMETALLE", "METALE CIĘŻKIE");
		R("КЕРАМИКА", "CERAMICS", "KERAMIK", "CERAMIKA");
		R("КОМПОЗИТЫ", "COMPOSITES", "VERBUNDWERKSTOFFE", "KOMPOZYTY");
		R("ЭЛАСТОМЕРЫ", "ELASTOMERS", "ELASTOMERE", "ELASTOMERY");
		R("ПРОЧЕЕ", "OTHER", "SONSTIGES", "INNE");
		R("ДИНАМИЧЕСКАЯ ЗАЩИТА", "EXPLOSIVE REACTIVE ARMOUR", "REAKTIVPANZERUNG", "PANCERZ REAKTYWNY");
		R("НЕРА", "NERA", "NERA", "NERA");
		R("НАСТРАИВАЕМЫЕ (СИЛУЭТ)", "CONFIGURABLE (SILHOUETTE)", "KONFIGURIERBAR (SILHOUETTE)", "KONFIGUROWALNE (SYLWETKA)");
		R("СССР / РОССИЯ", "USSR / RUSSIA", "UdSSR / RUSSLAND", "ZSRR / ROSJA");
		R("США", "USA", "USA", "USA");
		R("ГЕРМАНИЯ", "GERMANY", "DEUTSCHLAND", "NIEMCY");
		R("АВТОПУШКИ 25–30 мм", "AUTOCANNONS 25–30 mm", "MASCHINENKANONEN 25–30 mm", "DZIAŁKA 25–30 mm");
		R("ПРОЧИЕ", "OTHERS", "SONSTIGE", "INNE");
		R("стержень WHA", "WHA rod", "WHA-Stab", "pręt WHA");
		R("стержень DU", "DU rod", "DU-Stab", "pręt DU");
		R("бронебойный", "armour-piercing", "panzerbrechend", "przeciwpancerny");
		R("твёрдый сердечник", "hard core", "Hartkern", "twardy rdzeń");
		R("эталонный стержень", "reference rod", "Referenzstab", "pręt wzorcowy");
		R("ПГ-7В", "PG-7V", "PG-7W", "PG-7W");
		R("ПГ-7ВР", "PG-7VR", "PG-7WR", "PG-7WR");
		R("мягкая сталь", "mild steel", "Baustahl", "stal miękka");
		R("текстолит", "textolite", "Textolit", "tekstolit");
		R("кевлар", "Kevlar", "Kevlar", "kevlar");
		R("стеклопластик", "fibreglass", "GFK", "laminat szklany");
		R("Контакт-1", "Kontakt-1", "Kontakt-1", "Kontakt-1");
		R("Контакт-5", "Kontakt-5", "Kontakt-5", "Kontakt-5");
		R("Реликт", "Relikt", "Relikt", "Relikt");
		R("Кактус", "Kaktus", "Kaktus", "Kaktus");
		R("НЕРА-пакет 3 слоя", "NERA pack, 3 layers", "NERA-Paket, 3 Lagen", "pakiet NERA, 3 warstwy");
		R("НЕРА-пакет 5 слоёв", "NERA pack, 5 layers", "NERA-Paket, 5 Lagen", "pakiet NERA, 5 warstw");
		R("НЕРА с прослойкой", "NERA with interlayer", "NERA mit Zwischenschicht", "NERA z przekładką");
		R("реестр снарядов", "round registry", "Geschossregister", "rejestr pocisków");
		R("91W-7Ni-2Fe: рисуется прямоугольником, скорость задаётся полем", "91W-7Ni-2Fe: drawn as a rectangle, speed set in the field", "91W-7Ni-2Fe: als Rechteck gezeichnet, Geschwindigkeit im Feld", "91W-7Ni-2Fe: rysowany prostokątem, prędkość ustawia pole");
		R("сталь 4340 — опорный отладочный материал проекта", "4340 steel — the project's reference debugging material", "Stahl 4340 — das Referenz-Testmaterial des Projekts", "stal 4340 — wzorcowy materiał testowy projektu");
		R("N12e, >60 HRC (бриф 22)", "N12e, >60 HRC (brief 22)", "N12e, >60 HRC (Brief 22)", "N12e, >60 HRC (brief 22)");
		R("плотность {0:F0} кг/м3, C0 {1:F0} м/с, J-C A {2:F0} МПа", "density {0:F0} kg/m3, C0 {1:F0} m/s, J-C A {2:F0} MPa", "Dichte {0:F0} kg/m3, C0 {1:F0} m/s, J-C A {2:F0} MPa", "gęstość {0:F0} kg/m3, C0 {1:F0} m/s, J-C A {2:F0} MPa");
		R("Ø {0:F1} мм × {1:F0} мм", "Ø {0:F1} mm × {1:F0} mm", "Ø {0:F1} mm × {1:F0} mm", "Ø {0:F1} mm × {1:F0} mm");
		R(" (выведено из массы и L/D)", " (derived from mass and L/D)", " (aus Masse und L/D abgeleitet)", " (wyprowadzone z masy i L/D)");
		R(", нос {0}", ", nose {0}", ", Spitze {0}", ", nos {0}");
		R(" (умолчание каталога)", " (catalogue default)", " (Katalogvorgabe)", " (domyślne katalogu)");
		R(", v0 {0:F0} м/с, вещество ", ", v0 {0:F0} m/s, material ", ", v0 {0:F0} m/s, Material ", ", v0 {0:F0} m/s, materiał ");
		R("— (строки {0} нет)", "— (no {0} entry)", "— (kein Eintrag {0})", "— (brak wpisu {0})");
		R("; силуэт без оперения и поддона", "; silhouette without fins or sabot", "; Silhouette ohne Leitwerk und Treibspiegel", "; sylwetka bez usterzenia i sabotu");
		R("плоский торец", "flat face", "flache Stirn", "płaskie czoło");
		R("конус", "cone", "Kegel", "stożek");
		R("оживало", "ogive", "Ogive", "ostrołuk");
		R("притуплённое оживало", "blunt ogive", "stumpfe Ogive", "stępiony ostrołuk");
		R("этап 6 — взрывчатка не реализована", "stage 6 — explosives are not implemented", "Stufe 6 — Sprengstoff ist nicht umgesetzt", "etap 6 — materiały wybuchowe nie są zaimplementowane");
		R("нужна модель JH-2 (хрупкое разрушение керамики)", "the JH-2 model is needed (brittle failure of ceramics)", "das Modell JH-2 wird benötigt (Sprödbruch von Keramik)", "potrzebny model JH-2 (kruche pękanie ceramiki)");
		R("нужна ортотропная модель (композит не изотропен)", "an orthotropic model is needed (a composite is not isotropic)", "ein orthotropes Modell wird benötigt (ein Verbundwerkstoff ist nicht isotrop)", "potrzebny model ortotropowy (kompozyt nie jest izotropowy)");
		R("нет блока Джонсона–Кука вовсе — прочности нет", "no Johnson–Cook block at all — no strength", "überhaupt kein Johnson-Cook-Block — keine Festigkeit", "brak bloku Johnsona–Cooka w ogóle — brak wytrzymałości");
		R("нет блока разрушения", "no failure block", "kein Versagensblock", "brak bloku zniszczenia");
		R("нет параметров разрушения и скоростной чувствительности", "no failure or strain-rate sensitivity parameters", "keine Versagens- und Dehnratenparameter", "brak parametrów zniszczenia i wrażliwości na prędkość odkształcenia");
		R("нет параметров разрушения и скоростной чувствительности (в реестре нет)", "no failure or strain-rate sensitivity parameters (not in the registry)", "keine Versagens- und Dehnratenparameter (nicht im Register)", "brak parametrów zniszczenia i wrażliwości na prędkość odkształcenia (brak w rejestrze)");
		R("нет параметров разрушения и скоростной чувствительности (research_13)", "no failure or strain-rate sensitivity parameters (research_13)", "keine Versagens- und Dehnratenparameter (research_13)", "brak parametrów zniszczenia i wrażliwości na prędkość odkształcenia (research_13)");
		R("нужна модель JH-2 (хрупкое разрушение керамики) (research_13)", "the JH-2 model is needed (brittle failure of ceramics) (research_13)", "das Modell JH-2 wird benötigt (Sprödbruch von Keramik) (research_13)", "potrzebny model JH-2 (kruche pękanie ceramiki) (research_13)");
		R("только для проверок решателя — в песочнице не выбирается", "solver checks only — not selectable in the sandbox", "nur für Löserprüfungen — in der Sandbox nicht wählbar", "tylko do sprawdzania solvera — w piaskownicy nie do wybrania");
		R("строка в реестре есть, приёмка не пройдена — исход немонотонен по диапазону деформации разрушения", "the registry entry exists, but acceptance failed — the outcome is non-monotonic over the failure-strain range", "der Registereintrag existiert, die Abnahme ist nicht bestanden — das Ergebnis ist über den Bereich der Bruchdehnung nicht monoton", "wpis w rejestrze istnieje, odbiór nie przeszedł — wynik jest niemonotoniczny w zakresie odkształcenia niszczącego");
		R("нет модели струи — этап 6", "no jet model — stage 6", "kein Strahlmodell — Stufe 6", "brak modelu strumienia — etap 6");
		R("строка в реестре есть (S31, для Super), в «Плоскости» приёмки не было; блока разрушения D1–D5 у урана нет", "the registry entry exists (S31, for Super), but Plane has had no acceptance run; uranium has no D1–D5 failure block", "der Registereintrag existiert (S31, für Super), in der „Ebene“ gab es keine Abnahme; Uran hat keinen Versagensblock D1–D5", "wpis w rejestrze istnieje (S31, dla Super), w „Płaszczyźnie” nie było odbioru; uran nie ma bloku zniszczenia D1–D5");
		R("нет ударной адиабаты (C0/S)", "no shock Hugoniot (C0/S)", "keine Stoßadiabate (C0/S)", "brak adiabaty uderzeniowej (C0/S)");
		R("в реестре нет: набор J-C не добыт", "not in the registry: no J-C set was found", "nicht im Register: kein J-C-Satz beschafft", "brak w rejestrze: nie zdobyto zestawu J-C");
		R("в реестре нет", "not in the registry", "nicht im Register", "brak w rejestrze");
		R("нужен контакт между слоями и метаемые пластины — этап 6", "needs contact between layers and flying plates — stage 6", "benötigt Kontakt zwischen den Lagen und geschleuderte Platten — Stufe 6", "potrzebny kontakt między warstwami i miotane płyty — etap 6");
		R("колпачок — второе тело, силуэтом «Плоскости» не рисуется: вышел бы голый сердечник — соседняя строка под чужим именем", "the cap is a second body and cannot be drawn as a Plane silhouette: it would come out as a bare core — a neighbouring entry under someone else's name", "die Kappe ist ein zweiter Körper und lässt sich nicht als Silhouette der „Ebene“ zeichnen: es käme ein nackter Kern heraus — ein Nachbareintrag unter fremdem Namen", "czepiec to drugie ciało i nie da się go narysować sylwetką „Płaszczyzny”: wyszedłby goły rdzeń — sąsiedni wpis pod cudzą nazwą");
		R("у пресета нет размеров — силуэт не строится", "the preset has no dimensions — the silhouette cannot be built", "die Vorlage hat keine Abmessungen — die Silhouette lässt sich nicht erstellen", "gotowy pocisk nie ma wymiarów — sylwetka się nie buduje");
		P("нет строки {0} в реестре", "no {0} entry in the registry", "kein Eintrag {0} im Register", "brak wpisu {0} w rejestrze");
		P("реестр снарядов обычного режима не собрался: {0}", "the normal-mode round registry failed to build: {0}", "das Geschossregister des Normalmodus ließ sich nicht aufbauen: {0}", "rejestr pocisków trybu zwykłego nie zbudował się: {0}");
		R("ПРИЁМКА НЕ ПРОЙДЕНА", "ACCEPTANCE FAILED", "ABNAHME FEHLT", "ODBIÓR NIEZALICZONY");
		R("ПАСПОРТ ОТКЛОНЁН", "PASSPORT REJECTED", "PASS ABGELEHNT", "PASZPORT ODRZUCONY");
		R("ДВА ТЕЛА", "TWO BODIES", "ZWEI KÖRPER", "DWA CIAŁA");
		R("ЭТАП 6", "STAGE 6", "STUFE 6", "ETAP 6");
		R("НУЖНА JH-2", "NEEDS JH-2", "JH-2 NÖTIG", "POTRZEBNY JH-2");
		R("ОРТОТРОПИЯ", "ORTHOTROPY", "ORTHOTROPIE", "ORTOTROPIA");
		R("НЕТ J-C", "NO J-C", "KEIN J-C", "BRAK J-C");
		R("НЕТ РАЗРУШЕНИЯ", "NO FAILURE", "KEIN VERSAGEN", "BRAK ZNISZCZ.");
		R("НЕТ АДИАБАТЫ", "NO HUGONIOT", "KEINE ADIABATE", "BRAK ADIABATY");
		R("НЕТ РАЗМЕРОВ", "NO DIMENSIONS", "KEINE MASSE", "BRAK WYMIARÓW");
		R("ТОЛЬКО ТЕСТЫ", "TESTS ONLY", "NUR TESTS", "TYLKO TESTY");
		R("НЕТ В РЕЕСТРЕ", "NOT IN REGISTRY", "NICHT IM REGISTER", "BRAK W REJESTRZE");
		R("НЕДОСТУПНО", "UNAVAILABLE", "NICHT VERFÜGBAR", "NIEDOSTĘPNE");
		R("ЭКИПАЖ", "CREW", "BESATZUNG", "ZAŁOGA");
	}

	private static void AddModel()
	{
		R("ВНИМАНИЕ: не менее {0} частиц ПРИ ПОТОЛКЕ {1} (счёт остановлен) — фигура будет ОТКАЗАНА; уменьшите её или укрупните Δx", "WARNING: at least {0} particles WITH A CAP OF {1} (count stopped) — the drawing will be REFUSED; make it smaller or coarsen Δx", "ACHTUNG: mindestens {0} Partikel BEI EINER OBERGRENZE VON {1} (Zählung angehalten) — die Zeichnung wird ABGELEHNT; verkleinern oder Δx vergröbern", "UWAGA: co najmniej {0} cząstek PRZY LIMICIE {1} (liczenie zatrzymane) — rysunek zostanie ODRZUCONY; zmniejsz go lub zwiększ Δx");
		R("≈{0:F1} с = {1} шагов × ({2:F1} мс / {3} зондовых шагов)", "≈{0:F1} s = {1} steps × ({2:F1} ms / {3} probe steps)", "≈{0:F1} s = {1} Schritte × ({2:F1} ms / {3} Probeschritte)", "≈{0:F1} s = {1} kroków × ({2:F1} ms / {3} kroków próbnych)");
		R("броня", "armour", "Panzerung", "pancerz");
		R("снаряд", "projectile", "Geschoss", "pocisk");
		R("якорь", "anchor", "Anker", "kotwica");
		R("заливка: список многоугольников пуст", "fill: the polygon list is empty", "Füllung: die Polygonliste ist leer", "wypełnienie: lista wielokątów jest pusta");
		R("заливка: у многоугольников нет площади", "fill: the polygons have no area", "Füllung: die Polygone haben keine Fläche", "wypełnienie: wielokąty nie mają pola");
		R("заливка: НИ ОДИН узел решётки не попал внутрь — фигура тоньше шага Δx или всё нарисованное стёрто ластиком", "fill: NOT A SINGLE lattice node fell inside — the drawing is thinner than the Δx step or everything was erased", "Füllung: KEIN EINZIGER Gitterknoten liegt innen — die Zeichnung ist dünner als der Schritt Δx oder alles wurde radiert", "wypełnienie: ANI JEDEN węzeł siatki nie trafił do środka — rysunek jest cieńszy niż krok Δx albo wszystko wymazano gumką");
		R("заливка ОТКАЗАНА: {0}{1} частиц при потолке {2} (счёт остановлен на потолке). Крупнее шаг Δx или меньше фигура. ⚠️ Молча урезать нельзя: усечённая сцена — это ДРУГАЯ сцена.", "fill REFUSED: {0}{1} particles with a cap of {2} (count stopped at the cap). Use a coarser Δx or a smaller drawing. It cannot be trimmed silently: a truncated scene is a DIFFERENT scene.", "Füllung ABGELEHNT: {0}{1} Partikel bei einer Obergrenze von {2} (Zählung an der Grenze angehalten). Gröberes Δx oder kleinere Zeichnung. Stilles Kürzen ist nicht erlaubt: eine gekürzte Szene ist eine ANDERE Szene.", "wypełnienie ODRZUCONE: {0}{1} cząstek przy limicie {2} (liczenie zatrzymane na limicie). Większy krok Δx albo mniejszy rysunek. Nie wolno przyciąć po cichu: przycięta scena to INNA scena.");
		R("заливка: {0} частиц при потолке {1}; закреплено якорем {2}", "fill: {0} particles with a cap of {1}; pinned by anchor {2}", "Füllung: {0} Partikel bei einer Obergrenze von {1}; vom Anker fixiert {2}", "wypełnienie: {0} cząstek przy limicie {1}; zakotwiczono {2}");
		R("ПЛИТА", "PLATE", "PLATTE", "PŁYTA");
		R("КЛИН", "WEDGE", "KEIL", "KLIN");
		R("РАЗНЕСЁННАЯ", "SPACED", "BEABSTANDET", "ROZSUNIĘTA");
		R("УГОЛОК", "ANGLE", "WINKEL", "KĄTOWNIK");
		R("КОРОБ", "BOX", "KASTEN", "SKRZYNIA");
		R("ПЛИТА 100 мм + БОПС", "100 mm PLATE + APFSDS", "PLATTE 100 mm + APFSDS", "PŁYTA 100 mm + APFSDS");
		R("РАЗНЕСЁННАЯ БРОНЯ", "SPACED ARMOUR", "SCHOTTPANZERUNG", "PANCERZ ROZSUNIĘTY");
		R("ТЕЙЛОР", "TAYLOR", "TAYLOR", "TAYLOR");
		R("Эластомеры", "Elastomers", "Elastomere", "Elastomery");
		R("Композиты", "Composites", "Verbundwerkstoffe", "Kompozyty");
		R("нет модели прочности: блок Джонсона–Кука пуст (жидкость с УС) — твёрдым телом не считается", "no strength model: the Johnson–Cook block is empty (a fluid with an EOS) — not computed as a solid", "kein Festigkeitsmodell: der Johnson-Cook-Block ist leer (Fluid mit Zustandsgleichung) — wird nicht als Festkörper gerechnet", "brak modelu wytrzymałości: blok Johnsona–Cooka jest pusty (ciecz z równaniem stanu) — nie liczy się jako ciało stałe");
		R("нет ударной адиабаты (C0/S) — давление не считается", "no shock Hugoniot (C0/S) — pressure is not computed", "keine Stoßadiabate (C0/S) — der Druck wird nicht berechnet", "brak adiabaty uderzeniowej (C0/S) — ciśnienie nie jest liczone");
		R("только для тестов (T3b, струя S6): в песочнице не выбирается — запись реестра", "tests only (T3b, jet S6): not selectable in the sandbox — a registry entry", "nur für Tests (T3b, Strahl S6): in der Sandbox nicht wählbar — ein Registereintrag", "tylko do testów (T3b, strumień S6): w piaskownicy nie do wybrania — wpis rejestru");
		R("нет блока Джонсона–Кука вовсе (research_13)", "no Johnson–Cook block at all (research_13)", "überhaupt kein Johnson-Cook-Block (research_13)", "brak bloku Johnsona–Cooka w ogóle (research_13)");
		R("требуется модель JH-2 (research_13)", "the JH-2 model is required (research_13)", "das Modell JH-2 ist erforderlich (research_13)", "wymagany model JH-2 (research_13)");
		R("требуется ортотропная модель (research_13)", "an orthotropic model is required (research_13)", "ein orthotropes Modell ist erforderlich (research_13)", "wymagany model ortotropowy (research_13)");
		R("вещества нет в пикере «Плоскости»", "the material is not in the Plane picker", "das Material ist nicht in der Auswahl der „Ebene“", "materiału nie ma w wyborze „Płaszczyzny”");
		R("сталь 4340", "4340 steel", "Stahl 4340", "stal 4340");
		R("RHA (набор A, Meyer & Kleponis)", "RHA (set A, Meyer & Kleponis)", "RHA (Satz A, Meyer & Kleponis)", "RHA (zest. A, Meyer & Kleponis)");
		R("RHA (набор B, Grey et al.)", "RHA (set B, Grey et al.)", "RHA (Satz B, Grey et al.)", "RHA (zest. B, Grey et al.)");
		R("медь OFHC (только проверки)", "OFHC copper (tests only)", "OFHC-Kupfer (nur Tests)", "miedź OFHC (tylko testy)");
		R("литая броневая сталь", "cast armour steel", "Panzerstahlguss", "lana stal pancerna");
		R("неопрен (прослойка НДЗ)", "neoprene (NERA interlayer)", "Neopren (NERA-Zwischenschicht)", "neopren (przekładka NERA)");
		R("обеднённый уран DU-0.75Ti", "depleted uranium DU-0.75Ti", "abgereichertes Uran DU-0.75Ti", "zubożony uran DU-0.75Ti");
		R("МАНЕКЕН СИДЯ", "MANNEQUIN SEATED", "PUPPE SITZEND", "MANEKIN SIEDZĄCY");
		R("МАНЕКЕН СТОЯ", "MANNEQUIN STANDING", "PUPPE STEHEND", "MANEKIN STOJĄCY");
		R("экипаж (условная ткань)", "crew (notional tissue)", "Besatzung (Modellgewebe)", "załoga (umowna tkanka)");
	}

	private static void AddScreen()
	{
		R("ЧТО СТРЕЛЯЕТ И ВО ЧТО", "WHAT FIRES AND AT WHAT", "WAS SCHIESST WORAUF", "CO STRZELA I W CO");
		R("СНАРЯДЫ", "PROJECTILES", "GESCHOSSE", "POCISKI");
		R("МАТЕРИАЛЫ", "MATERIALS", "MATERIALIEN", "MATERIAŁY");
		R("ДЗ И НЕРА", "ERA AND NERA", "ERA UND NERA", "ERA I NERA");
		R("выбрано: {0}", "selected: {0}", "gewählt: {0}", "wybrano: {0}");
		R("выбрано: {0} — роль: {1}", "selected: {0} — role: {1}", "gewählt: {0} — Rolle: {1}", "wybrano: {0} — rola: {1}");
		R("{0}: убран из руки", "{0}: put away", "{0}: aus der Hand gelegt", "{0}: odłożony");
		R("{0} в руке — роль: {1}; превью у курсора, колесо вертит ({2} — точно), клик ставит; повторный выбор — убрать", "{0} in hand — role: {1}; preview follows the cursor, the wheel rotates ({2} — fine), click places it; pick it again to put it away", "{0} in der Hand — Rolle: {1}; Vorschau am Mauszeiger, das Mausrad dreht ({2} — fein), Klick setzt; erneut wählen — weglegen", "{0} w ręce — rola: {1}; podgląd przy kursorze, kółko obraca ({2} — dokładnie), kliknięcie stawia; ponowny wybór — odłóż");
		R("{0}: снаряд недоступен — {1}", "{0}: projectile unavailable — {1}", "{0}: Geschoss nicht verfügbar — {1}", "{0}: pocisk niedostępny — {1}");
		R("строки нет в реестре силуэтов", "no entry in the silhouette registry", "kein Eintrag im Silhouettenregister", "brak wpisu w rejestrze sylwetek");
		R("{0}: силуэт не построился", "{0}: the silhouette could not be built", "{0}: die Silhouette ließ sich nicht erstellen", "{0}: nie udało się zbudować sylwetki");
		R(", угол {0:F0}°", ", angle {0:F0}°", ", Winkel {0:F0}°", ", kąt {0:F0}°");
		R(" — выбор снят", " — selection cleared", " — Auswahl aufgehoben", " — wybór anulowany");
		R("скорость, м/с", "speed, m/s", "Geschw., m/s", "prędkość, m/s");
		R("скорость: не число — оставлено прежнее значение", "speed: not a number — the previous value is kept", "Geschwindigkeit: keine Zahl — der bisherige Wert bleibt", "prędkość: to nie liczba — pozostawiono poprzednią wartość");
		R("ОСТАНОВИТЬ", "STOP", "STOPP", "ZATRZYMAJ");
		R("ПОЛЕ:", "FIELD:", "FELD:", "POLE:");
		R("ТЕМПЕРАТУРА", "TEMPERATURE", "TEMPERATUR", "TEMPERATURA");
		R("КАК РИСУЕМ", "HOW WE DRAW", "WIE WIR ZEICHNEN", "JAK RYSUJEMY");
		R("Инструмент", "Tool", "Werkzeug", "Narzędzie");
		R("ПОЛИГОН", "POLYGON", "POLYGON", "WIELOKĄT");
		R("ПРЯМОУГ.", "RECT.", "RECHTECK", "PROSTOK.");
		R("КРУГ", "CIRCLE", "KREIS", "KOŁO");
		R("КИСТЬ", "BRUSH", "PINSEL", "PĘDZEL");
		R("ЛАСТИК", "ERASER", "RADIERER", "GUMKA");
		R("ЛИНЕЙКА", "RULER", "LINEAL", "LINIJKA");
		R("фигура замкнута", "shape closed", "Form geschlossen", "kształt zamknięty");
		R("ВЕРНУТЬ", "REDO", "WIEDERH.", "PONÓW");
		R("возвращено", "redone", "wiederhergestellt", "przywrócono");
		R("возвращать нечего", "nothing to redo", "nichts wiederherzustellen", "nie ma czego przywrócić");
		R("чисто", "cleared", "geleert", "wyczyszczono");
		R("СИММЕТРИЯ", "MIRROR", "SPIEGELN", "SYMETRIA");
		R("симметрия ВКЛ: отражаются контурные фигуры (мазки кисти и декали — нет)", "mirror ON: outline shapes are mirrored (brush strokes and decals are not)", "Spiegeln AN: Umrissformen werden gespiegelt (Pinselstriche und Decals nicht)", "symetria WŁ.: odbijane są kształty konturowe (pociągnięcia pędzla i kalkomanie — nie)");
		R("симметрия ВЫКЛ", "mirror OFF", "Spiegeln AUS", "symetria WYŁ.");
		R("ЯКОРЬ", "ANCHOR", "ANKER", "KOTWICA");
		R("кисть: зажать и вести — мазок; {0} + колесо над холстом — размер, не отрывая руки", "brush: press and drag — a stroke; {0} + wheel over the canvas — size, without letting go", "Pinsel: drücken und ziehen — ein Strich; {0} + Mausrad über der Fläche — Größe, ohne abzusetzen", "pędzel: wciśnij i prowadź — pociągnięcie; {0} + kółko nad płótnem — rozmiar, bez odrywania ręki");
		R("декаль берётся В РУКУ: превью следует за курсором, колесо вертит ({0} — точно, {1} — размер), клик ставит; убрать — повторное нажатие кнопки", "a decal is taken IN HAND: the preview follows the cursor, the wheel rotates ({0} — fine, {1} — size), click places it; press the button again to put it away", "ein Decal kommt IN DIE HAND: die Vorschau folgt dem Mauszeiger, das Mausrad dreht ({0} — fein, {1} — Größe), Klick setzt; weglegen — Schaltfläche erneut drücken", "kalkomania trafia DO RĘKI: podgląd podąża za kursorem, kółko obraca ({0} — dokładnie, {1} — rozmiar), kliknięcie stawia; odłożenie — ponowne naciśnięcie przycisku");
		R("Пресеты сцен", "Scene presets", "Szenen-Vorlagen", "Gotowe sceny");
		R("Привязка", "Snap", "Einrasten", "Przyciąganie");
		R("К СЕТКЕ Δx", "TO GRID Δx", "RASTER Δx", "SIATKA Δx");
		R("К УГЛУ 15°", "TO 15°", "WINKEL 15°", "KĄT 15°");
		R("СВОБОДНО", "FREE", "FREI", "SWOBODNIE");
		R("Рисунок", "Drawing", "Zeichnung", "Rysunek");
		R("холст пуст — рисуйте кистью, контуром или поставьте декаль", "the canvas is empty — draw with the brush, an outline, or place a decal", "die Fläche ist leer — mit dem Pinsel oder einem Umriss zeichnen oder ein Decal setzen", "płótno jest puste — rysuj pędzlem, konturem albo postaw kalkomanię");
		R("тел на холсте: {0}", "bodies on the canvas: {0}", "Körper auf der Fläche: {0}", "ciał na płótnie: {0}");
		R(" (по решателю)", " (per solver)", " (laut Löser)", " (według solvera)");
		R("рисунок цельный: снаряд из нескольких веществ, если его куски касаются, летит одним телом с одной скоростью", "the drawing is one piece: a projectile made of several materials flies as one body at one speed if its parts touch", "die Zeichnung ist ein Ganzes: ein Geschoss aus mehreren Stoffen fliegt als ein Körper mit einer Geschwindigkeit, wenn sich seine Teile berühren", "rysunek jest całością: pocisk z kilku materiałów, jeśli jego części się stykają, leci jako jedno ciało z jedną prędkością");
		R("закреплённые узлы показаны", "pinned nodes shown", "fixierte Knoten sichtbar", "zakotwiczone węzły widoczne");
		R("показ узлов выключен", "node display off", "Knotenanzeige aus", "pokazywanie węzłów wyłączone");
		R("Сцена", "Scene", "Szene", "Scena");
		R("ИЗМЕРЕНИЕ", "MEASURE", "MESSUNG", "POMIAR");
		R("ТОЧКИ", "DOTS", "PUNKTE", "PUNKTY");
		R("ПОВЕРХН.", "SURFACE", "OBERFL.", "POWIERZ.");
		R("ПОВЕРХНОСТЬ", "SURFACE", "OBERFLÄCHE", "POWIERZCHNIA");
		R("СВЕЧЕНИЕ: ВКЛ", "GLOW: ON", "GLÜHEN: AN", "ŻARZENIE: WŁ.");
		R("СВЕЧЕНИЕ: ВЫКЛ", "GLOW: OFF", "GLÜHEN: AUS", "ŻARZENIE: WYŁ.");
		R("остывание показано ускоренно", "cooling is shown sped up", "Abkühlung beschleunigt dargestellt", "stygnięcie pokazane w przyspieszeniu");
		R("СТАБИЛИЗАТОР: ВКЛ", "STABILISER: ON", "STABILISATOR: AN", "STABILIZATOR: WŁ.");
		R("СТАБИЛИЗАТОР: ВЫКЛ", "STABILISER: OFF", "STABILISATOR: AUS", "STABILIZATOR: WYŁ.");
		R("ТЕЛА РАЗДЕЛЬНЫ: ВКЛ", "SEPARATE BODIES: ON", "GETRENNTE KÖRPER: AN", "ROZDZIELNE CIAŁA: WŁ.");
		R("ТЕЛА РАЗДЕЛЬНЫ: ВЫКЛ", "SEPARATE BODIES: OFF", "GETRENNTE KÖRPER: AUS", "ROZDZIELNE CIAŁA: WYŁ.");
		R("СТАЧИВАНИЕ: ВКЛ", "EROSION: ON", "EROSION: AN", "EROZJA: WŁ.");
		R("СТАЧИВАНИЕ: ВЫКЛ", "EROSION: OFF", "EROSION: AUS", "EROZJA: WYŁ.");
		R("ТРЕЩИНЫ: ВКЛ", "CRACKS: ON", "RISSE: AN", "PĘKNIĘCIA: WŁ.");
		R("ТРЕЩИНЫ: ВЫКЛ", "CRACKS: OFF", "RISSE: AUS", "PĘKNIĘCIA: WYŁ.");
		R("УДЕРЖАНИЕ: ВКЛ", "RESTRAINT: ON", "HALTERUNG: AN", "UTWIERDZENIE: WŁ.");
		R("УДЕРЖАНИЕ: ВЫКЛ", "RESTRAINT: OFF", "HALTERUNG: AUS", "UTWIERDZENIE: WYŁ.");
		R("ВПИСАТЬ", "FIT", "EINPASSEN", "DOPASUJ");
		R("вид вписан", "view fitted", "Ansicht eingepasst", "widok dopasowany");
		R("СЕТКА", "GRID", "RASTER", "SIATKA");
		R("МЕНЮ (ESC)", "MENU (ESC)", "MENÜ (ESC)", "MENU (ESC)");
		R("удержание меняется до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел", "restraint is changed before FIRE: STOP the shot first", "die Halterung wird vor FEUER geändert: zuerst den Schuss STOPPEN", "utwierdzenie zmienia się przed OGNIA: najpierw ZATRZYMAJ strzał");
		R("удержание ВКЛ — броню у отверстия держит недостающее третье измерение; со следующего «ОГОНЬ»", "restraint ON — the missing third dimension holds the armour around the hole; from the next FIRE", "Halterung AN — die fehlende dritte Dimension hält die Panzerung am Loch; ab dem nächsten FEUER", "utwierdzenie WŁ. — brakujący trzeci wymiar trzyma pancerz przy otworze; od następnego OGNIA");
		R("удержание ВЫКЛ — плита как 2D-лезвие: гнётся целиком (прежняя физика)", "restraint OFF — the plate behaves as a 2D blade: it bends as a whole (previous physics)", "Halterung AUS — die Platte als 2D-Klinge: sie biegt sich als Ganzes (bisherige Physik)", "utwierdzenie WYŁ. — płyta jak ostrze 2D: gnie się w całości (dotychczasowa fizyka)");
		R("стачивание меняется до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел", "erosion is changed before FIRE: STOP the shot first", "die Erosion wird vor FEUER geändert: zuerst den Schuss STOPPEN", "erozja zmienia się przed OGNIA: najpierw ZATRZYMAJ strzał");
		R("стачивание ВКЛ — 80 % сильно продеформированных частиц уходит из счёта; со следующего «ОГОНЬ»", "erosion ON — 80 % of heavily deformed particles leave the computation; from the next FIRE", "Erosion AN — 80 % der stark verformten Partikel scheiden aus der Rechnung aus; ab dem nächsten FEUER", "erozja WŁ. — 80 % silnie odkształconych cząstek odpada z obliczeń; od następnego OGNIA");
		R("стачивание ВЫКЛ — частицы не исчезают (прежняя физика)", "erosion OFF — particles do not disappear (previous physics)", "Erosion AUS — Partikel verschwinden nicht (bisherige Physik)", "erozja WYŁ. — cząstki nie znikają (dotychczasowa fizyka)");
		R("трещины меняются до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел", "cracks are changed before FIRE: STOP the shot first", "Risse werden vor FEUER geändert: zuerst den Schuss STOPPEN", "pęknięcia zmienia się przed OGNIA: najpierw ZATRZYMAJ strzał");
		R("трещины ВКЛ — полосы сдвига, разрушенное не держит растяжения, разброс прочности; со следующего «ОГОНЬ»", "cracks ON — shear bands, failed material carries no tension, strength scatter; from the next FIRE", "Risse AN — Scherbänder, versagtes Material trägt keinen Zug, Festigkeitsstreuung; ab dem nächsten FEUER", "pęknięcia WŁ. — pasma ścinania, zniszczony materiał nie przenosi rozciągania, rozrzut wytrzymałości; od następnego OGNIA");
		R("трещины ВЫКЛ — прежняя физика разрушения", "cracks OFF — previous fracture physics", "Risse AUS — bisherige Bruchphysik", "pęknięcia WYŁ. — dotychczasowa fizyka zniszczenia");
		R("стабилизатор меняется до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел", "the stabiliser is changed before FIRE: STOP the shot first", "der Stabilisator wird vor FEUER geändert: zuerst den Schuss STOPPEN", "stabilizator zmienia się przed OGNIA: najpierw ZATRZYMAJ strzał");
		R("стабилизатор ВКЛ (искусственное напряжение, по растяжению) — действует со следующего «ОГОНЬ»", "stabiliser ON (artificial stress, tension-activated) — takes effect from the next FIRE", "Stabilisator AN (künstliche Spannung, bei Zug) — wirkt ab dem nächsten FEUER", "stabilizator WŁ. (sztuczne naprężenie, przy rozciąganiu) — działa od następnego OGNIA");
		R("стабилизатор ВЫКЛ — как было по умолчанию", "stabiliser OFF — the default as before", "Stabilisator AUS — wie bisher standardmäßig", "stabilizator WYŁ. — jak domyślnie dotąd");
		R("тела раздельны меняются до «ОГОНЬ»: сначала ОСТАНОВИТЬ выстрел", "separate bodies are changed before FIRE: STOP the shot first", "getrennte Körper werden vor FEUER geändert: zuerst den Schuss STOPPEN", "rozdzielne ciała zmienia się przed OGNIA: najpierw ZATRZYMAJ strzał");
		R("тела РАЗДЕЛЬНЫ — толкают друг друга, но не тянут; действует со следующего «ОГОНЬ»", "bodies SEPARATE — they push each other but do not pull; takes effect from the next FIRE", "Körper GETRENNT — sie drücken einander, ziehen aber nicht; wirkt ab dem nächsten FEUER", "ciała ROZDZIELNE — pchają się nawzajem, ale nie ciągną; działa od następnego OGNIA");
		R("тела СЦЕПЛЕНЫ (одно сплошное тело) — как было по умолчанию", "bodies JOINED (one continuous body) — the default as before", "Körper VERBUNDEN (ein durchgehender Körper) — wie bisher standardmäßig", "ciała POŁĄCZONE (jedno ciągłe ciało) — jak domyślnie dotąd");
		R("свечение: вида нет", "glow: no view", "Glühen: keine Ansicht", "żarzenie: brak widoku");
		R("свечение ВЫКЛ — частицы красятся только выбранным полем", "glow OFF — particles are coloured by the selected field only", "Glühen AUS — Partikel werden nur nach dem gewählten Feld eingefärbt", "żarzenie WYŁ. — cząstki barwione są tylko wybranym polem");
		R("свечение ВКЛ, но к виду ИЗМЕРЕНИЕ оно не применяется: это прибор, его кадры не меняются", "glow ON, but it is not applied to the MEASURE view: that is an instrument and its frames do not change", "Glühen AN, aber auf die Ansicht MESSUNG nicht angewandt: sie ist ein Messgerät, ihre Bilder ändern sich nicht", "żarzenie WŁ., ale nie dotyczy widoku POMIAR: to przyrząd, jego klatki się nie zmieniają");
		R("свечение ВКЛ — остывание показано ускоренно; температуру решателя смотрите полем ТЕМПЕРАТУРА", "glow ON — cooling is shown sped up; read the solver temperature with the TEMPERATURE field", "Glühen AN — die Abkühlung ist beschleunigt dargestellt; die Lösertemperatur zeigt das Feld TEMPERATUR", "żarzenie WŁ. — stygnięcie pokazane w przyspieszeniu; temperaturę solvera pokazuje pole TEMPERATURA");
		R("ПОВЕРХНОСТЬ недоступна: шейдер Flat2D/SurfaceScreen не найден в сборке — вид не переключён", "SURFACE is unavailable: the Flat2D/SurfaceScreen shader is missing from the build — the view was not switched", "OBERFLÄCHE nicht verfügbar: der Shader Flat2D/SurfaceScreen fehlt im Build — die Ansicht wurde nicht gewechselt", "POWIERZCHNIA niedostępna: w kompilacji brak shadera Flat2D/SurfaceScreen — widok nie został przełączony");
		R("вид: ИЗМЕРЕНИЕ — точные квадраты 0.5 Δx, зазор в шаг читается зазором", "view: MEASURE — exact 0.5 Δx squares, a one-step gap reads as a gap", "Ansicht: MESSUNG — exakte Quadrate 0.5 Δx, eine Lücke von einem Schritt bleibt als Lücke lesbar", "widok: POMIAR — dokładne kwadraty 0.5 Δx, szczelina o jeden krok jest widoczna jako szczelina");
		R("вид: ТОЧКИ — мягкие диски; зазор в шаг читается зазором", "view: DOTS — soft discs; a one-step gap reads as a gap", "Ansicht: PUNKTE — weiche Scheiben; eine Lücke von einem Schritt bleibt als Lücke lesbar", "widok: PUNKTY — miękkie dyski; szczelina o jeden krok jest widoczna jako szczelina");
		R("вид: ПОВЕРХНОСТЬ — сплошное тело; замеры читайте в «ИЗМЕРЕНИЕ»", "view: SURFACE — a solid body; take measurements in MEASURE", "Ansicht: OBERFLÄCHE — ein massiver Körper; Messungen in MESSUNG ablesen", "widok: POWIERZCHNIA — ciało ciągłe; pomiary odczytuj w POMIAR");
		R("Flat2D рисунок", "Flat2D drawing", "Flat2D-Zeichnung", "Rysunek Flat2D");
		R("СТРЕЛЯТЬ", "FIRE", "FEUERN", "STRZELAJ");
		R("ОТМЕНА", "CANCEL", "ABBRECHEN", "ANULUJ");
		R("ОТМЕНА", "CANCEL", "ABBRECHEN", "ANULUJ");
		R("сохранено: {0}", "saved: {0}", "gespeichert: {0}", "zapisano: {0}");
		R("не сохранилось", "not saved", "nicht gespeichert", "nie zapisano");
		R("загружено: {0}", "loaded: {0}", "geladen: {0}", "wczytano: {0}");
		R("сохранений семьи Flat2D нет", "there are no Flat2D saves", "keine Flat2D-Spielstände", "brak zapisów Flat2D");
		R("фигура {0} выбрана", "shape {0} selected", "Form {0} gewählt", "wybrano kształt {0}");
		R("выбор снят", "selection cleared", "Auswahl aufgehoben", "wybór anulowany");
		R("инструмент: {0}", "tool: {0}", "Werkzeug: {0}", "narzędzie: {0}");
		R("привязка: {0}", "snap: {0}", "Einrasten: {0}", "przyciąganie: {0}");
		R("к сетке Δx", "to the Δx grid", "am Δx-Raster", "do siatki Δx");
		R("к углу 15°", "to 15°", "auf 15°", "do kąta 15°");
		R("свободно", "free", "frei", "swobodnie");
		R("декаль убрана из руки", "decal put away", "Decal aus der Hand gelegt", "kalkomania odłożona");
		R("декаль {0} в руке: превью у курсора, колесо вертит ({1} — точно, {2} — размер), клик ставит", "decal {0} in hand: preview at the cursor, the wheel rotates ({1} — fine, {2} — size), click places it", "Decal {0} in der Hand: Vorschau am Mauszeiger, das Mausrad dreht ({1} — fein, {2} — Größe), Klick setzt", "kalkomania {0} w ręce: podgląd przy kursorze, kółko obraca ({1} — dokładnie, {2} — rozmiar), kliknięcie stawia");
		R("линейка: два клика по холсту — расстояние в мм", "ruler: two clicks on the canvas — distance in mm", "Lineal: zwei Klicks auf die Fläche — Abstand in mm", "linijka: dwa kliknięcia na płótnie — odległość w mm");
		R("роль: ЯКОРЬ — область закрепления, частиц не даёт", "role: ANCHOR — a pinning region, it makes no particles", "Rolle: ANKER — ein Fixierbereich, erzeugt keine Partikel", "rola: KOTWICA — obszar zamocowania, nie tworzy cząstek");
		R("роль: {0}", "role: {0}", "Rolle: {0}", "rola: {0}");
		R("вершина снята", "vertex removed", "Eckpunkt entfernt", "usunięto wierzchołek");
		R("отменено (в стопке {0})", "undone ({0} left in the stack)", "rückgängig gemacht ({0} im Stapel)", "cofnięto (w stosie {0})");
		R("отменять нечего", "nothing to undo", "nichts rückgängig zu machen", "nie ma czego cofnąć");
		R("линейка: вторая точка", "ruler: second point", "Lineal: zweiter Punkt", "linijka: drugi punkt");
		R("линейка: {0:F2} мм = {1:F1} Δx", "ruler: {0:F2} mm = {1:F1} Δx", "Lineal: {0:F2} mm = {1:F1} Δx", "linijka: {0:F2} mm = {1:F1} Δx");
		R("линейка: {0:F2} мм", "ruler: {0:F2} mm", "Lineal: {0:F2} mm", "linijka: {0:F2} mm");
		R("декали с таким именем нет", "there is no decal with that name", "kein Decal mit diesem Namen", "nie ma kalkomanii o tej nazwie");
		R("декаль {0} поставлена ({1}); она всё ещё в руке", "decal {0} placed ({1}); it is still in hand", "Decal {0} gesetzt ({1}); es ist noch in der Hand", "kalkomania {0} postawiona ({1}); nadal jest w ręce");
		R("без поворота", "no rotation", "ohne Drehung", "bez obrotu");
		R("вторая точка", "second point", "zweiter Punkt", "drugi punkt");
		R("радиус: второй клик", "radius: second click", "Radius: zweiter Klick", "promień: drugie kliknięcie");
		R("ластик: штамп стёр узлы под собой", "eraser: the stamp erased the nodes under it", "Radierer: der Stempel hat die Knoten darunter gelöscht", "gumka: stempel wymazał węzły pod sobą");
		R("кисть: штамп поставлен", "brush: stamp placed", "Pinsel: Stempel gesetzt", "pędzel: postawiono stempel");
		R("{0}: фигура добавлена", "{0}: shape added", "{0}: Form hinzugefügt", "{0}: dodano kształt");
		R("прямоугольник", "rectangle", "Rechteck", "prostokąt");
		R("круг", "circle", "Kreis", "koło");
		R("вершина {0}: ({1:F1}, {2:F1}) мм", "vertex {0}: ({1:F1}, {2:F1}) mm", "Eckpunkt {0}: ({1:F1}, {2:F1}) mm", "wierzchołek {0}: ({1:F1}, {2:F1}) mm");
		R("вещество: {0}", "material: {0}", "Material: {0}", "materiał: {0}");
		R("пресет загружен: {0}", "preset loaded: {0}", "Vorlage geladen: {0}", "wczytano gotową scenę: {0}");
		R("поле: ТЕМПЕРАТУРА — температура решателя как есть, шкала 300 K … T_melt сцены (свечение — это показ, прибор — это поле)", "field: TEMPERATURE — the solver temperature as is, scale 300 K … scene T_melt (glow is a display, the instrument is this field)", "Feld: TEMPERATUR — die Lösertemperatur unverändert, Skala 300 K … T_melt der Szene (Glühen ist Darstellung, das Messgerät ist dieses Feld)", "pole: TEMPERATURA — temperatura solvera bez zmian, skala 300 K … T_melt sceny (żarzenie to pokaz, przyrządem jest to pole)");
		R("поле: ", "field: ", "Feld: ", "pole: ");
		R("выстрела нет — останавливать нечего", "no shot — nothing to stop", "kein Schuss — nichts anzuhalten", "brak strzału — nie ma czego zatrzymać");
		R("выстрел остановлен — можно править фигуры и стрелять снова", "shot stopped — you can edit the drawing and fire again", "Schuss angehalten — die Zeichnung lässt sich bearbeiten und erneut schießen", "strzał zatrzymany — można poprawić rysunek i strzelać ponownie");
		R("ластик: ведите — след стирает", "eraser: drag — the trail erases", "Radierer: ziehen — die Spur löscht", "gumka: prowadź — ślad wymazuje");
		R("кисть: ведите — след красит", "brush: drag — the trail paints", "Pinsel: ziehen — die Spur malt", "pędzel: prowadź — ślad maluje");
		R("ластик: мазок закончен — узлы под ним пусты", "eraser: stroke finished — the nodes under it are empty", "Radierer: Strich beendet — die Knoten darunter sind leer", "gumka: pociągnięcie zakończone — węzły pod nim są puste");
		R("кисть: мазок закончен", "brush: stroke finished", "Pinsel: Strich beendet", "pędzel: pociągnięcie zakończone");
		R("{0}: угол {1:F0}° (скорость — вдоль острия)", "{0}: angle {1:F0}° (velocity along the tip)", "{0}: Winkel {1:F0}° (Geschwindigkeit entlang der Spitze)", "{0}: kąt {1:F0}° (prędkość wzdłuż ostrza)");
		R("декаль {0}: угол {1:F0}°", "decal {0}: angle {1:F0}°", "Decal {0}: Winkel {1:F0}°", "kalkomania {0}: kąt {1:F0}°");
		R("фигура под замком — снимите замок, чтобы вертеть", "the shape is locked — unlock it to rotate", "die Form ist gesperrt — zum Drehen entsperren", "kształt jest zablokowany — odblokuj, aby obracać");
		R("фигура {0}: угол {1:F0}°", "shape {0}: angle {1:F0}°", "Form {0}: Winkel {1:F0}°", "kształt {0}: kąt {1:F0}°");
		R(" (точный шаг 1°)", " (fine step 1°)", " (feiner Schritt 1°)", " (dokładny krok 1°)");
		R(" (шаг 15°)", " (step 15°)", " (Schritt 15°)", " (krok 15°)");
		R("выстрел готов: {0} частиц; оценка {1} — дольше {2:F0} с, нужно подтверждение", "shot ready: {0} particles; estimate {1} — longer than {2:F0} s, confirmation needed", "Schuss bereit: {0} Partikel; Schätzung {1} — länger als {2:F0} s, Bestätigung nötig", "strzał gotowy: {0} cząstek; szacunek {1} — dłużej niż {2:F0} s, potrzebne potwierdzenie");
		R("выстрел пошёл: {0} частиц; оценка {1}", "shot running: {0} particles; estimate {1}", "Schuss läuft: {0} Partikel; Schätzung {1}", "strzał trwa: {0} cząstek; szacunek {1}");
		R("сначала «ОГОНЬ» — он строит сцену и запускает счёт", "press FIRE first — it builds the scene and starts the run", "zuerst FEUER — es baut die Szene und startet die Rechnung", "najpierw OGNIA — buduje scenę i uruchamia obliczenia");
		R("счёт идёт", "running", "Rechnung läuft", "obliczenia trwają");
		R("пауза", "paused", "Pause", "pauza");
		R("выстрела нет", "no shot", "kein Schuss", "brak strzału");
		R(" | t = {0:F2} мкс, шаг {1}", " | t = {0:F2} µs, step {1}", " | t = {0:F2} µs, Schritt {1}", " | t = {0:F2} µs, krok {1}");
		R(", тел {0}", ", bodies {0}", ", Körper {0}", ", ciał {0}");
		R(" (тела: по решателю)", " (bodies: per solver)", " (Körper: laut Löser)", " (ciała: według solvera)");
		R("колесо: ПОВОРОТ {0} {1:F0}° · {2} точно", "wheel: ROTATE {0} {1:F0}° · {2} fine", "Mausrad: DREHEN {0} {1:F0}° · {2} fein", "kółko: OBRÓT {0} {1:F0}° · {2} dokładnie");
		R("снаряда", "projectile", "Geschoss", "pocisku");
		R("декали", "decal", "Decal", "kalkomanii");
		R("колесо: ПОВОРОТ фигуры {0} ({1:F0}°) · ВЫБР снять — зум", "wheel: ROTATE shape {0} ({1:F0}°) · deselect — zoom", "Mausrad: Form {0} DREHEN ({1:F0}°) · Auswahl aufheben — Zoom", "kółko: OBRÓT kształtu {0} ({1:F0}°) · odznacz — powiększenie");
		R("колесо: зум · {0} — кисть", "wheel: zoom · {0} — brush", "Mausrad: Zoom · {0} — Pinsel", "kółko: powiększenie · {0} — pędzel");
		R(" (свеч. не для прибора)", " (glow: not for the instrument)", " (Glühen: nicht fürs Messgerät)", " (żarz.: nie dla przyrządu)");
		R(" (свеч. не для поля ТЕМПЕРАТУРА)", " (glow: not for the TEMPERATURE field)", " (Glühen: nicht fürs Feld TEMPERATUR)", " (żarz.: nie dla pola TEMPERATURA)");
		R(" (свеч. отказано)", " (glow refused)", " (Glühen verweigert)", " (żarz. odmówione)");
		R(" (свеч. ВЫКЛ)", " (glow OFF)", " (Glühen AUS)", " (żarz. WYŁ.)");
		R("стаб. {0} | тела {1}{2} | вид {3}{4} | {5} | сетка {6} Δx | Δx {7:F2} мм | {8:F0} пикс/Δx | частиц {9}", "stab. {0} | bodies {1}{2} | view {3}{4} | {5} | grid {6} Δx | Δx {7:F2} mm | {8:F0} px/Δx | particles {9}", "Stab. {0} | Körper {1}{2} | Ansicht {3}{4} | {5} | Raster {6} Δx | Δx {7:F2} mm | {8:F0} px/Δx | Partikel {9}", "stab. {0} | ciała {1}{2} | widok {3}{4} | {5} | siatka {6} Δx | Δx {7:F2} mm | {8:F0} px/Δx | cząstek {9}");
		R("ВКЛ", "ON", "AN", "WŁ.");
		R("ВЫКЛ", "OFF", "AUS", "WYŁ.");
		R("РАЗД.", "SEP.", "GETR.", "ROZDZ.");
		R("СЦЕПЛ.", "JOINED", "VERB.", "POŁĄCZ.");
		R(" | стёрто {0}", " | eroded {0}", " | erodiert {0}", " | zerodowano {0}");
		R(" | стёрто {0}", " | eroded {0}", " | erodiert {0}", " | zerodowano {0}");
		R("{0} {1:F2} мм = {2:F1} Δx", "{0} {1:F2} mm = {2:F1} Δx", "{0} {1:F2} mm = {2:F1} Δx", "{0} {1:F2} mm = {2:F1} Δx");
		R("квадрат", "square", "Quadrat", "kwadrat");
		R("   тоньше 1 Δx — МОЖЕТ НЕ ДАТЬ НИ УЗЛА", "   thinner than 1 Δx — MAY GIVE NO NODE AT ALL", "   dünner als 1 Δx — ERGIBT EVTL. KEINEN KNOTEN", "   cieńszy niż 1 Δx — MOŻE NIE DAĆ ANI JEDNEGO WĘZŁA");
		R("   тоньше 3 Δx — НЕ ИЗГИБАЕТСЯ", "   thinner than 3 Δx — DOES NOT BEND", "   dünner als 3 Δx — BIEGT SICH NICHT", "   cieńszy niż 3 Δx — NIE ZGINA SIĘ");
		R("оценка появится по нажатию «ОГОНЬ»", "the estimate appears when you press FIRE", "die Schätzung erscheint beim Drücken von FEUER", "szacunek pojawi się po naciśnięciu OGNIA");
		R("оценка: ", "estimate: ", "Schätzung: ", "szacunek: ");
		R("ПАУЗА", "PAUSE", "PAUSE", "PAUZA");
		R("ПРОДОЛЖИТЬ", "RESUME", "WEITER", "WZNÓW");
		R("{0:F0} мм = {1:F0} Δx", "{0:F0} mm = {1:F0} Δx", "{0:F0} mm = {1:F0} Δx", "{0:F0} mm = {1:F0} Δx");
		R("{0:F2} мм", "{0:F2} mm", "{0:F2} mm", "{0:F2} mm");
		R("свечение: блоки веществ сцены не переданы виду — шкала неизвестна", "glow: the scene's material blocks were not passed to the view — the scale is unknown", "Glühen: die Materialblöcke der Szene wurden der Ansicht nicht übergeben — die Skala ist unbekannt", "żarzenie: bloki materiałów sceny nie zostały przekazane widokowi — skala nieznana");
		P("свечение: T_melt сцены {0:F0} K ниже низа палитры {1:F0} K", "glow: the scene T_melt {0:F0} K is below the bottom of the palette {1:F0} K", "Glühen: T_melt der Szene {0:F0} K liegt unter dem Palettenboden {1:F0} K", "żarzenie: T_melt sceny {0:F0} K jest poniżej dołu palety {1:F0} K");
		R("свечение: палитра вернула пустой ППТ", "glow: the palette returned an empty LUT", "Glühen: die Palette lieferte eine leere LUT", "żarzenie: paleta zwróciła pustą tablicę LUT");
		R("свечение: виду не сообщена длительность выстрела (PlannedDurationSeconds) — τ остывания не определено", "glow: the view was not told the shot duration (PlannedDurationSeconds) — the cooling τ is undefined", "Glühen: der Ansicht wurde die Schussdauer nicht mitgeteilt (PlannedDurationSeconds) — τ der Abkühlung ist unbestimmt", "żarzenie: widokowi nie podano czasu strzału (PlannedDurationSeconds) — τ stygnięcia nieokreślone");
		R("ПО ВЕЩЕСТВУ", "BY MATERIAL", "NACH MATERIAL", "WG MATERIAŁU");
		R("по веществу", "by material", "nach Material", "wg materiału");
		R("ЦВЕТ: {0}", "COLOUR: {0}", "FARBE: {0}", "KOLOR: {0}");
		R("цвет — только показ (поле ВЕЩЕСТВО): вещество, роль и скорость он не меняет; перекрасить — кистью того же вещества", "colour is display only (MATERIAL field): it changes neither material, role nor speed; to recolour, paint with the same material", "Farbe ist nur Darstellung (Feld MATERIAL): Material, Rolle und Geschwindigkeit bleiben; umfärben — mit dem Pinsel desselben Materials", "kolor to tylko wygląd (pole MATERIAŁ): nie zmienia materiału, roli ani prędkości; przemalować — pędzlem tego samego materiału");
		R("цвет: по веществу — новые мазки и фигуры красятся цветом вещества", "colour: by material — new strokes and shapes take the material colour", "Farbe: nach Material — neue Striche und Formen erhalten die Materialfarbe", "kolor: wg materiału — nowe pociągnięcia i kształty mają kolor materiału");
		R("цвет: {0} — новые мазки, фигуры и декали пластин рисуются им", "colour: {0} — new strokes, shapes and plate decals are drawn in it", "Farbe: {0} — neue Striche, Formen und Platten-Decals werden damit gezeichnet", "kolor: {0} — nowe pociągnięcia, kształty i kalkomanie płyt rysowane są nim");
		R("Кисть", "Brush", "Pinsel", "Pędzel");
		R("КРУГЛАЯ", "ROUND", "RUND", "OKRĄGŁY");
		R("КВАДРАТНАЯ", "SQUARE", "ECKIG", "KWADRATOWY");
		R("кисть: КРУГЛАЯ — курсор показывает круг отпечатка", "brush: ROUND — the cursor shows the circular footprint", "Pinsel: RUND — der Mauszeiger zeigt den runden Abdruck", "pędzel: OKRĄGŁY — kursor pokazuje okrągły odcisk");
		R("кисть: КВАДРАТНАЯ — курсор показывает квадрат отпечатка", "brush: SQUARE — the cursor shows the square footprint", "Pinsel: ECKIG — der Mauszeiger zeigt den quadratischen Abdruck", "pędzel: KWADRATOWY — kursor pokazuje kwadratowy odcisk");
		R("Декали", "Decals", "Decals", "Kalkomanie");
		R("толщина", "thickness", "Dicke", "grubość");
		R("длина", "length", "Länge", "długość");
		R("УЗЛЫ ЯКОРЯ", "ANCHOR NODES", "ANKERKNOTEN", "WĘZŁY KOTWICY");
		R("поставлен {0} в масштабе {1}: Ø{2:F1} × {3:F1} мм (натура Ø{4:F1} × {5:F0} мм), v0 {6:F0} м/с, {7}", "placed {0} at scale {1}: Ø{2:F1} × {3:F1} mm (full size Ø{4:F1} × {5:F0} mm), v0 {6:F0} m/s, {7}", "gesetzt: {0} im Maßstab {1}: Ø{2:F1} × {3:F1} mm (Original Ø{4:F1} × {5:F0} mm), v0 {6:F0} m/s, {7}", "postawiono {0} w skali {1}: Ø{2:F1} × {3:F1} mm (naturalnie Ø{4:F1} × {5:F0} mm), v0 {6:F0} m/s, {7}");
		R(" — Ø в модели {0:F1} Δx: сердечник почти не разрешён решёткой", " — Ø in the model {0:F1} Δx: the core is barely resolved by the lattice", " — Ø im Modell {0:F1} Δx: der Kern wird vom Gitter kaum aufgelöst", " — Ø w modelu {0:F1} Δx: rdzeń ledwo rozdzielony przez siatkę");
		R("толщина {0:F1} мм · длина {1:F0} мм · масштаб модели {2}", "thickness {0:F1} mm · length {1:F0} mm · model scale {2}", "Dicke {0:F1} mm · Länge {1:F0} mm · Modellmaßstab {2}", "grubość {0:F1} mm · długość {1:F0} mm · skala modelu {2}");
		R("{0} в руке: вещество «экипаж», органы раскрашены; масштаб модели {1} (стоя {2:F0} мм); клик ставит", "{0} in hand: material \"crew\", organs coloured; model scale {1} (standing {2:F0} mm); click places it", "{0} in der Hand: Material „Besatzung“, Organe eingefärbt; Modellmaßstab {1} (stehend {2:F0} mm); Klick setzt", "{0} w ręce: materiał „załoga”, narządy pokolorowane; skala modelu {1} (stojący {2:F0} mm); kliknięcie stawia");
		R("имя сохранения:", "save name:", "Name des Spielstands:", "nazwa zapisu:");
		R("ВЫБЕРИТЕ СОХРАНЕНИЕ", "CHOOSE A SAVE", "SPIELSTAND WÄHLEN", "WYBIERZ ZAPIS");
		R("сохранений 2D: {0} — выберите, что загрузить", "2D saves: {0} — choose one to load", "2D-Spielstände: {0} — zum Laden einen wählen", "zapisów 2D: {0} — wybierz, co wczytać");
	}

	static Flat2DText()
	{
		D = new Dictionary<string, string[]>(StringComparer.Ordinal);
		Patterns = new List<Pattern>();
		duplicates = new List<string>();
		Placeholder = new Regex("\\{(\\d+)(?:[,:][^}]*)?\\}", RegexOptions.CultureInvariant);
		AddScreen();
		AddCatalogue();
		AddModel();
	}

	private static void R(string ru, string en, string de, string pl)
	{
		if (D.ContainsKey(ru))
		{
			duplicates.Add(ru);
		}
		else
		{
			D.Add(ru, new string[3] { en, de, pl });
		}
	}

	private static void P(string ruFormat, string en, string de, string pl)
	{
		StringBuilder re = new StringBuilder("^");
		List<int> order = new List<int>();
		int pos = 0;
		foreach (Match match in Placeholder.Matches(ruFormat))
		{
			re.Append(Regex.Escape(ruFormat.Substring(pos, match.Index - pos)));
			re.Append("(.+?)");
			order.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
			pos = match.Index + match.Length;
		}
		re.Append(Regex.Escape(ruFormat.Substring(pos)));
		re.Append('$');
		Patterns.Add(new Pattern
		{
			re = new Regex(re.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant),
			ru = ruFormat,
			tr = new string[3] { en, de, pl },
			order = order.ToArray()
		});
	}

	private static int Column(Language l)
	{
		if (l == Language.DE)
		{
			return 1;
		}
		if (l == Language.PL)
		{
			return 2;
		}
		return 0;
	}

	public static string Tr(string ru)
	{
		if (string.IsNullOrEmpty(ru) || ShowsRussian)
		{
			return ru;
		}
		int column = Column(Lang);
		if (D.TryGetValue(ru, out string[] direct))
		{
			return direct[column];
		}
		foreach (Pattern pattern in Patterns)
		{
			Match match = pattern.re.Match(ru);
			if (!match.Success)
			{
				continue;
			}
			object[] args = new object[pattern.order.Length == 0 ? 0 : Max(pattern.order) + 1];
			for (int i = 0; i < pattern.order.Length; i++)
			{
				args[pattern.order[i]] = match.Groups[i + 1].Value;
			}
			return string.Format(Culture, StripFormats(pattern.tr[column]), args);
		}
		return ru;
	}

	public static string F(string ruFormat, params object[] args)
	{
		try
		{
			string format = ruFormat;
			if (!ShowsRussian && D.TryGetValue(ruFormat, out string[] translated))
			{
				format = translated[Column(Lang)];
			}
			return string.Format(Culture, format, args);
		}
		catch (FormatException)
		{
			return string.Format(CultureInfo.CurrentCulture, ruFormat, args);
		}
	}

	private static int Max(int[] a)
	{
		int max = 0;
		for (int i = 0; i < a.Length; i++)
		{
			max = Math.Max(max, a[i]);
		}
		return max;
	}

	private static string StripFormats(string fmt)
	{
		return Regex.Replace(fmt, "\\{(\\d+)[,:][^}]*\\}", "{$1}");
	}

	public static bool Has(string ru)
	{
		if (string.IsNullOrEmpty(ru) || D.ContainsKey(ru))
		{
			return true;
		}
		foreach (Pattern pattern in Patterns)
		{
			if (pattern.re.IsMatch(ru))
			{
				return true;
			}
		}
		return false;
	}

	public static List<string> PlaceholderMismatches()
	{
		List<string> result = new List<string>();
		string[] names = new string[3] { "EN", "DE", "PL" };
		foreach (KeyValuePair<string, string[]> row in D)
		{
			string signature = Signature(row.Key);
			for (int i = 0; i < 3; i++)
			{
				if (string.IsNullOrEmpty(row.Value[i]))
				{
					result.Add(names[i] + " пусто: " + row.Key);
				}
				else if (Signature(row.Value[i]) != signature)
				{
					result.Add(names[i] + " {n} ≠ RU: " + row.Key);
				}
			}
		}
		foreach (Pattern pattern in Patterns)
		{
			for (int i = 0; i < 3; i++)
			{
				if (Signature(pattern.tr[i]) != Signature(pattern.ru))
				{
					result.Add(names[i] + " {n} ≠ RU (шаблон): " + pattern.ru);
				}
			}
		}
		return result;
	}

	private static string Signature(string s)
	{
		SortedSet<int> indices = new SortedSet<int>();
		foreach (Match match in Placeholder.Matches(s))
		{
			indices.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
		}
		return string.Join(",", indices);
	}
}
