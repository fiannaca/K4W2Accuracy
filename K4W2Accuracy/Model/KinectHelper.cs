using GalaSoft.MvvmLight.Messaging;
using GistModulesLib;
using K4W2Accuracy.Infrastructure;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace K4W2Accuracy.Model
{
    /// <summary>
    /// This class is responsible for managing the Kinect hardware
    /// </summary>
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(IKinectHelper))]
    public class KinectHelper : IKinectHelper
    {
        [ImportingConstructor]
        public KinectHelper()
        {
            Status = HelperStatus.Uninitialized;
        }

        /// <summary>
        /// Indicates if the Infrared source is needed
        /// </summary>
        public bool UseInfraredSource { get; set; }

        /// <summary>
        /// Indicates if the Color source is needed
        /// </summary>
        public bool UseColorSource { get; set; }

        /// <summary>
        /// Indicates if the Depth source is needed
        /// </summary>
        public bool UseDepthSource { get; set; }

        /// <summary>
        /// Indicates if the BodyIndex source is needed
        /// </summary>
        public bool UseBodyIndexSource { get; set; }

        /// <summary>
        /// Indicates if the Body source is needed
        /// </summary>
        public bool UseBodySource { get; set; }

        /// <summary>
        /// Indicates if the Audio source is needed
        /// </summary>
        public bool UseAudioStream { get; set; }

        #region SUBSCRIBERS

        /// <summary>
        /// List of all modules which implement the IKinectInfraredSubscriber interface.
        /// </summary>
        [ImportMany]
        IEnumerable<IKinectInfraredSubscriber> InfraredSubscribers;

        /// <summary>
        /// List of all modules which implement the IKinectColorSubscriber interface.
        /// </summary>
        [ImportMany]
        IEnumerable<IKinectColorSubscriber> ColorSubscribers;

        /// <summary>
        /// List of all modules which implement the IKinectDepthSubscriber interface.
        /// </summary>
        [ImportMany]
        IEnumerable<IKinectDepthSubscriber> DepthSubscribers;

        /// <summary>
        /// List of all modules which implement the IKinectBodyIndexSubscriber interface.
        /// </summary>
        [ImportMany]
        IEnumerable<IKinectBodyIndexSubscriber> BodyIndexSubscribers;

        /// <summary>
        /// List of all modules which implement the IKinectBodySubscriber interface.
        /// </summary>
        [ImportMany]
        IEnumerable<IKinectBodySubscriber> BodySubscribers;

        /// <summary>
        /// List of all modules which implement the IKinectAudioSubscriber interface.
        /// </summary>
        [ImportMany]
        IEnumerable<IKinectAudioSubscriber> AudioSubscribers;

        #endregion

        //
        // Since the K4W v2 API uses the Source/Readers pattern, the updated verison of 
        // KinectHelper will manage the readers for all subscribers
        //
        #region READERS

        /// <summary>
        /// Set of Readers for all InfraredSubscribers
        /// </summary>
        Dictionary<int, InfraredFrameReader> InfraredReaders;

        /// <summary>
        /// Set of Readers for all ColorSubscribers
        /// </summary>
        Dictionary<int, ColorFrameReader> ColorReaders;

        /// <summary>
        /// Set of Readers for all DepthSubscribers
        /// </summary>
        Dictionary<int, DepthFrameReader> DepthReaders;

        /// <summary>
        /// Set of Readers for all BodyIndexSubscribers
        /// </summary>
        Dictionary<int, BodyIndexFrameReader> BodyIndexReaders;

        /// <summary>
        /// Set of Readers for all BodySubscribers
        /// </summary>
        Dictionary<int, BodyFrameReader> BodyReaders;

        //K4W v2: Audio isn't implemented yet
        //Dictionary<int, AudioBeamFrameReader> AudioReaders;
        #endregion

        #region PRIMARY KINECT OBJECTS

        public enum HelperStatus
        {
            Uninitialized,
            Started,
            Shutdown
        }

        /// <summary>
        /// Inidates the status of the KinectHelper object
        /// </summary>
        public HelperStatus Status { get; private set; }

        /// <summary>
        /// Gets a reference to the Kinect sensor
        /// </summary>
        public KinectSensor Kinect { get; private set; }

        /// <summary>
        /// Gets a reference to the coordinate mapper object. Allows for mapping between color, 
        /// depth, and body spaces
        /// </summary>
        [Export(typeof(CoordinateMapper))]
        public CoordinateMapper PointMapper
        {
            get
            {
                if (Kinect == null)
                    return null;

                return Kinect.CoordinateMapper;
            }
        }
        #endregion

        #region IKinectHelper METHOD IMPLEMENTATIONS

        public void StartHelper()
        {
            if (Status == HelperStatus.Uninitialized)
            {
                Messenger.Default.Send<KinectStatusMessage>(new KinectStatusMessage("Please wait while the Kinect initializes...", KinectState.Initializing));
            
                //Determine which sources are subscribed to
                UseInfraredSource = InfraredSubscribers.Count() > 0;
                UseColorSource = ColorSubscribers.Count() > 0;
                UseDepthSource = DepthSubscribers.Count() > 0;
                UseBodyIndexSource = BodyIndexSubscribers.Count() > 0;
                UseBodySource = BodySubscribers.Count() > 0;
                UseAudioStream = AudioSubscribers.Count() > 0;
                
                //Find the Kinect and CandidateStart it
                this.Kinect = KinectSensor.Default;

                StartKinect();

                KinectDisplayHelper.Init(this);

                Messenger.Default.Send<KinectStatusMessage>(
                    new KinectStatusMessage("The Kinect is now running.", KinectState.Running, this)
                );
                
                Status = HelperStatus.Started;
            }
            else
            {
                throw new Exception("StartHelper can only be called once at program initialization. To restart the helper object, call RestartHelper!");
            }
        }

        public void RestartHelper()
        {
            if (Status == HelperStatus.Started)
            {
                //Ensure the system isn't paused before restarting
                IsPaused = false;

                StopKinect(true);

                //If you want to do a more complex restart, you could update the composition
                // and redetect which streams are needed here

                StartKinect();
            }
            else
            {
                throw new Exception("The helper can only be restated when it is currently running. StartHelper should be called for the first time running the helper and if the helper has already been shutdown, it should be disposed and a new helper object should be created!");
            }
        }

        public void ShutdownHelper()
        {
            if (Status == HelperStatus.Started)
            {
                StopKinect();
            }
            else
            {
                throw new Exception("The helper was either never initialized or has already been shutdown!");
            }
        }
        
        public void StartKinect()
        {
            this.Kinect.Open();
            
            //Start the Infrared readers
            if(UseInfraredSource)
            {
                StartInfraredReaders();
            }

            //Start the Color readers
            if (UseColorSource)
            {
                StartColorReaders();
            }

            //Start the Depth readers
            if (UseDepthSource)
            {
                StartDepthReaders();
            }

            //Start the BodyIndex readers
            if(UseBodyIndexSource)
            {
                StartBodyIndexReaders();
            }

            //Start the Body readers
            if (UseBodySource)
            {
                StartBodyReaders();
            }

            //The kinect sensor has to be started before the audio source can be accessed
            if (UseAudioStream)
            {
                StartAudioReaders();
            }
        }

        public void StopKinect(bool isRestarting = false)
        {
            if (isRestarting)
                Messenger.Default.Send<KinectStatusMessage>(new KinectStatusMessage("The Kinect is now restarting.", KinectState.Restarting));
            else
                Messenger.Default.Send<KinectStatusMessage>(new KinectStatusMessage("The Kinect is now shutting down.", KinectState.ShuttingDown));

            //Start the Infrared readers
            if (UseInfraredSource)
            {
                StopInfraredReaders();
            }

            //Stop the Color readers
            if (UseColorSource)
            {
                StopColorReaders();
            }

            //Stop the Depth readers
            if (UseDepthSource)
            {
                StopDepthReaders();
            }

            //Start the BodyIndex Readers
            if (UseBodyIndexSource)
            {
                StopBodyIndexReaders();
            }

            //Stop the Body readers
            if (UseBodySource)
            {
                StopBodyReaders();
            }

            //Stop the Audio readers
            if (UseAudioStream)
            {
                StopAudioReaders();
            }

            //Stop the sensor
            this.Kinect.Close();

            if (!isRestarting)
                Messenger.Default.Send<KinectStatusMessage>(new KinectStatusMessage("The Kinect is now shut down.", KinectState.Shutdown));
        }

        public void StartInfraredReaders()
        {
            InfraredReaders = new Dictionary<int, InfraredFrameReader>();

            foreach(IKinectInfraredSubscriber s in InfraredSubscribers)
            {
                InfraredReaders.Add(
                    ((ModuleBase)s).SubscriberID,
                    this.Kinect.InfraredFrameSource.OpenReader()
                );

                InfraredReaders[((ModuleBase)s).SubscriberID].FrameArrived += s.InfraredFrameArrived;
                s.IrFrameDesc = this.Kinect.InfraredFrameSource.FrameDescription;

                if(!((ModuleBase)s).IsActive)
                {
                    InfraredReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                }
            }
        }

        public void StopInfraredReaders()
        {
            foreach(IKinectInfraredSubscriber s in InfraredSubscribers)
            {
                InfraredReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                InfraredReaders[((ModuleBase)s).SubscriberID].Dispose();
            }

            InfraredReaders = null;
        }

        public void StartColorReaders()
        {
            ColorReaders = new Dictionary<int, ColorFrameReader>();

            foreach(IKinectColorSubscriber s in ColorSubscribers)
            {
                ColorReaders.Add(
                    ((ModuleBase)s).SubscriberID,
                    this.Kinect.ColorFrameSource.OpenReader()
                );

                ColorReaders[((ModuleBase)s).SubscriberID].FrameArrived += s.ColorFrameArrived;
                s.ColorFrameDesc = this.Kinect.ColorFrameSource.FrameDescription;

                if (!((ModuleBase)s).IsActive)
                {
                    ColorReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                }
            }
        }

        public void StopColorReaders()
        {
            foreach(IKinectColorSubscriber s in ColorSubscribers)
            {
                ColorReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                ColorReaders[((ModuleBase)s).SubscriberID].Dispose();
            }

            ColorReaders = null;
        }
        
        public void StartDepthReaders()
        {
            DepthReaders = new Dictionary<int, DepthFrameReader>();

            foreach(IKinectDepthSubscriber s in DepthSubscribers)
            {
                DepthReaders.Add(
                    ((ModuleBase)s).SubscriberID,
                    this.Kinect.DepthFrameSource.OpenReader()
                );

                DepthReaders[((ModuleBase)s).SubscriberID].FrameArrived += s.DepthFrameArrived;
                s.DepthFrameDesc = this.Kinect.DepthFrameSource.FrameDescription;

                if (!((ModuleBase)s).IsActive)
                {
                    DepthReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                }
            }
        }

        public void StopDepthReaders()
        {
            foreach(IKinectDepthSubscriber s in DepthSubscribers)
            {
                DepthReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                DepthReaders[((ModuleBase)s).SubscriberID].Dispose();
            }

            DepthReaders = null;
        }

        public void StartBodyIndexReaders()
        {
            BodyIndexReaders = new Dictionary<int, BodyIndexFrameReader>();

            foreach(IKinectBodyIndexSubscriber s in BodyIndexSubscribers)
            {
                BodyIndexReaders.Add(
                    ((ModuleBase)s).SubscriberID,
                    this.Kinect.BodyIndexFrameSource.OpenReader()
                );

                BodyIndexReaders[((ModuleBase)s).SubscriberID].FrameArrived += s.BodyIndexFrameArrived;
                s.BodyIndexFrameDesc = this.Kinect.BodyIndexFrameSource.FrameDescription;

                if (!((ModuleBase)s).IsActive)
                {
                    BodyIndexReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                }
            }
        }

        public void StopBodyIndexReaders()
        {
            foreach(IKinectBodyIndexSubscriber s in BodyIndexSubscribers)
            {
                BodyIndexReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                BodyIndexReaders[((ModuleBase)s).SubscriberID].Dispose();
            }

            BodyIndexReaders = null;
        }

        public void StartBodyReaders()
        {
            BodyReaders = new Dictionary<int, BodyFrameReader>();

            foreach(IKinectBodySubscriber s in BodySubscribers)
            {
                BodyReaders.Add(
                    ((ModuleBase)s).SubscriberID,
                    this.Kinect.BodyFrameSource.OpenReader()
                );

                BodyReaders[((ModuleBase)s).SubscriberID].FrameArrived += s.BodyFrameArrived;
                s.BodyCount = this.Kinect.BodyFrameSource.BodyCount;

                if (!((ModuleBase)s).IsActive)
                {
                    BodyReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                }
            }
        }

        public void StopBodyReaders()
        {
            foreach(IKinectBodySubscriber s in BodySubscribers)
            {
                BodyReaders[((ModuleBase)s).SubscriberID].IsPaused = true;
                BodyReaders[((ModuleBase)s).SubscriberID].Dispose();
            }

            BodyReaders = null;
        }

        public void StartAudioReaders()
        {
            //K4W vs: Audio isn't implemented yet
        }

        public void StopAudioReaders()
        {
            //K4W vs: Audio isn't implemented yet
        }

        //K4W v1: There is no longer a motor controlling elevation in the new kinect
        //public void AdjustElevationAngle(int angle)
        //{
        //    if (Kinect != null)
        //    {
        //        //Cull the angle within the accepted limitations of the kinect hardware
        //        if (angle > Kinect.MaxElevationAngle)
        //        {
        //            angle = Kinect.MaxElevationAngle;
        //        }
        //        else if (angle < Kinect.MinElevationAngle)
        //        {
        //            angle = Kinect.MinElevationAngle;
        //        }

        //        try
        //        {
        //            Kinect.ElevationAngle = angle;
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.TargetSite + " - " + ex.Message);
        //        }
        //    }
        //}

        /// <summary>
        /// Pauses a specific reader (or all readers if SubscriberType is null) for a specific Subscriber
        /// </summary>
        /// <p name="SubscriberID">ID of the Subscriber</p>
        /// <p name="SubscriberType">Type of subscriber for the specific reader to stop</p>
        public void Pause(int SubscriberID, Type SubscriberType = null)
        {
            if(SubscriberType == null)
            {
                if (UseInfraredSource && InfraredReaders.ContainsKey(SubscriberID))
                    InfraredReaders[SubscriberID].IsPaused = true;

                if (UseColorSource && ColorReaders.ContainsKey(SubscriberID))
                    ColorReaders[SubscriberID].IsPaused = true;

                if (UseDepthSource && DepthReaders.ContainsKey(SubscriberID))
                    DepthReaders[SubscriberID].IsPaused = true;

                if (UseBodyIndexSource && BodyIndexReaders.ContainsKey(SubscriberID))
                    BodyIndexReaders[SubscriberID].IsPaused = true;

                if (UseBodySource && BodyReaders.ContainsKey(SubscriberID))
                    BodyReaders[SubscriberID].IsPaused = true;

                //K4W v2: Audio isn't implemented yet
            }
            else
            {
                if (SubscriberType == typeof(IKinectInfraredSubscriber) && InfraredReaders.ContainsKey(SubscriberID))
                    InfraredReaders[SubscriberID].IsPaused = true;

                if (SubscriberType == typeof(IKinectColorSubscriber) && ColorReaders.ContainsKey(SubscriberID))
                    ColorReaders[SubscriberID].IsPaused = true;

                if (SubscriberType == typeof(IKinectDepthSubscriber) && DepthReaders.ContainsKey(SubscriberID))
                    DepthReaders[SubscriberID].IsPaused = true;

                if (SubscriberType == typeof(IKinectBodyIndexSubscriber) && BodyIndexReaders.ContainsKey(SubscriberID))
                    BodyIndexReaders[SubscriberID].IsPaused = true;

                if (SubscriberType == typeof(IKinectBodySubscriber) && BodyReaders.ContainsKey(SubscriberID))
                    BodyReaders[SubscriberID].IsPaused = true;

                //K4W v2: Audio isn't implemented yet
            }
        }

        /// <summary>
        /// Pauses all readers for all subscribers for the current Kinect sensor
        /// </summary>
        private void Pause()
        {
            //Pause the infrared readers
            if(UseInfraredSource)
            {
                foreach(InfraredFrameReader r in InfraredReaders.Values)
                {
                    r.IsPaused = true;
                }
            }

            //Pause the color readers
            if (UseColorSource)
            {
                foreach(ColorFrameReader r in ColorReaders.Values)
                {
                    r.IsPaused = true;
                }
            }

            //Pause the depth readers
            if (UseDepthSource)
            {
                foreach(DepthFrameReader r in DepthReaders.Values)
                {
                    r.IsPaused = true;
                }
            }

            //Pause the bodyindex readers
            if(UseBodyIndexSource)
            {
                foreach(BodyIndexFrameReader r in BodyIndexReaders.Values)
                {
                    r.IsPaused = true;
                }
            }

            //Pause the body readers
            if (UseBodySource)
            {
                foreach(BodyFrameReader r in BodyReaders.Values)
                {
                    r.IsPaused = true;
                }
            }

            //Pause the audio readers
            if(UseAudioStream)
            {
                //K4W v2: Audio isn't implemented yet
                //foreach(AudioBeamFrameReader r in AudioReaders.Values)
                //{
                //    r.IsPaused = true;
                //}
            }
        }

        /// <summary>
        /// Continues a specific reader (or all readers if SubscriberType is null) for a specific Subscriber
        /// </summary>
        /// <p name="SubscriberID">ID of the Subscriber</p>
        /// <p name="SubscriberType">Type of subscriber for the specific reader to restart</p>
        public void Continue(int SubscriberID, Type SubscriberType = null)
        {
            if (SubscriberType == null)
            {
                if (UseInfraredSource && InfraredReaders.ContainsKey(SubscriberID))
                    InfraredReaders[SubscriberID].IsPaused = false;

                if (UseColorSource && ColorReaders.ContainsKey(SubscriberID))
                    ColorReaders[SubscriberID].IsPaused = false;

                if (UseDepthSource && DepthReaders.ContainsKey(SubscriberID))
                    DepthReaders[SubscriberID].IsPaused = false;

                if (UseBodyIndexSource && BodyIndexReaders.ContainsKey(SubscriberID))
                    BodyIndexReaders[SubscriberID].IsPaused = false;

                if (UseBodySource && BodyReaders.ContainsKey(SubscriberID))
                    BodyReaders[SubscriberID].IsPaused = false;

                //K4W v2: Audio isn't implemented yet
            }
            else
            {
                if (SubscriberType == typeof(IKinectInfraredSubscriber) && InfraredReaders.ContainsKey(SubscriberID))
                    InfraredReaders[SubscriberID].IsPaused = false;

                if (SubscriberType == typeof(IKinectColorSubscriber) && ColorReaders.ContainsKey(SubscriberID))
                    ColorReaders[SubscriberID].IsPaused = false;

                if (SubscriberType == typeof(IKinectDepthSubscriber) && DepthReaders.ContainsKey(SubscriberID))
                    DepthReaders[SubscriberID].IsPaused = false;

                if (SubscriberType == typeof(IKinectBodyIndexSubscriber) && BodyIndexReaders.ContainsKey(SubscriberID))
                    BodyIndexReaders[SubscriberID].IsPaused = false;

                if (SubscriberType == typeof(IKinectBodySubscriber) && BodyReaders.ContainsKey(SubscriberID))
                    BodyReaders[SubscriberID].IsPaused = false;

                //K4W v2: Audio isn't implemented yet
            }
        }

        /// <summary>
        /// Restarts all readers for all subscribers for the current Kinect sensor
        /// </summary>
        private void Continue()
        {
            //Pause the infrared readers
            if (UseInfraredSource)
            {
                foreach (InfraredFrameReader r in InfraredReaders.Values)
                {
                    r.IsPaused = false;
                }
            }

            //Pause the color readers
            if (UseColorSource)
            {
                foreach (ColorFrameReader r in ColorReaders.Values)
                {
                    r.IsPaused = false;
                }
            }

            //Pause the depth readers
            if (UseDepthSource)
            {
                foreach (DepthFrameReader r in DepthReaders.Values)
                {
                    r.IsPaused = false;
                }
            }

            //Pause the bodyindex readers
            if (UseBodyIndexSource)
            {
                foreach (BodyIndexFrameReader r in BodyIndexReaders.Values)
                {
                    r.IsPaused = false;
                }
            }

            //Pause the body readers
            if (UseBodySource)
            {
                foreach (BodyFrameReader r in BodyReaders.Values)
                {
                    r.IsPaused = false;
                }
            }

            //Pause the audio readers
            if (UseAudioStream)
            {
                //K4W v2: Audio isn't implemented yet
                //foreach(AudioBeamFrameReader r in AudioReaders.Values)
                //{
                //    r.IsPaused = false;
                //}
            }
        }

        #endregion

        private bool _isPaused = false;

        public bool IsPaused
        {
            get
            {
                return _isPaused;
            }
            set
            {
                if (_isPaused == value)
                    return;

                if (value)
                {
                    Pause();
                }
                else
                {
                    Continue();
                }

                _isPaused = value;
            }
        }

        public int MinDepth
        {
            get
            {
                if (Kinect != null)
                {
                    return Kinect.DepthFrameSource.DepthMinReliableDistance;
                }

                return 0;
            }
        }

        public int MaxDepth
        {
            get
            {
                if (Kinect != null)
                {
                    return Kinect.DepthFrameSource.DepthMaxReliableDistance;
                }

                return 0;
            }
        }
        
    }
}
