using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace WaiBao.Db.Models;

/// <summary>
/// 网站基础信息表
/// </summary>
public class SiteInfoEntity : BaseEntity
{
    /// <summary>
    /// 公司名字
    /// </summary>
    [Required, SugarColumn(IsNullable = false)]
    public string? CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// 联系电话
    /// </summary>
    [Required, SugarColumn(IsNullable = false)]
    public string? Mobile { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? Email { get; set; } = string.Empty;

    /// <summary>
    /// 地址
    /// </summary>
    public string? Address { get; set; } = string.Empty;

    /// <summary>
    /// 经度
    /// </summary>
    public string? Latitude { get; set; }   = string.Empty;

    /// <summary>
    /// 纬度
    /// </summary>
    public string? Longitude { get; set; } = string.Empty;

    /// <summary>
    /// 联系人
    /// </summary>
    public string? ContactPerson { get; set; } = string.Empty;
}
