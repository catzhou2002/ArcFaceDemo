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
using System.Diagnostics;
using System.Threading;

namespace ArcFace
{
    /// <summary>
    /// 四线程
    /// </summary>
    public static class Api
    {
        const int FeatureSize = 22020;
        const int TaskNum = 4;
        /// <summary>
        /// 人脸识别结果集
        /// </summary>
        public static FaceResults CacheFaceResults { get; set; }
        /// <summary>
        /// 人脸检测的缓存
        /// </summary>
        private static IntPtr _DBuffer = IntPtr.Zero;
        /// <summary>
        /// 人脸比对的缓存
        /// </summary>
        private static IntPtr[] _RBuffer = new IntPtr[TaskNum];
        /// <summary>
        /// 人脸检测的引擎
        /// </summary>
        private static IntPtr _DEnginer = IntPtr.Zero;
        /// <summary>
        /// 人脸比对的引擎
        /// </summary>
        private static IntPtr[] _REngine = new IntPtr[TaskNum];
        private static int _MaxFaceNumber;
        private static string _FaceDataPath, _FaceImagePath;

        private static readonly FaceLib[] _FaceLib = new FaceLib[TaskNum];
        private static DetectResult _DetectResult;
        private static readonly ReaderWriterLockSlim _RWL = new ReaderWriterLockSlim();

