using Emgu.CV;
using Emgu.CV.Structure;
using GistModulesLib;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Bitmap = System.Drawing.Bitmap;
using BitmapData = System.Drawing.Imaging.BitmapData;
using Imaging = System.Drawing.Imaging;
using pf = System.Drawing.Imaging.PixelFormat;

namespace K4W2Accuracy.Model
{
    public enum ImageFormat
    {
        WPF,
        OpenCV,
        AForge
    }

    public class DepthParameters
    {
        public int? MinClipDepth = null;

        public int? MaxClipDepth = null;

        public bool ToColor = true;
    }

    /// <summary>
    /// This class is responsible for managing the display output and the display settings
    /// for the KinectHelper object
    /// </summary>
    public static class KinectDisplayHelper
    {
        static KinectDisplayHelper()
        {
            DpiX = 96.0;
            DpiY = 96.0;
            
            RenderWidth = 320.0f;
            RenderHeight = 240.0f;

            JointThickness = 14;
            BodyCenterThickness = 10;
            ClipBoundsThickness = 10;
            HandSize = 40;

            BackgroundBrush = Brushes.Black;
            CenterPointBrush = Brushes.Blue;
            TrackedJointBrush = Brushes.LightBlue;
            InferredJointBrush = Brushes.Yellow;
            TrackedBonePen = new Pen(Brushes.Green, 10);
            InferredBonePen = new Pen(Brushes.Gray, 10);
            HandLassoBrush = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));
            HandClosedBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            HandOpenBrush = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
        }

        public static void Init(KinectHelper Helper)      
        {
            PointMapper = Helper.PointMapper;
        }
        
        /// <summary>
        /// a reference to the CoordinateMapper for the current Kinect sensor from the KinectHelper object
        /// </summary>
        public static CoordinateMapper PointMapper;

        #region GENERAL PROPERTIES

        /// <summary>
        /// Gets or sets the dpi x value used for creating WriteableBitmaps
        /// </summary>
        public static double DpiX { get; private set; }

        /// <summary>
        /// Gets or sets the dpi y value used for creating WriteableBitmaps
        /// </summary>
        public static double DpiY { get; private set; }

        #endregion

        #region INFRARED
        public static bool ProcessInfraredFrame(out WriteableBitmap image, InfraredFrameReference FrameRef,
                                                ushort[] FrameData)
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                image = null;

                using (InfraredFrame frame = FrameRef.AcquireFrame())
                {
                    if (frame != null)
                    {
                        frame.CopyFrameDataToArray(FrameData);
                        desc = frame.FrameDescription;
                        ProcessData = true;
                    }
                }

                if (ProcessData)
                {
                    byte[] ImageData = Colorizer.IrToGray(FrameData);
                    
                    image = new WriteableBitmap(desc.Width, desc.Height, DpiX, DpiY, PixelFormats.Bgr32, null);

                    image.Lock();
                    Marshal.Copy(ImageData, 0, image.BackBuffer, ImageData.Length);
                    image.AddDirtyRect(new Int32Rect(0, 0, desc.Width, desc.Height));
                    image.Unlock();
                }

                //Return the success of the function
                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;
            }
        }
        #endregion

        #region COLOR

        /// <summary>
        /// Processes a color frame by directly copying the image data to the output image. Use this
        /// version of the color processing if you don't need direct access to the color frame data.
        /// </summary>
        /// <param name="image">The output image</param>
        /// <param name="FrameRef">A reference to the color frame</param>
        /// <returns>True if the function successfully accessed the frame and copied the data; false otherwise</returns>
        public static bool ProcessColorFrame(out WriteableBitmap image, ColorFrameReference FrameRef)
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                image = null;

                using (ColorFrame frame = FrameRef.AcquireFrame())
                {
                    if (frame != null)
                    {
                        desc = frame.CreateFrameDescription(ColorImageFormat.Bgra);

                        image = new WriteableBitmap(desc.Width, desc.Height, DpiX, DpiY, PixelFormats.Bgra32, null);
                        
                        image.Lock();
                        frame.CopyConvertedFrameDataToBuffer(
                            desc.LengthInPixels * desc.BytesPerPixel,
                            image.BackBuffer,
                            ColorImageFormat.Bgra
                        );
                        image.AddDirtyRect(new Int32Rect(0, 0, desc.Width, desc.Height));
                        image.Unlock();

                        ProcessData = true;
                    }
                }

                //Return the success of the function
                return ProcessData;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;            
            }
        }

        /// <summary>
        /// Processes a color frame by copying the image data to the IrData array and then to the 
        /// output image. Use this version of the color processing if you need direct access to the 
        /// color frame data after processing is complete.
        /// </summary>
        /// <param name="image">The output image</param>
        /// <param name="FrameRef">A reference to the color frame</param>
        /// <param name="IrData">The color image data (input data will be overwritten)</param>
        /// <returns>True if the function successfully accessed the frame and copied the data; false otherwise</returns>
        public static bool ProcessColorFrame(out WriteableBitmap image, ColorFrameReference FrameRef,
                                                byte[] FrameData)
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                image = null;

                using (ColorFrame frame = FrameRef.AcquireFrame())
                {
                    if (frame != null)
                    {
                        frame.CopyConvertedFrameDataToArray(FrameData, ColorImageFormat.Bgra);
                        desc = frame.CreateFrameDescription(ColorImageFormat.Bgra);
                        ProcessData = true;
                    }
                }

                if(ProcessData)
                {
                    image = new WriteableBitmap(desc.Width, desc.Height, DpiX, DpiY, PixelFormats.Bgra32, null);

                    image.Lock();
                    Marshal.Copy(FrameData, 0, image.BackBuffer, FrameData.Length);
                    image.AddDirtyRect(new Int32Rect(0, 0, desc.Width, desc.Height));
                    image.Unlock();
                }

                //Return the success of the function
                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;
            }
        }

        /// <summary>
        /// Processes a color frame and constructs an EMGU image from the resulting data.
        /// </summary>
        /// <param name="image">The output EMGU (OpenCV) type image</param>
        /// <param name="FrameRef">A reference to the color frame</param>
        /// <returns>True if the function successfully accessed the frame and copied the data; false otherwise</returns>
        public static bool ProcessColorFrame(out Image<Rgb, Byte> image, ColorFrameReference FrameRef)
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                image = null;

                using (ColorFrame frame = FrameRef.AcquireFrame())
                {
                    if (frame != null)
                    {
                        desc = frame.CreateFrameDescription(ColorImageFormat.Rgba);

                        Bitmap bmap = new Bitmap(desc.Width, desc.Height, Imaging.PixelFormat.Format32bppRgb);

                        BitmapData bmapdata = bmap.LockBits(
                            new System.Drawing.Rectangle(0, 0, desc.Width, desc.Height),
                            Imaging.ImageLockMode.WriteOnly,
                            bmap.PixelFormat);

                        frame.CopyConvertedFrameDataToBuffer(
                            desc.BytesPerPixel * desc.LengthInPixels,
                            bmapdata.Scan0,
                            ColorImageFormat.Rgba
                        );

                        bmap.UnlockBits(bmapdata);

                        image = new Image<Rgb, byte>(bmap);

                        ProcessData = true;
                    }
                }

                //Return the success of the function
                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;
            }
        }

        #endregion

        #region DEPTH

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="FrameRef"></param>
        /// <param name="IrData"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool ProcessDepthFrame(out WriteableBitmap image, DepthFrameReference FrameRef, 
                                                ushort[] FrameData, DepthParameters p = null)
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                image = null;

                using(DepthFrame frame = FrameRef.AcquireFrame())
                {
                    if(frame != null)
                    {
                        frame.CopyFrameDataToArray(FrameData);
                        desc = frame.FrameDescription;
                        ProcessData = true;
                    }
                }

                if (ProcessData)
                {
                    byte[] ImageData = null;

                    if (p == null || p.ToColor)
                    {
                        if (p == null)
                            p = new DepthParameters();

                        ImageData = Colorizer.DepthToColor(FrameData, p.MinClipDepth, p.MaxClipDepth);
                    }
                    else
                    {
                        ImageData = Colorizer.DepthToGray(FrameData, p.MinClipDepth, p.MaxClipDepth);
                    }

                    image = new WriteableBitmap(desc.Width, desc.Height, DpiX, DpiY, PixelFormats.Bgr32, null);

                    image.Lock();
                    Marshal.Copy(ImageData, 0, image.BackBuffer, ImageData.Length);
                    image.AddDirtyRect(new Int32Rect(0, 0, desc.Width, desc.Height));
                    image.Unlock();
                }

                //Return the success of the function
                return ProcessData;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TColor"></typeparam>
        /// <param name="image"></param>
        /// <param name="FrameRef"></param>
        /// <param name="IrData"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool ProcessDepthFrame<TColor>(out Image<TColor, Byte> image, DepthFrameReference FrameRef, 
            ushort[] FrameData, DepthParameters p = null) where TColor : struct, IColor
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                image = null;

                using(DepthFrame frame = FrameRef.AcquireFrame())
                {
                    if(frame != null)
                    {
                        frame.CopyFrameDataToArray(FrameData);
                        desc = frame.FrameDescription;
                        ProcessData = true;
                    }
                }

                if (ProcessData)
                {
                    byte[] ImageData = null;

                    if (p == null || p.ToColor)
                    {
                        if (p == null)
                            p = new DepthParameters();

                        ImageData = Colorizer.DepthToColor(FrameData, p.MinClipDepth, p.MaxClipDepth, true);

                        // Write the converted depth data to the appropriate output
                        BitmapSource src = BitmapSource.Create(desc.Width, desc.Height,
                                                                    DpiX, DpiY,
                                                                    PixelFormats.Bgr32,
                                                                    null,
                                                                    ImageData,
                                                                    4 * desc.Width);
                        
                        MemoryStream stream;

                        Image<Bgr, Byte> tmpImg = new Image<Bgr, byte>(src.ToBitmap(out stream));

                        //Generate the output image
                        image = tmpImg.Convert<TColor, byte>();

                        stream.Close();
                    }
                    else
                    {
                        ImageData = Colorizer.DepthToGray(FrameData, p.MinClipDepth, p.MaxClipDepth);

                        // Write the converted depth data to the appropriate output
                        BitmapSource src = BitmapSource.Create(desc.Width, desc.Height,
                                                                    DpiX, DpiY,
                                                                    PixelFormats.Gray8,
                                                                    null,
                                                                    ImageData,
                                                                    desc.Width);

                        MemoryStream stream;

                        Image<Gray, Byte> tmpImg = new Image<Gray, byte>(src.ToBitmap(out stream));

                        //Generate the output image
                        image = tmpImg.Convert<TColor, byte>();

                        stream.Close();
                    }
                }
                
                //Return the success of the function
                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="FrameRef"></param>
        /// <param name="IrData"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static bool ProcessDepthFrame(out Bitmap bmp, DepthFrameReference FrameRef, 
                                                ushort[] FrameData, DepthParameters p = null)
        {
            try
            {
                bool ProcessData = false;
                FrameDescription desc = null;
                bmp = null;

                using(DepthFrame frame = FrameRef.AcquireFrame())
                {
                    if(frame != null)
                    {
                        frame.CopyFrameDataToArray(FrameData);
                        desc = frame.FrameDescription;
                        ProcessData = true;
                    }
                }

                if(ProcessData)
                {
                    byte[] ImageData = null;

                    if (p == null || p.ToColor)
                    {
                        if (p == null)
                            p = new DepthParameters();

                        ImageData = Colorizer.DepthToColor(FrameData, p.MinClipDepth, p.MaxClipDepth);
                        bmp = new Bitmap(desc.Width, desc.Height, pf.Format32bppRgb);
                    }
                    else
                    {
                        ImageData = Colorizer.DepthToGray(FrameData, p.MinClipDepth, p.MaxClipDepth);
                        bmp = new Bitmap(desc.Width, desc.Height, pf.Format8bppIndexed);
                    }

                    BitmapData data = bmp.LockBits(
                        new System.Drawing.Rectangle(0, 0, desc.Width, desc.Height),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        bmp.PixelFormat);

                    Marshal.Copy(ImageData, 0, data.Scan0, ImageData.Length);

                    bmp.UnlockBits(data);
                }

                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                bmp = null;

                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="IrData"></param>
        /// <param name="Width"></param>
        /// <param name="Height25"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap DepthPixels2Bitmap(ushort[] FrameData, int Width, int Height, DepthParameters p = null)
        {
            byte[] GreyData = Colorizer.DepthToGray(FrameData, p.MinClipDepth, p.MaxClipDepth);

            // Write the converted depth data to the appropriate output
            Bitmap bmp = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

            BitmapData data = bmp.LockBits(
                new System.Drawing.Rectangle(0, 0, Width, Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                bmp.PixelFormat);

            IntPtr ptr = data.Scan0;
            Marshal.Copy(GreyData, 0, ptr, GreyData.Length);

            bmp.UnlockBits(data);

            return bmp;
        }
        
        /// <summary>
        /// Colorizer object used for converting depth data into an Channel representation
        /// </summary>
        private static RangeColorizer Colorizer = new RangeColorizer(200, 4500, KinectDepthTreatment.TintUnreliableDepths);

        #endregion

        #region TODO: BODY INDEX VISUALIZATION

        #endregion

        #region BODY

        /// <summary>
        /// Draws each body onto either a solid background color or an image background
        /// </summary>
        /// <param name="image">The output image</param>
        /// <param name="FrameRef">A reference to the body frame</param>
        /// <param name="Bodies">An array holding the body data</param>
        /// <param name="Background">An optional background image to display the bodies on</param>
        /// <returns>True if the frame was accessed and the data was copied; false otherwise</returns>
        public static bool ProcessBodyFrame(out DrawingImage image, BodyFrameReference FrameRef, Body[] Bodies, WriteableBitmap Background = null)
        {
            try
            {
                bool ProcessData = false;
                image = null;

                using(BodyFrame frame = FrameRef.AcquireFrame())
                {
                    if(frame != null)
                    {
                        if(Bodies.Length != frame.BodyFrameSource.BodyCount)
                        {
                            throw new Exception("The Bodies array must be preallocated and of the same size as BodyCount");
                        }

                        frame.GetAndRefreshBodyData(Bodies);
                        ProcessData = true;
                    }
                }

                if(ProcessData)
                {
                    DrawingGroup Group = new DrawingGroup();
                    Group.ClipGeometry = new RectangleGeometry(new Rect(0, 0, RenderWidth, RenderHeight));
                    image = new DrawingImage(Group);

                    using(DrawingContext dc = Group.Open())
                    {
                        if(Background == null)
                        {
                            dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, RenderWidth, RenderHeight));
                        }
                        else
                        {
                            dc.DrawImage(Background, new Rect(0, 0, RenderWidth, RenderHeight));
                        }

                        foreach (Body body in Bodies)
                        {
                            if (body.IsTracked == true)
                            {
                                //Get the joint points in the depth space
                                Dictionary<JointType, Point> Points = new Dictionary<JointType, Point>();

                                foreach (JointType type in body.Joints.Keys)
                                {
                                    var cpt = PointMapper.MapCameraPointToColorSpace(body.Joints[type].Position);
                                    Points[type] = new Point(cpt.X, cpt.Y);
                                }

                                //Draw the data
                                DrawBody(body.Joints, Points, dc); 
                                DrawHand(body.HandLeftState, Points[JointType.HandLeft], dc);
                                DrawHand(body.HandRightState, Points[JointType.HandRight], dc);
                            }
                        }
                    }
                }
                
                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                image = null;

                return false;
            }
        }

        /// <summary>
        /// Draws each body onto either a solid background color or an image background. Use this version
        /// if you need to still have access to the drawing context after the body drawing process is 
        /// complete. Ensure that you close the drawing context at some point after calling this function!
        /// </summary>
        /// <param name="context">The drawing context</param>
        /// <param name="FrameRef">A reference to the body frame</param>
        /// <param name="Bodies">An array holding the body data</param>
        /// <param name="Background">An optional background image to display the bodies on</param>
        /// <returns>True if the frame was accessed and the data was copied; false otherwise</returns>
        public static bool ProcessBodyFrame(DrawingContext context, BodyFrameReference FrameRef, Body[] Bodies, WriteableBitmap Background = null)
        {
            try
            {
                bool ProcessData = false;

                using (BodyFrame frame = FrameRef.AcquireFrame())
                {
                    if (frame != null)
                    {
                        if (Bodies.Length != frame.BodyFrameSource.BodyCount)
                        {
                            throw new Exception("The Bodies array must be preallocated and of the same size as BodyCount");
                        }
                        
                        frame.GetAndRefreshBodyData(Bodies);
                        ProcessData = true;
                    }
                }

                if (ProcessData)
                {
                    if (Background == null)
                    {
                        context.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, RenderWidth, RenderHeight));
                    }
                    else
                    {
                        context.DrawImage(Background, new Rect(0, 0, RenderWidth, RenderHeight));
                    }

                    foreach (Body body in Bodies)
                    {
                        if (body.IsTracked == true)
                        {
                            //Get the joint points in the depth space
                            Dictionary<JointType, Point> Points = new Dictionary<JointType, Point>();

                            foreach (JointType type in body.Joints.Keys)
                            {
                                ColorSpacePoint depthSpacePoint = PointMapper.MapCameraPointToColorSpace(body.Joints[type].Position);
                                Points[type] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                            }

                            //Draw the data
                            DrawBody(body.Joints, Points, context);
                            DrawHand(body.HandLeftState, Points[JointType.HandLeft], context);
                            DrawHand(body.HandRightState, Points[JointType.HandRight], context);
                        }
                    }
                }

                return ProcessData;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.TargetSite + " - " + e.Message);

                context = null;

                return false;
            }
        }

        /// <summary>
        /// Draws a body
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="points">translated positions of joints to draw</param>
        /// <param name="dc">drawing context to draw to</param>
        private static void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> points, DrawingContext dc)
        {
            // Torso
            DrawBone(joints, points, JointType.Head, JointType.Neck, dc);
            DrawBone(joints, points, JointType.Neck, JointType.SpineShoulder, dc);
            DrawBone(joints, points, JointType.SpineShoulder, JointType.SpineMid, dc);
            DrawBone(joints, points, JointType.SpineMid, JointType.SpineBase, dc);
            DrawBone(joints, points, JointType.SpineShoulder, JointType.ShoulderRight, dc);
            DrawBone(joints, points, JointType.SpineShoulder, JointType.ShoulderLeft, dc);
            DrawBone(joints, points, JointType.SpineBase, JointType.HipRight, dc);
            DrawBone(joints, points, JointType.SpineBase, JointType.HipLeft, dc);

            // Right Arm    
            DrawBone(joints, points, JointType.ShoulderRight, JointType.ElbowRight, dc);
            DrawBone(joints, points, JointType.ElbowRight, JointType.WristRight, dc);
            DrawBone(joints, points, JointType.WristRight, JointType.HandRight, dc);
            DrawBone(joints, points, JointType.HandRight, JointType.HandTipRight, dc);
            DrawBone(joints, points, JointType.WristRight, JointType.ThumbRight, dc);

            // Left Arm
            DrawBone(joints, points, JointType.ShoulderLeft, JointType.ElbowLeft, dc);
            DrawBone(joints, points, JointType.ElbowLeft, JointType.WristLeft, dc);
            DrawBone(joints, points, JointType.WristLeft, JointType.HandLeft, dc);
            DrawBone(joints, points, JointType.HandLeft, JointType.HandTipLeft, dc);
            DrawBone(joints, points, JointType.WristLeft, JointType.ThumbLeft, dc);

            // Right Leg
            DrawBone(joints, points, JointType.HipRight, JointType.KneeRight, dc);
            DrawBone(joints, points, JointType.KneeRight, JointType.AnkleRight, dc);
            DrawBone(joints, points, JointType.AnkleRight, JointType.FootRight, dc);

            // Left Leg
            DrawBone(joints, points, JointType.HipLeft, JointType.KneeLeft, dc);
            DrawBone(joints, points, JointType.KneeLeft, JointType.AnkleLeft, dc);
            DrawBone(joints, points, JointType.AnkleLeft, JointType.FootLeft, dc);

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = TrackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = InferredJointBrush;
                }

                if (drawBrush != null)
                {
                    dc.DrawEllipse(drawBrush, null, points[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="points">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="dc">drawing context to draw to</param>
        private static void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == TrackingState.Inferred &&
                joint1.TrackingState == TrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = InferredBonePen;

            if ((joint0.TrackingState == TrackingState.Tracked) 
                && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = TrackedBonePen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }
        
        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(HandClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(HandOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(HandLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }
        
        /// <summary>
        /// Gets or sets the width for rendering skeletons to an image
        /// </summary>
        public static float RenderWidth { get; set; }

        /// <summary>
        /// Gets or sets the height for rendering skeletons to an image
        /// </summary>
        public static float RenderHeight { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the joints drawn for skeletons
        /// </summary>
        public static double JointThickness { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the body center drawn for skeletons
        /// </summary>
        public static double BodyCenterThickness { get; set; }

        /// <summary>
        /// Gets or sets the thickness of the clip bounds region for drawing skeletons
        /// </summary>
        public static double ClipBoundsThickness { get; set; }

        /// <summary>
        /// Gets or sets the color used to draw the background for skeletons
        /// </summary>
        public static Brush BackgroundBrush { get; set; }

        /// <summary>
        /// Get or sets the color used to draw the center point of a body
        /// </summary>
        public static Brush CenterPointBrush { get; set; }

        /// <summary>
        /// Gets or sets the color of tracked skeletal joints
        /// </summary>
        public static Brush TrackedJointBrush { get; set; }

        /// <summary>
        /// Gets or sets the color of inferred skeletal joints
        /// </summary>
        public static Brush InferredJointBrush { get; set; }

        /// <summary>
        /// Gets or sets the pen used to draw tracked body bones
        /// </summary>
        public static Pen TrackedBonePen { get; set; }

        /// <summary>
        /// Gets or sets the pen used to draw inferred body bones
        /// </summary>
        public static Pen InferredBonePen { get; set; }

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private static Brush HandLassoBrush { get; set; }

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as closed
        /// </summary>
        private static Brush HandClosedBrush { get; set; }

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private static Brush HandOpenBrush { get; set; }

        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private static double HandSize { get; set; }

        #endregion
    }
}
