CREATE OR ALTER PROCEDURE sp_cancel_booking
    @p_booking_id BIGINT,
    @p_user_name NVARCHAR(100) = 'system'
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE bookings
    SET status = 'CANCELLED',
        version = version + 1,
        updated_at = SYSDATETIMEOFFSET(),
        updated_by = @p_user_name
    WHERE booking_id = @p_booking_id AND is_deleted = 0;

    UPDATE payment_records
    SET transaction_status = 'REFUNDED'
    WHERE booking_id = @p_booking_id;
END