        /// <summary>
        /// 初始化，主要用于视频，取消人脸方向参数
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
        public static bool Init(out string message, string appId, string dKey, string rKey, int scale = 50, int maxFaceNumber = 10, string faceDataPath = "d:\\FeatureData")
        {
            if (scale < 2 || scale > 50)
            {
                message = "scale的值必须在2-50之间";
                return false;
            }
            if (maxFaceNumber < 1 || maxFaceNumber > 100)
            {
                message = "人脸数量必须在1-100之间";
                return false;
            }
            _DBuffer = Marshal.AllocCoTaskMem(20 * 1024 * 1024);

            var initResult = (ErrorCode)ArcWrapper.DInit(appId, dKey, _DBuffer, 20 * 1024 * 1024, out _DEnginer, (int)ArcFace.EOrientPriority.Only0, scale, maxFaceNumber);
            if (initResult != ErrorCode.Ok)
            {
                message = $"初始化人脸检测引擎失败，错误代码:{(int)initResult}，错误描述：{ ((DescriptionAttribute)(initResult.GetType().GetCustomAttribute(typeof(DescriptionAttribute), false))).Description}";
                return false;
            }
            for (int i = 0; i < TaskNum; i++)
            {
                _RBuffer[i] = Marshal.AllocCoTaskMem(40 * 1024 * 1024);
                initResult = (ErrorCode)ArcWrapper.RInit(appId, rKey, _RBuffer[i], 40 * 1024 * 1024, out _REngine[i]);
                if (initResult != ErrorCode.Ok)
                {
                    message = $"初始化人脸比对引擎失败，错误代码:{(int)initResult}，错误描述：{ ((DescriptionAttribute)(initResult.GetType().GetCustomAttribute(typeof(DescriptionAttribute), false))).Description}";
                    return false;
                }

                _FaceLib[i] = new FaceLib();
            }

            CacheFaceResults = new FaceResults(maxFaceNumber);
            _MaxFaceNumber = maxFaceNumber;

            _FaceDataPath = faceDataPath;
            _FaceImagePath = Path.Combine(_FaceDataPath, "Image");
            if (!Directory.Exists(faceDataPath))
                Directory.CreateDirectory(faceDataPath);
            if (!Directory.Exists(_FaceImagePath))
                Directory.CreateDirectory(_FaceImagePath);

            int index = 0;
            //for (int i = 0; i < 100; i++)

            foreach (var file in Directory.GetFiles(faceDataPath))
            {
                var info = new FileInfo(file);
                var data = File.ReadAllBytes(file);
                var pFeature = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, pFeature, data.Length);
                _FaceLib[index % TaskNum].Items.Add(new FaceLib.Item() { OrderId = 0, ID = info.Name.Replace(info.Extension, ""), FaceModel = new FaceModel { lFeatureSize = FeatureSize, pbFeature = pFeature } });
                index++;

            }

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
            if (_DBuffer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_DBuffer);
                _DBuffer = IntPtr.Zero;
            }
            for (int i = 0; i < TaskNum; i++)
            {
                if (_REngine[i] != IntPtr.Zero)
                {
                    ArcWrapper.RClose(_REngine[i]);
                    _REngine[i] = IntPtr.Zero;
                }
                if (_RBuffer[i] != IntPtr.Zero)
                {

                    Marshal.FreeCoTaskMem(_RBuffer[i]);
                    _RBuffer[i] = IntPtr.Zero;
                }
                foreach (var item in _FaceLib[i].Items)
                {
                    Marshal.FreeCoTaskMem(item.FaceModel.pbFeature);
                }
            }


        }


        /// <summary>
        /// 人脸比对
        /// </summary>
        /// <param name="bitmap">输入图片</param>
        public static void FaceMatch(Bitmap bitmap)
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

                _DetectResult = Marshal.PtrToStructure<DetectResult>(pDetectResult);

                CacheFaceResults.FaceNumber = _DetectResult.nFace;
                for (int i = _DetectResult.nFace; i < _MaxFaceNumber; i++)
                {
                    CacheFaceResults[i].ID = "";
                }
                if (_DetectResult.nFace == 0)
                    return;

                for (int i = 0; i < _DetectResult.nFace; i++)
                {
                    CacheFaceResults[i].Rect = Marshal.PtrToStructure<FaceRect>(IntPtr.Add(_DetectResult.rcFace, i * Marshal.SizeOf<FaceRect>()));
                }

                Task[] ts = new Task[TaskNum];

                int faceOffset = -1;
                for (int i = 0; i < TaskNum; i++)
                {
                    var rEngine = _REngine[i];
                    ts[i] = Task.Factory.StartNew(() =>
                    {
                        while (true)
                        {
                            var faceIndex = Interlocked.Increment(ref faceOffset);
                            if (faceIndex >= _DetectResult.nFace)
                                break;

                            var ffi = new FaceFeatureInput
                            {
                                rcFace = CacheFaceResults.Items[faceIndex].Rect,
                                lOrient = 1
                            };
                            ArcWrapper.ExtractFeature(rEngine, ref imageData, ref ffi, out var fm);//.  var fm);
                            Marshal.Copy(fm.pbFeature, CacheFaceResults.Items[faceIndex].FeatureData, 0, FeatureSize);
                        }
                    });
                }
                Task.WaitAll(ts);
                List<int> noMatchFaces = Enumerable.Range(0, _DetectResult.nFace).ToList();
                for (int i = 0; i < TaskNum; i++)
                {
                    var taskIndex = i;
                    ts[i] = Task.Factory.StartNew(() =>
                    {
                        var rEngine = _REngine[taskIndex];
                        FaceModel[] fms = new FaceModel[_DetectResult.nFace];
                        for (int faceIndex = 0; faceIndex < _DetectResult.nFace; faceIndex++)
                        {
                            fms[faceIndex] = new FaceModel()
                            {
                                lFeatureSize = FeatureSize,
                                pbFeature = Marshal.AllocCoTaskMem(FeatureSize)
                            };
                            Marshal.Copy(CacheFaceResults.Items[faceIndex].FeatureData, 0, fms[faceIndex].pbFeature, FeatureSize);
                        }
                        foreach (var item in _FaceLib[taskIndex].Items.OrderByDescending(ii => ii.OrderId))//.Skip(faceModelCount * taskIndex).Take(faceModelCount))
                        {
                            _RWL.EnterReadLock();
                            if (noMatchFaces.Count == 0)
                            {
                                _RWL.ExitReadLock();
                                break;
                            }
                            var faceIndexs = noMatchFaces.ToList();
                            _RWL.ExitReadLock();

                            foreach (var faceIndex in faceIndexs)
                            {
                                ArcWrapper.Match(rEngine, ref fms[faceIndex], ref item.FaceModel, out float score);

                                if (score > 0.55)
                                {
                                    CacheFaceResults[faceIndex].ID = item.ID;
                                    CacheFaceResults[faceIndex].Score = score;
                                    item.OrderId = DateTime.Now.Ticks;
                                    _RWL.EnterWriteLock();
                                    noMatchFaces.Remove(faceIndex);
                                    _RWL.ExitWriteLock();
                                }
                            }
                        }
                    });

                }

                Task.WaitAll(ts);
                foreach (var faceIndex in noMatchFaces)
                {
                    CacheFaceResults[faceIndex].ID = "";
                    CacheFaceResults[faceIndex].Score = 0;
                }


            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

        }

        public static bool CheckID(string id)
        {
            int count = 0;
            for (int i = 0; i < TaskNum; i++)
                count += _FaceLib[i].Items.Count(ii => ii.ID == id);
            return count > 0;
        }
        public static void AddFace(string id, byte[] featureData, Image img)
        {
            var fileName = Path.Combine(_FaceDataPath, id + ".dat");
            System.IO.File.WriteAllBytes(fileName, featureData);
            fileName = Path.Combine(_FaceImagePath, id + ".bmp");
            img.Save(fileName);
            var faceModel = new FaceModel
            {
                lFeatureSize = featureData.Length,
                pbFeature = Marshal.AllocHGlobal(featureData.Length)
            };

            Marshal.Copy(featureData, 0, faceModel.pbFeature, featureData.Length);
            _FaceLib[0].Items.Add(new FaceLib.Item() { OrderId = DateTime.Now.Ticks, ID = id, FaceModel = faceModel });
        }
    }
}