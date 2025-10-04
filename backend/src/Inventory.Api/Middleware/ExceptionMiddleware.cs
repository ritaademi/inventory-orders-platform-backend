using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Inventory.Api.Middleware
{
    public sealed class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                await Handle(ctx, ex);
            }
        }

        private async Task Handle(HttpContext ctx, Exception ex)
        {
            var status = ex switch
            {
                ValidationException => HttpStatusCode.BadRequest,
                KeyNotFoundException => HttpStatusCode.NotFound,
                DbUpdateConcurrencyException => HttpStatusCode.Conflict,
                DbUpdateException => HttpStatusCode.Conflict,
                _ => HttpStatusCode.InternalServerError
            };

            _logger.LogError(ex, "Unhandled exception for {Path} (TraceId: {TraceId})", ctx.Request.Path, ctx.TraceIdentifier);

            var problem = new ProblemDetails
            {
                Title = GetTitle(status),
                Status = (int)status,
                Detail = ex is ValidationException ? "Validation failed for one or more fields." : ex.Message,
                Instance = ctx.Request.Path
            };

            if (ex is ValidationException fv)
            {
                problem.Extensions["errors"] = fv.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            }

            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = problem.Status ?? 500;

            var json = JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await ctx.Response.WriteAsync(json);
        }

        private static string GetTitle(HttpStatusCode code) => code switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.Conflict => "Conflict",
            _ => "Internal Server Error"
        };
    }
}
