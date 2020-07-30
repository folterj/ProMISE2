using System;
using System.Collections.Generic;

namespace ProMISE2
{
	public class PreviewModel : Model
	{
		public PreviewModel(ProModelInterface model, InParamsExt inParams, OptionParams optionParams)
			: base(model, inParams, null, optionParams)
		{
		}

		public override void run()
		{
            generalInit();
		}

		public override void storeOut(ViewParams viewParams)
		{
			previewParams.outSet = storeOutVar(viewParams);
		}

		public OutSet storeOutVar(ViewParams viewParams)
		{
			// use predictive equations
			OutSet outSet = new OutSet(inParams);
			OutCell[][] outCells;
			Axes axes;
			Comp comp;
			OutComp outComp;
            OutComp outCompt = null;
			float pos, pos0, post;
			float totposu, totposl;
			float timepos;
			float maxtime0;
			float eepos = 0;
			float intAmount = 0;
			float sigma, sigma0;
			float timesigma = 0;
			float con, maxcon;
			float amp0;
			float div0;
			float maxposu = 0;
			float maxposl = 0;
			float mindrawpos = 0;
			float maxdrawpos = 0;
			float range;
			float stepsize;
			int ncomps = inParams.comps.Count;
			int nsize = 1000;
			float k;
			PhaseType phase = new PhaseType();
			KdefType kdef = inParams.kDefinition;
			RunModeType runMode = inParams.runMode;
			QuantityType natUnits = inParams.natUnits;
			float injectPos = 0;
			float lf = inParams.lf;
			float uf = inParams.uf;
			float px = inParams.px;
			float fu = 0;
			float fl = 0;
			float fnormu = 0;
			float fnorml = 0;
			float vc = 0;
			float mixspeed = 0;
			float efficiency = 0;
			bool allincol = true;
			bool timeMode = (inParams.viewUnits == QuantityType.Time);
			bool eeMode = (inParams.eeMode != EEModeType.None);
			bool intMode = (inParams.runMode == RunModeType.Intermittent);
			bool intCompMode = (inParams.intMode == IntModeType.Component);
			int intit;
			bool eluted;
			float elutable;

			eeDone = false;

			if (inParams.model == ModelType.CCD)
			{
				// [steps]
				vc = inParams.column;
				injectPos = inParams.getInjectPosNorm();
				if (runMode == RunModeType.CoCurrent)
				{
					injectPos = (inParams.column - 1) - injectPos;
				}
				fnormu = inParams.fnormu;
				fnorml = inParams.fnorml;
				// always use normalised flows
				fu = fnormu;
				fl = fnorml;
				mixspeed = 1;
				efficiency = Equations.calcEff1(inParams.efficiency);
			}
			else if (inParams.model == ModelType.Probabilistic)
			{
				// [volume]
				vc = inParams.vc;
				injectPos = inParams.getInjectPosNorm();
				fu = inParams.fu;
				fl = inParams.fl;
				// normalise flow
				fnormu = inParams.fnormu;
				fnorml = inParams.fnorml;
				if (fnorml > fnormu)
				{
					fnormu /= fnorml;
					fnorml = 1;
				}
				else
				{
					fnorml /= fnormu;
					fnormu = 1;
				}
				mixspeed = inParams.mixSpeed;
				efficiency = Equations.calcEff1(inParams.efficiency);
			}
			else if (inParams.model == ModelType.Transport)
			{
				// [time]
				natUnits = QuantityType.Time;
				vc = inParams.vc;
				injectPos = inParams.getInjectPosNorm();
				fu = inParams.fu;
				fl = inParams.fl;
				// normalise flow
				fnormu = fu / uf;
				fnorml = fl / lf;
				if (fnorml > fnormu)
				{
					fnormu /= fnorml;
					fnorml = 1;
				}
				else
				{
					fnorml /= fnormu;
					fnormu = 1;
				}
				mixspeed = 1;
				efficiency = (float)Math.Sqrt(inParams.ka * 10); // **** make correct for f? dx? dt?
			}

			if (runMode != RunModeType.CoCurrent)
			{
				fl = -fl;
				fnorml = -fnorml;
			}

			// Set maxpos
			for (int compi = 0; compi < ncomps; compi++)
			{
				comp = inParams.comps[compi];
				outComp = outSet.comps[compi];

				elutable = Equations.calcElutable(kdef, fu, fl, outComp.k);
				outComp.willElute = (elutable > 0.01);

				if (outComp.willElute)
				{
					outCompt = calcNewPos(comp, fu, fl, vc, injectPos, natUnits);	// outcomp is reassigned
					outComp = outSet.comps[compi];  // overwrite comp
					outComp.willElute = true;       // restore willElute
					if (comp.elute)
					{
						if (outCompt.retention < 0)
						{
							phase = PhaseType.Lower;
							if (Math.Abs(outCompt.retention) > maxposl)
							{
								maxposl = Math.Abs(outCompt.retention);
							}
						}
						else
						{
							phase = PhaseType.Upper;
							if (Math.Abs(outCompt.retention) > maxposu)
							{
								maxposu = Math.Abs(outCompt.retention);
							}
						}
						sigma = Equations.calcSigma(kdef, fnormu, fnorml, comp.k * px, outCompt.retentionTime, mixspeed, efficiency);
						if (runMode == RunModeType.DualMode && inParams.model != ModelType.Transport)
						{
							sigma *= 2;
						}
						sigma = inParams.convertUnit(sigma, QuantityType.Time, natUnits, phase);
					}
				}
				else
				{
					if (intMode && inParams.intFinalElute)
					{
						outComp.willElute = true;
					}
				}
			}
			if (intMode && !intCompMode)
			{
				intamountu = inParams.convertUnit(inParams.intUpSwitch, (QuantityType)inParams.intMode, natUnits, PhaseType.Upper);
				intamountl = inParams.convertUnit(inParams.intLpSwitch, (QuantityType)inParams.intMode, natUnits, PhaseType.Lower);
				if (inParams.model == ModelType.CCD)
				{
					intamountut = intamountu;
				}
				else
				{
					intamountut = inParams.convertUnit(inParams.intUpSwitch, (QuantityType)inParams.intMode, QuantityType.Time, PhaseType.Upper);
				}
				if (inParams.model == ModelType.CCD)
				{
					intamountlt = intamountl;
				}
				else
				{
					intamountlt = inParams.convertUnit(inParams.intLpSwitch, (QuantityType)inParams.intMode, QuantityType.Time, PhaseType.Lower);
				}
			}
			if (inParams.model == ModelType.CCD)
			{
				pos0 = vc / inParams.uf * inParams.maxIt; // in reality [steps]
			}
			else
			{
				pos0 = vc * inParams.maxIt;
			}
			if (inParams.doMaxIt && maxposu > pos0)
			{
				maxposu = pos0;
			}
			if (inParams.model == ModelType.CCD)
			{
				pos0 = vc / inParams.lf * inParams.maxIt; // in reality [steps]
			}
			else
			{
				pos0 = vc * inParams.maxIt;
			}
			if (inParams.doMaxIt && maxposl > pos0)
			{
				maxposl = pos0;
			}

			// init estmax [time/steps]
			previewParams.estmaxtime = 0;
			previewParams.estmaxstep = 0;

			// Set peaks
			// *** improve int mode: loop for [intit] with inner loop for [comps]: enable compmode and time scale correction
			for (int i = 0; i < ncomps; i++)
			{
				comp = inParams.comps[i];
				outComp = outSet.comps[i];
				k = comp.k;
				post = 0;

				// Calculate pos
				if (intMode)
				{
					// Int mode
					intit = 0;
					eluted = false;
					totposu = 0;
					totposl = 0;
					timepos = 0;
					pos = injectPos;
					phase = inParams.intStartPhase;
					while (intit / 2 < inParams.intMaxIt && !eluted)
					{
						if (phase == PhaseType.Upper)
						{
							if (!intCompMode)
							{
								if (natUnits == QuantityType.Steps)
								{
									intAmount = intamountu;
								}
								else
								{
									intAmount = intamountut;
								}
							}
							outCompt = calcNewPos(comp, fu, 0, vc, pos, natUnits, !intCompMode, intAmount, QuantityType.Time);
						}
						else
						{
							if (!intCompMode)
							{
								if (natUnits == QuantityType.Steps)
								{
									intAmount = intamountl;
								}
								else
								{
									intAmount = intamountlt;
								}
							}
							outCompt = calcNewPos(comp, 0, fl, vc, pos, natUnits, !intCompMode, intAmount, QuantityType.Time);
						}
                        eluted = outCompt.eluted;
						if (!eluted)
						{
                            post += Math.Abs(outCompt.retention - pos);
                            pos = outCompt.retention;
							if (phase == PhaseType.Upper)
							{
								totposu += intamountu;
								timepos += intamountut;
							}
							else
							{
								totposl += intamountl;
								timepos += intamountlt;
							}
							// prepare for next iteration/phase
							if (phase == PhaseType.Upper)
							{
								phase = PhaseType.Lower;
							}
							else
							{
								phase = PhaseType.Upper;
							}
							intit++;
						}
					}
					if (inParams.intFinalElute && !eluted)
					{
						// elute
						if (phase == PhaseType.Upper)
						{
                            outCompt = calcNewPos(comp, fu, 0, vc, pos, natUnits);
						}
						else
						{
                            outCompt = calcNewPos(comp, fl, 0, vc, pos, natUnits);
						}
                        eluted = outCompt.eluted;
					}
					if (eluted)
					{
						// timepos is always positive
                        //timepos+= inparams.convertUnit(Math::Abs(outCompt.ret),natUnits,UnitsType.Time,phase);
						timepos += Math.Abs(outCompt.retentionTime);
						if (timeMode)
						{
							// use timepos to calc pos
							if (inParams.model == ModelType.CCD)
							{
								pos = timepos;
							}
							else
							{
								pos = inParams.convertUnit(timepos, QuantityType.Time, natUnits, phase);
							}
							// correct negative pos
							if (phase == PhaseType.Lower)
							{
								pos = -Math.Abs(pos);
							}
						}
						else if (phase == PhaseType.Upper)
						{
                            pos = totposu + outCompt.retention0;
						}
						else
						{
                            pos = -totposl + outCompt.retention0;
						}
					}
					outComp.intIt = (float)intit / 2;
					outComp.intItSet = true;
				}
				else
				{
					// Normal (not Int) mode
					outCompt = calcNewPos(comp, fu, fl, vc, injectPos, natUnits, true, Math.Max(maxposu, maxposl), QuantityType.Volume);
					pos = outCompt.retention;
					post += Math.Abs(pos - injectPos);
					timepos = Math.Abs(outCompt.retentionTime);
					eluted = outCompt.eluted;
				}
				outComp.eluted = eluted;
				outComp.outCol = eluted;

				pos0 = pos;
				if (!eluted && eeMode)
				{
					// EE mode
					eeDone = true;
					if (inParams.isPosEEdir())
					{
						eepos = inParams.convertUnit(maxposu, natUnits, inParams.viewUnits, phase);
						if (!timeMode)
						{
							eepos += (vc - pos0);
						}
						else
						{
							eepos = 0;
						}
						pos = maxposu + (vc - pos0);
                        outCompt.phase = PhaseType.Upper;
					}
					else
					{
						eepos = inParams.convertUnit(maxposl, natUnits, inParams.viewUnits, phase);
						if (!timeMode)
						{
							eepos += pos0;
						}
						else
						{
							eepos = 0;
						}
						pos = -maxposl - pos0;
                        outCompt.phase = PhaseType.Lower;
					}
					outComp.outCol = true;
				}

                phase = outCompt.phase;
				// Calculate sigma
				if (outComp.outCol || outComp.willElute)
				{
					// include ee outcol
					timesigma = Equations.calcSigma(kdef, fnormu, fnorml, k * px, Math.Abs(timepos), mixspeed, efficiency);
					if (runMode == RunModeType.DualMode)
					{
						timesigma *= 2;
					}
					if (natUnits == QuantityType.Steps)
					{
						// [steps]
						sigma = timesigma;
					}
					else
					{
						// [volume]
						if (!outComp.eluted)
						{
							sigma = inParams.convertColUnit(timesigma, QuantityType.Time, natUnits);
						}
						else
						{
							sigma = inParams.convertUnit(timesigma, QuantityType.Time, natUnits, phase);
						}
					}
					if (!outComp.eluted)
					{
						// calc col sigma also for EE outcol
						// [volume] or [steps]
						sigma0 = sigma;
                        sigma = Equations.calcColSigma(sigma0, outCompt.retention0, post, vc);
					}
					if (outComp.eluted)
					{
						if (phase == PhaseType.Lower)
						{
							pos0 = pos - 3 * sigma;
							if (pos0 < mindrawpos)
							{
								mindrawpos = pos0;
							}
						}
						else
						{
							pos0 = pos + 3 * sigma;
							if (pos0 > maxdrawpos)
							{
								maxdrawpos = pos0;
							}
						}
					}
				}
				else
				{
					sigma = 1;
					phase = PhaseType.None;
				}
				outComp.sigma = sigma;
				outComp.phase = phase;

				// set estmax [time]
				if (outComp.outCol)
				{
					maxtime0 = Math.Abs(timepos) + 3 * timesigma;
					if (maxtime0 > previewParams.estmaxtime)
					{
						previewParams.estmaxtime = maxtime0;
					}
				}

				if (outComp.outCol)
				{
					if (outComp.eluted)
					{
						outComp.retention = inParams.convertUnit(Math.Abs(pos), natUnits, viewParams.viewUnits, phase);
						outComp.width = inParams.convertUnit(4 * sigma, natUnits, viewParams.viewUnits, phase);
					}
					else
					{
						// not eluted but outcol (by EE)
						outComp.retention = eepos;
						outComp.width = 4 * sigma;
					}
				}
				else
				{
					outComp.retention = inParams.convertColUnit(Math.Abs(pos), natUnits, viewParams.viewUnits);
					outComp.width = inParams.convertColUnit(4 * sigma, natUnits, viewParams.viewUnits);
				}
				outComp.drawPosition = pos; // convert to real value later
				// Calculate height
				if (float.IsInfinity(k) || sigma == 0)
				{
					outComp.height = 1;
				}
				else
				{
					outComp.height = Equations.calcHeight(sigma); // [volume]
				}
				outComp.height *= comp.m;
			}
			// set estmax [steps]
			if (inParams.doMaxIt)
			{
				// overwrite if maxit is set
				pos0 = 0;
				if (fu != 0)
				{
					pos0 = inParams.convertUnit(inParams.maxIt * vc, QuantityType.Volume, QuantityType.Time, PhaseType.Upper);
				}
				if (fl != 0)
				{
					pos0 = Math.Max(pos0, inParams.convertUnit(inParams.maxIt * vc, QuantityType.Volume, QuantityType.Time, PhaseType.Lower));
				}
				if (previewParams.estmaxtime > pos0)
				{
					previewParams.estmaxtime = pos0;
				}
			}
			if (previewParams.estmaxtime == 0)
			{
				previewParams.estmaxtime = inParams.Tmnorm;
				previewParams.estmaxstep = inParams.column;
			}
			else
			{
				if (natUnits == QuantityType.Steps)
				{
					// CCD mode: time units are actually step units
					previewParams.estmaxstep = previewParams.estmaxtime;
				}
				else
				{
					previewParams.estmaxstep = inParams.convertUnit(previewParams.estmaxtime, QuantityType.Time, QuantityType.Steps, phase);
				}
			}

			if (maxdrawpos == 0 && mindrawpos == 0)
			{
				if (inParams.doMaxIt)
				{
					maxdrawpos = maxposu;
					mindrawpos = -maxposl;
				}
			}
			if (eeMode)
			{
				if (inParams.isPosEEdir())
				{
					maxdrawpos += vc;
				}
				else
				{
					mindrawpos -= vc;
				}
			}
			range = maxdrawpos - mindrawpos + vc;
			// Correct drawpos
			for (int i = 0; i < ncomps; i++)
			{
				outComp = outSet.comps[i];
				pos0 = outComp.drawPosition;
				if (outComp.outCol)
				{
					if (outComp.phase == PhaseType.Upper)
					{
						pos0 = maxdrawpos - pos0 + vc;
					}
					else
					{
						pos0 = mindrawpos - pos0;
					}
					allincol = false;
				}
				outComp.drawPosition = convModelToReal(pos0, mindrawpos);
			}

			// Set Axes
			axes = outSet.axes;
			axes.rangex = range; //inparams->convertUnit(drawrange,UnitsType::Time,viewparams->viewUnits,PhaseType::Up);
			axes.showCol = true;
			axes.colstart = convModelToReal(0, mindrawpos);
			axes.colend = convModelToReal(vc, mindrawpos);

			axes.scaleminulabel = 0;
			axes.scalemaxulabel = inParams.convertUnit(maxdrawpos, natUnits, inParams.natUnits, PhaseType.Upper);
			axes.scaleminllabel = 0;
			axes.scalemaxllabel = inParams.convertUnit(-mindrawpos, natUnits, inParams.natUnits, PhaseType.Lower);
			axes.scaleminu = convModelToReal(maxdrawpos + vc, mindrawpos);
			axes.scalemaxu = convModelToReal(vc, mindrawpos);
			axes.scaleminl = convModelToReal(mindrawpos, mindrawpos);
			axes.scalemaxl = convModelToReal(0, mindrawpos);

			axes.logScale = (viewParams.yScale == YScaleType.Logarithmic);
			axes.update();

			// init outcells
			outCells = new OutCell[ncomps][];
			for (int compi = 0; compi < ncomps; compi++)
			{
				outCells[compi] = new OutCell[nsize];
			}
			axes.maxcon = new List<float>(ncomps);
			// Store outcells
			for (int compi = 0; compi < ncomps; compi++)
			{
				comp = inParams.comps[compi];
				outComp = outSet.comps[compi];
				// Pre-calcs to improve performance
				pos0 = outComp.drawPosition;
				sigma = outComp.sigma;
				if (sigma == 0)
				{
					sigma = 1;
				}
				amp0 = comp.m * Equations.calcHeight(sigma);
				div0 = 2 * (float)Math.Pow(sigma, 2);
				stepsize = range / nsize;
				maxcon = 0;
				for (int i = 0; i < nsize; i++)
				{
					pos = i * stepsize; // Real position
					// make sure peak top gets drawn:
					if (Math.Abs(pos - pos0) <= stepsize)
					{
						con = amp0;
					}
					else
					{
						con = amp0 * (float)Math.Exp(-Math.Pow(pos - pos0, 2) / div0);
					}
					if (con > maxcon && (outComp.outCol || allincol))
					{
						maxcon = con;
					}
					outCells[compi][i] = new OutCell(pos, con);
				}
				axes.maxcon.Add(maxcon);
				// Correct sigma:
				outComp.sigma = outComp.width / 4;
			}
			outSet.outCells = outCells;

			return outSet;
		}

