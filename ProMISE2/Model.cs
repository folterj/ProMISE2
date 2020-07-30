using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ProMISE2
{
	public abstract class Model
	{
		public ProModelInterface model;
		public InParamsExt inParams;
		public OutParams previewParams;
		public OptionParams optionParams;
		public OutParams outParams;
		public List<float> timeTime = new List<float>(); // timeTime[time]
		public bool[] compEluted;
		public List<float> intSwitchu = new List<float>();
		public List<float> intSwitchl = new List<float>();

		public PerformanceStats stats = new PerformanceStats();

		public bool running = false;
		public PhaseType curPhase = PhaseType.Both;
		public float intamountu, intamountl, intamountut, intamountlt;
		public float mincon;
		public bool eeDone;
		public int it;
		public int intit;

		public Model(ProModelInterface model, InParamsExt inParams, OutParams previewParams, OptionParams optionParams)
		{
			this.model = model;
			this.inParams = inParams;
			this.previewParams = previewParams;
			this.optionParams = optionParams;
		}

		public void generalInit()
		{
			int ncomps = inParams.comps.Count;

			intit = 0;
			intamountu = 0;
			intamountl = 0;
			eeDone = false;

			compEluted = new bool[ncomps];

			if (inParams.runMode == RunModeType.UpperPhase)
			{
				curPhase = PhaseType.Upper;
			}
			else if (inParams.runMode == RunModeType.LowerPhase)
			{
				curPhase = PhaseType.Lower;
			}
			else if (inParams.runMode == RunModeType.Intermittent)
			{
				curPhase = inParams.intStartPhase;
			}
			else
			{
				curPhase = PhaseType.Both;
			}
		}

		public void clearOut()
		{
			if (outParams == null)
			{
				outParams = new OutParams();
			}
			outParams.reset();
		}

		public void clearTimeOut()
		{
			if (outParams == null)
			{
				outParams = new OutParams();
			}
			outParams.resetTime();
		}

		public void update(InParamsExt inParams, OutParams previewParams)
		{
			outParams = new OutParams();

			this.inParams = inParams;
			this.previewParams = previewParams;
			if (this.previewParams == null)
			{
				this.previewParams = outParams;
			}

			stats.start();
			run();
			stats.storeModelTime();
		}

		public void updateOut(ViewParams viewParams)
		{
			stats.start();
			storeOut(viewParams);
			stats.storeOutTime();

			stats.start();
			updateVisOut(viewParams);
			stats.storeVisoutTime();
		}

		public void updateTimeOut(ViewParams viewParams)
		{
			stats.start();
			storeTimeOut(viewParams);
			stats.storeOutTime();

			stats.start();
			updateTimeVisOut(viewParams);
			stats.storeVisoutTime();
		}

		public abstract void run();

		public abstract void storeOut(ViewParams viewParams);

		public abstract void storeTimeOut(ViewParams viewParams);

		public float getRealTime(int time)
		{
			return timeTime[time];
		}

		public void updateVisOut(ViewParams viewParams)
		{
			outParams.visOutSet = updateVisOutVar(outParams.outSet, viewParams);
		}

		public void updateTimeVisOut(ViewParams viewParams)
		{
			int ntime = outParams.timeOutSet.Count;

			outParams.timeVisOutSet = new List<VisOutSet>(ntime);
			for (int timei = 0; timei < ntime; timei++)
			{
				outParams.timeVisOutSet.Add(updateVisOutVar(outParams.timeOutSet[timei], viewParams));
			}
		}

		public VisOutSet updateVisOutVar(OutSet outSet, ViewParams viewParams)
		{
			VisOutSet visOutSet = new VisOutSet();
			List<VisSerie> visSeries;
			List<VisSerie> visRawSeries = null;
			VisSerie visSerie;
			List<VisPoint> visPoints;
			OutCell[][] rawOutCells = outSet.rawOutCells;
			OutCell[] compRawOutCells;
			OutCell outCell;
			OutComp outComp;
			VisComp visComp;
			VisAxes visAxes = new VisAxes();
			VisAxis visAxis;
			Axes axes = outSet.axes;
			Axis xaxis = new Axis();
			Axis yaxis = new Axis();
			PhaseType phase = new PhaseType();
			int nphases;
			int ncomps;
			int npoints;
			int nseries;
			int ncells;
			bool previewMode = (viewParams.viewType == ViewType.Setup);
			bool dispDualTime = (viewParams.phaseDisplay == PhaseDisplayType.UpperLowerTime);
			bool dispDual = (viewParams.phaseDisplay == PhaseDisplayType.UpperLower || dispDualTime);
			bool usePrefixes = (viewParams.exponentType == ExponentType.Prefixes);
			bool scaleDrawReverse;
			bool scaleSet;
			bool sum;
			float maxcon = 0;
			float con = 0;
			float rangey = 0;
			float rangex = 0;
			float range;
			float rangey0;
			float scaleu, scalel;
			float miny, maxy;
			float minvx = 0;
			float maxvx = 0;
			float minxlabel = 0;
			float maxxlabel = 0;
			float k;
			float flow;
			float pos;
			float vceff;
			RectangleF allrect = new RectangleF(0, 0, 1, 1);
			RectangleF vrect = allrect;
			RectangleF rawvrect = allrect;
			float unitsize = 0.025f;
			float vx, vy;
			int s = 0;
			int c;
			string label = "";
			MassUnitsType prefMassUnits = inParams.massUnits;
			int prefMassUnitsi;
			float multiplier = 1;

			// general vars
			if (dispDual && !previewMode)
			{
				nphases = 2;
			}
			else
			{
				nphases = 1;
			}
			ncomps = inParams.comps.Count;
			nseries = outSet.outCells.Length;

			if (ncomps == 0)
			{
				return visOutSet;
			}

			// update outComps units
			foreach (OutComp outComp0 in outSet.comps)
			{
				outComp0.units = inParams.viewUnits;
				outComp0.volUnits = inParams.volUnits;
				outComp0.timeUnits = inParams.timeUnits;
				outComp0.massUnits = inParams.massUnits;
			}

			// convert out to units
			outSet.unitsOutCells = new OutCell[outSet.outCells.Length][];
			for (s = 0; s < nseries; s++)
			{
				c = s / nphases;
				if (viewParams.phaseDisplay == PhaseDisplayType.Lower)
				{
					phase = PhaseType.Lower;
				}
				else
				{
					phase = PhaseType.Upper;
				}
				if (nphases > 1)
				{
					phase = (PhaseType)((s % nphases) + 1);
				}
				ncells = outSet.outCells[s].Length;

				outSet.unitsOutCells[s] = new OutCell[ncells];
				for (int i = 0; i < ncells; i++)
				{
					pos = inParams.convertUnit(outSet.outCells[s][i].pos, inParams.natUnits, viewParams.viewUnits, phase);
					con = outSet.outCells[s][i].con;
					outSet.unitsOutCells[s][i] = new OutCell(pos, con);
				}
			}

			// initialise vis series
			visSeries = new List<VisSerie>(nseries);
			if (viewParams.showProbUnits)
			{
				visRawSeries = new List<VisSerie>(nseries);
			}

			if (viewParams.showProbUnits)
			{
				if (dispDual)
				{
					rawvrect.Height = unitsize * 2;
				}
				else
				{
					rawvrect.Height = unitsize;
				}

				vrect.Y = rawvrect.Bottom;
				vrect.Height = 1 - vrect.Y;
			}

			// Axes
			axes = outSet.axes;
			// Set maxcon / rangey
			if (viewParams.yScale == YScaleType.Absolute)
			{
				// * not exactly correct; should use maxcon from first (time) step
				maxcon = 0;
				for (int i = 0; i < outSet.comps.Count; i++)
				{
					if (outSet.comps[i].m > maxcon)
					{
						maxcon = outSet.comps[i].m;
					}
				}
				rangey = maxcon;
			}
			else if (viewParams.yScale == YScaleType.Normalised)
			{
				rangey = 0;
			}
			else // Auto or Log scale
			{
				maxcon = 0;
				for (int i = 0; i < axes.maxcon.Count; i++)
				{
					if (axes.maxcon[i] > maxcon)
					{
						maxcon = axes.maxcon[i];
					}
				}
				if (viewParams.yScale == YScaleType.Automatic)
				{
					rangey = maxcon;
				}
				else if (axes.logScale)
				{
					maxcon = (float)Math.Ceiling(Math.Log(maxcon)); // Nearest E power
					rangey = 5;
					// Scale: E^maxcon ... E^(maxcon-rangey)
				}
			}

			// Column lines
			if (viewParams.showProbUnits)
			{
				visAxis = new VisAxis();
				visAxis.drawColor = Colors.LightGray;
				visAxis.point1 = new VisPoint(vrect.Left, rawvrect.Top);
				visAxis.point2 = new VisPoint(vrect.Left, rawvrect.Bottom);
				visAxes.visAxes.Add(visAxis);

				visAxis = new VisAxis();
				visAxis.drawColor = Colors.LightGray;
				visAxis.point1 = new VisPoint(vrect.Right, rawvrect.Top);
				visAxis.point2 = new VisPoint(vrect.Right, rawvrect.Bottom);
				visAxes.visAxes.Add(visAxis);

				visAxis = new VisAxis();
				visAxis.point1 = new VisPoint(vrect.Left, rawvrect.Top);
				visAxis.point2 = new VisPoint(vrect.Right, rawvrect.Top);
				visAxes.visAxes.Add(visAxis);

				visAxis = new VisAxis();
				visAxis.point1 = new VisPoint(vrect.Left, rawvrect.Bottom);
				visAxis.point2 = new VisPoint(vrect.Right, rawvrect.Bottom);
				visAxes.visAxes.Add(visAxis);
			}
			if (axes.showCol)
			{
				visAxis = new VisAxis();
				visAxis.drawColor = Colors.LightGray;
				vx = axes.colstart / axes.rangex * allrect.Width + allrect.Left;
				visAxis.point1 = new VisPoint(vx, allrect.Top);
				visAxis.point2 = new VisPoint(vx, allrect.Bottom);
				visAxes.visAxes.Add(visAxis);

				visAxis = new VisAxis();
				visAxis.drawColor = Colors.LightGray;
				vx = axes.colend / axes.rangex * allrect.Width + allrect.Left;
				visAxis.point1 = new VisPoint(vx, allrect.Top);
				visAxis.point2 = new VisPoint(vx, allrect.Bottom);
				visAxes.visAxes.Add(visAxis);
			}
			if (axes.showDeadvolstart)
			{
				visAxis = new VisAxis();
				vx = axes.deadvolstart / axes.rangex * vrect.Width + vrect.Left;
				visAxis.point1 = new VisPoint(vx, vrect.Top);
				visAxis.point2 = new VisPoint(vx, vrect.Bottom);
				visAxes.visAxes.Add(visAxis);
			}
			if (axes.showDeadvolend)
			{
				visAxis = new VisAxis();
				vx = axes.deadvolend / axes.rangex * vrect.Width + vrect.Left;
				visAxis.point1 = new VisPoint(vx, vrect.Top);
				visAxis.point2 = new VisPoint(vx, vrect.Bottom);
				visAxes.visAxes.Add(visAxis);
			}
			if (axes.showDeadvolinsert)
			{
				visAxis = new VisAxis();
				vx = axes.deadvolinjectstart / axes.rangex * vrect.Width + vrect.Left;
				visAxis.point1 = new VisPoint(vx, vrect.Top);
				visAxis.point2 = new VisPoint(vx, vrect.Bottom);
				visAxes.visAxes.Add(visAxis);

				visAxis = new VisAxis();
				vx = axes.deadvolinjectend / axes.rangex * vrect.Width + vrect.Left;
				visAxis.point1 = new VisPoint(vx, vrect.Top);
				visAxis.point2 = new VisPoint(vx, vrect.Bottom);
				visAxes.visAxes.Add(visAxis);
			}

			// X Axis
			if (viewParams.syncScales && viewParams.viewUnits != QuantityType.ReS)
			{
				minxlabel = inParams.convertUnit(axes.scaleminulabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Upper);
				maxxlabel = inParams.convertUnit(axes.scalemaxulabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Upper);
				scaleu = Math.Abs(maxxlabel - minxlabel);

				minxlabel = inParams.convertUnit(axes.scaleminllabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Lower);
				maxxlabel = inParams.convertUnit(axes.scalemaxllabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Lower);
				scalel = Math.Abs(maxxlabel - minxlabel);

				if (scaleu != 0 && scalel != 0)
				{
					axes.sync(scaleu, scalel);
				}
			}
			label = "";
			for (int phasei = 0; phasei < 2; phasei++)
			{
				if (dispDual)
				{
					vy = vrect.Top + 0.5f * vrect.Height;
					scaleDrawReverse = ((PhaseType)(phasei + 1) == PhaseType.Lower);
				}
				else
				{
					vy = vrect.Bottom;
					scaleDrawReverse = false;
				}
				scaleSet = false;
				if ((PhaseType)(phasei + 1) == PhaseType.Upper && (inParams.runMode != RunModeType.LowerPhase || (inParams.eeMode != EEModeType.None && inParams.isPosEEdir())) && viewParams.phaseDisplay != PhaseDisplayType.Lower)
				{
					// show Up axis
					minvx = vrect.Left + axes.scaleminu / axes.rangexu * vrect.Width;
					maxvx = vrect.Left + axes.scalemaxu / axes.rangexu * vrect.Width;
					minxlabel = inParams.convertUnit(axes.scaleminulabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Upper);
					maxxlabel = inParams.convertUnit(axes.scalemaxulabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Upper);
					rangex = Math.Abs(axes.scalemaxu - axes.scaleminu);
					scaleSet = true;
				}
				else if ((PhaseType)(phasei + 1) == PhaseType.Lower && (inParams.runMode != RunModeType.UpperPhase || (inParams.eeMode != EEModeType.None && !inParams.isPosEEdir())) && viewParams.phaseDisplay != PhaseDisplayType.Upper)
				{
					// show Lp axis
					minvx = vrect.Left + axes.scaleminl / axes.rangexl * vrect.Width;
					maxvx = vrect.Left + axes.scalemaxl / axes.rangexl * vrect.Width;
					minxlabel = inParams.convertUnit(axes.scaleminllabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Lower);
					maxxlabel = inParams.convertUnit(axes.scalemaxllabel, inParams.natUnits, viewParams.viewUnits, PhaseType.Lower);
					rangex = Math.Abs(axes.scalemaxl - axes.scaleminl);
					scaleSet = true;
				}
				if (scaleSet)
				{
					range = maxxlabel - minxlabel;
					// if syncScales: use same divs/stepsize as other axis
					if (phasei == 0 || !viewParams.syncScales || viewParams.phaseDisplay == PhaseDisplayType.Upper || viewParams.phaseDisplay == PhaseDisplayType.Lower)
					{
						xaxis.calcScale(range);
					}
					if (label == "")
					{
						// only do first label
						label = inParams.getXaxisLegend();
					}
					else
					{
						label = "";
					}
					visAxes.addAxis(label,
									minvx, vy, maxvx, vy,
									false, true, true, !previewMode, !dispDual, scaleDrawReverse, false,
									false, 1,
									Colors.Black, 1,
									minxlabel, maxxlabel, xaxis.nMajorDivs, xaxis.majorStepSize, xaxis.nMinorDivs, xaxis.minorStepSize);
					if (viewParams.viewUnits == QuantityType.ReS)
					{
						// ReS (K-values) scale: overwrite labels
						visAxis = visAxes.visAxes[visAxes.visAxes.Count - 1];
						visAxis.clearLabels();
						for (int i = 0; i <= 8; i++)
						{
							if (i > 4)
							{
								k = (float)4 / (8 - i);
							}
							else
							{
								k = (float)i / 4;
							}
							flow = Equations.calcFlow(inParams.kDefinition, inParams.fu, -inParams.fl, k);
							if (flow < 0 && inParams.runMode == RunModeType.DualMode)
							{
								vceff = inParams.injectPos * inParams.vc;
							}
							else
							{
								vceff = (1 - inParams.injectPos) * inParams.vc;
							}
							if (flow != 0)
							{
								if (float.IsInfinity(k))
								{
									pos = Equations.calcInfPos(inParams.kDefinition, vceff, inParams.lf, inParams.uf, inParams.fu, inParams.fl);
								}
								else
								{
									pos = Equations.calcPos(inParams.kDefinition, vceff, inParams.lf, inParams.uf, flow, k);
								}
								if ((PhaseType)(phasei + 1) == PhaseType.Lower)
								{
									pos = -pos;
								}
								pos = inParams.convertUnit(pos, QuantityType.Time, inParams.natUnits, (PhaseType)(phasei + 1));
								if (pos > 0 && !float.IsInfinity(pos))
								{
									vx = minvx + pos / rangex * (maxvx - minvx);
									visAxis.addLabel(Util.toString(k, 2), vx, vy);
								}
							}
						}
					}
				}
			}
			// Y Axis
			range = rangey;
			if (!axes.logScale)
			{
				// normal scale
				yaxis.calcScale(range);
				rangey = yaxis.scale;
				miny = 0;
				maxy = rangey;
			}
			else
			{
				// log scale
				yaxis.majorStepSize = 1;
				yaxis.nMajorDivs = (int)rangey;
				yaxis.minorStepSize = 0;
				yaxis.nMinorDivs = 0;
				miny = maxcon - rangey;
				maxy = maxcon;
			}
			if (usePrefixes)
			{
				prefMassUnitsi = (int)inParams.massUnits;
				rangey0 = rangey;
				while (rangey0 >= 1000 && prefMassUnitsi < Enum.GetValues(typeof(MassUnitsType)).Length)
				{
					rangey0 /= 1000;
					multiplier /= 1000;
					prefMassUnitsi++;
				}
				while (rangey0 < 1 && prefMassUnitsi > 0)
				{
					rangey0 *= 1000;
					multiplier *= 1000;
					prefMassUnitsi--;
				}
				prefMassUnits = (MassUnitsType)prefMassUnitsi;
				label = inParams.getYaxisLegend(prefMassUnits);
			}
			else
			{
				label = inParams.getYaxisLegend(null);
			}
			if (dispDual)
			{
				vy = vrect.Top + 0.5f * vrect.Height;
				// positive y scale
				visAxes.addAxis("Upper Phase " + label,
								vrect.Left, vy, vrect.Left, vrect.Top,
								!previewMode, true, true, !previewMode, !axes.logScale, false, axes.logScale,
								!usePrefixes, multiplier,
								Colors.Black, 1,
								miny, maxy, yaxis.nMajorDivs, yaxis.majorStepSize, yaxis.nMinorDivs, yaxis.minorStepSize);
				// negative y scale
				if (!axes.logScale)
				{
					yaxis.inverse();
					miny = -miny;
					maxy = -maxy;
				}
				visAxes.addAxis("Lower Phase " + label,
								vrect.Left, vy, vrect.Left, vrect.Bottom,
								!previewMode, true, true, !previewMode, false, false, axes.logScale,
								!usePrefixes, multiplier,
								Colors.Black, 1,
								miny, maxy, yaxis.nMajorDivs, yaxis.majorStepSize, yaxis.nMinorDivs, yaxis.minorStepSize);
			}
			else
			{
				visAxes.addAxis(label,
								vrect.Left, vrect.Bottom, vrect.Left, vrect.Top,
								!previewMode, true, true, !previewMode, true, false, axes.logScale,
								!usePrefixes, multiplier,
								Colors.Black, 1,
								miny, maxy, yaxis.nMajorDivs, yaxis.majorStepSize, yaxis.nMinorDivs, yaxis.minorStepSize);
			}

			// Raw Series (units)
			if (viewParams.showProbUnits)
			{
				for (s = 0; s < rawOutCells.Length; s++)
				{
					compRawOutCells = rawOutCells[s];
					npoints = compRawOutCells.Length;
					c = s / nphases;

					visSerie = new VisSerie();
					visSerie.type = VisSerieType.Units;
					visSerie.visRect = new Rect(rawvrect.X, rawvrect.Y, rawvrect.Width, rawvrect.Height);
					visSerie.compi = c;
					visSerie.drawSize = unitsize;
					visSerie.drawWeight = outSet.comps[c].m;

					visPoints = new List<VisPoint>();

					if (viewParams.phaseDisplay == PhaseDisplayType.Lower)
					{
						phase = PhaseType.Lower;
					}
					else
					{
						phase = PhaseType.Upper;
					}
					if (nphases > 1)
					{
						phase = (PhaseType)((s % nphases) + 1);
					}
					if (phase == PhaseType.Upper)
					{
						rangex = axes.rangexu;
					}
					else
					{
						rangex = axes.rangexl;
					}
					if (c < ncomps)
					{
						visSerie.drawColor = Util.colorRange(c, ncomps);
					}
					else
					{
						visSerie.drawColor = Colors.Gray;
					}
					
					for (int i = 0; i < npoints; i++)
					{
						outCell = compRawOutCells[i];
						vx = rawvrect.Left + outCell.pos / rangex * rawvrect.Width;
						if (s % nphases == 0)
						{
							vy = rawvrect.Top;
						}
						else
						{
							vy = rawvrect.Top + 0.5f * rawvrect.Height;
						}

						visPoints.Add(new VisPoint(vx, vy));
					}
					visSerie.visPoints = visPoints.ToArray();
					visRawSeries.Add(visSerie);
				}
				visOutSet.visRawSeries = visRawSeries.ToArray();

				if (dispDual)
				{
					visAxis = new VisAxis();
					visAxis.point1 = new VisPoint(rawvrect.Left, 0.5f * rawvrect.Height);
					visAxis.point2 = new VisPoint(rawvrect.Right, 0.5f * rawvrect.Height);
					visAxes.visAxes.Add(visAxis);
				}
			}
			visOutSet.visAxes = visAxes;

			// Series
			if (viewParams.yScale == YScaleType.Automatic)
			{
				maxcon = rangey;
			}
			for (s = 0; s < nseries; s++)
			{
				npoints = outSet.outCells[s].Length;
				c = s / nphases;

				visSerie = new VisSerie();
				visSerie.type = VisSerieType.Graph;
				visSerie.visRect = new Rect(vrect.X, vrect.Y, vrect.Width, vrect.Height);
				visSerie.compi = c;

				visPoints = new List<VisPoint>();

				if (nphases > 1)
				{
					phase = (PhaseType)((s % nphases) + 1);
				}
				else if (viewParams.phaseDisplay == PhaseDisplayType.Lower)
				{
					phase = PhaseType.Lower;
				}
				else
				{
					phase = PhaseType.Upper;
				}
				if (phase == PhaseType.Upper)
				{
					rangex = axes.rangexu;
				}
				else
				{
					rangex = axes.rangexl;
				}
				if (c < ncomps)
				{
					visSerie.drawWeight = outSet.comps[c].m;
					visSerie.multiColor = false;
					visSerie.drawColor = Util.colorRange(c, ncomps);
					sum = false;
				}
				else
				{
					visSerie.multiColor = true;
					visSerie.drawColor = Colors.Gray;
					sum = true;
				}
				if (viewParams.peaksDisplay != PeaksDisplayType.Sum || sum)
				{
					// Sum graph: multi color
					if (viewParams.yScale == YScaleType.Normalised)
					{
						maxcon = axes.maxcon[c];
					}
					for (int i = 0; i < npoints; i++)
					{
						outCell = outSet.outCells[s][i];
						vx = vrect.Left + outCell.pos / rangex * vrect.Width;
						if (viewParams.yScale == YScaleType.Logarithmic)
						{
							if (outCell.con != 0 && maxcon != 0)
							{
								con = (float)(1 + (Math.Log(Math.Abs(outCell.con)) - maxcon) / rangey);
								if (con < 0)
								{
									con = 0;
								}
								if (outCell.con < 0)
								{
									con = -con;
								}
							}
							else
							{
								con = 0;
							}
						}
						else
						{
							if (maxcon != 0)
							{
								con = outCell.con / maxcon;
							}
						}
						if (dispDual)
						{
							vy = vrect.Top + (1 - con) / 2 * vrect.Height;
						}
						else
						{
							vy = vrect.Bottom - con * vrect.Height;
						}
						if (c < ncomps)
						{
							visPoints.Add(new VisPoint(vx, vy));
						}
						else
						{
							visPoints.Add(new VisPoint(vx, vy, outCell.color));
						}
					}
					visSerie.visPoints = visPoints.ToArray();
					visSeries.Add(visSerie);
				}
			}
			visOutSet.visSeries = visSeries.ToArray();

			// Peaks
			for (int i = 0; i < outSet.comps.Count; i++)
			{
				outComp = outSet.comps[i];
				if (outComp.phase == PhaseType.Upper)
				{
					vx = vrect.Left + outComp.drawPosition / axes.rangexu * vrect.Width;
				}
				else if (outComp.phase == PhaseType.Lower)
				{
					vx = vrect.Left + outComp.drawPosition / axes.rangexl * vrect.Width;
				}
				else
				{
					vx = vrect.Left + outComp.drawPosition / axes.rangex * vrect.Width;
				}
				if (dispDual && outComp.phase == PhaseType.Lower)
				{
					vy = vrect.Bottom;
				}
				else
				{
					vy = vrect.Top;
				}
				visComp = new VisComp(new VisPoint(vx, vy, Util.colorRange(i, ncomps, 0.5f)), outComp.label, Util.colorRange(i, ncomps, 2));
				visOutSet.comps.Add(visComp);
			}

			visOutSet.posUnits = inParams.getXaxisUnits();
			visOutSet.useMultiplier = usePrefixes;
			if (usePrefixes)
			{
				visOutSet.conMultiplier = multiplier;
				visOutSet.conUnits = inParams.getYaxisUnits(prefMassUnits);
			}
			else
			{
				visOutSet.conMultiplier = 1;
				visOutSet.conUnits = inParams.getYaxisUnits(null);
			}
			visOutSet.timeUnits = inParams.timeUnits.ToString();

			return visOutSet;
		}

		public void writeData(string filename, ViewParams viewParams, int time)
		{
			if (viewParams.viewType == ViewType.Out)
			{
				writeOut(filename, viewParams, outParams.outSet.unitsOutCells);
				//writeOutRaw(filename,viewparams,inparams,outParams.outSet.rawOutCells);
			}
			else if (viewParams.viewType == ViewType.Time)
			{
				writeOut(filename, viewParams, outParams.timeOutSet[time].unitsOutCells);
			}
		}

		public void writeOut(string fileName, ViewParams viewParams, OutCell[][] outCells)
		{
			TextWriter file;
			string unitsLabel = "";
			string label;
			string addlabel;
			int nphases;
			int ncomps;
			int nseries;
			float minpos, maxpos;
			bool dispDual = (viewParams.phaseDisplay == PhaseDisplayType.UpperLower || viewParams.phaseDisplay == PhaseDisplayType.UpperLowerTime);
			QuantityType viewUnits = viewParams.viewUnits;
			bool done;
			bool sepPos = false;
			int i;

			if (viewUnits == QuantityType.ReS)
			{
				viewUnits = QuantityType.Volume;
			}

			if (dispDual)
			{
				nphases = 2;
			}
			else
			{
				nphases = 1;
			}
			ncomps = inParams.comps.Count;
			if (viewParams.peaksDisplay != PeaksDisplayType.Peaks)
				ncomps++;
			nseries = ncomps * nphases;

			if (viewUnits == QuantityType.Steps)
			{
				unitsLabel = "Step";
			}
			else if (viewUnits == QuantityType.Volume)
			{
				unitsLabel = "Volume";
			}
			else if (viewUnits == QuantityType.Time)
			{
				unitsLabel = "Time";
			}
			else if (viewUnits == QuantityType.ReS)
			{
				unitsLabel = "ReS";
			}

			// check if all pos ranges are the same
			minpos = outCells[0][0].pos;
			maxpos = outCells[0][outCells[0].Length - 1].pos;
			for (int serie = 0; serie < outCells.Length; serie++)
			{
				if (outCells[serie][0].pos != minpos || outCells[serie][outCells[serie].Length - 1].pos != maxpos)
				{
					sepPos = true;
				}
			}

			file = new StreamWriter(fileName);
			// labels
			for (int c = 0; c < ncomps; c++)
			{
				if (c < inParams.comps.Count)
				{
					label = inParams.comps[c].label;
				}
				else
				{
					label = "Sum";
				}
				for (i = 0; i < nphases; i++)
				{
					if (nphases > 1 && (PhaseType)(i + 1) == PhaseType.Upper)
					{
						addlabel = "(UP)";
					}
					else if (nphases > 1 && (PhaseType)(i + 1) == PhaseType.Lower)
					{
						addlabel = "(LP)";
					}
					else
					{
						addlabel = "";
					}
					if (sepPos || (c == 0 && i == 0))
					{
						file.Write(unitsLabel);
						file.Write(",");
					}
					file.Write(label + addlabel);
					if (i + 1 < nphases)
					{
						file.Write(",");
					}
				}
				if (c + 1 < ncomps)
				{
					file.Write(",");
				}
			}
			file.WriteLine();

			// data
			i = 0;
			done = false;
			while (!done)
			{
				done = true;
				for (int serie = 0; serie < outCells.Length; serie++)
				{
					if (i < outCells[serie].Length)
					{
						if (sepPos || serie == 0)
						{
							file.Write(string.Format(CultureInfo.InvariantCulture, "{0},", outCells[serie][i].pos));
						}
						file.Write(string.Format(CultureInfo.InvariantCulture, "{0}", outCells[serie][i].con));
						if (serie + 1 < outCells.Length)
						{
							file.Write(",");
						}
						done = false;
					}
					else
					{
						// insert spacers
						if (sepPos || serie == 0)
						{
							file.Write(",");
						}
						if (serie + 1 < outCells.Length)
						{
							file.Write(",");
						}
					}
				}
				file.WriteLine();
				i++;
			}
			file.Close();
		}

		public void writeOutRaw(string fileName, ViewParams viewParams, InParamsExt inParams, OutParams outParams)
		{
			// *** obsolete; can re-use writeOut instead?
			TextWriter file;
			OutCell[][] rawOutCells;
			string unitsLabel = "";
			string label;
			string addlabel;
			int nphases;
			int ncomps;
			int nseries;
			int ncells;
			bool dispDual = (viewParams.phaseDisplay == PhaseDisplayType.UpperLower);
			bool first;

			if (dispDual)
			{
				nphases = 2;
			}
			else
			{
				nphases = 1;
			}
			ncomps = inParams.comps.Count;
			if (viewParams.peaksDisplay != PeaksDisplayType.Peaks)
			{
				ncomps++;
			}
			nseries = ncomps * nphases;

			if (inParams.natUnits == QuantityType.Steps)
			{
				unitsLabel = "Step";
			}
			else if (inParams.natUnits == QuantityType.Volume)
			{
				unitsLabel = "Volume";
			}
			else if (inParams.natUnits == QuantityType.Time)
			{
				unitsLabel = "Time";
			}
			else if (inParams.natUnits == QuantityType.ReS)
			{
				unitsLabel = "ReS";
			}

			file = new StreamWriter(fileName);
			// labels
			first = true;
			for (int c = 0; c < ncomps; c++)
			{
				if (c < inParams.comps.Count)
				{
					label = inParams.comps[c].label;
				}
				else
				{
					label = "Sum";
				}
				for (int i = 0; i < nphases; i++)
				{
					if (!first)
					{
						file.Write(",");
					}
					file.Write(unitsLabel);
					file.Write(",");
					if (nphases > 1 && (PhaseType)(i + 1) == PhaseType.Upper)
					{
						addlabel = "(UP)";
					}
					else if (nphases > 1 && (PhaseType)(i + 1) == PhaseType.Lower)
					{
						addlabel = "(LP)";
					}
					else
					{
						addlabel = "";
					}
					file.Write(label + addlabel);
					first = false;
				}
			}
			file.WriteLine();

			// data
			rawOutCells = outParams.outSet.rawOutCells;
			ncells = rawOutCells[0].Length;
			for (int i = 0; i < ncells; i++)
			{
				first = true;
				for (int s = 0; s < nseries; s++)
				{
					if (!first)
					{
						file.Write(",");
					}
					file.Write(string.Format(CultureInfo.InvariantCulture, "{0},{1}", rawOutCells[s][i].pos, rawOutCells[s][i].con));
					first = false;
				}
				file.WriteLine();
			}
			file.Close();
		}

	}
}
