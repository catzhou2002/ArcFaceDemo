using FaceRecognization.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceRecognization
{
    public partial class Main : Form
    {
        /// <summary>
        /// 虹软SDK的AppId
        /// </summary>
        const string AppId = "BKgqTWQPQQbomfqvyd2VJzTbzo5C4T5w4tzgN3GL6euK";
        /// <summary>
        /// 虹软SDK人脸检测的Key
        /// </summary>
        const string DKey = "2Yqm2EcsJyBbJjSrirPSNoyQNpaSJz19noCteLQ88SoG";
        /// <summary>
        /// 虹软SDK人脸比对的Key
        /// </summary>
        const string RKey = "2Yqm2EcsJyBbJjSrirPSNoyu2Rd4j1ydfwxwFX9vPmtY";
        CameraPara _CameraPara = null;
        /// <summary>
        /// 摄像头获取的图片和现实的图片的宽度高度比率
        /// </summary>
        float _RateW, _RateH;
        Font _FontId;
        Pen _PenFace;
        private readonly ReaderWriterLockSlim _CacheLock = new ReaderWriterLockSlim();
        System.Threading.CancellationTokenSource _CancellationTokenSource = new System.Threading.CancellationTokenSource();
        /// <summary>
        /// 准备注册的人脸的序号
        /// </summary>
        int _RegisterIndex = -1;
        /// <summary>
        /// 准备注册的人脸特征值
        /// </summary>
        byte[] _RegisterFeatureData = null;


        Dictionary<int, string> _MatchId = new Dictionary<int, string>();
        Dictionary<int, Rectangle> _MatchRect = new Dictionary<int, Rectangle>();

        public Main()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            //获取摄像头参数
            _CameraPara = Common.CameraPara.GetPara();
            if (!_CameraPara.HasVideoDevice)
            {
                MessageBox.Show("没有检测到摄像头");
                this.Close();
                return;
            }
            this.VideoPlayer.VideoSource = _CameraPara.VideoSource;
            this.VideoPlayer.Start();

            _RateH = 1.0F * this.VideoPlayer.Height / this._CameraPara.FrameHeight;
            _RateW = 1.0F * this.VideoPlayer.Width / this._CameraPara.FrameWidth;

            _FontId = new Font(this.Font.FontFamily, this.Font.Size / System.Math.Max(_RateH, _RateW));


            _PenFace = new Pen(Color.Yellow, 1);
            _PenFace.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
            _PenFace.DashPattern = new float[] { 5, 5 };
            if (!ArcFace.Api.Init(out string msg, AppId, DKey, RKey))
            {
                MessageBox.Show(msg);
                this.Close();
                return;
            }




            Task.Factory.StartNew(() =>
            {
                Task.Delay(1000).Wait();
                while (!_CancellationTokenSource.IsCancellationRequested)
                {
                    #region 200毫秒左右
                    try
                    {
                        MatchFrame();
                    }
                    catch
                    {

                    }
                    #endregion
                }
            }, _CancellationTokenSource.Token);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_CameraPara.HasVideoDevice)
            {
                _CancellationTokenSource.Cancel();
                System.Threading.Thread.Sleep(500);
                this.VideoPlayer.Stop();

                ArcFace.Api.Close();
            }
        }

        private void VideoPlayer_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.ScaleTransform(_RateW, _RateH);
            for (int i = 0; i < ArcFace.Api.FaceResults.FaceNumber; i++)
            {
                e.Graphics.DrawRectangle(_PenFace, ArcFace.Api.FaceResults[i].Rectangle);
                e.Graphics.DrawString(ArcFace.Api.FaceResults[i].ID, this._FontId, Brushes.Red, ArcFace.Api.FaceResults[i].Rectangle.Location);
            }
        }

        private void ButtonRegister_Click(object sender, EventArgs e)
        {
            if (_RegisterFeatureData == null)
            {
                MessageBox.Show("没有人脸数据，请面对摄像头并点击视频");
                return;
            }
            if (string.IsNullOrEmpty(this.TextBoxID.Text))
            {
                MessageBox.Show("请输入Id");
                this.TextBoxID.Focus();
                return;
            }

            if (ArcFace.Api.CheckID(this.TextBoxID.Text))
            {
                if (MessageBox.Show($"您要替换[{this.TextBoxID.Text}]的人脸数据吗？", "咨询", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.No)
                    return;
            }
            ArcFace.Api.AddFace(this.TextBoxID.Text, _RegisterFeatureData);
        }

        private void VideoPlayer_Click(object sender, EventArgs e)
        {

            if (ArcFace.Api.FaceResults.FaceNumber == 1)
            {
                _RegisterIndex = 0;
                return;
            }
            var p = this.VideoPlayer.PointToClient(Cursor.Position);
            var x = p.X / _RateW;
            var y = p.Y / _RateH;
            _RegisterIndex = ArcFace.Api.FaceResults.Items.IndexOf(ArcFace.Api.FaceResults.Items.Take(ArcFace.Api.FaceResults.FaceNumber).FirstOrDefault(ii => x >= ii.Rectangle.Left && x <= ii.Rectangle.Right && y >= ii.Rectangle.Top && y <= ii.Rectangle.Bottom));

            if (_RegisterIndex == -1)
            {
                MessageBox.Show("请点击人脸位置");
            }
        }

        private void VideoPlayer_MouseMove(object sender, MouseEventArgs e)
        {

            if (ArcFace.Api.FaceResults.FaceNumber == 1)
            {
                this.VideoPlayer.Cursor = Cursors.Hand;
                return;
            }
            var x = e.X / _RateW;
            var y = e.Y / _RateH;
            this.VideoPlayer.Cursor = ArcFace.Api.FaceResults.Items.IndexOf(ArcFace.Api.FaceResults.Items.Take(ArcFace.Api.FaceResults.FaceNumber).FirstOrDefault(ii => x >= ii.Rectangle.Left && x <= ii.Rectangle.Right && y >= ii.Rectangle.Top && y <= ii.Rectangle.Bottom))
              == -1 ? Cursors.Default : Cursors.Hand;

        }

        private void MatchFrame()
        {

            var img = this.VideoPlayer.GetCurrentVideoFrame();
            ArcFace.Api.FaceMatch(img, _RegisterIndex);

            if (_RegisterIndex != -1 && null != ArcFace.Api.FaceResults[_RegisterIndex].FeatureData)
            {
                this._RegisterFeatureData = ArcFace.Api.FaceResults[_RegisterIndex].FeatureData;
                this.Invoke(new Action(() =>
                {
                    this.TextBoxID.Text = ArcFace.Api.FaceResults[_RegisterIndex].ID;
                    this.groupBox1.Text = _RegisterIndex + "：" + ArcFace.Api.FaceResults[_RegisterIndex].Score;
                    using (var g = Graphics.FromImage(img))
                    {
                        g.DrawRectangle(_PenFace, ArcFace.Api.FaceResults[_RegisterIndex].Rectangle);
                    }
                    this.pictureBox1.Image = img;

                }));
                _RegisterIndex = -1;
            }
            else
            {
                img.Dispose();
            }

        }
    }
}