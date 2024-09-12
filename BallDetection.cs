using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;

namespace AutoHit
{
	public class BallDetection
	{
		private Image<Bgr, byte> prevFrame;
		private int motionThreshold = 1;
		private int minWhiteArea = 10;

		public bool DetectPitch(Bitmap croppedScreen, out Point clickPoint)
		{
			Image<Bgr, byte> currentFrame = BitmapToImage(croppedScreen);
			clickPoint = Point.Empty;

			if (prevFrame != null)
			{
				Image<Gray, byte> grayPrevFrame = prevFrame.Convert<Gray, byte>();
				Image<Gray, byte> grayCurrentFrame = currentFrame.Convert<Gray, byte>();
				Image<Gray, byte> motion = grayPrevFrame.AbsDiff(grayCurrentFrame).ThresholdBinary(new Gray(20), new Gray(255));

				if (CvInvoke.CountNonZero(motion) > motionThreshold)
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
								Rectangle boundingBox = CvInvoke.BoundingRectangle(contours[0]);
								int boundingBoxArea = boundingBox.Width * boundingBox.Height;

								if (boundingBoxArea >= 60)
								{
									clickPoint = new Point(boundingBox.X + boundingBox.Width / 2, boundingBox.Y + boundingBox.Height / 2);
									return true;
								}
							}
						}
					}
				}
			}

			prevFrame = currentFrame.Copy();
			return false;
		}

		private Image<Bgr, byte> BitmapToImage(Bitmap bitmap)
		{
			Mat mat = new Mat();
			bitmap.Save("temp.bmp");
			mat = CvInvoke.Imread("temp.bmp", Emgu.CV.CvEnum.ImreadModes.Color);
			Image<Bgr, byte> image = mat.ToImage<Bgr, byte>();

			return image;
		}
	}
}
