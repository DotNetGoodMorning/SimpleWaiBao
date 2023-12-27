using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SqlSugar;

namespace WaiBao.Db.Models;

/// <summary>
/// 数据库实体基类
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    /// <summary>
    /// 标识版本字段,用于乐观锁
    /// </summary>
    [SqlSugar.SugarColumn(IsEnableUpdateVersionValidation = true)]
    public long Ver { get; set; }
}

/// <summary>
/// 值转字符串
/// </summary>
/// <summary>
/// 长整形转字符串
/// </summary>
public class LongToStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            ReadOnlySpan<byte> span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out long number, out int bytesConsumed) && span.Length == bytesConsumed)
            {
                return number;
            }

            if (long.TryParse(reader.GetString(), out number))
            {
                return number;
            }
        }

        return reader.GetInt64();
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
