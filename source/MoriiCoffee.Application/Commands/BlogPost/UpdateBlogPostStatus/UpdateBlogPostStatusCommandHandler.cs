using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.Commands.BlogPost.CreateBlogPost;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPostStatus;

/// <summary>
/// Handles status transitions for blog posts while enforcing publish-ready rules.
/// </summary>
public class UpdateBlogPostStatusCommandHandler : ICommandHandler<UpdateBlogPostStatusCommand, BlogPostDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBlogPostStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogPostDetailDto> Handle(UpdateBlogPostStatusCommand request, CancellationToken cancellationToken)
    {
        var post = await _unitOfWork.BlogPosts
            .FindByCondition(x => x.Id == request.Id, true)
            .Include(x => x.BlogPostCategories)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("BlogPost", request.Id);

        if (request.Status == EBlogPostStatus.Published)
        {
            CreateBlogPostCommandHandler.ValidatePublishReady(
                request.Status,
                post.ContentHtml,
                post.ContentJson,
                post.BlogPostCategories.Select(x => x.BlogCategoryId).ToList());
        }

        post.SetStatus(request.Status);

        await _unitOfWork.BlogPosts.Update(post);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BlogPostDetailDto>(post);
    }
}
