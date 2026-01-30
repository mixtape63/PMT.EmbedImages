# PMT.EmbedImages

Плагин для (Revit/Autodesk?) — внедрение картинок в DWG.
> Если это именно Navisworks или Revit — уточню и поправим README.

## Быстрый старт
- Открой `PMT.EmbedImages.slnx` в Visual Studio
- Сборка: `Release|Any CPU` (или другой профиль)
- Точка входа: `Commands.cs` (команды/регистрация), UI: `MainForm.cs`

## Структура
- `Commands.cs` — команды/интеграция с хостом
- `EmbedLogic.cs` — основная логика внедрения
- `MainForm.*` — UI (WinForms)
- `PMT.EmbedImages.csproj` — проект

## Как мы работаем с ChatGPT
Когда ставишь задачу:
1) создавай Issue с описанием "ожидал/получил", шаги воспроизведения, версии (Revit/Navisworks, .NET)
2) прикладывай лог/stack trace (если есть)
3) указывай ссылку на файл и строки (GitHub позволяет копировать permalink на строку)

## Ограничения/заметки
- Большие файлы (DWG/IFC/NWC/NWD) в репозиторий не кладём, если не нужно для теста.
- `bin/`, `obj/`, `.vs/` не коммитим.
