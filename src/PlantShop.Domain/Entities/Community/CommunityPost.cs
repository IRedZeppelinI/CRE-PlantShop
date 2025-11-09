namespace PlantShop.Domain.Entities.Community;

public class CommunityPost
{
    public Guid Id { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
        
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; }
        
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
}
