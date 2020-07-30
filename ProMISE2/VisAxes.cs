using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ProMISE2
{
	public class VisAxes
	{
		public List<VisAxis> visAxes = new List<VisAxis>();

		public void addAxis(string title,
							float vx1, float vy1, float vx2, float vy2,
							bool drawLines, bool drawLabels, bool drawMajorTicks, bool drawMinorTicks, bool drawZero, bool drawReverse, bool logScale,
							bool expScale, float multiplier,
							Color drawcolor, float drawsize,
							float min, float max, int nlabeldiv, float labelstepsize, int ntickdiv, float tickstepsize)
		{
			VisAxis axis = new VisAxis();
			float frac, f;
			float vx, vy;
			string l;
			int exp = 0;
			int ndec = 0;

			axis.title = title;
			axis.point1.vx = vx1;
			axis.point1.vy = vy1;
			axis.point2.vx = vx2;
			axis.point2.vy = vy2;
			axis.drawLines = drawLines;
			axis.drawLabels = drawLabels;
			axis.drawMajorTicks = drawMajorTicks;
			axis.drawMinorTicks = drawMinorTicks;
			axis.drawColor = drawcolor;
			axis.drawSize = drawsize;
			axis.negative = (max < min);
			axis.drawReverse = drawReverse;

			if (expScale)
			{
				exp = Util.getExponent(string.Format("{0:E0}", max));
				ndec = Util.getNDecimals(Util.toString(labelstepsize / (float)Math.Pow(10, exp), 3));
			}

			for (int i = 0; i <= ntickdiv; i++)
			{
				if (max - min != 0)
				{
					frac = i * tickstepsize / (max - min);
					if (frac <= 1)
					{
						vx = vx1 + frac * (vx2 - vx1);
						vy = vy1 + frac * (vy2 - vy1);
						axis.scaletickpos.Add(new VisPoint(vx, vy));
					}
				}
			}
			for (int i = 0; i <= nlabeldiv; i++)
			{
				if (max - min != 0)
				{
					frac = i * labelstepsize / (max - min);
					if (frac <= 1)
					{
						vx = vx1 + frac * (vx2 - vx1);
						vy = vy1 + frac * (vy2 - vy1);
						axis.scalelabelpos.Add(new VisPoint(vx, vy));
						f = min + i * labelstepsize;
						l = "";
						if (logScale)
						{
							if (i != 0 || drawZero)
							{
								if (f < 0)
								{
									l = string.Format("E{0:F0}", f);
								}
								else
								{
									l = string.Format("E+{0:F0}", f);
								}
							}
						}
						else
						{
							if (f != 0 || drawZero)
							{
								if (expScale && f != 0)
								{
									l = string.Format("{0:F" + ndec + "}", Math.Abs(f) / (float)Math.Pow(10, exp));
									if (exp != 0)
									{
										l += string.Format("E{0}", exp);
									}
								}
								else
								{
									l = Util.toString(Math.Abs(f) * multiplier, 3);
								}
							}
						}
						axis.scalelabeltext.Add(l);
					}
				}
			}
			visAxes.Add(axis);
		}

	}
}
