namespace FitPulse.Domain.Exceptions;

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string name, object key)
        : base($"Сутність \"{name}\" з ключем ({key}) не знайдена.") { }
}