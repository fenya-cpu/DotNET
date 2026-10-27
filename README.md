# FitPulse Platform — Microservices & Clean Architecture

Платформа для фітнес-сервісів, реалізована на базі мікросервісної архітектури .NET з використанням Clean Architecture, CQRS, MediatR та NoSQL (MongoDB).

## 🏗 Архітектурна структура (Clean Architecture)

Мікросервіс відгуків **`Reviews`** (Лабораторна робота №04) розроблено відповідно до шаблону Clean Architecture та розділено на 4 класичні шари з чітким напрямком залежностей (внутрішні шари не знають про зовнішні):

```
Services/Reviews/
├── Reviews.Domain/          # Доменні сутності (Review, Comment), Value Objects (Author), бізнес-винятки та інтерфейси репозиторіїв.
├── Reviews.Application/     # CQRS Команди та Запити, MediatR обробники, Pipeline Behaviors (Logging, Validation, Performance), валідатори.
├── Reviews.Infrastructure/  # MongoDbContext, BSON-мапінг (BsonClassMap), ReviewRepository, ReviewQueries, авто-сідінг та індекси.
└── Reviews.WebApi/          # REST API контролери, Swagger UI, глобальна обробка помилок (ProblemDetails), конфігурація DI.

```

## 🛠 Технологічний стек

* **Платформа:** .NET 8 / 9

* **База даних:** MongoDB (MongoDB.Driver, BSON serialization)

* **Паттерни:** CQRS, Mediator (MediatR), Repository, Unit of Work, Value Objects, Pipeline Behaviors

* **Валідація:** FluentValidation

* **Мапінг:** AutoMapper

* **Документація:** Swagger / OpenAPI

## 🚀 Інструкція із запуску

### 1. Передумови

Переконайтеся, що локально або у Docker запущено **MongoDB** на стандартному порту `27017`:

```
docker run -d -p 27017:27017 --name mongodb mongo:latest

```

### 2. Запуск мікросервісу Reviews

З кореня репозиторію виконайте команду:

```
dotnet run --project services/Reviews/Reviews.WebApi/Reviews.WebApi.csproj

```

Після запуску відкрийте браузер за адресою:
👉 **`https://localhost:7001/swagger`** (або відповідний порт у консолі)

## 📋 Реалізований функціонал (Reviews Service)

* **CQRS через MediatR:** Окремі `CreateReviewCommand`, `GetReviewsListQuery`, `GetReviewByIdQuery`.

* **Pipeline Behaviors:**

  * `ValidationBehavior` — наскрізна перевірка FluentValidation до виклику обробника.

  * `LoggingBehavior` — структуроване логування запитів.

  * `PerformanceBehavior` — відстеження повільних запитів (>500ms).

* **Оптимістична конкурентність:** Контроль версій документів (`Version`) для запобігання конфліктам одночасного оновлення.

* **Автоматична індексація та сідінг:** Автоматичне створення складених, текстових та унікальних індексів (`productId + author.userId`) при старті застосунку.

* **Глобальна обробка помилок:** Централізований `GlobalExceptionHandler` із поверненням фреймворку `ProblemDetails` для доменних та MongoDB-винятків.