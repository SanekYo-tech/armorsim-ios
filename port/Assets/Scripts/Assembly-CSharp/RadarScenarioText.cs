using System.Collections.Generic;

public static class RadarScenarioText
{
	private static readonly Dictionary<string, string[]> D;

	public static IEnumerable<string> Keys => D.Keys;

	private static void AddLaunch()
	{
		D.Add("map.coverage", new string[5] { "Игровой радиус", "Game radius", "Уен радиусы", "Spielradius", "Promień gry" });
		D.Add("map.launcher_hint", new string[5] { "Пусковая установка использует отметки отдельной РЛС. Собственного радара нет.", "The launcher uses contacts from a separate radar. It has no radar of its own.", "Җибәрү җайланмасы аерым радар тамгаларын куллана. Үз радары юк.", "Der Werfer nutzt Kontakte eines separaten Radars. Er besitzt kein eigenes Radar.", "Wyrzutnia korzysta z kontaktów oddzielnego radaru. Nie ma własnego radaru." });
		D.Add("launch.title", new string[5] { "ПУСКОВЫЕ ПВО", "LAUNCHERS", "ҖИБӘРҮ ҖАЙЛАНМАЛАРЫ", "WERFER", "WYRZUTNIE" });
		D.Add("launch.unit", new string[5] { "Пусковая", "Launcher", "Җибәрү җайланмасы", "Werfer", "Wyrzutnia" });
		D.Add("launch.short", new string[5] { "ПУ", "L", "Җ", "W", "W" });
		D.Add("launch.ammo", new string[5] { "Запас", "Ammo", "Запас", "Vorrat", "Zapas" });
		D.Add("launch.active", new string[5] { "В пути", "Flying", "Юлда", "Unterwegs", "W locie" });
		D.Add("launch.arrived", new string[5] { "Прибыли", "Arrived", "Килде", "Angekommen", "Dotarły" });
		D.Add("launch.fire", new string[5] { "Пуск ракеты по отметке", "Launch missile at mark", "Тамгага ракета җибәрү", "Rakete zur Markierung starten", "Wystrzel rakietę do oznaczenia" });
		D.Add("launch.sent", new string[5] { "Игровая ракета запущена.", "Game missile launched.", "Уен ракетасы җибәрелде.", "Spielrakete gestartet.", "Rakieta gry wystrzelona." });
		D.Add("launch.reload", new string[5] { "Перезарядка:", "Reloading:", "Кабат кору:", "Nachladen:", "Przeładowanie:" });
		D.Add("launch.empty", new string[5] { "Ракеты закончились. Перезапустите сценарий для пополнения.", "No missiles left. Restart to refill.", "Ракеталар бетте. Яңадан башлагыз.", "Keine Raketen mehr. Zum Auffüllen neu starten.", "Brak rakiet. Uruchom scenariusz ponownie, aby uzupełnić." });
		D.Add("launch.noLauncher", new string[5] { "Сначала разместите пусковую на карте.", "Place a launcher on the map first.", "Башта картага җибәрү җайланмасын куегыз.", "Zuerst einen Werfer auf der Karte platzieren.", "Najpierw umieść wyrzutnię na mapie." });
		D.Add("launch.noContact", new string[5] { "Выберите видимую отметку на экране РЛС.", "Select a visible radar mark.", "Радарда күренгән тамганы сайлагыз.", "Eine sichtbare Radarmarkierung wählen.", "Wybierz widoczne oznaczenie radaru." });
		D.Add("launch.paused", new string[5] { "Снимите паузу для пуска.", "Resume before launching.", "Җибәрү өчен паузаны алыгыз.", "Vor dem Start fortsetzen.", "Wznów przed wystrzeleniem." });
	}

	private static void AddMap()
	{
		D.Add("map.title", new string[5] { "КАРТА СЦЕНАРИЯ", "SCENARIO MAP", "СЦЕНАРИЙ КАРТАСЫ", "SZENARIOKARTE", "MAPA SCENARIUSZA" });
		D.Add("map.units", new string[5] { "условных единиц", "game units", "уен берәмлеге", "Spieleinheiten", "jednostek gry" });
		D.Add("map.aircraft", new string[5] { "Самолёты", "Aircraft", "Очкычлар", "Flugzeuge", "Samoloty" });
		D.Add("map.add", new string[5] { "Добавить", "Add", "Өстәргә", "Hinzufügen", "Dodaj" });
		D.Add("map.remove", new string[5] { "Удалить", "Remove", "Бетерергә", "Entfernen", "Usuń" });
		D.Add("map.name", new string[5] { "Имя", "Name", "Исем", "Name", "Nazwa" });
		D.Add("map.model", new string[5] { "Модель", "Model", "Модель", "Modell", "Model" });
		D.Add("map.import", new string[5] { "Загрузить STL / OBJ", "Import STL / OBJ", "STL / OBJ йөкләргә", "STL / OBJ importieren", "Importuj STL / OBJ" });
		D.Add("map.model_hint", new string[5] { "Геометрия меняет только внешний вид", "Geometry affects appearance only", "Геометрия тышкы күренешне генә үзгәртә", "Geometrie ändert nur das Aussehen", "Geometria zmienia tylko wygląd" });
		D.Add("map.coating", new string[5] { "Покрытие (игровое)", "Coating (fictional)", "Каплама (уен өчен)", "Beschichtung (fiktiv)", "Powłoka (fikcyjna)" });
		D.Add("map.coat0", new string[5] { "Без покрытия", "Bare", "Капламасыз", "Unbeschichtet", "Bez powłoki" });
		D.Add("map.coat1", new string[5] { "Вуаль", "Veil", "Пәрдә", "Schleier", "Woal" });
		D.Add("map.coat2", new string[5] { "Туман", "Mist", "Томан", "Nebel", "Mgła" });
		D.Add("map.coat3", new string[5] { "Тень", "Shadow", "Күләгә", "Schatten", "Cień" });
		D.Add("map.visibility", new string[5] { "Условная заметность", "Visibility score", "Шартлы күренүчәнлек", "Sichtbarkeitswert", "Umowna widoczność" });
		D.Add("map.speed", new string[5] { "Скорость, ед./с", "Speed, units/s", "Тизлек, бер./с", "Tempo, Einh./s", "Prędkość, jedn./s" });
		D.Add("map.loop", new string[5] { "Замкнуть маршрут", "Loop route", "Юлны ябарга", "Route schließen", "Zamknij trasę" });
		D.Add("map.route", new string[5] { "Маршрут", "Route", "Очыш юлы", "Route", "Trasa" });
		D.Add("map.select", new string[5] { "Выбрать / двигать", "Select / move", "Сайларга / күчерергә", "Wählen / bewegen", "Wybierz / przesuń" });
		D.Add("map.draw", new string[5] { "Рисовать маршрут", "Draw route", "Очыш юлын сызарга", "Route zeichnen", "Rysuj trasę" });
		D.Add("map.clear", new string[5] { "Очистить маршрут", "Clear route", "Очыш юлын чистартырга", "Route leeren", "Wyczyść trasę" });
		D.Add("map.default_altitude", new string[5] { "Высота новых точек, м", "New-point altitude, m", "Яңа нокталар биеклеге, м", "Höhe neuer Punkte, m", "Wysokość nowych punktów, m" });
		D.Add("map.waypoint", new string[5] { "Точка", "Waypoint", "Нокта", "Wegpunkt", "Punkt trasy" });
		D.Add("map.altitude", new string[5] { "Высота над землёй, м", "Height above ground, m", "Җир өстеннән биеклек, м", "Höhe über Grund, m", "Wysokość nad ziemią, m" });
		D.Add("map.delete_point", new string[5] { "Удалить точку", "Delete waypoint", "Ноктаны бетерергә", "Wegpunkt löschen", "Usuń punkt" });
		D.Add("map.stations", new string[5] { "Наземные РЛС и пусковые", "Ground radars and launchers", "Җир радарлары һәм җайланмалар", "Bodenradare und Werfer", "Radary naziemne i wyrzutnie" });
		D.Add("map.add_radar", new string[5] { "Добавить радар", "Add radar", "Радар өстәргә", "Radar hinzufügen", "Dodaj radar" });
		D.Add("map.add_post", new string[5] { "Добавить ПУ", "Add launcher", "Җайланма өстәү", "Werfer hinzufügen", "Dodaj wyrzutnię" });
		D.Add("map.radar", new string[5] { "Радар", "Radar", "Радар", "Radar", "Radar" });
		D.Add("map.post", new string[5] { "Пусковая ПВО", "Launcher", "ПВО җайланмасы", "Werfer", "Wyrzutnia OPL" });
		D.Add("map.preset", new string[5] { "Тип станции", "Station type", "Станция төре", "Stationstyp", "Typ stacji" });
		D.Add("map.station_hint", new string[5] { "Радиус условный и зависит от типа. Круг ограничивает появление эха.", "Radius is fictional and varies by type. The circle limits radar echoes.", "Радиус шартлы һәм төргә бәйле. Кайтаваз түгәрәк эчендә күренә.", "Der fiktive Radius hängt vom Typ ab. Der Kreis begrenzt Radarechos.", "Umowny promień zależy od typu. Okrąg ogranicza echa radaru." });
		D.Add("map.no_plane", new string[5] { "Добавьте самолёт на карту", "Add an aircraft to the map", "Картага очкыч өстәгез", "Flugzeug auf der Karte platzieren", "Dodaj samolot do mapy" });
		D.Add("map.no_station", new string[5] { "Нет выбранной станции", "No station selected", "Станция сайланмаган", "Keine Station ausgewählt", "Nie wybrano stacji" });
		D.Add("map.no_point", new string[5] { "Выберите точку маршрута", "Select a waypoint", "Очыш юлы ноктасын сайлагыз", "Wegpunkt auswählen", "Wybierz punkt trasy" });
		D.Add("map.fit", new string[5] { "Вся карта", "Fit map", "Бөтен карта", "Gesamte Karte", "Cała mapa" });
		D.Add("map.forest", new string[5] { "Лес", "Forest", "Урман", "Wald", "Las" });
		D.Add("map.city", new string[5] { "Город", "City", "Шәһәр", "Stadt", "Miasto" });
		D.Add("map.mountain", new string[5] { "Горы", "Mountains", "Таулар", "Berge", "Góry" });
		D.Add("map.plain", new string[5] { "Равнина", "Plain", "Тигезлек", "Ebene", "Równina" });
		D.Add("map.water", new string[5] { "Вода", "Water", "Су", "Wasser", "Woda" });
		D.Add("map.hint_select", new string[5] { "Тяните точки и станции. Колесо: масштаб; средняя кнопка: сдвиг.", "Drag waypoints and stations. Wheel: zoom; middle button: pan.", "Нокталарны һәм станцияләрне күчерегез. Тәгәрмәч: масштаб; урта төймә: картаны күчерү.", "Punkte und Stationen ziehen. Mausrad: Zoom; mittlere Taste: Karte verschieben.", "Przeciągaj punkty i stacje. Kółko: zoom; środkowy przycisk: przesuwanie mapy." });
		D.Add("map.hint_draw", new string[5] { "Клик добавляет точку. Удерживайте кнопку, чтобы рисовать маршрут.", "Click to add a waypoint, or hold and draw a route.", "Басу нокта өсти. Очыш юлын сызу өчен төймәне тотып торыгыз.", "Klicken setzt einen Punkt. Zum Zeichnen die Taste gedrückt halten.", "Kliknij, aby dodać punkt, lub przytrzymaj przycisk i rysuj trasę." });
		D.Add("map.hint_aircraft", new string[5] { "Щёлкните на карте: точка старта нового самолёта.", "Click the map to place the new aircraft spawn.", "Яңа очкычның башлану ноктасын кую өчен картага басыгыз.", "Auf die Karte klicken, um den Startpunkt des Flugzeugs zu setzen.", "Kliknij mapę, aby ustawić punkt startu nowego samolotu." });
		D.Add("map.hint_radar", new string[5] { "Щёлкните на карте: разместить радар.", "Click the map to place a radar.", "Радар урнаштыру өчен картага басыгыз.", "Auf die Karte klicken, um ein Radar zu platzieren.", "Kliknij mapę, aby umieścić radar." });
		D.Add("map.hint_post", new string[5] { "Щёлкните на карте: разместить пусковую ПВО.", "Click the map to place a launcher.", "Җибәрү җайланмасын кую өчен картага басыгыз.", "Auf die Karte klicken, um einen Werfer zu platzieren.", "Kliknij mapę, aby umieścić wyrzutnię." });
		D.Add("map.limit", new string[5] { "Достигнут лимит", "Limit reached", "Чиккә җиттегез", "Grenze erreicht", "Osiągnięto limit" });
		D.Add("map.no_route", new string[5] { "Оставьте минимум две точки перед запуском", "Keep at least two waypoints before starting", "Башлар алдыннан кимендә ике нокта калдырыгыз", "Vor dem Start mindestens zwei Wegpunkte setzen", "Przed startem ustaw co najmniej dwa punkty" });
	}

