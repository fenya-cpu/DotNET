namespace Reviews.Domain.Entities;

using Reviews.Domain.Common;
using Reviews.Domain.ValueObjects;

public class Comment
{
    public Author Author { get; private set; }
    public string Text { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Comment(Author author, string text, DateTime createdAt)
    {
        Author = author;
        Text = text;
        CreatedAt = createdAt;
    }
}