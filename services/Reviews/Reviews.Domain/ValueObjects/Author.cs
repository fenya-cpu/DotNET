namespace Reviews.Domain.ValueObjects;

using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

public sealed class Author : ValueObject
{
    public string UserId { get; }
    public string DisplayName { get; }

    public Author(string userId, string displayName)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new DomainException("UserId не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("DisplayName не може бути порожнім.");

        UserId = userId;
        DisplayName = displayName;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return DisplayName;
    }
}