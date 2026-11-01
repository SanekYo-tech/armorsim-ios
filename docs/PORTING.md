# Правила восстановления C# из IL2CPP (ArmorSim)

Цель: максимально ТОЧНО восстановить оригинальную логику, без выдумывания.

## Источники истины (в порядке приоритета)
1. `reversed/cpp2il/isil_fields/IsilDump/Assembly-CSharp/<Type>.txt` — для каждого метода: x64 Disassembly + ISIL + имена `<PrivateImplementationDetails>` полей (старый дамп: `isil/`).
2. `reversed/cpp2il/diffcs/DiffableCs/Assembly-CSharp/<Type>.cs` — сигнатуры и `//Field offset: 0x..`.
3. `port/Assets/Scripts/Assembly-CSharp/<Type>.cs` — скелет, который правим (тела-заглушки).
4. Ассеты/сцены: `port/Assets`.

## Инструмент аннотации (ОБЯЗАТЕЛЬНО использовать)
```bash
cd /root/opencode-projects/ArmorSim
python3 tools_decomp/isil.py RodGeometry              # весь тип
python3 tools_decomp/isil.py RodGeometry NoseLengthM  # один метод
python3 tools_decomp/isil.py Flat2DSolver Solve --asm # + дизассемблер
```
Он подставляет: значения float/int/double по адресам `[0x...]` и имена полей по `[rcx+offset]`.
Для полей базовых/других типов смотри их diffcs вручную.

## Calling convention (Windows x64 IL2CPP)
- static: аргументы `rcx, rdx, r8, r9` (целые/указатели) и `xmm0..xmm3` (float/double), далее стек.
- instance: `rcx` = this, далее как выше. Возврат: `rax` (int/bool/ссылка) или `xmm0` (float/double).
- В ISIL `Compare a,b` + `JumpIfGreaterOrEqual` = `if (a >= b) goto`. Беззнаковые условия на float соответствуют `comiss`: учитывай NaN как в ассемблере.
- `Move rax, 0; Return rax` для bool = false; `sete/setne/...` иногда `NotImplemented` в ISIL — тогда смотри Disassembly.
- Вызовы `Call Namespace.Type.Method` — обычный вызов; `Call 0x...` без имени — сверяйся с дизассемблером/контекстом (часто Unity API или string ops).

## Требования к коду
- НЕ менять имена типов/членов, сигнатуры, модификаторы, порядок полей, константы.
- Восстанавливать вычисления, порядок проверок, clamp'ы, epsilon'ы и тексты дословно.
- Строки брать из оригинала (ISIL/дизасм/литералы скелета). Не переводить и не «улучшать».
- Нельзя додумывать физику. Если метод непонятен — оставить заглушку и записать в отчёт.
- Не трогать `.meta`, сцены, другие типы вне своего задания.
- Не запускать `dotnet build` (сборку делает координатор после слияния).

## Ограничение памяти (ВАЖНО)
На сервере всего 3.8 ГБ RAM. OOM-killer уже убивал python3-скрипты агентов, которые грузили весь `GameAssembly.dll` или весь дамп `isil_fields` целиком. Поэтому:
- НЕ пиши собственных скриптов, которые читают весь `GameAssembly.dll` (42 МБ → в Python распухает до гигабайт) или сканируют `isil_fields` по всем типам сразу.
- Работай по одному типу/методу: `python3 tools_decomp/isil.py <Type> [method]` — он лёгкий и уже всё аннотирует (константы, поля, имена вызовов).
- Если очень нужен кастомный разбор — открывай файлы порциями (по одному .txt), а не `read()` всего каталога.
- PowerShell-однострочники с `Get-Content` по 200-мегабайтным файлам — тоже под запретом, если ты не в Windows-агенте.

## Ghidra (для сложных методов)
Проект `reversed/ghidra/ArmorSimProj`, имена 9642 игровых функций импортированы.
Готовый C-псевдокод пишется в `reversed/ghidra/decompiled_all/<Type>__0xADDR.c`.
Ручной запуск по префиксу типа:
```bash
export JAVA_HOME=/root/opencode-projects/tools/jdk25 PATH=/root/opencode-projects/tools/jdk25/bin:$PATH
/root/opencode-projects/tools/ghidra/app/support/analyzeHeadless \
  /root/opencode-projects/ArmorSim/reversed/ghidra ArmorSimProj \
  -process GameAssembly.dll -noanalysis \
  -scriptPath /root/opencode-projects/ArmorSim/tools_decomp/ghidra \
  -postScript DecompileMethods.java \
  /root/opencode-projects/ArmorSim/reversed/cpp2il/method_map.csv \
  Assembly-CSharp /root/opencode-projects/ArmorSim/reversed/ghidra/decompiled RodGeometry
```

## Проверка координатором
```bash
export DOTNET_ROOT=/root/opencode-projects/tools/dotnet PATH=/root/opencode-projects/tools/dotnet:$PATH
cd /root/opencode-projects/ArmorSim/build && dotnet build -v q
```

## Отчёт по батчу
Для каждого типа: `DONE` (все методы восстановлены) / `PARTIAL` (список оставшихся методов и причина).
