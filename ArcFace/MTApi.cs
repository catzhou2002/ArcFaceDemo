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

namespace ArcFace
{
    /// <summary>
    /// 五线程，五张脸
    /// </summary>
    public static class MTApi
    {
        const int FeatureSize = 22020;
        const int ThreadNum = 4;
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
        private static IntPtr[] _RBuffer = new IntPtr[ThreadNum];
        /// <summary>
        /// 人脸检测的引擎
        /// </summary>
        private static IntPtr _DEnginer = IntPtr.Zero;
        /// <summary>
        /// 人脸比对的引擎
        /// </summary>
        private static IntPtr[] _REngine = new IntPtr[ThreadNum];
        private static string _FaceDataPath;
        private static readonly FaceLib[] _FaceLib = new FaceLib[ThreadNum];
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
        public static bool Init(out string message, string appId, string dKey, string rKey, int scale = 16, string faceDataPath = "d:\\FeatureData")
        {
            if (scale < 2 || scale > 50)
            {
                message = "scale的值必须在2-50之间";
                return false;
            }

            _DBuffer = Marshal.AllocCoTaskMem(20 * 1024 * 1024);

            var initResult = (ErrorCode)ArcWrapper.DInit(appId, dKey, _DBuffer, 20 * 1024 * 1024, out _DEnginer, (int)ArcFace.EOrientPriority.Only0, scale, ThreadNum); if (initResult != ErrorCode.Ok)
            {
                message = $"初始化人脸检测引擎失败，错误代码:{(int)initResult}，错误描述：{ ((DescriptionAttribute)(initResult.GetType().GetCustomAttribute(typeof(DescriptionAttribute), false))).Description}";
                return false;
            }
            for (int i = 0; i < ThreadNum; i++)
            {
                _RBuffer[i] = Marshal.AllocCoTaskMem(40 * 1024 * 1024);
                initResult = (ErrorCode)ArcWrapper.RInit(appId, rKey, _RBuffer[i], 40 * 1024 * 1024, out _REngine[i]);
                if (initResult != ErrorCode.Ok)
                {
                    message = $"初始化人脸比对引擎失败，错误代码:{(int)initResult}，错误描述：{ ((DescriptionAttribute)(initResult.GetType().GetCustomAttribute(typeof(DescriptionAttribute), false))).Description}";
                    return false;
                }
            }

            FaceResults = new FaceResults(ThreadNum);

            _FaceDataPath = faceDataPath;
            if (!Directory.Exists(faceDataPath))
                Directory.CreateDirectory(faceDataPath);
            else
            {

                for (int i = 0; i < ThreadNum; i++)
                    _FaceLib[i] = new FaceLib();
                int index = 0;
                for (int i = 0; i < 500; i++)

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
                        _FaceLib[index % ThreadNum].Items.Add(new FaceLib.Item() { OrderId = 0, ID = info.Name.Replace(info.Extension, ""), FaceModel = faceModel });
                        index++;
                    }

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
            for (int i = 0; i < ThreadNum; i++)
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
        /// <param name="featureDataIndex">需要转换人脸特征的序号</param>
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

                var detectResult = Marshal.PtrToStructure<DetectResult>(pDetectResult);

                FaceResults.FaceNumber = detectResult.nFace;
                if (detectResult.nFace == 0)
                    return;

                FaceFeatureInput[] ffis = new FaceFeatureInput[detectResult.nFace];
                for (int i = 0; i < detectResult.nFace; i++)
                {

                    IntPtr p = IntPtr.Add(detectResult.rcFace, i * Marshal.SizeOf<FaceRect>());

                    var faceRect = Marshal.PtrToStructure<FaceRect>(p);
                    FaceResults[i].Rectangle = new Rectangle(faceRect.left, faceRect.top, faceRect.right - faceRect.left, faceRect.bottom - faceRect.top);

                    //p = new IntPtr(detectResult.lfaceOrient.ToInt32() + i * Marshal.SizeOf<int>());
                    //var faceOrient = Marshal.PtrToStructure<int>(p);

                    ffis[i] = new FaceFeatureInput
                    {
                        rcFace = faceRect,
                        lOrient = 1
                    };

                }
                //获取特征值
                Task[] tasksF = new Task[detectResult.nFace];
                for (int i = 0; i < detectResult.nFace; i++)
                {
                    object objIn = i;
                    tasksF[i] = Task.Factory.StartNew((obj) =>
                     {
                         int index = (int)obj;
                         var fi = ffis[index];
                         ArcWrapper.ExtractFeature(_REngine[index], ref imageData, ref fi, out var fm);
                         Marshal.Copy(fm.pbFeature, FaceResults.Items[index].FeatureData, 0, FeatureSize);

                     }, objIn);

                }
                Task.WaitAll(tasksF);



                for (int i = 0; i < detectResult.nFace; i++)
                {
                    System.Threading.CancellationTokenSource cts = new System.Threading.CancellationTokenSource();
                    System.Threading.CancellationToken ct = cts.Token;
                    Stopwatch sw1 = new Stopwatch();
                    sw1.Restart();

                    List<Task<s>> tasksC = new List<Task<s>>();
                    for (int j = 0; j < ThreadNum; j++)
                    {
                        var index = j;
                        tasksC.Add(Task<s>.Factory.StartNew(() =>
                        {
                            return MatchAll(i, index);
                        })
                        );

                    }
                    Task.WaitAll(tasksC.ToArray());
                    var t = tasksC.Max(ii => ii.Result.score);
                    sw1.Stop();
                    if (t < 0.5)
                    {
                        FaceResults[i].ID = "不认识";
                        FaceResults[i].Score = 0;
                    }
                    else
                    {
                        var tt = tasksC.First(ii => ii.Result.score == t);
                        FaceResults[i].ID = tt.Result.id;
                        FaceResults[i].Score = t;

                    }
                    //while (tasksC.Count() > 0)
                    //{
                    //    var tf = Task.WhenAny(tasksC).Result;
                    //    if (tf.Result)
                    //    {
                    //        cts.Cancel();
                    //        break;
                    //    }
                    //    else
                    //        tasksC.Remove(tf);
                    //}
                    //if (tasksC.Count() == 0)
                    //{
                    //    FaceResults[i].ID = "不认识";
                    //    FaceResults[i].Score = 0;
                    //}
                }
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

        }
        class s
        {
            public float score { get; set; }
            public string id { get; set; }
        }
        private static s MatchAll(int i, int index)
        {
            s ret = new s() { score = 0 };
            var fm2 = new ArcFace.FaceModel
            {
                lFeatureSize = FeatureSize,
                pbFeature = Marshal.AllocCoTaskMem(FeatureSize)
            };
            Marshal.Copy(FaceResults[i].FeatureData, 0, fm2.pbFeature, FeatureSize);
            
            try
            {
                FaceLib.Item maxItem = new FaceLib.Item();
                foreach (var item in _FaceLib[index].Items)
                {

                    ArcWrapper.Match(_REngine[index], ref item.FaceModel, ref fm2, out float score);
                    if (score > ret.score)
                    {
                        maxItem = item;
                        //item.OrderId = DateTime.Now.Ticks;

                        //ret.id = item.ID;
                        ret.score = score;
                    }
                }
                ret.id = maxItem.ID;
            }
            finally
            {
                Marshal.FreeCoTaskMem(fm2.pbFeature);
            }
            return ret;
        }
        private static bool Match(int i, int index, System.Threading.CancellationToken ct)
        {
            var fm2 = new ArcFace.FaceModel
            {
                lFeatureSize = FeatureSize,
                pbFeature = Marshal.AllocCoTaskMem(FeatureSize)
            };
            Marshal.Copy(FaceResults[i].FeatureData, 0, fm2.pbFeature, FeatureSize);
            try
            {
                foreach (var item in _FaceLib[index].Items.OrderByDescending(ii => ii.OrderId))
                {

                    var fm = item.FaceModel;
                    ArcWrapper.Match(_REngine[index], ref fm, ref fm2, out float score);
                    if (score > 0.5)
                    {
                        item.OrderId = DateTime.Now.Ticks;
                        FaceResults[i].ID = item.ID;
                        FaceResults[i].Score = score;
                        return true;
                    }
                    if (ct.IsCancellationRequested)
                    {
                        return false;
                    }
                }
                return false;
            }
            finally
            {
                Marshal.FreeCoTaskMem(fm2.pbFeature);
            }
        }
        public static bool CheckID(string id)
        {
            int count = 0;
            for (int i = 0; i < ThreadNum; i++)
                count += _FaceLib[i].Items.Count(ii => ii.ID == id);
            return count == 1;
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
            _FaceLib[0].Items.Add(new FaceLib.Item() { OrderId = DateTime.Now.Ticks, ID = id, FaceModel = faceModel });
        }
    }
}