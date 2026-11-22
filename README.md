# WirePack

WirePack — настольное инженерное приложение для расчёта компактного размещения электрических проводов внутри круглой защитной трубки. Программа подбирает геометрически допустимые варианты укладки проводов и определяет минимальный найденный внутренний диаметр трубки.

Приложение помогает инженеру-конструктору оценить требуемый размер трубки и выбрать подходящий вариант компоновки проводов.

## Возможности

- Ввод проводов группами: наружный диаметр и количество (до 100 проводов).
- Автоматический поиск до пяти геометрически различных вариантов компоновки.
- Расчёт минимального внутреннего диаметра трубки по минимальной охватывающей окружности (MEC).
- Отображение коэффициента заполняемости для каждого варианта.
- Графическая визуализация трубки и проводов с сохранением пропорций и масштабированием.
- Остановка расчёта с сохранением уже найденных результатов, автоматический таймаут 60 секунд.
- Воспроизводимость результата при одинаковых исходных данных (кроме срабатывания аварийного таймаута).
- Сохранение проекта в JSON и открытие ранее сохранённого проекта с проверкой версии и корректности данных.

## Технологии

- C#
- .NET 10
- Avalonia UI 12
- MVVM (CommunityToolkit.Mvvm)
- xUnit для автоматических тестов

## Структура решения

| Проект | Назначение |
|--------|------------|
| `src/WirePack.Core` | Геометрическое ядро, модели предметной области, MEC, валидация компоновки, DTO проекта |
| `src/WirePack.Optimization` | Алгоритмы поиска и сжатия компоновки проводов |
| `src/WirePack.App` | Пользовательский интерфейс Avalonia UI, MVVM, JSON-сериализация проекта |
| `src/WirePack.Export` | Зарезервирован для будущего экспорта (в первой версии не используется) |
| `tests/WirePack.Core.Tests` | Тесты геометрического ядра |
| `tests/WirePack.Optimization.Tests` | Тесты оптимизатора |
| `tests/WirePack.App.Tests` | Тесты сериализации проекта |

Документация проекта находится в каталоге `docs/`.

## Требования

- .NET SDK 10.0 или новее.
- Ubuntu или Windows для запуска приложения.
- Для сборки под конкретную платформу используется соответствующий Runtime Identifier (`win-x64`, `linux-x64`).

Проверить установленную версию SDK:

```bash
dotnet --version
```

## Сборка и запуск

Восстановление зависимостей и сборка решения:

```bash
dotnet build WirePack.slnx -c Release
```

Запуск приложения из исходников:

```bash
dotnet run --project src/WirePack.App/WirePack.App.csproj
```

Запуск автоматических тестов (сборка выполняется автоматически):

```bash
dotnet test WirePack.slnx
```

Запуск тестов отдельного проекта:

```bash
dotnet test tests/WirePack.Core.Tests/WirePack.Core.Tests.csproj
```

## Публикация исполняемого файла

Команды ниже формируют самодостаточный (self-contained) исполняемый файл, для запуска которого на целевой машине не требуется установленный .NET Runtime.

### Windows (x64)

```bash
dotnet publish src/WirePack.App/WirePack.App.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/win-x64
```

Результат: `publish/win-x64/WirePack.App.exe`.

### Linux (Ubuntu, x64)

```bash
dotnet publish src/WirePack.App/WirePack.App.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/linux-x64
```

Результат: `publish/linux-x64/WirePack.App`.

Перед запуском на Linux при необходимости выдайте файлу право на исполнение:

```bash
chmod +x publish/linux-x64/WirePack.App
./publish/linux-x64/WirePack.App
```

### Публикация без встроенного рантайма

Если на целевой машине установлен .NET Runtime 10, можно собрать компактный framework-dependent вариант:

```bash
# Windows
dotnet publish src/WirePack.App/WirePack.App.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64-fd

# Linux (Ubuntu)
dotnet publish src/WirePack.App/WirePack.App.csproj -c Release -r linux-x64 --self-contained false -o publish/linux-x64-fd
```

## Документация

- `docs/requirements.md` — требования к продукту.
- `docs/architecture.md` — архитектура приложения и структура решения.
- `docs/geometry.md` — геометрическая модель и вычисления.
- `docs/optimization.md` — алгоритм оптимизации компоновки.
- `docs/data-format.md` — формат файла проекта.
- `docs/development-plan.md`, `docs/development-plan-detailed.md` — план разработки.
- `docs/development-plan-checklist.md` — чек-лист выполнения плана.
