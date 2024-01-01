using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WaiBao.Api;
using WaiBao.Db.Models;

namespace WaiBao;


/// <summary>
/// 站点信息设置
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class SiteInfoController : BaseApi
{
    /// <summary>
    /// 获取信息
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApiResult> Info()
    {
        var info = await GetAsync<SiteInfoEntity>(a => true);
        return Success(info);
    }



    /// <summary>
    /// 修改
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async  Task<ApiResult> Update([FromBody] SiteInfoEntity model)
    {
        return Result(await db.Updateable(model).ExecuteCommandAsync() > 0);
    }
}
