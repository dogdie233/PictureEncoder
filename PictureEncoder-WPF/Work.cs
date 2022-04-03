using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PictureEncoder_WPF
{
    public class Work : IProgress<double>, INotifyPropertyChanged
    {
        private string _fileName;
        private string _filePath;
        private double _progress;
        private bool _failed;
        private FileStream _stream;

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FileName)));
            }
        }
        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FilePath)));
            }
        }
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Progress)));
            }
        }
        public bool Failed
        {
            get => _failed;
            set
            {
                _failed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Failed)));
            }
        }
        public FileStream Stream => _stream;

        public Work(string fileName, string filePath)
        {
            _fileName = fileName;
            _filePath = filePath;
            _progress = 0d;
            _failed = false;
            _stream = File.OpenRead(filePath);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Report(double value) => Progress = value * 100;
    }
}
