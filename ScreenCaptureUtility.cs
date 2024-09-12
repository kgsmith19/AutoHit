using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoHit
{
	public class ScreenCaptureUtility
	{
		public Bitmap CaptureScreen(Rectangle watchArea)
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

		public Bitmap CropBitmap(Bitmap source, Rectangle section)
		{
			Bitmap croppedBitmap = new Bitmap(section.Width, section.Height);
			using (Graphics g = Graphics.FromImage(croppedBitmap))
			{
				g.DrawImage(source, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height), section, GraphicsUnit.Pixel);
			}
			return croppedBitmap;
		}
	}
}
