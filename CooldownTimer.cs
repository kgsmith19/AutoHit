using System;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace AutoHit
{
	public class CooldownTimer
	{
		private Timer cooldownTimer;
		private bool isCooldownActive;
		private int cooldownInterval;
		private Action onCooldownComplete;

		public CooldownTimer(int interval, Action onCooldownComplete)
		{
			this.cooldownInterval = interval;
			this.onCooldownComplete = onCooldownComplete;
			InitializeTimer();
		}

		private void InitializeTimer()
		{
			cooldownTimer = new Timer();
			cooldownTimer.Interval = cooldownInterval;
			cooldownTimer.Tick += CooldownTimer_Tick;
		}

		private void CooldownTimer_Tick(object sender, EventArgs e)
		{
			Stop();
			onCooldownComplete?.Invoke();
		}

		public void Start()
		{
			if (isCooldownActive) return;
			isCooldownActive = true;
			cooldownTimer.Start();
		}

		public void Stop()
		{
			isCooldownActive = false;
			cooldownTimer.Stop();
		}

		public bool IsCooldownActive => isCooldownActive;
	}
}
