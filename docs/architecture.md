# WirePack — архитектура приложения

**Версия:** 0.2  
**Статус:** Актуальное (вместе с планом разработки v0.1)

## 0. Справочник по документации

Следующая таблица сопоставляет архитектурные области с файлами детального описания:

| Архитектурная область | Документация с деталями | Кодовые файлы |
|------------------------|--------------------------|---------------|
| Геометрическая модель, MEC, проверки | `docs/geometry.md` | `src/WirePack.Core/GeometryOperations.cs`, `src/WirePack.Core/GeometryTolerance.cs`, `src/WirePack.Core/LayoutValidator.cs`, `src/WirePack.Core/MecResult.cs`, `src/WirePack.Core/ExactWeightedMecSolver.cs`, `src/WirePack.Core/NumericalWeightedMecSolver.cs` |
| Алгоритмы оптимизации, сжатие пучка | `docs/optimization.md` | `src/WirePack.Optimization/BundleCompressionOptimizer.cs`, `src/WirePack.Optimization/OptimizationRequest.cs`, `src/WirePack.Optimization/OptimizationResult.cs`, `src/WirePack.Optimization/LayoutVariant.cs`, `src/WirePack.Optimization/LayoutEqualityComparer.cs` |
| Формат проекта, JSON, DTO, сериализация | `docs/data-format.md` | `src/WirePack.App/ProjectFileSerializer.cs`, `src/WirePack.Core/Project/ProjectDocument.cs`, `src/WirePack.Core/Project/ProjectFormat.cs`, `src/WirePack.Core/Project/WireGroupDto.cs`, `src/WirePack.Core/Project/ProjectWireDto.cs`, `src/WirePack.App/Project/LayoutVariantDto.cs`, `src/WirePack.App/Project/ProjectPoint.cs` |
| Требования к продукту | `docs/requirements.md` | - |
| Правила работы agente, сборка, тесты | `AGENTS.md` | - |
| Настройка сборки | `Directory.Build.props` | - |

## 1. Общая архитектура

WirePack разрабатывается как настольное приложение на C# с использованием .NET 10 и Avalonia UI.

Архитектура должна разделять пользовательский интерфейс, геометрические вычисления и алгоритмы оптимизации.

Геометрическое ядро и алгоритмы оптимизации не должны зависеть от пользовательского интерфейса и конкретной операционной системы.

## 2. Структура решения

Решение состоит из следующих проектов,Arranged according to the dependency hierarchy:

### 2.1. WirePack.Core

Геометрическое ядро и основные предметные модели.

**Зависимости:** ни одна (самый нижний уровень).

**Ключевые файлы:**
- `src/WirePack.Core/Point.cs` — структура точки с координатами X, Y
- `src/WirePack.Core/Circle.cs` — структура окружности с центром и радиусом
- `src/WirePack.Core/Wire.cs` — структура провода с центром и диаметром
- `src/WirePack.Core/Tube.cs` — структура трубки с центром и внутренним радиусом
- `src/WirePack.Core/Layout.cs` — коллекция проводов компоновки
- `src/WirePack.Core/GeometryOperations.cs` — статический класс геометрических операций
- `src/WirePack.Core/GeometryTolerance.cs` — статический класс числового допуска `eps`
- `src/WirePack.Core/LayoutValidator.cs` — статический класс валидации компоновки
- `src/WirePack.Core/MecResult.cs` — результат вычисления MEC (центр, радиус, опоры)
- `src/WirePack.Core/ExactWeightedMecSolver.cs` — точный алгоритм MEC (для тестов)
- `src/WirePack.Core/NumericalWeightedMecSolver.cs` — числовой алгоритм MEC

**Ссылка на детали:** `docs/geometry.md` (примитивы, операции, допуск, MEC).

Проект не должен зависеть от Avalonia UI и других компонентов пользовательского интерфейса.

### 2.2. WirePack.Optimization

Поиск оптимального расположения проводов.

**Зависиencies:** WirePack.Core.

