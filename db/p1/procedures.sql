CREATE OR REPLACE PROCEDURE sp_create_booking_with_payment(
    p_client_id BIGINT,
    p_schedule_slot_id BIGINT,
    p_trainer_name VARCHAR(100),
    p_booking_date TIMESTAMP WITH TIME ZONE,
    p_amount NUMERIC(18,2),
    p_payment_method VARCHAR(50),
    INOUT p_new_booking_id BIGINT DEFAULT NULL
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO bookings (client_id, schedule_slot_id, trainer_name, booking_date, status)
    VALUES (p_client_id, p_schedule_slot_id, p_trainer_name, p_booking_date, 'CONFIRMED')
    RETURNING booking_id INTO p_new_booking_id;

    INSERT INTO payment_records (booking_id, amount, payment_method, transaction_status)
    VALUES (p_new_booking_id, p_amount, p_payment_method, 'SUCCESS');
END;
$$;

CREATE OR REPLACE PROCEDURE sp_cancel_booking(
    p_booking_id BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE bookings 
    SET status = 'CANCELLED', version = version + 1, updated_at = CURRENT_TIMESTAMP
    WHERE booking_id = p_booking_id AND is_deleted = FALSE;

    UPDATE payment_records
    SET transaction_status = 'REFUNDED'
    WHERE booking_id = p_booking_id;
END;
$$;

CREATE OR REPLACE FUNCTION sp_get_client_bookings_paged(
    p_client_id BIGINT,
    p_page_number INT DEFAULT 1,
    p_page_size INT DEFAULT 10
)
RETURNS TABLE (
    booking_id BIGINT,
    trainer_name VARCHAR(100),
    booking_date TIMESTAMP WITH TIME ZONE,
    booking_status VARCHAR(20),
    amount NUMERIC(18,2),
    transaction_status VARCHAR(20)
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
    SELECT 
        b.booking_id,
        b.trainer_name,
        b.booking_date,
        b.status AS booking_status,
        p.amount,
        p.transaction_status
    FROM bookings b
    LEFT JOIN payment_records p ON b.booking_id = p.booking_id
    WHERE b.client_id = p_client_id AND b.is_deleted = FALSE
    ORDER BY b.booking_date DESC
    LIMIT p_page_size
    OFFSET (p_page_number - 1) * p_page_size;
END;
$$;

CREATE OR REPLACE PROCEDURE sp_soft_delete_client(
    p_client_id BIGINT
)
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE clients
    SET is_deleted = TRUE, updated_at = CURRENT_TIMESTAMP
    WHERE client_id = p_client_id;
END;
$$;