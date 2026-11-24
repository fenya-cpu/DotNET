# 🏋️‍♂️ FitPulse Microservice Platform (.NET Aspire, gRPC, Resilience & Observability)

**FitPulse** — це відмовостійка мікросервісна платформа для фітнесу та здоров'я, побудована на базі **.NET Aspire**, з єдиною точкою входу **API Gateway (YARP)**, міжсервісною взаємодією по **gRPC**, дворівневим кешуванням (**In-Memory L1 + Redis L2**), політиками резильєнтності (**Polly**), розподіленим трасуванням та метриками **OpenTelemetry**, структурованим логуванням **Serilog** і комплексними **Health Checks**.

---

## 🏛️ Архітектура рішення

```
[ Клієнт / Swagger ]
        │
        ▼
┌────────────────────────────────────────────────────────┐
│  Platform.ApiGateway (YARP Reverse Proxy - Port 5000)  │
│  • Генерує/передає X-Correlation-Id                     │
│  • Логує тривалість та маршрутизацію                  │
└───────┬───────────────────────────────┬────────────────┘
        │                               │
        ▼ (HTTP)                        ▼ (HTTP)
┌────────────────────────┐      ┌────────────────────────┐
│  Platform.Aggregator   │      │   Прямі маршрути до    │
│  • gRPC Clients        │      │      мікросервісів     │
│  • Polly Resilience    │      └───────────────┬────────┘
│  • Redis Cache (L2)    │                      │
│  • Task.WhenAll        │                      │
└───────┬────────────────┘                      │
        │ (gRPC)                                │
        ├───────────────────┬───────────────────┤
        ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌──────────────────┐
│  FitPulse.Api   │ │  Reviews.WebApi │ │   Інші сервіси   │
│  (SQL + L1/L2)  │ │ (Mongo + L1/L2) │ │                  │
└────┬────────┬───┘ └────┬────────┬───┘ └──────────────────┘
     │        │          │        │
     ▼        │          ▼        │
┌──────────┐  │     ┌──────────┐  │
│SQL Server│  │     │ MongoDB  │  │
└──────────┘  │     └──────────┘  │
              └──────────┬────────┘
                         ▼
                 ┌───────────────┐
                 │  Redis Cache  │
                 └───────────────┘
```

### Основні компоненти:

* **`Platform.AppHost`** — Оркестратор .NET Aspire, що керує життєвим циклом усіх мікросервісів, перевірками стану (`WithHttpHealthCheck`) та Docker-контейнерами (**SQL Server**, **MongoDB**, **Redis**).
* **`Platform.ServiceDefaults`** — Спільна бібліотека конфігурацій: Serilog, OpenTelemetry (Traces, Metrics включно з `Polly`, `FitPulse.Api`, `FitPulse.Cache`), Service Discovery, `CorrelationIdMiddleware` та розділені ендпоінти `/health/live` і `/health/ready`.
* **`Platform.ApiGateway`** — Зворотний проксі на базі YARP, що маршрутизує запити за логічними іменами сервісів та генерує наскрізний `X-Correlation-Id`.
* **`Platform.Aggregator`** — Сервіс композиції даних, який виконує паралельні **gRPC-виклики** (`Task.WhenAll`), захищений диференційованими політиками резильєнтності, кешує агреговані відповіді в Redis та обробляє часткові відмови (Partial Failures).
* **`Services/FitPulse`** — Критичний сервіс управління тренуваннями та продуктами (SQL Server + EF Core / ADO.NET, gRPC-сервер, дворівневе кешування, власні бізнес-метрики).
* **`Services/Reviews`** — Некритичний сервіс відгуків та рейтингів (MongoDB + Clean Architecture + CQRS, gRPC-сервер).

---

## 🛡️ Резильєнтність (Resilience Patterns)

Для клієнтів у `Platform.Aggregator` стандартний обробник із `ServiceDefaults` очищено (`HttpMessageHandlerBuilderActions.Clear()`) перед додаванням спеціалізованих політик, щоб уникнути нашарування стратегій. Для повторів на рівні gRPC-статусів (`Unavailable`, `ResourceExhausted`) додатково налаштовано нативний `ServiceConfig.RetryPolicy`.

### Обґрунтування параметрів політик у `Platform.Aggregator`

