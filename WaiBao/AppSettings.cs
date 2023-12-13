namespace WaiBao
{
    public class AppSettings
    {
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public string JwtSecurityKey { get; set; }
        public string DbConnectionStrings { get; set; }

        /// <summary>
        /// 当前站点url链接
        /// </summary>
        public string WebUrl { get; set; }

        /// <summary>
        /// 默认账号
        /// </summary>
        public string DefaultAccount { get; set; }

        /// <summary>
        /// 默认密码
        /// </summary>
        public string DefaultPwd { get; set; }

        /// <summary>
        /// 管理员邮箱
        /// </summary>
        public string AdminEmail { get; set; }
    }

    public class EmailSetting
    {
        public string SenderName { get; set; }
        public string SmptServer { get; set; }
        public string Subject { get; set; }
        public string SenderEmail { get; set; }
        public string SenderPassword { get; set; }
    }

    public static class ServiceLocator
    {
        public static IServiceProvider Instance { get; set; }

        public static IApplicationBuilder ApplicationBuilder { get; set; }
    }

    public static class AppConfig
    {
        public static AppSettings Settings { get; set; }
        public static EmailSetting EmailSetting { get; set; }

        public static string RootPath { get; set; }
    }
}
