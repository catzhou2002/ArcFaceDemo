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
        internal List<ArcFace.FaceModel> fms = new List<FaceModel>();
        public FaceResults(int maxFaceNumber)
        {
            Items = new List<FaceResult>();
            for (int i = 0; i < maxFaceNumber; i++)
            {
                Items.Add(new FaceResult());
                fms.Add(new FaceModel());
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
                return new System.Drawing.Rectangle(Rect.left, Rect.top, Rect.right - Rect.left, Rect.bottom - Rect.top);
            }
        }
        public byte[] FeatureData { get; set; } = new byte[22020];
        public float Score { get; set; }
        internal FaceRect Rect;
    }
}
