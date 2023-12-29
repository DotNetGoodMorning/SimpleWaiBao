﻿using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using System.Xml.Linq;
using Mapster;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using WaiBao.Db.Models;

namespace WaiBao.Api;

/// <summary>
/// 产品
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
public class ProductController : BaseApi
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ProductController()
    {

    }

    #region 展示

    /// <summary>
    /// 获取产品分类树形
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApiResult> GetProductClassTree()
    {
        var tree = await db.Queryable<ProductClassEntity>().ToTreeAsync(it => it.Child, it => it.ParentId, 0);
        return Success(tree);
    }


    /// <summary>
    /// 获取产品列表
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost]
    public async Task<ApiResult> GetLstProduct([Required(ErrorMessage = "缺少入参")][FromBody] ReqProductPage model)
    {
        List<WhereIFs<ProductEntity>> whereIFs = new List<WhereIFs<ProductEntity>>();
        whereIFs.Add(
            new WhereIFs<ProductEntity>
            {
                where = model.ClassId != -1,
                expression = a => a.ClassId == model.ClassId
            });
        whereIFs.Add(
            new WhereIFs<ProductEntity>
            {
                where = model.IsHot != -1,
                expression = a => a.IsHot == model.IsHot
            });

        whereIFs.Add(
           new WhereIFs<ProductEntity>
           {
               where = string.IsNullOrEmpty(model.Key),
               expression = a => a.ProductName.Contains(model.Key) || a.ArticleNumber.Contains(model.Key)
           });
        var pageInfo = await GetPage<ProductEntity>(model.Adapt<ReqPage>(), whereIFs,a=>a.Id,  OrderByType.Desc,true);
        return Success(pageInfo);
    }
    #endregion


    #region 管理

    /// <summary>
    /// 保存产品分类
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Authorize]
    public async Task<ApiResult> SaveProductClass([Required(ErrorMessage = "缺少入参")][FromBody] ProductClassEntity model)
    {
        //var hasDuplicate = await db.Queryable<ProductClassEntity>()
        // .Where(a => a.Title == model.Title && (model.Id <= 0 || a.Id != model.Id))
        // .AnyAsync();
        //if (hasDuplicate)
        //{
        //    return Error("已经存在相同名称的产品分类了");
        //}
        await SaveAsync(model);
        return Success(model);
    }

    /// <summary>
    /// 删除产品分类
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApiResult> DelProductClass([Required(ErrorMessage = "缺少入参")] int id) => Success(await DeleteAsync<ProductClassEntity>(id));

    /// <summary>
    /// 保存产品
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    [HttpPost, Authorize]
    public async Task<ApiResult> SaveProduct([Required(ErrorMessage = "缺少入参")][FromBody] ProductEntity model)
    {
        var hasDuplicate = await db.Queryable<ProductEntity>()
         .Where(a => a.ProductName == model.ProductName && (model.Id <= 0 || a.Id != model.Id))
         .AnyAsync();
        if (hasDuplicate)
        {
            return Error("已经存在相同名称的产品分类了");
        }

        await SaveAsync(model);
        return Success(model);
    }

    /// <summary>
    /// 删除产品
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApiResult> DelProduct([Required(ErrorMessage = "缺少入参")] int id)
    {
        await DeleteAsync<ProductEntity>(id);
        return SuccessMsg("删除成功");
    }
    /// <summary>
    /// 删除产品
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    public async Task<ApiResult> DelProductMore([Required(ErrorMessage = "缺少入参")] int[] ids)
    {
        await DeleteAsync<ProductEntity>(ids);
        return SuccessMsg("删除成功");
    }

    #endregion
}

/// <summary>
/// 产品分页查询入参
/// </summary>
public class ReqProductPage : ReqPage
{
    /// <summary>
    /// 分类ID
    /// </summary>
    public int ClassId { get; set; } = -1;

    /// <summary>
    /// 是否是热销系列
    /// 0: 非热销
    /// 1:热销
    /// </summary>
    public int? IsHot { get; set; } = -1;
}
