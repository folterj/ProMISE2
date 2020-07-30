using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ProMISE2
{
	public class TransModel : Model
	{
		List<TransCon> conu = new List<TransCon>(); // conu[comp][n]
		List<TransCon> conl = new List<TransCon>(); // conl[comp][n]
		List<List<TransCon>> timeConu = new List<List<TransCon>>(); // timeConu[time][comp][n]
		List<List<TransCon>> timeConl = new List<List<TransCon>>(); // timeConl[time][comp][n]
		float[] compTotMass; // total insertion mass [comp]
		int columnsteps;
		int timesteps;
		float runcols;
		float minconnorm;
		float dx;
		float dt;
		float dtreal;
		float dtx;
		float fdtxu;
		float fdtxl;
		bool fullmasstransfer;

		public TransModel(ProModelInterface model, InParamsExt inParams, OutParams previewParams, OptionParams optionParams)
			: base(model, inParams, previewParams, optionParams)
		{
			mincon = 0.001f;
		}

		public override void run()
		{
			float estit;
			float storestep;
			float storeit = 0;
			int nstores = 0;

			minconnorm = mincon / (inParams.vc / inParams.column);
			fullmasstransfer = false;
			columnsteps = inParams.column2;
			timesteps = (int)Math.Round(columnsteps / optionParams.cflConstant); // general CFL stability condition
			dx = 1.0f / columnsteps;
			dt = 1.0f / timesteps;
			dtreal = inParams.Tmnorm * dt;
			dtx = (float)columnsteps / timesteps;

			it = 0;
			running = true;

			model.updateProgress(0);
			estit = init();
			storestep = estit / optionParams.timeStores;
			create();
			inject();
			storeTime();
			transfer();
			storeTime();
			it++;

			while (!isDone() && running)
			{
				transfer();
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
				inject();
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
			outParams.outSet = storeOutVar(conu, conl, viewParams, false, 0);
		}

		public override void storeTimeOut(ViewParams viewParams)
		{
			int ntime;

			model.updateProgress(0);
			ntime = timeConu.Count;
			outParams.timeOutSet = new List<OutSet>(ntime);
			for (int timei = 0; timei < ntime; timei++)
			{
				outParams.timeOutSet.Add(storeOutVar(timeConu[timei], timeConl[timei], viewParams, true, timeTime[timei]));
				model.updateProgress((float)timei / ntime);
			}
			model.clearProgress();
		}

		private OutSet storeOutVar(List<TransCon> conu0, List<TransCon> conl0, ViewParams viewparams, bool timeMode, float time)
		{
			OutSet outSet = new OutSet(inParams);
			TransCon compconu;
			TransCon compconl;
			List<List<OutCell>> outcells;
			Axes axes;
			int nseries;
			int ncomps2;
			int nphases;
			int nstepsu, nstepsl, nsteps;
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
			int offsetu, offsetl, offset;
			float pos;
			float con, con0, maxcon;
			PhaseType phase = new PhaseType();
			int serie;
			Color color;
			float a, r, g, b;

			outSet.time = time;

			// inverse upper phase
			inversePhase[0] = !(runLP || (runDual && (dispAll || viewparams.phaseDisplay == PhaseDisplayType.UpperLower || dispLP)));
			// inverse lower phase
			if (runCo || (runDual && dispDualTime))
				inversePhase[1] = inversePhase[0];
			else
				inversePhase[1] = !inversePhase[0];

			if (!showCol && !(runDual && dispDualTime))
			{
				// show column if non-outcol peaks inside
				for (int i = 0; i < ncomps; i++)
				{
					if (!compEluted[i])
						showCol = true;
				}
			}

			nstepsu = 0;
			nstepsl = 0;
			for (compi = 0; compi < ncomps; compi++)
			{
				nstepsu = Math.Max(nstepsu, conu[0].Count);
				nstepsl = Math.Max(nstepsl, conl[0].Count);
			}

			/*
				// alternative method (in case simple length can't be used)
				if (inParams->fu != 0)
					nstepsu = (runcols + 1) * columnsteps;
				else
					nstepsu = columnsteps;
				if (inParams->fl != 0)
					nstepsl = (runcols + 1) * columnsteps;
				else
					nstepsl = columnsteps;
			*/

			offset = 0;
			if (inversePhase[0])
				offset = nstepsu;
			if (inversePhase[1])
				offset = Math.Max(offset, nstepsl);

			if (inversePhase[0])
				offsetu = nstepsu;
			else
				offsetu = offset - inParams.column2;

			if (inversePhase[1])
				offsetl = nstepsl;
			else
				offsetl = offset - inParams.column2;

			if (inversePhase[0] != inversePhase[1] && !dispUP && !dispLP)
			{
				nsteps = nstepsu + nstepsl - inParams.column2;
			}
			else
			{
				if (dispUP && !runLP)
				{
					nsteps = nstepsu;
				}
				else if (dispLP && !runUP)
				{
					nsteps = nstepsl;
				}
				else
				{
					nsteps = Math.Max(nstepsu, nstepsl);
				}
			}
			if (!showCol)
				nsteps -= inParams.column2;

			if (dispDual)
				nphases = 2;
			else
				nphases = 1;
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
				axes.colstart = offset - inParams.column2;
				axes.colend = offset;

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

			axes.rangex = nsteps;
			if (axes.rangex == 0)
			{
				axes.rangex = 1; // prevent div by zero
			}

			axes.scaleminulabel = 0;
			axes.scalemaxulabel = nstepsu - inParams.column2;
			axes.scaleminllabel = 0;
			axes.scalemaxllabel = nstepsl - inParams.column2;
			if (inversePhase[0])
			{
				axes.scaleminu = 0;
				axes.scalemaxu = nstepsu - inParams.column2;
			}
			else
			{
				axes.scaleminu = nsteps;
				axes.scalemaxu = offset;
			}
			if (inversePhase[1])
			{
				axes.scaleminl = 0;
				axes.scalemaxl = nstepsl - inParams.column2;
			}
			else
			{
				axes.scaleminl = nsteps;
				axes.scalemaxl = offset;
			}

			axes.logScale = (viewparams.yScale == YScaleType.Logarithmic);
			axes.update();

			axes.maxcon = new List<float>(ncomps);

			// store peaks
			outSet.comps = storePeaks(conu0, conl0, offsetu, offsetl, inversePhase, axes, viewparams, showCol);

			// init outcells
			outcells = new List<List<OutCell>>(nseries);
			for (int i = 0; i < nseries; i++)
			{
				outcells.Add(new List<OutCell>(nsteps));
			}
			for (compi = 0; compi < ncomps; compi++)
			{
				for (int phasei = 0; phasei < nphases; phasei++)
				{
					serie = compi * nphases + phasei;
					compconu = conu0[compi];
					compconl = conl0[compi];
					maxcon = 0;
					for (int i = 0; i < nsteps; i++)
					{
						pos = i;
						con = 0;
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
							con += compconu.getNormCon(i, offsetu, inversePhase[0]);
						}
						if (phase == PhaseType.Lower || dispAll)
						{
							con += compconl.getNormCon(i, offsetl, inversePhase[1]);
						}
						if (con > maxcon)
						{
							maxcon = con;
						}
						if (dispDual && (PhaseType)(phasei + 1) == PhaseType.Lower)
						{
							con = -con;
						}
						outcells[serie].Add(new OutCell(pos, con));
					}
					axes.maxcon.Add(maxcon);
				}
			}

			color = new Color();

			if (viewparams.peaksDisplay == PeaksDisplayType.PeaksSum || viewparams.peaksDisplay == PeaksDisplayType.Sum)
			{
				// add series for sum
				for (int i = 0; i < nsteps; i++)
				{
					pos = i;
					maxcon = 0;
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

			fdtxu = 0;
			fdtxl = 0;
			runcols = 0;
			intit = 0;
			eeDone = false;
			conu.Clear();
			conl.Clear();
			timeTime.Clear();
			compTotMass = new float[ncomps];
			for (int compi = 0; compi < ncomps; compi++)
			{
				compTotMass[compi] = inParams.comps[compi].m;
			}
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
				newIntAmount();
			}
			else
			{
				curPhase = PhaseType.Both;
			}
			float tmnorm = inParams.Tmnorm;
			return previewParams.estmaxtime / dtreal;
		}

		private void create()
		{
			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				conu.Add(new TransCon(inParams));
				conl.Add(new TransCon(inParams));
				for (int i = 0; i < columnsteps; i++)
				{
					conu[compi].Add(0);
					conl[compi].Add(0);
				}
			}
		}

		private void inject()
		{
			Comp comp;
			float feedtime = 0;
			float compm;
			float fu;
			float fl;
			int insposu;
			int insposl;
			float tmnorm = inParams.Tmnorm;
			bool first = (it == 0);

			insposu = (int)inParams.getInjectPosNorm();
			if (inParams.runMode == RunModeType.CoCurrent)
			{
				insposl = insposu;
			}
			else
			{
				insposl = inParams.column2 - 1 - insposu;
			}

			if (inParams.injectMode == InjectModeType.Instant)
			{
				if (!first)
				{
					return;
				}
			}
			else
			{
				feedtime = inParams.convertUnit(inParams.injectFeed, inParams.injectFeedUnits, QuantityType.Time, inParams.injectPhase);
			}

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
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
					compm = comp.m / feedtime * dtreal;
					if (compm > compTotMass[compi])
						compm = compTotMass[compi];
				}
				if (compTotMass[compi] > 0 || inParams.injectMode == InjectModeType.Continuous)
				{
					if (inParams.injectPhase == PhaseType.Upper)
					{
						conu[compi][insposu] += compm;
					}
					else if (inParams.injectPhase == PhaseType.Lower)
					{
						conl[compi][insposl] += compm;
					}
					else
					{
						// make sure to have 'perfect' distribution if sample is inserted in both phases
						fu = Equations.calcTransferU(inParams.kDefinition, comp.k * inParams.px);
						fl = Equations.calcTransferL(inParams.kDefinition, comp.k * inParams.px);
						conu[compi][insposu] += fu * compm;
						conl[compi][insposl] += fl * compm;
					}
					compTotMass[compi] -= compm;
				}
			}
		}

		private void transfer()
		{
			TransCon newcompconu;
			TransCon newcompconl;
			TransCon compconu;
			TransCon compconl;
			Comp comp;
			float cu, cl, cu0, cl0;
			float k;
			float dc, dcu, dcl;
			float tmu = inParams.Tmu * 60; // assume [min] => [s]
			float tml = inParams.Tml * 60; // assume [min] => [s]
			float tmnorm = inParams.Tmnorm * 60; // assume [min] => [s]
			float tm;
			bool inverseu, inversel;
			int offsetu, offsetl;
			int nsteps;
			int i, i0, j;
			float ka = inParams.ka;
			bool intPhaseSwitch = false;
			int deadvolstartsteps = (int)Math.Round(inParams.getVdeadStart());
			int deadvolendsteps = (int)Math.Round(inParams.getVdeadEnd());
			int deadvolinsertstartsteps = (int)Math.Round(inParams.getVdeadInjectStart());
			int deadvolinsertendsteps = (int)Math.Round(inParams.getVdeadInjectEnd());

			if (inParams.runMode == RunModeType.CoCurrent)
			{
				inverseu = false;
				inversel = false;
				offsetu = 0;
				offsetl = 0;
			}
			else
			{
				inverseu = false;
				inversel = true;
				offsetu = 0;
				offsetl = inParams.column2;
			}

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				compconu = conu[compi];
				compconl = conl[compi];
				comp = inParams.comps[compi];

				if (fullmasstransfer)
				{
					nsteps = compconu.Count + compconl.Count - inParams.column2;
					if (inParams.runMode != RunModeType.CoCurrent)
					{
						offsetu = compconl.Count - inParams.column2;
						offsetl = compconl.Count;
					}
				}
				else
				{
					nsteps = inParams.column2;
				}

				// buffer for modified values
				newcompconu = new TransCon(compconu);
				newcompconl = new TransCon(compconl);

				k = comp.k;

				// phase movement (on buffered values)

				// mobile upper phase movement
				if (curPhase == PhaseType.Upper || curPhase == PhaseType.Both)
				{
					for (i = nsteps - 1; i >= 0; i--)
					{
						if (inParams.runMode == RunModeType.CoCurrent && i > inParams.column2)
						{
							tm = tmnorm;
						}
						else
						{
							tm = tmu;
						}

						cu = compconu.getNorm(i, offsetu, inverseu);
						cu0 = compconu.getNorm(i - 1, offsetu, inverseu);

						dcu = -tmnorm / tm * (cu - cu0) / dx;
						if (dcu != 0)
						{
							cu = newcompconu.getNorm(i, offsetu, inverseu);
							newcompconu.setNorm(i, offsetu, inverseu, cu + dcu * dt);
						}
					}
				}
				// mobile lower phase movement
				if (curPhase == PhaseType.Lower || curPhase == PhaseType.Both)
				{
					for (j = 0; j < nsteps; j++)
					{
						if (inParams.runMode == RunModeType.CoCurrent)
						{
							i = nsteps - 1 - j;
							// normal order: current OutCell - previous OutCell
							i0 = i - 1;
						}
						else
						{
							i = j;
							// inverse order: current OutCell - next OutCell
							i0 = i + 1;
						}
						if (inParams.runMode == RunModeType.CoCurrent && i > inParams.column2)
						{
							tm = tmnorm;
						}
						else
						{
							tm = tml;
						}

						cl = compconl.getNorm(i, offsetl, inversel);
						cl0 = compconl.getNorm(i0, offsetl, inversel);

						dcl = -tmnorm / tm * (cl - cl0) / dx;
						if (dcl != 0)
						{
							cl = newcompconl.getNorm(i, offsetl, inversel);
							newcompconl.setNorm(i, offsetl, inversel, cl + dcl * dt);
						}
					}
				}

				// copy back buffer: overwrite original values
				conu[compi] = newcompconu;
				conl[compi] = newcompconl;

				compconu = conu[compi];
				compconl = conl[compi];

				// do transfer between phases, over moved phases (on original values)

				// transfer between phases
				if (inParams.runMode != RunModeType.CoCurrent)
				{
					offsetu = 0;
					offsetl = inParams.column2;
				}
				for (i = 0; i < inParams.column2; i++)
				{
					// don't redistribute in dead volumes
					if (i >= deadvolstartsteps && i < deadvolendsteps && (i < deadvolinsertstartsteps || i >= deadvolinsertendsteps))
					{
						// active cells
						cu = compconu.getNorm(i, offsetu, inverseu);
						cl = compconl.getNorm(i, offsetl, inversel);

						dc = Equations.calcDc(inParams.kDefinition, cu, cl, k);
						dcu = tmnorm * ka / inParams.uf * dc;
						dcl = -tmnorm * ka / inParams.lf * dc;
						if (dcu != 0)
						{
							compconu.setNorm(i, offsetu, inverseu, cu + dcu * dt);
						}
						if (dcl != 0)
						{
							compconl.setNorm(i, offsetl, inversel, cl + dcl * dt);
						}
					}
				}
			}

			if (curPhase == PhaseType.Upper || curPhase == PhaseType.Both || (inParams.runMode == RunModeType.Intermittent && inParams.viewUnits == QuantityType.Time))
			{
				if (inParams.runMode == RunModeType.CoCurrent)
				{
					tm = tmnorm;
				}
				else
				{
					tm = tmu;
				}
				fdtxu += dtx * tmnorm / tm;

				while (fdtxu > 1)
				{
					for (int compi = 0; compi < inParams.comps.Count; compi++)
					{
						compconu = conu[compi];
						if (curPhase == PhaseType.Upper || curPhase == PhaseType.Both)
						{
							if (fullmasstransfer)
							{
								compconu.Add(0);
							}
							else
							{
								compconu.insertAfterCol(compconu.getLastCol());
							}
						}
						else
						{
							compconu.insertAfterCol(0);
						}
					}
					fdtxu -= 1;
				}
			}
			if (curPhase == PhaseType.Lower || curPhase == PhaseType.Both || (inParams.runMode == RunModeType.Intermittent && inParams.viewUnits == QuantityType.Time))
			{
				if (inParams.runMode == RunModeType.CoCurrent)
				{
					tm = tmnorm;
				}
				else
				{
					tm = tml;
				}
				fdtxl += dtx * tmnorm / tm;

				while (fdtxl > 1)
				{
					for (int compi = 0; compi < inParams.comps.Count; compi++)
					{
						compconl = conl[compi];
						if (curPhase == PhaseType.Lower || curPhase == PhaseType.Both)
						{
							if (fullmasstransfer)
							{
								compconl.Add(0);
							}
							else
							{
								compconl.insertAfterCol(compconl.getLastCol());
							}
						}
						else
						{
							compconl.insertAfterCol(0);
						}
					}
					fdtxl -= 1;
				}
			}

			if (inParams.runMode == RunModeType.Intermittent)
			{
				if (inParams.intMode != IntModeType.Component)
				{
					if (curPhase == PhaseType.Upper)
					{
						intamountu -= dtreal;
						if (intamountu <= 0)
						{
							intPhaseSwitch = true;
						}
					}
					if (curPhase == PhaseType.Lower)
					{
						intamountl -= dtreal;
						if (intamountl <= 0)
						{
							intPhaseSwitch = true;
						}
					}
				}

				if (intPhaseSwitch && intit / 2 < inParams.intMaxIt)
				{
					if (curPhase == PhaseType.Upper)
					{
						curPhase = PhaseType.Lower;
					}
					else
					{
						curPhase = PhaseType.Upper;
					}
					intit++;
					newIntAmount();
				}
			}

			runcols += (1.0f / timesteps);
		}

		private void newIntAmount()
		{
			if (curPhase == PhaseType.Upper)
			{
				intamountu += inParams.convertUnit(inParams.intUpSwitch, (QuantityType)inParams.intMode, QuantityType.Time, PhaseType.Upper);
			}
			else
			{
				intamountl += inParams.convertUnit(inParams.intLpSwitch, (QuantityType)inParams.intMode, QuantityType.Time, PhaseType.Lower);
			}
		}

		private void doEE()
		{
			TransCon compconu;
			TransCon compconl;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				compconu = conu[compi];
				compconl = conl[compi];
				for (int i = 0; i < columnsteps; i++)
				{
					if (inParams.getNormEEdiru())
						compconu.insertStartCol(0);
					else
						compconu.insertAfterCol(0);
					if (inParams.getNormEEdirl())
						compconl.insertStartCol(0);
					else
						compconl.insertAfterCol(0);
				}
				compEluted[compi] = true;
			}
			runcols += 1;
			eeDone = true;
		}

		private bool isDone()
		{
			bool done = false;
			if (inParams.doMaxIt && (inParams.convertUnit(it * dtreal, QuantityType.Time, QuantityType.Volume, PhaseType.Upper) >= inParams.maxIt * inParams.vc || inParams.convertUnit(it * dtreal, QuantityType.Time, QuantityType.Volume, PhaseType.Lower) >= inParams.maxIt * inParams.vc))
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
				if (compTotMass[compi] > 0.01f * inParams.comps[compi].m)
					done = false;
			}
			return done;
		}

		private bool isAllOut()
		{
			TransCon compconu;
			TransCon compconl;
			bool allout = true;
			float mu;
			float ml;
			float mt;
			float mtnorm;
			Comp comp;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				compconu = conu[compi];
				compconl = conl[compi];
				comp = inParams.comps[compi];
				if (comp.elute && previewParams.outSet.comps[compi].willElute && !compEluted[compi])
				{
					mt = 0;
					for (int i = 0; i < inParams.column2; i++)
					{
						mu = compconu[i];
						ml = compconl[i];
						mt += (mu + ml);
					}
					mtnorm = mt / comp.m;
					if (mtnorm < minconnorm)
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

		private void storeTime()
		{
			int ncomps = inParams.comps.Count;

			List<TransCon> conu0 = new List<TransCon>(ncomps);
			List<TransCon> conl0 = new List<TransCon>(ncomps);

			if (optionParams.timeStores == 0)
			{
				return;
			}

			for (int compi = 0; compi < ncomps; compi++)
			{
				conu0.Add(new TransCon(conu[compi]));
				conl0.Add(new TransCon(conl[compi]));
			}
			timeConu.Add(conu0);
			timeConl.Add(conl0);

			timeTime.Add(it * dtreal);
		}

		private void trimTimeStores()
		{
			int ntimeunits = timeConu.Count;

			for (int i = ntimeunits - 1; i > 0; i -= 2)
			{
				timeConu.RemoveAt(i);
				timeConl.RemoveAt(i);
				timeTime.RemoveAt(i);
			}
		}

		private List<OutComp> storePeaks(List<TransCon> conu0, List<TransCon> conl0, int offsetu0, int offsetl0, bool[] inversePhase, Axes axes, ViewParams viewparams, bool showCol)
		{
			List<OutComp> outComps = new List<OutComp>();
			OutComp outComp;
			TransCon compconu;
			TransCon compconl;
			int ncomps = inParams.comps.Count;
			float cu, cl;
			float con, maxcon;
			float pos, normpos, realpos;
			float width, hwidth, swidth;
			float sumavg, avg;
			float normavg, realavg;
			float totm, totmup, totmlp;
			bool up;
			int nsteps;
			int offsetu = 0;
			int offsetl = 0;
			bool inverseu, inversel;
			float contarget, conother, conall;
			float purity;
			float recovery;

			if (inParams.runMode == RunModeType.CoCurrent)
			{
				inverseu = false;
				inversel = false;
				offsetu = 0;
				offsetl = 0;
			}
			else
			{
				inverseu = false;
				inversel = true;
			}

			for (int compi = 0; compi < ncomps; compi++)
			{
				compconu = conu0[compi];
				compconl = conl0[compi];
				nsteps = compconu.Count + compconl.Count - inParams.column2;
				if (inParams.runMode != RunModeType.CoCurrent)
				{
					offsetu = compconl.Count - inParams.column2;
					offsetl = compconl.Count;
				}

				pos = 0;
				maxcon = 0;
				normpos = 0;
				up = false;
				for (int i = 0; i < nsteps; i++)
				{
					cu = compconu.getNormCon(i, offsetu, inverseu);
					cl = compconl.getNormCon(i, offsetl, inversel);
					con = cu + cl;
					if (con > maxcon)
					{
						maxcon = con;
						normpos = i;
						up = (cu > cl);
					}
				}
				hwidth = calcHeightWidth(compconu, compconl, offsetu, offsetl, inverseu, inversel, nsteps, normpos, maxcon);
				swidth = calcSlopeWidth(compconu, compconl, offsetu, offsetl, inverseu, inversel, nsteps, normpos);
				width = Equations.calcBestWidth(hwidth, swidth);

				// totm + avg
				totm = 0;
				totmup = 0;
				totmlp = 0;
				sumavg = 0;
				for (int i = 0; i < nsteps; i++)
				{
					cu = compconu.getNorm(i, offsetu, inverseu);
					cl = compconl.getNorm(i, offsetl, inversel);
					//if (inParams->runMode == RunMode::Co && i >= inParams->column2) {
					// correct cu, cl ?
					//}
					con = cu + cl;
					sumavg += (i * con);
					totm += con;
					totmup += cu;
					totmlp += cl;
				}
				normavg = sumavg / totm;

				// purity
				contarget = 0;
				conall = 0;
				for (int i = 0; i < nsteps; i++)
				{
					cu = compconu.getNorm(i, offsetu, inverseu);
					cl = compconl.getNorm(i, offsetl, inversel);
					con = cu + cl;
					if (con > mincon)
					{
						// target component present
						contarget += con;
						for (int c = 0; c < ncomps; c++)
						{
							cu = conu0[c].getNorm(i, offsetu, inverseu);
							cl = conl0[c].getNorm(i, offsetl, inversel);
							con = cu + cl;
							conall += con;
						}
					}
				}
				purity = contarget / conall;

				// recovery
				contarget = 0;
				for (int i = 0; i < nsteps; i++)
				{
					conother = 0;
					for (int c = 0; c < ncomps; c++)
					{
						if (c != compi)
						{
							cu = conu0[c].getNorm(i, offsetu, inverseu);
							cl = conl0[c].getNorm(i, offsetl, inversel);
							con = cu + cl;
							conother += con;
						}
					}
					if (conother < mincon)
					{
						cu = compconu.getNorm(i, offsetu, inverseu);
						cl = compconl.getNorm(i, offsetl, inversel);
						con = cu + cl;
						contarget += con;
					}
				}
				recovery = contarget;

				outComp = new OutComp(inParams.comps[compi]);
				outComp.height = maxcon;
				outComp.totm = totm;
				outComp.totmup = totmup;
				outComp.totmlp = totmlp;
				outComp.purity = purity;
				outComp.recovery = recovery;
				if (up)
				{
					outComp.phase = PhaseType.Upper;
					// convert norm back to model
					pos = compconu.convNormToModel((int)normpos, offsetu, inverseu);
					avg = compconu.convNormToModel((int)normavg, offsetu, inverseu);
					// convert normal position back into screen position (using out/draw params):
					outComp.drawPosition = compconu.convModelToNorm((int)pos, offsetu0, inversePhase[0]);
					// convert position to retention
					realpos = compconu.Count - pos;
					realavg = compconu.Count - avg;
				}
				else
				{
					outComp.phase = PhaseType.Lower;
					// convert norm back to model
					pos = compconl.convNormToModel((int)normpos, offsetl, inversel);
					avg = compconl.convNormToModel((int)normavg, offsetl, inversel);
					// convert normal position back into screen position (using out/draw params):
					outComp.drawPosition = compconl.convModelToNorm((int)pos, offsetl0, inversePhase[1]);
					// convert position to retention
					realpos = compconl.Count - pos;
					realavg = compconl.Count - avg;
				}

				outComp.outCol = (pos > inParams.column2);
				outComp.eluted = outComp.outCol;

				if (!outComp.eluted)
				{
					// if not eluted; redo position calc depending on point of insertion
					outComp.phase = PhaseType.None;
					//	if (inParams->runMode != RunMode::Lp) {
					pos = compconu.convNormToModel((int)normpos, offsetu, inverseu);
					avg = compconu.convNormToModel((int)normavg, offsetu, inverseu);
					//	} else {
					//		pos = compconl.convNormToModel((int)normpos,offsetl,inversel);
					//		avg = compconl.convNormToModel((int)normavg,offsetl,inversel);
					//	}
				}

				if (outComp.eluted)
				{
					outComp.retention = inParams.convertUnit(realpos, inParams.natUnits, viewparams.viewUnits, outComp.phase);
					outComp.average = inParams.convertUnit(realavg, inParams.natUnits, viewparams.viewUnits, outComp.phase);
					outComp.width = inParams.convertUnit(width, inParams.natUnits, viewparams.viewUnits, outComp.phase);
					outComp.hwidth = inParams.convertUnit(hwidth, inParams.natUnits, viewparams.viewUnits, outComp.phase);
					outComp.swidth = inParams.convertUnit(swidth, inParams.natUnits, viewparams.viewUnits, outComp.phase);
				}
				else
				{
					// if not outcol: overwrite with column value
					outComp.retention = inParams.convertColUnit(pos, inParams.natUnits, viewparams.viewUnits);
					outComp.average = inParams.convertColUnit(avg, inParams.natUnits, viewparams.viewUnits);
					outComp.width = inParams.convertColUnit(width, inParams.natUnits, viewparams.viewUnits);
					outComp.hwidth = inParams.convertColUnit(hwidth, inParams.natUnits, viewparams.viewUnits);
					outComp.swidth = inParams.convertColUnit(swidth, inParams.natUnits, viewparams.viewUnits);
					outComp.phase = PhaseType.None;
				}
				outComp.sigma = outComp.width / 4;

				outComps.Add(outComp);
			}
			return outComps;
		}

		private float calcHeightWidth(TransCon compconu, TransCon compconl, int offsetu, int offsetl, bool inverseu, bool inversel, float nsteps, float peakpos, float peakheight)
		{
			// outComp width : determine at set height
			float width;
			float wheight = 0.6065f * peakheight;
			float pos1 = peakpos;
			float pos2 = peakpos;
			float cu, cl;
			float con;
			float lastcon = 0;

			for (int i = (int)peakpos; i > 0; i--)
			{
				cu = compconu.getNormCon(i, offsetu, inverseu);
				cl = compconl.getNormCon(i, offsetl, inversel);
				con = cu + cl;
				if (con < wheight && i != peakpos)
				{
					// interpolate
					pos1 = Util.calcCorX(i + 1, i, lastcon, con, wheight);
					break;
				}
				lastcon = con;
			}
			for (int i = (int)peakpos; i < nsteps; i++)
			{
				cu = compconu.getNormCon(i, offsetu, inverseu);
				cl = compconl.getNormCon(i, offsetl, inversel);
				con = cu + cl;
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

		private float calcSlopeWidth(TransCon compconu, TransCon compconl, int offsetu, int offsetl, bool inverseu, bool inversel, float nsteps, float peakpos)
		{
			// outComp width : look for slopes closest to max outComp value
			float width;
			float pos1 = peakpos;
			float pos2 = peakpos;
			float maxpos = peakpos;
			float maxpos0 = peakpos;
			float maxcon = 0;
			float maxcon0 = 0;
			float cu, cl;
			float con;
			float con0 = 0;
			float dcon, maxdcon;

			maxdcon = 0;
			for (int i = (int)peakpos; i > 0; i--)
			{
				cu = compconu.getNormCon(i, offsetu, inverseu);
				cl = compconl.getNormCon(i, offsetl, inversel);
				con = cu + cl;
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
			for (int i = (int)peakpos; i < nsteps; i++)
			{
				cu = compconu.getNormCon(i, offsetu, inverseu);
				cl = compconl.getNormCon(i, offsetl, inversel);
				con = cu + cl;
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

	}
}
