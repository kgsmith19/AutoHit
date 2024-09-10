using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AutoHit
{
	public partial class MainForm : Form
	{
		private bool isWatching = false;
		private bool isAutoHitEnabled = false;  // Flag for auto-hit functionality
		private int clickCount = 0;  // Counter for clicks
		private Timer screenWatcherTimer;
		private Rectangle watchArea;  // Define the watched area
		private Image<Bgr, byte> baseballTemplate;  // Store the baseball template for matching
		private Image<Bgr, byte> prevFrame;  // Store the previous frame for motion detection
		private int motionThreshold = 50;  // Motion detection sensitivity threshold
		private int minWhiteArea = 500;  // Minimum white area threshold to detect the baseball

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

		private const int MOUSEEVENTF_LEFTDOWN = 0x02;
		private const int MOUSEEVENTF_LEFTUP = 0x04;

		public MainForm()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.lblStatus = new System.Windows.Forms.Label();
			this.btnToggleWatcher = new System.Windows.Forms.Button();
			this.btnStartAutoHit = new System.Windows.Forms.Button();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			this.lblPictureTracking = new System.Windows.Forms.Label();
			this.lblClickCount = new System.Windows.Forms.Label();  // Label for click count
			this.checkBoxAllowClicks = new System.Windows.Forms.CheckBox();  // Checkbox for allowing clicks

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

			// 
			// checkBoxAllowClicks
			// 
			this.checkBoxAllowClicks.AutoSize = true;
			this.checkBoxAllowClicks.Location = new System.Drawing.Point(12, 160);
			this.checkBoxAllowClicks.Name = "checkBoxAllowClicks";
			this.checkBoxAllowClicks.Size = new System.Drawing.Size(80, 17);
			this.checkBoxAllowClicks.TabIndex = 6;
			this.checkBoxAllowClicks.Text = "Allow Clicks";
			this.checkBoxAllowClicks.Checked = true;  // Default to checked
			this.checkBoxAllowClicks.UseVisualStyleBackColor = true;

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
			this.Controls.Add(this.checkBoxAllowClicks);  // Add allow clicks checkbox to form
			this.Name = "MainForm";
			this.Text = "Screen Watcher";
			this.ResumeLayout(false);
			this.PerformLayout();

			screenWatcherTimer = new Timer();
			screenWatcherTimer.Interval = 100; // Adjust as needed
			screenWatcherTimer.Tick += ScreenWatcher_Tick;

			// Load the baseball template from the uploaded image file
			baseballTemplate = new Image<Bgr, byte>(@"C:\\Test.PNG");

			// Define the watched area (smaller region within the screen)
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			watchArea = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 150, 300, 300);  // Watched area
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
			checkBoxAllowClicks.Checked = true;  // Ensure allow clicks is checked when auto-hit starts
			lblStatus.Text = "Auto Hit Enabled";
		}

		private void ScreenWatcher_Tick(object sender, EventArgs e)
		{
			// Capture the screen
			Bitmap screenCapture = CaptureScreen();

			// Display the captured screen in the PictureBox (for visualization)
			pictureBox.Image = screenCapture;  // Display the captured screen

			if (isAutoHitEnabled && checkBoxAllowClicks.Checked)
			{
				// Crop the screen capture to the watched area
				Bitmap croppedScreen = CropBitmap(screenCapture, watchArea);

				// Convert Bitmap to Image<Bgr, byte> for Emgu CV processing
				Image<Bgr, byte> currentFrame = BitmapToImage(croppedScreen);

				// Step 1: Detect motion
				if (prevFrame != null)
				{
					// Convert both prevFrame and currentFrame to grayscale
					Image<Gray, byte> grayPrevFrame = prevFrame.Convert<Gray, byte>();
					Image<Gray, byte> grayCurrentFrame = currentFrame.Convert<Gray, byte>();

					// Calculate the difference between the grayscale frames
					Image<Gray, byte> motion = grayPrevFrame.AbsDiff(grayCurrentFrame);

					// Threshold the motion to remove noise
					motion = motion.ThresholdBinary(new Gray(30), new Gray(255));

					// Check if motion is detected by calculating the non-zero pixels
					int motionArea = CvInvoke.CountNonZero(motion);

					if (motionArea > motionThreshold)
					{
						// Step 2: Apply color filtering (white ball detection)
						Image<Hsv, byte> hsvImage = currentFrame.Convert<Hsv, byte>();
						Image<Gray, byte> whiteMask = hsvImage.InRange(new Hsv(0, 0, 180), new Hsv(180, 30, 255));  // Filter for white

						// Filter out small areas (noise)
						CvInvoke.Erode(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 2, BorderType.Default, new MCvScalar(0));
						CvInvoke.Dilate(whiteMask, whiteMask, null, new System.Drawing.Point(-1, -1), 2, BorderType.Default, new MCvScalar(0));

						// Detect if the white area matches the expected baseball size
						if (CvInvoke.CountNonZero(whiteMask) > minWhiteArea)
						{
							// Baseball detected! Simulate two clicks at the matched location
							SimulateMouseClick(watchArea.X + 100, watchArea.Y + 100);  // You can adjust the click location
							clickCount++;
							lblClickCount.Text = $"Number of clicks: {clickCount}";

							// Uncheck the allow clicks checkbox after the click
							checkBoxAllowClicks.Checked = false;
						}
					}
				}

				// Store the current frame as the previous frame for the next iteration
				prevFrame = currentFrame.Copy();
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

		// Method to detect the baseball using template matching
		private void DetectBaseballWithTemplateMatching(Image<Bgr, byte> screenImage, Image<Gray, byte> whiteMask)
		{
			// Ensure the template and screen image are valid and initialized
			if (baseballTemplate == null || baseballTemplate.Width == 0 || baseballTemplate.Height == 0)
			{
				MessageBox.Show("Error: Baseball template is not properly initialized.");
				return;
			}

			if (screenImage == null || screenImage.Width == 0 || screenImage.Height == 0)
			{
				MessageBox.Show("Error: Screen image is not valid.");
				return;
			}

			if (screenImage.Width < baseballTemplate.Width || screenImage.Height < baseballTemplate.Height)
			{
				MessageBox.Show("Error: Template is larger than the screen image, cannot perform template matching.");
				return;
			}

			// Resize template to fit within the screenImage if necessary
			Image<Bgr, byte> resizedTemplate = baseballTemplate.Resize(screenImage.Width, screenImage.Height, Emgu.CV.CvEnum.Inter.Linear);

			try
			{
				// Perform template matching using the masked image
				using (Image<Gray, float> result = screenImage.MatchTemplate(resizedTemplate, TemplateMatchingType.CcoeffNormed))
				{
					double[] minVal, maxVal;
					Point[] minLoc, maxLoc;
					result.MinMax(out minVal, out maxVal, out minLoc, out maxLoc);

					// Set a threshold for matching confidence (adjust as needed)
					if (maxVal[0] >= 0.8)  // Confidence threshold
					{
						// Baseball detected! Simulate two clicks at the matched location
						SimulateMouseClick(maxLoc[0].X + watchArea.X, maxLoc[0].Y + watchArea.Y);

						// Increment click count and update the label
						clickCount++;
						lblClickCount.Text = $"Number of clicks: {clickCount}";

						// Uncheck the allow clicks checkbox after the click
						checkBoxAllowClicks.Checked = false;
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error during template matching: {ex.Message}");
			}
		}


		// Helper method to convert Bitmap to Mat
		private Mat BitmapToMat(Bitmap bitmap)
		{
			Mat mat = new Mat();
			bitmap.Save("temp.bmp");
			mat = CvInvoke.Imread("temp.bmp", Emgu.CV.CvEnum.ImreadModes.Color);
			return mat;
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
		private void SimulateMouseClick(int x, int y)
		{
			Cursor.Position = new Point(x, y);
			mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
			System.Threading.Thread.Sleep(100); // Small delay for second click
			mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
		}

		private System.Windows.Forms.Label lblStatus;
		private System.Windows.Forms.Button btnToggleWatcher;
		private System.Windows.Forms.Button btnStartAutoHit;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Label lblPictureTracking;
		private System.Windows.Forms.Label lblClickCount;  // Label for displaying the number of clicks
		private System.Windows.Forms.CheckBox checkBoxAllowClicks;  // Checkbox for allowing clicks
	}
}
