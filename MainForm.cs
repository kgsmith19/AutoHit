using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Emgu.CV.Util;

namespace AutoHit
{
	public partial class MainForm : Form
	{

		//Latest with this program...
		//Poor with detecting the ball being pitched.  Can detect a homer run derby pitch like 7/10.
		//What's even worse is when it detects it the accuracy is terrible... I'm now think the code isn't moving the mouse at all. I think it is 
		//calculating the cooridinates correctly but not clicking on the screen where it should for some reason.
		
		//Steps for the future - Get the detection better. Get the click to actually click the coordinates for the ball.
		private bool isWatching = false;
		private bool isAutoHitEnabled = false;
		private int clickCount = 0;
		private Timer screenWatcherTimer;
		private Rectangle watchArea;
		private Image<Bgr, byte> prevFrame;
		private int motionThreshold = 1;
		private int minWhiteArea = 10;
		private IntPtr gameWindowHandle;
		private Timer cooldownTimer;
		private bool isCooldownActive = false;

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct INPUT
		{
			public uint type;
			public InputUnion u;
			public static int Size => Marshal.SizeOf(typeof(INPUT));
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct InputUnion
		{
			[FieldOffset(0)] public MOUSEINPUT mi;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public uint mouseData;
			public uint dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		private const int INPUT_MOUSE = 0;
		private const uint MOUSEEVENTF_MOVE = 0x0001;
		private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
		private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
		private const uint MOUSEEVENTF_LEFTUP = 0x0004;

		private Label lblPitchDetected; // Label for pitch detection

		public MainForm()
		{
			InitializeComponent();

			gameWindowHandle = FindWindow(null, "Roblox");
			if (gameWindowHandle == IntPtr.Zero)
			{
				MessageBox.Show("Could not find the game window. Please make sure the game is running.");
			}
		}

		private void InitializeComponent()
		{
			this.lblStatus = new System.Windows.Forms.Label();
			this.btnToggleWatcher = new System.Windows.Forms.Button();
			this.btnStartAutoHit = new System.Windows.Forms.Button();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			this.lblPictureTracking = new System.Windows.Forms.Label();
			this.lblClickCount = new System.Windows.Forms.Label();
			this.lblPitchDetected = new System.Windows.Forms.Label();

			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
			this.SuspendLayout();

			// lblStatus
			this.lblStatus.AutoSize = true;
			this.lblStatus.Location = new System.Drawing.Point(12, 9);
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(80, 13);
			this.lblStatus.Text = "Not watching.";

			// btnToggleWatcher
			this.btnToggleWatcher.Location = new System.Drawing.Point(12, 50);
			this.btnToggleWatcher.Size = new System.Drawing.Size(150, 23);
			this.btnToggleWatcher.Text = "Toggle Screen Capture";
			this.btnToggleWatcher.UseVisualStyleBackColor = true;
			this.btnToggleWatcher.Click += new System.EventHandler(this.btnToggleWatcher_Click);

			// btnStartAutoHit
			this.btnStartAutoHit.Location = new System.Drawing.Point(12, 90);
			this.btnStartAutoHit.Size = new System.Drawing.Size(150, 23);
			this.btnStartAutoHit.Text = "Start Auto Hit";
			this.btnStartAutoHit.UseVisualStyleBackColor = true;
			this.btnStartAutoHit.Click += new System.EventHandler(this.btnStartAutoHit_Click);

			// pictureBox
			this.pictureBox.Location = new System.Drawing.Point(200, 50);
			this.pictureBox.Size = new System.Drawing.Size(400, 300);
			this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;

			// lblPictureTracking
			this.lblPictureTracking.AutoSize = true;
			this.lblPictureTracking.Location = new System.Drawing.Point(200, 30);
			this.lblPictureTracking.Text = "Preview Screen";

			// lblClickCount
			this.lblClickCount.AutoSize = true;
			this.lblClickCount.Location = new System.Drawing.Point(12, 130);
			this.lblClickCount.Text = "Number of clicks: 0";

			// lblPitchDetected
			this.lblPitchDetected.AutoSize = true;
			this.lblPitchDetected.Location = new System.Drawing.Point(12, 170);
			this.lblPitchDetected.Text = "";  // Start empty

			// MainForm
			this.ClientSize = new System.Drawing.Size(630, 400);
			this.Controls.Add(this.lblStatus);
			this.Controls.Add(this.btnToggleWatcher);
			this.Controls.Add(this.btnStartAutoHit);
			this.Controls.Add(this.pictureBox);
			this.Controls.Add(this.lblPictureTracking);
			this.Controls.Add(this.lblClickCount);
			this.Controls.Add(this.lblPitchDetected);
			this.Text = "Screen Watcher";
			this.ResumeLayout(false);
			this.PerformLayout();

			screenWatcherTimer = new Timer();
			screenWatcherTimer.Interval = 5;
			screenWatcherTimer.Tick += ScreenWatcher_Tick;

			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			watchArea = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 150, 300, 300);

			cooldownTimer = new Timer();
			cooldownTimer.Interval = 4000;
			cooldownTimer.Tick += CooldownTimer_Tick;
		}

		private void btnToggleWatcher_Click(object sender, EventArgs e)
		{
			isWatching = !isWatching;
			if (isWatching)
			{
				screenWatcherTimer.Start();
				lblStatus.Text = "Watching screen... (Preview Mode)";
			}
			else
			{
				screenWatcherTimer.Stop();
				lblStatus.Text = "Not watching.";
			}
		}

		private void btnStartAutoHit_Click(object sender, EventArgs e)
		{
			isAutoHitEnabled = true;
			lblStatus.Text = "Auto Hit Enabled";
		}

		private void ScreenWatcher_Tick(object sender, EventArgs e)
		{
			if (isCooldownActive) return;

			Bitmap screenCapture = CaptureScreen();
			pictureBox.Image = screenCapture;

			if (isAutoHitEnabled)
			{
				Bitmap croppedScreen = CropBitmap(screenCapture, watchArea);
				Image<Bgr, byte> currentFrame = BitmapToImage(croppedScreen);

				if (prevFrame != null)
				{
					Image<Gray, byte> grayPrevFrame = prevFrame.Convert<Gray, byte>();
					Image<Gray, byte> grayCurrentFrame = currentFrame.Convert<Gray, byte>();
					Image<Gray, byte> motion = grayPrevFrame.AbsDiff(grayCurrentFrame);
					motion = motion.ThresholdBinary(new Gray(20), new Gray(255));

					int motionArea = CvInvoke.CountNonZero(motion);

					if (motionArea > motionThreshold)
					{
						Image<Hsv, byte> hsvImage = currentFrame.Convert<Hsv, byte>();
						Image<Gray, byte> whiteMask = hsvImage.InRange(new Hsv(0, 0, 150), new Hsv(180, 50, 255));

						CvInvoke.Erode(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
						CvInvoke.Dilate(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));

						if (CvInvoke.CountNonZero(whiteMask) > 0)
						{
							using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
							{
								CvInvoke.FindContours(whiteMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

								if (contours.Size > 0)
								{
									int largestContourIndex = 0;
									double maxArea = 0;

									for (int i = 0; i < contours.Size; i++)
									{
										double area = CvInvoke.ContourArea(contours[i]);
										if (area > maxArea)
										{
											largestContourIndex = i;
											maxArea = area;
										}
									}

									Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[largestContourIndex]);
									int boundingBoxArea = boundingBox.Width  * boundingBox.Height;

									if (boundingBoxArea < 60)
									{
										lblPitchDetected.Text = "Pitch detected";
									} 
									else if (boundingBoxArea >= 60)
									{
										int centerX = boundingBox.X + boundingBox.Width / 2;
										int centerY = boundingBox.Y + boundingBox.Height / 2;

										int absoluteX = watchArea.X + centerX;
										int absoluteY = watchArea.Y + centerY;

										SimulateMouseClick(absoluteX, absoluteY, gameWindowHandle);
										clickCount++;
										lblClickCount.Text = $"Number of clicks: {clickCount}";

										StartCooldown();
										lblPitchDetected.Text = "";
									}
								}
							}
						}
					}
				}

				prevFrame = currentFrame.Copy();
				pictureBox.Image = screenCapture;
				pictureBox.Refresh();
			}
		}

		private Image<Bgr, byte> BitmapToImage(Bitmap bitmap)
		{
			Mat mat = new Mat();
			bitmap.Save("temp.bmp");
			mat = CvInvoke.Imread("temp.bmp", Emgu.CV.CvEnum.ImreadModes.Color);
			Image<Bgr, byte> image = mat.ToImage<Bgr, byte>();

			return image;
		}

		private Bitmap CropBitmap(Bitmap source, Rectangle section)
		{
			Bitmap croppedBitmap = new Bitmap(section.Width, section.Height);
			using (Graphics g = Graphics.FromImage(croppedBitmap))
			{
				g.DrawImage(source, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height), section, GraphicsUnit.Pixel);
			}
			return croppedBitmap;
		}

