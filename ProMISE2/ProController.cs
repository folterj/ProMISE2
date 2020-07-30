using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml.Serialization;

namespace ProMISE2
{
	public interface ProControllerInterface : TimeObserver
    {
        bool requestTabChange(ViewType viewType);

		void exportData();
		void exportImage();
		void exportImages();
		void showReport();
        void setPhaseDisplay(PhaseDisplayType phaseDisplay);
        void setPeaksDisplay(PeaksDisplayType peakDisplay);
        void toggleProbUnits();
        void setXScale(QuantityType units);
        void toggleSyncScale();
        void setYScale(YScaleType yScale);
		void setExponents(ExponentType exponentType);
        void showOptions();
        void showStats();
		void checkUpdates(bool showResults = false);
		void showAbout(bool splashMode = false);
		void exit();
    }

	public interface PrintContentInterface
	{
		UIElement[] getPrintPages(double width, double height);
	}

    class ProController : ProControllerInterface, ControlParamsObserver, PrintContentInterface
    {
        ProModelInterface model;
        ProView view;
        ViewType currentView = ViewType.Setup;

        ControlParams controlParams = new ControlParams();
        OptionParams optionParams = new OptionParams();
        ViewParams viewParams = new ViewParams();
		Printing printing = new Printing();

		string settingsFilename = "";

        public bool updateModelReq = true;
        public bool updateOutReq = true;
        public bool updateTimeOutReq = true;

		int selectedTimei = 0;

        public ProController(ProModelInterface model)
        {
            this.model = model;

			loadOptionParams();

			viewParams.init(controlParams);
			viewParams.altMode = (Control.ModifierKeys == Keys.Shift);
			view = new ProView(this, controlParams, optionParams, viewParams, model);
            view.create();

            controlParams.registerObserver(this);

			printing.setPrintContentInterface(this);

            view.mainWindow.CommandBindings.Add(
                new CommandBinding(ApplicationCommands.New, new ExecutedRoutedEventHandler(clearExecute)));

            view.mainWindow.CommandBindings.Add(
                new CommandBinding(ApplicationCommands.Open, new ExecutedRoutedEventHandler(loadExecute)));

            view.mainWindow.CommandBindings.Add(
                new CommandBinding(ApplicationCommands.Save, new ExecutedRoutedEventHandler(saveAsExecute)));

			view.mainWindow.CommandBindings.Add(
				new CommandBinding(ApplicationCommands.PrintPreview, new ExecutedRoutedEventHandler(printPreviewExecute)));

			view.mainWindow.CommandBindings.Add(
				new CommandBinding(ApplicationCommands.Print, new ExecutedRoutedEventHandler(printExecute)));

			view.mainWindow.CommandBindings.Add(
				new CommandBinding(ApplicationCommands.Close, new ExecutedRoutedEventHandler(exitExecute)));

			checkUpdates();
			showAbout(true);
		}

        public void setNewControlParams()
        {
            // new controlparams not registered yet
            controlParams.registerObserver(this);
            controlParams.update();						// validate/update params
            view.updateControlParams(controlParams);	// propagate new controlparams -> view (-> ... etc)
        }

        public void updateControlParams()
        {
            if (currentView == ViewType.Setup)
            {
                controlParams.updateComps();
                viewParams.update(controlParams);
				view.updateMenus(viewParams);
                model.updatePreview(controlParams, viewParams, optionParams);
                updateModelReq = true;
                updateOutReq = true;
                updateTimeOutReq = true;
            }
            view.updateControlParams(controlParams);
        }

        public void updateViewParams()
        {
			viewParams.update(controlParams);
            controlParams.viewUnits = viewParams.viewUnits;
			view.updateMenus(viewParams);

            if (currentView == ViewType.Setup)
            {
                model.updatePreview(controlParams, viewParams, optionParams);
            }
            else if (currentView == ViewType.Out)
            {
                model.updateModel(controlParams, viewParams, optionParams, currentView, updateModelReq, updateOutReq);
                updateModelReq = false;
                updateOutReq = false;
            }
            else if (currentView == ViewType.Time)
            {
                model.updateModel(controlParams, viewParams, optionParams, currentView, updateModelReq, updateTimeOutReq);
                updateModelReq = false;
                updateTimeOutReq = false;
            }
        }

