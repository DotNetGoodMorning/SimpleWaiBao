using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using SqlSugar;

namespace WaiBao;



public class SqlSugarHelper
{

    public static readonly SqlSugarScope Db = new SqlSugarScope(new ConnectionConfig()
    {
        ConnectionString = AppConfig.Settings.DbConnectionStrings,//连接符字串
        DbType = DbType.MySql,
        IsAutoCloseConnection = true,
    }, db =>
    {
        ExternalServicesSetting(db);
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            Console.WriteLine(sql);
        };
    });

    /// <summary>
    /// 拓展配置
    /// </summary>
    /// <param name="db"></param>
    /// <param name="config"></param>
    private static void ExternalServicesSetting(SqlSugarClient db)
    {
        var cache = ServiceLocator.Instance.GetService<SqlSugarMemoryCacheService>();
        db.CurrentConnectionConfig.ConfigureExternalServices = new ConfigureExternalServices
        {
            DataInfoCacheService = cache,
            EntityService = (c, p) =>
            {
                if (p.IsPrimarykey == false && new NullabilityInfoContext()
                 .Create(c).WriteState is NullabilityState.Nullable)
                {
                    p.IsNullable = true;
                }
            }
        };
    }
}


public class WhereIFs<T>
{
    public Expression<Func<T, bool>> expression { get; set; }
    public bool where { get; set; }
}