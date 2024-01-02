using SqlSugar;

namespace WaiBao.Db.Models;

/// <summary>
/// 文件资源
/// </summary>
public class FileSourceEntity : BaseEntity
{
    /// <summary>
    /// 
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 封面图
    /// </summary>
    [SugarColumn(ColumnDataType = "varchar(500)")]
    public string CoverImage { get; set; } = string.Empty;

    /// <summary>
    /// 本地 路径
    /// </summary>
    public string Path { get; set; }

    /// <summary>
    /// 链接
    /// </summary>
    public string Ur { get; set; }

    /// <summary>
    /// 0 图片  1 视频 2:文件
    /// </summary>
    public Int16 SourceType { get; set; }

    /// <summary>
    /// 分类Code
    /// </summary>
    public string ClassCode { get; set; } = string.Empty;

    /// <summary>
    /// 产品信息
    /// </summary>
    [Navigate( NavigateType.OneToOne, nameof(Id),nameof(ProductEntity.VideoId))]
    public ProductEntity? ProductInfo { get; set; }
}


/// <summary>
/// 文件资源分类
/// </summary>
public class FileSourceClassEntity : BaseEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 英文名
    /// </summary>
    public string EnName { get; set; } = string.Empty;

    /// <summary>
    /// 编码   默认数据：001 图片  002 视频 
    /// </summary>
    public string Code { get; set; } = string.Empty;
}