	private static void AddPhysical()
	{
		D.Add("map.model.arcade", new string[5] { "УСЛОВНАЯ ИГРОВАЯ МОДЕЛЬ", "ARCADE GAME MODEL", "ШАРТЛЫ УЕН МОДЕЛЕ", "ARCADE-SPIELMODELL", "UMOWNY MODEL GRY" });
		D.Add("map.model.physical", new string[5] { "ФИЗИЧЕСКАЯ МОДЕЛЬ: атлас ЭПР · уравнение радиолокации", "PHYSICAL MODEL: RCS atlas · radar equation", "ФИЗИК МОДЕЛЬ: ЭПР атласы · радиолокация тигезләмәсе", "PHYSIKALISCHES MODELL: RCS-Atlas · Radargleichung", "MODEL FIZYCZNY: atlas RCS · równanie radarowe" });
		D.Add("map.model.beta", new string[5] { "(бета)", "(beta)", "(бета)", "(Beta)", "(beta)" });
		D.Add("map.obs.title", new string[5] { "НАБЛЮДАТЕЛЬ", "OBSERVER", "КҮЗӘТҮЧЕ", "BEOBACHTER", "OBSERWATOR" });
		D.Add("map.obs.truth", new string[5] { "ИСТИНА — оператору не видна", "TRUTH — not visible to the operator", "ХАКЫЙКАТЬ — операторга күренми", "WAHRHEIT — für den Bediener unsichtbar", "PRAWDA — niewidoczna dla operatora" });
		D.Add("map.obs.view", new string[5] { "Вид из РЛС", "View from the radar", "РЛСтан караш", "Blick vom Radar", "Widok z radaru" });
		D.Add("map.obs.aspect", new string[5] { "Ракурс в осях модели", "Aspect in model axes", "Модель күчәрләрендә ракурс", "Aspekt in Modellachsen", "Aspekt w osiach modelu" });
		D.Add("map.obs.flicker", new string[5] { "ЭПР на ракурсе (среднее по ячейке)", "RCS at aspect (cell mean)", "Ракурстагы ЭПР (күзәнәк буенча уртача)", "RCS im Aspekt (Zellmittel)", "RCS w aspekcie (średnia komórki)" });
		D.Add("map.obs.aircraft", new string[5] { "Самолёт", "Aircraft", "Очкыч", "Flugzeug", "Samolot" });
		D.Add("map.obs.radar", new string[5] { "РЛС", "Radar", "РЛС", "Radar", "Radar" });
		D.Add("map.obs.position", new string[5] { "Положение x / y", "Position x / y", "Урын x / y", "Position x / y", "Położenie x / y" });
		D.Add("map.obs.altitude", new string[5] { "Высота над морем", "Height above sea", "Диңгездән биеклек", "Höhe über Meer", "Wysokość n.p.m." });
		D.Add("map.obs.speed", new string[5] { "Скорость V", "Speed V", "Тизлек V", "Geschwindigkeit V", "Prędkość V" });
		D.Add("map.obs.heading", new string[5] { "Курс ψ", "Heading ψ", "Курс ψ", "Kurs ψ", "Kurs ψ" });
		D.Add("map.obs.pitch", new string[5] { "Тангаж θ", "Pitch θ", "Тангаж θ", "Nickwinkel θ", "Pochylenie θ" });
		D.Add("map.obs.bank", new string[5] { "Крен φ", "Bank φ", "Крен φ", "Querlage φ", "Przechylenie φ" });
		D.Add("map.obs.load", new string[5] { "Перегрузка n", "Load factor n", "Артык йөкләнеш n", "Lastvielfaches n", "Przeciążenie n" });
		D.Add("map.obs.ground", new string[5] { "Дальность по земле G", "Ground range G", "Җир буенча ераклык G", "Bodenentfernung G", "Odległość po ziemi G" });
		D.Add("map.obs.slant", new string[5] { "Наклонная дальность R", "Slant range R", "Авыш ераклык R", "Schrägentfernung R", "Odległość skośna R" });
		D.Add("map.obs.elevRadar", new string[5] { "Угол места цели ε_r", "Target elevation ε_r", "Максат почмагы ε_r", "Zielelevation ε_r", "Elewacja celu ε_r" });
		D.Add("map.obs.elevTarget", new string[5] { "Угол места РЛС с цели ε_t", "Radar elevation from target ε_t", "Максаттан РЛС почмагы ε_t", "Radarelevation vom Ziel ε_t", "Elewacja radaru z celu ε_t" });
		D.Add("map.obs.bearing", new string[5] { "Пеленг на РЛС β", "Bearing to radar β", "РЛСка пеленг β", "Peilung zum Radar β", "Namiar na radar β" });
		D.Add("map.obs.horizon", new string[5] { "Запас до радиогоризонта", "Margin to radio horizon", "Радиогоризонтка кадәр запас", "Abstand zum Radiohorizont", "Zapas do radiohoryzontu" });
		D.Add("map.obs.axes", new string[5] { "синяя — нос, зелёная — верх, красная — правое крыло", "blue — nose, green — up, red — right wing", "зәңгәр — борын, яшел — өс, кызыл — уң канат", "blau — Nase, grün — oben, rot — rechte Tragfläche", "niebieska — nos, zielona — góra, czerwona — prawe skrzydło" });
		D.Add("map.obs.k2", new string[5] { "Индекс флуктуаций k2 ячейки", "Cell fluctuation index k2", "Күзәнәкнең флуктуация индексы k2", "Fluktuationsindex k2 der Zelle", "Wskaźnik fluktuacji k2 komórki" });
		D.Add("map.obs.trace", new string[5] { "След ракурса за 60 с", "Aspect trace, last 60 s", "Соңгы 60 с ракурс эзе", "Aspektspur der letzten 60 s", "Ślad aspektu z 60 s" });
		D.Add("map.phys.km", new string[5] { "км", "km", "км", "km", "km" });
		D.Add("map.phys.m", new string[5] { "м", "m", "м", "m", "m" });
		D.Add("map.phys.mps", new string[5] { "м/с", "m/s", "м/с", "m/s", "m/s" });
		D.Add("map.phys.units", new string[5] { "км · физическая модель", "km · physical model", "км · физик модель", "km · physikalisches Modell", "km · model fizyczny" });
		D.Add("map.phys.switch", new string[5] { "Модель обнаружения", "Detection model", "Табу модели", "Erfassungsmodell", "Model wykrywania" });
		D.Add("map.phys.arcadeShort", new string[5] { "Условная", "Arcade", "Шартлы", "Arcade", "Umowny" });
		D.Add("map.phys.physicalShort", new string[5] { "Физическая", "Physical", "Физик", "Physikalisch", "Fizyczny" });
		D.Add("map.phys.speed", new string[5] { "Скорость, м/с", "Speed, m/s", "Тизлек, м/с", "Geschwindigkeit, m/s", "Prędkość, m/s" });
		D.Add("map.phys.bank", new string[5] { "Предельный крен φ_max, °", "Maximum bank φ_max, °", "Иң зур крен φ_max, °", "Maximale Querlage φ_max, °", "Maks. przechylenie φ_max, °" });
		D.Add("map.phys.rollRate", new string[5] { "Скорость ввода крена p_max, °/с", "Roll rate p_max, °/s", "Крен кертү тизлеге p_max, °/с", "Rollrate p_max, °/s", "Prędkość przechylania p_max, °/s" });
		D.Add("map.phys.turn", new string[5] { "Радиус виража {0} км · перегрузка {1}", "Turn radius {0} km · load factor {1}", "Борылыш радиусы {0} км · йөкләнеш {1}", "Kurvenradius {0} km · Lastvielfaches {1}", "Promień zakrętu {0} km · przeciążenie {1}" });
		D.Add("map.phys.warnTurn", new string[5] { "Точка {0}: самолёт не успеет довернуть — пролетит мимо (соседняя точка ближе двух радиусов виража)", "Waypoint {0}: the aircraft cannot finish the turn and will fly past (neighbour closer than two turn radii)", "{0} нокта: очкыч борылып өлгерми — узып китәчәк (күрше нокта ике борылыш радиусыннан якынрак)", "Punkt {0}: Das Flugzeug schafft die Kurve nicht und fliegt vorbei (Nachbarpunkt näher als zwei Kurvenradien)", "Punkt {0}: samolot nie zdąży skręcić i przeleci obok (sąsiedni punkt bliżej niż dwa promienie zakrętu)" });
		D.Add("map.phys.station", new string[5] { "Станция (физ.)", "Station (physical)", "Станция (физ.)", "Station (physikalisch)", "Stacja (fizyczna)" });
		D.Add("map.phys.freq", new string[5] { "Частота {0} · λ = {1} см", "Frequency {0} · λ = {1} cm", "Ешлык {0} · λ = {1} см", "Frequenz {0} · λ = {1} cm", "Częstotliwość {0} · λ = {1} cm" });
		D.Add("map.phys.mast", new string[5] { "Мачта, м", "Mast, m", "Мачта, м", "Mast, m", "Maszt, m" });
		D.Add("map.phys.mastDefault", new string[5] { "Мачта станции по умолчанию ({0} м)", "Station default mast ({0} m)", "Станциянең гадәти мачтасы ({0} м)", "Standardmast der Station ({0} m)", "Domyślny maszt stacji ({0} m)" });
		D.Add("map.phys.sector", new string[5] { "Центр сектора ФАР", "Phased-array sector centre", "ФАР секторы үзәге", "Mitte des Phased-Array-Sektors", "Środek sektora anteny ścianowej" });
		D.Add("map.phys.horizonRows", new string[5] { "Радиогоризонт: цель на 1 км — {0} км, на 10 км — {1} км", "Radio horizon: target at 1 km — {0} km, at 10 km — {1} km", "Радиогоризонт: 1 км биеклектәге максат — {0} км, 10 км да — {1} км", "Radiohorizont: Ziel in 1 km — {0} km, in 10 km — {1} km", "Radiohoryzont: cel na 1 km — {0} km, na 10 km — {1} km" });
		D.Add("map.phys.ring", new string[5] { "цель {0} км", "target {0} km", "максат {0} км", "Ziel {0} km", "cel {0} km" });
		D.Add("map.phys.st.x_rot60", new string[5] { "РЛС X, 60 об/мин", "X-band radar, 60 rpm", "X РЛС, 60 әйл./мин", "X-Band-Radar, 60 U/min", "Radar X, 60 obr./min" });
		D.Add("map.phys.st.x_rot30", new string[5] { "РЛС X, 30 об/мин", "X-band radar, 30 rpm", "X РЛС, 30 әйл./мин", "X-Band-Radar, 30 U/min", "Radar X, 30 obr./min" });
		D.Add("map.phys.modelHint", new string[5] { "Модель физического режима готовится на стенде: единицы, оси и кнопка «В карту».", "Physical-mode models are prepared on the bench: units, axes and the \"To map\" button.", "Физик режим модельләре стендта әзерләнә: берәмлекләр, күчәрләр һәм «Картага» төймәсе.", "Modelle des physikalischen Modus werden am Prüfstand vorbereitet: Einheiten, Achsen und die Taste „Zur Karte“.", "Modele trybu fizycznego przygotowuje się na stanowisku: jednostki, osie i przycisk „Na mapę”." });
		D.Add("map.phys.flown", new string[5] { "чёрная линия — облёт по физике (виражи с креном)", "black line — the physical flight (banked turns)", "кара сызык — физика буенча очыш (крен белән борылышлар)", "schwarze Linie — physikalischer Flug (Kurven mit Querlage)", "czarna linia — lot fizyczny (zakręty z przechyleniem)" });
		D.Add("map.phys.time", new string[5] { "Время", "Time", "Вакыт", "Zeit", "Czas" });
		D.Add("map.phys.pending", new string[5] { "Экран оператора физической модели появится в фазе M2. Обнаружения ещё нет: сейчас по физике летят только самолёты, их видно лишь «Наблюдателю».", "The physical operator scope arrives in phase M2. There is no detection yet: only the aircraft fly by physics, visible to the Observer alone.", "Физик модельнең оператор экраны M2 фазасында барлыкка киләчәк. Табу әлегә юк: хәзер физика буенча очкычлар гына оча, аларны «Күзәтүче» генә күрә.", "Der physikalische Bedienerschirm kommt in Phase M2. Noch keine Erfassung: Nur die Flugzeuge fliegen physikalisch, sichtbar allein für den Beobachter.", "Ekran operatora modelu fizycznego pojawi się w fazie M2. Wykrywania jeszcze nie ma: fizycznie latają tylko samoloty, widoczne jedynie dla Obserwatora." });
		D.Add("map.phys.refuse.title", new string[5] { "Физический сценарий не запущен:", "Physical scenario not started:", "Физик сценарий башланмады:", "Physikalisches Szenario nicht gestartet:", "Scenariusz fizyczny nie wystartował:" });
		D.Add("map.phys.refuse.atlas", new string[5] { "Физический режим не запущен: ЭПР этой модели на частоте станции лучами не считается (L/λ = {0})", "Physical mode not started: rays cannot compute this model's RCS at the station frequency (L/λ = {0})", "Физик режим башланмады: бу модельнең станция ешлыгындагы ЭПРын нурлар белән исәпләп булмый (L/λ = {0})", "Physikalischer Modus nicht gestartet: Strahlen können die RCS dieses Modells bei der Stationsfrequenz nicht berechnen (L/λ = {0})", "Tryb fizyczny nie wystartował: promienie nie policzą RCS tego modelu na częstotliwości stacji (L/λ = {0})" });
		D.Add("map.phys.refuse.hf", new string[5] { "КВ-станция в физическом режиме не работает: ионосферное распространение не моделируется", "HF radar is not available in physical mode: ionospheric propagation is not modelled", "КД станциясе физик режимда эшләми: ионосфера аша таралу модельләштерелми", "HF-Radar ist im physikalischen Modus nicht verfügbar: ionosphärische Ausbreitung wird nicht modelliert", "Radar KF nie działa w trybie fizycznym: propagacja jonosferyczna nie jest modelowana" });
		D.Add("map.phys.refuse.model", new string[5] { "Модель «{0}» не подготовлена: откройте её на стенде, задайте единицы и оси, нажмите «В карту»", "Model \"{0}\" is not prepared: open it on the bench, set units and axes, press \"To map\"", "«{0}» моделе әзерләнмәгән: аны стендта ачыгыз, берәмлекләрне һәм күчәрләрне куегыз, «Картага» басыгыз", "Modell „{0}“ ist nicht vorbereitet: am Prüfstand öffnen, Einheiten und Achsen setzen, „Zur Karte“ drücken", "Model „{0}” nie jest przygotowany: otwórz go na stanowisku, ustaw jednostki i osie, naciśnij „Na mapę”" });
		D.Add("map.phys.refuse.changed", new string[5] { "Файл модели изменился после подготовки на стенде — откройте её на стенде снова ({0})", "The model file changed after bench preparation — open it on the bench again ({0})", "Модель файлы стендта әзерләнгәннән соң үзгәргән — аны стендта яңадан ачыгыз ({0})", "Die Modelldatei hat sich nach der Vorbereitung geändert — am Prüfstand erneut öffnen ({0})", "Plik modelu zmienił się po przygotowaniu — otwórz go ponownie na stanowisku ({0})" });
		D.Add("map.phys.refuse.station", new string[5] { "Станция «{0}» не входит в каталог физического режима", "Station \"{0}\" is not in the physical-mode catalogue", "«{0}» станциясе физик режим каталогына керми", "Station „{0}“ ist nicht im Katalog des physikalischen Modus", "Stacja „{0}” nie należy do katalogu trybu fizycznego" });
		D.Add("map.phys.refuse.noRadar", new string[5] { "Нет ни одной РЛС физического режима", "No physical-mode radar in the scenario", "Физик режимның бер РЛСы да юк", "Kein Radar des physikalischen Modus im Szenario", "Brak radaru trybu fizycznego w scenariuszu" });
		D.Add("map.phys.toMap", new string[5] { "В КАРТУ", "TO MAP", "КАРТАГА", "ZUR KARTE", "NA MAPĘ" });
		D.Add("map.phys.toMap.done", new string[5] { "Модель подготовлена для физической карты: {0} · {1} м", "Model prepared for the physical map: {0} · {1} m", "Модель физик карта өчен әзерләнде: {0} · {1} м", "Modell für die physikalische Karte vorbereitet: {0} · {1} m", "Model przygotowany do mapy fizycznej: {0} · {1} m" });
		D.Add("map.phys.toMap.need", new string[5] { "Для карты нужна импортированная модель с подтверждёнными единицами или длиной", "The map needs an imported model with confirmed units or length", "Карта өчен берәмлекләре яки озынлыгы расланган импортланган модель кирәк", "Die Karte braucht ein importiertes Modell mit bestätigten Einheiten oder Länge", "Mapa wymaga zaimportowanego modelu z potwierdzonymi jednostkami lub długością" });
		D.Add("map.phys.toMap.failed", new string[5] { "Модель не записана в реестр: {0}", "Model not written to the registry: {0}", "Модель реестрга язылмады: {0}", "Modell nicht in das Register geschrieben: {0}", "Model nie został zapisany w rejestrze: {0}" });
		D.Add("map.atlas.title", new string[5] { "Атлас ЭПР", "RCS atlas", "ЭПР атласы", "RCS-Atlas", "Atlas RCS" });
		D.Add("map.atlas.note", new string[5] { "среднее по ячейке {0}° · одно отражение (PO) · {1}", "{0}° cell mean · single bounce (PO) · {1}", "{0}° күзәнәк буенча уртача · бер чагылыш (PO) · {1}", "Zellmittel {0}° · Einfachreflexion (PO) · {1}", "średnia komórki {0}° · jedno odbicie (PO) · {1}" });
		D.Add("map.atlas.boundary", new string[5] { "пограничная область: модель ≈ {0} длин волн", "boundary region: model ≈ {0} wavelengths", "чик өлкә: модель ≈ {0} дулкын озынлыгы", "Grenzbereich: Modell ≈ {0} Wellenlängen", "obszar graniczny: model ≈ {0} długości fali" });
		D.Add("map.atlas.eta", new string[5] { "Оценка выпечки: {0}", "Bake estimate: {0}", "Пешерү бәяләмәсе: {0}", "Backschätzung: {0}", "Szacunek wypieku: {0}" });
		D.Add("map.atlas.metal", new string[5] { "металл", "metal", "металл", "Metall", "metal" });
		D.Add("map.atlas.bake", new string[5] { "ВЫПЕЧЬ 2°", "BAKE 2°", "ПЕШЕРЕРГӘ 2°", "BACKEN 2°", "WYPIECZ 2°" });
		D.Add("map.atlas.draft", new string[5] { "ЧЕРНОВИК 5°", "DRAFT 5°", "КАРАЛАМА 5°", "ENTWURF 5°", "SZKIC 5°" });
		D.Add("map.atlas.cancel", new string[5] { "ОТМЕНА", "CANCEL", "ТУКТАТУ", "ABBRECHEN", "ANULUJ" });
		D.Add("map.atlas.state.none", new string[5] { "Атлас для этой модели и частоты ещё не выпечен", "No atlas baked yet for this model and frequency", "Бу модель һәм ешлык өчен атлас әле пешерелмәгән", "Für dieses Modell und diese Frequenz ist noch kein Atlas gebacken", "Dla tego modelu i częstotliwości atlasu jeszcze nie wypieczono" });
		D.Add("map.atlas.state.ready", new string[5] { "Атлас: {0} · {1} ячеек · {2}", "Atlas: {0} · {1} cells · {2}", "Атлас: {0} · {1} күзәнәк · {2}", "Atlas: {0} · {1} Zellen · {2}", "Atlas: {0} · {1} komórek · {2}" });
		D.Add("map.atlas.state.baking", new string[5] { "Выпечка: ячеек {0} из {1} · потоков {2}", "Baking: cell {0} of {1} · {2} threads", "Пешерү: {1} күзәнәктән {0} · {2} агым", "Backen: Zelle {0} von {1} · {2} Threads", "Wypiek: komórka {0} z {1} · wątków {2}" });
		D.Add("map.atlas.state.failed", new string[5] { "Выпечка не удалась: {0}", "Bake failed: {0}", "Пешерү барып чыкмады: {0}", "Backen fehlgeschlagen: {0}", "Wypiek nieudany: {0}" });
		D.Add("map.atlas.state.refused", new string[5] { "Атлас не строится: модель ≈ {0} длин волн (< 3) — лучи неприменимы", "No atlas: model ≈ {0} wavelengths (< 3) — rays do not apply", "Атлас төзелми: модель ≈ {0} дулкын озынлыгы (< 3) — нурлар яраксыз", "Kein Atlas: Modell ≈ {0} Wellenlängen (< 3) — Strahlen nicht anwendbar", "Brak atlasu: model ≈ {0} długości fali (< 3) — promienie nie mają zastosowania" });
		D.Add("map.atlas.state.slow", new string[5] { "Оценка больше 30 мин ({0}). Предлагается черновик 5° или отмена.", "Estimate exceeds 30 min ({0}). Draft 5° or cancel is suggested.", "Бәяләмә 30 мин дан артык ({0}). Каралама 5° яки туктату тәкъдим ителә.", "Schätzung über 30 min ({0}). Entwurf 5° oder Abbruch empfohlen.", "Szacunek ponad 30 min ({0}). Proponowany szkic 5° lub anulowanie." });
		D.Add("map.atlas.sectors", new string[5] { "Медианы (el = 0): спереди ±30° {0} · сбоку {1} · сзади ±30° {2} дБм²", "Medians (el = 0): front ±30° {0} · side {1} · rear ±30° {2} dBsm", "Медианалар (el = 0): алдан ±30° {0} · яннан {1} · арттан ±30° {2} дБм²", "Mediane (el = 0): vorn ±30° {0} · seitlich {1} · hinten ±30° {2} dBsm", "Mediany (el = 0): przód ±30° {0} · bok {1} · tył ±30° {2} dBsm" });
		D.Add("map.atlas.cursor", new string[5] { "az {0}° · el {1}° · σ̄ {2} дБм² · k2 {3}", "az {0}° · el {1}° · σ̄ {2} dBsm · k2 {3}", "az {0}° · el {1}° · σ̄ {2} дБм² · k2 {3}", "az {0}° · el {1}° · σ̄ {2} dBsm · k2 {3}", "az {0}° · el {1}° · σ̄ {2} dBsm · k2 {3}" });
		D.Add("map.atlas.slice", new string[5] { "Срез по углу места {0}°", "Slice at elevation {0}°", "{0}° почмактагы кисем", "Schnitt bei Elevation {0}°", "Przekrój przy elewacji {0}°" });
		D.Add("map.atlas.range", new string[5] { "Диапазон {0} дБ", "Range {0} dB", "Диапазон {0} дБ", "Bereich {0} dB", "Zakres {0} dB" });
		D.Add("map.atlas.scale", new string[5] { "σ̄, дБм²", "σ̄, dBsm", "σ̄, дБм²", "σ̄, dBsm", "σ̄, dBsm" });
		D.Add("map.atlas.marker", new string[5] { "крест — текущее направление стенда", "cross — current bench direction", "хач — стендның хәзерге юнәлеше", "Kreuz — aktuelle Prüfstandsrichtung", "krzyżyk — bieżący kierunek stanowiska" });
		D.Add("map.atlas.axes", new string[5] { "азимут −180…180° по горизонтали, угол места −90…90° по вертикали", "azimuth −180…180° across, elevation −90…90° up", "азимут −180…180° горизонталь буенча, урын почмагы −90…90° вертикаль буенча", "Azimut −180…180° waagerecht, Elevation −90…90° senkrecht", "azymut −180…180° w poziomie, elewacja −90…90° w pionie" });
		D.Add("map.phys.bake.offer", new string[5] { "Нет атласа «{0}» на {1}. Выпечь? {2}", "No atlas for \"{0}\" at {1}. Bake? {2}", "«{0}» өчен {1} ешлыгында атлас юк. Пешерергәме? {2}", "Kein Atlas für „{0}“ bei {1}. Backen? {2}", "Brak atlasu „{0}” na {1}. Wypiec? {2}" });
		D.Add("map.phys.bake.run", new string[5] { "Выпечь недостающие атласы", "Bake the missing atlases", "Җитмәгән атласларны пешерергә", "Fehlende Atlanten backen", "Wypiecz brakujące atlasy" });
		D.Add("map.scope.threshold", new string[5] { "Порог (вероятность ложной тревоги)", "Threshold (false-alarm probability)", "Бусага (ялган тревога ихтималы)", "Schwelle (Falschalarmwahrscheinlichkeit)", "Próg (prawdopodobieństwo fałszywego alarmu)" });
		D.Add("map.scope.fa", new string[5] { "Ожидаемые ложные тревоги за обзор", "Expected false alarms per scan", "Бер карауга көтелгән ялган тревогалар", "Erwartete Falschalarme je Umlauf", "Oczekiwane fałszywe alarmy na obrót" });
		D.Add("map.scope.integration", new string[5] { "Накопление", "Integration", "Туплау", "Integration", "Całkowanie" });
		D.Add("map.scope.emission", new string[5] { "Излучение", "Emission", "Нурланыш", "Abstrahlung", "Emisja" });
		D.Add("map.scope.display", new string[5] { "Только изображение — не чувствительность", "Display only — not sensitivity", "Сурәт кенә — сизгерлек түгел", "Nur Darstellung — keine Empfindlichkeit", "Tylko obraz — nie czułość" });
		D.Add("map.scope.sigmaEcho", new string[5] { "ЭПР по эху (кажущаяся)", "Echo RCS (apparent)", "Яңгыраш буенча ЭПР (күренгән)", "Echo-RCS (scheinbar)", "RCS z echa (pozorna)" });
		D.Add("map.scope.estimateNote", new string[5] { "оценка по одному эху — колеблется от обзора к обзору", "single-echo estimate — fluctuates scan to scan", "бер яңгыраш буенча бәя — караудан карауга үзгәрә", "Einzelechoschätzung — schwankt von Umlauf zu Umlauf", "oszacowanie z jednego echa — zmienia się z obrotu na obrót" });
		D.Add("map.scope.station", new string[5] { "Станция на экране", "Station on screen", "Экрандагы станция", "Station auf dem Schirm", "Stacja na ekranie" });
		D.Add("map.scope.on", new string[5] { "ВКЛ", "ON", "КАБЫЗУ", "EIN", "WŁ" });
		D.Add("map.scope.off", new string[5] { "ВЫКЛ", "OFF", "СҮНДЕРҮ", "AUS", "WYŁ" });
		D.Add("map.scope.period", new string[5] { "период обзора", "scan period", "карау периоды", "Umlaufzeit", "okres obrotu" });
		D.Add("map.scope.sector", new string[5] { "Сектор ФАР: центр · ширина", "Phased-array sector: centre · width", "ФАР секторы: үзәк · киңлек", "Phased-Array-Sektor: Mitte · Breite", "Sektor anteny ścianowej: środek · szerokość" });
		D.Add("map.scope.scale", new string[5] { "Масштаб, км", "Scale, km", "Масштаб, км", "Maßstab, km", "Skala, km" });
		D.Add("map.scope.persistence", new string[5] { "Послесвечение, с", "Afterglow, s", "Балкыш дәвамы, с", "Nachleuchten, s", "Poświata, s" });
		D.Add("map.scope.brightness", new string[5] { "Яркость экрана", "Screen brightness", "Экран яктылыгы", "Bildschirmhelligkeit", "Jasność ekranu" });
		D.Add("map.scope.pick", new string[5] { "Щёлкните по отметке, чтобы открыть формуляр контакта.", "Click a plot to open the contact card.", "Контакт формулярын ачу өчен тамгага басыгыз.", "Auf eine Markierung klicken, um die Kontaktkarte zu öffnen.", "Kliknij znacznik, aby otworzyć kartę kontaktu." });
		D.Add("map.scope.contact", new string[5] { "Контакт №", "Contact #", "Контакт №", "Kontakt Nr.", "Kontakt nr" });
		D.Add("map.scope.plots", new string[5] { "отметок", "plots", "тамга", "Markierungen", "znaczników" });
		D.Add("map.scope.snr", new string[5] { "ОСШ", "SNR", "СШН", "SNR", "SNR" });
		D.Add("map.scope.since", new string[5] { "С последней отметки", "Since the last plot", "Соңгы тамгадан бирле", "Seit der letzten Markierung", "Od ostatniego znacznika" });
		D.Add("map.scope.analyzer", new string[5] { "Анализатор", "Analyzer", "Анализатор", "Analysator", "Analizator" });
		D.Add("map.scope.scan", new string[5] { "обзор", "scan", "карау", "Umlauf", "obrót" });
		D.Add("map.scope.launchers", new string[5] { "Пусковые (условный пуск по контакту)", "Launchers (conditional launch at a contact)", "Җибәрү урыннары (контакт буенча шартлы җибәрү)", "Startgeräte (bedingter Start auf einen Kontakt)", "Wyrzutnie (umowny start do kontaktu)" });
		D.Add("map.scope.launch", new string[5] { "Пуск", "Launch", "Җибәрү", "Start", "Start" });
		D.Add("map.scope.launched", new string[5] { "Пуск выполнен", "Launched", "Җибәрелде", "Gestartet", "Wystrzelono" });
		D.Add("map.scope.launchRefused", new string[5] { "Пуск невозможен: нет свежей отметки контакта, нет боезапаса или идёт перезарядка", "Launch refused: no fresh contact plot, no ammunition or reloading", "Җибәрү мөмкин түгел: контактның яңа тамгасы юк, снаряд юк яки корылу бара", "Start abgelehnt: keine frische Kontaktmarkierung, keine Munition oder Nachladen", "Start odrzucony: brak świeżego znacznika kontaktu, amunicji lub trwa przeładowanie" });
		D.Add("map.obs.snr", new string[5] { "ОСШ̄ по σ̄ · Pd при пороге", "Mean SNR from σ̄ · Pd at the threshold", "σ̄ буенча уртача СШН · бусагада Pd", "Mittleres SNR aus σ̄ · Pd an der Schwelle", "Średni SNR z σ̄ · Pd przy progu" });
		D.Add("map.obs.graphs", new string[5] { "σ̄(t) и ОСШ̄(t) за 60 с · ● обнаружено, × пропуск", "σ̄(t) and SNR̄(t), 60 s · ● detected, × missed", "60 с та σ̄(t) һәм СШН̄(t) · ● табылды, × калды", "σ̄(t) und SNR̄(t), 60 s · ● erkannt, × verpasst", "σ̄(t) i SNR̄(t), 60 s · ● wykryto, × pominięto" });
		D.Add("map.coat.title", new string[5] { "Покрытие (физ.)", "Coating (physical)", "Каплау (физ.)", "Beschichtung (physikalisch)", "Pokrycie (fizyczne)" });
		D.Add("map.coat.metal", new string[5] { "металл", "metal", "металл", "Metall", "metal" });
		D.Add("map.coat.salisbury", new string[5] { "экран Солсбери, {0} ГГц", "Salisbury screen, {0} GHz", "Солсбери экраны, {0} ГГц", "Salisbury-Schirm, {0} GHz", "ekran Salisbury'ego, {0} GHz" });
		D.Add("map.coat.jaumann", new string[5] { "слой Яуманна, {0} ГГц", "Jaumann layer, {0} GHz", "Яуман катламы, {0} ГГц", "Jaumann-Schicht, {0} GHz", "warstwa Jaumanna, {0} GHz" });
		D.Add("map.coat.custom", new string[5] { "свой слой", "custom layer", "үз катламы", "eigene Schicht", "własna warstwa" });
		D.Add("map.coat.f0", new string[5] { "Частота настройки, ГГц", "Tuning frequency, GHz", "Көйләү ешлыгы, ГГц", "Abstimmfrequenz, GHz", "Częstotliwość strojenia, GHz" });
		D.Add("map.coat.eps", new string[5] { "Диэлектрическая проницаемость ε′ / потери ε″", "Permittivity ε′ / loss ε″", "Диэлектрик үтемлелеге ε′ / югалтулар ε″", "Permittivität ε′ / Verlust ε″", "Przenikalność ε′ / straty ε″" });
		D.Add("map.coat.mu", new string[5] { "Магнитная проницаемость μ′ / потери μ″", "Permeability μ′ / loss μ″", "Магнит үтемлелеге μ′ / югалтулар μ″", "Permeabilität μ′ / Verlust μ″", "Przenikalność magnetyczna μ′ / straty μ″" });
		D.Add("map.coat.thickness", new string[5] { "Толщина, мм", "Thickness, mm", "Калынлык, мм", "Dicke, mm", "Grubość, mm" });
		D.Add("map.coat.range", new string[5] { "Вне пределов, не применено: {0}", "Out of range, not applied: {0}", "Чикләрдән тыш, кулланылмады: {0}", "Außerhalb des Bereichs, nicht übernommen: {0}", "Poza zakresem, nie zastosowano: {0}" });
		D.Add("map.coat.assumed", new string[5] { "ввод игрока (ASSUMED)", "player input (ASSUMED)", "уенчы кертүе (ASSUMED)", "Spielereingabe (ASSUMED)", "dane gracza (ASSUMED)" });
		D.Add("map.coat.note", new string[5] { "физика слоя (RADAR-2 §5.1) · кромки не считаются — ЭПР с покрытием оптимистична", "layer physics (RADAR-2 §5.1) · edges not modelled — coated RCS is optimistic", "катлам физикасы (RADAR-2 §5.1) · кырыйлар исәпләнми — каплаулы ЭПР оптимистик", "Schichtphysik (RADAR-2 §5.1) · Kanten nicht modelliert — RCS mit Beschichtung ist optimistisch", "fizyka warstwy (RADAR-2 §5.1) · krawędzie nie są liczone — RCS z pokryciem jest optymistyczna" });
		D.Add("map.coat.bake", new string[5] { "Для покрытия «{0}» на {1} ГГц нет атласа. Выпечь? Оценка: {2}", "No atlas for coating \"{0}\" at {1} GHz. Bake? Estimate: {2}", "«{0}» каплавы өчен {1} ГГц та атлас юк. Пешерергәме? Бәяләмә: {2}", "Kein Atlas für Beschichtung „{0}“ bei {1} GHz. Backen? Schätzung: {2}", "Brak atlasu dla pokrycia „{0}” na {1} GHz. Wypiec? Szacunek: {2}" });
		D.Add("map.coat.apply", new string[5] { "Применить (Enter)", "Apply (Enter)", "Кулланырга (Enter)", "Übernehmen (Enter)", "Zastosuj (Enter)" });
		D.Add("map.coat.band", new string[5] { "|Γ|² по нормали на {0}: {1} дБ", "|Γ|² at normal incidence at {0}: {1} dB", "{0} ешлыкта нормаль буенча |Γ|²: {1} дБ", "|Γ|² bei senkrechtem Einfall bei {0}: {1} dB", "|Γ|² przy padaniu normalnym na {0}: {1} dB" });
		AddPhysicalM3();
		D.Add("map.phys.bake.progress", new string[5] { "Выпечка атласов: {0} из {1} · ячеек {2} из {3}", "Baking atlases: {0} of {1} · cell {2} of {3}", "Атласлар пешерелә: {1} дән {0} · {3} күзәнәктән {2}", "Atlanten werden gebacken: {0} von {1} · Zelle {2} von {3}", "Wypiek atlasów: {0} z {1} · komórka {2} z {3}" });
	}

