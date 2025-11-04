namespace PlantShop.Application.DTOs.Community;

public class PostCommentDto
{
    public Guid Id { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}