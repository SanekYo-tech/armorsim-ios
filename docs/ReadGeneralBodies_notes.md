# ReadGeneralBodies (0x180760C60) — ВОССТАНОВЛЕН (сессия 3)

Статус: полное тело перенесено в
`port/Assets/Scripts/Assembly-CSharp/SuperShotRunner.cs` (~6900 строк ISIL -> ~640 строк C#).
Метод ReadScene (главная перегрузка) теперь получает рабочий маршрут General.

## Инструментальная находка (ВАЖНО для всех следующих агентов)
`isil.py` НЕВЕРНО переводит `test a, b` + `jne` как `Compare a,b; JumpIfNotEqual` —
это на самом деле `if ((a & b) != 0)`. Пример: guard «non_plate» (инстр. 605-606)
выглядит в ISIL как `reactive != (kind==Era||Nera)`, а по дизассемблеру это
`test [rsp+0x3388], bl; jne` = `reactive && (kind==Era||kind==Nera)` → пропуск отказа.
ВСЕ булевы ветвления сверять с сырым дизассемблером. Готовый конвейер:

1. `python3 /tmp/opencode/rgb.dis`-генератор: capstone по [0x180760C60, 0x180769680).
2. DP-выравнивание ISIL-номеров на адреса по call-якорям (сегменты между вызовами).
3. Просмотр: `python3 /tmp/opencode/view.py N1 N2` (адрес + asm + ISIL).

Слот-карта аргументов (rsp после пролога): 0x3380=threatSpeed, 0x3388=reactive,
0x3390=quiet, 0x3398=plates, 0x33A0=labels, 0x33A8=labelsShown, 0x33B0=objOf,
0x33B8=kindOf, 0x33C0=passport, 0x33C8=shown, 0x33D0=&chain, 0x33D8=&hollowTargetSpec,
0x33E0=&hollowTargetPlate, 0x33E8=&hollowAheadCount, 0x33F0=&view, 0x33F8=projBodies,
0x3400=triggerBodyIndex, 0x3408=&reasonKey, 0x3410=&reasonArgs, 0x3418=models,
0x3420=region. (0x3360=retaddr, 0x3368=inFront home.)

## Ключевые сверки, сделанные по дизассемблеру
- Guard non_plate: `kind!=Plate && !IsRod && !(reactive&&(Era||Nera))` (как в Legacy).
- z-флип (инстр. 723-757): инвертируются ТОЛЬКО fwdB и rightB (upB НЕ трогается —
  в заметках прошлой сессии было неверно).
- statics Vector3 (класс 0x1825F8FA8): zero@0, one@0xC, up@0x18, down@0x24, left@0x30,
  right@0x3C, forward@0x48, back@0x54. NERA-оси: q*Vector3.forward и q*Vector3.right.
- NERA-кватернион: `rotation * (Flipped ? Euler(0,180,0) : identity)` (0x40490FDB = π
  через Quaternion.Internal_FromEulerRad = Euler(0,180,0), НЕ 90°!) затем
  `*= Euler(p.neraMountAngleDeg, 0, 0)` — вокруг X (не Y!).
- Sources: dz@0x50 = -z0 (сдвиг декора), pad@0x54 = SuperClip.Band*SpacingM
  (в заметках прошлой сессии dz/pad были перепутаны).
- Цикл 1 (плиты): 4 ветки kind (Plate/Rod/Nera/Era; kind 1-3 — без спецификаций),
  clip-цикл по o.specs (sp.clip=clip; ResolveSpacing(ключ "«name»"); CountPosedPlate;
  Kept==0 → счётчик; OnLine/bestZ), затем onLine/firstContactZ/firstOnLineSpec-цикл.
  clipped==Count → объект выбрасывается + tuples.Add((name,shown,bestZ)); 0<clipped<Count
  → refuse region_partial.
- Сортировка: b__71_2 = `int z=b.z.CompareTo(a.z); return z!=0?z:a.order.CompareTo(b.order);`
  (ОДИН вызов CompareTo — исходник сохранял результат в переменную).
- Новый posed: onLine[0..Count-2] + bystanders + последний onLine-объект.
- z0: onLine[0] (ближайший); mesh → MeshOnLine(vx, part, clip, spacing, r=0), иначе
  LineThroughBox(specs[firstOnLineSpec], 0, 0); фолбэк — o.firstContactZ.
- Отказы: non_plate, region_off_line (LineThroughBox=false), first_contact
  (bestShown!=null: объект ближе рамы), no_plates/region_empty (posed.Count==0),
  no_line, reactive_last (последний onLine не Plate/Rod/mesh), region_partial,
  plate_in_model (CountInMesh>0), pending, material (3 места), nera.refuse.tilt,
  кассета/пакет refuseKey.
- hollowTarget*: присваивается в путях Rod И Plate (кто первый в порядке эмиссии),
  hollowAheadCount = счётчик уже эмитированных on-line спецификаций.

## Остаточные допущения (семантически эквивалентные формы)
- `new Vector2(point.x, point.y).magnitude` (в двоичном — Vector2.magnitude от (x,y)).
- Формы MathF.Max(a,b) vs Mathf.Max — порядок операндов не влияет.
- Литеральные ключи локализаций сверены со строками ISIL построчно.
