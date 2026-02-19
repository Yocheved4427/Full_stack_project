using Entities;
using Microsoft.AspNetCore.Http;
using Services;
using System.Net.Http;
using System.Threading.Tasks;
namespace WebApiShop.MiddleWare
{
    public class RatingMiddleware
    {
        private readonly RequestDelegate _next;
        public RatingMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context, IRatingService ratingService)
        {
            Rating rating = new Rating();
            rating.Host = context.Request.Host.Value;
            rating.Method = context.Request.Method;
            rating.Path = context.Request.Path;
            rating.Referer = context.Request.Headers.Referer;
            rating.UserAgent = context.Request.Headers.UserAgent;
            rating.RecordDate = DateTime.Now;
            await ratingService.AddRating(rating);
            await _next(context);
        }
    }
    public static class RatingExtensions
    {
        public static IApplicationBuilder UseRating(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RatingMiddleware>();
        }
    }
}
