using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GistModulesLib
{
    [InheritedExport(typeof(IKinectBodySubscriber))]
    public interface IKinectBodySubscriber
    {
        void BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e);

        int BodyCount { get; set; }
    }
}