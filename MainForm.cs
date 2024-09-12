using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using Emgu.CV.Util;

namespace AutoHit
{
	public enum HitMode
	{
		AutoHit,
		SemiAutoHit
	}

	public partial class MainForm : Form
	{
		private bool isWatching = false;
		private bool isAutoHitEnabled = false;  // Flag for auto-hit functionality
		private int clickCount = 0;  // Counter for clicks
		private Timer screenWatcherTimer;
		private Rectangle watchArea;  // Define the watched area
		private int minWhiteArea = 10;  // Minimum white area threshold to detect the baseball
		private int lastClickX = -1;
		private int lastClickY = -1;

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

		private const int MOUSEEVENTF_LEFTDOWN = 0x02;
		private const int MOUSEEVENTF_LEFTUP = 0x04;

		private Label lblStatus;
		private Button btnShowScreen;
		private Button btnStartStop;
		private PictureBox pictureBox;
		private Label lblPictureTracking;
		private Label lblClickCount;

		public MainForm()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.lblStatus = new System.Windows.Forms.Label();
			this.btnShowScreen = new System.Windows.Forms.Button();
			this.btnStartStop = new System.Windows.Forms.Button();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			this.lblPictureTracking = new System.Windows.Forms.Label();
			this.lblClickCount = new System.Windows.Forms.Label();

			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
			this.SuspendLayout();

			// 
			// lblStatus
			// 
			this.lblStatus.AutoSize = true;
			this.lblStatus.Location = new System.Drawing.Point(12, 9);
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(80, 13);
			this.lblStatus.TabIndex = 0;
			this.lblStatus.Text = "Not watching.";

			// 
			// btnShowScreen
			// 
			this.btnShowScreen.Location = new System.Drawing.Point(12, 50);
			this.btnShowScreen.Name = "btnShowScreen";
			this.btnShowScreen.Size = new System.Drawing.Size(150, 23);
			this.btnShowScreen.TabIndex = 1;
			this.btnShowScreen.Text = "Show Screen";
			this.btnShowScreen.UseVisualStyleBackColor = true;
			this.btnShowScreen.Click += new System.EventHandler(this.btnShowScreen_Click);

			// 
			// btnStartStop
			// 
			this.btnStartStop.Location = new System.Drawing.Point(12, 90);
			this.btnStartStop.Name = "btnStartStop";
			this.btnStartStop.Size = new System.Drawing.Size(150, 23);
			this.btnStartStop.TabIndex = 2;
			this.btnStartStop.Text = "Start";
			this.btnStartStop.UseVisualStyleBackColor = true;
			this.btnStartStop.Enabled = false;
			this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);

			// 
			// pictureBox
			// 
			this.pictureBox.Location = new System.Drawing.Point(200, 50);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(400, 300);
			this.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBox.TabIndex = 3;

			// 
			// lblPictureTracking
			// 
			this.lblPictureTracking.AutoSize = true;
			this.lblPictureTracking.Location = new System.Drawing.Point(200, 30);
			this.lblPictureTracking.Name = "lblPictureTracking";
			this.lblPictureTracking.Size = new System.Drawing.Size(85, 13);
			this.lblPictureTracking.TabIndex = 4;
			this.lblPictureTracking.Text = "Preview Screen";

			// 
			// lblClickCount
			// 
			this.lblClickCount.AutoSize = true;
			this.lblClickCount.Location = new System.Drawing.Point(12, 130);
			this.lblClickCount.Name = "lblClickCount";
			this.lblClickCount.Size = new System.Drawing.Size(85, 13);
			this.lblClickCount.TabIndex = 5;
			this.lblClickCount.Text = "Number of clicks: 0";
			this.lblClickCount.Enabled = false;

			this.Controls.Add(this.lblStatus);
			this.Controls.Add(this.btnShowScreen);
			this.Controls.Add(this.btnStartStop);
			this.Controls.Add(this.pictureBox);
			this.Controls.Add(this.lblPictureTracking);
			this.Controls.Add(this.lblClickCount);

			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(630, 400);
			this.Name = "MainForm";
			this.Text = "Screen Watcher";

