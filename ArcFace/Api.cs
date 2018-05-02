using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace ArcFace
{
    public static class Api
    {
        /// <summary>
        /// 人脸识别结果集
        /// </summary>
        public static FaceResults FaceResults { get; set; }
        /// <summary>
        /// 人脸检测的缓存
        /// </summary>
        private static IntPtr _DBuffer = IntPtr.Zero;
        /// <summary>
        /// 人脸比对的缓存
        /// </summary>
        private static IntPtr _RBuffer = IntPtr.Zero;
        /// <summary>
        /// 人脸检测的引擎
        /// </summary>
        private static IntPtr _DEnginer = IntPtr.Zero;
        /// <summary>
        /// 人脸比对的引擎
        /// </summary>
        private static IntPtr _REngine = IntPtr.Zero;
        private static int _MaxFaceNumber;
        private static string _FaceDataPath;
        private static readonly FaceLib _FaceLib = new FaceLib();
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="appId">虹软SDK的AppId</param>
        /// <param name="dKey">虹软SDK人脸检测的Key</param>
        /// <param name="rKey">虹软SDK人脸比对的Key</param>
        /// <param name="orientPriority">脸部角度，毋宁说是鼻子方向，上下为0或180度，左右为90或270度</param>
        /// <param name="scale">最小人脸尺寸有效值范围[2,50] 推荐值 16。该尺寸是人脸相对于所在图片的长边的占比。例如，如果用户想检测到的最小人脸尺寸是图片长度的 1/8，那么这个 scale 就应该设置为8</param>
        /// <param name="maxFaceNumber">用户期望引擎最多能检测出的人脸数有效值范围[1,100]</param>
        /// <param name="faceDataPath">人脸数据文件夹</param>
        /// <param name="rateH">视频图片采集高度和显示高度的比值</param>
        /// <param name="rateW">视频图片采集宽度和显示宽度的比值</param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool Init(out string message, string appId, string dKey, string rKey, EOrientPriority orientPriority = EOrientPriority.Ext0, int scale = 16, int maxFaceNumber = 10, string faceDataPath = "d:\\FeatureData")
        {
            if (scale < 2 || scale > 50)
            {
                message = "scale的值必须在2-50之间";
                return false;
            }
            if (maxFaceNumber < 1 || maxFaceNumber > 100)
            {
                message = "maxFaceNumber的值必须在1-100之间";
                return false;
            }
            _DBuffer = Marshal.AllocCoTaskMem(20 * 1024 * 1024);
            _RBuffer = Marshal.AllocCoTaskMem(40 * 1024 * 1024);

            var initResult = (ErrorCode)ArcWrapper.DInit(appId, dKey, _DBuffer, 20 * 1024 * 1024, out _DEnginer, (int)orientPriority, scale, maxFaceNumber);
            if (initResult != ErrorCode.Ok)
            {
                message = $"初始化人脸检测引擎失败，错误代码:{(int)initResult}，错误描述：{ ((DescriptionAttribute)(initResult.GetType().GetCustomAttribute(typeof(DescriptionAttribute), false))).Description}";
                return false;
            }
            initResult = (ErrorCode)ArcWrapper.RInit(appId, rKey, _RBuffer, 40 * 1024 * 1024, out _REngine);
            if (initResult != ErrorCode.Ok)
            {
                message = $"初始化人脸比对引擎失败，错误代码:{(int)initResult}，错误描述：{ ((DescriptionAttribute)(initResult.GetType().GetCustomAttribute(typeof(DescriptionAttribute), false))).Description}";
                return false;
            }

            FaceResults = new FaceResults(maxFaceNumber);

            _FaceDataPath = faceDataPath;
            if (!Directory.Exists(faceDataPath))
                Directory.CreateDirectory(faceDataPath);
            else
            {
                foreach (var file in Directory.GetFiles(faceDataPath))
                {
                    var info = new FileInfo(file);
                    var data = File.ReadAllBytes(file);
                    var faceModel = new FaceModel
                    {
                        lFeatureSize = data.Length,
                        pbFeature = Marshal.AllocCoTaskMem(data.Length)
                    };

                    Marshal.Copy(data, 0, faceModel.pbFeature, data.Length);
                    _FaceLib.Items.Add(new FaceLib.Item() { OrderId = 0, ID = info.Name.Replace(info.Extension, ""), FaceModel = faceModel });
                }

            }
            _MaxFaceNumber = maxFaceNumber;
            message = "初始化成功";
            return true;
        }
        public static void Close()
        {
            if (_DEnginer != IntPtr.Zero)
            {
                ArcWrapper.DClose(_DEnginer);
                _DEnginer = IntPtr.Zero;
            }
            if (_REngine != IntPtr.Zero)
            {
                ArcWrapper.RClose(_REngine);
                _REngine = IntPtr.Zero;
            }
            if (_DBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_DBuffer);
                _DBuffer = IntPtr.Zero;
                Marshal.FreeCoTaskMem(_RBuffer);
                _RBuffer = IntPtr.Zero;
            }
            foreach (var item in _FaceLib.Items)
            {
                Marshal.FreeCoTaskMem(item.FaceModel.pbFeature);
            }
        }
        /// <summary>
        /// 人脸比对
        /// </summary>
        /// <param name="bitmap">输入图片</param>
        /// <param name="featureDataIndex">需要转换人脸特征的序号</param>
        public static void FaceMatch(Bitmap bitmap, int featureDataIndex = -1)
        {

            var bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var imageData = new ImageData
            {
                u32PixelArrayFormat = 513,//Rgb24,
                i32Width = bitmap.Width,
                i32Height = bitmap.Height,
                pi32Pitch = new int[4],
                ppu8Plane = new IntPtr[4]
            };
            imageData.pi32Pitch[0] = bmpData.Stride;
            imageData.ppu8Plane[0] = bmpData.Scan0;




            try
            {
                var ret = (ErrorCode)ArcWrapper.Detection(_DEnginer, ref imageData, out var pDetectResult);
                if (ret != ErrorCode.Ok)
                    return;

                var detectResult = Marshal.PtrToStructure<DetectResult>(pDetectResult);

                FaceResults.FaceNumber = detectResult.nFace;
                if (detectResult.nFace == 0)
                    return;


                for (int i = 0; i < detectResult.nFace; i++)
                {
                    IntPtr p = new IntPtr(detectResult.rcFace.ToInt32() + i * Marshal.SizeOf<FaceRect>());
                    var faceRect = Marshal.PtrToStructure<FaceRect>(p);
                    FaceResults[i].Rectangle = new Rectangle(faceRect.left, faceRect.top, faceRect.right - faceRect.left, faceRect.bottom - faceRect.top);

                    p = new IntPtr(detectResult.lfaceOrient.ToInt32() + i * Marshal.SizeOf<int>());
                    var faceOrient = Marshal.PtrToStructure<int>(p);

                    var faceFeatureInput = new FaceFeatureInput
                    {
                        rcFace = faceRect,
                        lOrient = faceOrient
                    };
                    if ((ErrorCode)ArcWrapper.ExtractFeature(_REngine, ref imageData, ref faceFeatureInput, out var faceModel) != ErrorCode.Ok)
                        continue;


                    if (featureDataIndex == i)
                    {
                        if (FaceResults[i].FeatureData == null)
                            FaceResults[i].FeatureData = new byte[faceModel.lFeatureSize];
                        Marshal.Copy(faceModel.pbFeature, FaceResults[i].FeatureData, 0, faceModel.lFeatureSize);
                    }
                    bool matched = false;
                    foreach (var item in _FaceLib.Items.OrderByDescending(ii => ii.OrderId))
                    {
                        var fm = item.FaceModel;
                        ArcWrapper.Match(_REngine, ref fm, ref faceModel, out float score);
                        if (score > 0.5)
                        {
                            matched = true;
                            item.OrderId = DateTime.Now.Ticks;
                            FaceResults[i].ID = item.ID;
                            FaceResults[i].Score = score;
                            break;
                        }
                    }
                    if (!matched)
                    {
                        FaceResults[i].ID = "不认识";
                        FaceResults[i].Score = 0;
                    }
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

        }
        public static bool CheckID(string id)
        {
            return _FaceLib.Items.Count(ii => ii.ID == id) == 1;
        }
        public static void AddFace(string id, byte[] featureData)
        {
            var fileName = Path.Combine(_FaceDataPath, id + ".dat");
            System.IO.File.WriteAllBytes(fileName, featureData);
            var faceModel = new FaceModel
            {
                lFeatureSize = featureData.Length,
                pbFeature = Marshal.AllocHGlobal(featureData.Length)
            };

            Marshal.Copy(featureData, 0, faceModel.pbFeature, featureData.Length);
            _FaceLib.Items.Add(new FaceLib.Item() { OrderId = DateTime.Now.Ticks, ID = id, FaceModel = faceModel });
        }
    }
}