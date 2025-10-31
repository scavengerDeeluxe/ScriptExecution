using System.Collections.ObjectModel;
using System.ComponentModel;


namespace ScriptRunner
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private string _fullPath;
        public string FullPath
        {
            get { return _fullPath; }
            set
            {
                _fullPath = value;
                OnPropertyChanged(nameof(FullPath));
            }
        }

        private bool _isDirectory;
        public bool IsDirectory
        {
            get { return _isDirectory; }
            set
            {
                _isDirectory = value;
                OnPropertyChanged(nameof(IsDirectory));
            }
        }

        private int _rating;
        public int Rating
        {
            get { return _rating; }
            set
            {
                _rating = value;
                OnPropertyChanged(nameof(Rating));
            }
        }

        // You could add other properties as needed, like:
        // private long _size;
        // public long Size { get { return _size; } set { _size = value; OnPropertyChanged(nameof(Size)); } }

        // This is optional but good practice if you want to represent child items (folders)
        public ObservableCollection<FileSystemItem> Children { get; set; } = new ObservableCollection<FileSystemItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
