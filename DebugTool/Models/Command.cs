using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugTool.Models
{
    public class Command : INotifyPropertyChanged
    {
        private string name;
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private string method;
        public string Method
        {
            get => method;
            set
            {
                if (method != value)
                {
                    method = value;
                    OnPropertyChanged(nameof(Method));
                }
            }
        }
        public Dictionary<string, object> Parameters { get; set; }
        public Command(string name)
        {
            Name = name;
            Method = "";
            Parameters = new Dictionary<string, object>();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