			screenWatcherTimer = new Timer();
			screenWatcherTimer.Interval = 5; // Adjust as needed
			screenWatcherTimer.Tick += ScreenWatcher_Tick;

			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			watchArea = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 150, 300, 300);

			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		private void btnShowScreen_Click(object sender, EventArgs e)
		{
			isWatching = !isWatching;
			if (isWatching)
			{
				btnShowScreen.Text = "Hide Screen";
				btnStartStop.Enabled = true;  // Enable the Start/Stop button
				lblClickCount.Enabled = true;
				screenWatcherTimer.Start();
				lblStatus.Text = "Watching screen... (Preview Mode)";
			}
			else
			{
				btnShowScreen.Text = "Show Screen";
				btnStartStop.Enabled = false;
				lblClickCount.Enabled = false;
				screenWatcherTimer.Stop();
				lblStatus.Text = "Not watching.";
			}
		}

		private void btnStartStop_Click(object sender, EventArgs e)
		{
			isAutoHitEnabled = !isAutoHitEnabled;
			if (isAutoHitEnabled)
			{
				btnStartStop.Text = "Stop";
				lblStatus.Text = "AutoHit/SemiAutoHit Enabled";
			}
			else
			{
				btnStartStop.Text = "Start";
				lblStatus.Text = "AutoHit/SemiAutoHit Disabled";
			}
		}

		private void ScreenWatcher_Tick(object sender, EventArgs e)
		{
			Bitmap screenCapture = CaptureScreen();
			pictureBox.Image = screenCapture;

			if (lastClickX != -1 && lastClickY != -1)
			{
				using (Graphics g = Graphics.FromImage(screenCapture))
				{
					g.FillEllipse(Brushes.Red, lastClickX - 5, lastClickY - 5, 10, 10);
				}

				pictureBox.Image = screenCapture;
				pictureBox.Refresh();
			}

			if (isAutoHitEnabled)
			{
				TrackBallAndClick();
			}
		}

		private void TrackBallAndClick()
		{
			Bitmap screenCapture = CaptureScreen();  // Capture the screen
			Bitmap croppedScreen = CropBitmap(screenCapture, watchArea);  // Crop to the watched area
			Image<Bgr, byte> currentFrame = BitmapToImage(croppedScreen);  // Convert to image for processing

			// Convert the image to HSV color space and create a mask for the white ball
			Image<Hsv, byte> hsvImage = currentFrame.Convert<Hsv, byte>();
			Image<Gray, byte> whiteMask = hsvImage.InRange(new Hsv(0, 0, 180), new Hsv(180, 30, 255));

			// Apply erosion and dilation to filter noise and refine the ball detection
			CvInvoke.Erode(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 2, BorderType.Default, new MCvScalar(0));
			CvInvoke.Dilate(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 2, BorderType.Default, new MCvScalar(0));

			// Check if any large enough white area is detected, which could be the ball
			if (CvInvoke.CountNonZero(whiteMask) > minWhiteArea)
			{
				using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
				{
					// Find contours in the white mask
					CvInvoke.FindContours(whiteMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

					if (contours.Size > 0)
					{
						// Get the bounding box of the first contour (largest white area)
						Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[0]);
						int centerX = boundingBox.X + boundingBox.Width / 2;
						int centerY = boundingBox.Y + boundingBox.Height / 2;
						int absoluteX = watchArea.X + centerX;
						int absoluteY = watchArea.Y + centerY;

						// Simulate a click at the detected ball position
						SimulateMouseClick(absoluteX, absoluteY);
						lastClickX = absoluteX;
						lastClickY = absoluteY;

						// Update click count for UI display
						clickCount++;
						lblClickCount.Text = $"Number of clicks: {clickCount}";
					}
				}
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

		private Bitmap CropBitmap(Bitmap source, Rectangle section)
		{
			Bitmap croppedBitmap = new Bitmap(section.Width, section.Height);
			using (Graphics g = Graphics.FromImage(croppedBitmap))
			{
				g.DrawImage(source, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height), section, GraphicsUnit.Pixel);
			}
			return croppedBitmap;
		}

		private void SimulateMouseClick(int x, int y)
		{
			Cursor.Position = new Point(x, y);
			mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
			System.Threading.Thread.Sleep(50);
			mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
		}
	}
}
