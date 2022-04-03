using PictureEncoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
        WriteFailed,
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _working = false;

        private readonly BindingList<Work> works;
        public bool IsWorksEmpty => works.Count == 0;
        public bool Working
        {
            get => _working;
            set
            {
                if (value == _working) { return; }
                _working = value;
                Dispatcher.Invoke(value ? LockAllElements : UnlockAllElements);
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
                    return;
                }
                ImportTipLabel.Visibility = Visibility.Collapsed;
            };
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
            var resultSB = new System.Text.StringBuilder();
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

        // 删除任务
        private void FileList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (FileList.SelectedItems.Count == 0) { return; }
                var selectedItems = new Work[FileList.SelectedItems.Count];
                FileList.SelectedItems.CopyTo(selectedItems, 0);
                foreach (var item in selectedItems) { works.Remove(item); }
            }
        }

        private void EncodeButton_Click(object sender, RoutedEventArgs e)
        {
            var savePath = Utils.GetSaveFolderPath();
            if (savePath == null) { return; }
            EncodeAndSaveAsync(savePath);
        }

        private void DecodeButton_Click(object sender, RoutedEventArgs e)
        {
            LockAllElements();
            var savePath = Utils.GetSaveFolderPath();
            if (savePath == null)
            {
                UnlockAllElements();
                return;
            }
        }

        private async void EncodeAndSaveAsync(string savePath)
        {
            Working = true;
            // List效率不高，用Array
            var myWorks = new Work[works.Count];
            works.CopyTo(myWorks, 0);
            var tasks = new Task<DoWorkResult>[myWorks.Length];
            var password = PasswordField.Text;

            for (int i = 0; i < myWorks.Length; i++)
            {
                var work = myWorks[i];
                tasks[i] = Task.Run(async () =>
                {
                    var path = savePath + "/" + work.FileName + "_encoded.png";
                    var ms = await Encoder.Encode(password, work.Stream, work);
                    try
                    {
                        if (File.Exists(path)) { File.Delete(path); }
                        using var fs = File.Create(path);
                        ms.CopyTo(fs);
                        fs.Flush();
                    }
                    catch
                    {
                        work.Failed = true;
                        return DoWorkResult.WriteFailed;
                    }
                    return DoWorkResult.Succeed;
                });
            }

            await Task.Run(() =>
            {
                Task.WaitAll(tasks);
                var results = new Dictionary<DoWorkResult, List<Work>>();
                
                for (int i = 0; i < myWorks.Length; i++)
                {
                    if (!results.ContainsKey(tasks[i].Result)) { results[tasks[i].Result] = new List<Work>(); }
                    results[tasks[i].Result].Add(myWorks[i]);
                }

                var sb = new System.Text.StringBuilder();
                foreach (var kvp in results)
                {
                    sb.AppendLine(Utils.GetWorkResultString(kvp.Key));
                    foreach (var work in kvp.Value)
                    {
                        sb.Append(" - ");
                        sb.AppendLine(work.FileName);
                    }
                }
                MessageBox.Show(sb.ToString(), "加密完成");
            });
        }

        private void LockAllElements()
        {
            EncodeButton.IsEnabled = false;
            DecodeButton.IsEnabled = false;
            PasswordField.IsEnabled = false;
        }

        private void UnlockAllElements()
        {
            EncodeButton.IsEnabled = true;
            DecodeButton.IsEnabled = true;
            PasswordField.IsEnabled = true;
        }

        private void MergeInWorkList(IEnumerable<FileInfo> files)
        {
            var origin = works.ToArray();
            works.Clear();
            var newList = origin.Select(w => new FileInfo(w.FilePath))
                .Union(files)
                .Distinct((a, b) => a.Name == b.Name)
                .Select(i => new Work(i.Name, i.FullName));
            foreach (var element in newList)
            {
                works.Add(element);
            }
        }
    }
}
