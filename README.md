# FitPulse — SaaS-платформа для фітнес-клубів

## 1. Опис предметної області та мікросервісних контекстів

**FitPulse** — це SaaS-платформа для автоматизації роботи мережі фітнес-клубів. Для забезпечення високої автономності та масштабованості система розділена на 3 незалежні контексти (мікросервіси) з власними окремими базами даних **без фізичних `FOREIGN KEY` між ними**:

1. **Booking & Payments Context (`fitpulse_booking_db` - PostgreSQL)**  
   Відповідає за облік клієнтів, придбання абонементів, бронювання занять у розкладі та обробку платежів.
2. **Catalog & Schedule Context (`fitpulse_catalog_db` - PostgreSQL)**  
   Відповідає за каталог тренерів, спортивні зали, категорії тренувань та формування загального розкладу занять.
3. **Feedback & Activity Logs Context (`fitpulse_reviews_db` - MongoDB)**  
   Відповідає за збереження відгуків про тренерів, коментарів, денормалізованих оцінок та щоденників фізичної активності клієнтів.

---

## 2. Таблиця політики дублювання даних та Eventual Consistency

Оскільки бази даних розділені, для забезпечення автономності сервісів та швидкого виконання вибірок (без прямого з'єднання через JOIN між базами) застосовується політика асинхронного дублювання даних:

| Сутність / Дані | Джерело (Source DB) | Де дублюється (Target DB) | Стратегія дублювання | Модель узгодженості |
| :--- | :--- | :--- | :--- | :--- |
| **Профіль тренера** *(FullName, Specialization)* | `fitpulse_catalog_db` (`trainers`) | `fitpulse_booking_db` (`bookings.trainer_name`), `fitpulse_reviews_db` (`reviews`) | Read-side Denormalization (дублювання імені для швидких звітів без міжсервісних HTTP-запитів) | **Eventual Consistency** (асинхронне оновлення через шину подій `TrainerProfileUpdatedEvent`) |
| **Дані клієнта** *(FullName, Email)* | `fitpulse_booking_db` (`clients`) | `fitpulse_reviews_db` (`reviews.client_name`) | Document Embedding / Local Cache | **Eventual Consistency** (подія `ClientProfileUpdatedEvent`) |
| **Слот розкладу** *(ScheduleSlotId)* | `fitpulse_catalog_db` (`schedule_slots`) | `fitpulse_booking_db` (`bookings.schedule_slot_id`) | Soft Key (збереження лише ідентифікатора slot_id) | **Strict Validation** (перевірка існування ID при створенні бронювання) |

---

## 3. Порядок розгортання та запуску баз даних

Для розгортання всіх трьох баз даних виконайте скрипти у вказаному порядку з папки `db/`:

### Крок 1. Розгортання БД Проєкту №1 (Booking & Payments)
Виконайте скрипти в СУБД PostgreSQL для бази `fitpulse_booking_db`:
```bash
# 1. Створення таблиць, ключів, обмежень та індексів
psql -U postgres -d fitpulse_booking_db -f db/p1/schema.sql

# 2. Створення збережуваних процедур та функцій
psql -U postgres -d fitpulse_booking_db -f db/p1/procedures.sql

# 3. Ідемпотентне заповнення початковими даними
psql -U postgres -d fitpulse_booking_db -f db/p1/seed.sql

Крок 2. Розгортання БД Проєкту №2 (Catalog & Schedule)
Виконайте скрипти в СУБД PostgreSQL для бази fitpulse_catalog_db:

Bash
# 1. Створення таблиць, зв'язків 1:1, 1:N, M:N з власними полями
psql -U postgres -d fitpulse_catalog_db -f db/p2/schema.sql

# 2. Ідемпотентне заповнення каталогу
psql -U postgres -d fitpulse_catalog_db -f db/p2/seed.sql

Крок 3. Розгортання БД Проєкту №3 (Feedback & Activity Logs - MongoDB)
Виконайте JS-скрипти в MongoDB Compass або через mongosh:

Bash
# 1. Створення колекцій та складеного індексу
mongosh mongodb://localhost:27017/fitpulse_reviews_db db/p3/collections.js

# 2. Заповнення документами (Embedding, Referencing)
mongosh mongodb://localhost:27017/fitpulse_reviews_db db/p3/seed.js