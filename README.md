# FitPulse — Мікросервісна платформа спортивного комплексу

## 1. Опис предметної області
FitPulse — це цифрова платформа для автоматизації фітнес-клубів. Система побудована за мікросервісною архітектурою з 3 автономними контекстами та окремими базами даних **без фізичних FOREIGN KEY між ними**:

1. **Booking & Payments Context (`fitpulse_booking_db` - PostgreSQL)**: Управління клієнтами, абонементами, бронюванням та платежами.
2. **Catalog & Schedule Context (`fitpulse_catalog_db` - PostgreSQL)**: Управління тренерами, залами, типами занять та розкладом.
3. **Feedback & Activity Context (`fitpulse_reviews_db` - MongoDB)**: Збереження відгуків про тренерів та журналів активності.

---

## 2. Таблиця політики дублювання даних між базами

| Сутність / Дані | Джерело (Source DB) | Де дублюється (Target DB) | Стратегія дублювання | Модель узгодженості |
| :--- | :--- | :--- | :--- | :--- |
| `trainer_name` | `fitpulse_catalog_db.trainers` | `fitpulse_booking_db.bookings` | Знімок (Snapshot) | Історична незмінність на момент бронювання |
| `trainer_id` | `fitpulse_catalog_db.trainers` | `fitpulse_reviews_db.reviews` | Referencing (Soft Key) | М'яке посилання без FOREIGN KEY |
| `client_id` | `fitpulse_booking_db.clients` | `fitpulse_reviews_db.reviews` | Referencing (Soft Key) | М'яке посилання без FOREIGN KEY |
| `avg_rating` | `fitpulse_reviews_db.reviews` | `fitpulse_reviews_db.trainer_ratings` | Оновлення подіями (Eventual) | Агрегована статистика |

## 3. Порядок розгортання та запуску
Крок 1. Розгортання PostgreSQL (Context 1 & Context 2)

psql -U postgres -d fitpulse_booking_db -f db/p1/schema.sql
psql -U postgres -d fitpulse_booking_db -f db/p1/procedures.sql
psql -U postgres -d fitpulse_booking_db -f db/p1/seed.sql

psql -U postgres -d fitpulse_catalog_db -f db/p2/schema.sql
psql -U postgres -d fitpulse_catalog_db -f db/p2/seed.sql