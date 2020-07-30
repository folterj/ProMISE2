using System.Collections.Generic;

namespace ProMISE2
{
	public class OutSet
	{
		public OutCell[][] outCells = new OutCell[0][];
        public OutCell[][] rawOutCells = new OutCell[0][];
        public OutCell[][] unitsOutCells = new OutCell[0][];
		public Axes axes = new Axes();
		public List<OutComp> comps = new List<OutComp>();
        public float time = 0;

		public OutSet()
		{
		}

		public OutSet(InParamsExt inParams)
		{
			int ncomps = inParams.comps.Count;
			for (int compi = 0; compi < ncomps; compi++)
			{
				comps.Add(new OutComp(inParams.comps[compi]));
			}
		}

	}
}