		private Bitmap CaptureScreen()
		{
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
				Pen borderPen = new Pen(Color.Red, 5);
				g.DrawRectangle(borderPen, watchArea);
			}

			return bitmap;
		}

		private void SimulateMouseClick(int x, int y, IntPtr gameWindowHandle)
		{
			SetForegroundWindow(gameWindowHandle);

			RECT gameWindowRect = new RECT();
			GetWindowRect(gameWindowHandle, ref gameWindowRect);

			int relativeX = x - gameWindowRect.Left;
			int relativeY = y - gameWindowRect.Top;

			if (relativeX < 0 || relativeY < 0 || relativeX > (gameWindowRect.Right - gameWindowRect.Left) || relativeY > (gameWindowRect.Bottom - gameWindowRect.Top))
			{
				Console.WriteLine("Click coordinates are out of the game window bounds.");
				return;
			}

			INPUT[] inputs = new INPUT[2];

			inputs[0].type = INPUT_MOUSE;
			inputs[0].u.mi = new MOUSEINPUT
			{
				dx = relativeX * 65535 / (gameWindowRect.Right - gameWindowRect.Left),
				dy = relativeY * 65535 / (gameWindowRect.Bottom - gameWindowRect.Top),
				dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
			};

			inputs[1].type = INPUT_MOUSE;
			inputs[1].u.mi = new MOUSEINPUT
			{
				dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP
			};

			SendInput((uint)inputs.Length, inputs, INPUT.Size);
		}

		private void StartCooldown()
		{
			isCooldownActive = true;
			cooldownTimer.Start();
		}

		private void CooldownTimer_Tick(object sender, EventArgs e)
		{
			isCooldownActive = false;
			cooldownTimer.Stop();
			lblStatus.Text = "Auto Hit Enabled";
		}

		private System.Windows.Forms.Label lblStatus;
		private System.Windows.Forms.Button btnToggleWatcher;
		private System.Windows.Forms.Button btnStartAutoHit;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Label lblPictureTracking;
		private System.Windows.Forms.Label lblClickCount;
	}
}
