using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Expression.Encoder;
using Microsoft.Expression.Encoder.Devices;
using Microsoft.Expression.Encoder.ScreenCapture;
using System.Collections.ObjectModel;

namespace ScreenCapture
{
    public partial class ScreenCapture : Form
    {
        public ScreenCapture()
        {
            InitializeComponent();
        }

        private ScreenCaptureJob job = new ScreenCaptureJob();
        private Job j = new Job();

        private void buttonStartStop_Click(object sender, EventArgs e)
        {
            if (job.Status == RecordStatus.Running || job.Status == RecordStatus.Paused)
            {
                buttonPauseResume.Enabled = false;
                buttonPauseResume.Text = "Pause";

                job.Stop();
                buttonStartStop.Text = "Saving...";
                if (job.Status != RecordStatus.Running)
                {
                    Encode();
                }
                buttonStartStop.Text = "Start";
            }
            else
            {
                StartRecording();
            }
        }

        private void StartRecording()
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                buttonPauseResume.Enabled = true;
                progressBar1.Value = 0;
                label1.Text = " ";

                buttonStartStop.Text = "Stop";


                job = new ScreenCaptureJob();

                Size workingArea = SystemInformation.WorkingArea.Size;
                //Size workingArea = SystemInformation.PrimaryMonitorSize; //Full Screen
                Rectangle captureRect = new Rectangle(0, 0, workingArea.Width - (workingArea.Width % 4), workingArea.Height - (workingArea.Height % 4));
                job.CaptureRectangle = captureRect;

                //Properties
                job.ScreenCaptureVideoProfile.FrameRate = 60;
                job.ScreenCaptureVideoProfile.Quality = 100;
                job.ScreenCaptureVideoProfile.SmoothStreaming = true;
                job.CaptureMouseCursor = true;
                job.ShowCountdown = true;
                job.ShowFlashingBoundary = true;

                job.AddAudioDeviceSource(AudioDevices());

                job.OutputPath = folderBrowserDialog1.SelectedPath + "\\";
                job.OutputScreenCaptureFileName = job.OutputPath + "ScreenCapture_" + DateTime.Today.ToString("MM-dd-yyyy ") + DateTime.Now.ToString("h.mm.ss tt") + ".xesc";

                job.Start();
            }
        }

        private EncoderDevice AudioDevices()
        {
            EncoderDevice foundDevice = null;
            Collection<EncoderDevice> audioDevices = EncoderDevices.FindDevices(EncoderDeviceType.Audio);
            try
            {
                foundDevice = audioDevices.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No audio device is found" + audioDevices[0].Name + ex.Message);
            }
            return foundDevice;
        }

        void Encode()
        {
            using (j = new Job())
            {
                MediaItem mediaItem = new MediaItem(job.OutputScreenCaptureFileName);
                var size = mediaItem.OriginalVideoSize;
                WindowsMediaOutputFormat format = new WindowsMediaOutputFormat();
                format.VideoProfile = new Microsoft.Expression.Encoder.Profiles.AdvancedVC1VideoProfile();
                format.AudioProfile = new Microsoft.Expression.Encoder.Profiles.WmaAudioProfile();
                format.VideoProfile.AspectRatio = new System.Windows.Size(16, 9);
                format.VideoProfile.AutoFit = true;
                if (size.Width > 1920 && size.Height > 1080)
                {
                    format.VideoProfile.Size = new Size(1920, 1080);
                    format.VideoProfile.Bitrate = new Microsoft.Expression.Encoder.Profiles.VariableUnconstrainedBitrate(6000);
                }
                else if (size.Width > 1280 && size.Height > 720)
                {
                    format.VideoProfile.Size = new Size(1280, 720);
                    format.VideoProfile.Bitrate = new Microsoft.Expression.Encoder.Profiles.VariableUnconstrainedBitrate(4000);
                }
                else
                {
                    format.VideoProfile.Size = new Size(size.Width, Size.Height);
                    format.VideoProfile.Bitrate = new Microsoft.Expression.Encoder.Profiles.VariableUnconstrainedBitrate(2000);
                }

                mediaItem.VideoResizeMode = VideoResizeMode.Letterbox;
                mediaItem.OutputFormat = format;
                j.MediaItems.Add(mediaItem);
                j.CreateSubfolder = false;
                j.OutputDirectory = folderBrowserDialog1.SelectedPath;
                j.EncodeProgress += new EventHandler<EncodeProgressEventArgs>(j_encodeProgress);
                j.Encode();
            }
        }

        private void j_encodeProgress(object sender, EncodeProgressEventArgs e)
        {
            string status = string.Format("{0:F1}%", e.Progress);
            label1.Text = "Step: " + e.CurrentPass + "/2 " + status;
            progressBar1.Value = Convert.ToInt16(e.Progress);
            Refresh();
        }

        private void buttonPauseResume_Click(object sender, EventArgs e)
        {
            if (job.Status == RecordStatus.Paused)
            {
                job.Resume();
                buttonPauseResume.Text = "Pause";
            }
            else
            {
                job.Pause();
                buttonPauseResume.Text = "Resume";
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (job != null)
            {
                if (job.Status == RecordStatus.Running)
                {
                    job.Stop();
                }
                job.Dispose();
            }
        }
    }
}
