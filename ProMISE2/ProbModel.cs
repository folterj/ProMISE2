using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace ProMISE2
{
	public class ProbModel : Model
	{
		List<List<Unit>> units = new List<List<Unit>>(); // units[comp][n]
		List<List<List<Unit>>> timeUnits = new List<List<List<Unit>>>(); // timeUnits[time][comp][n]
		int[] compTotUnits; // total # injected units [comp]
		int probnunits;
		int densitysteps;
		float densitystepsize;
		float mixdt;
		float movedu, movedl;
		List<float> timemovedu = new List<float>();
		List<float> timemovedl = new List<float>();
		Random rnd = new Random();

		public ProbModel(ProModelInterface model, InParamsExt inParams, OutParams previewParams, OptionParams optionParams)
			: base(model, inParams, previewParams, optionParams)
		{
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
			inject(0);
			storeTime();
			dist();
			storeTime();
			it++;
			storeit++;
			while (!isDone() && running)
			{
				move();
				inject(it);
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
			outParams.outSet = storeOutVar(units, movedu, movedl, viewParams, false, 0);
		}

		public override void storeTimeOut(ViewParams viewParams)
		{
			int ntime;

			model.updateProgress(0);
			ntime = timeUnits.Count;
			outParams.timeOutSet = new List<OutSet>(ntime);
			for (int timei = 0; timei < ntime; timei++)
			{
				outParams.timeOutSet.Add(storeOutVar(timeUnits[timei], timemovedu[timei], timemovedl[timei], viewParams, true, timeTime[timei]));
				model.updateProgress((float)timei / ntime);
			}
			model.clearProgress();
		}

		public OutSet storeOutVar(List<List<Unit>> units0, float movedu0, float movedl0, ViewParams viewparams, bool timeMode, float time)
		{
			OutSet outSet = new OutSet(inParams);
			List<Unit> compunits;
			List<OutCell> comprawoutcells;
			List<List<OutCell>> rawoutcells;
			List<List<OutCell>> outcells;
			List<OutCell> intcells;
			Axes axes;
			float[] density;
			int[] densityzone;
			float[] filterWeight;
			Unit unit;
			int nphases;
			int ncomps2;
			int nseries;
			int serie;
			int nunits;
			int ncomps = inParams.comps.Count;
			int compi;
			float densitystepsizeu = densitystepsize * inParams.uf;
			float densitystepsizel = densitystepsize * inParams.lf;
			int densitysize;
			int densitypos;
			int filtersize;
			bool dispDualTime = (viewparams.phaseDisplay == PhaseDisplayType.UpperLowerTime);
			bool dispDual = (viewparams.phaseDisplay == PhaseDisplayType.UpperLower || dispDualTime);
			bool dispAll = (viewparams.phaseDisplay == PhaseDisplayType.All);
			bool dispUP = (viewparams.phaseDisplay == PhaseDisplayType.Upper);
			bool dispLP = (viewparams.phaseDisplay == PhaseDisplayType.Lower);
			bool runDual = (inParams.runMode == RunModeType.DualMode || inParams.runMode == RunModeType.Intermittent);
			bool runCo = (inParams.runMode == RunModeType.CoCurrent);
			bool runUP = (inParams.runMode == RunModeType.UpperPhase);
			bool runLP = (inParams.runMode == RunModeType.LowerPhase);
			bool showCol = ((runDual && (viewparams.phaseDisplay == PhaseDisplayType.UpperLower || dispAll)) || timeMode);
			bool compInUP = (!runLP || inParams.eeMode == EEModeType.BEE);
			bool compInLP = ((runLP || runDual) || inParams.eeMode == EEModeType.BEE);
			bool[] inversePhase = new bool[4];
			float minposu, maxposu, minposl, maxposl, minpos, maxpos;
			float posrange, posrange0;
			float minposu0, maxposu0, minposl0, maxposl0, minpos0, maxpos0;
			float pos, totpos; //,totw;
			float con, totcon;
			int totn;
			bool conset;
			int zone, currentzone;
			float con0, conall, conall0;
			float max;
			int start = 0;
			int i, j, h;
			Color color;
			float a, r, g, b;

			outSet.time = time;

			// Model -> Real (linear scale)
			// inverse lower phase
			inversePhase[1] = !(runLP || (runDual && !dispUP));
			//inversePhase[1] = !(compInLP && !dispUP);
			// inverse upper phase
			inversePhase[0] = (inversePhase[1] || (runDual && dispDualTime && !timeMode));
			// (almost always: inverse lower phase = inverse upper phase)

			// Model -> Real (time mode)
			// inverse lower phase
			inversePhase[3] = runCo;
			// inverse upper phase
			inversePhase[2] = true;
			// (almost always: lower: false, upper: true)

			if (!showCol && !(runDual && dispDualTime))
			{
				// show column if non-outcol peaks inside
				for (i = 0; i < ncomps; i++)
				{
					if (!compEluted[i])
					{
						showCol = true;
					}
				}
			}

			minposu = Math.Min(movedu, 0.0f);
			maxposu = Math.Max(movedu + inParams.vc2, inParams.vc2);
			minposl = Math.Min(movedl, 0.0f);
			maxposl = Math.Max(movedl + inParams.vc2, inParams.vc2);
			if (dispUP && !runLP)
			{
				minpos = minposu;
				maxpos = maxposu;
			}
			else if (dispLP && !runUP)
			{
				minpos = minposl;
				maxpos = maxposl;
			}
			else
			{
				minpos = Math.Min(minposl, minposu);
				maxpos = Math.Max(maxposl, maxposu);
			}
			posrange = maxpos - minpos;

			minposu0 = Math.Min(movedu0, 0.0f);
			maxposu0 = Math.Max(movedu0 + inParams.vc2, inParams.vc2);
			minposl0 = Math.Min(movedl0, 0.0f);
			maxposl0 = Math.Max(movedl0 + inParams.vc2, inParams.vc2);
			if (dispUP && !runLP)
			{
				minpos0 = minposu0;
				maxpos0 = maxposu0;
			}
			else if (dispLP && !runUP)
			{
				minpos0 = minposl0;
				maxpos0 = maxposl0;
			}
			else
			{
				minpos0 = Math.Min(minposl0, minposu0);
				maxpos0 = Math.Max(maxposl0, maxposu0);
			}
			posrange0 = maxpos0 - minpos0;

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

			densitysize = (int)(densitysteps / inParams.vc2 * posrange + 1);

			// store axes
			axes = outSet.axes;
			axes.showCol = showCol;
			if (showCol)
			{
				axes.colstart = convModelToReal(0, minpos, posrange, inversePhase[0]);
				axes.colend = convModelToReal(inParams.vc2, minpos, posrange, inversePhase[0]);

				if (inParams.vdeadInEnabled)
				{
					axes.showDeadvolstart = true;
					if (inParams.getNormalColDirection())
					{
						axes.deadvolstart = convModelToReal(inParams.getVdeadIn(), minpos, posrange, inversePhase[0]);
					}
					else
					{
						axes.deadvolstart = convModelToReal(inParams.vc2 - inParams.getVdeadIn(), minpos, posrange, inversePhase[0]);
					}
				}
				else
				{
					axes.showDeadvolstart = false;
				}
				if (inParams.vdeadOutEnabled)
				{
					axes.showDeadvolend = true;
					if (inParams.getNormalColDirection())
					{
						axes.deadvolend = convModelToReal(inParams.vc2 - inParams.getVdeadOut(), minpos, posrange, inversePhase[0]);
					}
					else
					{
						axes.deadvolend = convModelToReal(inParams.getVdeadOut(), minpos, posrange, inversePhase[0]);
					}
				}
				else
				{
					axes.showDeadvolend = false;
				}
				if (inParams.vdeadInjectEnabled)
				{
					axes.showDeadvolinsert = true;
					axes.deadvolinjectstart = convModelToReal(inParams.getVdeadInjectStart(), minpos, posrange, inversePhase[0]);
					axes.deadvolinjectend = convModelToReal(inParams.getVdeadInjectEnd(), minpos, posrange, inversePhase[0]);
				}
				else
				{
					axes.showDeadvolinsert = false;
				}
			}

			if (runDual && dispDualTime && !timeMode)
			{
				axes.rangex = Math.Max(maxpos, Math.Abs(minpos) + inParams.vc2);
			}
			else
			{
				axes.rangex = posrange;
			}

			if (!showCol)
			{
				axes.rangex -= inParams.vc2;
			}

			if (axes.rangex == 0)
			{
				axes.rangex = 1; // prevent div by zero
			}

			// dont show scale in column:
			if (inversePhase[2])
			{
				minposu += inParams.vc2;
			}
			else
			{
				maxposu -= inParams.vc2; // never happens?
			}
			if (inversePhase[3])
			{
				minposl += inParams.vc2;
			}
			else
			{
				maxposl -= inParams.vc2;
			}

			axes.scaleminu = convModelToReal(minposu, minpos, posrange, inversePhase[0]);
			axes.scalemaxu = convModelToReal(maxposu, minpos, posrange, inversePhase[0]);
			axes.scaleminl = convModelToReal(minposl, minpos, posrange, inversePhase[1]);
			axes.scalemaxl = convModelToReal(maxposl, minpos, posrange, inversePhase[1]);
			axes.scaleminulabel = convModelToReal(minposu, minpos, posrange, inversePhase[2]);
			axes.scalemaxulabel = convModelToReal(maxposu, minpos, posrange, inversePhase[2]);
			axes.scaleminllabel = convModelToReal(minposl, minpos, posrange, inversePhase[3]);
			axes.scalemaxllabel = convModelToReal(maxposl, minpos, posrange, inversePhase[3]);

			axes.logScale = (viewparams.yScale == YScaleType.Logarithmic);
			axes.update();

			// store peaks
			outSet.comps = storePeaks(units0, minpos, posrange, minpos0, posrange0, inversePhase, viewparams, showCol);

			// store raw (create)
			rawoutcells = new List<List<OutCell>>(nseries);
			// store raw
			for (compi = 0; compi < units0.Count; compi++)
			{
				compunits = units0[compi];
				for (int phasei = 0; phasei < nphases; phasei++)
				{
					serie = compi * nphases + phasei;
					comprawoutcells = new List<OutCell>(compunits.Count);
					for (int celli = 0; celli < compunits.Count; celli++)
					{
						unit = compunits[celli];
						if ((nphases > 1 && unit.phase == (PhaseType)(phasei + 1)) || (dispUP && unit.phase == PhaseType.Upper) || (dispLP && unit.phase == PhaseType.Lower) || dispAll)
						{
							pos = convModelToReal(unit.pos, minpos, posrange, inversePhase[(int)unit.phase - 1]);
							con = unit.m;
							comprawoutcells.Add(new OutCell(pos, con));
						}
					}
					rawoutcells.Add(comprawoutcells);
				}
			}

			// store (create)
			outcells = new List<List<OutCell>>(nseries);
			for (i = 0; i < nseries; i++)
			{
				outcells.Add(new List<OutCell>(densitysize));
			}

			axes.maxcon = new List<float>(ncomps2);
			for (i = 0; i < ncomps2; i++)
			{
				axes.maxcon.Add(0);
			}

			// accumulate / store
			density = new float[densitysize];
			densityzone = new int[densitysize];
			for (compi = 0; compi < units0.Count; compi++)
			{
				compunits = units0[compi];
				filterWeight = Util.createFilter(outSet.comps[compi].filterSigma);
				filtersize = filterWeight.Length;
				max = 0;
				for (int phasei = 0; phasei < nphases; phasei++)
				{
					serie = compi * nphases + phasei;
					Array.Clear(density, 0, densitysize);
					Array.Clear(densityzone, 0, densitysize);
					for (int celli = 0; celli < compunits.Count; celli++)
					{
						unit = compunits[celli];
						if ((nphases > 1 && unit.phase == (PhaseType)(phasei + 1)) || (dispUP && unit.phase == PhaseType.Upper) || (dispLP && unit.phase == PhaseType.Lower) || dispAll)
						{
							// accumulate
							densitypos = convModelToDensity(unit.pos, minpos, posrange, densitysize, inversePhase[(int)unit.phase - 1]);
							// convert from mass to concentration (densitystepsize = volume of each bin)
							if (unit.incol)
							{
								// correct concentration inside column according to volumes
								if (unit.phase == PhaseType.Upper)
								{
									density[densitypos] += unit.m / densitystepsizeu;
								}
								else if (unit.phase == PhaseType.Lower)
								{
									density[densitypos] += unit.m / densitystepsizel;
								}
							}
							else
							{
								density[densitypos] += unit.m / densitystepsize;
							}
							densityzone[densitypos] = unit.zone + 1;
						}
					}
					// fill in unset density zones
					zone = 0;
					i = 0;
					while (i < densitysize)
					{
						if (densityzone[i] != 0)
						{
							if (densityzone[i] == zone)
							{
								// same zone
								for (j = start + 1; j < i; j++)
								{
									// fill in
									densityzone[j] = zone;
								}
							}
							else
							{
								// different zone
								zone = densityzone[i];
							}
							start = i;
						}
						i++;
					}
					// filter
					for (i = 0; i < densitysize; i++)
					{
						// i: dest. position
						con = 0;
						//totw = 0;
						conset = false;
						for (j = 0; j < filtersize; j++)
						{
							h = i + j - filtersize / 2;
							// h: sample position
							if (h >= 0 && h < densitysize)
							{
								if (densityzone[i] == densityzone[h])
								{
									// only add if sample position is in same zone as dest. position
									con += filterWeight[j] * density[h];
									//totw+= filterWeight[j];
									conset = true;
								}
							}
						}
						if (conset)
						{
							// to smoothen zone cuts: don't normalise
							// (normalise: divide by total)
							//con/= totw;
							if (con > axes.maxcon[compi])
							{
								axes.maxcon[compi] = con;
								max = convDensityToReal(i, posrange, densitysize);
							}
							if (dispDual && (PhaseType)(phasei + 1) == PhaseType.Lower)
							{
								con = -con;
							}
						}
						else
						{
							con = 0;
						}
						pos = convDensityToReal(i, posrange, densitysize);
						outcells[serie].Add(new OutCell(pos, con));
					}

					if (inParams.runMode == RunModeType.Intermittent && viewparams.peaksDisplay == PeaksDisplayType.IntTotals)
					{
						// intermittent mode: only show zone totals
						intcells = new List<OutCell>();

						currentzone = 0;
						totpos = 0;
						totcon = 0;
						totn = 0;
						for (i = 0; i < densitysize; i++)
						{
							zone = densityzone[i];
							if (zone != currentzone || i == 0)
							{
								if (totn != 0 && currentzone != 0)
								{
									if (intcells.Count == 0)
									{
										intcells.Add(new OutCell(totpos / totn, 0));
									}
									intcells.Add(new OutCell(totpos / totn, totcon));
									if (Math.Abs(totcon) > axes.maxcon[compi])
									{
										axes.maxcon[compi] = Math.Abs(totcon);
									}
								}
								currentzone = zone;
								totpos = 0;
								totcon = 0;
								totn = 0;
							}
							totpos += outcells[serie][i].pos;
							totcon += outcells[serie][i].con;
							totn++;
						}
						if (intcells.Count != 0)
						{
							totpos = intcells[intcells.Count - 1].pos;
						}
						else
						{
							totpos = 0;
						}
						intcells.Add(new OutCell(totpos, 0));
						// overwrite outcells
						outcells[serie].Clear();
						for (i = 0; i < intcells.Count; i++)
						{
							outcells[serie].Add(intcells[i]);
						}
					}
				}
				outSet.comps[compi].retention = inParams.convertUnit(max, inParams.natUnits, viewparams.viewUnits, outSet.comps[compi].phase);
			}

			if (inParams.runMode != RunModeType.Intermittent || viewparams.peaksDisplay != PeaksDisplayType.IntTotals)
			{
				// if not int totals mode
				color = new Color();
				if (viewparams.peaksDisplay == PeaksDisplayType.PeaksSum || viewparams.peaksDisplay == PeaksDisplayType.Sum)
				{
					// add series for sum
					for (i = 0; i < densitysize; i++)
					{
						pos = convDensityToReal(i, posrange, densitysize);
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

							compi = ncomps;
							serie = ncomps * nphases + phasei;
							
							if (con != 0)
							{
								a = 1;
								r /= Math.Abs(con);
								g /= Math.Abs(con);
								b /= Math.Abs(con);
								color = Color.FromScRgb(a, r, g, b);
							}
							else
							{
								if (i > 0)
								{
									color = outcells[serie][i - 1].color;
								}
								else
								{
									color = Colors.Transparent;
								}
							}

							if (Math.Abs(con) > axes.maxcon[compi])
							{
								axes.maxcon[compi] = Math.Abs(con);
							}
							outcells[serie].Add(new OutCell(pos, con, color));
						}
					}
				}

				// peaks (additional)
				for (int c = 0; c < ncomps; c++)
				{
					nunits = 0;
					for (int phasei = 0; phasei < nphases; phasei++)
					{
						serie = c * nphases + phasei;
						nunits = Math.Max(nunits, outcells[serie].Count);
					}

					// peak height
					outSet.comps[c].height = axes.maxcon[c];

					// purity
					con = 0;
					conall = 0;
					for (i = 0; i < nunits; i++)
					{
						con0 = 0;
						conall0 = 0;
						for (compi = 0; compi < ncomps; compi++)
						{
							for (int phasei = 0; phasei < nphases; phasei++)
							{
								serie = compi * nphases + phasei;
								conall0 += Math.Abs(outcells[serie][i].con);
								if (compi == c)
									con0 += Math.Abs(outcells[serie][i].con);
							}
						}
						if (con0 > mincon)
						{
							// min component present -> include current position
							con += con0;
							conall += conall0;
						}
					}
					outSet.comps[c].purity = con / conall;

					// recovery
					con = 0;
					for (i = 0; i < nunits; i++)
					{
						con0 = 0;
						conall0 = 0;
						for (compi = 0; compi < ncomps; compi++)
						{
							for (int phasei = 0; phasei < nphases; phasei++)
							{
								serie = compi * nphases + phasei;
								if (compi == c)
									con0 += Math.Abs(outcells[serie][i].con);
								else
									conall0 += Math.Abs(outcells[serie][i].con);
							}
						}
						if (conall0 < mincon)
						{
							// min impurity present -> include current position
							con += con0;
						}
					}
					outSet.comps[c].recovery = con * densitystepsize;
				}
			}

			// convert List to array
			outSet.rawOutCells = new OutCell[rawoutcells.Count][];
			for (i = 0; i < rawoutcells.Count; i++)
			{
				outSet.rawOutCells[i] = new OutCell[rawoutcells[i].Count];
				for (j = 0; j < rawoutcells[i].Count; j++)
				{
					outSet.rawOutCells[i][j] = rawoutcells[i][j];
				}
			}

			outSet.outCells = new OutCell[outcells.Count][];
			for (i = 0; i < outcells.Count; i++)
			{
				outSet.outCells[i] = new OutCell[outcells[i].Count];
				for (j = 0; j < outcells[i].Count; j++)
				{
					outSet.outCells[i][j] = outcells[i][j];
				}
			}

			return outSet;
		}

		private float init()
		{
			int ncomps = inParams.comps.Count;

			probnunits = inParams.probUnits;
			densitysteps = inParams.densitySteps;
			densitystepsize = inParams.vc2 / densitysteps;
			mixdt = 1.0f / inParams.mixSpeed;
			mincon = 1.0f / probnunits / densitystepsize;

			generalInit();

			movedu = 0;
			movedl = 0;
			units.Clear();
			timeUnits.Clear();
			timemovedu.Clear();
			timemovedl.Clear();
			intSwitchu.Clear();
			intSwitchl.Clear();
			timeTime.Clear();
			compTotUnits = new int[ncomps];
			for (int compi = 0; compi < ncomps; compi++)
			{
				compTotUnits[compi] = probnunits;
			}
			if (inParams.runMode == RunModeType.Intermittent)
			{
				newIntAmount();
			}
			return previewParams.estmaxtime / mixdt;
		}

		private void inject(int it)
		{
			List<Unit> compunits;
			Comp comp;
			float im;
			float feedtime;
			float stepdt;
			float steps;
			int n = 0;
			bool first = (it == 0);
			float inspos = inParams.getInjectPosNorm();

			if (inParams.injectMode == InjectModeType.Instant)
			{
				if (!first)
				{
					return;
				}
				n = probnunits;
			}
			else if (inParams.injectMode == InjectModeType.Batch)
			{
				feedtime = inParams.convertUnit(inParams.injectFeed, inParams.injectFeedUnits, QuantityType.Time, inParams.injectPhase);
				stepdt = 1.0f / inParams.mixSpeed;
				steps = feedtime / stepdt;
				if (probnunits / steps >= 1)
				{
					n = (int)Math.Round(probnunits / steps);
				}
				else
				{
					// n < 1
					n = (int)Math.Round(steps / probnunits);
					if (it % n == 0)
					{
						n = 1;
					}
					else
					{
						n = 0;
					}
				}
			}

			if (n > 0)
			{
				for (int compi = 0; compi < inParams.comps.Count; compi++)
				{
					comp = inParams.comps[compi];
					if (inParams.injectMode == InjectModeType.Continuous)
					{
						n = (int)(comp.concentration / comp.m * probnunits); // remove mass
					}
					else
					{
						if (n > compTotUnits[compi])
						{
							n = compTotUnits[compi];
						}
					}
					if (compTotUnits[compi] > 0 || inParams.injectMode == InjectModeType.Continuous)
					{
						im = comp.m / probnunits;
						if (first)
						{
							compunits = new List<Unit>();
						}
						else
						{
							compunits = units[compi];
						}
						for (int i = 0; i < n; i++)
						{
							compunits.Add(new Unit(im, inspos, inParams.injectPhase));
						}
						if (first)
						{
							units.Add(compunits);
						}
						compTotUnits[compi] -= n;
					}
				}
				// make sure to have 'perfect' distribution if sample is inserted in both phases
				if (inParams.injectPhase == PhaseType.Both)
				{
					distPerfect();
				}
			}
		}

		private void dist()
		{
			Unit unit;
			float k;
			float x;
			float fup, flp;
			float f = 0;
			float eff = inParams.efficiency;
			float deadvolstart = inParams.getVdeadStart();
			float deadvolend = inParams.getVdeadEnd();
			float deadvolinsertstart = inParams.getVdeadInjectStart();
			float deadvolinsertend = inParams.getVdeadInjectEnd();

			x = inParams.px;
			for (int i = 0; i < inParams.comps.Count; i++)
			{
				k = inParams.comps[i].k;
				// in upper phase; chance to move to lower phase:
				fup = Equations.calcTransferL(inParams.kDefinition, k * x);
				// in lower phase; chance to move to upper phase:
				flp = Equations.calcTransferU(inParams.kDefinition, k * x);
				for (int j = 0; j < units[i].Count; j++)
				{
					unit = units[i][j];
					if (unit.incol)
					{
						if (unit.pos >= deadvolstart && unit.pos <= deadvolend && (unit.pos <= deadvolinsertstart || unit.pos >= deadvolinsertend))
						{
							// active units
							if (rnd.NextDouble() <= eff)
							{
								// chance to move to other phase
								if (unit.phase == PhaseType.Upper)
								{
									f = fup;
								}
								else if (unit.phase == PhaseType.Lower)
								{
									f = flp;
								}
								if (rnd.NextDouble() < f)
								{
									// swap phase
									if (unit.phase == PhaseType.Upper)
									{
										unit.phase = PhaseType.Lower;
									}
									else if (unit.phase == PhaseType.Lower)
									{
										unit.phase = PhaseType.Upper;
									}
								}
							}
						}
						else
						{
							// inactive units
							if (curPhase == PhaseType.Upper && unit.phase == PhaseType.Lower)
							{
								// move all units in lower to upper
								unit.phase = PhaseType.Upper;
							}
							else if (curPhase == PhaseType.Lower && unit.phase == PhaseType.Upper)
							{
								// move all units in upper to lower
								unit.phase = PhaseType.Lower;
							}
						}
					}
				}
			}
		}

		private void distPerfect()
		{
			Unit unit;
			float k;
			float x;
			float fup, flp;

			x = inParams.px;
			for (int i = 0; i < inParams.comps.Count; i++)
			{
				k = inParams.comps[i].k;
				// in upper phase; chance to move to lower phase:
				fup = Equations.calcTransferL(inParams.kDefinition, k * x);
				// in lower phase; chance to move to upper phase:
				flp = Equations.calcTransferU(inParams.kDefinition, k * x);
				for (int j = 0; j < units[i].Count; j++)
				{
					unit = units[i][j];
					if (unit.incol && unit.phase == PhaseType.Both)
					{
						if (rnd.NextDouble() < fup)
						{
							unit.phase = PhaseType.Upper;
						}
						else
						{
							unit.phase = PhaseType.Lower;
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

		private void move()
		{
			Unit unit;
			float dvu, dvl, dvun, dvln;
			bool intPhaseSwitch = false;
			float movedu0, movedl0;

			// outside column / general
			// delta time -> delta vol between each 'mix step'
			dvu = inParams.fu * mixdt;
			dvl = inParams.fl * mixdt;
			if (inParams.runMode == RunModeType.CoCurrent)
			{
				dvu = (inParams.fu + inParams.fl) * mixdt;
				dvl = dvu;
			}

			// inside column
			// scale to mobile phase: delta time -> delta vol -> linear velocity
			dvun = inParams.fnormu * mixdt;
			dvln = inParams.fnorml * mixdt;

			if (inParams.runMode == RunModeType.Intermittent)
			{
				if (curPhase == PhaseType.Upper)
				{
					if (inParams.viewUnits != QuantityType.Time)
					{
						dvl = 0;
						dvln = 0;
					}
					if (inParams.intMode != IntModeType.Component)
					{
						intamountu -= dvu;
						if (intamountu <= 0)
						{
							intPhaseSwitch = true;
						}
					}
				}
				else if (curPhase == PhaseType.Lower)
				{
					if (inParams.viewUnits != QuantityType.Time)
					{
						dvu = 0;
						dvun = 0;
					}
					if (inParams.intMode != IntModeType.Component)
					{
						intamountl -= dvl;
						if (intamountl <= 0)
						{
							intPhaseSwitch = true;
						}
					}
				}
			}

			if (inParams.runMode != RunModeType.CoCurrent)
			{
				// make lower phase flow negative
				dvl = -dvl;
				dvln = -dvln;
			}

			for (int i = 0; i < inParams.comps.Count; i++)
			{
				for (int j = 0; j < units[i].Count; j++)
				{
					unit = units[i][j];
					if (unit.incol)
					{
						// in col
						if (unit.phase == curPhase || inParams.runMode != RunModeType.Intermittent || inParams.viewUnits != QuantityType.Time)
						{
							if (unit.phase == PhaseType.Upper)
							{
								unit.pos += dvun;
							}
							else if (unit.phase == PhaseType.Lower)
							{
								unit.pos += dvln;
							}
						}
						if (unit.pos < 0 || unit.pos > inParams.vc2)
						{
							// moved out col now
							unit.incol = false;
							if (inParams.runMode == RunModeType.Intermittent)
							{
								if (inParams.intMode == IntModeType.Component)
								{
									if ((curPhase == PhaseType.Upper && i != inParams.intUpComp) || (curPhase == PhaseType.Lower && i != inParams.intLpComp))
									{
										intPhaseSwitch = true;
									}
								}
								unit.zone = intit + 1;
							}
							else
							{
								unit.zone = 1;
							}
						}
					}
					else
					{
						// out col
						if (unit.phase == PhaseType.Upper)
						{
							unit.pos += dvu;
						}
						else if (unit.phase == PhaseType.Lower)
						{
							unit.pos += dvl;
						}
					}
				}
			}

			movedu += dvu;
			movedl += dvl;

			if (intPhaseSwitch && intit / 2 < inParams.intMaxIt)
			{
				movedu0 = 0;
				for (int i = 0; i < intSwitchu.Count; i++)
				{
					movedu0 += intSwitchu[i];
				}
				intSwitchu.Add(movedu - movedu0);
				movedl0 = 0;
				for (int i = 0; i < intSwitchl.Count; i++)
				{
					movedl0 += intSwitchl[i];
				}
				intSwitchl.Add(movedl - movedl0);
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
			Unit unit;
			bool posdir;

			switch (inParams.runMode)
			{
				case RunModeType.Intermittent:
					posdir = (inParams.intStartPhase == PhaseType.Upper);
					break;
				case RunModeType.LowerPhase:
					posdir = false;
					break;
				default:
					posdir = true;
					break;
			}
			if (inParams.eeMode == EEModeType.BEE)
			{
				posdir = !posdir;
			}
			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				for (int i = 0; i < units[compi].Count; i++)
				{
					unit = units[compi][i];
					if (unit.incol)
					{
						if (posdir)
						{
							unit.pos += inParams.vc2;
						}
						else
						{
							unit.pos -= inParams.vc2;
						}
					}
					else
					{
						if (unit.phase == PhaseType.Upper && posdir)
						{
							unit.pos += inParams.vc2;
						}
						else if (unit.phase == PhaseType.Lower && !posdir)
						{
							unit.pos -= inParams.vc2;
						}
					}
				}
				compEluted[compi] = true;
			}
			if (posdir)
			{
				movedu += inParams.vc2;
			}
			else
			{
				movedl -= inParams.vc2;
			}
			eeDone = true;
		}

		private bool isDone()
		{
			bool done = false;
			if (inParams.doMaxIt && (Math.Abs(movedu) >= inParams.maxIt * inParams.vc || Math.Abs(movedl) >= inParams.maxIt * inParams.vc))
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
				if (compTotUnits[compi] > 0.01 * probnunits)
				{
					done = false;
				}
			}
			return done;
		}

		private bool isAllOut()
		{
			bool allout = true;
			Comp comp;
			float mincol;
			float moutcol;

			for (int compi = 0; compi < inParams.comps.Count; compi++)
			{
				comp = inParams.comps[compi];
				if (previewParams.outSet.comps[compi].willElute && !compEluted[compi])
				{
					mincol = 0;
					moutcol = 0;
					for (int i = 0; i < units[compi].Count; i++)
					{
						if (units[compi][i].incol)
							mincol += units[compi][i].m;
						else
							moutcol += units[compi][i].m;
					}
					// 0.001 means less than 1 particles (out of 1000)
					if (mincol / (mincol + moutcol) < 2 * mincon)
					{
						compEluted[compi] = true;
					}
					else
					{
						if (comp.elute)
						{
							allout = false;
							break;
						}
					}
				}
			}
			return allout;
		}

		private void storeTime()
		{
			List<List<Unit>> units0;
			List<Unit> compunits;
			int nunits;
			int ncomps = inParams.comps.Count;
			// Copy units to timeUnits

			if (optionParams.timeStores == 0)
			{
				return;
			}

			units0 = new List<List<Unit>>(ncomps);
			for (int i = 0; i < ncomps; i++)
			{
				//sort(units.at(i).begin(),units.at(i).end());
				nunits = units[i].Count;
				compunits = new List<Unit>(nunits);
				for (int j = 0; j < nunits; j++)
				{
					compunits.Add(new Unit(units[i][j]));
				}
				units0.Add(compunits);
			}
			timeUnits.Add(units0);

			timemovedu.Add(movedu);
			timemovedl.Add(movedl);

			timeTime.Add(it * mixdt);
		}

		private void trimTimeStores()
		{
			int ntimeunits = timeUnits.Count;

			for (int i = ntimeunits - 1; i > 0; i -= 2)
			{
				timeUnits.RemoveAt(i);
				timemovedu.RemoveAt(i);
				timemovedl.RemoveAt(i);
				timeTime.RemoveAt(i);
			}
		}

		private List<OutComp> storePeaks(List<List<Unit>> units0, float minpos, float posrange, float minpos0, float posrange0, bool[] inversePhase, ViewParams viewparams, bool showCol)
		{
			List<OutComp> outComps = new List<OutComp>();
			OutComp outComp;
			Unit unit;
			float sum, sumu, suml;
			float avg, avgu, avgl;
			float intavg = 0;
			float sigma, maxsigma;
			float sum0;
			float totm0;
			float avg0;
			float intsigma = 0;
			float totm, totmup, totmlp, totmup0, totmlp0, totmcol;
			float inttotm;
			int intzone = 0;
			PhaseType phase = new PhaseType();
			PhaseType intphase = new PhaseType();
			bool outcol;
			int ncomps = inParams.comps.Count;

			for (int compi = 0; compi < ncomps; compi++)
			{
				//sort(units0->at(compi).begin(),units0->at(compi).end());
				// average (mu)
				sum = 0;
				sumu = 0;
				suml = 0;
				totm = 0;
				totmup = 0;
				totmlp = 0;
				totmcol = 0;
				for (int i = 0; i < units0[compi].Count; i++)
				{
					unit = units0[compi][i];
					sum += unit.pos * unit.m;
					totm += unit.m;
					if (unit.phase == PhaseType.Upper)
					{
						sumu += unit.pos * unit.m;
						totmup += unit.m;
					}
					else if (unit.phase == PhaseType.Lower)
					{
						suml += unit.pos * unit.m;
						totmlp += unit.m;
					}
					if (unit.pos >= 0 && unit.pos <= inParams.vc2)
					{
						totmcol += unit.m;
					}
				}
				avg = sum / totm;
				avgu = sumu / totmup;
				avgl = suml / totmlp;
				outcol = (totm - totmcol > totmcol);
				if (inParams.runMode == RunModeType.Intermittent)
				{
					// int mode: determine simple average and sigma for each zone
					// store max sigma
					inttotm = 0;
					intzone = 0;
					intavg = 0;
					intsigma = 0;
					for (int zone = 0; zone < intit + 2; zone++)
					{
						sum0 = 0;
						totm0 = 0;
						totmup0 = 0;
						totmlp0 = 0;
						for (int i = 0; i < units0[compi].Count; i++)
						{
							unit = units0[compi][i];
							if (unit.zone == zone)
							{
								sum0 += unit.pos * unit.m;
								totm0 += unit.m;
								if (unit.phase == PhaseType.Upper)
								{
									totmup0 += unit.m;
								}
								else if (unit.phase == PhaseType.Lower)
								{
									totmlp0 += unit.m;
								}
							}
						}
						if (totm0 > 0.01)
						{
							avg0 = sum0 / totm0;
							sum0 = 0;
							if (totm0 > inttotm)
							{
								inttotm = totm0;
								intzone = zone;
								intavg = avg0;
								if (totmlp0 > totmup0)
								{
									intphase = PhaseType.Lower;
								}
								else
								{
									intphase = PhaseType.Upper;
								}
							}
							for (int i = 0; i < units0[compi].Count; i++)
							{
								unit = units0[compi][i];
								if (unit.zone == zone)
								{
									sum0 += (float)Math.Pow(unit.pos - avg0, 2) * unit.m;
								}
							}
							if (Math.Sqrt(sum0 / totm0) > intsigma)
							{
								intsigma = (float)Math.Sqrt(sum0 / totm0);
							}
						}
					}
				}

				outComp = new OutComp(inParams.comps[compi]);
				if (inParams.runMode == RunModeType.Intermittent && intzone > 0)
				{
					outComp.intIt = (float)(intzone - 1) / 2;
					outComp.intItSet = true;
				}
				// deviation (sigma)
				sum = 0;
				sumu = 0;
				suml = 0;
				for (int i = 0; i < units0[compi].Count; i++)
				{
					unit = units0[compi][i];
					sum += (float)Math.Pow(unit.pos - avg, 2) * unit.m;
					if (unit.phase == PhaseType.Upper)
					{
						sumu += (float)Math.Pow(unit.pos - avgu, 2) * unit.m;
					}
					else if (unit.phase == PhaseType.Lower)
					{
						suml += (float)Math.Pow(unit.pos - avgl, 2) * unit.m;
					}
				}
				if (totmlp > totmup)
				{
					phase = PhaseType.Lower;
				}
				else
				{
					phase = PhaseType.Upper;
				}
				outComp.outCol = outcol;
				outComp.eluted = outComp.outCol;
				if (inParams.runMode == RunModeType.CoCurrent || !outComp.eluted)
				{
					// co-current mode or peak not eluted
					sigma = (float)Math.Sqrt(sum / totm);
					outComp.phase = PhaseType.None;
				}
				else if (inParams.runMode == RunModeType.Intermittent)
				{
					// int mode
					avg = intavg;
					sigma = (float)Math.Sqrt(sum / totm);
					phase = intphase;
					outComp.phase = phase;
				}
				else
				{
					if (phase == PhaseType.Lower)
					{
						avg = avgl;
						sigma = (float)Math.Sqrt(suml / totmlp);
					}
					else
					{
						avg = avgu;
						sigma = (float)Math.Sqrt(sumu / totmup);
					}
					outComp.phase = phase;
				}
				outComp.totm = totm;
				outComp.totmup = totmup;
				outComp.totmlp = totmlp;
				if (outComp.eluted)
				{
					avg0 = convModelToReal(avg, minpos0, posrange0, inversePhase[(int)phase + 1]);
					outComp.average = inParams.convertUnit(avg0, inParams.natUnits, viewparams.viewUnits, phase);
				}
				else
				{
					avg0 = avg;
					outComp.average = inParams.convertColUnit(avg0, inParams.natUnits, viewparams.viewUnits);
				}
				outComp.drawPosition = convModelToReal(avg, minpos, posrange, inversePhase[(int)phase - 1]);
				if (outComp.eluted)
				{
					outComp.sigma = inParams.convertUnit(sigma, inParams.natUnits, viewparams.viewUnits, phase);
				}
				else
				{
					outComp.sigma = inParams.convertColUnit(sigma, inParams.natUnits, viewparams.viewUnits);
				}
				outComp.width = 4 * outComp.sigma;

				if (inParams.runMode == RunModeType.Intermittent)
				{
					sigma = intsigma;
				}
				maxsigma = 30;
				outComp.filterSigma = (float)Math.Ceiling(sigma / (densitystepsize * 2) / 1.2);
				if (outComp.filterSigma > maxsigma)
				{
					outComp.filterSigma = maxsigma;
				}

				outComps.Add(outComp);
			}
			return outComps;
		}

		private float convModelToReal(float pos, float minpos, float posrange, bool inverse)
		{
			if (inverse)
			{
				return (posrange + minpos) - pos;
			}
			return pos - minpos;
		}

		private float convRealToModel(float pos, float minpos, float posrange, bool inverse)
		{
			if (inverse)
			{
				return (posrange + minpos) - pos;
			}
			return pos + minpos;
		}

		private int convRealToDensity(float pos, float posrange, int densitysize)
		{
			int density;
			float normpos = pos / posrange;
			density = (int)(normpos * densitysize);
			if (density < 0)
			{
				density = 0;
			}
			if (density >= densitysize)
			{
				density = densitysize - 1;
			}
			return density;
		}

		private float convDensityToReal(int density, float posrange, int densitysize)
		{
			float pos;
			float normpos = (float)density / densitysize;
			pos = normpos * posrange;
			return pos;
		}

		private int convModelToDensity(float pos, float minpos, float posrange, int densitysize, bool inverse)
		{
			return convRealToDensity(convModelToReal(pos, minpos, posrange, inverse), posrange, densitysize);
		}

		private float convDensityToModel(int density, float minpos, float posrange, int densitysize, bool inverse)
		{
			return convRealToModel(convDensityToReal(density, posrange, densitysize), minpos, posrange, inverse);
		}

	}
}
