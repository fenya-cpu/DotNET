CREATE TABLE IF NOT EXISTS clients (
    client_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    external_user_id UUID NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    phone VARCHAR(20) NOT NULL,
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100) NOT NULL DEFAULT 'system',
    updated_at TIMESTAMP WITH TIME ZONE NULL,
    updated_by VARCHAR(100) NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS subscriptions (
    subscription_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    title VARCHAR(100) NOT NULL,
    price NUMERIC(18,2) NOT NULL CHECK (price >= 0),
    duration_days INT NOT NULL CHECK (duration_days > 0),
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS client_subscriptions (
    client_subscription_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    client_id BIGINT NOT NULL REFERENCES clients(client_id) ON DELETE CASCADE,
    subscription_id INT NOT NULL REFERENCES subscriptions(subscription_id) ON DELETE CASCADE,
    start_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    end_date TIMESTAMP WITH TIME ZONE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE' CHECK (status IN ('ACTIVE', 'EXPIRED', 'CANCELLED')),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS bookings (
    booking_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    client_id BIGINT NOT NULL REFERENCES clients(client_id),
    schedule_slot_id BIGINT NOT NULL,
    trainer_name VARCHAR(100) NOT NULL,
    booking_date TIMESTAMP WITH TIME ZONE NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'CONFIRMED', 'CANCELLED', 'COMPLETED')),
    version INT NOT NULL DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100) NOT NULL DEFAULT 'system',
    updated_at TIMESTAMP WITH TIME ZONE NULL,
    updated_by VARCHAR(100) NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE
);


CREATE TABLE IF NOT EXISTS payment_records (
    booking_id BIGINT PRIMARY KEY REFERENCES bookings(booking_id) ON DELETE CASCADE,
    amount NUMERIC(18,2) NOT NULL CHECK (amount > 0),
    payment_method VARCHAR(50) NOT NULL,
    transaction_status VARCHAR(20) NOT NULL CHECK (transaction_status IN ('SUCCESS', 'FAILED', 'REFUNDED')),
    paid_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Індекси з поясненнями конкретних запитів:

-- Призначення: Прискорення вибірки замовлень конкретного клієнта
-- Запит: SELECT * FROM bookings WHERE client_id = $1 AND is_deleted = FALSE;
CREATE INDEX IF NOT EXISTS ix_bookings_client_id ON bookings(client_id);

-- Призначення: Пошук бронювань на конкретну дату для звітів
-- Запит: SELECT * FROM bookings WHERE booking_date BETWEEN $1 AND $2;
CREATE INDEX IF NOT EXISTS ix_bookings_booking_date ON bookings(booking_date);

-- Призначення: Пошук активних абонементів клієнта
-- Запит: SELECT * FROM client_subscriptions WHERE client_id = $1 AND status = 'ACTIVE';
CREATE INDEX IF NOT EXISTS ix_client_subscriptions_client_id ON client_subscriptions(client_id);