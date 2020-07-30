using System.Collections.Generic;
using System.Windows.Media;

namespace ProMISE2
{
	public class VisAxis
	{
		public List<VisPoint> scaletickpos = new List<VisPoint>();
		public List<VisPoint> scalelabelpos = new List<VisPoint>();
		public List<string> scalelabeltext = new List<string>();
		public VisPoint point1 = new VisPoint();
		public VisPoint point2 = new VisPoint();
		public bool drawLines = true;
        public bool drawLabels = false;
        public bool drawMajorTicks = false;
		public bool drawMinorTicks = false;
		public bool negative = false;
		public bool drawReverse = false;
		public Color drawColor = Colors.Black;
		public float drawSize = 1;
		public string title = "";

		public void clearLabels()
		{
			scaletickpos.Clear();
			scalelabelpos.Clear();
			scalelabeltext.Clear();
            drawLines = false;
            drawLabels = false;
            drawMajorTicks = false;
            drawMinorTicks = false;
		}

		public void addLabel(string label, float vx, float vy)
		{
			scalelabeltext.Add(label);
			scalelabelpos.Add(new VisPoint(vx, vy));
            drawLines = true;
            drawLabels = true;
            drawMajorTicks = true;
            drawMinorTicks = true;
		}

		public bool isHorizontal()
		{
			return (point1.vy == point2.vy);
		}

		public bool isNegative()
		{
			return negative;
		}

		public bool isReverse()
		{
			return drawReverse;
		}

		public int getScaleSideFactor()
		{
			if (drawReverse)
			{
				return -1;
			}
			return 1;
		}

	}
}
