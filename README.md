# FitPulse Platform — Лабораторна робота №02

Тришарова мікросервісна архітектура на .NET 10, ADO.NET, Dapper, SQL Server та .NET Aspire для домену **FitPulse** (бронювання тренувань та облік платежів).

---

## 📐 Опис домену та архітектури

Проект реалізує модуль бронювання персональних тренувань та фіксації платежів:
- **Clients** — клієнти фітнес-клубу.
- **Bookings** — бронювання сеансів тренувань (підтримує версіонування `version` та Soft Delete `is_deleted`).
- **Payment Records** — чеки про оплату (зв'язок 1:1 з бронюванням).

---

## 📊 Таблиця відповідності критеріям перевірки

| Критерій | Де шукати у проєкті |
|---|---|
| **Розділення на шари API → BLL → DAL (+ Domain)** | `Platform.sln`, `Services/FitPulse/FitPulse.Api`, `FitPulse.Bll`, `FitPulse.Dal`, `FitPulse.Domain` |
| **Інтерфейси репозиторіїв з `IGenericRepository<T>`** | `Services/FitPulse/FitPulse.Dal/Interfaces/` |
| **Репозиторій на чистому ADO.NET** | `Services/FitPulse/FitPulse.Dal/Repositories/ClientRepository.cs` |
| **Репозиторії на Dapper зі спільним `BaseDapperRepository<T>`** | `Services/FitPulse/FitPulse.Dal/Repositories/BookingRepository.cs`, `PaymentRepository.cs` |
| **Параметризовані SQL-запити та контроль з'єднань** | `Services/FitPulse/FitPulse.Dal/Repositories/` |
| **Unit of Work з одним з'єднанням і транзакцією** | `Services/FitPulse/FitPulse.Dal/UnitOfWork/UnitOfWork.cs` |
| **DTO та профілі AutoMapper** | `Services/FitPulse/FitPulse.Bll/Dtos/`, `Services/FitPulse/FitPulse.Bll/Profiles/` |
| **Транзакційна бізнес-операція через BLL** | `Services/FitPulse/FitPulse.Bll/Services/BookingService.cs` |
| **Доменні винятки (NotFound, BusinessConflict, Validation)** | `Services/FitPulse/FitPulse.Domain/Exceptions/` |
| **Дворівнева валідація (DataAnnotations + BLL)** | DTOs в `FitPulse.Bll/Dtos/` та перевірки бізнес-правил в `BookingService.cs` |
| **Асинхронні REST-контролери та Swagger** | `Services/FitPulse/FitPulse.Api/Controllers/BookingsController.cs` |
| **Централізована обробка помилок (ProblemDetails)** | `Services/FitPulse/FitPulse.Api/Program.cs` / Exception Middleware |
| **Оркестрація через .NET Aspire** | `Platform.AppHost/` (`Program.cs` / `AppHost.cs`) |

---

## 🚀 Інструкція із запуску та розгортання БД

### 1. Розгортання бази даних (T-SQL / SQL Server)
Виконайте послідовно скрипти з папки `db/p1/`:
1. `db/p1/schema.sql`
2. `db/p1/procedures.sql`
3. `db/p1/seed.sql`

### 2. Запуск через .NET Aspire
Встановіть проєкт `Platform.AppHost` (або `FitPulse.AppHost`) як стартовий та виконайте:
```bash
dotnet run --project Platform.AppHost

📮 Приклади API-запитів (Swagger)
Створення бронювання та платежу (POST /api/bookings)
JSON
POST /api/bookings
Content-Type: application/json

{
  "clientId": 1,
  "scheduleSlotId": 101,
  "trainerName": "Олександр Коваленко",
  "bookingDate": "2026-10-15T10:00:00Z",
  "amount": 350.00,
  "paymentMethod": "CARD"
}

Отримання бронювання з чеком (GET /api/bookings/1)
HTTP
GET /api/bookings/1
Accept: application/json

Скасування бронювання через Stored Procedure (POST /api/bookings/1/cancel)
HTTP
POST /api/bookings/1/cancel