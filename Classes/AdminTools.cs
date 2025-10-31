using System.ComponentModel;

namespace ScriptRunner
{
    public class AdminTools : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string executeTarget { get; set; }
        public string tool { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}