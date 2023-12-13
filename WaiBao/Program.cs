using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WaiBao;
using System.Text.Json.Serialization;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

#region 代理请求测试
//await ProxyRun();
#endregion


builder.Services.Configure<MvcOptions>(o =>
{
    o.Filters.Add<AuthFilter>();
    o.Filters.Add<ActionFilter>();
    o.Filters.Add<ResourceFilter>();
    o.Filters.Add<ResultFilter>();
});





builder.Services.AddControllers()
.AddJsonOptions(options =>
{

    //避免在序列化对象时陷入无限循环，如果这样了这个配置就直接不鸟它
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    //默认就是转换为小驼峰式命名规则，设为null 就是保留原来格式
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    //将对象序列化为 JSON 字符串时，将使用缩进格式来使生成的 JSON 字符串更易读
    options.JsonSerializerOptions.WriteIndented = true;
    /*
     * 本意：如果该值为 null，则在序列化过程中该属性将被忽略，仅适用于引用类型属性和字段
     * 和之前的 就是之前的 IgnoreNullValues = true; 一个意思
     */
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    //在序列化和反序列化枚举类型时使用字符串表示
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    //大小写敏感
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
    //在将字典序列化为 JSON 字符串时，字典键的名称将使用驼峰命名法
    options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    //禁止在生成的 JSON 字符串中包含尾随逗号
    options.JsonSerializerOptions.AllowTrailingCommas = false;
    //禁止在 JSON 字符串中包含注释
    options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Disallow;
    //设置JSON序列化器的最大嵌套深度为 0 （防止恶意攻击，往你json里面塞进去几个G的字符串）
    options.JsonSerializerOptions.MaxDepth = 0;
});

#region 基本设置



#region 加载配置

string myEnvVariableValue = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
if (!string.IsNullOrWhiteSpace(myEnvVariableValue)) myEnvVariableValue = $".{myEnvVariableValue}"; else myEnvVariableValue = "";
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile($"appsettings{myEnvVariableValue}.json", optional: true, reloadOnChange: true)
    .Build();

var appSettings = new AppSettings();
var emailSetting = new EmailSetting();
configuration.GetSection("AppSettings").Bind(appSettings);
configuration.GetSection("EmailSetting").Bind(emailSetting);
AppConfig.Settings = appSettings;
AppConfig.EmailSetting = emailSetting;
AppConfig.RootPath = builder.Environment.WebRootPath;
#endregion

//内存缓存
builder.Services.AddMemoryCache();
//控制器
builder.Services.AddControllers();
//限制文件上传大小
builder.Services.Configure<FormOptions>(options =>
{
    // 设置上传大小限制256MB
    options.MultipartBodyLengthLimit = 268435456;

});
//注入sqlsugar缓存实现
builder.Services.AddSingleton<SqlSugarMemoryCacheService>();
#endregion

#region 简单的授权鉴权

// 添加身份验证和授权中间件
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = AppConfig.Settings.JwtIssuer,
            ValidAudience = AppConfig.Settings.JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AppConfig.Settings.JwtSecurityKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    //简单添加一个管理员身份策略，但是在本项目体现不出来，如果需要使用则在指定api上加 [Authorize(Policy = "AdminOnly")] 代表该接口使用 AdminOnly 策略
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireClaim("role", "admin");
    });
});

#endregion

#region 添加 swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "企业官网Api", Version = "v1" });

    // 添加身份验证
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    // 添加授权要求
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });

    // 设置 XML 注释文件的路径
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
#endregion

var app = builder.Build();
app.UseSwagger();
app.UseStaticFiles();
//允许所有源
app.UseCors(options =>
{
    options.AllowAnyOrigin();
    options.AllowAnyMethod();
    options.AllowAnyHeader();
    //options.AllowCredentials();
});
//异常处理中间件，包含401等状态处理
app.UseMiddleware<ExceptionHandlingMiddleware>();
// 启用身份验证和授权中间件
app.UseAuthentication();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "企业官网Api");
    // 将 Swagger UI 设置为应用程序的根路径
    c.RoutePrefix = string.Empty;
});
//方便随地大小便，但是官方是不推荐这样做的（去踏马的官方，爷走特色主义路线）
ServiceLocator.Instance = app.Services;
ServiceLocator.ApplicationBuilder = app;
app.MapGet("/health", () => "1024");
app.Run();

//static async Task ProxyRun()
//{
//    var proxy = new WebProxy("proxy.shenlongproxy.com", 31212);                                             
//    proxy.Credentials = new NetworkCredential(userName: "customer-42b05a76a27", password: "d413b4b9"); 
//    var httpClientHandler = new HttpClientHandler
//    {
//        Proxy = proxy,
//    };
//    HttpClient client = new HttpClient(httpClientHandler);
//    var ret = await client.GetStringAsync("https://www.google.com");
//    Console.WriteLine(ret);
//}