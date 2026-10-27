namespace Reviews.Domain.Entities;

using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

public class Review : BaseEntity
{
    private readonly List<Comment> _comments = new();

    public long ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public Author Author { get; private set; } = null!;
    public int Rating { get; private set; }
    public string Text { get; private set; } = string.Empty;

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private Review() { } // Для серіалізації

    public Review(long productId, string productName, Author author, int rating, string text)
    {
        if (rating is < 1 or > 5)
            throw new DomainException("Рейтинг повинен бути від 1 до 5.");

        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Текст відгуку не може бути порожнім.");

        ProductId = productId;
        ProductName = productName;
        Author = author;
        Rating = rating;
        Text = text;
    }

    public void AddComment(Author author, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Текст коментаря обов'язковий.");

        _comments.Add(new Comment(author, text, DateTime.UtcNow));
        Touch();
    }
}