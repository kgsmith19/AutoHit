using System;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;


namespace AutoHit
{
	public class ScreenWatcher
	{
		private Timer screenWatcherTimer;
		private ScreenCaptureUtility screenCaptureUtility;
		private Rectangle watchArea;
		private Action<Bitmap> onScreenUpdate;

		public ScreenWatcher(Rectangle watchArea, int interval, Action<Bitmap> onScreenUpdate)
		{
			this.watchArea = watchArea;
			this.onScreenUpdate = onScreenUpdate;
			screenCaptureUtility = new ScreenCaptureUtility();

			screenWatcherTimer = new Timer();
			screenWatcherTimer.Interval = interval;
			screenWatcherTimer.Tick += ScreenWatcher_Tick;
		}

		public void StartWatching()
		{
			screenWatcherTimer.Start();
		}

		public void StopWatching()
		{
			screenWatcherTimer.Stop();
		}

		private void ScreenWatcher_Tick(object sender, EventArgs e)
		{
			Bitmap screenCapture = screenCaptureUtility.CaptureScreen(watchArea);
			Bitmap croppedScreen = screenCaptureUtility.CropBitmap(screenCapture, watchArea);

			onScreenUpdate?.Invoke(croppedScreen);
		}
	}
}
