using System;
using System.Collections;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, TimeObserver
	{
		ProControllerInterface controller;

		SetupView setupView;
		ChromView outView;
		ChromView timeView;
		PeakInfo peakinfo;
		PeaksInfo peaksinfo;

		bool tabChanging = false;

        Stopwatch sw = new Stopwatch();

		public MainWindow(ProControllerInterface controller, ControlParams controlParams)
		{
			this.controller = controller;
			InitializeComponent();

            // static for now; make change dynamically

			setupView = new SetupView(controlParams);
			setupTab.Content = setupView;

			outView = new ChromView(Transparency.Outline, true);
			outView.CompSelected += new SelectCompEventHandler(chromview_CompSelected);
			outView.CompsSelected += new SelectCompsEventHandler(chromview_CompsSelected);
			outView.TimeMode = false;
			outTab.Content = outView;

			timeView = new ChromView(Transparency.Outline, true);
			timeView.registerTimeObserver(this);
			timeView.registerTimeObserver(controller);
			timeView.CompSelected += new SelectCompEventHandler(chromview_CompSelected);
			timeView.CompsSelected += new SelectCompsEventHandler(chromview_CompsSelected);
			timeView.TimeMode = true;
			timeTab.Content = timeView;

			peakinfo = new PeakInfo();
			peaksinfo = new PeaksInfo();
		}

		public void updateTitle(string customTitle)
		{
			Title = customTitle;
		}

		public void updateParams(ControlParams controlParams)
		{
            if (setupTab.IsSelected)
            {
                setupView.updateParams(controlParams);
            }
		}

        public void updatePreview(OutParams outParams)
		{		
			setupView.updatePreview(outParams);
		}

		public void updateModel(OutParams outParams)
		{
            // Called from model thread; need to invoke to update UI controls
			Dispatcher.Invoke((Action)(() =>
			{
                if (outTab.IsSelected)
                {
                    outView.update(outParams);
                }
                else if (timeTab.IsSelected)
                {
                    timeView.update(outParams);
                }
			}));
		}

		private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// make sure e.Handled is set to true in all child control comboboxes and datagrids; otherwise this event is incorrectly triggered
			e.Handled = true;

			if (!tabChanging)
			{
				tabChanging = true;
				if (controller.requestTabChange((ViewType)tabControl.SelectedIndex))
				{
					// tab change ok
					peakinfo.Hide();
					peaksinfo.Hide();
				}
				else
				{
					// revert tab change
					IList unselectedTabs = e.RemovedItems;
					if (unselectedTabs != null)
					{
						if (unselectedTabs.Count > 0)
						{
							tabControl.SelectedItem = unselectedTabs[0];
						}
					}
				}
				tabChanging = false;
			}
		}

		public void resetChrom()
		{
			outView.reset();
			timeView.reset();
		}

		public int getChromNtime()
		{
			return timeView.getChromNtime();
		}

		public void setChromTimei(int timei)
		{
			timeView.setChromTimei(timei);
		}

		public void setTab(int i)
		{
			tabControl.SelectedIndex = i;
		}

		public OutParams getChromOutParams()
		{
			if (outTab.IsSelected)
			{
				return outView.getOutParams();
			}
			else if (timeTab.IsSelected)
			{
				return timeView.getOutParams();
			}
			return null;
		}

		public VisOutSet getChromVisOutSet()
		{
			if (outTab.IsSelected)
			{
				return outView.getVisOutSet();
			}
			else if (timeTab.IsSelected)
			{
				return timeView.getVisOutSet();
			}
			return null;
		}

		public void timeChanged(int timei, string timeLabel)
		{
			setStatus(timeLabel);
		}

        public void setStatus(string status)
        {
            statusLabel.Content = status;
        }

        public void setProgress(float progress, string customText = "")
        {
            // Called from model thread; need to invoke to update UI controls
            Dispatcher.Invoke((Action)(() =>
            {
                string s = "";
                double estimate = 0;
                TimeSpan elapsedTimespan;
                TimeSpan estimateTimespan;

                if (!sw.IsRunning)
                {
                    progressBar.Visibility = Visibility.Visible;
                    sw.Reset();
                    sw.Start();
                }

                elapsedTimespan = sw.Elapsed;
                if (progress > 0)
                {
                    estimate = elapsedTimespan.TotalSeconds * (1 / progress - 1);
                }
                estimateTimespan = TimeSpan.FromSeconds(estimate);

                s = string.Format("{0:P1}  Elapsed: {1:hh\\:mm\\:ss}  Remaining: {2:hh\\:mm\\:ss}", progress, elapsedTimespan, estimateTimespan);
                progressBar.Value = progress;
                if (customText != "")
                {
                    s = customText + " " + s;
                }
                statusLabel.Content = s;
            }));
        }

        public void clearProgress()
        {
            // Called from model thread; need to invoke to update UI controls
            Dispatcher.Invoke((Action)(() =>
            {
                sw.Stop();
                progressBar.Value = 0;
                statusLabel.Content = "";
                progressBar.Visibility = Visibility.Collapsed;
            }));
        }

		private void chromview_CompSelected(object sender, OutComp comp)
		{
			if (comp != null)
			{
				peakinfo.updateParams(comp);
				peakinfo.Show();
				peakinfo.Activate();
			}
			else
			{
				peakinfo.Hide();
			}
		}

		private void chromview_CompsSelected(object sender, OutComp comp1, OutComp comp2)
		{
			if (comp1 != null && comp2 != null)
			{
				peaksinfo.updateParams(comp1, comp2);
				peaksinfo.Show();
				peaksinfo.Activate();
			}
			else
			{
				peaksinfo.Hide();
			}
		}

		private void exportDataMenuItem_Click(object sender, RoutedEventArgs e)
		{
			controller.exportData();
		}

		private void exportImageMenuItem_Click(object sender, RoutedEventArgs e)
		{
			controller.exportImage();
		}

		private void exportImagesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			controller.exportImages();
		}

		private void reportMenuItem_Click(object sender, RoutedEventArgs e)
		{
			controller.showReport();
		}
	
		private void phaseUpperLowerTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPhaseDisplay(PhaseDisplayType.UpperLowerTime);
        }

        private void phaseUpperLowerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPhaseDisplay(PhaseDisplayType.UpperLower);
        }

        private void phaseAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPhaseDisplay(PhaseDisplayType.All);
        }

        private void phaseUpperMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPhaseDisplay(PhaseDisplayType.Upper);
        }

        private void phaseLowerMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPhaseDisplay(PhaseDisplayType.Lower);
        }

        private void peaksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPeaksDisplay(PeaksDisplayType.Peaks);
        }

        private void peaksSumMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPeaksDisplay(PeaksDisplayType.PeaksSum);
        }

        private void sumMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPeaksDisplay(PeaksDisplayType.Sum);
        }

        private void intTotalsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setPeaksDisplay(PeaksDisplayType.IntTotals);
        }

        private void probUnitsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.toggleProbUnits();
        }

        private void xScaleStepsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setXScale(QuantityType.Steps);
        }

        private void xScaleVolumeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setXScale(QuantityType.Volume);
        }

        private void xScaleTimeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setXScale(QuantityType.Time);
        }

        private void xScaleNormalisedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setXScale(QuantityType.Column);
        }

        private void xScaleResMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setXScale(QuantityType.ReS);
        }

        private void syncScalesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.toggleSyncScale();
        }

        private void yScaleAutomaticMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setYScale(YScaleType.Automatic);
        }

        private void yScaleNormalisedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setYScale(YScaleType.Normalised);
        }

        private void yScaleAbsoluteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setYScale(YScaleType.Absolute);
        }

        private void yScaleLogarithmicMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.setYScale(YScaleType.Logarithmic);
        }

		private void exponentsMenuItem_Click(object sender, RoutedEventArgs e)
		{
			controller.setExponents(ExponentType.Exponents);
		}

		private void prefixesMenuItem_Click(object sender, RoutedEventArgs e)
		{
			controller.setExponents(ExponentType.Prefixes);
		}
	
		private void optionsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.showOptions();
        }

        private void checkUpdatesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.checkUpdates(true);
        }

        private void statsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.showStats();
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            controller.showAbout();
        }

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			controller.exit();
		}

	}
}
