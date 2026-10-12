-- 1. Створення клієнта
CREATE OR REPLACE PROCEDURE sp_create_client(
    p_external_id UUID,
    p_full_name VARCHAR(100),
    p_email VARCHAR(100),
    p_phone VARCHAR(20),
    p_created_by VARCHAR(100) DEFAULT 'system'
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO clients (external_user_id, full_name, email, phone, created_by)
    VALUES (p_external_id, p_full_name, p_email, p_phone, p_created_by)
    ON CONFLICT (email) DO NOTHING;
END;
$$;

-- 2. Оновлення даних клієнта (Optimistic Concurrency)
CREATE OR REPLACE PROCEDURE sp_update_client(
    p_client_id BIGINT,
    p_full_name VARCHAR(100),
    p_phone VARCHAR(20),
    p_expected_version INT,
    p_updated_by VARCHAR(100) DEFAULT 'system'
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_updated_rows INT;
BEGIN
    UPDATE clients
    SET full_name = p_full_name,
        phone = p_phone,
        version = version + 1,
        updated_at = CURRENT_TIMESTAMP,
        updated_by = p_updated_by
    WHERE client_id = p_client_id AND version = p_expected_version AND is_deleted = FALSE;

    GET DIAGNOSTICS v_updated_rows = ROW_COUNT;
    IF v_updated_rows = 0 THEN
        RAISE EXCEPTION 'Помилка оптимістичного блокування: запис змінено або видалено.';
    END IF;
END;
$$;

-- 3. Транзакційне створення бронювання з оплатою
CREATE OR REPLACE PROCEDURE sp_create_booking_with_payment(
    p_client_id BIGINT,
    p_schedule_slot_id BIGINT,
    p_trainer_name VARCHAR(100),
    p_booking_date TIMESTAMP WITH TIME ZONE,
    p_amount NUMERIC(18,2),
    p_payment_method VARCHAR(50),
    p_created_by VARCHAR(100) DEFAULT 'system'
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_new_booking_id BIGINT;
BEGIN
    INSERT INTO bookings (client_id, schedule_slot_id, trainer_name, booking_date, status, created_by)
    VALUES (p_client_id, p_schedule_slot_id, p_trainer_name, p_booking_date, 'CONFIRMED', p_created_by)
    RETURNING booking_id INTO v_new_booking_id;

    INSERT INTO payment_records (booking_id, amount, payment_method, transaction_status)
    VALUES (v_new_booking_id, p_amount, p_payment_method, 'SUCCESS');
EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Помилка створення бронювання та платежу: %', SQLERRM;
END;
$$;

-- 4. Транзакційна зміна статусу з валідацією та обробкою помилок
CREATE OR REPLACE PROCEDURE sp_cancel_booking(
    p_booking_id BIGINT,
    p_user_name VARCHAR(100) DEFAULT 'system'
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_current_status VARCHAR(20);
BEGIN
    SELECT status INTO v_current_status
    FROM bookings
    WHERE booking_id = p_booking_id AND is_deleted = FALSE;

    IF v_current_status IS NULL THEN
        RAISE EXCEPTION 'Бронювання з ID % не знайдено або воно видалене.', p_booking_id;
    END IF;

    IF v_current_status = 'CANCELLED' THEN
        RAISE EXCEPTION 'Недопустимий перехід: Бронювання вже скасоване.';
    END IF;

    IF v_current_status = 'COMPLETED' THEN
        RAISE EXCEPTION 'Недопустимий перехід: Завершене тренування скасувати неможливо.';
    END IF;

    UPDATE bookings
    SET status = 'CANCELLED',
        version = version + 1,
        updated_at = CURRENT_TIMESTAMP,
        updated_by = p_user_name
    WHERE booking_id = p_booking_id;

    UPDATE payment_records
    SET transaction_status = 'REFUNDED'
    WHERE booking_id = p_booking_id;

EXCEPTION
    WHEN OTHERS THEN
        RAISE EXCEPTION 'Транзакція скасування скасована: %', SQLERRM;
END;
$$;

-- 5. М'яке видалення клієнта (Soft Delete)
CREATE OR REPLACE PROCEDURE sp_soft_delete_client(
    p_client_id BIGINT,
    p_user_name VARCHAR(100) DEFAULT 'system'
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE clients
    SET is_deleted = TRUE,
        updated_at = CURRENT_TIMESTAMP,
        updated_by = p_user_name
    WHERE client_id = p_client_id AND is_deleted = FALSE;
END;
$$;