	private static void AddPhysicalM3()
	{
		D.Add("map.prop.lowAngle", new string[5] { "низкий угол", "low angle", "түбән почмак", "flacher Winkel", "niski kąt" });
		D.Add("map.prop.coast", new string[5] { "Побережье", "Coast", "Яр буе", "Küste", "Wybrzeże" });
		D.Add("map.prop.seaState", new string[5] { "Состояние моря (код WMO)", "Sea state (WMO code)", "Диңгез халәте (WMO коды)", "Seegang (WMO-Code)", "Stan morza (kod WMO)" });
		D.Add("map.prop.pol", new string[5] { "Поляризация", "Polarization", "Поляризация", "Polarisation", "Polaryzacja" });
		D.Add("map.prop.lowAlt", new string[5] { "Режим малых высот", "Low-altitude mode", "Түбән биеклек режимы", "Tiefflugmodus", "Tryb małych wysokości" });
		D.Add("map.prop.manualHeight", new string[5] { "Ручной ввод высоты", "Manual height entry", "Биеклекне кулдан кертү", "Manuelle Höheneingabe", "Ręczne wprowadzenie wysokości" });
		D.Add("map.prop.ghost", new string[5] { "изображение цели под поверхностью", "target image below the surface", "максатның өслек астындагы сурәте", "Zielbild unter der Oberfläche", "obraz celu pod powierzchnią" });
		D.Add("map.prop.polH", new string[5] { "Г", "H", "Г", "H", "H" });
		D.Add("map.prop.polV", new string[5] { "В", "V", "В", "V", "V" });
		D.Add("map.prop.seaNote", new string[5] { "высота волн H1/3 {0} м · только при «Побережье»", "wave height H1/3 {0} m · only with \"Coast\"", "дулкын биеклеге H1/3 {0} м · «Яр буе» булганда гына", "Wellenhöhe H1/3 {0} m · nur mit „Küste“", "wysokość fal H1/3 {0} m · tylko z „Wybrzeżem”" });
		D.Add("map.prop.site", new string[5] { "Площадка {0} м · антенна {1} м над морем", "Site {0} m · antenna {1} m above sea", "Мәйданчык {0} м · антенна диңгездән {1} м", "Standort {0} m · Antenne {1} m über Meer", "Stanowisko {0} m · antena {1} m n.p.m." });
		D.Add("map.prop.throughTerrain", new string[5] { "Участки маршрута ниже рельефа + 50 м: самолёт будет поджат вверх ({0})", "Route legs below terrain + 50 m: the aircraft will be lifted ({0})", "Маршрутның рельеф + 50 м дан түбән өлешләре: очкыч өскә күтәреләчәк ({0})", "Routenabschnitte unter Gelände + 50 m: das Flugzeug wird angehoben ({0})", "Odcinki trasy poniżej terenu + 50 m: samolot zostanie podniesiony ({0})" });
		D.Add("map.prop.heightScope", new string[5] { "Дальность — высота (оценки станции)", "Range — height (station estimates)", "Ераклык — биеклек (станция бәяләмәләре)", "Entfernung — Höhe (Schätzungen der Station)", "Odległość — wysokość (oszacowania stacji)" });
		D.Add("map.prop.set", new string[5] { "Задать", "Set", "Куярга", "Setzen", "Ustaw" });
		D.Add("map.prop.clear", new string[5] { "Сброс", "Clear", "Бетерергә", "Löschen", "Wyczyść" });
		D.Add("map.prop.manualShown", new string[5] { "H вручную {0} м", "manual H {0} m", "кулдан H {0} м", "manuelle H {0} m", "ręczna H {0} m" });
		D.Add("map.prop.on", new string[5] { "ВКЛ", "ON", "КАБЫЗЫЛГАН", "EIN", "WŁ." });
		D.Add("map.prop.off", new string[5] { "ВЫКЛ", "OFF", "СҮНДЕРЕЛГӘН", "AUS", "WYŁ." });
		D.Add("map.prop.tabSignal", new string[5] { "σ̄ и ОСШ", "σ̄ and SNR", "σ̄ һәм ОСШ", "σ̄ und SNR", "σ̄ i SNR" });
		D.Add("map.prop.tabPath", new string[5] { "Трасса", "Path", "Юл", "Ausbreitungsweg", "Tor propagacji" });
		D.Add("map.prop.state.open", new string[5] { "трасса открыта", "path open", "юл ачык", "Pfad offen", "tor otwarty" });
		D.Add("map.prop.state.horizon", new string[5] { "за радиогоризонтом: F = 0", "beyond the radio horizon: F = 0", "радиогоризонт артында: F = 0", "hinter dem Radiohorizont: F = 0", "za radiohoryzontem: F = 0" });
		D.Add("map.prop.state.terrain", new string[5] { "закрыта рельефом: F = 0", "masked by terrain: F = 0", "рельеф белән ябык: F = 0", "vom Gelände verdeckt: F = 0", "zasłonięty terenem: F = 0" });
		D.Add("map.prop.legs", new string[5] { "отражённый путь закрыт рельефом", "reflected path masked by terrain", "чагылган юл рельеф белән ябык", "reflektierter Pfad vom Gelände verdeckt", "droga odbita zasłonięta terenem" });
		D.Add("map.prop.noSpecular", new string[5] { "зеркальной точки нет", "no specular point", "көзге ноктасы юк", "kein Spiegelpunkt", "brak punktu zwierciadlanego" });
		D.Add("map.prop.surface", new string[5] { "Поверхность в точке отражения", "Surface at the specular point", "Чагылу ноктасындагы өслек", "Oberfläche am Spiegelpunkt", "Powierzchnia w punkcie odbicia" });
		D.Add("map.prop.surface.sea", new string[5] { "морская вода", "sea water", "диңгез суы", "Meerwasser", "woda morska" });
		D.Add("map.prop.surface.fresh", new string[5] { "пресная вода (река)", "fresh water (river)", "төче су (елга)", "Süßwasser (Fluss)", "woda słodka (rzeka)" });
		D.Add("map.prop.surface.ground", new string[5] { "грунт (равнина)", "ground (plain)", "грунт (тигезлек)", "Boden (Ebene)", "grunt (równina)" });
		D.Add("map.prop.surface.diffuse", new string[5] { "лес / город / горы — рассеяние, ρ = 0", "forest / city / mountains — diffuse, ρ = 0", "урман / шәһәр / таулар — чәчелү, ρ = 0", "Wald / Stadt / Berge — diffus, ρ = 0", "las / miasto / góry — rozproszenie, ρ = 0" });
		D.Add("map.prop.clamped", new string[5] { "ε грунта зажата к 1–10 ГГц", "ground ε clamped to 1–10 GHz", "грунт ε 1–10 ГГц чикләренә кысылган", "Boden-ε auf 1–10 GHz begrenzt", "ε gruntu ograniczona do 1–10 GHz" });
		D.Add("map.prop.profile", new string[5] { "Профиль: рельеф + горб Земли, прямой и отражённый лучи", "Profile: terrain + Earth bulge, direct and reflected rays", "Профиль: рельеф + Җир бөкресе, туры һәм чагылган нурлар", "Profil: Gelände + Erdwölbung, direkter und reflektierter Strahl", "Profil: teren + wybrzuszenie Ziemi, promień bezpośredni i odbity" });
		D.Add("map.prop.level", new string[5] { "Уровень поверхности", "Surface level", "Өслек дәрәҗәсе", "Oberflächenniveau", "Poziom powierzchni" });
		D.Add("map.prop.twin", new string[5] { "Высота: истинная {0} м · по моноимпульсу без шума {1} м", "Height: true {0} m · noise-free monopulse {1} m", "Биеклек: чын {0} м · шаусыз моноимпульс {1} м", "Höhe: wahr {0} m · Monopuls ohne Rauschen {1} m", "Wysokość: prawdziwa {0} m · monoimpuls bez szumu {1} m" });
	}

