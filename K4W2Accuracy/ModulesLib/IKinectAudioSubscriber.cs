using GistModulesLib;
using Microsoft.Kinect;
using System.ComponentModel.Composition;

namespace GistModulesLib
{
    [InheritedExport(typeof(IKinectAudioSubscriber))]
    public interface IKinectAudioSubscriber
    {
        //K4W v1: v2 uses Source/Reader pattern. Audio not yet implemented in K4W2 SDK
        //void AudioSourceReady(KinectAudioSource AudioSource);
    }
}
