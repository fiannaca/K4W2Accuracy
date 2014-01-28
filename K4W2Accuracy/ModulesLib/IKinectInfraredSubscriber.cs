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
    [InheritedExport(typeof(IKinectInfraredSubscriber))]
    public interface IKinectInfraredSubscriber
    {
        void InfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e);

        FrameDescription IrFrameDesc { get; set; }
    }
}