	private static void AddRadar()
	{
		D.Add("radar.operator", new string[5] { "ЭКРАН ОПЕРАТОРА", "OPERATOR DISPLAY", "ОПЕРАТОР ЭКРАНЫ", "BEDIENANZEIGE", "EKRAN OPERATORA" });
		D.Add("radar.station", new string[5] { "Радар", "Radar", "Радар", "Radar", "Radar" });
		D.Add("radar.post", new string[5] { "Пост ПВО", "Defence post", "Һава саклау посты", "Abwehrposten", "Stanowisko obrony" });
		D.Add("radar.gain", new string[5] { "Усиление", "Gain", "Көчәйтү", "Verstärkung", "Wzmocnienie" });
		D.Add("radar.range", new string[5] { "Масштаб", "Range", "Масштаб", "Anzeigeradius", "Zasięg ekranu" });
		D.Add("radar.distance", new string[5] { "Расстояние", "Distance", "Ераклык", "Entfernung", "Odległość" });
		D.Add("radar.clutter", new string[5] { "Эхо местности", "Terrain echoes", "Урын кайтавазы", "Geländeechos", "Echa terenu" });
		D.Add("radar.persistence", new string[5] { "Послесвечение, с", "Persistence, s", "Яктыру вакыты, с", "Nachleuchten, s", "Poświata, s" });
		D.Add("radar.rings", new string[5] { "Кольца", "Range rings", "Боҗралар", "Entfernungsringe", "Kręgi odległości" });
		D.Add("radar.on", new string[5] { "вкл.", "on", "кушылган", "an", "wł." });
		D.Add("radar.off", new string[5] { "выкл.", "off", "сүнгән", "aus", "wył." });
		D.Add("radar.clear", new string[5] { "Снять отметки", "Clear marks", "Тамгаларны бетерү", "Markierungen löschen", "Usuń oznaczenia" });
		D.Add("radar.contact", new string[5] { "Отметка", "Mark", "Тамга", "Markierung", "Oznaczenie" });
		D.Add("radar.marked", new string[5] { "ВАШИ ОТМЕТКИ", "YOUR MARKS", "СЕЗНЕҢ ТАМГАЛАР", "IHRE MARKIERUNGEN", "TWOJE OZNACZENIA" });
		D.Add("radar.bearing", new string[5] { "Направление", "Bearing", "Юнәлеш", "Richtung", "Kierunek" });
		D.Add("radar.age", new string[5] { "Возраст эха", "Echo age", "Кайтаваз яше", "Echoalter", "Wiek echa" });
		D.Add("radar.none", new string[5] { "Нажмите на видимое эхо, чтобы поставить отметку.", "Click a visible echo to mark it.", "Тамгалау өчен күренгән кайтавазга басыгыз.", "Klicken Sie auf ein sichtbares Echo, um es zu markieren.", "Kliknij widoczne echo, aby je oznaczyć." });
		D.Add("radar.screenHint", new string[5] { "Луч обновляет эхо при проходе. Между проходами оно неподвижно и гаснет. Местность тоже даёт отметки: ищите изменения от оборота к обороту.", "The sweep samples echoes as it passes. Each echo then stays still and fades. Terrain also returns echoes: look for changes between rotations.", "Нур узганда кайтаваз яңара. Аннары ул хәрәкәтсез кала һәм сүнә. Урын да кайтаваз бирә: әйләнешләр арасындагы үзгәрешләрне эзләгез.", "Der Strahl erfasst Echos beim Vorbeilaufen. Danach bleiben sie stehen und verblassen. Auch Gelände liefert Echos: Achten Sie auf Änderungen zwischen Umläufen.", "Wiązka odświeża echa podczas przejścia. Potem echo pozostaje nieruchome i zanika. Teren też daje echa: szukaj zmian między obrotami." });
		D.Add("radar.signal", new string[5] { "Развёртка", "Sweep", "Әйләнеш", "Abtastung", "Wiązka" });
		D.Add("radar.units", new string[5] { "игровые единицы", "game units", "уен берәмлекләре", "Spieleinheiten", "jednostki gry" });
	}

