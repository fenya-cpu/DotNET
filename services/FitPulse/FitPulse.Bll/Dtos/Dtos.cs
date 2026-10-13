using System.ComponentModel.DataAnnotations;

namespace FitPulse.Bll.Dtos;

public record ClientDto(
    long ClientId,
    Guid ExternalUserId,
    string FullName,
    string Email,
    string Phone
);

public record CreateClientDto(
    [Required, StringLength(100, MinimumLength = 2)] string FullName,
    [Required, EmailAddress] string Email,
    [Required, Phone] string Phone
);

public record PaymentDto(
    long BookingId,
    decimal Amount,
    string PaymentMethod,
    string TransactionStatus,
    DateTime PaidAt
);

public record BookingDto(
    long BookingId,
    long ClientId,
    long ScheduleSlotId,
    string TrainerName,
    DateTime BookingDate,
    string Status,
    PaymentDto? Payment
);

public record CreateBookingWithPaymentDto(
    [Range(1, long.MaxValue)] long ClientId,
    [Range(1, long.MaxValue)] long ScheduleSlotId,
    [Required, StringLength(100)] string TrainerName,
    [Required] DateTime BookingDate,
    [Range(0.01, 100000.00)] decimal Amount,
    [Required, RegularExpression("CARD|CASH|APPLE_PAY")] string PaymentMethod
);