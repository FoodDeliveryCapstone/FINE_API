using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FINE.Service.Caches
{ 
    public class CacheAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _timeToLiveSeconds;

        public CacheAttribute(int timeToLiveSeconds = 1000)
        {
            _timeToLiveSeconds = timeToLiveSeconds;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var cacheService = context.HttpContext.RequestServices.GetService<IDistributedCache>();
            var cacheKey = GenerateCacheKeyFromRequest(context.HttpContext.Request);
            var cacheResponse = await CacheManager.GetObjectAsync<object>(null, cacheService, cacheKey);

            if(cacheResponse != null)
            {
                var containResult = new ContentResult
                {
                    Content = cacheResponse.ToString(),
                    ContentType = "application/json",
                    StatusCode = 200
                };
                context.Result = containResult;
                return;
            }

            var executedConext = await next();
            if(executedConext.Result is OkObjectResult objectResult)
            {
                await CacheManager.SetObjectAsync(TimeSpan.FromMilliseconds(_timeToLiveSeconds), null, cacheService,
                    cacheKey, objectResult.Value);
            }
        }

        private string GenerateCacheKeyFromRequest(HttpRequest request)
        {
            var keyBuilder = new StringBuilder();
            keyBuilder.Append($"{request.Path}");
            foreach (var (key, value) in request.Query.OrderBy(x => x.Key))
            {
                keyBuilder.Append($"|{key}-{value}");
            }

            return keyBuilder.ToString();
        }
    }
}
