using System.Collections.Generic;

namespace ProMISE2
{
	public class VisOutSet
	{
		public VisSerie[] visSeries = new VisSerie[0];
		public VisSerie[] visRawSeries = new VisSerie[0];
		public VisAxes visAxes = new VisAxes();
		public List<VisComp> comps = new List<VisComp>();
		public string posUnits, conUnits, timeUnits;
		public bool useMultiplier;
		public float conMultiplier;
	}
}