**Ключевые файлы:**
- `src/WirePack.Optimization/BundleCompressionOptimizer.cs` — основной класс оптимизации сжатия пучка
- `src/WirePack.Optimization/OptimizationRequest.cs` — запрос на запуск оптимизации (данные, таймаут, seed, цель)
- `src/WirePack.Optimization/OptimizationResult.cs` — результат оптимизации (варианты, причина остановки, время)
- `src/WirePack.Optimization/LayoutVariant.cs` — вариант компоновки (макет, диаметр MEC, трубка)
- `src/WirePack.Optimization/LayoutEqualityComparer.cs` — comparer для определения геометрической эквивалентности вариантов
- `src/WirePack.Optimization/OptimizationStopReason.cs` — перечисление причин остановки (таймаут, пользователь, достаточно)
- `src/WirePack.Optimization/InitialLayoutGenerator.cs` — генератор начальных укладок
- `src/WirePack.Optimization/RelaxationEngine.cs` — движок релаксации проводов

**Ссылка на детали:** `docs/optimization.md` (контракт, цикл сжатия, релаксация, варианты, остановка).

### 2.3. WirePack.App

Пользовательский интерфейс приложения.

**Зависиencies:** WirePack.Core, WirePack.Optimization.

**Ключевые файлы:**
- `src/WirePack.App/ProjectFileSerializer.cs` — сериализация/десериализация проекта в JSON
- `src/WirePack.App/Models/ProjectDocument.cs` — модель документа проекта
- `src/WirePack.App/Models/ProjectFormat.cs` — версия формата проекта
- `src/WirePack.App/Models/ProjectGroupDto.cs` — DTO группы проводов
- `src/WirePack.App/Models/LayoutVariantDto.cs` — DTO варианта компоновки
- `src/WirePack.App/Models/ProjectPoint.cs` — DTO точки для проекта
- `src/WirePack.App/Models/ProjectWireDto.cs` — DTO провода для проекта
- `src/WirePack.App/ViewModels/MainViewModel.cs` — модель представления main-окна
- `src/WirePack.App/Commands/CalculateCommand.cs` — команда расчёта
- `src/WirePack.App/Commands/StopCommand.cs` — команда остановки

**Ссылка на детали:** `docs/data-format.md` (формат проекта, DTO, сериализация), `docs/architecture.md:5` (маршрут работы).

### 2.4. WirePack.Export

Вспомогательные операции экспорта.

**Зависиencies:** WirePack.Core.

**Статус:** Зарезервирован для возможного расширения в будущем. В первой версии не реализуется, поскольку экспорт не входит в требования первой версии. WirePack.App не ссылается на этот проект до начала его реализации.

**Ссылка на детали:** Архитектура оговаривает резервирование проекта (см. раздел 2.4).

## 3. Взаимодействие компонентов

Общий поток работы по этапам:

### 3.1. Поток работы при запуске расчёта

1. **WirePack.App** собирает исходные данные от пользователя (группы проводов: диаметр, количество, координаты центров).
2. **WirePack.App** преобразует данные в формат проекта и передает их в **WirePack.Optimization**.
3. **WirePack.Optimization** запускает алгоритм сжатия пучка, используя функции из **WirePack.Core** для геометрических вычислений и проверок.
4. При каждом улучшении **WirePack.Optimization** передает лучшие результаты обратно в **WirePack.App**.
5. **WirePack.App** отображает результаты пользователю (визуализация компоновки, список вариантов, прогресс, таймер).
6. Пользователь может запросить остановку — **WirePack.Optimization** корректно завершает расчёт и передаёт сохранённые лучшие результаты.
7. Пользователь может сохранить проект — **WirePack.App** сериализует данные через **ProjectFileSerializer** (формат проекта,см. `docs/data-format.md`).

### 3.2. Обратная связь и проверка

- **WirePack.Optimization** всегда проверяет решения через **WirePack.Core** (LayoutValidator, ValidateAll).
- После завершения оптимизации **WirePack.App** может произвести независимую проверку результатов через геометрическое ядро.
- При открытии проекта **WirePack.App** валидирует формат и данные (проверка версии, обязательных полей, проверка на устаревшие результаты).

## 4. Геометрическое ядро

Геометрическое ядро должно быть самостоятельным и не зависеть от UI.

Основные геометрические сущности:
- Точка на плоскости.
- Окружность (центр + радиус).
- Провод с заданным наружным диаметром.
- Геометрическая компоновка проводов.

Геометрические операции (см. `docs/geometry.md`):
- Расчёт расстояния между точками.
- Проверка пересечения окружностей.
- Проверка касания окружностей (касание считается допустимым).
- Проверка нахождения окружности внутри другой окружности.
- Определение минимальной охватывающей окружности (MEC) для набора проводов.

