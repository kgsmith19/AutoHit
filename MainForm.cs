using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoHit
{
	public partial class MainForm : Form
	{
		//This is the best the program has been. It consistently detects every pitch, but it is swinging slightly early... but it's consistently early..
		//I think if I get it to delay the click by some ms it can be on time.
		//Lastly, The click location just needs to be accuruate.  We are almost there!!!
		private bool isWatching = false;
		private bool isAutoHitEnabled = false;
		private int clickCount = 0;
		private IntPtr gameWindowHandle;

		private BallDetection ballDetection;
		private MouseClicker mouseClicker;
		private CooldownTimer cooldownTimer;
		private ScreenWatcher screenWatcher;

		public MainForm()
		{
			InitializeComponent();

			gameWindowHandle = FindWindow(null, "Roblox");
			if (gameWindowHandle == IntPtr.Zero)
			{
				MessageBox.Show("Could not find the game window. Please make sure the game is running.");
				return;
			}

			ballDetection = new BallDetection();
			mouseClicker = new MouseClicker(gameWindowHandle);

			InitializeTimers();
			InitializeScreenWatcher();
		}

		private void InitializeTimers()
		{
			cooldownTimer = new CooldownTimer(4000, OnCooldownComplete);
		}

		private void InitializeScreenWatcher()
		{
			Rectangle bounds = Screen.PrimaryScreen.Bounds;
			Rectangle watchArea = new Rectangle(bounds.Width / 2 - 150, bounds.Height / 2 - 150, 300, 300);

			screenWatcher = new ScreenWatcher(watchArea, 5, OnScreenUpdated);
		}

		private void OnCooldownComplete()
		{
			lblStatus.Text = "Auto Hit Enabled";
		}

		private void OnScreenUpdated(Bitmap croppedScreen)
		{
			if (cooldownTimer.IsCooldownActive) return;

			pictureBox.Image = croppedScreen;

			if (isAutoHitEnabled)
			{
				bool isPitchDetected = ballDetection.DetectPitch(croppedScreen, out Point clickPoint);

				if (isPitchDetected)
				{ 
					lblPitchDetected.Text = "Pitch detected";
					//125 seems to be good timing...
					System.Threading.Thread.Sleep(125);
					mouseClicker.SimulateMouseClick(clickPoint.X, clickPoint.Y);
					clickCount++;
					lblClickCount.Text = $"Number of clicks: {clickCount}";

					cooldownTimer.Start(); // Start the cooldown after clicking
					lblPitchDetected.Text = "";
				}
			}
		}

		private void btnToggleWatcher_Click(object sender, EventArgs e)
		{
			isWatching = !isWatching;
			lblStatus.Text = isWatching ? "Watching screen... (Preview Mode)" : "Not watching.";

			if (isWatching)
			{
				screenWatcher.StartWatching();
				btnStartAutoHit.Enabled = true; // Enable AutoHit button when watching is enabled
			}
			else
			{
				screenWatcher.StopWatching();
				btnStartAutoHit.Enabled = false; // Disable AutoHit button when watching is stopped
			}
		}

		private void btnStartAutoHit_Click(object sender, EventArgs e)
		{
			isAutoHitEnabled = true;
			lblStatus.Text = "Auto Hit Enabled";
		}

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

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
			this.btnStartAutoHit.Enabled = false;
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

			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
		}

		private System.Windows.Forms.Label lblStatus;
		private System.Windows.Forms.Button btnToggleWatcher;
		private System.Windows.Forms.Button btnStartAutoHit;
		private System.Windows.Forms.PictureBox pictureBox;
		private System.Windows.Forms.Label lblPictureTracking;
		private System.Windows.Forms.Label lblClickCount;
		private System.Windows.Forms.Label lblPitchDetected;
	}
}
