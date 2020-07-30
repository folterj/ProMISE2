using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace ProMISE2
{
    public class InParamsExt : InParams, INotifyPropertyChanged
    {
        public InParamsExt()
            : base()
        {
            update();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string sProp)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(sProp));
            }
        }

        [XmlIgnore]
        public float vc2 { get; set; }

        [XmlIgnore]
        public int column2 { get; set; }

        [XmlIgnore]
        public float vu
        {
            get
            {
                return uf * vc;
            }
            set { }
        }

        [XmlIgnore]
        public float vl
        {
            get
            {
                return lf * vc;
            }
            set { }
        }

        [XmlIgnore]
        public float px
        {
            get
            {
                return Equations.calcPX(kDefinition, lf, uf);
            }
            set { }
        }

        [XmlIgnore]
        public float fnormu { get; set; }

        [XmlIgnore]
        public float fnorml { get; set; }

        [XmlIgnore]
        public QuantityType natUnits
        {
            get
            {
                switch (model)
                {
                    case ModelType.CCD:
                    case ModelType.Transport:
                        return QuantityType.Steps;

                    default:
                        return QuantityType.Volume;
                }
            }
        }

        [XmlIgnore]
        public float Tmu
        {
            get
            {
                if (fu != 0)
                {
                    return vu / fu;
                }
                return 0;
            }
        }

        [XmlIgnore]
        public float Tml
        {
            get
            {
                if (fl != 0)
                {
                    return vl / fl;
                }
                return 0;
            }
        }

        [XmlIgnore]
        public float Tmnorm
        {
            get
            {
                if (runMode == RunModeType.CoCurrent)
                {
                    return vc / (fu + fl);
                }
                return Math.Max(Tmu, Tml);
            }
        }

        public void updateProfile()
        {
            switch (profile)
            {
                case ProfileType.CCD:
                case ProfileType.CPC:
                case ProfileType.VortexCCD:
                case ProfileType.ToroidalCCC:
                    model = ModelType.CCD;
                    break;

                case ProfileType.CCC:
                case ProfileType.DroppletCCC:
                    model = ModelType.Probabilistic;
                    break;
            }
            NotifyPropertyChanged("model");

            updateModel();
        }

        public void updateModel()
        {
            if (model == ModelType.CCD || model == ModelType.Transport)
            {
                mixSpeed = 1;
            }
            else
            {
                ptransMode = false;
                if (viewUnits == QuantityType.Steps)
                {
                    viewUnits = QuantityType.Volume;
                }
                if (injectFeedUnits == QuantityType.Steps)
                {
                    injectFeedUnits = QuantityType.Volume;
                }
                if (vdeadUnits == QuantityType.Steps)
                {
                    vdeadUnits = QuantityType.Volume;
                }
                if (intMode == IntModeType.Steps)
                {
                    intMode = IntModeType.Volume;
                }
            }

            NotifyPropertyChanged("natUnits");

            if (model == ModelType.Transport)
            {
                autoFilter = false;

                NotifyPropertyChanged("autoFilter");
            }
            else if (model == ModelType.Probabilistic)
            {
                autoFilter = true;

                NotifyPropertyChanged("autoFilter");
            }

            if (model != ModelType.CCD && ptransMode)
            {
                ptransMode = false;
                NotifyPropertyChanged("PtransMode");
                NotifyPropertyChanged("PtransModeEnabled");
            }

			NotifyPropertyChanged("TextLabel");

            normFlow();
        }

        public void updateRunMode()
        {
            float fu0 = 0;
            float fl0 = 0;
            float ptransu0 = 0;
            float ptransl0 = 0;
            PhaseType injectPhase0 = PhaseType.Both;
            float injectPos0 = 0;

            switch (runMode)
            {
                case RunModeType.UpperPhase: fu0 = 1; fl0 = 0; ptransu0 = 1; ptransl0 = 0; injectPhase0 = PhaseType.Upper; injectPos0 = 0; break;
                case RunModeType.LowerPhase: fu0 = 0; fl0 = 1; ptransu0 = 0; ptransl0 = 1; injectPhase0 = PhaseType.Lower; injectPos0 = 0; break;
                case RunModeType.DualMode: fu0 = 1; fl0 = 1; ptransu0 = 1; ptransl0 = 1; injectPhase0 = PhaseType.Both; injectPos0 = 0.5f; break;
                case RunModeType.Intermittent: fu0 = 1; fl0 = 1; ptransu0 = 1; ptransl0 = 1; injectPhase0 = PhaseType.Both; injectPos0 = 0.5f; break;
                case RunModeType.CoCurrent: fu0 = 1; fl0 = 1; ptransu0 = 1; ptransl0 = 1; injectPhase0 = PhaseType.Both; injectPos0 = 0; break;
            }
            if (fu == 0 || fu0 == 0)
            {
                fu = fu0;
            }
            if (fl == 0 || fl0 == 0)
            {
                fl = fl0;
            }
            if (ptransu == 0 || ptransu0 == 0 || !ptransMode)
            {
                ptransu = ptransu0;
            }
            if (ptransl == 0 || ptransl0 == 0 || !ptransMode)
            {
                ptransl = ptransl0;
            }
            injectPos = injectPos0;
            injectPhase = injectPhase0;

            NotifyPropertyChanged("injectPhase");
            NotifyPropertyChanged("injectPos");
            NotifyPropertyChanged("fu");
            NotifyPropertyChanged("fl");
            NotifyPropertyChanged("ptransu");
            NotifyPropertyChanged("ptransl");

            if (runMode != RunModeType.Intermittent)
            {
                intStartPhase = PhaseType.None;
                intMode = IntModeType.Time;
                intFinalElute = true;
                intUpSwitch = 0;
                intLpSwitch = 0;
                intUpComp = -1;
                intLpComp = -1;

                NotifyPropertyChanged("intStartPhase");
                NotifyPropertyChanged("intMode");
                NotifyPropertyChanged("intFinalElute");
                NotifyPropertyChanged("intUpSwitch");
                NotifyPropertyChanged("intLpSwitch");
                NotifyPropertyChanged("intUpComp");
                NotifyPropertyChanged("intLpComp");
            }
            else
            {
                if (intStartPhase == PhaseType.None)
                {
                    intStartPhase = PhaseType.Upper;

                    NotifyPropertyChanged("intStartPhase");
                }
            }

            normFlow();

			NotifyPropertyChanged("TextLabel");
        }

        public void updateIntMode()
        {
            if (runMode == RunModeType.Intermittent)
            {
                if (intMode == IntModeType.Component)
                {
                    intUpSwitch = 0;
                    intLpSwitch = 0;

                    NotifyPropertyChanged("intUpSwitch");
                    NotifyPropertyChanged("intLpSwitch");
                }
                else
                {
                    intUpComp = -1;
                    intLpComp = -1;

                    NotifyPropertyChanged("intUpComp");
                    NotifyPropertyChanged("intLpComp");
                }
            }
        }

        public void updateInjectMode()
        {
            if (injectMode == InjectModeType.Continuous)
            {
                doMaxIt = true;

                NotifyPropertyChanged("doMaxIt");
            }
            else if (injectMode == InjectModeType.Batch)
            {
                if (injectFeed == 0)
                {
                    injectFeed = 1;

                    NotifyPropertyChanged("injectFeed");
                }
            }
            else
            {
                injectFeed = 0;

                NotifyPropertyChanged("injectFeed");
            }
        }

        public void updateAdvancedMode()
        {
            PhaseType injectPhase0 = PhaseType.Both;
            float injectPos0 = 0;

            switch (runMode)
            {
                case RunModeType.UpperPhase: injectPhase0 = PhaseType.Upper; injectPos0 = 0; break;
                case RunModeType.LowerPhase: injectPhase0 = PhaseType.Lower; injectPos0 = 0; break;
                case RunModeType.DualMode: injectPhase0 = PhaseType.Both; injectPos0 = 0.5f; break;
                case RunModeType.Intermittent: injectPhase0 = PhaseType.Both; injectPos0 = 0.5f; break;
                case RunModeType.CoCurrent: injectPhase0 = PhaseType.Both; injectPos0 = 0; break;
            }

            if (!advancedMode)
            {
                doMaxIt = false;
                eeMode = EEModeType.None;
                intFinalElute = true;
				ptransMode = false;

                if (model == ModelType.CCD)
                {
                    efficiency = 1;
				}
				else if (model == ModelType.Probabilistic)
				{
					probUnits = 10000;
					densitySteps = 200;
				}
                autoFilter = true;
                injectPos = injectPos0;
                injectPhase = injectPhase0;

				updateProfile();

                NotifyPropertyChanged("doMaxIt");
                NotifyPropertyChanged("eeMode");
                NotifyPropertyChanged("intFinalElute");
                NotifyPropertyChanged("eff");
                NotifyPropertyChanged("autoFilter");
                NotifyPropertyChanged("injectPos");
                NotifyPropertyChanged("injectPhase");
            }
        }

        public void update()
        {
            int ncomps = comps.Count;
            float vol;
            bool justset;

            if (!doMaxIt)
            {
                maxIt = 10;
            }

            normFlow();

            if (intUpComp >= ncomps)
            {
                intUpComp = ncomps - 1;
            }
            if (intLpComp >= ncomps)
            {
                intLpComp = ncomps - 1;
            }

            if (!vdeadInEnabled)
            {
                vdeadIn = 0;
            }
            if (!vdeadOutEnabled)
            {
                vdeadOut = 0;
            }
            if (!vdeadInjectEnabled)
            {
                vdeadInject = 0;
            }

            for (int i = 0; i < comps.Count; i++) // always use dynamic comps.size in case of length change
            {
                justset = (comps[i].m <= 0 && comps[i].concentration <= 0);
                if (comps[i].label == "" && !justset)
                {
                    // remove
                    comps.RemoveAt(i);
                }
                else
                {
                    // set defaults
					if (justset && comps[i].label == "")
					{
						comps[i].label = string.Format("K={0}", comps[i].k);
					}
					if (justset || !advancedMode)
					{
						comps[i].elute = true;
					}
					if (justset || comps[i].m == 0)
					{
						comps[i].m = 1;
					}
					if (justset || !advancedMode || comps[i].concentration == 0 || injectMode != InjectModeType.Continuous)
                    {
                        // con only changable in continuous sample insertion mode
                        if (injectMode == InjectModeType.Batch)
                        {
                            vol = convertUnit(injectFeed, injectFeedUnits, QuantityType.Volume, injectPhase);
                            comps[i].concentration = comps[i].m / vol;
                        }
                        else if (injectMode == InjectModeType.Continuous)
                        {
                            comps[i].concentration = comps[i].m / 100;
                        }
                        else
                        {
                            comps[i].concentration = comps[i].m;
                        }
                    }
                }
            }
            column2 = column + (int)Math.Round(getVdeadIn()) + (int)Math.Round(getVdeadOut()) + (int)Math.Round(getVdeadInject());
            vc2 = vc + getVdeadIn() + getVdeadOut() + getVdeadInject();

            NotifyPropertyChanged("vc2");
            NotifyPropertyChanged("column2");
            NotifyPropertyChanged("comps");

            NotifyPropertyChanged("VdeadIn");
            NotifyPropertyChanged("VdeadOut");
            NotifyPropertyChanged("VdeadInject");
            NotifyPropertyChanged("VdeadStart");
            NotifyPropertyChanged("VdeadEnd");
            NotifyPropertyChanged("VdeadInjectStart");
            NotifyPropertyChanged("VdeadInjectEnd");

			NotifyPropertyChanged("TextLabel");
        }

        public void normFlow()
        {
            fnormu = fu;
            fnorml = fl;

            if (model == ModelType.CCD)
            {
                if (runMode == RunModeType.DualMode || runMode == RunModeType.Intermittent || runMode == RunModeType.CoCurrent)
                {
                    if (fnorml != 0 && fnormu != 0)
                    {
                        // correct flow for phase ratio:
                        fnormu /= uf;
                        fnorml /= lf;
                    }
                    // normalise flow:
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
                }
                else
                {
                    if (fnormu != 0 && fnormu != 1)
                    {
                        fnormu = 1;
                    }
                    if (fnorml != 0 && fnorml != 1)
                    {
                        fnorml = 1;
                    }
                }
                if (ptransMode)
                {
                    fnormu *= ptransu;
                    fnorml *= ptransl;
                }
            }
            else if (model == ModelType.Probabilistic)
            {
                fnormu /= uf;
                fnorml /= lf;
            }

            NotifyPropertyChanged("fnormu");
            NotifyPropertyChanged("fnorml");
        }

        public float getCellVol(PhaseType phase)
        {
            float vol = 0;
            if (phase == PhaseType.Upper || phase == PhaseType.Both)
            {
                vol += vu / column;
            }
            if (phase == PhaseType.Lower || phase == PhaseType.Both)
            {
                vol += vl / column;
            }
            return vol;
        }

        public bool getNormalColDirection()
        {
            if (model == ModelType.CCD)
            {
				// CCD model
                if (runMode == RunModeType.LowerPhase || runMode == RunModeType.CoCurrent ||
					(runMode == RunModeType.Intermittent && intStartPhase == PhaseType.Lower))
                    return false;
            }
            else
            {
				// Prob / Trans model
                if (runMode == RunModeType.LowerPhase ||
					(runMode == RunModeType.Intermittent && intStartPhase == PhaseType.Lower))
                    return false;
            }
            return true;
        }

        public float getInjectPosNorm()
        {
            float pos = 0;

            if (natUnits == QuantityType.Steps)
            {
                if (getNormalColDirection())
                {
                    pos = injectPos * (column2 - 1);
                }
                else
                {
                    pos = (1 - injectPos) * (column2 - 1);
                }
            }
            else
            {
                if (getNormalColDirection())
                {
                    pos = injectPos * vc2;
                }
                else
                {
                    pos = (1 - injectPos) * vc2;
                }
            }
            return pos;
        }

        public bool isPosEEdir()
        {
            bool posdir = (runMode != RunModeType.LowerPhase);
            if (eeMode == EEModeType.BEE)
            {
                posdir = !posdir;
            }
            return posdir;
        }

        public bool getNormEEdiru()
        {
            bool normdir;

            switch (runMode)
            {
                case RunModeType.CoCurrent: normdir = true; break;
                case RunModeType.Intermittent: normdir = (intStartPhase == PhaseType.Upper); break;
                case RunModeType.LowerPhase: normdir = false; break;
                default: normdir = true; break;
            }
            if (eeMode == EEModeType.BEE)
            {
                normdir = !normdir;
            }
            return normdir;
        }

        public bool getNormEEdirl()
        {
            bool normdir;

            switch (runMode)
            {
                case RunModeType.CoCurrent: normdir = true; break;
                case RunModeType.Intermittent: normdir = (intStartPhase == PhaseType.Lower); break;
                case RunModeType.LowerPhase: normdir = true; break;
                default: normdir = false; break;
            }
            if (eeMode == EEModeType.BEE)
            {
                normdir = !normdir;
            }
            return normdir;
        }

        public float getVdeadIn()
        {
            if (vdeadInEnabled)
            {
                return convertColUnit(vdeadIn, vdeadUnits, natUnits);
            }
            return 0;
        }

        public float getVdeadOut()
        {
            if (vdeadOutEnabled)
            {
                return convertColUnit(vdeadOut, vdeadUnits, natUnits);
            }
            return 0;
        }

        public float getVdeadInject()
        {
            if (vdeadInjectEnabled && injectPos != 0 && injectPos != 1)
            {
                return convertColUnit(vdeadInject, vdeadUnits, natUnits);
            }
            return 0;
        }

        public float getVdeadStart()
        {
            if (getNormalColDirection())
            {
                return getVdeadIn();
            }
            else
            {
                return getVdeadOut();
            }
        }

        public float getVdeadEnd()
        {
            if (getNormalColDirection())
            {
                if (natUnits == QuantityType.Steps)
                {
                    return column2 - getVdeadOut();
                }
                else
                {
                    return vc2 - getVdeadOut();
                }
            }
            else
            {
                if (natUnits == QuantityType.Steps)
                {
                    return column2 - getVdeadIn();
                }
                else
                {
                    return vc2 - getVdeadIn();
                }
            }
        }

        public float getVdeadInjectStart()
        {
            return getInjectPosNorm() - getVdeadInject() / 2;
        }

        public float getVdeadInjectEnd()
        {
            return getInjectPosNorm() + getVdeadInject() / 2;
        }

		public string getXaxisUnits()
		{
			string s = "";

			switch (viewUnits)
			{
				case QuantityType.Volume: s = volUnits.ToString(); break;
				case QuantityType.Time: s = timeUnits.ToString(); break;
			}
			return s;
		}

        public string getXaxisLegend()
        {
			string s = getXaxisUnits();

            if (s != "")
            {
                s = " [" + s + "]";
            }
            s = viewUnits.ToString() + s;

            return s;
        }

		public string getYaxisUnits(MassUnitsType? prefMassUnits)
		{
			MassUnitsType massUnits0;
			string units;

			if (prefMassUnits != null)
			{
				massUnits0 = (MassUnitsType)prefMassUnits;
			}
			else
			{
				massUnits0 = massUnits;
			}
			units = Util.GetEnumDescription(massUnits0).Replace("[", "").Replace("]", "");
			return string.Format("{0}/{1}", units, volUnits);
		}

		public string getYaxisLegend(MassUnitsType? prefMassUnits)
        {
			return string.Format("Concentration [{0}]", getYaxisUnits(prefMassUnits));
        }

		public TextParamList getText(bool showAll = false)
		{
			TextParamList textList = new TextParamList();
			string units, compLabel;

			// Main
			if (showAll)
			{
				textList.addHeader("Main parameters");
				textList.add("Profile", Util.GetEnumDescription(profile));
				textList.add("Model", model.ToString());
				textList.add("Run mode", Util.GetEnumDescription(runMode));
				textList.add("K definition", Util.GetEnumDescription(kDefinition));
			}

			// Column
			textList.addHeader("Column parameters");
			textList.add("Vc", vc.ToString(), volUnits.ToString());
			if (vdeadInEnabled)
			{
				textList.add("Vdead in", vdeadIn.ToString(), volUnits.ToString());
			}
			if (vdeadOutEnabled)
			{
				textList.add("Vdead out", vdeadOut.ToString(), volUnits.ToString());
			}
			if (vdeadInjectEnabled)
			{
				textList.add("Vdead inject", vdeadInject.ToString(), volUnits.ToString());
			}
			textList.add("Uf", uf.ToString(), "0..1");
			textList.add("Lf", lf.ToString(), "0..1");

			// Flow
			textList.addHeader("Flow parameters");
			units = string.Format("{0}/{1}", volUnits, timeUnits);
			if (fu != 0)
			{
				textList.add("Fu", fu.ToString(), units);
			}
			if (fl != 0)
			{
				textList.add("Fl", fl.ToString(), units);
			}
			if (eeMode != EEModeType.None)
			{
				textList.add(Util.GetEnumDescription(eeMode));
			}
			if (runMode == RunModeType.Intermittent)
			{
				textList.add("start phase", intStartPhase.ToString());
				if (intMode == IntModeType.Component)
				{
					if (intUpComp >= 0 && intUpComp < comps.Count)
					{
						compLabel = comps[intUpComp].label;
					}
					else
					{
						compLabel = "-";
					}
					textList.add("Upper switch", compLabel);

					if (intLpComp >= 0 && intLpComp < comps.Count)
					{
						compLabel = comps[intLpComp].label;
					}
					else
					{
						compLabel = "-";
					}
					textList.add("Lower switch", compLabel);
				}
				else if (intMode == IntModeType.Volume)
				{
					textList.add("Upper switch", intUpSwitch.ToString(), volUnits.ToString());
					textList.add("Lower switch", intLpSwitch.ToString(), volUnits.ToString());
				}
				else
				{
					textList.add("Upper switch", intUpSwitch.ToString(), timeUnits.ToString());
					textList.add("Lower switch", intLpSwitch.ToString(), timeUnits.ToString());
				}
				textList.add("Max iterations", intMaxIt.ToString());
				if (intFinalElute)
				{
					textList.add("Final elution");
				}
			}

			// Inject
			textList.addHeader("Inject parameters");
			textList.add("Inject mode", injectMode.ToString());
			textList.add("Inject phase", injectPhase.ToString());
			textList.add("Inject volume", injectVolume.ToString(), volUnits.ToString());
			textList.add("Inject position", injectPos.ToString(), "0..1");
			if (injectMode == InjectModeType.Batch)
			{
				if (injectFeedUnits == QuantityType.Volume)
				{
					units = volUnits.ToString();
				}
				else if (injectFeedUnits == QuantityType.Time)
				{
					units = timeUnits.ToString();
				}
				textList.add("Inject feed", injectFeed.ToString(), units);
			}

			// Model
			textList.addHeader("Model parameters");
			if (model == ModelType.CCD || model == ModelType.Transport)
			{
				textList.add("Steps", column.ToString());
			}
			if (model == ModelType.Probabilistic)
			{
				units = string.Format("1/{0}", timeUnits);
				textList.add("Rotational speed", mixSpeed.ToString(), units);
			}
			if (model == ModelType.CCD || model == ModelType.Probabilistic)
			{
				textList.add("Efficiency", Util.toString(efficiency, 4), "0..1");
			}
			if (model == ModelType.Transport)
			{
				textList.add("Mass transfer", ka.ToString());
			}
			return textList;
		}

		public ParamType validate(OptionParams optionparams)
		{
			ParamType paramType = ParamType.None;
			bool colok, volok;
			bool rotok, effok, kaok;
			bool maxitok, phaseok, flowok, ptransok;
			bool injectposok, injectvolok, injectfeedok, compsok;
			bool intUPok, intLPok, intUPcompok, intLPcompok, intmaxitok;

			volok = (vc > 0);
			rotok = (mixSpeed > 0 && mixSpeed < 100000);
			colok = (column > 0 && column < 10000);
			effok = (efficiency >= 0 && efficiency <= 1);
			kaok = (ka >= 0 && ka <= 1);
			maxitok = (maxIt >= 0);

			phaseok = (px > 0.01 && px < 100);
			flowok = (((runMode == RunModeType.UpperPhase && fu > 0) || (runMode == RunModeType.LowerPhase && fl > 0) || (runMode != RunModeType.UpperPhase && runMode != RunModeType.LowerPhase)) &&
					(fu > 0 || fl > 0) && fu >= 0 && fl >= 0 && fu < 1000 && fl < 1000);
			ptransok = true;
			if (ptransMode)
			{
				ptransok = ((ptransu > 0 || ptransl > 0) && ptransu >= 0 && ptransl >= 0 && ptransu <= 1 && ptransl <= 1);
			}

			injectposok = (injectPos >= 0 && injectPos <= 1);
			injectvolok = (injectVolume > 0);
			injectfeedok = (injectFeed > 0);

			if (runMode == RunModeType.Intermittent || eeMode != EEModeType.None)
			{
				compsok = true;
			}
			else
			{
				compsok = false;
				for (int compi = 0; compi < comps.Count; compi++)
				{
					if (comps[compi].elute)
					{
						compsok = true;
					}
				}
			}

			intUPok = (intUpSwitch >= 0);
			intLPok = (intLpSwitch >= 0);
			intUPcompok = (intUpComp >= 0 && intUpComp < comps.Count);
			intLPcompok = (intLpComp >= 0 && intLpComp < comps.Count);
			intmaxitok = (intMaxIt >= 0);

			if (!volok)
			{
				paramType = ParamType.Column;
			}
			else if (!rotok && model == ModelType.Probabilistic)
			{
				paramType = ParamType.Advanced;
			}
			else if (!colok && (model == ModelType.CCD || model == ModelType.Transport))
			{
				paramType = ParamType.Column;
			}
			else if (!effok && (model == ModelType.CCD || model == ModelType.Probabilistic))
			{
				paramType = ParamType.Advanced;
			}
			else if (!kaok && model == ModelType.Transport)
			{
				paramType = ParamType.Advanced;
			}
			else if (!maxitok && doMaxIt)
			{
				paramType = ParamType.Flow;
			}
			else if (!phaseok)
			{
				paramType = ParamType.Column;
			}
			else if (!flowok)
			{
				paramType = ParamType.Flow;
			}
			else if (!ptransok)
			{
				paramType = ParamType.Flow;
			}
			else if (!injectposok)
			{
				paramType = ParamType.Inject;
			}
			else if (!injectvolok)
			{
				paramType = ParamType.Inject;
			}
			else if (!injectfeedok && injectMode == InjectModeType.Batch)
			{
				paramType = ParamType.Inject;
			}
			else if (!compsok)
			{
				paramType = ParamType.Components;
			}
			else if (!intUPok && intMode != IntModeType.Component)
			{
				paramType = ParamType.Flow;
			}
			else if (!intLPok && intMode != IntModeType.Component)
			{
				paramType = ParamType.Flow;
			}
			else if (!intUPcompok && intMode == IntModeType.Component)
			{
				paramType = ParamType.Flow;
			}
			else if (!intLPcompok && intMode == IntModeType.Component)
			{
				paramType = ParamType.Flow;
			}
			else if (!intmaxitok)
			{
				paramType = ParamType.Flow;
			}
			else if (!checkTransStability(optionparams) && model == ModelType.Transport && !advancedMode)
			{
				paramType = ParamType.Stability;
			}

			return paramType;
		}

		public bool checkTransStability(OptionParams optionparams)
		{
			bool condition1, condition2;
			float maxdelta1, maxdelta2u, maxdelta2l;
			int columnsteps = column;
			int timesteps = (int)Math.Round(columnsteps / optionparams.cflConstant);
			float dx = 1.0f / columnsteps;
			float dt = 1.0f / timesteps;
			float tm, tmu, tml;
			float maxk = 0;

			for (int compi = 0; compi < comps.Count; compi++)
			{
				if (comps[compi].k > maxk)
					maxk = comps[compi].k;
			}
			maxk = Math.Max(maxk, 1.0f);

			maxdelta1 = dt / dx; // CFL constant (should be same value as optionparams->cflconstant)
			condition1 = (maxdelta1 <= 1); // CFL

			tm = convertUnit(column, QuantityType.Steps, QuantityType.Time, PhaseType.Both) * 60; // assume [min] => [s]
			tmu = convertUnit(column, QuantityType.Steps, QuantityType.Time, PhaseType.Upper) * 60; // assume [min] => [s]
			tml = convertUnit(column, QuantityType.Steps, QuantityType.Time, PhaseType.Lower) * 60; // assume [min] => [s]
			if (tmu == 0)
			{
				tmu = tm;
			}
			if (tml == 0)
			{
				tml = tm;
			}

			maxdelta2u = (ka * maxk * tmu * dt / uf);
			maxdelta2l = (ka * maxk * tml * dt / lf);
			condition2 = (maxdelta2u <= 1 && maxdelta2l <= 1);

			return (condition1 && condition2);
		}

        public float convertUnit(float val0, QuantityType srcUnits, QuantityType dstUnits, PhaseType phase)
        {
            float val = val0;
            float vm0 = 0;
            float vm = 0;
            float tm = 0;
            float flow = 0;

            if (dstUnits == srcUnits)
            {
                return val;
            }

            if (phase == PhaseType.Upper || phase == PhaseType.Both || runMode == RunModeType.CoCurrent)
            {
                vm0 += vu;
                vm += vu * fnormu;
                flow += fu;
                if (fu != 0)
                {
                    tm += vu / fu;
                }
            }
            if (phase == PhaseType.Lower || phase == PhaseType.Both || runMode == RunModeType.CoCurrent)
            {
                vm0 += vl;
                vm += vl * fnorml;
                flow += fl;
                if (fl != 0)
                {
                    tm += vl / fl;
                }
            }

            if (model == ModelType.Transport)
            {
                if (runMode == RunModeType.CoCurrent)
                {
                    tm = Tmnorm;
                }
                // x -> time
                switch (srcUnits)
                {
                    case QuantityType.Steps:
                        val *= (tm / column);
                        break;
                    case QuantityType.Volume:
                        val /= flow;
                        break;
                }
                tm = Tmnorm;
                // time -> y
                switch (dstUnits)
                {
                    case QuantityType.Steps:
                        val /= (tm / column);
                        break;
                    case QuantityType.Volume:
                        val *= flow;
                        break;
                    case QuantityType.Column:
                        val /= tm;
                        break;
                    case QuantityType.ReS:
                        val = Equations.calcK(kDefinition, vu, vl, fu, fl, val * flow);
                        break;
                }
            }
            else
            {
                // x -> vol
                switch (srcUnits)
                {
                    case QuantityType.Steps:
                        val *= (vm / column);
                        break;
                    case QuantityType.Time:
                        val *= flow;
                        break;
                }
                // vol -> y
                switch (dstUnits)
                {
                    case QuantityType.Steps:
                        val /= (vm / column);
                        break;
                    case QuantityType.Time:
                        val /= flow;
                        break;
                    case QuantityType.Column:
                        val /= vm0;
                        break;
                    case QuantityType.ReS:
                        val = Equations.calcK(kDefinition, vu, vl, fu, fl, val);
                        break;
                }
            }
            return val;
        }

        public float convertColUnit(float val0, QuantityType srcUnits, QuantityType dstUnits)
        {
            float val = val0;
            float tm = 0;
            float flow = 0;

            if (dstUnits == srcUnits)
            {
                return val;
            }

            flow = fu + fl;
            tm = vc / flow;

            if (model == ModelType.Transport)
            {
                if (srcUnits == QuantityType.Steps && dstUnits == QuantityType.Volume)
                {
                    val *= (vc / column);
                    return val;
                }
                else if (srcUnits == QuantityType.Volume && dstUnits == QuantityType.Steps)
                {
                    val *= (column / vc);
                    return val;
                }

                // x -> time
                switch (srcUnits)
                {
                    case QuantityType.Steps:
                        val *= (tm / column);
                        break;
                    case QuantityType.Volume:
                        val /= flow;
                        break;
                }
                // time -> y
                switch (dstUnits)
                {
                    case QuantityType.Steps:
                        val /= (tm / column);
                        break;
                    case QuantityType.Volume:
                        val *= flow;
                        break;
                    case QuantityType.Column:
                        val /= tm;
                        break;
                    case QuantityType.ReS:
                        val = Equations.calcK(kDefinition, vu, vl, fu, fl, val * flow);
                        break;
                }
            }
            else
            {
                // x -> vol
                switch (srcUnits)
                {
                    case QuantityType.Steps:
                        val *= (vc / column);
                        break;
                    case QuantityType.Time:
                        val *= flow;
                        break;
                }
                // vol -> y
                switch (dstUnits)
                {
                    case QuantityType.Steps:
                        val /= (vc / column);
                        break;
                    case QuantityType.Time:
                        val /= flow;
                        break;
                    case QuantityType.Column:
                        val /= vc;
                        break;
                    case QuantityType.ReS:
                        val = Equations.calcK(kDefinition, vu, vl, fu, fl, val);
                        break;
                }
            }
            return val;
        }

    }
}
