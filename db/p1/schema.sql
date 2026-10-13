CREATE TABLE clients (
    client_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    external_user_id UNIQUEIDENTIFIER NOT NULL UNIQUE,
    full_name NVARCHAR(100) NOT NULL,
    email NVARCHAR(100) NOT NULL UNIQUE,
    phone NVARCHAR(20) NOT NULL,
    version INT NOT NULL DEFAULT 1,
    created_at DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    created_by NVARCHAR(100) NOT NULL DEFAULT 'system',
    updated_at DATETIMEOFFSET NULL,
    updated_by NVARCHAR(100) NULL,
    is_deleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE bookings (
    booking_id BIGINT IDENTITY(1,1) PRIMARY KEY,
    client_id BIGINT NOT NULL REFERENCES clients(client_id),
    schedule_slot_id BIGINT NOT NULL,
    trainer_name NVARCHAR(100) NOT NULL,
    booking_date DATETIMEOFFSET NOT NULL,
    status NVARCHAR(20) NOT NULL DEFAULT 'PENDING' CHECK (status IN ('PENDING', 'CONFIRMED', 'CANCELLED', 'COMPLETED')),
    version INT NOT NULL DEFAULT 1,
    created_at DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    created_by NVARCHAR(100) NOT NULL DEFAULT 'system',
    updated_at DATETIMEOFFSET NULL,
    updated_by NVARCHAR(100) NULL,
    is_deleted BIT NOT NULL DEFAULT 0
);

CREATE TABLE payment_records (
    booking_id BIGINT PRIMARY KEY REFERENCES bookings(booking_id) ON DELETE CASCADE,
    amount DECIMAL(18,2) NOT NULL CHECK (amount > 0),
    payment_method NVARCHAR(50) NOT NULL CHECK (payment_method IN ('CARD', 'CASH', 'APPLE_PAY')),
    transaction_status NVARCHAR(20) NOT NULL CHECK (transaction_status IN ('SUCCESS', 'FAILED', 'REFUNDED')),
    paid_at DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);