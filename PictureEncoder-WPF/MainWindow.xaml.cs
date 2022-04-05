using PictureEncoder;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PictureEncoder_WPF
{
    public enum FileLoadFailedReason
    {
        FileNotFound,
        ExtensionNotSupport,
    }

    public enum DoWorkResult
    {
        Succeed,
        UnknownImageFormat,
        NoPremission,
        InputFileNotFound,
        WriteFailed,
        Unknown,
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _working = false;

        private readonly BindingList<Work> works;
        public bool Working
        {
            get => _working;
            set
            {
                if (value == _working) { return; }
                _working = value;
                Dispatcher.Invoke(value ? LockAllComponents : UnlockAllComponents);
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            works = new();
            FileList.ItemsSource = works;

            // 提示文字的出现与消失
            works.ListChanged += (_, e) =>
            {
                if (Working) { return; }
                if (works.Count == 0)
                {
                    ImportTipLabel.Visibility = Visibility.Visible;
                    LockAllComponents();
                    return;
                }
                ImportTipLabel.Visibility = Visibility.Collapsed;
                UnlockAllComponents();
            };

            Working = false;
        }

        #region File Drop Process
        private void FileList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Link;
                return;
            }
            e.Effects = DragDropEffects.None;
        }

        private void FileList_Drop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            (var succeed, var failedPaths) = Utils.FliterPaths(files);

            // 输出结果
            var resultSB = new StringBuilder();
            resultSB.Append("成功导入 ");
            resultSB.Append(succeed.Count);
            resultSB.AppendLine(" 张图片");
            if (failedPaths.Count != 0)
            {
                resultSB.AppendLine("导入失败:");
                foreach (var kvp in failedPaths)
                {
                    resultSB.Append(" - ");
                    resultSB.Append(kvp.Key);
                    resultSB.Append(": ");
                    resultSB.AppendLine(Utils.GetImportFailedString(kvp.Value));
                }
            }
            MessageBox.Show(resultSB.ToString(), "导入结果", MessageBoxButton.OK, failedPaths.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

            MergeInWorkList(succeed);
        }
        #endregion

        #region Delete Works
        private void FileList_KeyUp(object sender, KeyEventArgs e)
        {
            if (Working || e.Key != Key.Delete) { return; }
            DeleteSelectedItems();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (Working) { return; }
            DeleteSelectedItems();
        }

        private void DeleteSelectedItems()
        {
            if (FileList.SelectedItems.Count == 0) { return; }
            var selectedItems = new Work[FileList.SelectedItems.Count];
            FileList.SelectedItems.CopyTo(selectedItems, 0);
            foreach (var item in selectedItems)
            {
                works.Remove(item);
            }
        }
        #endregion

        #region Buttons Process
        private void EncodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Working) { return; }
            if (string.IsNullOrEmpty(PasswordField.Text))
            {
                var tipResult = MessageBox.Show("当前密码为空，这会使你的加密结果不安全，是否继续", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (tipResult != MessageBoxResult.Yes) { return; }
            }
            var savePath = Utils.GetSaveFolderPath();
            if (savePath == null) { return; }
            OperateAndSaveAsync(savePath, false);
        }

        private void DecodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Working) { return; }
            if (string.IsNullOrEmpty(PasswordField.Text))
            {
                var tipResult = MessageBox.Show("当前密码为空，这大概率会得到错误的结果，是否继续", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (tipResult != MessageBoxResult.Yes) { return; }
            }
            var savePath = Utils.GetSaveFolderPath();
            if (savePath == null) { return; }
            OperateAndSaveAsync(savePath, true);
        }
        #endregion

        private async void OperateAndSaveAsync(string savePath, bool decode)
        {
            // 格式化保存位置
            savePath = savePath.Replace('\\', '/');
            savePath = savePath.EndsWith('/') ? savePath : savePath + '/';

            Working = true;
            // List效率不高，用Array
            var myWorks = new Work[works.Count];
            works.CopyTo(myWorks, 0);
            var tasks = new Task<DoWorkResult>[myWorks.Length];
            var password = PasswordField.Text;

            for (int i = 0; i < myWorks.Length; i++)
            {
                var work = myWorks[i];
                work.Reset();  // 重置一下显示
                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        // 打开文件流
                        using var inputStream = File.OpenRead(work.FilePath);

                        // 准备变量
                        var path = savePath + work.FileName.Substring(0, work.FileName.LastIndexOf('.')) + (decode ? "_decoded.png" : "_encoded.png");

                        // 执行操作
                        using var ms = await (decode ? PicEncoder.Decode(password, inputStream, work) : PicEncoder.Encode(password, inputStream, work));

                        // 防止在加密过程中删掉了输出文件夹
                        if (!Directory.Exists(savePath)) { Directory.CreateDirectory(savePath); }

                        // 替换
                        if (File.Exists(path)) { File.Delete(path); }
                        using var fs = File.Create(path);

                        // 写入
                        ms.CopyTo(fs);
                    }
                    catch (UnknownImageFormatException)
                    {
                        work.Succeed = false;
                        return DoWorkResult.UnknownImageFormat;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        work.Succeed = false;
                        return DoWorkResult.NoPremission;
                    }
                    catch (FileNotFoundException)
                    {
                        work.Succeed = false;
                        return DoWorkResult.InputFileNotFound;
                    }
                    catch (IOException)
                    {
                        work.Succeed = false;
                        return DoWorkResult.WriteFailed;
                    }
                    catch (Exception)
                    {
                        work.Succeed = false;
#if DEBUG
                        throw;  // 抛出让我ｄｅｂｕｇ罢
#else
                        return DoWorkResult.Unknown;
#endif
                    }
                    return DoWorkResult.Succeed;
                });
            }

            await Task.Run(() =>
            {
                // 等待所有任务完成
                Task.WaitAll(tasks);
                var results = new Dictionary<DoWorkResult, List<Work>>();
                
                for (int i = 0; i < myWorks.Length; i++)
                {
                    if (!results.ContainsKey(tasks[i].Result)) { results[tasks[i].Result] = new List<Work>(); }
                    results[tasks[i].Result].Add(myWorks[i]);
                }

                // 打印结果
                var sb = new StringBuilder();
                foreach (var kvp in results)
                {
                    sb.AppendLine(Utils.GetWorkResultString(kvp.Key));
                    foreach (var work in kvp.Value)
                    {
                        sb.Append(" - ");
                        sb.AppendLine(work.FileName);
                    }
                }
                MessageBox.Show(sb.ToString(), decode ? "解密完成" : "加密完成");
                Working = false;
            });
        }

        private void PasswordField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(PasswordField.Text))
            {
                PasswordFieldTip.Visibility = Visibility.Visible;
                return;
            }
            PasswordFieldTip.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 锁定下面的3个组件
        /// </summary>
        private void LockAllComponents()
        {
            EncodeButton.IsEnabled = false;
            DecodeButton.IsEnabled = false;
            PasswordField.IsEnabled = false;
        }

        /// <summary>
        /// 解锁下面的3个组件
        /// </summary>
        private void UnlockAllComponents()
        {
            EncodeButton.IsEnabled = true;
            DecodeButton.IsEnabled = true;
            PasswordField.IsEnabled = true;
        }
        
        /// <summary>
        /// 将新的文件与已经导入的文件合并
        /// </summary>
        /// <param name="files">待添加的图片</param>
        private void MergeInWorkList(IEnumerable<FileInfo> files)
        {
            var origin = works.ToArray();
            works.Clear();
            var newList = origin.Select(w => new FileInfo(w.FilePath))
                .Union(files)
                .Distinct(i => i.Name)
                .Select(i => new Work(i.Name, i.FullName));
            foreach (var element in newList)
            {
                works.Add(element);
            }
        }
    }
}
