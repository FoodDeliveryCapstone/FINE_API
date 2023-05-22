using AutoMapper;
using AutoMapper.QueryableExtensions;
using FINE.Data.Entity;
using FINE.Data.UnitOfWork;
using FINE.Service.Commons;
using FINE.Service.DTO.Request;
using FINE.Service.DTO.Request.BlogPost;
using FINE.Service.DTO.Response;
using FINE.Service.Exceptions;
using FINE.Service.Utilities;
using NetTopologySuite.Algorithm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FINE.Service.Helpers.ErrorEnum;

namespace FINE.Service.Service
{
    public interface IBlogPostService
    {
        Task<BaseResponsePagingViewModel<BlogPostResponse>> GetBlogPost(BlogPostResponse filter, PagingRequest paging);
        Task<BaseResponseViewModel<BlogPostResponse>> GetBlogPostById(int blogPostId);
        Task<BaseResponseViewModel<BlogPostResponse>> CreateBlogPost(CreateBlogPostRequest request);
        Task<BaseResponseViewModel<BlogPostResponse>> UpdateBlogPost(int blogPostId, UpdateBlogPostRequest request);
    }

    public class BlogPostService : IBlogPostService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public BlogPostService(IMapper mapper, IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<BaseResponseViewModel<BlogPostResponse>> CreateBlogPost(CreateBlogPostRequest request)
        {
            try
            {
                var blogPost = _mapper.Map<CreateBlogPostRequest, BlogPost>(request);
                blogPost.Active = true;
                blogPost.CreateAt = DateTime.Now;
                await _unitOfWork.Repository<BlogPost>().InsertAsync(blogPost);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<BlogPostResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<BlogPostResponse>(blogPost)
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public async Task<BaseResponsePagingViewModel<BlogPostResponse>> GetBlogPost(BlogPostResponse filter, PagingRequest paging)
        {
            try
            {
                var blogPost = _unitOfWork.Repository<BlogPost>().GetAll()
                                          .ProjectTo<BlogPostResponse>(_mapper.ConfigurationProvider)
                                           .DynamicFilter(filter)
                                           .DynamicSort(filter)
                                           .PagingQueryable(paging.Page, paging.PageSize, Constants.LimitPaging,
                                        Constants.DefaultPaging);
                return new BaseResponsePagingViewModel<BlogPostResponse>()
                {
                    Metadata = new PagingsMetadata()
                    {
                        Page = paging.Page,
                        Size = paging.PageSize,
                        Total = blogPost.Item1
                    },
                    Data = blogPost.Item2.ToList()
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<BlogPostResponse>> GetBlogPostById(int blogPostId)
        {
            try
            {
                var blogPost = _unitOfWork.Repository<BlogPost>().GetAll().FirstOrDefault(x => x.Id == blogPostId);
                if (blogPost == null)
                    throw new ErrorResponse(404, (int)BlogPostErrorEnums.NOT_FOUND_ID,
                                        BlogPostErrorEnums.NOT_FOUND_ID.GetDisplayName());
                return new BaseResponseViewModel<BlogPostResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<BlogPostResponse>(blogPost)
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<BaseResponseViewModel<BlogPostResponse>> UpdateBlogPost(int blogPostId, UpdateBlogPostRequest request)
        {
            try
            {
                var blogPost = _unitOfWork.Repository<BlogPost>().Find(x => x.Id == blogPostId);
                if (blogPost == null)
                    throw new ErrorResponse(404, (int)BlogPostErrorEnums.NOT_FOUND_ID,
                                       BlogPostErrorEnums.NOT_FOUND_ID.GetDisplayName());
                var blogPostMappingResult = _mapper.Map<UpdateBlogPostRequest, BlogPost>(request, blogPost);
                blogPostMappingResult.UpdateAt = DateTime.Now;
                await _unitOfWork.Repository<BlogPost>().UpdateDetached(blogPostMappingResult);
                await _unitOfWork.CommitAsync();
                return new BaseResponseViewModel<BlogPostResponse>()
                {
                    Status = new StatusViewModel()
                    {
                        Message = "Success",
                        Success = true,
                        ErrorCode = 0
                    },
                    Data = _mapper.Map<BlogPostResponse>(blogPostMappingResult)
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
