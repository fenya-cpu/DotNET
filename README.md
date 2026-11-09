# 🏋️‍♂️ FitPulse Microservice Platform (.NET Aspire & Observability)

**FitPulse** — це мікросервісна платформа для фітнесу та здоров'я, побудована на базі **.NET Aspire**, з єдиною точкою входу **API Gateway (YARP)**, сервісом агрегації даних **Aggregator Pattern**, розподіленим трасуванням **OpenTelemetry** та структурованим логуванням **Serilog**.

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
        ▼                               ▼
┌────────────────────────┐      ┌────────────────────────┐
│  Platform.Aggregator   │      │   Прямі маршрути до    │
│  • Task.WhenAll        │      │      мікросервісів     │
│  • Partial Failures    │      └───────────────┬────────┘
└───────┬────────────────┘                      │
        │                                       │
        └───────────────────┬───────────────────┘
                            │
         ┌──────────────────┼──────────────────┐
         ▼                  ▼                  ▼
┌─────────────────┐┌─────────────────┐┌──────────────────┐
│  FitPulse.Api   ││  Reviews.WebApi ││   Інші сервіси   │
│  (SQL Server)   ││    (MongoDB)    ││                  │
└────────┬────────┘└────────┬────────┘└──────────────────┘
         │                  │
         ▼                  ▼
┌─────────────────┐┌─────────────────┐
│ SQL Server Cont.││  MongoDB Cont.  │
└─────────────────┘└─────────────────┘
```

### Основні компоненти:

* **`Platform.AppHost`** — Оркестратор .NET Aspire, що керує життєвим циклом усіх мікросервісів та Docker-контейнерів (SQL Server, MongoDB).
* **`Platform.ServiceDefaults`** — Спільна бібліотека налаштувань (Serilog, OpenTelemetry, Health Checks, Service Discovery, CorrelationId Middleware & Handler).
* **`Platform.ApiGateway`** — Зворотний проксі на YARP, що маршрутизує запити за логічними іменами сервісів та генерує `X-Correlation-Id`.
* **`Platform.Aggregator`** — Сервіс композиції даних, який виконує паралельні запити (`Task.WhenAll`) до мікросервісів та обробляє часткові відмови (Partial Failures).
* **`Services/FitPulse`** — Сервіс управління тренуваннями та бронюваннями (SQL Server + EF Core / ADO.NET).
* **`Services/Reviews`** — Сервіс відгуків та рейтингів (MongoDB + Clean Architecture + CQRS).

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
   Після запуску в консолі з'явиться посилання на Aspire Dashboard (наприклад, `http://localhost:18888`), де ви побачите стан усіх контейнерів та сервісів (**State: Running**).

---

## 🌐 Ендпоінти та маршрутизація

Всі запити здійснюються через **API Gateway (Порт 5000)**.

| Призначення | HTTP Метод | Gateway URL | Цільовий сервіс |
| :--- | :--- | :--- | :--- |
| **Отримання товару/тренування** | `GET` | `http://localhost:5000/api/products/{id}` | `fitpulse-api` |
| **Створення бронювання** | `POST` | `http://localhost:5000/api/bookings` | `fitpulse-api` |
| **Отримання відгуків** | `GET` | `http://localhost:5000/api/reviews?productId={id}` | `reviews-api` |
| **Агрегована картка товару** | `GET` | `http://localhost:5000/api/aggregator/products/{id}/full` | `aggregator` |

---

## 🔬 Приклади запитів (cURL / HTTP)

### 1. Запит до мікросервісу через Gateway:
```bash
curl -i -X GET http://localhost:5000/api/products/1
```
*У відповіді обов'язково присутній заголовок `X-Correlation-Id`.*

### 2. Агрегований запит (Aggregator Service):
```bash
curl -i -X GET http://localhost:5000/api/aggregator/products/1/full
```
**Приклад відповіді (200 OK):**
```json
{
  "product": {
    "id": 1,
    "name": "Персональне тренування",
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

### 3. Сценарій часткової відмови (Partial Failure Test):
1. В **Aspire Dashboard** зупиніть сервіс `reviews-api` (кнопка *Stop*).
2. Виконайте той самий агрегований запит до `http://localhost:5000/api/aggregator/products/1/full`.
3. Система поверне статус `200 OK` із доступними даними про продукт та описом проблеми у масиві `warnings`:
```json
{
  "product": {
    "id": 1,
    "name": "Персональне тренування",
    "price": 500.00
  },
  "reviews": [],
  "warnings": [
    "Сервіс reviews-api тимчасово недоступний."
  ]
}
```

---

## 📊 Моніторинг та Спостережуваність (Observability)

* **Розподілене трасування (Traces):** 
  Вкладка **Traces** в Aspire Dashboard дозволяє простежити повний шлях запиту: `Gateway` $\rightarrow$ `Aggregator` $\rightarrow$ `FitPulse.Api` / `Reviews.WebApi` $\rightarrow$ `SQL Server` / `MongoDB`.
* **Наскрізний CorrelationId:**
  Кожен запит отримує унікальний `X-Correlation-Id`. Зробивши пошук за цим GUID у вкладці **Structured Logs**, можна побачити всі записи логів з усіх сервісів, які брали участь в обробці запиту.
* **Структуровані логи (Serilog):**
  Логи збагачені метаданими `ServiceName`, `Environment`, `MachineName`, `ThreadId`, `CorrelationId`, `TraceId` та `SpanId`.