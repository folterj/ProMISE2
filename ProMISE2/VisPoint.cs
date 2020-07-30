using System.Windows.Media;

namespace ProMISE2
{
	public class VisPoint
	{
		public float vx = 0;
		public float vy = 0;
		public Color color = Colors.Black;

		public VisPoint()
		{
		}

		public VisPoint(float vx, float vy)
		{
			this.vx = vx;
			this.vy = vy;
		}

		public VisPoint(float vx, float vy, Color color)
		{
			this.vx = vx;
			this.vy = vy;
			this.color = color;
		}

	}
}
