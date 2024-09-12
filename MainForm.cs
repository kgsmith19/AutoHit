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
		private bool isWatching = false;
		private bool isAutoHitEnabled = false;  // Flag for auto-hit functionality
		private int clickCount = 0;  // Counter for clicks
		private Timer screenWatcherTimer;
		private Rectangle watchArea;  // Define the watched areas
		private Image<Bgr, byte> prevFrame;  // Store the previous frame for motion detection
		private int motionThreshold = 1;  // Motion detection sensitivity threshold
		private int minWhiteArea = 1;  // Minimum white area threshold to detect the baseball
		private IntPtr gameWindowHandle; // Store the handle of the game window

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

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

		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

		private const int INPUT_MOUSE = 0;
		private const uint MOUSEEVENTF_MOVE = 0x0001;
		private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
		private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
		private const uint MOUSEEVENTF_LEFTUP = 0x0004;

		// Add variables to store the last click position
		private int lastClickX = -1;
		private int lastClickY = -1;

		private Timer cooldownTimer;  // Timer for 3-second cooldown
		private bool isCooldownActive = false;  // Flag to check if cooldown is active

		public MainForm()
		{
			InitializeComponent();

			// Find the game window handle by window title (replace with your game's window title)
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
			this.lblClickCount = new System.Windows.Forms.Label();  // Label for click count

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
			// btnToggleWatcher
			// 
			this.btnToggleWatcher.Location = new System.Drawing.Point(12, 50);
			this.btnToggleWatcher.Name = "btnToggleWatcher";
			this.btnToggleWatcher.Size = new System.Drawing.Size(150, 23);
			this.btnToggleWatcher.TabIndex = 1;
			this.btnToggleWatcher.Text = "Toggle Screen Capture";
			this.btnToggleWatcher.UseVisualStyleBackColor = true;
			this.btnToggleWatcher.Click += new System.EventHandler(this.btnToggleWatcher_Click);

			// 
			// btnStartAutoHit
			// 
			this.btnStartAutoHit.Location = new System.Drawing.Point(12, 90);
			this.btnStartAutoHit.Name = "btnStartAutoHit";
			this.btnStartAutoHit.Size = new System.Drawing.Size(150, 23);
			this.btnStartAutoHit.TabIndex = 2;
			this.btnStartAutoHit.Text = "Start Auto Hit";
			this.btnStartAutoHit.UseVisualStyleBackColor = true;
			this.btnStartAutoHit.Click += new System.EventHandler(this.btnStartAutoHit_Click);

			// 
			// pictureBox
			// 
			this.pictureBox.Location = new System.Drawing.Point(200, 50);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(400, 300);  // Adjust size based on your needs
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

			this.lblPitchDetected = new System.Windows.Forms.Label();
			this.lblPitchDetected.AutoSize = true;
			this.lblPitchDetected.Location = new System.Drawing.Point(12, 170);
			this.lblPitchDetected.Name = "lblPitchDetected";
			this.lblPitchDetected.Size = new System.Drawing.Size(85, 13);
			this.lblPitchDetected.TabIndex = 6;
			this.lblPitchDetected.Text = "";  // Start empty

			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(630, 400);
			this.Controls.Add(this.lblStatus);
			this.Controls.Add(this.btnToggleWatcher);
			this.Controls.Add(this.btnStartAutoHit);
			this.Controls.Add(this.pictureBox);
			this.Controls.Add(this.lblPictureTracking);
			this.Controls.Add(this.lblClickCount);  // Add click count label to form
			this.Controls.Add(this.lblPitchDetected);
			this.Name = "MainForm";
			this.Text = "Screen Watcher";
			this.ResumeLayout(false);
			this.PerformLayout();

			screenWatcherTimer = new Timer();
			screenWatcherTimer.Interval = 5; // Adjust as needed
			screenWatcherTimer.Tick += ScreenWatcher_Tick;

			// Define the watched area (smaller region within the screen)
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			//watchArea = new Rectangle(bounds.Width / 2 - 90, bounds.Height / 2 - 90, 180, 180);  // Watched area
			watchArea = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 150, 300, 300);

			cooldownTimer = new Timer();
			cooldownTimer.Interval = 5000;  // Set to 4 seconds (adjust as needed)
			cooldownTimer.Tick += CooldownTimer_Tick;  // Event handler for when the cooldown ends
		}

		// Button to toggle screen capture (for preview only, no auto hit)
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

		// Button to start auto hit (enables mouse events)
		private void btnStartAutoHit_Click(object sender, EventArgs e)
		{
			isAutoHitEnabled = true;
			lblStatus.Text = "Auto Hit Enabled";
		}

		private void ScreenWatcher_Tick(object sender, EventArgs e)
		{
			if (isCooldownActive) return;  // Skip if cooldown is active

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

						CvInvoke.Erode(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));  // Reduced to 1 iteration
						CvInvoke.Dilate(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));  // Reduced to 1 iteration

						if (CvInvoke.CountNonZero(whiteMask) > minWhiteArea)
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
									int boundingBoxArea = boundingBox.Width * boundingBox.Height;

									if (boundingBoxArea > 100)
										return;

									// Zone 1: Detect motion but no click yet
									if (boundingBoxArea < 50)  // Adjust threshold for Zone 1
									{
										lblPitchDetected.Text = $"Zone 1 Pitch detected {boundingBoxArea.ToString()}";

									}
									lblPitchDetected.Text = $"Pitch detected {boundingBoxArea.ToString()}";
									//Zone 2: middle of path
									if (boundingBoxArea >=70)  // Adjust threshold for Zone 1
									{
										lblPitchDetected.Text = $"Zone 2 Pitch detected {boundingBoxArea.ToString()}";
										int centerX = boundingBox.X + boundingBox.Width / 2;
										int centerY = boundingBox.Y + boundingBox.Height / 2;
										int absoluteX = watchArea.X + centerX;
										int absoluteY = watchArea.Y + centerY;

										SimulateMouseClick(absoluteX, absoluteY, gameWindowHandle);
										clickCount++;
										lblClickCount.Text = $"Number of clicks: {clickCount}";

										lastClickX = absoluteX;
										lastClickY = absoluteY;

										StartCooldown();
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
			// Convert Bitmap to Mat (matrix format used by Emgu CV)
			Mat mat = new Mat();
			bitmap.Save("temp.bmp");  // Save the Bitmap temporarily
			mat = CvInvoke.Imread("temp.bmp", Emgu.CV.CvEnum.ImreadModes.Color);  // Read the temporary file into a Mat

			// Convert Mat to Image<Bgr, byte>
			Image<Bgr, byte> image = mat.ToImage<Bgr, byte>();

			return image;
		}

		// Method to crop the bitmap to the watched area
		private Bitmap CropBitmap(Bitmap source, Rectangle section)
		{
			Bitmap croppedBitmap = new Bitmap(section.Width, section.Height);
			using (Graphics g = Graphics.FromImage(croppedBitmap))
			{
				g.DrawImage(source, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height),
					section, GraphicsUnit.Pixel);
			}
			return croppedBitmap;
		}

		private Bitmap CaptureScreen()
		{
			// Define the area of the screen you want to capture (full screen in this case)
			Rectangle bounds = Screen.PrimaryScreen.Bounds;

			// Create a Bitmap with the same size as the screen
			Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

			using (Graphics g = Graphics.FromImage(bitmap))
			{
				// Capture the screen content into the bitmap
				g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

				// Draw a red border to visually indicate the watched area
				Pen borderPen = new Pen(Color.Red, 5); // Red border with 5-pixel thickness

				// Draw the border around the watched area
				g.DrawRectangle(borderPen, watchArea);
			}

			return bitmap;
		}

		// Method to simulate mouse clicks
		private void SimulateMouseClick(int x, int y, IntPtr gameWindowHandle)
		{
			// Bring the game window to the foreground before clicking
			SetForegroundWindow(gameWindowHandle);

			// Get the game window's position
			RECT gameWindowRect = new RECT();
			GetWindowRect(gameWindowHandle, ref gameWindowRect);

			// Calculate the relative position within the game window
			int relativeX = x - gameWindowRect.Left;
			int relativeY = y - gameWindowRect.Top;

			// Set up the INPUT structure for moving the mouse and clicking
			INPUT[] inputs = new INPUT[2];

			// First input for moving the mouse (relative to the game window)
			inputs[0].type = INPUT_MOUSE;
			inputs[0].u.mi = new MOUSEINPUT
			{
				dx = relativeX * 65535 / (gameWindowRect.Right - gameWindowRect.Left),   // Normalize to 0 - 65535
				dy = relativeY * 65535 / (gameWindowRect.Bottom - gameWindowRect.Top),  // Normalize to 0 - 65535
				dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
			};

			// Second input for left mouse click (down and up)
			inputs[1].type = INPUT_MOUSE;
			inputs[1].u.mi = new MOUSEINPUT
			{
				dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP
			};

			// Send the inputs
			SendInput((uint)inputs.Length, inputs, INPUT.Size);
		}
		private void StartCooldown()
		{
			// Set cooldown flag before starting cooldown
			isCooldownActive = true;

			// Start the cooldown timer for 3 seconds
			cooldownTimer.Start();
		}

		private void CooldownTimer_Tick(object sender, EventArgs e)
		{
			isCooldownActive = false;  // Cooldown is over, allow clicks again
			cooldownTimer.Stop();  // Stop the cooldown timer
			lblStatus.Text = "Auto Hit Enabled";
		}

		private System.Windows.Forms.Label lblStatus;
		private System.Windows.Forms.Button btnToggleWatcher;
		private System.Windows.Forms.Button btnStartAutoHit;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Label lblPictureTracking;
		private System.Windows.Forms.Label lblClickCount;
		private Label lblPitchDetected;  // Label to indicate pitch detection
	}
}