MEC (Minimum Enclosing Circle) для набора проводов — окружность с центром `c` и минимальным радиусом `R`, для которой выполняется условие $|p_i - c| + r_i <= R$ для каждого провода. Радиус вычисляется как $R = min_c max_i (|p_i - c| + r_i)$, где $p_i$ — центр провода, $r_i$ — его радиус.

Для проводов разных радиусов используется взвешенная MEC кругов. Это не MEC центров с добавлением максимального радиуса. Центр MEC не является независимой переменной: для заданной укладки он определяется по координатам проводов. В типичном случае окружность определяется двумя или тремя опорными проводами, касающимися её границы.

Все вычисления выполняются в миллиметрах с использованием `double` и согласованного численного допуска (`docs/geometry.md:109-113`).

**Ключевые файлы:**
- `src/WirePack.Core/Point.cs` — структура точки с координатами X, Y
- `src/WirePack.Core/Circle.cs` — структура окружности с центром и радиусом
- `src/WirePack.Core/Wire.cs` — структура провода с центром и диаметром
- `src/WirePack.Core/Tube.cs` — структура трубки с центром и внутренним радиусом
- `src/WirePack.Core/Layout.cs` — коллекция проводов компоновки
- `src/WirePack.Core/GeometryOperations.cs` — статический класс геометрических операций
- `src/WirePack.Core/GeometryTolerance.cs` — статический класс числового допуска `eps`
- `src/WirePack.Core/LayoutValidator.cs` — статический класс валидации компоновки
- `src/WirePack.Core/MecResult.cs` — результат вычисления MEC (центр, радиус, опоры)
- `src/WirePack.Core/ExactWeightedMecSolver.cs` — точный алгоритм MEC (для тестов и сверки)
- `src/WirePack.Core/NumericalWeightedMecSolver.cs` — числовой алгоритм MEC (базовый solver)

## 5. Оптимизация

Алгоритм оптимизации должен быть отделён от геометрического ядра.

**Зависимости:** WirePack.Core.

**Ключевые файлы:**
- `src/WirePack.Optimization/BundleCompressionOptimizer.cs` — основной класс оптимизации сжатия пучка
- `src/WirePack.Optimization/OptimizationRequest.cs` — запрос на запуск оптимизации (данные, таймаут, seed, цель)
- `src/WirePack.Optimization/OptimizationResult.cs` — результат оптимизации (варианты, причина остановки, время)
- `src/WirePack.Optimization/LayoutVariant.cs` — вариант компоновки (макет, диаметр MEC, трубка)
- `src/WirePack.Optimization/LayoutEqualityComparer.cs` — comparer для определения геометрической эквивалентности вариантов
- `src/WirePack.Optimization/OptimizationStopReason.cs` — перечисление причин остановки (таймаут, пользователь, достаточно)
- `src/WirePack.Optimization/InitialLayoutGenerator.cs` — генератор начальных укладок
- `src/WirePack.Optimization/RelaxationEngine.cs` — движок релаксации проводов

Его задача — найти допустимое расположение всех заданных проводов с наименьшим найденным диаметром охватывающей окружности.

Основной алгоритм первой версии — **сжатие пучка с квазистатической релаксацией** (`docs/optimization.md:70-76`). Алгоритм эвристический (`docs/optimization.md:72`).

Шаги алгоритма (см. `docs/optimization.md:80-92`):
1. Построить начальную допустимую укладку.
2. Вычислить MEC: центр `c` и радиус `R` (`docs/optimization.md:81-82`).
3. Перевести укладку так, чтобы центр MEC находился в начале координат (`docs/optimization.md:82`).
4. Уменьшить целевой радиус: $R_{target} = R - delta$ (`docs/optimization.md:83`).
5. Выполнить квазистатическую релаксацию при неподвижном центре (`docs/optimization.md:84`).
6. Если укладка допустима — пересчитать MEC, перецентрировать, обновить лучший результат и продолжить сжатие (`docs/optimization.md:85-89`).
7. Если допустимую укладку получить не удалось — уменьшить шаг `delta` или выполнить перезапуск (`docs/optimization.md:90-91`).
8. Повторять до выполнения критерий остановки (`docs/optimization.md:92`).

Критерий раннего завершения (см. `docs/optimization.md:121-126`):
- Найдено целевое количество различных вариантов (до пяти);
- В течение `N` последовательных итераций лучший диаметр не улучшается больше `eps`.

