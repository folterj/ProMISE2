using System;

namespace ProMISE2
{
	public class Axis
	{
		public float scale = 0;
		public float majorStepSize = 0;
		public float minorStepSize = 0;
		public int nMajorDivs = 0;
		public int nMinorDivs = 0;

		public float[] autoScaleValues = new float[] { 0.12f, 0.14f, 0.16f, 0.18f, 0.2f, 0.25f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };


		public Axis()
		{
		}

		public void calcScale(float maxval)
		{
			int exp;
			float maxval0;
			bool neg;
			float expFactor;

			if (maxval == 0)
			{
				return;
			}

			neg = (maxval < 0);
			if (neg)
			{
				maxval0 = -maxval;
			}
			else
			{
				maxval0 = maxval;
			}
			exp = (int)Math.Ceiling(Math.Log10(maxval0));
			expFactor = (float)Math.Pow(10, exp);
			maxval0 /= expFactor;

			foreach (float autoScaleValue in autoScaleValues)
			{
				if (autoScaleValue >= maxval0)
				{
					scale = autoScaleValue;
					break;
				}
			}

			if (scale > 0.5f)
			{
				majorStepSize = 0.1f;
			}
			else if (scale > 0.2f)
			{
				majorStepSize = 0.05f;
			}
			else
			{
				majorStepSize = 0.02f;
			}
			minorStepSize = majorStepSize / 2;

			nMajorDivs = (int)Math.Round(scale / majorStepSize);
			nMinorDivs = (int)Math.Round(scale / minorStepSize);

			scale *= expFactor;
			majorStepSize = scale / nMajorDivs;  //majStepSize *= expFactor;
			minorStepSize = scale / nMinorDivs;  //minStepSize *= expFactor;
			if (neg)
			{
				scale = -scale;
				majorStepSize = -majorStepSize;
				minorStepSize = -minorStepSize;
			}
		}

		public void inverse()
		{
			majorStepSize = -majorStepSize;
			minorStepSize = -minorStepSize;
		}

	}
}
