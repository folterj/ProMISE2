using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ProMISE2
{
	public class CCDModel : Model
	{
		List<UCCDCells> cellsu = new List<UCCDCells>(); // cellsu[comp][n]
		List<LCCDCells> cellsl = new List<LCCDCells>(); // cellsl[comp][n]
		List<List<UCCDCells>> timeCellsu = new List<List<UCCDCells>>(); // timeCellsu[time][comp][n]
		List<List<LCCDCells>> timeCellsl = new List<List<LCCDCells>>(); // timeCellsl[time][comp][n]
		List<int> ncellsutime = new List<int>(); // ncellsutime[time]
		List<int> ncellsltime = new List<int>(); // ncellsltime[time]
		float[] compTotMass;		// total insertion mass [comp]
		float[] runningFilterSigma; // runningFilterSigma[comp]
		int ncellsu, ncellsl;
		int stepu, stepl;
		float fstepu, fstepl;
		float filterthreshold;

		public CCDModel(ProModelInterface model, InParamsExt inParams, OutParams previewParams, OptionParams optionParams)
			: base(model, inParams, previewParams, optionParams)
		{
			mincon = 0.001f;
			filterthreshold = 1.025f;
		}

		public override void run()
		{
			float estit;
			float storestep;
			float storeit = 0;
			int nstores = 0;

			it = 0;
			running = true;

			model.updateProgress(0);
			estit = init();
			storestep = estit / optionParams.timeStores;
			create();
			inject();
			storeTime();
			dist();
			storeTime();
			it++;
			storeit++;
			while (!isDone() && running)
			{
				move();
				inject();
				dist();
				if (storeit >= storestep)
				{
					storeTime();
					storeit -= storestep;
					nstores++;
					if (nstores > 2 * optionParams.timeStores)
					{
						trimTimeStores();
						nstores /= 2;
						storestep *= 2;
					}
				}
				model.updateProgress((float)it / estit);
				it++;
				storeit++;
			}
			storeTime();
			if (inParams.eeMode != EEModeType.None)
			{
				doEE();
				storeTime();
			}
			model.clearProgress();

			running = false;
		}

		public override void storeOut(ViewParams viewParams)
		{
			outParams.outSet = storeOutVar(cellsu, cellsl, ncellsu, ncellsl, viewParams, false, 0, eeDone);
		}

		public override void storeTimeOut(ViewParams viewParams)
		{
			int ntime;
			bool eeDone0 = false;

			model.updateProgress(0);
			ntime = timeCellsu.Count;
			outParams.timeOutSet = new List<OutSet>(ntime);
			for (int timei = 0; timei < ntime; timei++)
			{
				eeDone0 = (eeDone && timei == ntime - 1);
				outParams.timeOutSet.Add(storeOutVar(timeCellsu[timei], timeCellsl[timei], ncellsutime[timei], ncellsltime[timei], viewParams, true, timeTime[timei], eeDone0));
				model.updateProgress((float)timei / ntime);
			}
			model.clearProgress();
		}

		public OutSet storeOutVar(List<UCCDCells> cellsu0, List<LCCDCells> cellsl0, int ncellsu0, int ncellsl0, ViewParams viewparams, bool timeMode, float time, bool eeDone0)
		{
			OutSet outSet = new OutSet(inParams);
			UCCDCells compcellsu;
			LCCDCells compcellsl;
			List<List<OutCell>> outcells;
			Axes axes;
			float[] filterWeight = null;
			int nseries;
			int ncomps2;
			int ncells;
			int nphases;
			int ncomps = inParams.comps.Count;
			int compi;
			bool dispDualTime = (viewparams.phaseDisplay == PhaseDisplayType.UpperLowerTime);
			bool dispDual = (viewparams.phaseDisplay == PhaseDisplayType.UpperLower || dispDualTime);
			bool dispAll = (viewparams.phaseDisplay == PhaseDisplayType.All);
			bool dispUP = (viewparams.phaseDisplay == PhaseDisplayType.Upper);
			bool dispLP = (viewparams.phaseDisplay == PhaseDisplayType.Lower);
			bool runDual = (inParams.runMode == RunModeType.DualMode || inParams.runMode == RunModeType.Intermittent);
			bool runCo = (inParams.runMode == RunModeType.CoCurrent);
			bool runUP = (inParams.runMode == RunModeType.UpperPhase);
			bool runLP = (inParams.runMode == RunModeType.LowerPhase);
			bool showCol = ((runDual && (dispAll || viewparams.phaseDisplay == PhaseDisplayType.UpperLower)) || (timeMode && !dispDualTime));
			bool[] inversePhase = new bool[2];
			int totn = 0;
			float pos = 0;
			float totpos = 0;
			float m;
			float totm = 0;
			float vol;
			float con, con0, maxcon;
			PhaseType phase = new PhaseType();
			int serie;
			int ncol, ncolu, ncoll;
			int ncellsu2, ncellsl2;
			int zone = 0;
			int currentzone = 0;
			Color color;
			float a, r, g, b;

			outSet.time = time;

			if (inParams.fnormu > 0)
			{
				ncellsu2 = (int)((ncellsu - inParams.column2) / inParams.fnormu + inParams.column2);
			}
			else
			{
				ncellsu2 = ncellsu;
			}
			if (inParams.fnorml > 0)
			{
				ncellsl2 = (int)((ncellsl - inParams.column2) / inParams.fnorml + inParams.column2);
			}
			else
			{
				ncellsl2 = ncellsl;
			}

			// inverse upper phase
			inversePhase[0] = !(runLP || (runDual && (dispAll || viewparams.phaseDisplay == PhaseDisplayType.UpperLower || dispLP)));
			// inverse lower phase
			if (runCo || (runDual && dispDualTime))
			{
				inversePhase[1] = !inversePhase[0];
			}
			else
			{
				inversePhase[1] = inversePhase[0];
			}

			if (!showCol && !(runDual && dispDualTime))
			{
				// show column if non-outcol peaks inside
				for (int i = 0; i < ncomps; i++)
				{
					if (!compEluted[i])
					{
						showCol = true;
					}
				}
			}

			if (inversePhase[0] == inversePhase[1] && !dispUP && !dispLP)
			{
				ncells = ncellsu2 + ncellsl2 - inParams.column2;
				if (eeDone0)
				{
					ncells -= inParams.column2;
				}
			}
			else
			{
				if (dispUP && !runLP)
				{
					ncells = ncellsu2;
				}
				else if (dispLP && !runUP)
				{
					ncells = ncellsl2;
				}
				else
				{
					ncells = Math.Max(ncellsu2, ncellsl2);
				}
			}
			if (!showCol)
			{
				ncells -= inParams.column2;
			}

			if (inversePhase[0])
			{
				ncolu = ncellsu2 - inParams.column2;
			}
			else
			{
				ncolu = 0;
			}
			if (!inversePhase[1])
			{
				ncoll = ncellsl2 - inParams.column2;
			}
			else
			{
				ncoll = 0;
			}
			ncol = Math.Max(ncolu, ncoll);
			if (!(runDual && dispDualTime))
			{
				ncolu = ncol;
				ncoll = ncol;
			}
			if (eeDone0 && inversePhase[0] == inversePhase[1] && !dispUP && !dispLP)
			{
				ncoll -= inParams.column2;
			}

			if (dispDual)
			{
				nphases = 2;
			}
			else
			{
				nphases = 1;
			}
			if (viewparams.peaksDisplay == PeaksDisplayType.PeaksSum || viewparams.peaksDisplay == PeaksDisplayType.Sum)
			{
				ncomps2 = ncomps + 1;
			}
			else
			{
				ncomps2 = ncomps;
			}
			nseries = nphases * ncomps2;

			// store axes
			axes = outSet.axes;
			axes.showCol = showCol;
			if (showCol)
			{
				axes.colstart = ncol;
				axes.colend = ncol + inParams.column2;

				if (inParams.vdeadInEnabled)
				{
					axes.showDeadvolstart = true;
					if (inParams.getNormalColDirection() == inversePhase[0])
					{
						axes.deadvolstart = axes.colstart + (inParams.column2 - inParams.getVdeadIn());
					}
					else
					{
						axes.deadvolstart = axes.colstart + inParams.getVdeadIn();
					}
				}
				else
				{
					axes.showDeadvolstart = false;
				}
				if (inParams.vdeadOutEnabled)
				{
					axes.showDeadvolend = true;
					if (inParams.getNormalColDirection() == inversePhase[0])
					{
						axes.deadvolend = axes.colstart + inParams.getVdeadOut();
					}
					else
					{
						axes.deadvolend = axes.colstart + (inParams.column2 - inParams.getVdeadOut());
					}
				}
				else
				{
					axes.showDeadvolend = false;
				}
				if (inParams.vdeadInjectEnabled)
				{
					axes.showDeadvolinsert = true;
					axes.deadvolinjectstart = axes.colstart + inParams.getVdeadInjectStart();
					axes.deadvolinjectend = axes.colstart + inParams.getVdeadInjectEnd();
				}
				else
				{
					axes.showDeadvolinsert = false;
				}
			}

			axes.rangex = ncells;
			if (axes.rangex == 0)
			{
				axes.rangex = 1; // prevent div by zero
			}

			axes.scaleminulabel = 0;
			axes.scalemaxulabel = ncellsu2 - inParams.column2;
			axes.scaleminllabel = 0;
			axes.scalemaxllabel = ncellsl2 - inParams.column2;
			if (inversePhase[0])
			{
				axes.scaleminu = 0;
				axes.scalemaxu = ncolu;
			}
			else
			{
				axes.scaleminu = ncells;
				axes.scalemaxu = ncolu + inParams.column2;
			}
			if (!inversePhase[1])
			{
				axes.scaleminl = 0;
				axes.scalemaxl = ncoll;
			}
			else
			{
				axes.scaleminl = ncells;
				axes.scalemaxl = ncoll + inParams.column2;
			}

			axes.logScale = (viewparams.yScale == YScaleType.Logarithmic);
			axes.update();

			axes.maxcon = new List<float>(ncomps);

			// store peaks
			outSet.comps = storePeaks(cellsu0, cellsl0, ncellsu0, ncellsl0, ncolu, ncoll, inversePhase, axes, viewparams, showCol, eeDone0);

			// init outcells
			outcells = new List<List<OutCell>>(nseries);
			for (compi = 0; compi < nseries; compi++)
			{
				outcells.Add(new List<OutCell>(ncells));
			}
			// store
			for (compi = 0; compi < ncomps; compi++)
			{
				filterWeight = Util.createFilter(runningFilterSigma[compi]);
				for (int phasei = 0; phasei < nphases; phasei++)
				{
					serie = compi * nphases + phasei;
					compcellsu = cellsu0[compi];
					compcellsl = cellsl0[compi];
					if (inParams.runMode == RunModeType.Intermittent && viewparams.peaksDisplay == PeaksDisplayType.IntTotals)
					{
						// int totals mode
						currentzone = 0;
						totpos = 0;
						totm = 0;
						totn = 1;
					}
					maxcon = 0;
					for (int i = 0; i < ncells; i++)
					{
						pos = i;
						m = 0;
						vol = 0;
						if (nphases > 1)
						{
							phase = (PhaseType)(phasei + 1);
						}
						else if (viewparams.phaseDisplay == PhaseDisplayType.Lower)
						{
							phase = PhaseType.Lower;
						}
						else
						{
							phase = PhaseType.Upper;
						}
						if (phase == PhaseType.Upper || dispAll)
						{
							m += compcellsu.getNorm2(i, ncolu, inversePhase[0], filterWeight);
							vol += compcellsu.getStepVol(i, ncolu, inversePhase[0]);
							zone = compcellsu.getNorm2zone(i, ncolu, inversePhase[0]);
						}
						if (phase == PhaseType.Lower || dispAll)
						{
							m += compcellsl.getNorm2(i, ncoll, inversePhase[1], filterWeight);
							vol += compcellsl.getStepVol(i, ncoll, inversePhase[1]);
							zone = compcellsl.getNorm2zone(i, ncoll, inversePhase[1]);
						}
						if (dispDual && (PhaseType)(phasei + 1) == PhaseType.Lower)
						{
							m = -m;
						}
						// convert mass to concentration
						if (vol != 0)
						{
							con = m / vol;
						}
						else
						{
							con = 0;
						}
						if (inParams.runMode == RunModeType.Intermittent && viewparams.peaksDisplay == PeaksDisplayType.IntTotals)
						{
							// intermittent mode: only store zone (mass) totals
							if (zone != currentzone || i == 0)
							{
								if (totn != 0 && totm != 0)
								{
									if (outcells[serie].Count == 0)
									{
										outcells[serie].Add(new OutCell(totpos / totn, 0));
									}
									outcells[serie].Add(new OutCell(totpos / totn, totm));
									if (Math.Abs(totm) > maxcon)
									{
										maxcon = Math.Abs(totm);
									}
								}
								currentzone = zone;
								totpos = 0;
								totm = 0;
								totn = 0;
							}
							totpos += pos;
							totm += m;
							totn++;
						}
						else
						{
							// normal store out
							outcells[serie].Add(new OutCell(pos, con));
							if (Math.Abs(con) > maxcon)
							{
								maxcon = Math.Abs(con);
							}
						}
					}
					axes.maxcon.Add(maxcon);
					if (inParams.runMode == RunModeType.Intermittent && viewparams.peaksDisplay == PeaksDisplayType.IntTotals)
					{
						// int totals mode
						if (outcells[serie].Count != 0)
						{
							totpos = outcells[serie][outcells[serie].Count - 1].pos;
						}
						else
						{
							totpos = 0;
						}
						outcells[serie].Add(new OutCell(totpos, 0));
					}
				}
			}

			if (inParams.runMode != RunModeType.Intermittent || viewparams.peaksDisplay != PeaksDisplayType.IntTotals)
			{
				// if not int totals mode
				color = new Color();
				if (viewparams.peaksDisplay == PeaksDisplayType.PeaksSum || viewparams.peaksDisplay == PeaksDisplayType.Sum)
				{
					// add series for sum
					maxcon = 0;
					for (int i = 0; i < ncells; i++)
					{
						pos = i;
						for (int phasei = 0; phasei < nphases; phasei++)
						{
							con = 0;
							a = 0;
							r = 0;
							g = 0;
							b = 0;
							for (compi = 0; compi < ncomps; compi++)
							{
								serie = compi * nphases + phasei;
								con0 = outcells[serie][i].con;
								con += con0;
								color = Util.colorRange(compi, ncomps);
								r += color.ScR * Math.Abs(con0);
								g += color.ScG * Math.Abs(con0);
								b += color.ScB * Math.Abs(con0);
							}
							if (con != 0)
							{
								a = 1;
								r /= Math.Abs(con);
								g /= Math.Abs(con);
								b /= Math.Abs(con);
							}
							compi = ncomps;
							serie = ncomps * nphases + phasei;
							if (Math.Abs(con) > maxcon)
							{
								maxcon = Math.Abs(con);
							}
							outcells[serie].Add(new OutCell(pos, con, Color.FromScRgb(a, r, g, b)));
						}
					}
					axes.maxcon.Add(maxcon);
				}
			}

			// convert List to array
			outSet.outCells = new OutCell[outcells.Count][];
			for (int i = 0; i < outcells.Count; i++)
			{
				outSet.outCells[i] = new OutCell[outcells[i].Count];
				for (int j = 0; j < outcells[i].Count; j++)
				{
					outSet.outCells[i][j] = outcells[i][j];
				}
			}

			return outSet;
		}

		private float init()
		{
			int ncomps = inParams.comps.Count;

			generalInit();

			ncellsu = 0;
			ncellsl = 0;
			stepu = 0;
			stepl = 0;
			fstepu = 0;
			fstepl = 0;
			cellsu.Clear();
			cellsl.Clear();
			timeCellsu.Clear();
			timeCellsl.Clear();
			ncellsutime.Clear();
			ncellsltime.Clear();
			intSwitchu.Clear();
			intSwitchl.Clear();
			timeTime.Clear();
			runningFilterSigma = new float[ncomps];
			compTotMass = new float[ncomps];
			for (int compi = 0; compi < ncomps; compi++)
			{
				compTotMass[compi] = inParams.comps[compi].m;
			}
			if (inParams.runMode == RunModeType.Intermittent)
			{
				newIntAmount();
			}
			return previewParams.estmaxstep;
		}

		private void create()
		{
			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				cellsu.Add(new UCCDCells(inParams));
				cellsl.Add(new LCCDCells(inParams));
				for (int i = 0; i < inParams.column2; i++)
				{
					cellsu[compi].Add(new Cell());
					cellsl[compi].Add(new Cell());
				}
			}
			ncellsu = inParams.column2;
			ncellsl = inParams.column2;
		}

		private void inject()
		{
			UCCDCells compcellsu;
			LCCDCells compcellsl;
			Comp comp;
			float feedvol = 0;
			float totcellvol = 0;
			float compm;
			bool first = (it == 0);
			int injectPos = (int)inParams.getInjectPosNorm();
			float m0, mu0, ml0;
			float fu, fl;

			if (inParams.injectMode == InjectModeType.Instant)
			{
				if (!first)
				{
					return;
				}
			}
			else
			{
				feedvol = inParams.convertUnit(inParams.injectFeed, inParams.injectFeedUnits, QuantityType.Volume, inParams.injectPhase);
				totcellvol = inParams.getCellVol(inParams.injectPhase);
			}

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				compcellsu = cellsu[compi];
				compcellsl = cellsl[compi];
				comp = inParams.comps[compi];
				if (inParams.injectMode == InjectModeType.Instant)
				{
					compm = comp.m;
				}
				else if (inParams.injectMode == InjectModeType.Continuous)
				{
					compm = comp.concentration;
				}
				else
				{
					compm = comp.m / feedvol * totcellvol;
					if (compm > compTotMass[compi])
					{
						compm = compTotMass[compi];
					}
				}
				if (compTotMass[compi] > 0 || inParams.injectMode == InjectModeType.Continuous)
				{
					if (inParams.injectPhase == PhaseType.Upper)
					{
						m0 = compcellsu.getNorm(injectPos);
						compcellsu.setNorm(injectPos, m0 + compm);
					}
					else if (inParams.injectPhase == PhaseType.Lower)
					{
						m0 = compcellsl.getNorm(injectPos);
						compcellsl.setNorm(injectPos, m0 + compm);
					}
					else
					{
						// make sure to have 'perfect' distribution if sample is inserted in both phases
						mu0 = compcellsu.getNorm(injectPos);
						ml0 = compcellsl.getNorm(injectPos);
						fu = Equations.calcTransferU(inParams.kDefinition, comp.k * inParams.px);
						fl = Equations.calcTransferL(inParams.kDefinition, comp.k * inParams.px);
						compcellsu.setNorm(injectPos, mu0 + compm * fu);
						compcellsl.setNorm(injectPos, ml0 + compm * fl);
					}
					compTotMass[compi] -= compm;
				}
			}
		}

		private void dist()
		{
			Comp comp;
			UCCDCells compcellsu;
			LCCDCells compcellsl;
			float mu, ml, mu0, ml0, mt;
			float fu, fl;
			float eff = inParams.efficiency;
			float neff = 1 - eff;
			int ncomps = inParams.comps.Count;
			int deadvolstartsteps = (int)Math.Round(inParams.getVdeadStart());
			int deadvolendsteps = (int)Math.Round(inParams.getVdeadEnd());
			int deadvolinsertstartsteps = (int)Math.Round(inParams.getVdeadInjectStart());
			int deadvolinsertendsteps = (int)Math.Round(inParams.getVdeadInjectEnd());

			for (int compi = 0; compi < ncomps; compi++)
			{
				comp = inParams.comps[compi];
				compcellsu = cellsu[compi];
				compcellsl = cellsl[compi];
				fu = Equations.calcTransferU(inParams.kDefinition, comp.k * inParams.px);
				fl = Equations.calcTransferL(inParams.kDefinition, comp.k * inParams.px);
				for (int i = 0; i < inParams.column2; i++)
				{
					// don't redistribute in dead volumes
					if (i >= deadvolstartsteps && i < deadvolendsteps && (i < deadvolinsertstartsteps || i >= deadvolinsertendsteps))
					{
						// active cells
						mu0 = compcellsu.getNorm(i);
						ml0 = compcellsl.getNorm(i);
						mt = mu0 + ml0;
						mu = mt * fu;
						ml = mt * fl;
						if (eff == 1)
						{
							compcellsu.setNorm(i, mu);
							compcellsl.setNorm(i, ml);
						}
						else
						{
							compcellsu.setNorm(i, mu * eff + mu0 * neff);
							compcellsl.setNorm(i, ml * eff + ml0 * neff);
						}
					}
					else
					{
						// inactive cells
						if (curPhase == PhaseType.Upper)
						{
							mu0 = compcellsu.getNorm(i);
							ml0 = compcellsl.getNorm(i);
							// move all comp in lower to upper
							if (ml0 > 0)
							{
								mu = mu0 + ml0;
								ml = 0;
								compcellsu.setNorm(i, mu);
								compcellsl.setNorm(i, ml);
							}
						}
						else if (curPhase == PhaseType.Lower)
						{
							mu0 = compcellsu.getNorm(i);
							ml0 = compcellsl.getNorm(i);
							// move all comp in upper to lower
							if (mu0 > 0)
							{
								mu = 0;
								ml = ml0 + mu0;
								compcellsu.setNorm(i, mu);
								compcellsl.setNorm(i, ml);
							}
						}
					}
				}
			}
		}

		private void newIntAmount()
		{
			if (curPhase == PhaseType.Upper)
			{
				intamountu += inParams.convertUnit(inParams.intUpSwitch, (QuantityType)inParams.intMode, inParams.natUnits, PhaseType.Upper);
			}
			else
			{
				intamountl += inParams.convertUnit(inParams.intLpSwitch, (QuantityType)inParams.intMode, inParams.natUnits, PhaseType.Lower);
			}
		}

		private void movePartu()
		{
			Comp comp;
			UCCDCells compcellsu;
			int ncomps = inParams.comps.Count;
			float dm;

			for (int compi = 0; compi < ncomps; compi++)
			{
				comp = inParams.comps[compi];
				compcellsu = cellsu[compi];
				for (int i = compcellsu.Count - inParams.column2; i < compcellsu.Count; i++)
				{
					if (i > 0)
					{
						dm = compcellsu[i].m * inParams.ptransu;
						compcellsu[i - 1].m += dm;
						compcellsu[i].m -= dm;
					}
				}
			}
		}

		private void movePartl()
		{
			Comp comp;
			LCCDCells compcellsl;
			int ncomps = inParams.comps.Count;
			float dm;

			for (int compi = 0; compi < ncomps; compi++)
			{
				comp = inParams.comps[compi];
				compcellsl = cellsl[compi];
				for (int i = compcellsl.Count - inParams.column2; i < compcellsl.Count; i++)
				{
					if (i > 0)
					{
						dm = compcellsl[i].m * inParams.ptransl;
						compcellsl[i - 1].m += dm;
						compcellsl[i].m -= dm;
					}
				}
			}
		}

		private void move()
		{
			UCCDCells compcellsu;
			LCCDCells compcellsl;
			bool intPhaseSwitch = false;
			int ncomps = inParams.comps.Count;
			int fstepu0, fstepl0;

			if (inParams.runMode == RunModeType.Intermittent && inParams.intMode == IntModeType.Component)
			{
				if (curPhase == PhaseType.Upper)
				{
					if (checkFinalColMExu(inParams.intUpComp))
					{
						intPhaseSwitch = true;
					}
				}
				else if (curPhase == PhaseType.Lower)
				{
					if (checkFinalColMExl(inParams.intLpComp))
					{
						intPhaseSwitch = true;
					}
				}
			}

			if (inParams.runMode != RunModeType.Intermittent || inParams.viewUnits == QuantityType.Time || curPhase == PhaseType.Upper)
			{
				fstepu += inParams.fnormu;
				if (inParams.runMode == RunModeType.Intermittent && inParams.intMode != IntModeType.Component && curPhase == PhaseType.Upper)
				{
					intamountu--;
					if (intamountu <= 0)
					{
						intPhaseSwitch = true;
					}
				}
			}
			if (inParams.runMode != RunModeType.Intermittent || inParams.viewUnits == QuantityType.Time || curPhase == PhaseType.Lower)
			{
				fstepl += inParams.fnorml;
				if (inParams.runMode == RunModeType.Intermittent && inParams.intMode != IntModeType.Component && curPhase == PhaseType.Lower)
				{
					intamountl--;
					if (intamountl <= 0)
					{
						intPhaseSwitch = true;
					}
				}
			}

			while ((int)fstepu > stepu)
			{
				for (int compi = 0; compi < ncomps; compi++)
				{
					compcellsu = cellsu[compi];
					if ((inParams.runMode == RunModeType.Intermittent && inParams.viewUnits == QuantityType.Time && curPhase == PhaseType.Lower) || inParams.ptransMode)
					{
						compcellsu.Insert(compcellsu.Count - inParams.column2, new Cell());
					}
					else
					{
						compcellsu.Add(new Cell());
					}
					if (inParams.runMode == RunModeType.Intermittent)
					{
						compcellsu.justOut().zone = intit + 1;
					}
					else
					{
						compcellsu.justOut().zone = 1;
					}
				}
				ncellsu++;
				stepu++;
			}
			if (inParams.ptransMode)
			{
				movePartu();
			}

			while ((int)fstepl > stepl)
			{
				for (int compi = 0; compi < ncomps; compi++)
				{
					compcellsl = cellsl[compi];
					if ((inParams.runMode == RunModeType.Intermittent && inParams.viewUnits == QuantityType.Time && curPhase == PhaseType.Upper) || inParams.ptransMode)
					{
						compcellsl.Insert(compcellsl.Count - inParams.column2, new Cell());
					}
					else
					{
						compcellsl.Add(new Cell());
					}
					if (inParams.runMode == RunModeType.Intermittent)
					{
						compcellsl.justOut().zone = intit + 1;
					}
					else
					{
						compcellsl.justOut().zone = 1;
					}
				}
				ncellsl++;
				stepl++;
			}
			if (inParams.ptransMode)
			{
				movePartl();
			}

			if (intPhaseSwitch && intit / 2 < inParams.intMaxIt)
			{
				fstepu0 = 0;
				for (int i = 0; i < intSwitchu.Count; i++)
				{
					fstepu0 += (int)intSwitchu[i];
				}
				intSwitchu.Add(fstepu - fstepu0);
				fstepl0 = 0;
				for (int i = 0; i < intSwitchl.Count; i++)
				{
					fstepl0 += (int)intSwitchl[i];
				}
				intSwitchl.Add(fstepl - fstepl0);
				if (curPhase == PhaseType.Upper)
				{
					curPhase = PhaseType.Lower;
				}
				else if (curPhase == PhaseType.Lower)
				{
					curPhase = PhaseType.Upper;
				}
				intit++;
				newIntAmount();
			}
		}

		private void doEE()
		{
			UCCDCells compcellsu;
			LCCDCells compcellsl;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				compcellsu = cellsu[compi];
				compcellsl = cellsl[compi];
				for (int i = 0; i < inParams.column2; i++)
				{
					if (inParams.getNormEEdiru())
					{
						compcellsu.Add(new Cell());
					}
					else
					{
						compcellsu.Insert(compcellsu.Count - inParams.column2, new Cell());
					}
					if (inParams.getNormEEdirl())
					{
						compcellsl.Add(new Cell());
					}
					else
					{
						compcellsl.Insert(compcellsl.Count - inParams.column2, new Cell());
					}
				}
				compEluted[compi] = true;
			}
			ncellsu += inParams.column2;
			ncellsl += inParams.column2;
			eeDone = true;
		}

		private bool isDone()
		{
			bool done = false;
			if (inParams.doMaxIt && (inParams.convertUnit(stepu, inParams.natUnits, QuantityType.Volume, PhaseType.Upper) >= inParams.maxIt * inParams.vc || inParams.convertUnit(stepl, inParams.natUnits, QuantityType.Volume, PhaseType.Lower) >= inParams.maxIt * inParams.vc))
			{
				done = true;
			}
			if (inParams.runMode == RunModeType.Intermittent)
			{
				// intermittent mode
				if ((float)intit / 2 >= inParams.intMaxIt)
				{
					done = true;
				}
				if (done && inParams.intFinalElute)
				{
					done = false;
				}
			}
			if (!done)
			{
				if (isInjectDone())
				{
					done = isAllOut();
				}
			}
			return done;
		}

		private bool isInjectDone()
		{
			bool done = true;
			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				// allow for a margin of inaccuracy
				if (compTotMass[compi] > 0.01 * inParams.comps[compi].m)
				{
					done = false;
				}
			}
			return done;
		}

		private bool isAllOut()
		{
			UCCDCells compcellsu;
			LCCDCells compcellsl;
			bool allout = true;
			float mu, ml, mt, mtnorm;
			Comp comp;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				compcellsu = cellsu[compi];
				compcellsl = cellsl[compi];
				comp = inParams.comps[compi];
				if (comp.elute && previewParams.outSet.comps[compi].willElute && !compEluted[compi])
				{
					mt = 0;
					for (int i = 0; i < inParams.column2; i++)
					{
						mu = compcellsu.getNorm(i);
						ml = compcellsl.getNorm(i);
						mt += (mu + ml);
					}
					mtnorm = mt / comp.m;
					if (mtnorm < mincon)
					{
						compEluted[compi] = true;
					}
					else
					{
						allout = false;
						break;
					}
				}
			}
			return allout;
		}

		private bool checkFinalColMExu(int excomp)
		{
			bool found = false;
			UCCDCells compcellsu;
			float pos;
			float m;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				if (compi != excomp)
				{
					compcellsu = cellsu[compi];
					pos = compcellsu.normToModel(0, true);
					m = compcellsu[(int)pos].m;
					if (m > 0.0001)
					{
						found = true;
					}
				}
			}
			return found;
		}

		private bool checkFinalColMExl(int excomp)
		{
			bool found = false;
			LCCDCells compcellsl;
			float pos;
			float m;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				if (compi != excomp)
				{
					compcellsl = cellsl[compi];
					pos = compcellsl.normToModel(0, false);
					m = compcellsl[(int)pos].m;
					if (m > 0.0001)
					{
						found = true;
					}
				}
			}
			return found;
		}

		private void storeTime()
		{
			int ncomps = inParams.comps.Count;

			List<UCCDCells> cellsu0 = new List<UCCDCells>(ncomps);
			List<LCCDCells> cellsl0 = new List<LCCDCells>(ncomps);

			if (optionParams.timeStores == 0)
			{
				return;
			}

			for (int compi = 0; compi < ncomps; compi++)
			{
				cellsu0.Add(new UCCDCells(cellsu[compi]));
				cellsl0.Add(new LCCDCells(cellsl[compi]));
			}
			timeCellsu.Add(cellsu0);
			timeCellsl.Add(cellsl0);

			ncellsutime.Add(ncellsu);
			ncellsltime.Add(ncellsl);

			timeTime.Add(inParams.convertUnit(it, inParams.natUnits, QuantityType.Time, PhaseType.Both));
		}

		private void trimTimeStores()
		{
			int ntimeunits = timeCellsu.Count;

			for (int i = ntimeunits - 1; i > 0; i -= 2)
			{
				timeCellsu.RemoveAt(i);
				timeCellsl.RemoveAt(i);
				ncellsutime.RemoveAt(i);
				ncellsltime.RemoveAt(i);
				timeTime.RemoveAt(i);
			}
		}

		private List<OutComp> storePeaks(List<UCCDCells> cellsu0, List<LCCDCells> cellsl0, int ncellsu0, int ncellsl0, int ncolu0, int ncoll0, bool[] inversePhase, Axes axes, ViewParams viewparams, bool showCol, bool eeDone0)
		{
			List<OutComp> outComps = new List<OutComp>();
			OutComp outComp;
			UCCDCells compcellsu = null;
			LCCDCells compcellsl = null;
			float[] filterWeight = null;
			bool needFilter = (inParams.runMode == RunModeType.DualMode || inParams.runMode == RunModeType.CoCurrent || inParams.ptransMode);
			bool dispUP = (viewparams.phaseDisplay == PhaseDisplayType.Upper);
			bool dispLP = (viewparams.phaseDisplay == PhaseDisplayType.Lower);
			float mt, mu, ml, mu0, ml0;
			float maxmu, maxml;
			float vol, volu, voll;
			float con;
			float maxcon = 0;
			float totm, totmup, totmlp;
			float purity;
			float recovery;
			float mtarget, mother, mall;
			float pos = 0;
			float realpos, normpos;
			float width, hwidth, swidth;
			float sumavg, avg, realavg;
			float totinvaru, totinvarl, totoutvaru, totoutvarl;
			float maxsigma;
			bool done;
			bool up = false;
			int ncomps = inParams.comps.Count;
			int ncol, ncol0, ncolu, ncoll;
			int ncellsu2, ncellsl2, ncells;
			int zone = 0;
			PhaseType phase = new PhaseType();

			if (inParams.fnormu > 0)
			{
				ncellsu2 = (int)((ncellsu0 - inParams.column2) / inParams.fnormu + inParams.column2);
			}
			else
			{
				ncellsu2 = ncellsu0;
			}
			if (inParams.fnorml > 0)
			{
				ncellsl2 = (int)((ncellsl0 - inParams.column2) / inParams.fnorml + inParams.column2);
			}
			else
			{
				ncellsl2 = ncellsl0;
			}
			ncells = ncellsu2 + ncellsl2 - inParams.column2;
			if (eeDone0)
			{
				ncells -= inParams.column2;
			}
			ncol = ncellsl2 - inParams.column2;
			ncol0 = ncol;
			ncoll = ncol;
			if (eeDone0)
			{
				ncol -= inParams.column2;
			}
			ncolu = ncol;

			maxsigma = (float)Math.Max(ncellsu2 - inParams.column2, ncellsl2 - inParams.column2) / 4;
			if (maxsigma > 15)
			{
				maxsigma = 15;
			}

			for (int compi = 0; compi < ncomps; compi++)
			{
				done = false;
				if (inParams.runMode == RunModeType.CoCurrent)
				{
					runningFilterSigma[compi] = 1;
				}
				else
				{
					runningFilterSigma[compi] = 0;
				}
				while (!done)
				{
					compcellsu = cellsu0[compi];
					compcellsl = cellsl0[compi];
					filterWeight = Util.createFilter(runningFilterSigma[compi]);
					pos = 0;
					maxcon = 0;
					maxmu = 0;
					maxml = 0;
					mu0 = 0;
					ml0 = 0;
					totinvaru = 0;
					totinvarl = 0;
					totoutvaru = 0;
					totoutvarl = 0;
					for (int i = 0; i < ncells; i++)
					{
						mu = compcellsu.getNorm2(i, ncolu, false, filterWeight);
						ml = compcellsl.getNorm2(i, ncoll, false, filterWeight);
						mt = mu + ml;
						// convert mass to concentration
						volu = compcellsu.getStepVol(i, ncolu, false);
						voll = compcellsl.getStepVol(i, ncoll, false);
						vol = volu + voll;
						con = mt / vol;
						if (con > maxcon)
						{
							pos = i;
							maxcon = con;
							up = (mu > ml);
							if (up)
							{
								zone = compcellsu.getNorm2zone(i, ncol, false);
							}
							else
							{
								zone = compcellsl.getNorm2zone(i, ncol, false);
							}
						}
						if (mu > maxmu)
						{
							maxmu = mu;
						}
						if (ml > maxml)
						{
							maxml = ml;
						}
						if (i == 0 || i == ncol || i == ncol + inParams.column2)
						{
							mu0 = mu;
							ml0 = ml;
						}
						if (i >= ncol && i <= ncol + inParams.column2)
						{
							totinvaru += Math.Abs(mu - mu0);
							totinvarl += Math.Abs(ml - ml0);
						}
						else
						{
							totoutvaru += Math.Abs(mu - mu0);
							totoutvarl += Math.Abs(ml - ml0);
						}
						mu0 = mu;
						ml0 = ml;
					}
					totinvaru /= (maxmu * 2);
					totinvarl /= (maxml * 2);
					totoutvaru /= (maxmu * 2);
					totoutvarl /= (maxml * 2);
					if (inParams.autoFilter && needFilter && (totinvaru > filterthreshold || totinvarl > filterthreshold || totoutvaru > filterthreshold || totoutvarl > filterthreshold) && runningFilterSigma[compi] < maxsigma)
					{
						runningFilterSigma[compi]++;
						done = false;
					}
					else
					{
						done = true;
					}
				}
				hwidth = calcHeightWidth(compcellsu, compcellsl, filterWeight, ncells, ncolu, ncoll, pos, maxcon);
				swidth = calcSlopeWidth(compcellsu, compcellsl, filterWeight, ncells, ncolu, ncoll, pos);
				width = Equations.calcBestWidth(hwidth, swidth);

				// purity (+ avg)
				mtarget = 0;
				mall = 0;
				totm = 0;
				totmup = 0;
				totmlp = 0;
				sumavg = 0;
				for (int i = 0; i < ncells; i++)
				{
					mu = compcellsu.getNorm2(i, ncolu, false, filterWeight);
					ml = compcellsl.getNorm2(i, ncoll, false, filterWeight);
					mt = mu + ml;
					volu = compcellsu.getStepVol(i, ncolu, false);
					voll = compcellsl.getStepVol(i, ncoll, false);
					vol = volu + voll;
					con = mt / vol;
					sumavg += (i * mt);
					totm += mt;
					totmup += mu;
					totmlp += ml;
					if (con > mincon)
					{
						// target component present
						mtarget += mt;
						for (int c = 0; c < ncomps; c++)
						{
							mu = cellsu0[c].getNorm2(i, ncolu, false, filterWeight);
							ml = cellsl0[c].getNorm2(i, ncoll, false, filterWeight);
							mt = mu + ml;
							mall += mt;
						}
					}
				}
				avg = sumavg / totm;
				purity = mtarget / mall;

				// recovery
				mtarget = 0;
				for (int i = 0; i < ncells; i++)
				{
					mother = 0;
					for (int c = 0; c < ncomps; c++)
					{
						if (c != compi)
						{
							mu = cellsu0[c].getNorm2(i, ncolu, false, filterWeight);
							ml = cellsl0[c].getNorm2(i, ncoll, false, filterWeight);
							mt = mu + ml;
							mother += mt;
						}
					}
					if (mother < mincon)
					{
						mu = compcellsu.getNorm2(i, ncolu, false, filterWeight);
						ml = compcellsl.getNorm2(i, ncoll, false, filterWeight);
						mt = mu + ml;
						mtarget += mt;
					}
				}
				recovery = mtarget;

				outComp = new OutComp(inParams.comps[compi]);
				outComp.height = maxcon;
				outComp.outCol = (pos < ncol || pos >= ncol + inParams.column2);
				outComp.eluted = (pos < ncol || pos >= ncol0 + inParams.column2);
				if (inParams.runMode == RunModeType.Intermittent && zone > 0)
				{
					outComp.intIt = (float)(zone - 1) / 2;
					outComp.intItSet = true;
				}
				outComp.totm = totm;
				outComp.totmup = totmup;
				outComp.totmlp = totmlp;
				outComp.purity = purity;
				outComp.recovery = recovery;

				// official phase
				if (up)
				{
					outComp.phase = PhaseType.Upper;
				}
				else
				{
					outComp.phase = PhaseType.Lower;
				}
				// phase used for conversions
				phase = outComp.phase;
				if (phase == PhaseType.Upper && inParams.fu == 0)
				{
					phase = PhaseType.Lower;
					up = false;
				}
				else if (phase == PhaseType.Lower && inParams.fl == 0)
				{
					phase = PhaseType.Upper;
					up = true;
				}
				// convert norm2 position into real position:
				if (phase == PhaseType.Upper)
				{
					realpos = modelToReal(pos, ncells, (inParams.runMode != RunModeType.CoCurrent));
					realavg = modelToReal(avg, ncells, (inParams.runMode != RunModeType.CoCurrent));
				}
				else
				{
					realpos = modelToReal(pos, ncells, false);
					realavg = modelToReal(avg, ncells, false);
				}
				if (outComp.outCol)
				{
					outComp.retention = inParams.convertUnit(realpos, inParams.natUnits, viewparams.viewUnits, phase);
					outComp.average = inParams.convertUnit(realavg, inParams.natUnits, viewparams.viewUnits, phase);
					outComp.width = inParams.convertUnit(width, inParams.natUnits, viewparams.viewUnits, phase);
					outComp.hwidth = inParams.convertUnit(hwidth, inParams.natUnits, viewparams.viewUnits, phase);
					outComp.swidth = inParams.convertUnit(swidth, inParams.natUnits, viewparams.viewUnits, phase);
				}
				else
				{
					// if not outcol: overwrite with column value
					realpos = pos - ncol;
					realavg = avg - ncol;
					outComp.retention = inParams.convertColUnit(realpos, inParams.natUnits, viewparams.viewUnits);
					outComp.average = inParams.convertColUnit(realavg, inParams.natUnits, viewparams.viewUnits);
					outComp.width = inParams.convertColUnit(width, inParams.natUnits, viewparams.viewUnits);
					outComp.hwidth = inParams.convertColUnit(hwidth, inParams.natUnits, viewparams.viewUnits);
					outComp.swidth = inParams.convertColUnit(swidth, inParams.natUnits, viewparams.viewUnits);
					outComp.phase = PhaseType.None;
				}
				outComp.sigma = outComp.width / 4;

				// convert norm2 position back into draw position:
				if (up)
				{
					normpos = compcellsu.norm2ToNorm(pos, ncol);
					outComp.drawPosition = compcellsu.normToNorm2(normpos, ncolu0, inversePhase[0]);
				}
				else
				{
					normpos = compcellsl.norm2ToNorm(pos, ncol);
					outComp.drawPosition = compcellsl.normToNorm2(normpos, ncoll0, inversePhase[1]);
				}
				outComp.filterSigma = runningFilterSigma[compi];

				outComps.Add(outComp);
			}
			return outComps;
		}

		private float calcHeightWidth(UCCDCells compcellsu, LCCDCells compcellsl, float[] filterWeight, float ncells, float ncolu, float ncoll, float peakpos, float peakheight)
		{
			// peak width : determine at set height
			float width;
			float wheight = 0.6065f * peakheight;
			float pos1 = 0;
			float pos2 = 0;
			float mt, mu, ml;
			float vol, volu, voll;
			float con;
			float lastcon = 0;

			for (int i = (int)peakpos; i > 0; i--)
			{
				mu = compcellsu.getNorm2((float)i, (int)ncolu, false, filterWeight);
				ml = compcellsl.getNorm2((float)i, (int)ncoll, false, filterWeight);
				mt = mu + ml;
				volu = compcellsu.getStepVol(i, (int)ncolu, false);
				voll = compcellsl.getStepVol(i, (int)ncoll, false);
				vol = volu + voll;
				con = mt / vol;
				if (con < wheight && i != peakpos)
				{
					// interpolate
					pos1 = Util.calcCorX(i + 1, i, lastcon, con, wheight);
					break;
				}
				lastcon = con;
			}
			for (int i = (int)peakpos; i < ncells; i++)
			{
				mu = compcellsu.getNorm2((float)i, (int)ncolu, false, filterWeight);
				ml = compcellsl.getNorm2((float)i, (int)ncoll, false, filterWeight);
				mt = mu + ml;
				volu = compcellsu.getStepVol(i, (int)ncolu, false);
				voll = compcellsl.getStepVol(i, (int)ncoll, false);
				vol = volu + voll;
				con = mt / vol;
				if (con < wheight && i != peakpos)
				{
					// interpolate
					pos2 = Util.calcCorX(i - 1, i, lastcon, con, wheight);
					break;
				}
				lastcon = con;
			}
			width = 2 * Math.Abs(pos2 - pos1);
			return width;
		}

		private float calcSlopeWidth(UCCDCells compcellsu, LCCDCells compcellsl, float[] filterWeight, float ncells, float ncolu, float ncoll, float peakpos)
		{
			// peak width : look for slopes closest to max peak value
			float width;
			float pos1, pos2;
			float maxpos = 0;
			float maxpos0 = 0;
			float mt, mu, ml;
			float vol, volu, voll;
			float con;
			float con0 = 0;
			float maxcon = 0;
			float maxcon0 = 0;
			float dcon, maxdcon;

			maxdcon = 0;
			for (int i = (int)peakpos; i > 0; i--)
			{
				mu = compcellsu.getNorm2((float)i, (int)ncolu, false, filterWeight);
				ml = compcellsl.getNorm2((float)i, (int)ncoll, false, filterWeight);
				mt = mu + ml;
				volu = compcellsu.getStepVol(i, (int)ncolu, false);
				voll = compcellsl.getStepVol(i, (int)ncoll, false);
				vol = volu + voll;
				con = mt / vol;
				if (i != peakpos)
				{
					dcon = con0 - con;
					if (dcon > maxdcon)
					{
						maxdcon = dcon;
						maxcon = con;
						maxcon0 = con0;
						maxpos = i;
						maxpos0 = i + 1;
					}
				}
				con0 = con;
			}
			// extrapolate slope (y = 0)
			pos1 = Util.calcCorX(maxpos0, maxpos, maxcon0, maxcon, 0);

			maxdcon = 0;
			for (int i = (int)peakpos; i < ncells; i++)
			{
				mu = compcellsu.getNorm2((float)i, (int)ncolu, false, filterWeight);
				ml = compcellsl.getNorm2((float)i, (int)ncoll, false, filterWeight);
				mt = mu + ml;
				volu = compcellsu.getStepVol(i, (int)ncolu, false);
				voll = compcellsl.getStepVol(i, (int)ncoll, false);
				vol = volu + voll;
				con = mt / vol;
				if (i != peakpos)
				{
					dcon = con0 - con;
					if (dcon > maxdcon)
					{
						maxdcon = dcon;
						maxcon = con;
						maxcon0 = con0;
						maxpos = i;
						maxpos0 = i - 1;
					}
				}
				con0 = con;
			}
			// extrapolate slope (y = 0)
			pos2 = Util.calcCorX(maxpos0, maxpos, maxcon0, maxcon, 0);

			width = Math.Abs(pos2 - pos1);
			return width;
		}

		private float modelToReal(float val, int ncells, bool inversePhase)
		{
			if (inversePhase)
			{
				return ncells - val;
			}
			return val;
		}

	}
}
