using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PictureEncoder_WPF
{
    public class Utils
    {
        public static readonly string[] supportExtensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp" };

        /// <summary>
        /// 过滤路径，返回存在的、后缀名是支持的路径，其实应该通过文件头来判断的
        /// </summary>
        /// <param name="paths">路径</param>
        /// <returns>返回过滤的路径</returns>
        public static (List<FileInfo> succeedFiles, List<KeyValuePair<string, FileLoadFailedReason>> failedFiles) FliterPaths(string[] paths)
        {
            var failedFiles = new List<KeyValuePair<string, FileLoadFailedReason>>();
            var result = new List<FileInfo>();
            foreach (var path in paths)
            {
                try
                {
                    var info = new FileInfo(path);
                    if (!info.Exists)
                    {
                        failedFiles.Add(new(path, FileLoadFailedReason.FileNotFound));
                        continue;
                    }
                    if (!supportExtensions.Contains(info.Extension.ToLower()))
                    {
                        failedFiles.Add(new(path, FileLoadFailedReason.ExtensionNotSupport));
                        continue;
                    }
                    result.Add(info);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法加载文件 {path}\n{ex.ToString()}");
                }
            }
            return (result, failedFiles);
        }

        /// <summary>
        /// 通过枚举值获取失败原因的字符串
        /// </summary>
        /// <param name="reason">导入失败的原因枚举</param>
        /// <returns>导入失败的字符串</returns>
        public static string GetImportFailedString(FileLoadFailedReason reason) => reason switch
        {
            FileLoadFailedReason.FileNotFound => "文件不存在",
            FileLoadFailedReason.ExtensionNotSupport => "不支持的格式",
            _ => "",
        };

        /// <summary>
        /// 通过枚举值获取操作结果的字符串
        /// </summary>
        /// <param name="result">操作结果的枚举</param>
        /// <returns>操作结果的字符串</returns>
        public static string GetWorkResultString(DoWorkResult result) => result switch
        {
            DoWorkResult.Succeed => "保存成功",
            DoWorkResult.UnknownImageFormat => "图片已损坏/不支持的图片格式",
            DoWorkResult.NoPremission => "权限不足，请使用管理员权限运行或更换图片目录",
            DoWorkResult.InputFileNotFound => "待加密图片不存在",
            DoWorkResult.WriteFailed => "保存时写入失败",
            DoWorkResult.Unknown => "未知错误",
            _ => "",
        };

        /// <summary>
        /// 打开一个选择文件夹对话框，返回选择的文件夹路径
        /// </summary>
        /// <returns>如果为null则点了取消，否则为用户选择的文件夹路径</returns>
        public static string? GetSaveFolderPath()
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "选择保存文件夹";
            dialog.Multiselect = false;
            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok) { return dialog.FileName; }
            else { return null; }
        }
    }
}