Детерминированность (см. `docs/optimization.md:130-134`):
- По умолчанию используется фиксированный seed.
- Последовательность начальных укладок и перезапусков детерминирована.
- Критерий отсутствия улучшений измеряется в итерациях, а не в секундах.
- При срабатывании аварийного таймаута строгая воспроизводимость не гарантируется.

**Архитектурные свойства:**
- Алгоритм является эвристическим. Конкретные параметры и критерий раннего завершения определяются в `optimization.md` и подбираются на бенчмарках.
- Архитектура допускает замену или добавление алгоритма оптимизации без изменения геометрического ядра.
- Не считать найденное решение доказанным глобальным оптимумом без математического подтверждения.

Основные требования к алгоритму (из `docs/requirements.md:59-61,82-83,118-120`):
- Находить до пяти геометрически различных вариантов.
- Расчёт на 100 проводах не более 60 секунд.
- Ранняя остановка при достижении критерия достаточности.
- Воспроизводимость при фиксированном seed.
- Все возвращаемые решения должны проходить независимую проверку через WirePack.Core.

## 6. Модели данных и сохранение проектов

Для хранения проекта используется отдельная модель данных, не зависящая от внутренней реализации геометрического ядра и UI.

**Ключевые файлы:**
- `src/WirePack.Core/Project/ProjectDocument.cs` — модель документа проекта
- `src/WirePack.Core/Project/ProjectFormat.cs` — версия формата проекта
- `src/WirePack.Core/Project/WireGroupDto.cs` — DTO группы проводов
- `src/WirePack.Core/Project/ProjectWireDto.cs` — DTO провода для проекта
- `src/WirePack.App/Project/LayoutVariantDto.cs` — DTO варианта компоновки
- `src/WirePack.App/Project/ProjectPoint.cs` — DTO точки для проекта
- `src/WirePack.App/ProjectFileSerializer.cs` — сериализация/десериализация проекта в JSON
- `src/WirePack.App/ProjectFile.cs` — файл проекта (путь к файлу)

**Проект должен содержать:**
- Версию формата проекта.
- Исходные группы проводов.
- Найденные варианты компоновки.
- Координаты центров проводов.
- Центр и рассчитанный диаметр охватывающей окружности.
- Уникальные идентификаторы вариантов.

**Модель данных проекта размещается в WirePack.Core в отдельном пространстве имён и не использует типы геометрического ядра** (`docs/architecture.md:142`). JSON-сериализация выполняется в WirePack.App.

**Формат хранения:** JSON (`docs/data-format.md:51-68`).

Согласно `docs/data-format.md:16-20`, формат проекта включает:
- Версию формата.
- Источники групп проводов (диаметр, количество).
- Варианты расчёта (ID, диаметр трубки, центр трубки, провода).

При открытии проверяются обязательные поля, диаметры, количество, координаты, размеры и соответствие результатов исходному набору (`docs/data-format.md:59-66`). Сохранённые центр и диаметр пересчитываются по координатам и проверяется их соответствие (`docs/data-format.md:47`).

## 7. Визуализация

Визуализация выполняется средствами Avalonia UI.

Отображение компоновки должно:
- Показывать границу защитной трубки.
- Показывать каждый провод в виде окружности.
- Сохранять пропорции геометрических объектов.
- Обеспечивать масштабирование изображения для удобного просмотра.

**Важное правило:** Геометрические координаты не должны зависеть от масштаба отображения. Масштаб отображения влияет только на видимость, а не на геометрические данные, передаваемые из WirePack.Core и WirePack.Optimization.

## 8. Тестирование

Геометрическое ядро и алгоритмы оптимизации должны иметь автоматические тесты. Для автотестов используется xUnit. Тестовые проекты `tests/WirePack.Core.Tests` и `tests/WirePack.Optimization.Tests` должны включаться в решение (`docs/architecture.md:158`, `WirePack.slnx:2-7`).