	private static void AddShell()
	{
		D.Add("scenario.entry", new string[5] { "Карта и игровой радар", "Map and radar game", "Карта һәм радар уены", "Karte und Radarspiel", "Mapa i gra radarowa" });
		D.Add("shell.editorTitle", new string[5] { "RADARSIM / Редактор сценария", "RADARSIM / Scenario editor", "RADARSIM / Сценарий мөхәррире", "RADARSIM / Szenario-Editor", "RADARSIM / Edytor scenariusza" });
		D.Add("shell.operatorTitle", new string[5] { "RADARSIM / Рабочее место оператора", "RADARSIM / Operator station", "RADARSIM / Оператор урыны", "RADARSIM / Bedienplatz", "RADARSIM / Stanowisko operatora" });
		D.Add("shell.fictional", new string[5] { "ИГРОВАЯ МОДЕЛЬ: условная заметность, покрытия и помехи. Расчёт ЭПР стенда здесь не используется.", "FICTIONAL GAME: abstract visibility, coatings and clutter. The bench RCS calculation is not used here.", "УЕН МОДЕЛЕ: шартлы күренүчәнлек, капламалар һәм комачаулар. Стендның ЭПР исәбе монда кулланылмый.", "SPIELMODELL: fiktive Sichtbarkeit, Beschichtungen und Störechos. Keine Kopplung an die RCS-Berechnung.", "MODEL GRY: umowna widoczność, powłoki i zakłócenia. Obliczenia RCS stanowiska nie są tu używane." });
		D.Add("shell.backBench", new string[5] { "Вернуться к стенду", "Back to bench", "Стендка кайту", "Zurück zum Prüfstand", "Powrót do stanowiska" });
		D.Add("shell.ready", new string[5] { "Готово: за радар", "Ready: operate radar", "Әзер: радарга", "Bereit: Radar bedienen", "Gotowe: obsługuj radar" });
		D.Add("shell.save", new string[5] { "Сохранить сцену", "Save scenario", "Сценарийны саклау", "Szenario speichern", "Zapisz scenariusz" });
		D.Add("shell.load", new string[5] { "Загрузить сцену", "Load scenario", "Сценарийны йөкләү", "Szenario laden", "Wczytaj scenariusz" });
		D.Add("shell.workflow", new string[5] { "1. Расставьте объекты   2. Нарисуйте маршруты   3. Задайте высоты и покрытия", "1. Place objects   2. Draw routes   3. Set altitudes and coatings", "1. Объектларны урнаштырыгыз   2. Маршрутлар ясагыз   3. Биеклек һәм капламаларны сайлагыз", "1. Objekte platzieren   2. Routen zeichnen   3. Höhen und Beschichtungen wählen", "1. Rozmieść obiekty   2. Narysuj trasy   3. Ustaw wysokości i powłoki" });
		D.Add("shell.editorHint", new string[5] { "Начальная сцена готова к запуску. Для собственного маршрута выберите самолёт и очистите маршрут.", "The initial scenario is ready. Select an aircraft and clear its route to draw your own.", "Башлангыч сценарий әзер. Үз маршрутыгызны ясау өчен очкычны сайлап, маршрутны чистартыгыз.", "Das Startszenario ist bereit. Für eine eigene Route ein Flugzeug wählen und die Route löschen.", "Scenariusz początkowy jest gotowy. Wybierz samolot i wyczyść trasę, aby narysować własną." });
		D.Add("shell.operatorHint", new string[5] { "Наблюдайте развёртку. Щёлкните по отметке, чтобы пометить контакт. Пауза останавливает полёт и развёртку.", "Watch the sweep. Click an echo to mark a contact. Pause stops both flight and sweep.", "Радарны күзәтегез. Контактны билгеләү өчен тамгага басыгыз. Пауза очышны да, радарны да туктата.", "Den Umlauf beobachten. Ein Echo anklicken, um einen Kontakt zu markieren. Pause stoppt Flug und Umlauf.", "Obserwuj obrót wiązki. Kliknij echo, aby oznaczyć kontakt. Pauza zatrzymuje lot i obrót." });
		D.Add("shell.pause", new string[5] { "Пауза", "Pause", "Пауза", "Pause", "Pauza" });
		D.Add("shell.resume", new string[5] { "Продолжить", "Resume", "Дәвам итү", "Fortsetzen", "Wznów" });
		D.Add("shell.restart", new string[5] { "Сначала", "Restart", "Яңадан", "Neustart", "Od początku" });
		D.Add("shell.edit", new string[5] { "К карте", "Edit map", "Картага", "Karte bearbeiten", "Edytuj mapę" });
		D.Add("shell.time", new string[5] { "Время", "Time", "Вакыт", "Zeit", "Czas" });
		D.Add("shell.completed", new string[5] { "Маршруты завершены:", "Routes completed:", "Тәмамланган маршрутлар:", "Routen beendet:", "Ukończone trasy:" });
		D.Add("shell.finished", new string[5] { "Полёты завершены. Можно начать заново или вернуться к карте.", "Flights completed. Restart or return to the map.", "Очышлар тәмамланды. Яңадан башлагыз яки картага кайтыгыз.", "Flüge beendet. Neu starten oder zur Karte zurückkehren.", "Loty zakończone. Uruchom ponownie lub wróć do mapy." });
		D.Add("shell.loading", new string[5] { "Загрузка геометрии модели...", "Loading model geometry...", "Модель геометриясе йөкләнә...", "Modellgeometrie wird geladen...", "Wczytywanie geometrii modelu..." });
		D.Add("shell.importTitle", new string[5] { "Своя модель STL / OBJ", "Custom STL / OBJ model", "STL / OBJ моделе", "Eigenes STL-/OBJ-Modell", "Własny model STL / OBJ" });
		D.Add("shell.importHint", new string[5] { "Укажите полный путь или выберите файл ниже. Силуэт строится по двум большим осям модели. Геометрия задаёт внешний вид, а не заметность.", "Enter a full path or choose a file below. The silhouette uses the two largest model axes. Geometry affects appearance, not visibility.", "Тулы юлны языгыз яки файл сайлагыз. Силуэт модельнең ике зур күчәре буенча ясала. Геометрия тышкы күренешне билгели.", "Vollen Pfad eingeben oder Datei wählen. Die Silhouette nutzt die zwei größten Modellachsen. Geometrie bestimmt nur das Aussehen.", "Wpisz pełną ścieżkę lub wybierz plik. Sylwetka używa dwóch największych osi modelu. Geometria określa tylko wygląd." });
		D.Add("shell.import", new string[5] { "Загрузить", "Import", "Йөкләү", "Importieren", "Importuj" });
		D.Add("shell.close", new string[5] { "Закрыть", "Close", "Ябу", "Schließen", "Zamknij" });
		D.Add("shell.imported", new string[5] { "Модель загружена:", "Model imported:", "Модель йөкләнде:", "Modell importiert:", "Model zaimportowany:" });
		D.Add("shell.invalid", new string[5] { "Проверьте сцену:", "Check scenario:", "Сценарийны тикшерегез:", "Szenario prüfen:", "Sprawdź scenariusz:" });
		D.Add("shell.missingModel", new string[5] { "Модель ещё не загружена или файл отсутствует:", "Model pending or file missing:", "Модель йөкләнмәгән яки файл юк:", "Modell noch nicht geladen oder Datei fehlt:", "Model nie został wczytany lub brakuje pliku:" });
		D.Add("shell.saved", new string[5] { "Сохранено:", "Saved:", "Сакланды:", "Gespeichert:", "Zapisano:" });
		D.Add("shell.loaded", new string[5] { "Сцена загружена.", "Scenario loaded.", "Сценарий йөкләнде.", "Szenario geladen.", "Scenariusz wczytany." });
		D.Add("shell.importFailed", new string[5] { "Ошибка импорта:", "Import failed:", "Импорт хатасы:", "Importfehler:", "Błąd importu:" });
		D.Add("shell.saveFailed", new string[5] { "Не удалось сохранить:", "Save failed:", "Саклау хатасы:", "Speichern fehlgeschlagen:", "Błąd zapisu:" });
		D.Add("shell.loadFailed", new string[5] { "Не удалось загрузить:", "Load failed:", "Йөкләү хатасы:", "Laden fehlgeschlagen:", "Błąd wczytywania:" });
		D.Add("shell.fileType", new string[5] { "Выберите STL или OBJ.", "Choose STL or OBJ.", "STL яки OBJ сайлагыз.", "STL oder OBJ wählen.", "Wybierz STL lub OBJ." });
		D.Add("shell.modelLimit", new string[5] { "Лимит: 16 моделей в библиотеке.", "Library limit: 16 models.", "Китапханә чиге: 16 модель.", "Bibliothek: höchstens 16 Modelle.", "Limit biblioteki: 16 modeli." });
	}

