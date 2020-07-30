namespace ProMISE2
{
	public class ViewParams
	{
        public ViewType viewType = ViewType.Setup;
        public PhaseDisplayType phaseDisplay = PhaseDisplayType.UpperLower;
        public QuantityType viewUnits = QuantityType.Volume;
        public YScaleType yScale = YScaleType.Automatic;
		public ExponentType exponentType = ExponentType.Exponents;
        public PeaksDisplayType peaksDisplay = PeaksDisplayType.PeaksSum;
        public bool showProbUnits = true;
        public bool autoZoom = true;
        public bool syncScales = false;
		public bool altMode = false;

		public ViewParams()
		{
		}

        public ViewParams(ViewParams viewParams)
		{
            viewType = viewParams.viewType;
			phaseDisplay = viewParams.phaseDisplay;
			viewUnits = viewParams.viewUnits;
			yScale = viewParams.yScale;
			exponentType = viewParams.exponentType;
			peaksDisplay = viewParams.peaksDisplay;
			showProbUnits = viewParams.showProbUnits;
			autoZoom = viewParams.autoZoom;
			syncScales = viewParams.syncScales;
			altMode = viewParams.altMode;
		}

		public void init(InParams inParams)
		{
			if (viewType == ViewType.Setup)
			{
				phaseDisplay = PhaseDisplayType.All;
			}
			else if (viewType == ViewType.Time)
			{
				phaseDisplay = PhaseDisplayType.UpperLower;
			}
			else if (inParams.runMode == RunModeType.LowerPhase)
			{
				phaseDisplay = PhaseDisplayType.All;
			}
			else if (inParams.runMode == RunModeType.UpperPhase)
			{
				phaseDisplay = PhaseDisplayType.All;
			}
			else
			{
				phaseDisplay = PhaseDisplayType.UpperLowerTime;
			}

			syncScales = (inParams.runMode == RunModeType.CoCurrent);
			peaksDisplay = PeaksDisplayType.PeaksSum;
			yScale = YScaleType.Automatic;
			exponentType = ExponentType.Exponents;
			viewUnits = inParams.viewUnits;
			showProbUnits = (inParams.model == ModelType.Probabilistic && viewType != ViewType.Setup);
			autoZoom = true;
			update(inParams);
		}

		public void update(InParams inParams)
		{
			if (viewType == ViewType.Setup)
			{
				phaseDisplay = PhaseDisplayType.All;
				peaksDisplay = PeaksDisplayType.Peaks;
				showProbUnits = false;
				autoZoom = true;
			}

			if (phaseDisplay == PhaseDisplayType.Upper && inParams.runMode == RunModeType.LowerPhase && inParams.eeMode == EEModeType.None)
			{
				phaseDisplay = PhaseDisplayType.Lower;
			}

			if (phaseDisplay == PhaseDisplayType.Lower && inParams.runMode == RunModeType.UpperPhase && inParams.eeMode == EEModeType.None)
			{
				phaseDisplay = PhaseDisplayType.Upper;
			}

			if (inParams.model == ModelType.Probabilistic)
			{
                if (viewUnits == QuantityType.Steps)
                {
                    viewUnits = QuantityType.Volume;
                }
			}
			else
			{
				showProbUnits = false;
			}

			if (inParams.runMode == RunModeType.Intermittent)
			{
				if (inParams.viewUnits == QuantityType.Time)
				{
					// Int Time mode (not allowed: Volume,K)
                    if (viewUnits == QuantityType.Volume || viewUnits == QuantityType.ReS)
                    {
                        viewUnits = inParams.viewUnits;
                    }
				}
				else
				{
					// Int Volume/Step mode (not allowed: Time)
                    if (viewUnits == QuantityType.Time)
                    {
                        viewUnits = inParams.viewUnits;
                    }
				}
			}
			else if (peaksDisplay == PeaksDisplayType.IntTotals)
			{
				peaksDisplay = PeaksDisplayType.Peaks;
			}

			if (yScale == YScaleType.Logarithmic)
			{
				exponentType = ExponentType.Exponents;
			}
		}

	}
}
