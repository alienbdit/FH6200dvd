using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Axio_2016_Video_Converter
{
    public partial class MainForm : Form
    {
        private System.ComponentModel.IContainer components = null;
        private Button btnSelectFiles;
        private Button btnOutputFolder;
        private Button btnStartConversion;
        private Button btnStop;
        private TextBox txtOutputFolder;
        private ListView listViewFiles;
        private ProgressBar progressBarOverall;
        private Label lblOverallProgress;
        private Label lblCurrentFile;
        private TextBox txtFFmpegPath;
        private Button btnBrowseFFmpeg;
        private CheckBox chkDeleteOriginal;
        private ComboBox cmbHardwareAccel;
        private Label lblHardwareAccel;
        private CheckBox chkUseMaxThreads;

        private List<VideoFile> videoFiles;
        private bool isConverting = false;
        private Process currentProcess;
        private CancellationTokenSource cancellationTokenSource;

        public MainForm()
        {
            videoFiles = new List<VideoFile>();
            InitializeComponent();
            SetFFmpegPath();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "FFmpeg Video Converter - GPU & CPU Optimized";
            this.Size = new Size(800, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 560);

            // FFmpeg Path section
            Label lblFFmpeg = new Label();
            lblFFmpeg.Text = "FFmpeg Status:";
            lblFFmpeg.Location = new Point(12, 15);
            lblFFmpeg.Size = new Size(80, 23);
            this.Controls.Add(lblFFmpeg);

            txtFFmpegPath = new TextBox();
            txtFFmpegPath.Location = new Point(98, 12);
            txtFFmpegPath.Size = new Size(580, 23);
            txtFFmpegPath.ReadOnly = true; // Make it read-only since we're auto-detecting
            this.Controls.Add(txtFFmpegPath);

            btnBrowseFFmpeg = new Button();
            btnBrowseFFmpeg.Text = "Browse...";
            btnBrowseFFmpeg.Location = new Point(684, 11);
            btnBrowseFFmpeg.Size = new Size(75, 25);
            btnBrowseFFmpeg.Click += BtnBrowseFFmpeg_Click;
            this.Controls.Add(btnBrowseFFmpeg);

            // File selection
            btnSelectFiles = new Button();
            btnSelectFiles.Text = "Select Video Files";
            btnSelectFiles.Location = new Point(12, 50);
            btnSelectFiles.Size = new Size(120, 30);
            btnSelectFiles.Click += BtnSelectFiles_Click;
            this.Controls.Add(btnSelectFiles);

            // Output folder section
            Label lblOutput = new Label();
            lblOutput.Text = "Output Folder:";
            lblOutput.Location = new Point(12, 95);
            lblOutput.Size = new Size(80, 23);
            this.Controls.Add(lblOutput);

            txtOutputFolder = new TextBox();
            txtOutputFolder.Location = new Point(98, 92);
            txtOutputFolder.Size = new Size(580, 23);
            this.Controls.Add(txtOutputFolder);

            btnOutputFolder = new Button();
            btnOutputFolder.Text = "Browse...";
            btnOutputFolder.Location = new Point(684, 91);
            btnOutputFolder.Size = new Size(75, 25);
            btnOutputFolder.Click += BtnOutputFolder_Click;
            this.Controls.Add(btnOutputFolder);

            // Options
            chkDeleteOriginal = new CheckBox();
            chkDeleteOriginal.Text = "Delete original files after successful conversion";
            chkDeleteOriginal.Location = new Point(12, 125);
            chkDeleteOriginal.Size = new Size(300, 23);
            this.Controls.Add(chkDeleteOriginal);

            // Hardware Acceleration
            lblHardwareAccel = new Label();
            lblHardwareAccel.Text = "Hardware Acceleration:";
            lblHardwareAccel.Location = new Point(320, 128);
            lblHardwareAccel.Size = new Size(120, 23);
            this.Controls.Add(lblHardwareAccel);

            cmbHardwareAccel = new ComboBox();
            cmbHardwareAccel.Location = new Point(445, 125);
            cmbHardwareAccel.Size = new Size(150, 23);
            cmbHardwareAccel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHardwareAccel.Items.AddRange(new string[] {
                "Auto-Detect",
                "CPU Only (Software)",
                "NVIDIA GPU (NVENC)",
                "Intel GPU (QuickSync)",
                "AMD GPU (AMF)",
                "DXVA2 (Windows)",
                "OpenCL"
            });
            cmbHardwareAccel.SelectedIndex = 0; // Auto-Detect by default
            this.Controls.Add(cmbHardwareAccel);

            // Max CPU Threads
            chkUseMaxThreads = new CheckBox();
            chkUseMaxThreads.Text = "Use Maximum CPU Threads";
            chkUseMaxThreads.Location = new Point(605, 125);
            chkUseMaxThreads.Size = new Size(160, 23);
            chkUseMaxThreads.Checked = true; // Enabled by default
            this.Controls.Add(chkUseMaxThreads);

            // File list
            listViewFiles = new ListView();
            listViewFiles.Location = new Point(12, 180);
            listViewFiles.Size = new Size(760, 220);
            listViewFiles.View = View.Details;
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;

            listViewFiles.Columns.Add("File Name", 300);
            listViewFiles.Columns.Add("Status", 100);
            listViewFiles.Columns.Add("Progress", 100);
            listViewFiles.Columns.Add("Output File", 260);

            this.Controls.Add(listViewFiles);

            // Progress section
            lblCurrentFile = new Label();
            lblCurrentFile.Text = "Ready";
            lblCurrentFile.Location = new Point(12, 415);
            lblCurrentFile.Size = new Size(760, 23);
            lblCurrentFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(lblCurrentFile);

            progressBarOverall = new ProgressBar();
            progressBarOverall.Location = new Point(12, 445);
            progressBarOverall.Size = new Size(650, 23);
            progressBarOverall.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.Controls.Add(progressBarOverall);

            lblOverallProgress = new Label();
            lblOverallProgress.Text = "0 / 0";
            lblOverallProgress.Location = new Point(668, 448);
            lblOverallProgress.Size = new Size(100, 23);
            lblOverallProgress.TextAlign = ContentAlignment.MiddleLeft;
            lblOverallProgress.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.Controls.Add(lblOverallProgress);

            // Control buttons
            btnStartConversion = new Button();
            btnStartConversion.Text = "Start Conversion";
            btnStartConversion.Location = new Point(12, 480);
            btnStartConversion.Size = new Size(120, 35);
            btnStartConversion.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnStartConversion.Click += BtnStartConversion_Click;
            this.Controls.Add(btnStartConversion);

            btnStop = new Button();
            btnStop.Text = "Stop";
            btnStop.Location = new Point(138, 480);
            btnStop.Size = new Size(75, 35);
            btnStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnStop.Enabled = false;
            btnStop.Click += BtnStop_Click;
            this.Controls.Add(btnStop);

            this.ResumeLayout();
        }

        private void BtnBrowseFFmpeg_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "FFmpeg Executable|ffmpeg.exe|All Files|*.*";
                dialog.Title = "Select FFmpeg Executable";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtFFmpegPath.ReadOnly = false;
                    if (ValidateFFmpeg(dialog.FileName))
                    {
                        txtFFmpegPath.Text = dialog.FileName + " ✓";
                        txtFFmpegPath.BackColor = Color.LightGreen;
                        DetectHardwareAcceleration(dialog.FileName);
                    }
                    else
                    {
                        txtFFmpegPath.Text = dialog.FileName + " (Warning: Cannot validate)";
                        txtFFmpegPath.BackColor = Color.LightYellow;
                    }
                    txtFFmpegPath.ReadOnly = true;
                }
            }
        }

        private void BtnSelectFiles_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv;*.flv;*.webm;*.m4v|All Files|*.*";
                dialog.Multiselect = true;
                dialog.Title = "Select Video Files to Convert";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    AddFiles(dialog.FileNames);
                }
            }
        }

        private void BtnOutputFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select output folder for converted videos";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtOutputFolder.Text = dialog.SelectedPath;
                }
            }
        }

        private void AddFiles(string[] filePaths)
        {
            // Ensure videoFiles is initialized
            if (videoFiles == null)
                videoFiles = new List<VideoFile>();

            foreach (string filePath in filePaths)
            {
                if (!videoFiles.Any(v => v.InputPath == filePath))
                {
                    var videoFile = new VideoFile
                    {
                        InputPath = filePath,
                        FileName = Path.GetFileName(filePath),
                        Status = "Pending",
                        Progress = 0
                    };

                    videoFiles.Add(videoFile);

                    ListViewItem item = new ListViewItem(videoFile.FileName);
                    item.SubItems.Add(videoFile.Status);
                    item.SubItems.Add("0%");
                    item.SubItems.Add(""); // Output file will be set later
                    item.Tag = videoFile;

                    listViewFiles.Items.Add(item);
                }
            }

            UpdateUI();
        }

        private async void BtnStartConversion_Click(object sender, EventArgs e)
        {
            if (videoFiles.Count == 0)
            {
                MessageBox.Show("Please select video files to convert.", "No Files Selected",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputFolder.Text) || !Directory.Exists(txtOutputFolder.Text))
            {
                MessageBox.Show("Please select a valid output folder.", "Invalid Output Folder",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ffmpegPath = txtFFmpegPath.Text.Replace(" ✓", "").Replace(" (Warning: Cannot validate)", "");

            if (string.IsNullOrWhiteSpace(ffmpegPath) || ffmpegPath.Contains("not found"))
            {
                MessageBox.Show("FFmpeg executable not found. Please install FFmpeg and ensure it's in your system PATH, or use the Browse button to locate it manually.", "FFmpeg Not Found",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isConverting = true;
            cancellationTokenSource = new CancellationTokenSource();

            btnStartConversion.Enabled = false;
            btnStop.Enabled = true;
            btnSelectFiles.Enabled = false;

            progressBarOverall.Maximum = videoFiles.Count;
            progressBarOverall.Value = 0;

            try
            {
                await ConvertAllFiles(cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                lblCurrentFile.Text = "Conversion stopped by user";
            }
            finally
            {
                isConverting = false;
                btnStartConversion.Enabled = true;
                btnStop.Enabled = false;
                btnSelectFiles.Enabled = true;

                if (currentProcess != null && !currentProcess.HasExited)
                {
                    currentProcess.Kill();
                }
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }

            if (currentProcess != null && !currentProcess.HasExited)
            {
                currentProcess.Kill();
            }
        }

        private async Task ConvertAllFiles(CancellationToken cancellationToken)
        {
            int completedFiles = 0;

            foreach (var videoFile in videoFiles.Where(v => v.Status == "Pending"))
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                string outputFileName = Path.GetFileNameWithoutExtension(videoFile.FileName) + "-fh6200dvd.mp4";
                videoFile.OutputPath = Path.Combine(txtOutputFolder.Text, outputFileName);

                UpdateFileStatus(videoFile, "Converting...", 0);
                string accelMethod = cmbHardwareAccel.SelectedItem.ToString();
                lblCurrentFile.Text = $"Converting with {accelMethod}: {videoFile.FileName}";

                try
                {
                    await ConvertSingleFile(videoFile, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        UpdateFileStatus(videoFile, "Completed", 100);
                        completedFiles++;

                        // Delete original file if option is checked
                        if (chkDeleteOriginal.Checked)
                        {
                            try
                            {
                                File.Delete(videoFile.InputPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Could not delete original file {videoFile.FileName}: {ex.Message}",
                                              "Delete Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateFileStatus(videoFile, "Error", 0);
                    MessageBox.Show($"Error converting {videoFile.FileName}: {ex.Message}",
                                  "Conversion Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                progressBarOverall.Value = completedFiles;
                lblOverallProgress.Text = $"{completedFiles} / {videoFiles.Count}";
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                lblCurrentFile.Text = $"Conversion completed! {completedFiles} files processed.";
                MessageBox.Show($"Conversion completed successfully!\n{completedFiles} files processed.",
                              "Conversion Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async Task ConvertSingleFile(VideoFile videoFile, CancellationToken cancellationToken)
        {
            string ffmpegPath = txtFFmpegPath.Text.Replace(" ✓", "").Replace(" (Warning: Cannot validate)", "");
            string arguments = BuildOptimizedFFmpegCommand(videoFile);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            currentProcess = new Process { StartInfo = startInfo };

            var tcs = new TaskCompletionSource<bool>();

            currentProcess.ErrorDataReceived += (sender, e) => {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    // Parse FFmpeg progress output (works for both hardware and software encoding)
                    var timeMatch = Regex.Match(e.Data, @"time=(\d+):(\d+):(\d+\.\d+)");
                    var frameMatch = Regex.Match(e.Data, @"frame=\s*(\d+)");

                    if (timeMatch.Success || frameMatch.Success)
                    {
                        // Calculate progress based on time or frame count
                        int progress = Math.Min(95, videoFile.Progress + 2); // More frequent updates

                        this.Invoke((MethodInvoker)delegate {
                            UpdateFileStatus(videoFile, "Converting...", progress);
                        });
                    }

                    // Check for hardware acceleration status
                    if (e.Data.Contains("hwaccel") || e.Data.Contains("cuda") || e.Data.Contains("qsv") || e.Data.Contains("nvenc"))
                    {
                        this.Invoke((MethodInvoker)delegate {
                            lblCurrentFile.Text = $"Converting with GPU acceleration: {videoFile.FileName}";
                        });
                    }
                }
            };

            currentProcess.Exited += (sender, e) => {
                tcs.SetResult(currentProcess.ExitCode == 0);
            };

            currentProcess.EnableRaisingEvents = true;
            currentProcess.Start();
            currentProcess.BeginErrorReadLine();

            // Wait for completion or cancellation
            while (!currentProcess.HasExited && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                if (!currentProcess.HasExited)
                {
                    currentProcess.Kill();
                }
                throw new OperationCanceledException();
            }

            await tcs.Task;

            if (currentProcess.ExitCode != 0)
            {
                throw new Exception($"FFmpeg process failed with exit code {currentProcess.ExitCode}");
            }
        }

        private void UpdateFileStatus(VideoFile videoFile, string status, int progress)
        {
            videoFile.Status = status;
            videoFile.Progress = progress;

            foreach (ListViewItem item in listViewFiles.Items)
            {
                if (item.Tag == videoFile)
                {
                    item.SubItems[1].Text = status;
                    item.SubItems[2].Text = $"{progress}%";
                    if (!string.IsNullOrEmpty(videoFile.OutputPath))
                    {
                        item.SubItems[3].Text = Path.GetFileName(videoFile.OutputPath);
                    }
                    break;
                }
            }
        }

        private void UpdateUI()
        {
            if (videoFiles == null)
                videoFiles = new List<VideoFile>();

            btnStartConversion.Enabled = videoFiles.Count > 0 && !isConverting;
        }

        private void SetFFmpegPath()
        {
            // Try to find ffmpeg in system PATH
            string ffmpegPath = FindFFmpegInPath();

            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                txtFFmpegPath.Text = ffmpegPath;
                txtFFmpegPath.BackColor = Color.LightGreen;
                // Validate that ffmpeg works
                if (ValidateFFmpeg(ffmpegPath))
                {
                    txtFFmpegPath.Text = ffmpegPath + " ✓";
                    // Detect available hardware acceleration
                    DetectHardwareAcceleration(ffmpegPath);
                }
                else
                {
                    txtFFmpegPath.BackColor = Color.LightYellow;
                    txtFFmpegPath.Text = ffmpegPath + " (Warning: Cannot validate)";
                }
            }
            else
            {
                txtFFmpegPath.Text = "FFmpeg not found in PATH - Please browse to locate ffmpeg.exe";
                txtFFmpegPath.BackColor = Color.LightCoral;
                txtFFmpegPath.ReadOnly = false; // Allow manual input if not found
            }
        }

        private string FindFFmpegInPath()
        {
            try
            {
                // Try common names for ffmpeg
                string[] possibleNames = { "ffmpeg", "ffmpeg.exe" };

                foreach (string name in possibleNames)
                {
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = name,
                            Arguments = "-version",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };

                        using (Process process = Process.Start(startInfo))
                        {
                            process.WaitForExit(3000); // Wait max 3 seconds
                            if (process.ExitCode == 0)
                            {
                                return name; // Found working ffmpeg
                            }
                        }
                    }
                    catch
                    {
                        continue; // Try next name
                    }
                }
            }
            catch
            {
                // Ignore errors in path detection
            }

            return null;
        }

        private bool ValidateFFmpeg(string ffmpegPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath.Replace(" ✓", ""), // Remove checkmark if present
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit(3000);
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void DetectHardwareAcceleration(string ffmpegPath)
        {
            try
            {
                // Check for available encoders
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath.Replace(" ✓", ""),
                    Arguments = "-encoders",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);

                    // Clear existing items except Auto-Detect and CPU Only
                    while (cmbHardwareAccel.Items.Count > 2)
                        cmbHardwareAccel.Items.RemoveAt(2);

                    // Add detected hardware encoders
                    if (output.Contains("h264_nvenc"))
                        cmbHardwareAccel.Items.Add("NVIDIA GPU (NVENC) ✓");
                    if (output.Contains("h264_qsv"))
                        cmbHardwareAccel.Items.Add("Intel GPU (QuickSync) ✓");
                    if (output.Contains("h264_amf"))
                        cmbHardwareAccel.Items.Add("AMD GPU (AMF) ✓");
                    if (output.Contains("h264_mf"))
                        cmbHardwareAccel.Items.Add("Windows Media Foundation ✓");
                }
            }
            catch
            {
                // If detection fails, keep default options
            }
        }

        private string BuildOptimizedFFmpegCommand(VideoFile videoFile)
        {
            var args = new List<string>();

            // Hardware acceleration and input
            string hwAccel = cmbHardwareAccel.SelectedItem.ToString();

            // Add hardware acceleration for decoding
            if (hwAccel.Contains("NVIDIA") || hwAccel.Contains("NVENC"))
            {
                args.Add("-hwaccel nvdec");
                args.Add("-hwaccel_output_format cuda");
            }
            else if (hwAccel.Contains("Intel") || hwAccel.Contains("QuickSync"))
            {
                args.Add("-hwaccel qsv");
                args.Add("-hwaccel_output_format qsv");
            }
            else if (hwAccel.Contains("AMD") || hwAccel.Contains("AMF"))
            {
                args.Add("-hwaccel d3d11va");
                args.Add("-hwaccel_output_format d3d11");
            }
            else if (hwAccel.Contains("DXVA2"))
            {
                args.Add("-hwaccel dxva2");
            }

            // Input file
            args.Add($"-i \"{videoFile.InputPath}\"");

            // Threading - use maximum available cores
            if (chkUseMaxThreads.Checked)
            {
                args.Add("-threads 0"); // 0 = auto-detect and use all available cores
            }

            // Video codec and settings
            if (hwAccel.Contains("NVIDIA") || hwAccel.Contains("NVENC"))
            {
                args.Add("-c:v h264_nvenc");
                args.Add("-profile:v baseline");
                args.Add("-level 3.0");
                args.Add("-preset p1"); // Fastest preset for NVENC
                args.Add("-tune hq"); // High quality tune
                args.Add("-b:v 1200k");
                args.Add("-maxrate 1200k");
                args.Add("-bufsize 2400k");
                args.Add("-vf scale_cuda=720:480"); // GPU scaling
            }
            else if (hwAccel.Contains("Intel") || hwAccel.Contains("QuickSync"))
            {
                args.Add("-c:v h264_qsv");
                args.Add("-profile:v baseline");
                args.Add("-level 3.0");
                args.Add("-preset faster");
                args.Add("-b:v 1200k");
                args.Add("-maxrate 1200k");
                args.Add("-vf scale_qsv=720:480"); // QuickSync scaling
            }
            else if (hwAccel.Contains("AMD") || hwAccel.Contains("AMF"))
            {
                args.Add("-c:v h264_amf");
                args.Add("-profile:v baseline");
                args.Add("-rc cbr");
                args.Add("-b:v 1200k");
                args.Add("-vf scale=720:480");
            }
            else
            {
                // Software encoding with maximum performance
                args.Add("-c:v libx264");
                args.Add("-profile:v baseline");
                args.Add("-level 3.0");
                args.Add("-preset ultrafast"); // Fastest software preset
                args.Add("-tune fastdecode"); // Optimize for fast decoding
                args.Add("-b:v 1200k");
                args.Add("-vf scale=720:480");

                // Additional CPU optimizations
                args.Add("-x264-params sliced-threads=1"); // Better multi-threading
                args.Add("-x264-params sync-lookahead=0"); // Reduce latency
            }

            // Audio settings (always use software - hardware audio encoding is limited)
            args.Add("-c:a aac");
            args.Add("-b:a 128k");
            args.Add("-ac 2"); // Stereo audio

            // Global optimizations
            args.Add("-movflags +faststart"); // Optimize for streaming
            args.Add("-avoid_negative_ts make_zero"); // Avoid timestamp issues

            // Output file
            args.Add($"\"{videoFile.OutputPath}\"");

            return string.Join(" ", args);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class VideoFile
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public int Progress { get; set; }
    }

   
}