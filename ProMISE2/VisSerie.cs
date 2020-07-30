using System.Windows;
using System.Windows.Media;

namespace ProMISE2
{
	public class VisSerie
	{
		public VisPoint[] visPoints = new VisPoint[0];
		public VisSerieType type = VisSerieType.Graph;
		public Color drawColor = Colors.Black;
		public Rect visRect;
		public bool multiColor = false;
		public float drawSize = 1;
		public float drawWeight = 1;
		public int compi = 0;
	}
}
