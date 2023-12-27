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

namespace WaiBao.Api;


/// <summary>
/// 系统管理
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class SystemManagerController : BaseApi
{
    protected IMemoryCache _memoryCache;
    public SystemManagerController(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// 获取系统是否初始化
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ApiResult GetIsInitSystem()
    {
        if (CheckIsInitSystem())
        {
            return Success(1, "系统已初始化成功");
        }
        return Success(0, "系统尚未初始化");
    }

    /// <summary>
    /// 默认系统初始化，无权限，只能初始化一次
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApiResult> DefaultInitSystem()
    {
        if (CheckIsInitSystem())
        {
            return Success(1, "系统已初始化成功，请勿重复操作，如需重置系统请登录后操作");
        }

        //不存在创建数据库存在不会重复创建
        db.DbMaintenance.CreateDatabase();

        return await InitSystem();
    }

    /// <summary>
    /// 系统初始化操作，有权限,用于后台重置网站数据
    /// </summary>
    /// <returns></returns>
    [HttpGet, Authorize]
    public async Task<ApiResult> InitSystem()
    {
        //清空表
        if (db.DbMaintenance.IsAnyTable(nameof(SysUserEntity), false)) db.DbMaintenance.DropTable(nameof(SysUserEntity));
        if (db.DbMaintenance.IsAnyTable(nameof(FileSourceEntity), false)) db.DbMaintenance.DropTable(nameof(FileSourceEntity));
        if (db.DbMaintenance.IsAnyTable(nameof(ArticleEntity), false)) db.DbMaintenance.DropTable(nameof(ArticleEntity));
        if (db.DbMaintenance.IsAnyTable(nameof(ProductClassEntity), false)) db.DbMaintenance.DropTable(nameof(ProductClassEntity));
        if (db.DbMaintenance.IsAnyTable(nameof(ProductEntity), false)) db.DbMaintenance.DropTable(nameof(ProductEntity));
        if (db.DbMaintenance.IsAnyTable(nameof(SlideShowEntity), false)) db.DbMaintenance.DropTable(nameof(SlideShowEntity));    
        if (db.DbMaintenance.IsAnyTable(nameof(FeedBackEntity), false)) db.DbMaintenance.DropTable(nameof(FeedBackEntity));

        //初始化用户表
        db.CodeFirst.InitTables<SysUserEntity>();
        db.CodeFirst.InitTables<FeedBackEntity>();

        #region 初始化用户表数据
        string name = AppConfig.Settings.DefaultAccount;
        string pwd = Encrypt(AppConfig.Settings.DefaultPwd);
        var loginResult = await db.Queryable<SysUserEntity>().Where(a => !a.IsBan.Value && a.UsePwd == pwd && a.UserName == name).AnyAsync();
        if (!loginResult)
        {
            await db.Insertable<SysUserEntity>(new SysUserEntity { IsBan = false, UsePwd = pwd, UserName = name }).ExecuteCommandAsync();
        }
        #endregion

        //初始化基础表
        db.CodeFirst.InitTables<FileSourceEntity>();
        db.CodeFirst.InitTables<ArticleEntity>();

        #region 初始化文章表数据（文章类型 0：普通文章 211:关于我们  212：网站底部信息 213：备案号）
        db.Insertable(new ArticleEntity { Type = 211, Content = "关于我们的内容", CreateTime = DateTime.Now, Sort = -1, Title = "关于我们" }).ExecuteCommand();
        db.Insertable(new ArticleEntity { Type = 212, Content = "网站底部信息", CreateTime = DateTime.Now, Sort = -1, Title = "网站底部信息" }).ExecuteCommand();
        db.Insertable(new ArticleEntity { Type = 213, Content = "备案号", CreateTime = DateTime.Now, Sort = -1, Title = "备案号" }).ExecuteCommand();
        #endregion

        db.CodeFirst.InitTables<ProductClassEntity>();
        db.CodeFirst.InitTables<ProductEntity>();
        db.CodeFirst.InitTables<SlideShowEntity>();

        return SuccessMsg("初始化完成");
    }


}
