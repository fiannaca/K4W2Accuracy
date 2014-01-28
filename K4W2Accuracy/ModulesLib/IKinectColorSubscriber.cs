using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace GistModulesLib
{
    [InheritedExport(typeof(IKinectColorSubscriber))]
    public interface IKinectColorSubscriber
    {
        void ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e);

        FrameDescription ColorFrameDesc { get; set; }
    }
}
