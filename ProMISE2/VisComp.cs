using System.Windows.Media;

namespace ProMISE2
{
	public class VisComp
	{
		public VisPoint point = new VisPoint();
		public string label = "";
		public Color lineColor = Colors.LightGray;

		public VisComp()
		{
		}

		public VisComp(VisPoint point, string label, Color lineColor)
		{
			this.point = point;
			this.label = label;
			this.lineColor = lineColor;
		}

	}
}