**Ключевые тестовые файлы:**
- `tests/WirePack.Core.Tests/PointTests.cs` — тесты точки и окружности
- `tests/WirePack.Core.Tests/ToleranceTests.cs` — тесты числового допуска
- `tests/WirePack.Core.Tests/GeometryOperationTests.cs` — тесты операций между геометрией
- `tests/WirePack.Core.Tests/LayoutValidatorTests.cs` — тесты валидации компоновки
- `tests/WirePack.Core.Tests/WeightedMecTests.cs` — тесты MEC (разные наборы проводов)
- `tests/WirePack.Optimization.Tests/BundleCompressionOptimizerTests.cs` — интеграционные тесты оптимизатора
- `tests/WirePack.Optimization.Tests/OptimizationContractTests.cs` — тесты контракта оптимизации
- `tests/WirePack.Optimization.Tests/LayoutEqualityComparerTests.cs` — тесты сравнения вариантов
- `tests/WirePack.Optimization.Tests/TestWireSets.cs` — наборы тестовых проводов
- `tests/WirePack.App.Tests/ProjectFileSerializerTests.cs` — тесты сериализации проекта

Основные направления тестирования (см. `docs/optimization.md:160-177`, `docs/geometry.md:161-194`):
- Корректность геометрических операций.
- Проверка пересечений и касаний.
- Проверка нахождения проводов внутри трубки.
- Корректность определения минимальной охватывающей окружности (MEC).
- Корректность результатов оптимизации (валидация через Core).
- Корректность сохранения и открытия проектов (`docs/data-format.md:51-68`).
- Производительность на наборах до 100 проводов (`docs/optimization.md:169-170`).
- Соблюдение 60-секундного таймаута (`docs/optimization.md:51-53`).
- Корректность раннего завершения по критерию достаточности (`docs/optimization.md:121-126`).
- Воспроизводимость результатов при фиксированном seed (`docs/optimization.md:130-134`).

**Критическое требование:** Найденные оптимизатором решения должны дополнительно проходить независимую геометрическую проверку через WirePack.Core (`docs/optimization.md:177`, `docs/geometry.md:96-105`).

## 9. Зависимости между проектами

Допустимые зависимости (см. также раздел 0 — Справочник по документации):

- WirePack.App → WirePack.Core
- WirePack.App → WirePack.Optimization
- WirePack.Optimization → WirePack.Core
- WirePack.Export → WirePack.Core

**WirePack.Core не зависит от остальных проектов.** Это фундаментальное ограничение архитектуры.

**WirePack.App не ссылается на WirePack.Export в первой версии.** Экспорт проектов зарезервирован для будущих версий.

| Направление зависимостей | Цель | Ссылка |
|--------------------------|------|--------|
| WirePack.App → WirePack.Core | Геометрические вычисления, проверка, DTO | `docs/architecture.md:38-46` |
| WirePack.App → WirePack.Optimization | Поиск компоновок, оптимизация | `docs/architecture.md:50-66` |
| WirePack.Optimization → WirePack.Core | Геометрия, MEC, проверки | `docs/architecture.md:38-46` |
| WirePack.Export → WirePack.Core | Возможный будущий экспорт | `docs/architecture.md:68-72` |

## 10. Основные архитектурные ограничения

- Геометрическое ядро не зависит от UI (фундаментальное ограничение, раздел 2.2).
- Алгоритмы оптимизации не содержат кода визуализации (только геометрия через Core).
- UI не реализует геометрические вычисления (только координаты из ввода и отображение).
- Все геометрические проверки выполняются через WirePack.Core (LayoutValidator, MEC solver).
- Модель данных проекта не размещается в WirePack.App (находится в Core, разделы 2.1, 6).
- Основной алгоритм первой версии — сжатие пучка (раздел 5); допускается его замена по результатам бенчмарков (`docs/optimization.md:126`).
- Архитектура должна допускать замену или добавление алгоритмов оптимизации без изменения геометрического ядра (разделы 2.3, 5).
- WirePack.App не ссылается на WirePack.Export в первой версии (раздел 2.4).
- Оптимизатор должен возвращать только допустимые варианты (проверка через Core).
- Финальный диаметр считаться через MEC (раздел 5, `docs/optimization.md:109`).
- MEC считаться по взвешенной формуле (раздел 4, `docs/geometry.md:76-86`).
- DTO проекта находиться в Core, JSON — в App (раздел 6, `docs/architecture.md:142`).
- Воспроизводимость подтверждена при фиксированном seed (раздел 5, `docs/optimization.md:130-134`).
- Возвращается до пяти различных вариантов (раздел 5, `docs/optimization.md:116`).
- Тесты Core проходят (раздел 8, `AGENTS.md:67-71`).
- Сборка без предупреждений, `TreatWarningsAsErrors` включён (`Directory.Build.props:7`).
