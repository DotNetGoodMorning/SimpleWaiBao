using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace WaiBao.Db.Models;

/// <summary>
/// 系统管理员
/// </summary>
public class SysUserEntity : BaseEntity
{
    /// <summary>
    /// 账号
    /// </summary>
    [Required, SugarColumn(IsNullable = false)]
    public string UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [Required, SugarColumn(IsNullable = false)]
    public string UsePwd { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public string? Email { get; set; }

    /// <summary>
    /// 为true的时候禁止登录
    /// </summary>
    public bool? IsBan { get; set; } = false;
}
