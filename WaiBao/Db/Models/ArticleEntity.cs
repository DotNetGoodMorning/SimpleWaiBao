using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace WaiBao.Db.Models;

public class ReqArticle(string Title, string Content, Int16 Sort, Int16 Type);

/// <summary>
/// 文章-包括关于我们
/// </summary>
public class ArticleEntity : BaseEntity
{
    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(IsNullable = false), Required(ErrorMessage = "缺少标题信息")]
    public string? Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string Content { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>

    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 下一个
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public int? NetxtId { get; set; }

    /// <summary>
    /// 下一个文章信息
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(NetxtId))]
    public ArticleEntity? NextArticle { get; set; }

    /// <summary>
    /// 上一个
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public int? PreviousId { get; set; }

    /// <summary>
    /// 上一个文章信息
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(PreviousId))]
    public ArticleEntity? PreviousArticle { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 文章类型 0：普通文章 211:关于我们  212：网站底部信息 213：备案号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public Int16 Type { get; set; }
}
