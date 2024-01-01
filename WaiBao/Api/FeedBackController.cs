using Mapster;
using Masuit.Tools.Strings;
using Masuit.Tools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WaiBao.Db.Models;
using Microsoft.Extensions.Caching.Memory;

namespace WaiBao.Api;


/// <summary>
/// 客户反馈
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class FeedBackController : BaseApi
{
    /// <summary>
    /// 缓存
    /// </summary>
    protected IMemoryCache _memoryCache;

    /// <summary>
    /// 构造
    /// </summary>
    /// <param name="memoryCache"></param>
    public FeedBackController(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    /// <summary>
    /// 反馈列表
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ApiResult> GetPage([FromBody] ReqPage model)
    {
        var page = await GetPage<FeedBackEntity>(model, !string.IsNullOrWhiteSpace(model.Key), a => a.CusName.Contains(model.Key));
        return Success(page);
    }

    /// <summary>
    /// 保存
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ApiResult> Save([FromBody] ReqSaveFeedBack model)
    {
        var entity = model.Adapt<FeedBackEntity>();
        entity.CreateTime = DateTime.Now;
        await SaveAsync(entity);
        return Success(entity);
    }

    /// <summary>
    /// 删除
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet, Authorize]
    public async Task<ApiResult> Del(int id) => Result(await DeleteAsync<FeedBackEntity>(id));

    /// <summary>
    /// 校验验证码
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ApiResult CheckValidateCode(string code,string mobile)
    {

        var mCode = _memoryCache.Get<string>($"fbcode_{mobile}");
        if (mCode == code)
        {
            return SuccessMsg("校验成功");
        }

        return Error("无效的验证码");
    }


    /// <summary>
    /// 返回验证码
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public IActionResult SendValidateCode(string mobile)
    {
        var code = ValidateCode.CreateValidateCode(4); // 生成6位长度的验证码
        var stream = code.CreateValidateGraphic(); // 生成验证码图片流

        _memoryCache.Set($"fbcode_{mobile}", code, DateTime.Now.AddMinutes(5));


        byte[] verifyByte = ConvertStreamToByteArray(stream); // 将 Stream 转换为字节数组


        return File(verifyByte, "image/jpg");
    }

    private byte[] ConvertStreamToByteArray(Stream stream)
    {
        using (MemoryStream memoryStream = new MemoryStream())
        {
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }


}
