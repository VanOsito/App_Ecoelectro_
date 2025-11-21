using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Models
{
    public class DetectionWithComponents
    {
        public DetectionInfo Detection { get; set; } = default!;
        public ObservableCollection<ComponentInfo> Components { get; set; } = new ObservableCollection<ComponentInfo>();
    }
}
