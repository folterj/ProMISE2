using System;
using System.Collections.Generic;

namespace ProMISE2
{
    [Serializable]
    public class Comp
    {
        public string label;
        public float k;
        public float m;
        public float concentration;
        public bool elute;

        public Comp()
        {
            label = "";
            k = 0;
            m = 1;
            concentration = m;
            elute = true;
        }

        public Comp(Comp comp)
        {
            label = comp.label;
            k = comp.k;
            m = comp.m;
            concentration = comp.concentration;
            elute = comp.elute;
        }
    }

    [Serializable]
    public class InParams
    {
        public List<Comp> comps;

        public ProfileType profile;
        public ModelType model;
        public RunModeType runMode;
        public EEModeType eeMode;

        public float vc;
        public float uf, lf;

        public float fu, fl;

        public int column;
        public int probUnits;
		public int densitySteps;

        public float mixSpeed;
        public float efficiency;
        public float ka;

        public bool doMaxIt;
        public float maxIt;

        public bool autoFilter;

        public QuantityType vdeadUnits;
        public bool vdeadInEnabled, vdeadOutEnabled, vdeadInjectEnabled;
        public float vdeadIn, vdeadOut, vdeadInject;

        public InjectModeType injectMode;
        public PhaseType injectPhase;
        public float injectPos;
		public float injectVolume;
        public QuantityType injectFeedUnits;
        public float injectFeed;

        public IntModeType intMode;
        public PhaseType intStartPhase;
        public float intUpSwitch, intLpSwitch;
        public int intUpComp, intLpComp;
        public float intMaxIt;
        public bool intFinalElute;

        public bool ptransMode;
        public float ptransu, ptransl;

        public KdefType kDefinition;

        public QuantityType viewUnits;

        public VolUnitsType volUnits;
        public MassUnitsType massUnits;
        public TimeUnitsType timeUnits;

        public bool advancedMode;


        public InParams()
        {
            reset();
        }

        public void reset()
        {
            comps = new List<Comp>();

            profile = ProfileType.CCC;
            model = ModelType.Probabilistic;
            runMode = RunModeType.UpperPhase;
            eeMode = EEModeType.None;

            vc = 100;
            uf = 0.5f;
            lf = 1 - uf;

            fu = 1;
            fl = 0;

            column = 100;
            probUnits = 10000;
			densitySteps = 200;

            mixSpeed = 1;
            efficiency = 1;
            ka = 0.01f;

            doMaxIt = false;
            maxIt = 10;

            autoFilter = true;

            vdeadUnits = QuantityType.Volume;
            vdeadInEnabled = false;
            vdeadOutEnabled = false;
            vdeadInjectEnabled = false;
            vdeadIn = 0;
            vdeadOut = 0;
            vdeadInject = 0;

            injectMode = InjectModeType.Instant;
            injectPhase = PhaseType.Upper;
            injectPos = 0;
			injectVolume = 1;
            injectFeedUnits = QuantityType.Volume;
            injectFeed = 0;

            intMode = IntModeType.Time;
            intStartPhase = PhaseType.Upper;
            intUpSwitch = 0;
            intLpSwitch = 0;
            intUpComp = -1;
            intLpComp = -1;
            intMaxIt = 10;
            intFinalElute = true;

            ptransMode = false;
            ptransu = 1;
            ptransl = 1;

            kDefinition = KdefType.U_L;

            viewUnits = QuantityType.Volume;

            volUnits = VolUnitsType.ml;
            massUnits = MassUnitsType.mg;
            timeUnits = TimeUnitsType.min;

            advancedMode = false;
        }

    }
}
