using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using WaiBao.Db.Models;

namespace WaiBao.Api
{
    /// <summary>
    /// 基类Api
    /// </summary>
    public class BaseApi : ControllerBase
    {
        /// <summary>
        /// sqlsugar单例对象
        /// </summary>
        public SqlSugarScope db = SqlSugarHelper.Db;


        #region Orm封装
        /// <summary>
        /// 新增单条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<bool> AddAsync<T>(T model) where T : BaseEntity, new()
        {
            var result = await db.Insertable<T>(model).ExecuteCommandAsync();
            return result > 0;
        }

        /// <summary>
        /// 修改数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        [NonAction]
        public async Task<bool> UpdateAync<T>(T model) where T : BaseEntity, new()
        {
            var result = await db.Updateable<T>(model).ExecuteCommandAsync();
            return result > 0;
        }


        #region 保存

        /// <summary>
        /// 保存单条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> SaveAsync<T>(T model) where T : BaseEntity, new()
        {
            var x = db.Storageable(model).ToStorage();
            int insterResult = await x.AsInsertable.ExecuteCommandAsync();
            int updateResult = await x.AsUpdateable.ExecuteCommandWithOptLockAsync();
            //TODO 有空再弄严谨一点
            return true;
        }

        /// <summary>
        /// 保存多条数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> SaveAsync<T>(List<T> model) where T : BaseEntity, new()
        {
            var x = db.Storageable(model).ToStorage();
            int insterResult = await x.AsInsertable.ExecuteCommandAsync();
            int updateResult = await x.AsUpdateable.ExecuteCommandWithOptLockAsync();
            //TODO 有空再弄严谨一点
            return true;
        }

        #endregion

        #region 删除

        /// <summary>
        /// 删除一条指定主键ID的数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAsync<T>(int id) where T : BaseEntity, new() => await db.Deleteable<T>().Where(a => a.Id == id).ExecuteCommandAsync() > 0;

        #endregion

        #region 普通查询

        /// <summary>
        /// 获取单个数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(Expression<Func<T, bool>> expression) => await db.Queryable<T>().Where(expression).FirstAsync();

        /// <summary>
        /// 获取数据列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> expression) => await db.Queryable<T>().Where(expression).ToListAsync();


        #endregion

        #region 分页查询


        [NonAction]
        public async Task<object> GetPage<T>(ReqPage model, bool isWhere, Expression<Func<T, bool>> expression, Expression<Func<T, object>>? orderByexpression = null, OrderByType type = OrderByType.Asc)
        {
            //单表分页
            RefAsync<int> total = 0;//REF和OUT不支持异步,想要真的异步这是最优解
            var lstQuery = db.Queryable<T>();

            var lst = await lstQuery.WhereIF(isWhere, expression)
                .OrderByIF(orderByexpression != null, orderByexpression, type)
                 .ToPageListAsync(model.PageNumber, model.PageSize, total);

            return new ResPage
            {
                Data = lst,
                PageSize = model.PageSize,
                PageNumber = model.PageNumber,
                Total = total
            };

        }

        [NonAction]
        public async Task<object> GetPage<T>(ReqPage model, Dictionary<bool, Expression<Func<T, bool>>> whereIFs)
        {
            //单表分页
            RefAsync<int> total = 0;//REF和OUT不支持异步,想要真的异步这是最优解
            var lstQuery = db.Queryable<T>();

            foreach (var whereIF in whereIFs)
            {
                lstQuery = lstQuery.WhereIF(whereIF.Key, whereIF.Value);
            }

            var lst = await lstQuery
                 .ToPageListAsync(model.PageNumber, model.PageSize, total);

            return new ResPage
            {
                Data = lst,
                PageSize = model.PageSize,
                PageNumber = model.PageNumber,
                Total = total
            };

        }

        #endregion

        #endregion

        /// <summary>
        /// 根据操作结果提示成功还是失败
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        [NonAction]
        public ApiResult Result(bool result)
        {
            if (result)
            {
                return SuccessMsg("操作成功");
            }
            return Error("操作失败");
        }

        [NonAction]
        public ApiResult Success(object data, string msg = "成功")
        {
            return new ApiResult() { Code = 0, Data = data, Msg = msg };
        }

        [NonAction]
        public ApiResult SuccessMsg(string msg = "执行成功")
        {
            return new ApiResult() { Code = 0, Data = new { }, Msg = msg };
        }

        [NonAction]
        public ApiResult Error(string msg)
        {
            return new ApiResult() { Code = 0, Data = new { }, Msg = msg };
        }



        [NonAction]
        public string Encrypt(string input)
        {
            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                string encryptedString = Convert.ToBase64String(encryptedBytes);

                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytesMd5 = Encoding.UTF8.GetBytes(encryptedString);
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString().ToLower();
                }
            }
        }


        [NonAction]
        public bool CheckIsInitSystem()
        {
            if (
                db.DbMaintenance.IsAnyTable(nameof(SysUserEntity), false)
                && db.DbMaintenance.IsAnyTable(nameof(FileSourceEntity), false)
                && db.DbMaintenance.IsAnyTable(nameof(ArticleEntity), false)
                && db.DbMaintenance.IsAnyTable(nameof(ProductClassEntity), false)
                && db.DbMaintenance.IsAnyTable(nameof(ProductEntity), false)
                )
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检测Token信息
        /// </summary>
        /// <returns></returns>
        [NonAction]
        public string GetCurrentTokenName()
        {

            var httpContext = HttpContext;

            // 从请求头中获取 Authorization 标头的值
            var authorizationHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer "))
            {
                // 提取令牌字符串（去除 "Bearer " 前缀）
                var token = authorizationHeader.Substring(7);

                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                // 获取 ClaimTypes.Name 的值
                var username = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "name")?.Value;

                // 在这里使用 username 进行其他操作

                return username;
            }

            throw new Exception("无法获取到token信息");
        }


        /// <summary>
        /// 给管理员邮箱发送消息
        /// </summary>
        [NonAction]
        public void NoticeAdminEmail(string msg) => SendEmail(msg);

        [NonAction]
        public void SendEmail(string body, string subject = "网站安全通知")
        {
            var email = new Email(recipientEmail: AppConfig.Settings.AdminEmail, subject: subject ?? AppConfig.EmailSetting.Subject, body);
            var sender = new EmailSender(AppConfig.EmailSetting.SmptServer, 465, senderEmail: AppConfig.EmailSetting.SenderEmail, senderPassword: AppConfig.EmailSetting.SenderPassword);

            sender.SendEmail(email);
        }
    }


    public class DataPageBase
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public int Total { get; set; }

    }

    public class ReqPage : DataPageBase
    {
        public string Key { get; set; }
    }

    public class ResPage : DataPageBase
    {
        public object Data { get; set; }
    }

    public class ApiResult
    {
        /// <summary>
        /// -1 异常失败
        /// 0 正常
        /// 6: 未授权
        /// 9：请求方法错误
        /// 
        /// tip：为啥这么搞？增加一点可有可无的安全性
        /// </summary>
        public int Code { get; set; }
        public string Msg { get; set; }
        public object Data { get; set; }
    }
}
