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
		private Rectangle watchArea;  // Define the watched area
		private Image<Bgr, byte> baseballTemplate;  // Store the baseball template for matching
		private Image<Bgr, byte> prevFrame;  // Store the previous frame for motion detection
		private int motionThreshold = 10;  // Motion detection sensitivity threshold was 50
		private int minWhiteArea = 10;  // Minimum white area threshold to detect the baseball was 500

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

		private const int MOUSEEVENTF_LEFTDOWN = 0x02;
		private const int MOUSEEVENTF_LEFTUP = 0x04;

		// Add variables to store the last click position
		private int lastClickX = -1;
		private int lastClickY = -1;

		private Timer cooldownTimer;  // Timer for 3-second cooldown
		private bool isCooldownActive = false;  // Flag to check if cooldown is active

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
			this.Name = "MainForm";
			this.Text = "Screen Watcher";
			this.ResumeLayout(false);
			this.PerformLayout();

			screenWatcherTimer = new Timer();
			screenWatcherTimer.Interval = 5; // Adjust as needed was 100
			screenWatcherTimer.Tick += ScreenWatcher_Tick;

			// Load the baseball template from the uploaded image file
			baseballTemplate = new Image<Bgr, byte>(@"C:\\Test.PNG");

			// Define the watched area (smaller region within the screen)
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			watchArea = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 150, 300, 300);  // Watched area

			cooldownTimer = new Timer();
			cooldownTimer.Interval = 4000;  // Set to 3 seconds (3000 milliseconds)
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
			// Check if cooldown is active, if so, skip this cycle
			if (isCooldownActive)
			{
				lblStatus.Text = "Cooldown in progress...";
				return;  // Skip further processing if cooldown is active
			}

			// Capture the screen
			Bitmap screenCapture = CaptureScreen();

			// Display the captured screen in the PictureBox (for visualization)
			pictureBox.Image = screenCapture;  // Display the captured screen

			// If clicks are not allowed, continue to display the red dot at the last known position
			if (lastClickX != -1 && lastClickY != -1)
			{
				using (Graphics g = Graphics.FromImage(screenCapture))
				{
					// Redraw the red dot at the last click location
					g.FillEllipse(Brushes.Red, lastClickX - 5, lastClickY - 5, 10, 10);
				}

				// Update the PictureBox with the updated screen
				pictureBox.Image = screenCapture;
				pictureBox.Refresh();
			}

			if (isAutoHitEnabled)
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

					// Threshold the motion to remove noise and detect earlier
					motion = motion.ThresholdBinary(new Gray(20), new Gray(255)); // More sensitive motion detection

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
							// Step 3: Find the bounding box of the white ball
							using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
							{
								CvInvoke.FindContours(whiteMask, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);

								if (contours.Size > 0)
								{
									// Get the largest contour, assuming it's the ball
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

									// Get the bounding box of the largest contour (the white ball)
									Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[largestContourIndex]);

									// Calculate the center of the bounding box
									int centerX = boundingBox.X + boundingBox.Width / 2;
									int centerY = boundingBox.Y + boundingBox.Height / 2;

									// Convert to absolute screen position relative to the watched area
									int absoluteX = watchArea.X + centerX;
									int absoluteY = watchArea.Y + centerY;

									// Ensure the click location is within bounds of the croppedScreen
									if (centerX >= 0 && centerX < croppedScreen.Width && centerY >= 0 && centerY < croppedScreen.Height)
									{
										// Simulate the mouse click at the center of the ball
										SimulateMouseClick(absoluteX, absoluteY);   // Now clicks directly on the ball
										clickCount++;
										lblClickCount.Text = $"Number of clicks: {clickCount}";

										// Store the last click position
										lastClickX = absoluteX;
										lastClickY = absoluteY;

										// Draw a red dot on the cropped screen
										using (Graphics g = Graphics.FromImage(croppedScreen))
										{
											// Draw a red circle at the click location on the croppedScreen
											g.FillEllipse(Brushes.Red, centerX - 5, centerY - 5, 50, 50);  // Red dot of size 10x10
										}

										// Now we copy the cropped screen back into the main screen capture
										using (Graphics g = Graphics.FromImage(screenCapture))
										{
											g.DrawImage(croppedScreen, watchArea);  // Place the modified cropped image back in the original screen capture
										}

										// Cooldown starts here after the click
										StartCooldown();
									}
									else
									{
										Console.WriteLine("Click location is out of bounds.");
									}
								}
							}
						}
					}
				}

				// Store the current frame as the previous frame for the next iteration
				prevFrame = currentFrame.Copy();

				// Force PictureBox to update with the screen capture that includes the red dot
				pictureBox.Image = screenCapture;
				pictureBox.Refresh();  // Force refresh to ensure the dot is visible
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
			// Simulate the mouse click
			Cursor.Position = new Point(x, y);
			mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, 0);
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
			lblStatus.Text = "";
		}

		private System.Windows.Forms.Label lblStatus;
		private System.Windows.Forms.Button btnToggleWatcher;
		private System.Windows.Forms.Button btnStartAutoHit;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Label lblPictureTracking;
		private System.Windows.Forms.Label lblClickCount;  // Label for displaying the number of clicks
	}
}