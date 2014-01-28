using K4W2Accuracy.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace K4W2Accuracy.Infrastructure
{
    public enum KinectState
    {
        Initializing,
        Running,
        ShuttingDown,
        Shutdown,
        Restarting
    }

    public class KinectStatusMessage
    {
        public KinectStatusMessage()
        {
            Message = "";
        }

        public KinectStatusMessage(string msg, KinectState state)
        {
            Message = msg;
            State = state;
        }

        public KinectStatusMessage(string msg, KinectState state, KinectHelper helper)
        {
            Message = msg;
            State = state;
            Helper = helper;
        }

        public string Message { get; set; }

        public KinectState State { get; set; }

        public KinectHelper Helper { get; set; }
    }
}
