using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaiBao.Db.Models;

namespace WaiBao.Api;


/// <summary>
/// 客户反馈
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class FeedBackController:BaseApi
{
  
    /// <summary>
    /// 反馈列表
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ApiResult> GetPage([FromBody] ReqPage model)
    {
       var page = await GetPage<FeedBackEntity>(model,!string.IsNullOrWhiteSpace(model.Key),a=>a.CusName.Contains(model.Key));
        return Success(page);
    }

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost,Authorize]
    public async Task<ApiResult> Save([FromBody] ReqSaveFeedBack model)
    {
        var entity = model.Adapt<FeedBackEntity>();
        await SaveAsync(entity);
        return Success(entity);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet,Authorize]
    public async Task<ApiResult> Del(int id)=> Result(await DeleteAsync<FeedBackEntity>(id));

}
