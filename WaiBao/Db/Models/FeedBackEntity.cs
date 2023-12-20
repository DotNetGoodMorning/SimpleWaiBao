namespace WaiBao.Db.Models;
using SqlSugar;


/// <summary>
/// 客户反馈模型
/// </summary>
public class FeedBackEntity:BaseEntity
{
    /// <summary>
    /// 客户名称
    /// </summary>
    public string CusName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 反馈内容
    /// </summary>
    [SugarColumn(ColumnDataType = "varchar(1000)")]
    public string? Content { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}

/// <summary>
/// 保存DTO
/// </summary>
public class ReqSaveFeedBack
{
    /// <summary>
    /// 客户名称
    /// </summary>
    public string CusName { get; set; } = string.Empty;

    /// <summary>
    /// 手机号
    /// </summary>
    public string Mobile { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 反馈内容
    /// </summary>
    [SugarColumn(ColumnDataType = "varchar(1000)")]
    public string? Content { get; set; } = string.Empty;
}
