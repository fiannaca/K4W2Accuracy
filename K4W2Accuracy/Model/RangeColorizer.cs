using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Microsoft.Kinect;

namespace K4W2Accuracy.Model
{
    /// <summary>
    /// Visualization treatments for KinectDepthViewer.
    /// </summary>
    public enum KinectDepthTreatment
    {
        /// <summary>
        /// Clamp depth values that are outside the reliable range.
        /// </summary>
        ClampUnreliableDepths = 0,

        /// <summary>
        /// Display all depth values, but apply a tint to values outside the reliable range.
        /// </summary>
        TintUnreliableDepths,

        /// <summary>
        /// Display all depth values normally.
        /// </summary>
        DisplayAllDepths
    }

    /// <summary>
    /// Generates a color representation of a depth frame.
    /// </summary>
    public class RangeColorizer
    {
        public int MinDepth { get; set; }

        public int MaxDepth { get; set; }

        public ushort MinIR { get; set; }

        public ushort MaxIR { get; set; }

        public KinectDepthTreatment DepthTreatment { get; set; }

        public byte[] DepthColorTable;

        public byte[] InfraredColorTable;

        public RangeColorizer()
        {
            MinDepth = 400;
            MaxDepth = 45000;

            MinIR = ushort.MinValue;
            MaxIR = ushort.MaxValue;

            DepthColorTable = new byte[MaxDepth - MinDepth + 1];
            InfraredColorTable = new byte[(MaxIR - MinIR) / 4 + 1];

            BuildColorTables();

            DepthTreatment = KinectDepthTreatment.TintUnreliableDepths;
        }

        public RangeColorizer(int min, int max, KinectDepthTreatment treatment)
        {
            MinDepth = min;
            MaxDepth = max;

            DepthColorTable = new byte[MaxDepth - MinDepth + 1];
            InfraredColorTable = new byte[(MaxIR - MinIR) / 4 + 1];
            BuildColorTables();

            DepthTreatment = treatment;
        }

        private void BuildColorTables()
        {
            int rightShift = MinDepth + 100;
            int DepthSize = MaxDepth - MinDepth + 1;
            int IrSize = (MaxIR - MinIR) / 4 + 1;
            int value;

            //
            //Color the input values using exponential functions in the range 0 to 255
            //

            for (int i = 0; i < DepthSize; ++i)
            {
                value = i + MinDepth;
                DepthColorTable[i] = (byte)(255 * Math.Pow(Math.E, (-0.0009 * (Math.Max(rightShift, value) - rightShift))));
            }

            for(int i = 0; i < IrSize; ++i)
            {
                value = i * 4;
                InfraredColorTable[i] = (byte)(255 * Math.Pow(Math.E, (-0.00005 * value)));
            }
        }

        public byte[] IrToGray(ushort[] IrData)
        {
            int channels = 4;
            byte[] RgbData = new byte[IrData.Length * channels];

            for (int i = 0; i < IrData.Length; ++i)
            {
                byte val = InfraredColorTable[IrData[i] / 4];

                RgbData[(i * 4) + 0] = val; //B
                RgbData[(i * 4) + 1] = val; //G
                RgbData[(i * 4) + 2] = val; //R
                //Ignore A
            }
            
            return RgbData;
        }

        //Need to ensure that the returned data is in 32bppRGB format

        public byte[] DepthToColor(ushort[] DepthData, int? min = null, int? max = null, bool bgra = false)
        {
            int tmpMin = MinDepth, tmpMax = MaxDepth;
            int PixelIndex = 0;
            byte Intensity;

            byte[] RgbData = new byte[DepthData.Length * sizeof(int)];

            //Temporarily set the min and max if need be
            if (min != null)
            {
                MinDepth = min.Value;
            }

            if (max != null)
            {
                MaxDepth = max.Value;
            }

            //Generate the image data
            for (int i = 0; i < DepthData.Length; ++i)
            {
                if (DepthData[i] >= MinDepth && DepthData[i] <= MaxDepth)
                {
                    Intensity = DepthColorTable[(short)DepthData[i] - MinDepth];

                    RgbData[PixelIndex++] = Intensity;
                    RgbData[PixelIndex++] = Intensity;
                    RgbData[PixelIndex++] = Intensity;

                    PixelIndex++;
                }
                else
                {
                    RgbData[PixelIndex++] = 0;
                    RgbData[PixelIndex++] = 0;
                    RgbData[PixelIndex++] = 255;

                    PixelIndex++;
                }
            }

            //Reset the min and max
            MinDepth = tmpMin;
            MaxDepth = tmpMax;

            return RgbData;
        }

        //Need to ensure that the returned data is in 32bppRGB format

        public byte[] DepthToGray(ushort[] DepthData, int? min = null, int? max = null)
        {
            int tmpMin = MinDepth, tmpMax = MaxDepth;
            int PixelIndex = 0;
            byte Intensity;

            int Length = (DepthData.Length * sizeof(int)) / 4;

            byte[] GrayData = new byte[Length];

            //Temporarily set the min and max if need be
            if (min != null)
            {
                MinDepth = min.Value;
            }

            if (max != null)
            {
                MaxDepth = max.Value;
            }

            //Generate the image data
            for (int i = 0; i < DepthData.Length; ++i)
            {
                if (DepthData[i] >= MinDepth && DepthData[i] <= MaxDepth)
                {
                    Intensity = DepthColorTable[(short)DepthData[i] - MinDepth];

                    GrayData[PixelIndex++] = Intensity;
                }
                else
                {
                    GrayData[PixelIndex++] = 0;
                }
            }

            //Reset the min and max
            MinDepth = tmpMin;
            MaxDepth = tmpMax;

            return GrayData;
        }
    }

}
