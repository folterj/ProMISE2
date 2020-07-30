using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;

namespace ProMISE2
{
	public interface ControlCompObserver
	{
		void updateControlComp();
	}

	public class ControlComp : OutComp, INotifyPropertyChanged
	{
        [XmlIgnore]
		ArrayList observers;

		[XmlIgnore]
		public InParams inParams;

		[XmlIgnore]
		bool defaultLabel = true;

		[XmlIgnore]
		public String Label
		{
			get { return label; }
			set { defaultLabel = false; label = value; update(); }
		}

		[XmlIgnore]
		public float K
		{
			get { return k; }
			set { k = value; NotifyPropertyChanged("K"); update(); }
		}

		[XmlIgnore]
		public float M
		{
			get { return m; }
			set
			{
				m = value;
				if (inParams != null)
				{
					if (inParams.injectVolume != 0)
					{
						concentration = m / inParams.injectVolume;
					}
					else
					{
						concentration = m;
					}
				}
				NotifyPropertyChanged("M");
				NotifyPropertyChanged("Concentration");
				update();
			}
		}

		[XmlIgnore]
		public float Concentration
		{
			get { return concentration; }
			set
			{
				concentration = value;
				if (inParams != null)
				{
					if (inParams.injectVolume != 0)
					{
						m = concentration * inParams.injectVolume;
					}
					else
					{
						m = concentration;
					}
				}
				NotifyPropertyChanged("Concentration");
				NotifyPropertyChanged("M");
				update();
			}
		}

		[XmlIgnore]
		public bool Elute
		{
			get { return elute; }
			set { elute = value; NotifyPropertyChanged("Elute"); update(); }
		}

		[XmlIgnore]
		public string Phase
		{
			get
			{
				if (outCol)
				{
					return phase.ToString();
				}
				return "-";
			}
		}

		[XmlIgnore]
		public float Retention
		{
			get { return retention; }
		}

		[XmlIgnore]
		public float Average
		{
			get { return average; }
		}

		[XmlIgnore]
		public float Sigma
		{
			get { return sigma; }
		}

		[XmlIgnore]
		public float Width
		{
			get { return width; }
		}

		[XmlIgnore]
		public float Height
		{
			get { return height; }
		}

		[XmlIgnore]
		public float Purity
		{
			get { return purity; }
		}

		[XmlIgnore]
		public float Recovery
		{
			get { return recovery; }
		}

		[XmlIgnore]
		public string Units
		{
			get
			{
				switch (units)
				{
					case QuantityType.Volume: return string.Format("[{0}]", volUnits);
					case QuantityType.Time: return string.Format("[{0}]", timeUnits);
					default: return "";
				}
			}
		}

		[XmlIgnore]
		public string MassUnits
		{
			get { return string.Format("[{0}]", massUnits); }
		}

		[XmlIgnore]
		public string ConcentrationUnits
		{
			get { return string.Format("[{0}/{1}]", massUnits, volUnits); }
		}

        public ControlComp()
            : base()
        {
            init();
        }

        public ControlComp(Comp comp)
            : base(comp)
        {
            init();
        }

        public ControlComp(OutComp outComp)
            : base(outComp)
        {
            updateFromOutComp(outComp);
            init();
        }

        void init()
        {
            observers = new ArrayList();
        }

        public void updateFromOutComp(OutComp outComp)
        {
            retention = outComp.retention;
            sigma = outComp.sigma;
            width = outComp.width;
            height = outComp.height;
            notify();
        }

