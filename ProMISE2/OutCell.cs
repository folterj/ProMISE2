using System.Windows.Media;

namespace ProMISE2
{
	public class OutCell
	{
		public float pos = 0;
		public float con = 0;
		public Color color = new Color();

		public OutCell()
		{
		}

		public OutCell(float pos, float con)
		{
			this.pos = pos;
			this.con = con;
		}

		public OutCell(float pos, float con, Color color)
		{
			this.pos = pos;
			this.con = con;
			this.color = color;
		}

	}
}
