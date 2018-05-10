using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognization.Common
{
    public class CameraPara
    {
        /// <summary>
        /// 是否有摄像头
        /// </summary>
        public bool HasVideoDevice { get; set; }
        /// <summary>
        /// 视频源
        /// </summary>
        public VideoCaptureDevice VideoSource { get; set; }
        /// <summary>
        /// 视频图片的宽度
        /// </summary>
        public int FrameWidth { get; set; }
        /// <summary>
        /// 视频图片的高度
        /// </summary>
        public int FrameHeight { get; set; }
        /// <summary>
        /// 视频图片的字节数
        /// </summary>
        public int ByteCount { get; set; }
        public static CameraPara GetPara()
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            CameraPara p = new CameraPara();
            if (videoDevices.Count == 0)//没有检测到摄像头
            {
                p.HasVideoDevice = false;
                return p;
            }

            p.VideoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);//连接第一个摄像头
            var videoResolution = p.VideoSource.VideoCapabilities[0];//.First(ii => ii.FrameSize.Width == p.VideoSource.VideoCapabilities.Max(jj => jj.FrameSize.Width)); //获取摄像头最高的分辨率

            p.FrameWidth = videoResolution.FrameSize.Width;
            p.FrameHeight = videoResolution.FrameSize.Height;
            p.ByteCount = videoResolution.BitCount / 8;
            p.VideoSource.VideoResolution = videoResolution;
            p.HasVideoDevice = true;
            return p;
        }

    }
}
