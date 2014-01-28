using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace K4W2Accuracy.Model
{
    /// <summary>
    /// Provides the interface for interacting with a Kinect sensor
    /// </summary>
    public interface IKinectHelper
    {
        /// <summary>
        /// Begins the Kinect sensor initialization process. Helper properties should be
        /// set before calling this function.
        /// </summary>
        void StartHelper();

        /// <summary>
        /// Shuts down the active Kinect sensor and then restarts it, if there is an active
        /// sensor. This should be used to reinitialize the system after any settings have
        /// changed, such as the flipping of a stream activation flag or the changing of
        /// a stream format property.
        /// </summary>
        void RestartHelper();

        /// <summary>
        /// Shuts down all of the helper processes and turns off any Kinect sensors which
        /// my have been initialized.
        /// </summary>
        void ShutdownHelper();
        
        /// <summary>
        /// Shuts down a kinect sensor and each of its enabled data streams
        /// </summary>
        /// <p name="kinect">the Kinect sensor to shutdown</p>
        void StopKinect(bool isRestarting = false);

        /// <summary>
        /// Initializes a kinect sensor
        /// </summary>
        /// <p name="kinect">The sensor to CandidateStart up</p>
        void StartKinect();
    }
}