	private static void AddTerrain()
	{
		D.Add("radar.gameStrength", new string[5] { "Сила (игр.)", "Power (game)", "Көч (уен)", "Stärke (Spiel)", "Siła (gra)" });
		D.Add("terrain.heightFilter", new string[5] { "Рельеф: мин. высота, %", "Terrain: min. height, %", "Рельеф: мин. биеклек, %", "Gelände: Mindesthöhe, %", "Teren: min. wysokość, %" });
		D.Add("terrain.heightHint", new string[5] { "Высота рельефа условная: 0–100%. Фильтр фона, не наклон антенны. При 0 видны все высоты.", "Terrain height is fictional: 0–100%. Background filter, not antenna tilt. At 0 all heights are shown.", "Рельеф биеклеге шартлы: 0–100%. Бу фон фильтры, антенна авышлыгы түгел. 0 булганда барлык биеклекләр күренә.", "Geländehöhe im Spiel: 0–100%. Hintergrundfilter, keine Antennenneigung. Bei 0 sind alle Höhen sichtbar.", "Umowna wysokość terenu: 0–100%. Filtr tła, nie nachylenie anteny. Przy 0 widać wszystkie wysokości." });
	}

	private static void AddTracking()
	{
		D.Add("track.acquire", new string[5] { "Сопровождать", "Track target", "Күзәтеп бару", "Ziel verfolgen", "Śledź cel" });
		D.Add("track.release", new string[5] { "Снять", "Release", "Туктату", "Lösen", "Zwolnij" });
		D.Add("track.none", new string[5] { "Выберите отметку и включите сопровождение.", "Select a mark, then track it.", "Тамганы сайлагыз һәм күзәтеп барыгыз.", "Markierung wählen und verfolgen.", "Wybierz oznaczenie i włącz śledzenie." });
		D.Add("track.locked", new string[5] { "Сопровождение", "Tracking", "Күзәтү", "Verfolgung", "Śledzenie" });
		D.Add("track.lost", new string[5] { "Контакт потерян", "Contact lost", "Контакт югалды", "Kontakt verloren", "Kontakt utracony" });
		D.Add("track.destroyed", new string[5] { "Сбит", "Shot down", "Бәреп төшерелде", "Abgeschossen", "Zestrzelony" });
		D.Add("track.hits", new string[5] { "Сбито", "Shot down", "Бәреп төшерелде", "Abgeschossen", "Zestrzelono" });
		D.Add("track.misses", new string[5] { "Промахи", "Misses", "Тидерелмәде", "Verfehlt", "Pudła" });
		D.Add("track.miss", new string[5] { "Промах", "Miss", "Тидерелмәде", "Verfehlt", "Pudło" });
		D.Add("track.needLock", new string[5] { "Для пуска нужна цель на сопровождении.", "Track a visible target before launching.", "Җибәрү өчен максатны күзәтеп барыгыз.", "Vor dem Start ein sichtbares Ziel verfolgen.", "Przed startem śledź widoczny cel." });
		D.Add("track.gameHint", new string[5] { "Игровое попадание: удерживайте контакт до конца полёта. Потеря контакта — промах.", "Arcade hit: keep contact until the flight ends. Lost contact means a miss.", "Уенда тидерү өчен очыш ахырына кадәр контактны саклагыз. Контакт югалса, ракета тидерми.", "Spieltreffer: Kontakt bis zum Flugende halten. Kontaktverlust bedeutet Fehlschuss.", "Trafienie w grze: utrzymaj kontakt do końca lotu. Utrata kontaktu oznacza pudło." });
	}

	static RadarScenarioText()
	{
		D = new Dictionary<string, string[]>();
		AddMap();
		AddRadar();
		AddShell();
		AddLaunch();
		AddTracking();
		AddTerrain();
		AddPhysical();
	}

	public static string Get(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return string.Empty;
		}
		if (D.TryGetValue(key, out var array))
		{
			int num = (int)Localization.Current;
			if (num < 0 || num >= array.Length)
			{
				num = 1;
			}
			return array[num];
		}
		return key;
	}

	public static bool Contains(string key)
	{
		return key != null && D.ContainsKey(key);
	}
}
