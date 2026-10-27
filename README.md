# 🏋️‍♂️ FitPulse Catalog API

RESTful мікросервіс управління каталогом спортивного харчування та товарів для фітнесу **FitPulse**. Проєкт реалізовано на платформі **.NET 9 / ASP.NET Core** із дотриманням принципів **Clean Architecture (N-Tier Architecture)**, **Domain-Driven Design (DDD)** та паттернів **Repository / Unit of Work**.

---

## 🛠 Стек технологій

* **Платформа:** .NET 9.0 / C# 13
* **Web API:** ASP.NET Core Controller-based API
* **ORM:** Entity Framework Core 9.0 (Code-First)
* **БД:** Microsoft SQL Server / LocalDB
* **Валідація:** FluentValidation
* **Мапінг:** AutoMapper
* **Логування:** Serilog (з підтримкою виводу SQL-запитів EF Core у консоль)
* **Специфікації:** Ardalis.Specification
* **Документація API:** Swagger / OpenAPI

---

## 🏛 Архітектура проєкту

Рішення розділено на 4 чіткі шари відповідно до принципів розділення відповідальності (Separation of Concerns):

```text
FitPulse/
├── 📁 FitPulse.Domain/      # Доменний шар (Сутності, бізнес-правила, кастомні винятки)
├── 📁 FitPulse.Dal/         # Шар доступу до даних (DbContext, Fluent API, Репозиторії, Unit of Work, Міграції)
├── 📁 FitPulse.Bll/         # Шар бізнес-логіки (DTO, Сервіси, AutoMapper, FluentValidation)
└── 📁 FitPulse.Api/         # Шар веб-інтерфейсу (REST-контролери, Middleware, Serilog, Program.cs)
```

---

## 🗄 Модель даних та зв'язки (EF Core)

Сутності домену розроблені з урахуванням інкапсуляції (приватні сетери, методи зміни стану `AddStock`, `RemoveStock`, `UpdatePrice`):

1. **`Product` ↔ `ProductDetail` (1:1)**:
   * Кожен товар має детальний опис та інформацію про виробника.
   * Конфігурація каскадного видалення.
2. **`Product` ↔ `ProductImage` (1:N)**:
   * Товар може мати декілька зображень із порядком сортування (`SortOrder`).
3. **`Product` ↔ `Category` (M:N з додатковими даними)**:
   * Зв'язок багатими-до-багатьох реалізовано через проміжну сутність `ProductCategory`, що містить додаткову властивість `AddedAt` (дата додавання товару до категорії).

---

## 🚀 Інструкція з розгортання та запуску

### Передумови
* [NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* SQL Server Express / LocalDB (або Docker-контейнер з MSSQL)
* Встановлений інструмент `dotnet-ef`:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

### 1. Клонування репозиторію
```bash
git clone https://github.com/your-username/FitPulse.git
cd FitPulse
```

### 2. Конфігурація підключення до БД
Перевірте рядок підключення у `FitPulse.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FitPulseCatalogDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

### 3. Застосування міграцій
Для створення бази даних `FitPulseCatalogDb` та створення всіх таблиць і обмежень виконайте:
```bash
dotnet ef database update --project FitPulse.Dal --startup-project FitPulse.Api
```

*(За бажанням)* Для створення SQL-скрипту створення БД:
```bash
dotnet ef migrations script --project FitPulse.Dal --startup-project FitPulse.Api -o script.sql
```

### 4. Запуск сервісу
```bash
dotnet run --project FitPulse.Api
```
Після успішного запуску перейдіть за адресою:
👉 **`https://localhost:7001/swagger`** (або відповідний порт з консолі).

---

## 📡 Основні API Endpoints

### 📦 Товари (`/api/products`)

| Метод | Шлях | Опис |
| :--- | :--- | :--- |
| **GET** | `/api/products` | Отримати список товарів з пагінацією, фільтрацією та сортуванням |
| **GET** | `/api/products/{id}` | Отримати детальний товар за ID (включно з категоріями) |
| **POST** | `/api/products` | Створити новий товар з валідацією DTO |
| **POST** | `/api/products/{id}/add-stock` | Поповнити складський залишок товару |
| **POST** | `/api/products/{id}/remove-stock` | Списати товар зі складу (з перевіркою залишку) |

---

## 📑 Приклади відповідей API

### 1. Успішна пагінація (`GET /api/products?page=1&pageSize=2`) — `200 OK`
```json
{
  "items": [
    {
      "id": 1,
      "name": "Whey Protein Isolate 1kg",
      "sku": "WHEY-ISO-1KG",
      "price": 1299.00,
      "availableStock": 50,
      "description": "Високоякісний ізолят сироваткового протеїну.",
      "categories": ["Спортивне харчування"]
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 2,
  "totalPages": 1,
  "hasNext": false,
  "hasPrevious": false
}
```

### 2. Помилка валідації (`POST /api/products` з порожнім ім'ям) — `400 Bad Request`
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "Назва товару обов'язкова."
    ]
  }
}
```

### 3. Конфлікт бізнес-правил (Дублікат SKU) — `409 Conflict`
```json
{
  "title": "Конфлікт даних",
  "status": 409,
  "detail": "Товар з SKU 'WHEY-ISO-1KG' вже існує.",
  "instance": "/api/products"
}
```

---

## 📝 Автор
Лабораторна робота №3 з дисципліни "Розробка Web-застосунків на платформі .NET".