		public override void storeTimeOut(ViewParams viewparams)
		{
			// Dummy; never used
		}

		public OutComp calcNewPos(Comp comp, float fu, float fl, float vc, float pos0, QuantityType natUnits)
		{
			return calcNewPos(comp, fu, fl, vc, pos0, natUnits, false, 0, QuantityType.Column);
		}

		public OutComp calcNewPos(Comp comp, float fu, float fl, float vc, float pos0, QuantityType natUnits, bool limit, float maxpos, QuantityType limitUnits)
		{
			OutComp outcomp = new OutComp();
			float flow;
			float ret;
			float rettime;
			float vceff;
			PhaseType phase = new PhaseType();
			float elutable;
			float lf = inParams.lf;
			float uf = inParams.uf;
			float px = inParams.px;
			KdefType kdef = inParams.kDefinition;
			float k = comp.k;

			// * flow
			if (natUnits == QuantityType.Steps)
			{
				flow = Equations.calcStepFlow(kdef, lf, uf, fu, fl, k);
			}
			else
			{
				flow = Equations.calcFlow(kdef, fu, fl, k);
			}

			if (inParams.runMode != RunModeType.CoCurrent)
			{
				elutable = Equations.calcElutable(kdef, fu, -fl, k);
			}
			else
			{
				elutable = Equations.calcElutable(kdef, fu, fl, k);
			}

			if (elutable > 0.01)
			{
				// will elute
				// * vceff
				if (flow < 0)
				{
					vceff = pos0;
				}
				else
				{
					vceff = vc - pos0;
				}
				// * ret
				// [time]
				rettime = Equations.calcPos(kdef, vceff, lf, uf, flow, k);
				outcomp.retentionTime = rettime;
				if (rettime < 0)
				{
					phase = PhaseType.Lower;
				}
				else
				{
					phase = PhaseType.Upper;
				}

				if (natUnits == QuantityType.Steps)
				{
					// [steps]
					ret = rettime;
				}
				else
				{
					// [volume] or [time]
					ret = inParams.convertUnit(rettime, QuantityType.Time, natUnits, phase);
				}
				outcomp.retention0 = ret;
				// * new pos
				if (limit && (Math.Abs(rettime) > maxpos && limitUnits == QuantityType.Time || Math.Abs(ret) > maxpos && limitUnits != QuantityType.Time))
				{
					// [volume] or [steps]
					outcomp.retention = pos0 + Equations.calcColPos(kdef, maxpos, lf, uf, flow, k);
					outcomp.eluted = false;
				}
				else
				{
					outcomp.retention = ret;
					outcomp.eluted = true;
				}
			}
			else
			{
				outcomp.retention = pos0;
				if (flow < 0)
				{
					phase = PhaseType.Lower;
				}
				else
				{
					phase = PhaseType.Upper;
				}
			}
			outcomp.phase = phase;
			return outcomp;
		}

		public float convModelToReal(float pos, float minpos)
		{
			return pos - minpos;
		}

		public float convRealToModel(float pos, float minpos)
		{
			return pos + minpos;
		}

	}
}