		public override string ToString()
		{
			return label;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string sProp)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(sProp));
			}
		}

		public void update()
		{
			if (label == "" || defaultLabel)
			{
				label = String.Format("K={0}", k);
				NotifyPropertyChanged("label");
			}
			updateObservers();
			notify();
		}

		public void update(InParams inParams)
		{
			// called from ControlParams
			this.inParams = inParams;
			if (inParams.injectVolume != 0)
			{
				concentration = m / inParams.injectVolume;
			}
			else
			{
				concentration = m;
			}
			NotifyPropertyChanged("concentration");
		}

		public void notify()
		{
			// notify UI of updated params
			NotifyPropertyChanged("retention");
			NotifyPropertyChanged("sigma");
			NotifyPropertyChanged("width");
			NotifyPropertyChanged("height");
		}

		public void registerObserver(ControlCompObserver observer)
		{
			observers.Add(observer);
		}

		public void unregisterObserver(ControlCompObserver observer)
		{
			observers.Remove(observer);
		}

		void updateObservers()
		{
			for (int i = 0; i < observers.Count; i++)
			{
				((ControlCompObserver)observers[i]).updateControlComp();
			}
		}
	}


	public interface ControlParamsObserver
	{
		void updateControlParams();
	}


	public class ControlParams : InParamsExt, ControlCompObserver
	{
        [XmlIgnore]
        ArrayList observers;

        [XmlIgnore]
		public List<ControlComp> controlcomps { get; set; }

		[XmlIgnore]
		public bool AdvancedMode
		{
			get { return advancedMode; }
            set
            {
				advancedMode = value;
				NotifyPropertyChanged("AdvancedMode");
				updateAdvancedMode();
                updateObservers();
            }
		}

		[XmlIgnore]
		public int Profile
		{
			get { return (int)profile; }
            set
            {
                profile = (ProfileType)value;
                NotifyPropertyChanged("Profile");
                updateProfile();
                updateObservers();
            }
		}

        [XmlIgnore]
        public ModelType Model
        {
            get { return model; }
            set
            {
                model = value;
                NotifyPropertyChanged("Model");
                updateModel();
                updateObservers();
            }
        }

		[XmlIgnore]
		public int RunMode
		{
			get { return (int)runMode; }
			set
			{
				runMode = (RunModeType)value;
				NotifyPropertyChanged("RunMode");
				NotifyPropertyChanged("IntEnabled");
				updateRunMode();
				updateObservers();
			}
		}

        [XmlIgnore]
        public int EEMode
        {
            get { return (int)eeMode; }
            set
            {
                eeMode = (EEModeType)value;
                NotifyPropertyChanged("EEMode");
                updateObservers();
            }
        }

		[XmlIgnore]
		public float Vc
		{
			get { return vc; }
			set
			{
				if (value <= 0)
					throw new Exception("must be greater than zero");
				vc = value;
				NotifyPropertyChanged("Vc");
				NotifyPropertyChanged("vu");
				NotifyPropertyChanged("vl");
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public int Column
		{
			get { return column; }
			set
			{
				if (value <= 0)
					throw new Exception("must be greater than zero");
				column = value;
				NotifyPropertyChanged("Column");
				update();
				updateObservers();
			}
		}

        [XmlIgnore]
        public int ProbUnits
        {
            get { return probUnits; }
            set
            {
                probUnits = value;
                NotifyPropertyChanged("ProbUnits");
                updateObservers();
            }
        }

		[XmlIgnore]
		public int DensitySteps
		{
			get { return densitySteps; }
			set
			{
				densitySteps = value;
				NotifyPropertyChanged("DensitySteps");
				updateObservers();
			}
		}

		[XmlIgnore]
		public float Uf
		{
			get { return uf; }
			set { uf = value; NotifyPropertyChanged("Uf"); updateFromUf(); }
		}

		[XmlIgnore]
		public float Lf
		{
			get { return lf; }
			set { lf = value; NotifyPropertyChanged("Lf"); updateFromLf(); }
		}

		[XmlIgnore]
		public float MixSpeed
		{
			get { return mixSpeed; }
			set
			{
				if (value <= 0)
					throw new Exception("must be greater than zero");
				mixSpeed = value;
				NotifyPropertyChanged("MixSpeed");
				updateObservers();
			}
		}

		[XmlIgnore]
		public float Efficiency
		{
			get { return efficiency; }
			set
			{
				if (value < 0 || value > 1)
					throw new Exception("invalid value");
				efficiency = value;
				NotifyPropertyChanged("Efficiency");
				updateObservers();
			}
		}

		[XmlIgnore]
		public float KA
		{
			get { return ka; }
			set
			{
				if (value <= 0 || value > 1)
					throw new Exception("invalid value");
				ka = value;
				NotifyPropertyChanged("KA");
				updateObservers();
			}
		}

		[XmlIgnore]
		public bool DoMaxIt
		{
			get { return doMaxIt; }
			set { doMaxIt = value; NotifyPropertyChanged("DoMaxit"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public float MaxIt
		{
			get { return maxIt; }
			set { maxIt = value; NotifyPropertyChanged("Maxit"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public float VdeadIn
		{
			get { return vdeadIn; }
			set
			{
				if (value < 0)
					throw new Exception("must be positive");
				vdeadIn = value;
				NotifyPropertyChanged("VdeadIn");
				NotifyPropertyChanged("VdeadStart");
				NotifyPropertyChanged("VdeadEnd");
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public float VdeadOut
		{
			get { return vdeadOut; }
			set
			{
				if (value < 0)
					throw new Exception("must be positive");
				vdeadOut = value;
				NotifyPropertyChanged("VdeadOut");
				NotifyPropertyChanged("VdeadStart");
				NotifyPropertyChanged("VdeadEnd");
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public float VdeadInject
		{
			get { return vdeadInject; }
			set
			{
				if (value < 0)
					throw new Exception("must be positive");
				vdeadInject = value;
				NotifyPropertyChanged("VdeadInject");
				NotifyPropertyChanged("VdeadInjectStart");
				NotifyPropertyChanged("VdeadInjectEnd");
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public bool VdeadInEnabled
		{
			get { return vdeadInEnabled; }
			set { vdeadInEnabled = value; NotifyPropertyChanged("VdeadInEnabled"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public bool VdeadOutEnabled
		{
			get { return vdeadOutEnabled; }
			set { vdeadOutEnabled = value; NotifyPropertyChanged("VdeadOutEnabled"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public bool VdeadInjectEnabled
		{
			get { return vdeadInjectEnabled; }
			set { vdeadInjectEnabled = value; NotifyPropertyChanged("VdeadInjectEnabled"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public float Fu
		{
			get { return fu; }
			set { fu = value; NotifyPropertyChanged("fu"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public float Fl
		{
			get { return fl; }
			set { fl = value; NotifyPropertyChanged("fl"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public bool PtransMode
		{
			get { return ptransMode; }
			set { ptransMode = value; NotifyPropertyChanged("PtransMode"); updateObservers(); }
		}

		[XmlIgnore]
		public float Ptransu
		{
			get { return ptransu; }
			set { ptransu = value; NotifyPropertyChanged("Ptransu"); updateObservers(); }
		}

		[XmlIgnore]
		public float Ptransl
		{
			get { return ptransl; }
			set { ptransl = value; NotifyPropertyChanged("Ptransl"); updateObservers(); }
		}

		[XmlIgnore]
		public bool PtransModeEnabled
		{
			get { return (advancedMode && model == ModelType.CCD); }
		}

		[XmlIgnore]
		public InjectModeType InjectMode
		{
			get { return injectMode; }
			set
			{
				injectMode = value;
				NotifyPropertyChanged("InjectMode");
				NotifyPropertyChanged("InjectFeedEnabled");
				updateInjectMode();
				updateCompsCon();
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public PhaseType InjectPhase
		{
			get { return injectPhase; }
			set { injectPhase = value; NotifyPropertyChanged("InjectPhase"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public float InjectPos
		{
			get { return injectPos; }
			set
			{
				if (value < 0 || value > 1)
					throw new Exception("invalid value");
				injectPos = value;
				NotifyPropertyChanged("InjectPos");
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public float InjectVolume
		{
			get { return injectVolume; }
			set
			{
				if (value <= 0)
					throw new Exception("must larger than zero");
				injectVolume = value;
				NotifyPropertyChanged("InjectVolume");
				updateCompsCon();
				update();
				updateObservers();
			}
		}

        [XmlIgnore]
        public QuantityType InjectFeedUnits
        {
            get { return injectFeedUnits; }
            set
            {
                injectFeedUnits = value;
                NotifyPropertyChanged("InjectFeedUnits");
                NotifyPropertyChanged("ShowFeedUnits");
                update();
                updateObservers();
            }
        }

		[XmlIgnore]
		public float InjectFeed
		{
			get { return injectFeed; }
			set
			{
				if (value < 0)
					throw new Exception("must be positive");
				injectFeed = value;
				NotifyPropertyChanged("InjectFeed");
				update();
				updateObservers();
			}
		}

		[XmlIgnore]
		public bool InjectFeedEnabled
		{
            get { return (injectMode == InjectModeType.Batch); }
		}

		[XmlIgnore]
		public int KDefinition
		{
			get { return (int)kDefinition; }
			set { kDefinition = (KdefType)value; NotifyPropertyChanged("KDefinition"); update(); updateObservers(); }
		}

		[XmlIgnore]
		public IntModeType IntMode
		{
			get { return intMode; }
			set
			{
				intMode = value;
				NotifyPropertyChanged("Intmode");
				NotifyPropertyChanged("IntEnabled");
				NotifyPropertyChanged("IntCompVisibility");
				NotifyPropertyChanged("IntSwitchVisibility");
                NotifyPropertyChanged("ShowIntUnits");
				updateIntMode();
				updateObservers();
			}
		}

		[XmlIgnore]
		public PhaseType IntStartPhase
		{
			get { return intStartPhase; }
			set { intStartPhase = value; NotifyPropertyChanged("IntStartPhase"); updateObservers(); }
		}

		[XmlIgnore]
		public float IntUpSwitch
		{
			get { return intUpSwitch; }
			set { intUpSwitch = value; NotifyPropertyChanged("IntUpSwitch"); updateObservers(); }
		}

		[XmlIgnore]
		public float IntLpSwitch
		{
			get { return intLpSwitch; }
			set { intLpSwitch = value; NotifyPropertyChanged("IntLpSwitch"); updateObservers(); }
		}

		[XmlIgnore]
		public int IntUpComp
		{
			get { return intUpComp; }
			set { intUpComp = value; NotifyPropertyChanged("IntUpComp"); updateObservers(); }
		}

		[XmlIgnore]
		public int IntLpComp
		{
			get { return intLpComp; }
			set { intLpComp = value; NotifyPropertyChanged("IntLpComp"); updateObservers(); }
		}

		[XmlIgnore]
		public float IntMaxIt
		{
			get { return intMaxIt; }
			set { intMaxIt = value; NotifyPropertyChanged("IntMaxIt"); updateObservers(); }
		}

		[XmlIgnore]
		public bool IntFinalElute
		{
			get { return intFinalElute; }
			set { intFinalElute = value; NotifyPropertyChanged("IntFinalElute"); updateObservers(); }
		}

		[XmlIgnore]
		public bool IntEnabled
		{
			get { return runMode == RunModeType.Intermittent; }
		}

		[XmlIgnore]
		public Visibility IntSwitchVisibility
		{
			get
			{
				if (intMode != IntModeType.Component)
				{
					return Visibility.Visible;
				}
				else
				{
					return Visibility.Hidden;
				}
			}
		}

		[XmlIgnore]
		public Visibility IntCompVisibility
		{
			get
			{
				if (intMode == IntModeType.Component)
				{
					return Visibility.Visible;
				}
				else
				{
					return Visibility.Hidden;
				}
			}
		}

        [XmlIgnore]
        public int VolUnits
        {
            get { return (int)volUnits; }
            set { volUnits = (VolUnitsType)value; NotifyPropertyChanged("VolUnits"); updateObservers(); }
        }

        [XmlIgnore]
        public int MassUnits
        {
            get { return (int)massUnits; }
            set { massUnits = (MassUnitsType)value; NotifyPropertyChanged("MassUnits"); updateObservers(); }
        }

        [XmlIgnore]
        public int TimeUnits
        {
            get { return (int)timeUnits; }
            set { timeUnits = (TimeUnitsType)value; NotifyPropertyChanged("TimeUnits"); updateObservers(); }
        }

        [XmlIgnore]
        public string ShowVolUnits
        {
            get { return string.Format("[{0}]", volUnits); }
        }

        [XmlIgnore]
        public string ShowMassUnits
        {
            get { return string.Format("[{0}]", massUnits); }
        }

        [XmlIgnore]
        public string ShowTimeUnits
        {
            get { return string.Format("[{0}]", timeUnits); }
        }

		[XmlIgnore]
		public string ShowMixSpeedUnits
		{
			get { return string.Format("[1/{0}]", timeUnits); }
		}

		[XmlIgnore]
		public string ShowFlowUnits
		{
			get { return string.Format("[{0}/{1}]", volUnits, timeUnits); }
		}
		
		[XmlIgnore]
        public string ShowFeedUnits
        {
            get
            {
                switch (injectFeedUnits)
                {
                    case QuantityType.Volume: return string.Format("[{0}]", volUnits);
                    case QuantityType.Time: return string.Format("[{0}]", timeUnits);
                    default: return "";
                } 
            }
        }

        [XmlIgnore]
        public string ShowIntUnits
        {
            get
            {
                switch (intMode)
                {
                    case IntModeType.Volume: return string.Format("[{0}]", volUnits);
                    case IntModeType.Time: return string.Format("[{0}]", timeUnits);
                    default: return "";
                } 
            }
        }


		public ControlParams()
			: base()
		{
            observers = new ArrayList();
            controlcomps = new List<ControlComp>();
            updateCompsObervers();
		}

		public bool updateFromUf()
		{
			float newlf = 1 - uf;
			if (newlf != lf)
			{
				lf = newlf;
				NotifyPropertyChanged("lf");
				NotifyPropertyChanged("px");
                NotifyPropertyChanged("vu");
                NotifyPropertyChanged("vl");
                update();
				updateObservers();
				return true;
			}
			return false;
		}

		public bool updateFromLf()
		{
			float newuf = 1 - lf;
			if (newuf != uf)
			{
				uf = newuf;
				NotifyPropertyChanged("uf");
				NotifyPropertyChanged("px");
                NotifyPropertyChanged("vu");
                NotifyPropertyChanged("vl");
                update();
				updateObservers();
				return true;
			}
			return false;
		}

		public void updateCompsObervers()
		{
			for (int i = 0; i < controlcomps.Count; i++)
			{
				controlcomps[i].registerObserver(this);
			}
		}

		public void updateControlComp()
		{
			// called when a comp is updated
			updateCompsCon();
			updateObservers();
		}

		public void updateComps()
		{
			// copy controlcomps to comps
			comps.Clear();
			for (int i = 0; i < controlcomps.Count; i++)
			{
				comps.Add(new Comp(controlcomps[i]));
			}
		}

        public void updateControlComps()
        {
            // copy comps to controlcomps
            controlcomps.Clear();
            for (int i = 0; i < comps.Count; i++)
            {
                controlcomps.Add(new ControlComp(comps[i]));
            }
        }

		public void updateCompsCon()
		{
			foreach (ControlComp comp in controlcomps)
			{
				comp.update(this);
			}
		}

		public void updateFromOutParams(OutParams outParams)
		{
			for (int compi = 0; compi < outParams.outSet.comps.Count; compi++)
			{
                controlcomps[compi].updateFromOutComp(outParams.outSet.comps[compi]);
			}
		}

		public void registerObserver(ControlParamsObserver observer)
		{
			observers.Add(observer);
		}

		public void unregisterObserver(ControlParamsObserver observer)
		{
			observers.Remove(observer);
		}

		public void updateObservers()
		{
			for (int i = 0; i < observers.Count; i++)
			{
				((ControlParamsObserver)observers[i]).updateControlParams();
			}
		}

	}
}
