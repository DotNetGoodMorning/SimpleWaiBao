using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using WaiBao.Db.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WaiBao.Api;

#region FastEndpoints 简单测试

//public class MyEndpoint : Endpoint<SysUserEntity, ApiResult>
//{
//    public override void Configure()
//    {
//        Post("/api/test/test");
//        AllowAnonymous();
//    }

//    public override async Task HandleAsync(SysUserEntity req, CancellationToken ct)
//    {
//        await SendAsync(new()
//        {
//            Code = 0,
//            Data = "aaa",
//            Msg = "啊？"
//        });
//    }
//}

#endregion

/// <summary>
/// 系统用户
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class SysUserController : BaseApi
{
    protected IMemoryCache _memoryCache;
    public SysUserController(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// 检测Token信息
    /// </summary>
    /// <returns></returns>
    [Microsoft.AspNetCore.Mvc.HttpGet]
    [Authorize(Policy = "AdminOnly", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public ApiResult CheckToken() => Success($"当前Token用户是:{GetCurrentTokenName()}");

    /// <summary>
    /// 获取用户列表分页
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Authorize]
    public async Task<ApiResult> GetPage([FromBody] ReqPage model)
    {
        var lst = await GetPage<SysUserEntity>(model, true, a => a.UserName.Contains(model.Key));
        return Success(lst);
    }

    /// <summary>
    /// 添加系统用户
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Authorize]
    public async Task<ApiResult> AddSysUser([FromBody] SysUserEntity model)
    {
        model.UsePwd = Encrypt(model.UsePwd);
        await db.Insertable<SysUserEntity>(model).ExecuteCommandAsync();
        return Success("添加成功");
    }

    /// <summary>
    /// 解封账号
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet, Authorize]
    public async Task<ApiResult> UnBan(int id)
    {
        var hasAccount = await db.Queryable<SysUserEntity>().Where(a => a.Id == id).FirstAsync();
        if (hasAccount == null) return Error("该用户不存在");

        //解除数据库
        hasAccount.IsBan = false;
        await db.Updateable<SysUserEntity>(hasAccount).ExecuteCommandAsync();

        //解除内存缓存控制
        _memoryCache.Remove(hasAccount.UserName);

        //通知操作
        NoticeAdminEmail($"对 {hasAccount.UserName}的封禁，已经于 {DateTime.Now.ToString("yyyy年MM月dd日 HH时mm分ss秒")} 由 {GetCurrentTokenName()} 解除");

        return Success(null);
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [Microsoft.AspNetCore.Mvc.HttpPost]
    public async Task<ApiResult> Login([FromBody] SysUserEntity model)
    {
        var replyVal = _memoryCache.Get<int>(model.UserName);
        string pwd = Encrypt(model.UsePwd);
        if (replyVal >= 10)
        {

            var hasAccount = await db.Queryable<SysUserEntity>().Where(a => !a.IsBan.Value && a.UsePwd == pwd && a.UserName == model.UserName).FirstAsync();
            if (hasAccount != null)
            {
                hasAccount.IsBan = true;
                await db.Updateable<SysUserEntity>(hasAccount).ExecuteCommandAsync();
            }

            NoticeAdminEmail($"{model.UserName} 于 {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 因三小时内账号连续输入错误密码而被禁止登录");
            return Error("三小时内账号密码已经连续输入错误10次，已禁止登录，请联系管理员");
        }



        var loginResult = await db.Queryable<SysUserEntity>().Where(a => !a.IsBan.Value && a.UsePwd == pwd && a.UserName == model.UserName).AnyAsync();

        // 验证用户名和密码
        if (!loginResult)
        {
            _memoryCache.Set<int>(model.UserName, replyVal += 1, TimeSpan.FromHours(3));
            return Error($"账号密码错误,你还有{10 - replyVal}次机会");
        }

        _memoryCache.Remove(model.UserName);

        // 生成 JWT 令牌
        var tokenHandler = new JwtSecurityTokenHandler();

        var key = Encoding.ASCII.GetBytes(AppConfig.Settings.JwtSecurityKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.UserName),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim(JwtRegisteredClaimNames.Jti, model.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToString()),
                new Claim(ClaimTypes.Expiration, DateTime.Now.AddHours(10).ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = AppConfig.Settings.JwtIssuer,
            Audience = AppConfig.Settings.JwtAudience,

        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        //邮件通知
        NoticeAdminEmail($"{model.UserName} 于 {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} 登录了网站后台");
        // 返回 JWT 令牌
        return Success(new { token = "Bearer " + tokenString });
    }

    #region 发送邮件

    /// <summary>
    /// 发送邮件
    /// </summary>
    /// <param name="body"></param>
    /// <param name="mailTo"></param>
    [HttpGet, Authorize]
    public void SendEmail(string body = "我是171测试邮箱", string mailTo = "prcknightning@gmail.com", string subject = "你有一个新的通知")
    {
        var email = new Email(recipientEmail: mailTo, subject: subject ?? AppConfig.EmailSetting.Subject, body);
        var sender = new EmailSender(AppConfig.EmailSetting.SmptServer, 465, senderEmail: AppConfig.EmailSetting.SenderEmail, senderPassword: AppConfig.EmailSetting.SenderPassword);

        sender.SendEmail(email);
    }

    #endregion
}
