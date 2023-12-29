using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using Mapster;
using WaiBao.Db.Models;
using System.Linq.Expressions;

namespace WaiBao.Api
{
    /// <summary>
    /// 文章管理
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ArticleController : BaseApi
    {

        /// <summary>
        /// 获取文章分页列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> GetPage([FromBody] ArticleReq model)
        {

            List<WhereIFs<ArticleEntity>> whereIFs = new List<WhereIFs<ArticleEntity>>();

            whereIFs.Add(
                new WhereIFs<ArticleEntity>
                {
                    where = string.IsNullOrEmpty(model.Key),
                    expression = a => a.Title.Contains(model.Key)
                });

            whereIFs.Add(
               new WhereIFs<ArticleEntity>
               {
                   where = model.Type!=-1,
                   expression = a => a.Type == model.Type
               });

            var pageInfo = await GetPage<ArticleEntity>(model, whereIFs, a => a.Sort, OrderByType.Desc);
            return Success(pageInfo);
        }

        /// <summary>
        /// 根据类型获取文章详情
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ApiResult> GetDtlByType(int type)
        {
            var info = await db.Queryable<ArticleEntity>().WithCache(TimeSpan.FromHours(10).Seconds).Where(a => a.Type == type).FirstAsync();
            return Success(info);
        }

        /// <summary>
        /// 根据文章ID获取文章详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ApiResult> GetDtlById(int id)
        {
            var info = await db.Queryable<ArticleEntity>().Where(a => a.Id == id).FirstAsync();
            return Success(info);
        }


        /// <summary>
        /// 保存文章
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>        
        [HttpPost, Authorize]
        public async Task<ApiResult> SaveArticle([FromBody] ArticleEntity model)
        {
            //var entity = model.Adapt<ArticleEntity>();
            model.CreateTime = DateTime.Now;
            await SaveAsync(model);
            return Success(model);
        }

        /// <summary>
        /// 删除文章
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet, Authorize]
        public async Task<ApiResult> DelArticle(int id)
        {
            var info = await GetAsync<ArticleEntity>(a => a.Id == id);
            if (info == null) return SuccessMsg("删除成功");

            //211:关于我们  212：网站底部信息 213：备案号
            int[] banDelArticleType = new int[] { 211, 212, 213 };
            if (banDelArticleType.Contains(info.Type))
            {
                return Error($"'{info.Title}'为系统默认的文章，无法删除");
            }

            await DeleteAsync<ArticleEntity>(id);
            return SuccessMsg(msg: "删除成功");
        }

    }
}
