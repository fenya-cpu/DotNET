namespace FitPulse.Domain.Entities;

public class Client
{
    public long ClientId { get; set; }
    public Guid ExternalUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

public class Booking
{
    public long BookingId { get; set; }
    public long ClientId { get; set; }
    public long ScheduleSlotId { get; set; }
    public string TrainerName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string Status { get; set; } = "PENDING";
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    public PaymentRecord? Payment { get; set; }
}

public class PaymentRecord
{
    public long BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "CARD";
    public string TransactionStatus { get; set; } = "SUCCESS";
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
}