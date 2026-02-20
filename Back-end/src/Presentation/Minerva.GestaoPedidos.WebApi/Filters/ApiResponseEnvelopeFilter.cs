using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Minerva.GestaoPedidos.WebApi.Common;

namespace Minerva.GestaoPedidos.WebApi.Filters;

/// <summary>
/// Encapsula respostas 200 e 201 no envelope ApiResponse&lt;T&gt;.
/// </summary>
public sealed class ApiResponseEnvelopeFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is OkObjectResult okResult)
        {
            if (okResult.Value is not ApiResponse<object>)
                context.Result = new OkObjectResult(ApiResponse<object>.Ok(okResult.Value));
            return;
        }

        if (context.Result is CreatedAtActionResult created)
        {
            var value = created.Value is ApiResponse<object> ? created.Value : ApiResponse<object>.Ok(created.Value);
            var actionContext = new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor);
            var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(actionContext);
            var location = urlHelper?.Action(created.ActionName, created.ControllerName, created.RouteValues) ?? "";
            if (!string.IsNullOrEmpty(location))
                context.HttpContext.Response.Headers.Append("Location", location);
            context.Result = new ObjectResult(value) { StatusCode = StatusCodes.Status201Created };
            return;
        }

        if (context.Result is BadRequestObjectResult badRequest && badRequest.Value is not ApiResponse<object>)
        {
            var errors = GetErrorsFromValue(badRequest.Value);
            context.Result = new ObjectResult(ApiResponse<object>.Failure(errors.FirstOrDefault() ?? "Requisição inválida.", errors)) { StatusCode = StatusCodes.Status400BadRequest };
            return;
        }

        if (context.Result is UnauthorizedObjectResult unauthorized && unauthorized.Value is not ApiResponse<object>)
        {
            var errors = GetErrorsFromValue(unauthorized.Value);
            context.Result = new ObjectResult(ApiResponse<object>.Failure(errors.FirstOrDefault() ?? "Não autorizado.", errors)) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }

    private static List<string> GetErrorsFromValue(object? value)
    {
        if (value == null) return new List<string> { "Requisição inválida." };
        if (value is List<string> list) return list;
        var type = value.GetType();
        if (type.GetProperty("errors")?.GetValue(value) is IEnumerable<object> errs)
            return errs.Select(e => e?.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)).ToList();
        if (type.GetProperty("error")?.GetValue(value) is string single)
            return string.IsNullOrEmpty(single) ? new List<string>() : new List<string> { single };
        return new List<string> { value.ToString() ?? "Requisição inválida." };
    }

    public void OnResultExecuted(ResultExecutedContext context) { }
}
