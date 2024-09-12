using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace AutoHit
{
	public class MouseClicker
	{
		private IntPtr gameWindowHandle;

		public MouseClicker(IntPtr gameWindowHandle)
		{
			this.gameWindowHandle = gameWindowHandle;
		}

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

		public void SimulateMouseClick(int x, int y)
		{
			SetForegroundWindow(gameWindowHandle);

			RECT gameWindowRect = new RECT();
			GetWindowRect(gameWindowHandle, ref gameWindowRect);

			int windowWidth = gameWindowRect.Right - gameWindowRect.Left;
			int windowHeight = gameWindowRect.Bottom - gameWindowRect.Top;

			int relativeX = x - gameWindowRect.Left;
			int relativeY = y - gameWindowRect.Top;

			if (relativeX < 0 || relativeY < 0 || relativeX > windowWidth || relativeY > windowHeight)
			{
				Console.WriteLine("Click coordinates are out of the game window bounds.");
				return;
			}

			INPUT[] inputs = new INPUT[2];

			// Move the mouse to the click position
			inputs[0].type = INPUT_MOUSE;
			inputs[0].u.mi = new MOUSEINPUT
			{
				dx = relativeX * 65535 / windowWidth,
				dy = relativeY * 65535 / windowHeight,
				dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
			};

			// Perform the left mouse click
			inputs[1].type = INPUT_MOUSE;
			inputs[1].u.mi = new MOUSEINPUT
			{
				dwFlags = MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP
			};

			// Send the input
			SendInput((uint)inputs.Length, inputs, INPUT.Size);
		}
	}
}
