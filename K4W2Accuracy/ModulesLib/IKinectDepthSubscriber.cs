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
    [InheritedExport(typeof(IKinectDepthSubscriber))]
    public interface IKinectDepthSubscriber
    {
        void DepthFrameArrived(object sender, DepthFrameArrivedEventArgs e);

        FrameDescription DepthFrameDesc { get; set; }
    }
}
