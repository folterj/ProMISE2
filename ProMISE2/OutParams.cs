using System.Collections;
using System.Collections.Generic;

namespace ProMISE2
{
	public class OutComp : Comp
	{
		public PhaseType phase;

		public float retention;
		public float average;
		public float retention0;
		public float retentionTime;
		public float drawPosition;

		public float sigma;
		public float width, hwidth, swidth;
		public float height;
		public float purity;
		public float recovery;

		public float totm, totmup, totmlp;

		public bool willElute;
		public bool eluted;
		public bool outCol;

		public float intIt;
		public bool intItSet;

		public QuantityType units;
		public VolUnitsType volUnits;
		public TimeUnitsType timeUnits;
		public MassUnitsType massUnits;

		public float filterSigma;

		public OutComp()
			: base()
		{
			reset();
		}

		public OutComp(Comp comp)
			: base(comp)
		{
			reset();
		}

		public OutComp(OutComp comp)
			: base(comp)
		{
			phase = comp.phase;
			retention = comp.retention;
			average = comp.average;
			retention0 = comp.retention0;
			retentionTime = comp.retentionTime;
			drawPosition = comp.drawPosition;

			sigma = comp.sigma;
			width = comp.width;
			hwidth = comp.hwidth;
			swidth = comp.swidth;
			height = comp.height;
			purity = comp.purity;
			recovery = comp.recovery;

			totm = comp.totm;
			totmup = comp.totmup;
			totmlp = comp.totmlp;

			willElute = comp.willElute;
			eluted = comp.eluted;
			outCol = comp.outCol;

			intIt = comp.intIt;
			intItSet = comp.intItSet;

			units = comp.units;
			volUnits = comp.volUnits;
			timeUnits = comp.timeUnits;
			massUnits = comp.massUnits;

			filterSigma = comp.filterSigma;
		}

		void reset()
		{
			phase = PhaseType.None;
			retention = 0;
			average = 0;
			retention0 = 0;
			retentionTime = 0;
			sigma = 0;
			width = 0;
			hwidth = 0;
			swidth = 0;
			height = 0;
			purity = 0;
			recovery = 0;

			totm = 0;
			totmup = 0;
			totmlp = 0;

			willElute = true;
			eluted = false;

			intIt = 0;
			intItSet = false;

			filterSigma = 0;
		}
	}

	public class OutParams
	{
		ArrayList observers = new ArrayList();

		public OutSet outSet;
		public List<OutSet> timeOutSet;

		public VisOutSet visOutSet;
		public List<VisOutSet> timeVisOutSet;

		public float estmaxtime = 0;
		public float estmaxstep = 0;

		public OutParams()
		{
			reset();
			resetTime();
		}

		public void reset()
		{
			outSet = new OutSet();
			visOutSet = new VisOutSet();
		}

		public void resetTime()
		{
			timeOutSet = new List<OutSet>();
			timeVisOutSet = new List<VisOutSet>();
		}

		public string getText(int timei = -1)
		{
			List<OutComp> outcomps;
			string s = "";

			if (timei >= 0)
			{
				outcomps = timeOutSet[timei].comps;
			}
			else
			{
				outcomps = outSet.comps;
			}

			if (outcomps.Count > 0)
			{
				OutComp prevComp = null;
				float rs = 0;

				ControlComp comp0 = new ControlComp(outcomps[0]);

				s += "<b>Label\tK\tM\tCon\tPhase\tRet (max)\tRet (avg)\tWidth\tSigma\tHeight\tPurity\tRecovery\tResolution</b>\n";
				s += string.Format("<b>\t\t{0}\t{1}\t\t{2}\t{2}\t{2}\t{2}\t{1}\t\t\t</b>\n",
									comp0.MassUnits, comp0.ConcentrationUnits, comp0.Units, comp0.Units, comp0.Units);

				foreach (OutComp comp in outcomps)
				{
					s += comp.label + "\t";
					s += string.Format("{0}\t", comp.k);
					s += string.Format("{0}\t", comp.m);
					s += string.Format("{0:0.0#E+0}\t", comp.concentration);
					s += string.Format("{0}\t", comp.phase);
					s += string.Format("{0:0.0#}\t", comp.retention);
					s += string.Format("{0:0.0#}\t", comp.average);
					s += string.Format("{0:0.0#}\t", comp.width);
					s += string.Format("{0:0.0#}\t", comp.sigma);
					s += string.Format("{0:0.0#E+0}\t", comp.height);
					s += string.Format("{0:0.0%}\t", comp.purity);
					s += string.Format("{0:0.0%}\t", comp.recovery);

					if (prevComp != null)
					{
						rs = Equations.calcRes(prevComp, comp);
						if (rs != 0)
						{
							s += string.Format("{0:0.000#}", rs);
						}
						else
						{
							s += "-";
						}
					}

					s += "\n";

					prevComp = comp;
				}
			}
			return s;
		}

	}
}
