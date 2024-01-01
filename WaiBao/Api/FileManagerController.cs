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
        #region 资源分类


        /// <summary>
        /// 保存文件分类
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost, Authorize]
        public async Task<ApiResult> SaveFileClass([FromBody] FileSourceClassEntity model)
        {
            bool has = false;
            if (model.Id > 0)
                has = await db.Queryable<FileSourceClassEntity>().Where(a => a.Id != model.Id && a.Code == model.Code).AnyAsync();
            else
                has = await db.Queryable<FileSourceClassEntity>().Where(a => a.Code == model.Code).AnyAsync();

            if (has)
                return Error("添加失败,已存在相同编码的文件分类");

            return Result(await SaveAsync<FileSourceClassEntity>(model));
        }


        /// <summary>
        /// 获取文件分类列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> GetListFileClass([FromBody] ReqPage model)
        {
            var lst = await GetPage<FileSourceClassEntity>(model,
                new List<WhereIFs<FileSourceClassEntity>> {
                    new WhereIFs<FileSourceClassEntity>
                    {
                  expression =  a => a.Name.Contains(model.Key),
                                   where = !string.IsNullOrWhiteSpace(model.Key)
                     }
                });

            return Success(lst);
        }

        /// <summary>
        /// 获取前台展示文件分类列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> GetListFrontFileClass([FromBody] ReqPage model)
        {

            var notViewsClassCode = new List<string> { "001", "002" };

            var lst = await GetPage<FileSourceClassEntity>(model,
                new List<WhereIFs<FileSourceClassEntity>> {
                    new WhereIFs<FileSourceClassEntity>
                    {
                  expression =  a => a.Name.Contains(model.Key),
                                   where = !string.IsNullOrWhiteSpace(model.Key)
                     },
                    new WhereIFs<FileSourceClassEntity>
                    {
                         where = true,
                          expression = a=> !notViewsClassCode.Contains(a.Code)
                    }
                });

            return Success(lst);
        }


        /// <summary>
        /// 删除文件分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async  Task<ApiResult> DelFileClass(int id)
        {
          return Result(await DeleteAsync<FileSourceClassEntity>(id));
        }

        #endregion

        /// <summary>
        /// 资源列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> GetPage([FromBody] FileReqPage model)
        {
            var whereIfs = new List<WhereIFs<FileSourceEntity>>
        {
             new WhereIFs<FileSourceEntity>
             {
                  expression =  a => a.SourceType == model.SourceType,
                  where = model.SourceType != -1
             },
             new WhereIFs<FileSourceEntity>
             {
                  expression =  a => a.Name.Contains(model.Key),
                                   where = !string.IsNullOrWhiteSpace(model.Key)
             }
        };

            var pageInfo = await GetPage(model, whereIfs, a => a.Id, OrderByType.Desc,true);

            return Success(pageInfo);
        }

        ///// <summary>
        ///// 下载文件
        ///// </summary>
        ///// <param name="id">资源ID</param>
        ///// <returns></returns>
        //[HttpGet]
        //public async Task<FileStreamResult> DownFile(int id)
        //{
        //    var info = await db.Queryable<FileSourceEntity>().Where(a => a.Id == id).FirstAsync();
        //    if (info == null)
        //    {
        //        throw new Exception("资源文件不存在");
        //    }

        //    var stream = System.IO.File.OpenRead(info.Path);
        //    string fileExt = ".mp4";
        //    var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        //    var memi = provider.Mappings[fileExt];
        //    var fileName = Path.GetFileName(info.Path);
        //    return File(stream, memi, fileName);
        //}


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

            var localFilePath = Path.Combine(AppConfig.RootPath, info.Path);

            var stream = System.IO.File.OpenRead(localFilePath);
            var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
            var fileName = Path.GetFileName(info.Path);
            var fileExt = Path.GetExtension(info.Path)?.TrimStart('.');
            if (string.IsNullOrEmpty(fileExt) || !provider.TryGetContentType("." + fileExt, out var memi))
            {
                memi = "application/octet-stream"; // 默认的 MIME 类型
            }
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
            if (allowedFile.Sum(c => c.Length) > 1024 * 1024 * 500) return Error("视频过大");

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
                    ClassCode = "002",
                    CoverImage = dto.CoverImage,
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
                    ClassCode = "001",
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

        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ApiResult> UploadFile([FromForm] UploadFileDto dto)
        {

            if (dto.Files == null || !dto.Files.Any()) return Error("请选择上传的图片。");
            //格式限制
            //var allowType = new string[] { "image/jpg", "image/png", "image/jpeg" };
            //var allowedFile = dto.Files.Where(c => allowType.Contains(c.ContentType));
            //if (!allowedFile.Any()) return Error("图片格式错误");
            //if (allowedFile.Sum(c => c.Length) > 1024 * 1024 * 2) return Error("图片过大，请保持2M以下");

            string foldername = "files";
            string settingPath = "nfile";
            string folderpath = Path.Combine(settingPath, foldername);

            //本地文件夹路径
            string localPath = Path.Combine(AppConfig.RootPath, folderpath);
            if (!Directory.Exists(localPath))
            {
                Directory.CreateDirectory(localPath);
            }

            List<FileSourceEntity> lst = new();

            foreach (var file in dto.Files)
            {
                string strpath = Path.Combine(foldername, DateTime.Now.ToString("MMddHHmmss") + Path.GetFileName(file.FileName));
                var path = Path.Combine(settingPath, strpath);

                //本地文件路径
                var localFilePath = Path.Combine(AppConfig.RootPath, path);

                lst.Add(new FileSourceEntity
                {
                    ClassCode = dto.Foo,
                    CoverImage = dto.CoverImage,
                    Name = file.FileName,
                    Path = path,
                    SourceType = 2,
                    Ur = AppConfig.Settings.WebUrl + "/" + path.Replace("\\", "/"),
                });
                using (var stream = new FileStream(localFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    await file.CopyToAsync(stream);
                }
            }

            //保存
            var saveResult = await db.Insertable(lst).ExecuteCommandAsync();

            var excludeFiles = dto.Files.Except(dto.Files);

            if (excludeFiles.Any())
            {
                var infoMsg = $"{string.Join('、', excludeFiles.Select(c => c.FileName))} 格式错误";
                return Success(infoMsg);
            }
            return Success(lst);
        }


        /// <summary>
        ///  删除资源
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ApiResult> Del(int id)
        {
            var info = await GetAsync<FileSourceEntity>(a => a.Id == id);
            if (info == null) return SuccessMsg("删除成功");

            try
            {
                var localFilePath = Path.Combine(AppConfig.RootPath, info.Path);
                System.IO.File.Delete(localFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"文件删除失败,{ex.ToString()}");
            }
         

            return Result(await DeleteAsync<FileSourceEntity>(id));
        }

        public class UploadFileDto
        {
            //多文件
            [Required(ErrorMessage = "上传的文件不可为空")]
            public IFormFileCollection Files { get; set; }

            //单文件
            //public IFormFile File { get; set; }

            //其他数据
            public string Foo { get; set; } = string.Empty;

            /// <summary>
            /// 封面图
            /// </summary>
            public string CoverImage { get; set; } = string.Empty;

        }

    }
}
