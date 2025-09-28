using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrayApp.Core;

namespace TrayApp.FileOperations
{
    /// <summary>
    /// 时间戳文件操作器，实现IFileOperator接口
    /// 支持将文件移动到时间戳命名的目录
    /// </summary>
    public class TimestampFileOperator : IFileOperator
    {
        private readonly ILogger _logger;

        /// <summary>
        /// 初始化TimestampFileOperator实例
        /// </summary>
        /// <param name="logger">日志服务</param>
        public TimestampFileOperator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 将文件移动到时间戳命名的子目录
        /// </summary>
        /// <param name="filePaths">文件路径列表</param>
        /// <param name="baseDirectory">基础目录（时间戳子目录将创建在此目录下）</param>
        /// <returns>创建的时间戳目录路径</returns>
        public string MoveFilesToTimestampDirectory(IEnumerable<string> filePaths, string baseDirectory)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));
            if (string.IsNullOrWhiteSpace(baseDirectory)) throw new ArgumentException("基础目录不能为空", nameof(baseDirectory));

            var files = filePaths.ToList();

            try
            {
                // 创建时间戳目录（格式：yyyyMMddHHmmss）
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string targetDirectory = Path.Combine(baseDirectory, timestamp);
                
                // 创建目录（如果不存在）
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                    _logger.Info($"已创建时间戳目录: {targetDirectory}");
                }

                if (files.Count == 0)
                {
                    _logger.Info("没有文件需要移动，但已创建时间戳目录");
                    return targetDirectory;
                }

                // 移动所有文件
                int successCount = 0;
                foreach (var filePath in files)
                {
                    try
                    {
                        if (!File.Exists(filePath))
                        {
                            _logger.Warning($"文件不存在，跳过移动: {filePath}");
                            continue;
                        }

                        string fileName = Path.GetFileName(filePath);
                        string targetPath = Path.Combine(targetDirectory, fileName);

                        // 如果目标文件已存在，添加计数器
                        if (File.Exists(targetPath))
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                            string extension = Path.GetExtension(fileName);
                            int counter = 1;
                            
                            while (File.Exists(targetPath))
                            {
                                targetPath = Path.Combine(targetDirectory, $"{fileNameWithoutExt}_{counter}{extension}");
                                counter++;
                            }
                            
                            _logger.Info($"文件已存在，重命名为: {Path.GetFileName(targetPath)}");
                        }

                        // 移动文件
                        File.Move(filePath, targetPath);
                        successCount++;
                        _logger.Info($"文件已移动: {filePath} -> {targetPath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"移动文件失败: {filePath}", ex);
                    }
                }

                _logger.Info($"文件移动完成: {successCount}/{files.Count} 个文件成功移动");
                return targetDirectory;
            }
            catch (Exception ex)
            {
                _logger.Error("创建时间戳目录失败", ex);
                throw;
            }
        }
    }
}