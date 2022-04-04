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
        private bool _succeed;

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
        public bool Succeed
        {
            get => _succeed;
            set
            {
                _succeed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Succeed)));
            }
        }

        public Work(string fileName, string filePath)
        {
            _fileName = fileName;
            _filePath = filePath;
            _progress = 0d;
            _succeed = true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Report(double value) => Progress = value * 100;

        public void Reset()
        {
            Progress = 0f;
            Succeed = true;
        }
    }
}
