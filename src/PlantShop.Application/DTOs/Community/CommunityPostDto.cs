namespace PlantShop.Application.DTOs.Community;

public class CommunityPostDto
{
    public Guid Id { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<PostCommentDto> Comments { get; set; } = new List<PostCommentDto>();
}