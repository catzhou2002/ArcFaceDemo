using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArcFace
{
    /// <summary>
    /// 虹软Dll封装
    /// </summary>
    internal class ArcWrapper
    {
        /// <summary>
        /// 人脸Dll文件夹
        /// </summary>
        private const string DllPath = @"D:\ArcFaceDll\";
        /// <summary>
        /// 人脸检测Dll文件
        /// </summary>
        public const string DDllFileName = DllPath + "libarcsoft_fsdk_face_detection.dll";
        /// <summary>
        /// 人脸识别Dll文件
        /// </summary>
        public const string RDllFileName = DllPath + "libarcsoft_fsdk_face_recognition.dll";

        /// <summary>
        /// 人脸检测初始化
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="sdkKey"></param>
        /// <param name="pBuffer">至少20M</param>
        /// <param name="bufferSize"></param>
        /// <param name="engine"></param>
        /// <param name="orientPriority">脸部角度</param>
        /// <param name="scale">最小人脸尺寸有效值范围[2,50] 推荐值 16。该尺寸是人脸相对于所在图片的长边的占比。例如，如果用户想检测到的最小人脸尺寸是图片长度的 1/8，那么这个 nScale 就应该设置为8</param>
        /// <param name="maxFaceNumber">用户期望引擎最多能检测出的人脸数有效值范围[1,100]</param>
        /// <returns></returns>
        [DllImport(DDllFileName, EntryPoint = "AFD_FSDK_InitialFaceEngine", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int DInit(string appId, string sdkKey, IntPtr pBuffer, int bufferSize, out IntPtr engine, int orientPriority, int scale, int maxFaceNumber);
        [DllImport(DDllFileName, EntryPoint = "AFD_FSDK_StillImageFaceDetection", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Detection(IntPtr engine, ref ImageData imgData, out IntPtr pDetectResult);
        [DllImport(DDllFileName, EntryPoint = "AFD_FSDK_UninitialFaceEngine", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int DClose(IntPtr engine);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="sdkKey"></param>
        /// <param name="pBuffer">至少40M</param>
        /// <param name="bufferSize"></param>
        /// <param name="engine"></param>
        /// <returns></returns>
        [DllImport(RDllFileName, EntryPoint = "AFR_FSDK_InitialEngine", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int RInit(string appId, string sdkKey, IntPtr pBuffer, int bufferSize, out IntPtr engine);
        [DllImport(RDllFileName, EntryPoint = "AFR_FSDK_ExtractFRFeature", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int ExtractFeature(IntPtr engine, ref ImageData imageData, ref FaceFeatureInput faceFeatureInput, out FaceModel faceModel);
        [DllImport(RDllFileName, EntryPoint = "AFR_FSDK_FacePairMatching", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int Match(IntPtr engine, ref FaceModel faceModel1, ref FaceModel faceModel2, out float score);
        [DllImport(RDllFileName, EntryPoint = "AFR_FSDK_UninitialEngine", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int RClose(IntPtr engine);

        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);
    }
}