| Параметр | Критичний сервіс: `fitpulse-api` (Каталог/Тренування) | Некритичний сервіс: `reviews-api` (Відгуки) | Обґрунтування вибору |
| :--- | :--- | :--- | :--- |
| **MaxRetryAttempts** | `4` (Exponential + Jitter) | `1` (Constant + Jitter) | Без даних про продукт/тренування відповідь агрегатора не має сенсу, тому виконується більше повторів із експоненційною затримкою та jitter для уникнення *thundering herd*. Відгуки є другорядними — достатньо 1 швидкої спроби, після чого повертається часткова відповідь. |
| **AttemptTimeout** | `4 с` | `2 с` | Критичний запит до SQL/кешу має достатній запас часу на виконання, тоді як повільні відгуки відсікаються швидко (*fail-fast*). |
| **TotalRequestTimeout** | `20 с` | `5 с` | Враховує сумарний час усіх повторних спроб та затримок між ними для критичного сервісу й обмежує загальну затримку відповіді користувачу для некритичного. |
| **CircuitBreaker FailureRatio** | `0.3` (30%) | `0.7` (70%) | Чутливіший запобіжник для критичного сервісу швидше захищає його від каскадного перевантаження при деградації. Для некритичного сервісу поріг вищий, щоб поодинокі помилки не блокували роботу. |
| **SamplingDuration / BreakDuration** | `15 с` / `15 с` | `10 с` / `10 с` | Вікно вибірки (`SamplingDuration`) перевищує подвійний `AttemptTimeout` (вимога валідації Polly). Зменшений `MinimumThroughput = 5` адаптує запобіжник для навчального стенду. |

### Graceful Degradation для кешу (Redis)
* У `TwoLevelCacheService` та `ProductAggregatorService` усі звернення до Redis (`GetAsync`, `SetAsync`, `RemoveAsync`) обгорнуто в обробку винятків `RedisConnectionException` та `RedisTimeoutException`.
* При недоступності контейнера Redis система логує попередження (`Warning`) та автоматично виконує запасне читання безпосередньо з бази даних без падіння запиту.

---

## 🏥 Моніторинг стану (Health Checks)

Усі сервіси публікують розділені ендпоінти стану для оркестраторів (Aspire / Kubernetes):

* **`GET /health/live`** (та `/alive`) — швидка перевірка життєздатності процесу (`< 100 мс`, тег `live`) без звернення до зовнішніх баз даних.
* **`GET /health/ready`** — комплексна перевірка готовності сервісу приймати трафік (тег `ready`) з детальною JSON-відповіддю щодо кожної залежності:
  * **`fitpulse-db` (SQL Server):** статус збою `Unhealthy` (таймаут 5 с).
  * **`reviews-db` (MongoDB):** статус збою `Unhealthy` (таймаут 5 с).
  * **`redis-cache` (Redis):** статус збою **`Degraded`** (таймаут 3 с) — оскільки кеш є необов'язковою залежністю і система працює без нього.
  * **`fitpulse-api` / `reviews-api` (gRPC Health Checking Protocol):** перевірка стану downstream gRPC-сервісів в `Aggregator` через `Grpc.Health.V1.HealthClient`.

---

## 🚀 Швидкий запуск

### Передумови:
1. Встановлено **.NET 9 SDK** (або .NET 8 SDK).
2. Встановлено та запущено **Docker Desktop** (переконайтеся, що віртуалізація WSL2/Hyper-V увімкнена).

### Кроки запуску:

1. **Перейдіть у корінь проєкту:**
   ```bash
   cd D:\.NET\DotNET
   ```

2. **Задайте пароль для контейнера SQL Server у секретах Aspire:**
   ```bash
   dotnet user-secrets set "Parameters:sql-password" "P@ssw0rd2026!" --project Platform.AppHost/Platform.AppHost.csproj
   ```

3. **Запустіть оркестратор AppHost:**
   * **Через CLI:**
     ```bash
     dotnet run --project Platform.AppHost/Platform.AppHost.csproj
     ```
   * **Через Visual Studio:** 
     Встановіть `Platform.AppHost` як **Startup Project** і натисніть `F5`.