		void loadOptionParams()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(OptionParams));
			try
			{
				optionParams = (OptionParams)serializer.Deserialize(new StreamReader(optionParams.filePath));
			}
			catch (Exception)
			{
			}
		}

		public bool requestTabChange(ViewType tabView)
        {
			string message = "";

			if (currentView == ViewType.Setup && tabView != ViewType.Setup)
			{
				ParamType paramType = controlParams.validate(optionParams);
				if (paramType != ParamType.None)
				{
					// validation error
					switch (paramType)
					{
						case ParamType.Column: message = "Invalid input parameter in column properties"; break;
						case ParamType.Flow: message = "Invalid input parameter in flow properties"; break;
						case ParamType.Inject: message = "Invalid input parameter in injection properties"; break;
						case ParamType.Advanced: message = "Invalid input parameter in advanced properties"; break;
						case ParamType.Components: message = "Invalid component(s)"; break;
						case ParamType.Stability: message = "Instability in parameters for transport model"; break;
					}
					System.Windows.MessageBox.Show(message, "Invalid input parameter");

					return false;
				}
			}

			if (currentView != ViewType.Setup && tabView == ViewType.Setup)
			{
				if (model.abortModel())
				{
					updateModelReq = true;
				}
			}

			if (currentView != ViewType.Setup && tabView != ViewType.Setup)
			{
				// switch between chrom and time: not allowed while model is running
				if (model.isRunning())
				{
					return false;
				}
			}

			tabUpdate(tabView);

			return true;
        }

		void tabUpdate(ViewType tabView)
		{
			currentView = tabView;
			viewParams.viewType = currentView;
			viewParams.init(controlParams);
			view.clearStatus();
			view.resetChrom();
			updateViewParams();
		}

		void updateTitle()
		{
			string customTitle = "ProMISE2";

			if (settingsFilename != "")
			{
				customTitle += string.Format(" [{0}]", settingsFilename);
			}
			view.updateTitle(customTitle);
		}

		public void timeChanged(int timei, string timeLabel)
		{
			selectedTimei = timei;
		}

        public void clearExecute(object sender, ExecutedRoutedEventArgs e)
        {
            controlParams = new ControlParams();
			settingsFilename = "";
            // new controlparams not registered yet
            setNewControlParams();
			updateTitle();
        }

        public void loadExecute(object sender, ExecutedRoutedEventArgs e)
        {
            FileDialog dialog = new OpenFileDialog();
            dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
				settingsFilename = dialog.FileName;

				view.setTab(ViewType.Setup);

				controlParams.unregisterObserver(this);

                XmlSerializer serializer = new XmlSerializer(typeof(ControlParams));
                try
                {
					controlParams = (ControlParams)serializer.Deserialize(new StreamReader(settingsFilename));
                }
                catch (InvalidOperationException)
                {
                    // read/parse error: controlparams appears unaffected
                }
                controlParams.updateControlComps();
                // new controlparams not registered yet
                setNewControlParams();

				updateTitle();
            }
        }

        public void saveAsExecute(object sender, ExecutedRoutedEventArgs e)
        {
            FileDialog dialog = new SaveFileDialog();
            dialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
			dialog.FileName = settingsFilename;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
				settingsFilename = dialog.FileName;

                XmlSerializer serializer = new XmlSerializer(typeof(ControlParams));
				serializer.Serialize(new StreamWriter(settingsFilename), controlParams);

				updateTitle();
            }
        }

		public void printPreviewExecute(object sender, ExecutedRoutedEventArgs e)
		{
			if (currentView != ViewType.Setup)
			{
				printing.print(true);
				Util.gcCollect();
			}
		}

		public void printExecute(object sender, ExecutedRoutedEventArgs e)
		{
			if (currentView != ViewType.Setup)
			{
				printing.print(false);
				Util.gcCollect();
			}
		}

		public void exitExecute(object sender, ExecutedRoutedEventArgs e)
		{
			exit();
		}

		public void exit()
		{
			Environment.Exit(0);
		}

		public void exportData()
		{
            FileDialog dialog = new SaveFileDialog();
            dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
				model.writeData(dialog.FileName, viewParams, selectedTimei);
			}
		}

		public void exportImage()
		{
			if (currentView != ViewType.Setup)
			{
				FileDialog dialog = new SaveFileDialog();
				double width, height, dpi;
				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					dpi = optionParams.exportDpi;
					width = Util.convertToDpi(printing.getWidth(), dpi);
					height = Util.convertToDpi(printing.getHeight(), dpi);

					ChromView chromView = new ChromView(Transparency.Partial);
					chromView.visOutSet = view.getChromVisOutSet();
					chromView.Arrange(new Rect(new Size(width, height)));
					chromView.UpdateLayout();

					Util.saveBitmapImage(Util.visualToBitmap(chromView, width, height), dialog.FileName);
				}
				Util.gcCollect();
			}
		}

		public void exportImages()
		{
			if (currentView == ViewType.Time)
			{
				FileDialog dialog = new SaveFileDialog();
				double width = 1920;
				double height = 1080;
				int ntime = view.getChromNtime();
				string filename;

				dialog.Filter = "PNG files (*.png)|*.png|All files (*.*)|*.*";
				if (dialog.ShowDialog() == DialogResult.OK)
				{
					ChromView chromView = new ChromView(Transparency.Opaque);
					chromView.outParams = view.getChromOutParams();
					chromView.TimeMode = true;
					chromView.Arrange(new Rect(new Size(width, height)));
					chromView.UpdateLayout();
					
					for (int timei = 0; timei < ntime; timei++)
					{
						filename = Util.createNumberedFilename(dialog.FileName, timei);

						chromView.setChromTimei(timei);
						chromView.UpdateLayout();
						Util.saveBitmapImage(Util.visualToBitmap(chromView, width, height), filename);

						Util.gcCollect();
						view.updateProgress((float)timei / ntime);
						System.Windows.Forms.Application.DoEvents();
					}
					view.clearProgress();
				}
			}
		}

		public void showReport()
		{
			ReportWindow reportWindow;
			int timei = -1;
			double width = printing.getWidth();
			double height = printing.getHeight();

			if (currentView == ViewType.Time)
			{
				timei = selectedTimei;
			}

			reportWindow = new ReportWindow(width, height, getInParams(), getOutParams(timei));
			reportWindow.ShowDialog();
		}

		public UIElement[] getPrintPages(double width, double height)
		{
			List<UIElement> pages = new List<UIElement>();
			ChromPage chromPage;
			DateTime timestamp = DateTime.Now;
			int timei = -1;

			if (currentView == ViewType.Time)
			{
				timei = selectedTimei;
			}

			if (currentView != ViewType.Setup)
			{
				// page 1
				chromPage = new ChromPage(width, height, timestamp, ChromPageContent.Chrom, 1, 2);
				chromPage.chromView.visOutSet = view.getChromVisOutSet();
				pages.Add(chromPage);

				// page 2
				chromPage = new ChromPage(width, height, timestamp, ChromPageContent.Params, 2, 2);
				chromPage.createReport(getInParams(), getOutParams(timei));
				pages.Add(chromPage);
			}
			return pages.ToArray();
		}

		public TextParamList getInParams()
		{
			TextParamList textList = null;

			if (model != null)
			{
				if (model.model != null)
				{
					if (model.model.inParams != null)
					{
						textList = model.model.inParams.getText(true);
					}
				}
			}
			return textList;
		}

		public string getOutParams(int timei)
		{

			if (model != null)
			{
				if (model.model != null)
				{
					if (model.model.outParams != null)
					{
						return model.model.outParams.getText(timei);
					}
				}
			}
			return "";
		}
		
		public void setPhaseDisplay(PhaseDisplayType phaseDisplay)
        {
            viewParams.phaseDisplay = phaseDisplay;
            updateOutReq = true;
            updateTimeOutReq = true;
            updateViewParams();
        }

        public void setPeaksDisplay(PeaksDisplayType peaksDisplay)
        {
            viewParams.peaksDisplay = peaksDisplay;
            updateOutReq = true;
            updateTimeOutReq = true;
            updateViewParams();
        }

        public void toggleProbUnits()
        {
            viewParams.showProbUnits = !viewParams.showProbUnits;
            updateOutReq = true;
            updateTimeOutReq = true;
            updateViewParams();
        }

        public void setXScale(QuantityType units)
        {
            viewParams.viewUnits = units;
            updateOutReq = true;
            updateTimeOutReq = true;
            updateViewParams();
        }

        public void toggleSyncScale()
        {
            viewParams.syncScales = !viewParams.syncScales;
            updateOutReq = true;
            updateTimeOutReq = true;
            updateViewParams();
        }

        public void setYScale(YScaleType yScale)
        {
            viewParams.yScale = yScale;
            updateOutReq = true;
            updateTimeOutReq = true;
            updateViewParams();
        }

		public void setExponents(ExponentType exponentType)
		{
			viewParams.exponentType = exponentType;
			updateOutReq = true;
			updateTimeOutReq = true;
			updateViewParams();
		}

        public void showOptions()
        {
			OptionsWindow optionsWindow = new OptionsWindow(optionParams);
			optionsWindow.Owner = view.mainWindow;
			if ((bool)optionsWindow.ShowDialog())
			{
				updateModelReq = true;
				updateViewParams();
			}
        }

        public void showStats()
        {
            if (currentView == ViewType.Setup)
            {
                if (model.preview != null)
                {
                    System.Windows.MessageBox.Show(model.preview.stats.printf(), "Preview stats");
                }
            }
            else
            {
                if (model.model != null)
                {
                    System.Windows.MessageBox.Show(model.model.stats.printf(), "Model stats");
                }
            }
        }

		public void checkUpdates(bool showResult = false)
        {
			Thread updateThread = new Thread(checkUpdates);
			updateThread.Start(showResult);
		}

		void checkUpdates(object showResult0)
		{
			bool showResult = (bool)showResult0;
			string currentVersion;
			string webVersion;
			string assemblyName;

			try
			{
				currentVersion = Util.getAssemblyVersion();
				assemblyName = Util.getAssemblyName().ToLower();
				webVersion = Util.getUrl(Constants.webFilesUrl + assemblyName + "ver");
				if (webVersion != "")
				{
					if (compareVersions(currentVersion, webVersion) > 0)
					{
						// newer version found
						if (System.Windows.MessageBox.Show("New version available to download\nGo to download page now?", Util.getAssemblyTitle(),
							MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
						{
							Util.openWebLink(Constants.webPage);
						}
						return;
					}
					else if (showResult)
					{
						// same (or older) version
						System.Windows.MessageBox.Show("Current version is up to date.", Util.getAssemblyTitle());
						return;
					}
				}
			}
			catch (Exception)
			{
			}
			if (showResult)
			{
				// unable to check version
				System.Windows.MessageBox.Show("No newer version found.", Util.getAssemblyTitle());
			}
		}

		int compareVersions(string version1, string version2)
		{
			int comp = 0;
			string[] versions1 = version1.Split('.');
			string[] versions2 = version2.Split('.');
			int v1, v2;

			for (int i = 0; i < versions1.Length && i < versions2.Length; i++)
			{
				int.TryParse(versions1[i], out v1);
				int.TryParse(versions2[i], out v2);

				if (v2 > v1)
				{
					comp = 1;
					break;
				}
				else if (v2 < v1)
				{
					comp = -1;
					break;
				}
			}
			return comp;
		}

		public void showAbout(bool splashMode)
        {
			AboutWindow aboutWindow = new AboutWindow(splashMode);
			aboutWindow.Owner = view.mainWindow;
			aboutWindow.ShowDialog();
        }

    }
}
