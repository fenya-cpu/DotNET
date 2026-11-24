namespace Platform.EventBus.Events;

public record ProductPriceChangedEvent(
    int ProductId,
    string ProductName,
    decimal OldPrice,
    decimal NewPrice) : IntegrationEvent;

public record ProductRenamedEvent(
    int ProductId,
    string OldName,
    string NewName) : IntegrationEvent;