﻿using PlantShop.Application.Interfaces.Services.Shop; 
using PlantShop.Application.Interfaces.Persistence; 
using PlantShop.Application.DTOs.Shop; 
using PlantShop.Domain.Entities.Shop; 

namespace PlantShop.Application.Services.Shop;

public class ArticleService : IArticleService 
{
    private readonly IUnitOfWork _unitOfWork;

    public ArticleService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ArticleDto>> GetAllArticlesAsync(CancellationToken cancellationToken = default)
    {
        var articles = await _unitOfWork.Articles.GetAllAsync(cancellationToken);
        return articles.Select(MapArticleToDto); 
    }

    
    public async Task<ArticleDto?> GetArticleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var article = await _unitOfWork.Articles.GetByIdAsync(id, cancellationToken);
        if (article == null)
        {
            return null;
        }
        return MapArticleToDto(article);
    }

    public async Task<IEnumerable<ArticleDto>> GetArticlesByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var articles = await _unitOfWork.Articles.GetArticlesByCategoryAsync(categoryId, cancellationToken);
        return articles.Select(MapArticleToDto);
    }

    public async Task<IEnumerable<ArticleDto>> GetFeaturedArticlesAsync(CancellationToken cancellationToken = default)
    {
        var articles = await _unitOfWork.Articles.GetFeaturedArticlesAsync(cancellationToken);
        return articles.Select(MapArticleToDto);
    }

    public async Task<ArticleDto> CreateArticleAsync(ArticleDto articleDto, CancellationToken cancellationToken = default)
    {
        if (articleDto.CategoryId <= 0)
        {
            throw new ArgumentException("A valid CategoryId must be provided.", nameof(articleDto.CategoryId));            
        }

        //Verificar se a Categoria existe na BD
        var categoryExists = await _unitOfWork.Categories.GetByIdAsync(articleDto.CategoryId, cancellationToken) != null;
        if (!categoryExists)
        {            
            throw new KeyNotFoundException($"Category with Id {articleDto.CategoryId} not found.");            
        }

        var articleEntity = new Article
        {
            Name = articleDto.Name,
            Description = articleDto.Description,
            Price = articleDto.Price,
            StockQuantity = articleDto.StockQuantity,
            ImageUrl = articleDto.ImageUrl, 
            IsFeatured = articleDto.IsFeatured,
            CategoryId = articleDto.CategoryId
        };
                
        await _unitOfWork.Articles.AddAsync(articleEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
                
        var savedArticle = await _unitOfWork.Articles.GetByIdAsync(articleEntity.Id, cancellationToken);
        if (savedArticle == null) throw new InvalidOperationException("Failed to retrieve article after creation.");
        return MapArticleToDto(savedArticle);
    }


    public async Task UpdateArticleAsync(ArticleDto articleDto, CancellationToken cancellationToken = default)
    {
        var articleEntity = await _unitOfWork.Articles.GetByIdAsync(articleDto.Id, cancellationToken);

        if (articleEntity == null)
        {
            throw new KeyNotFoundException($"Article with Id {articleDto.Id} not found for update.");
        }

        
        if (articleDto.CategoryId <= 0)
        {
            throw new ArgumentException("CategoryId must be provided.", nameof(articleDto.CategoryId));
        }

        if (articleEntity.CategoryId != articleDto.CategoryId) 
        {
            var categoryExists = await _unitOfWork.Categories.GetByIdAsync(articleDto.CategoryId, cancellationToken) != null;
            if (!categoryExists)
            {
                throw new KeyNotFoundException($"Category with Id {articleDto.CategoryId} not found.");
            }
        }

        articleEntity.Name = articleDto.Name;
        articleEntity.Description = articleDto.Description;
        articleEntity.Price = articleDto.Price;
        articleEntity.StockQuantity = articleDto.StockQuantity;
        articleEntity.ImageUrl = articleDto.ImageUrl;
        articleEntity.IsFeatured = articleDto.IsFeatured;
        articleEntity.CategoryId = articleDto.CategoryId;
                    

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    
    public async Task DeleteArticleAsync(int id, CancellationToken cancellationToken = default)
    {
        var articleEntity = await _unitOfWork.Articles.GetByIdAsync(id, cancellationToken);

        if (articleEntity == null)
        {
            throw new KeyNotFoundException($"Article with Id {id} not found for deletion.");
        }

        
        bool isInOrderItem = await _unitOfWork.OrderItems.ExistsWithArticleIdAsync(id, cancellationToken); 
        if (isInOrderItem)
        {
            throw new InvalidOperationException($"Cannot delete article '{articleEntity.Name}' (Id: {id}) because it exists in one or more orders.");
        }

        await _unitOfWork.Articles.DeleteAsync(articleEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }


    
    private static ArticleDto MapArticleToDto(Article article)
    {
        return new ArticleDto
        {
            Id = article.Id,
            Name = article.Name,
            Description = article.Description,
            Price = article.Price,
            StockQuantity = article.StockQuantity,
            ImageUrl = article.ImageUrl,
            IsFeatured = article.IsFeatured,
            CategoryId = article.CategoryId,
            
            CategoryName = article.Category?.Name ?? "N/A" 
        };
    }
}