4. **Відкрийте Aspire Dashboard:**
   Після запуску перейдіть за посиланням із консолі (наприклад, `http://localhost:18888`). У вкладці **Resources** усі сервіси та контейнери відображаються зі статусом **Healthy**.

---

## 🌐 Ендпоінти та маршрутизація

Всі клієнтські запити проходять через **API Gateway (Порт 5000)**:

| Призначення | HTTP Метод | Gateway URL | Цільовий сервіс |
| :--- | :--- | :--- | :--- |
| **Отримання товару/тренування** | `GET` | `http://localhost:5000/api/products/{id}` | `fitpulse-api` |
| **Створення бронювання** | `POST` | `http://localhost:5000/api/bookings` | `fitpulse-api` |
| **Отримання відгуків** | `GET` | `http://localhost:5000/api/reviews?productId={id}` | `reviews-api` |
| **Агрегована картка (gRPC + Cache)** | `GET` | `http://localhost:5000/api/aggregator/products/{id}/full` | `aggregator` |
| **Liveness Probe** | `GET` | `/health/live` (на порту конкретного сервісу) | Всі сервіси |
| **Readiness Probe (JSON деталі)** | `GET` | `/health/ready` (на порту конкретного сервісу) | Всі сервіси |

---

## 🔬 Приклади тестування сценаріїв

### 1. Агрегований запит (gRPC + L1/L2 Cache):
```bash
curl -i -X GET http://localhost:5000/api/aggregator/products/1/full
```
**Приклад відповіді (200 OK):**
```json
{
  "product": {
    "id": 1,
    "name": "Персональне тренування #1",
    "price": 500.00
  },
  "reviews": [
    {
      "id": "654c123...",
      "productId": 1,
      "authorName": "Олексій",
      "rating": 5,
      "text": "Чудовий тренер!"
    }
  ],
  "warnings": []
}
```

### 2. Перевірка Resilience (`OnRetry`, `OnCircuitOpened` та Partial Failure):
1. У **Aspire Dashboard** зупиніть сервіс `reviews-api` (кнопка *Stop*).
2. Виконайте 5–6 запитів поспіль до `http://localhost:5000/api/aggregator/products/1/full`.
3. Система поверне статус `200 OK` із доступними даними продукту та повідомленням у масиві `warnings`:
```json
{
  "product": {
    "id": 1,
    "name": "Персональне тренування #1",
    "price": 500.00
  },
  "reviews": [],
  "warnings": [
    "Сервіс reviews-api тимчасово недоступний або повернув помилку..."
  ]
}
```
4. У вкладці **Metrics** (Meter `Polly` $\rightarrow$ `resilience.polly.strategy.events`) зафіксуються події `OnRetry` та `OnCircuitOpened`.

### 3. Перевірка Graceful Degradation для Redis:
1. Зупиніть контейнер `redis` у **Aspire Dashboard**.
2. Запит `GET /health/ready` поверне статус **`Degraded`**, проте `GET http://localhost:5000/api/aggregator/products/1/full` продовжить успішно повертати `200 OK` з бази даних.

---

## 📊 Спостережуваність (Observability) у Aspire Dashboard

* **Metrics (OpenTelemetry Meter API):**
  * **`FitPulse.Api`:** бізнес-лічильник `fitpulse.workouts.requested` (теги `source`, `status`), гістограма затримок `fitpulse.operation.duration` (`ms`) та gauge `fitpulse.active_sessions`.
  * **`FitPulse.Cache`:** лічильники влучань/промахів кешу `cache.hits` та `cache.misses` (тег `level`: `L1` / `L2`).
  * **`Polly`:** телеметрія стратегій `resilience.polly.strategy.events`, тривалість спроб `resilience.polly.strategy.attempt.duration` та тривалість пайплайну `resilience.polly.pipeline.duration`.
* **Traces:**
  * Повний наскрізний граф запиту: `Gateway` $\rightarrow$ `Aggregator` $\rightarrow$ паралельні gRPC spans до `FitPulse.Api` та `Reviews.WebApi` $\rightarrow$ `SQL Server` / `MongoDB` / `Redis`.
* **Structured Logs (Serilog):**
  * Логи збагачені властивостями `ServiceName`, `CorrelationId`, `TraceId` та `SpanId` для миттєвого пошуку інцидентів.