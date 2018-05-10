using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArcFace
{
    /// <summary>
    /// 人脸库
    /// </summary>
    internal class FaceLib
    {
        public List<Item> Items { get; set; } = new List<Item>();
        public class Item
        {
            /// <summary>
            /// 用于排序
            /// </summary>
            public long OrderId { get; set; }
            /// <summary>
            /// 文件名作为ID
            /// </summary>
            public string ID { get; set; }
            /// <summary>
            /// 人脸模型
            /// </summary>
            public FaceModel FaceModel;// { get; set; }
        }
    }
    /// <summary>
    /// 人脸识别结果集
    /// </summary>
    public class FaceResults
    {
        public List<FaceResult> Items { get; set; }
        public int FaceNumber { get; set; }
        public FaceResults(int maxFaceNumber)
        {
            Items = new List<FaceResult>();
            for (int i = 0; i < maxFaceNumber; i++)
            {
                Items.Add(new FaceResult());
            }
        }
        public FaceResult this[int index]
        {
            get
            {
                return Items[index];
            }
            set
            {
                Items[index] = value;
            }
        }

    }
    /// <summary>
    /// 人脸识别结果
    /// </summary>
    public class FaceResult
    {
        public string ID { get; set; }
        public System.Drawing.Rectangle Rectangle
        {
            get
            {
                return new System.Drawing.Rectangle(FFI.FaceRect.Left, FFI.FaceRect.Top, FFI.FaceRect.Right - FFI.FaceRect.Left, FFI.FaceRect.Bottom - FFI.FaceRect.Top);
            }
        }
        public byte[] GetFeatureData()
        {
            var data = new byte[22020];
            Marshal.Copy(FaceModel.PFeature, data, 0, 22020);
            return data;
        }
        public float Score { get; set; }
        internal FaceFeatureInput FFI = new FaceFeatureInput() { Orient = 1 };
        internal FaceModel FaceModel = new FaceModel() { Size = 22020, PFeature = Marshal.AllocCoTaskMem(22020) };
    }
}
