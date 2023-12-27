using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SqlSugar;
using System.Xml.Linq;
using System.ComponentModel.DataAnnotations;
using System.IO;
using WaiBao.Db.Models;

namespace WaiBao.Api
{
    /// <summary>
    /// 文件管理
    /// </summary>
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FileManagerController : BaseApi
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public FileManagerController()
        {
        }

        /// <summary>
        /// 视频列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> GetPage([FromBody] ReqPage model)
        {
            var pageInfo = await GetPage<FileSourceEntity>(model, !string.IsNullOrWhiteSpace(model.Key), a => a.Name.Contains(model.Key), a => a.Id, OrderByType.Desc);
            return Success(pageInfo);
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="id">资源ID</param>
        /// <returns></returns>
        [HttpGet]
        public async Task<FileStreamResult> DownFile(int id)
        {
            var info = await db.Queryable<FileSourceEntity>().Where(a => a.Id == id).FirstAsync();
            if (info == null)
            {
                throw new Exception("资源文件不存在");
            }

            var stream = System.IO.File.OpenRead(info.Path);
            string fileExt = ".mp4";
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            var memi = provider.Mappings[fileExt];
            var fileName = Path.GetFileName(info.Path);
            return File(stream, memi, fileName);
        }

        /// <summary>
        /// 上传视频
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> UploadVideo([FromForm] UploadFileDto dto)
        {

            if (dto.Files == null || !dto.Files.Any()) return Error("请选择上传的视频。");
            //格式限制
            //var allowType = new string[] { "image/jpg", "image/png", "image/jpeg" };
            var allowType = new string[] { "video/mp4" };

            var allowedFile = dto.Files.Where(c => allowType.Contains(c.ContentType));
            if (!allowedFile.Any()) return Error("视频格式错误");
            if (allowedFile.Sum(c => c.Length) > 1024 * 1024 * 4) return Error("视频过大");

            //string foldername = "images";
            string foldername = "videos";
            string settingPath = "nfile";
            string folderpath = Path.Combine(settingPath, foldername);
            //本地文件夹路径
            string localPath = Path.Combine(AppConfig.RootPath, folderpath);

            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            List<FileSourceEntity> lst = new();

            foreach (var file in allowedFile)
            {
                string strpath = Path.Combine(foldername, DateTime.Now.ToString("MMddHHmmss") + Path.GetFileName(file.FileName));
                var path = Path.Combine(settingPath, strpath);

                //本地文件路径
                var localFilePath = Path.Combine(AppConfig.RootPath, path);

                lst.Add(new FileSourceEntity
                {
                    Name = file.FileName,
                    Path = path,
                    SourceType = 1,
                    Ur = AppConfig.Settings.WebUrl + "/" + path.Replace("\\", "/"),
                });
                using (var stream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    await file.CopyToAsync(stream);
                }
            }

            //保存
            var saveResult = await db.Insertable(lst).ExecuteCommandAsync();

            var excludeFiles = dto.Files.Except(allowedFile);

            if (excludeFiles.Any())
            {
                var infoMsg = $"{string.Join('、', excludeFiles.Select(c => c.FileName))} 图片格式错误";
                return Success(infoMsg);
            }
            return Success("上传成功");
        }

        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> UploadImages([FromForm] UploadFileDto dto)
        {

            if (dto.Files == null || !dto.Files.Any()) return Error("请选择上传的图片。");
            //格式限制
            var allowType = new string[] { "image/jpg", "image/png", "image/jpeg" };

            var allowedFile = dto.Files.Where(c => allowType.Contains(c.ContentType));
            if (!allowedFile.Any()) return Error("图片格式错误");
            if (allowedFile.Sum(c => c.Length) > 1024 * 1024 * 2) return Error("图片过大，请保持2M以下");

            string foldername = "images";
            string settingPath = "nfile";
            string folderpath = Path.Combine(settingPath, foldername);

            //本地文件夹路径
            string localPath = Path.Combine(AppConfig.RootPath, folderpath);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            List<FileSourceEntity> lst = new();

            foreach (var file in allowedFile)
            {
                string strpath = Path.Combine(foldername, DateTime.Now.ToString("MMddHHmmss") + Path.GetFileName(file.FileName));
                var path = Path.Combine(settingPath, strpath);

                //本地文件路径
                var localFilePath = Path.Combine(AppConfig.RootPath, path);

                lst.Add(new FileSourceEntity
                {
                    Name = file.FileName,
                    Path = path,
                    SourceType = 0,
                    Ur = AppConfig.Settings.WebUrl + "/" + path.Replace("\\", "/"),
                });
                using (var stream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    await file.CopyToAsync(stream);
                }
            }

            //保存
            var saveResult = await db.Insertable(lst).ExecuteCommandAsync();

            var excludeFiles = dto.Files.Except(allowedFile);

            if (excludeFiles.Any())
            {
                var infoMsg = $"{string.Join('、', excludeFiles.Select(c => c.FileName))} 图片格式错误";
                return Success(infoMsg);
            }
            return Success(lst);
        }

        public class UploadFileDto
        {
            //多文件
            [Required(ErrorMessage = "上传的文件不可为空")]
            public IFormFileCollection Files { get; set; }

            //单文件
            //public IFormFile File { get; set; }

            //其他数据
            public string Foo { get; set; }


        }

    }
}
