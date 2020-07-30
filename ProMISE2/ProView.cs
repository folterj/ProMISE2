using System.Windows;

namespace ProMISE2
{
    interface ProViewInterface
    {
    }

    class ProView : ProViewInterface, PreviewObserver, ModelObserver
    {
        ProModelInterface model;
        ProControllerInterface controller;
        ControlParams controlParams;
        OptionParams optionParams;
        ViewParams viewParams;

        public MainWindow mainWindow;

        public ProView(ProControllerInterface controller,
                        ControlParams controlParams,
                        OptionParams optionParams,
                        ViewParams viewParams,
                        ProModelInterface model)
        {
            this.controller = controller;
            this.controlParams = controlParams;
            this.optionParams = optionParams;
            this.viewParams = viewParams;
            this.model = model;
            model.registerPreviewObserver(this);
            model.registerModelObserver(this);
        }

		public void updateTitle(string customTitle)
		{
			mainWindow.updateTitle(customTitle);
		}

        public void updateControlParams(ControlParams controlParams)
        {
            this.controlParams = controlParams;
            if (mainWindow != null)
            {
                mainWindow.updateParams(controlParams);
            }
        }

        public void create()
        {
            mainWindow = new MainWindow(controller, controlParams);
            mainWindow.Show();
            updateMenus(viewParams);
        }

        public void previewUpdate(OutParams outParams)
        {
            mainWindow.updatePreview(outParams);
            // also update view using controlparams
            controlParams.updateFromOutParams(outParams);
        }

        public void modelUpdate(OutParams outParams)
        {
            mainWindow.updateModel(outParams);
        }

		public void setTab(ViewType viewType)
		{
			mainWindow.setTab((int)viewType);
		}

		public OutParams getChromOutParams()
		{
			return mainWindow.getChromOutParams();
		}

		public VisOutSet getChromVisOutSet()
		{
			return mainWindow.getChromVisOutSet();
		}

		public void resetChrom()
		{
			mainWindow.resetChrom();
		}

		public int getChromNtime()
		{
			return mainWindow.getChromNtime();
		}

		public void setChromTimei(int timei)
		{
			mainWindow.setChromTimei(timei);
		}
		
		public void clearStatus()
        {
            mainWindow.setStatus("");
        }

        public void clearProgress()
        {
            mainWindow.clearProgress();
        }

        public void updateProgress(float progress)
        {
            if (progress < 0)
            {
                progress = 0;
            }
            if (progress > 1)
            {
                progress = 1;
            }
            mainWindow.setProgress(progress);
        }

        public void updateMenus(ViewParams viewParams)
        {
			bool enabled = (viewParams.viewType == ViewType.Out || viewParams.viewType == ViewType.Time);

			if (viewParams.altMode)
			{
				mainWindow.exportImagesMenuItem.Visibility = Visibility.Visible;
			}
			else
			{
				mainWindow.exportImagesMenuItem.Visibility = Visibility.Collapsed;
			}

			mainWindow.exportDataMenuItem.IsEnabled = enabled;
			mainWindow.exportImageMenuItem.IsEnabled = enabled;
			mainWindow.exportImagesMenuItem.IsEnabled = (viewParams.viewType == ViewType.Time);
			mainWindow.reportMenuItem.IsEnabled = enabled;
			mainWindow.printPreviewMenuItem.IsEnabled = enabled;
			mainWindow.printMenuItem.IsEnabled = enabled;

			//mainWindow.viewMenuItem.IsEnabled = enabled;
			mainWindow.phaseMenuItem.IsEnabled = enabled;
			mainWindow.peakMenuItem.IsEnabled = enabled;
			mainWindow.xScaleMenuItem.IsEnabled = enabled;
			mainWindow.yScaleMenuItem.IsEnabled = enabled;

            mainWindow.phaseUpperLowerTimeMenuItem.IsChecked = (viewParams.phaseDisplay == PhaseDisplayType.UpperLowerTime);
            mainWindow.phaseUpperLowerMenuItem.IsChecked = (viewParams.phaseDisplay == PhaseDisplayType.UpperLower);
            mainWindow.phaseAllMenuItem.IsChecked = (viewParams.phaseDisplay == PhaseDisplayType.All);
            mainWindow.phaseUpperMenuItem.IsChecked = (viewParams.phaseDisplay == PhaseDisplayType.Upper);
            mainWindow.phaseLowerMenuItem.IsChecked = (viewParams.phaseDisplay == PhaseDisplayType.Lower);

            mainWindow.peaksMenuItem.IsChecked = (viewParams.peaksDisplay == PeaksDisplayType.Peaks);
            mainWindow.peaksSumMenuItem.IsChecked = (viewParams.peaksDisplay == PeaksDisplayType.PeaksSum);
            mainWindow.sumMenuItem.IsChecked = (viewParams.peaksDisplay == PeaksDisplayType.Sum);
            mainWindow.intTotalsMenuItem.IsChecked = (viewParams.peaksDisplay == PeaksDisplayType.IntTotals);

            mainWindow.probUnitsMenuItem.IsChecked = viewParams.showProbUnits;

            mainWindow.xScaleStepsMenuItem.IsChecked = (viewParams.viewUnits == QuantityType.Steps);
            mainWindow.xScaleVolumeMenuItem.IsChecked = (viewParams.viewUnits == QuantityType.Volume);
            mainWindow.xScaleTimeMenuItem.IsChecked = (viewParams.viewUnits == QuantityType.Time);
            mainWindow.xScaleNormalisedMenuItem.IsChecked = (viewParams.viewUnits == QuantityType.Column);
            mainWindow.xScaleResMenuItem.IsChecked = (viewParams.viewUnits == QuantityType.ReS);

            mainWindow.syncScalesMenuItem.IsChecked = viewParams.syncScales;

            mainWindow.yScaleAutomaticMenuItem.IsChecked = (viewParams.yScale == YScaleType.Automatic);
            mainWindow.yScaleNormalisedMenuItem.IsChecked = (viewParams.yScale == YScaleType.Normalised);
            mainWindow.yScaleAbsoluteMenuItem.IsChecked = (viewParams.yScale == YScaleType.Absolute);
            mainWindow.yScaleLogarithmicMenuItem.IsChecked = (viewParams.yScale == YScaleType.Logarithmic);

			mainWindow.exponentsMenuItem.IsChecked = (viewParams.exponentType == ExponentType.Exponents);
			mainWindow.prefixesMenuItem.IsChecked = (viewParams.exponentType == ExponentType.Prefixes);
        }

    }
}
