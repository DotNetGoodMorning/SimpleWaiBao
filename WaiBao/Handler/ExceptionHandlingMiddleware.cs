using System;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WaiBao.Api;

namespace WaiBao;

/// <summary>
/// 全局异常捕获中间件
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;  // 用来处理上下文请求
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// 执行中间件
    /// </summary>
    /// <param name="httpContext"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        bool isHandle = false;
        try
        {   
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
            isHandle = true;
        }
        finally
        {
            if (!isHandle && httpContext.Response.StatusCode == 401 )
            {
                await HandleNotSuccessSattusCodeAsync(httpContext, httpContext.Response.StatusCode);
            }
        }
    }

    /// <summary>
    /// 处理非200的StatusCode
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    private async Task HandleNotSuccessSattusCodeAsync(HttpContext context, int statusCode)
    {
        context.Response.ContentType = "application/json";  
        var response = context.Response;
        var errorResponse = new ApiResult
        {
            Code = -1,
        };

        response.StatusCode = (int)HttpStatusCode.OK;
        switch (statusCode)
        {
            case 401:

                errorResponse.Code = 6;
                errorResponse.Msg = "您没有这个权限访问喔";
                break;
            case 405:
                errorResponse.Msg = "请求类型错误，请尝试更换Get或者Post进行请求";
                break;
            case 403:
                errorResponse.Code = 9;
                errorResponse.Msg = "请求已被服务器拒绝，你当前所用端无权限访问本站点";
                break;
            case 400:
                errorResponse.Msg = "请求已被服务器拒绝，我无法理解你在做什么";
                break;
            case 201:
            case 204:
                errorResponse.Code = 0;
                errorResponse.Msg = "请求成功，无内容返回";
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Msg = $"服务器出现未知返回码：{statusCode},请联系管理员";
                break;
        }

        var result = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(result);
    }

    /// <summary>
    /// 异步处理异常
    /// </summary>
    /// <param name="context"></param>
    /// <param name="exception"></param>
    /// <returns></returns>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";  // 返回json 类型
        var response = context.Response;
        var errorResponse = new ApiResult
        {
            Code = -1,
        };
        // 自定义的异常错误信息类型
        switch (exception)
        {
            case ApplicationException ex:
                if (ex.Message.Contains("Invalid token"))
                {
                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                    errorResponse.Msg = ex.Message;
                    break;
                }
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Msg = ex.Message;
                break;
            case KeyNotFoundException ex:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Msg = ex.Message;
                break;
            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Msg = $"服务器发生异常:{exception.Message}";
                break;
        }
        _logger.LogError(exception.Message);
        var result = JsonSerializer.Serialize(errorResponse);
        await context.Response.WriteAsync(result);
    }
}


public class ResultFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {

        var res = context.Result;
        //在这块处理，由于ApiController特性已经帮你处理了这个错误，封装成了BadRequestObjectResult
        if (res is BadRequestObjectResult)
        {
            var badresObj = (BadRequestObjectResult)res;
            var details = badresObj.Value;
            if (details is ValidationProblemDetails errDetails)
            {
                //errDetails.Errors.Values.Any()
            }
            //badresObj.Value = new ApiResult() { Code = 500, Msg = "服务器异常" };
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var result = JsonSerializer.Serialize(new ApiResult() { Code = 500, Msg = "服务器异常" }, options);

            context.Result = new OkObjectResult(result);


        }
        await next();


    }



}




public class AuthFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var res = context.Result;



        return;
    }
}

public class ActionFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var res = context.Result;
        await next();
    }
}

public class ResourceFilter : IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var res = context.Result;
        await next();
    }
}

