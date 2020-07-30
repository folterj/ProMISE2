using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace ProMISE2
{
    public partial class SetupPreviewProfileView : ProfileView
    {
        ControlParams controlParams;

        public SetupPreviewProfileView(ControlParams controlParams)
            : base()
        {
            this.controlParams = controlParams;
            InitializeComponent();

            modelCombo.ItemsSource = Enum.GetValues(typeof(ModelType));
            runModeCombo.ItemsSource = Util.GetEnumDescriptions(typeof(RunModeType));
            kdefCombo.ItemsSource = Util.GetEnumDescriptions(typeof(KdefType));

            columnVis.updateControlParams(controlParams);

            this.DataContext = controlParams;
            previewDataGrid.DataContext = controlParams;

			updateParams(controlParams);
        }

        public override void updateParams(ControlParams controlParams)
        {
            this.controlParams = controlParams;
            this.DataContext = controlParams;
            previewDataGrid.DataContext = controlParams;

            columnVis.updateControlParams(controlParams);
            columnVis.update();

			updateParamSummary();
		}

		void updateParamSummary()
		{
			TextParamList textList = controlParams.getText();
			TextBlock textBlock = new TextBlock();
			string s;

			double rowHeight = 1.2 * textBlock.FontSize;
			double colWidth = 10 * rowHeight;

			int ncols = 2;
			int nlines = textList.getNLines();
			int nrows = (int)Math.Ceiling((float)nlines / ncols);
			int coli = 0;
			int linei = 0;

			paramGrid.Children.Clear();
			foreach (TextParam param in textList)
			{
				if (param.isHeader && linei > 0)
				{
					linei++;
					if (linei >= nrows)
					{
						coli++;
						linei = 0;
					}
				}

				s = param.name;
				if (param.value != "" || param.units != "")
				{
					s += "\t";
					if (param.value != "")
					{
						s += param.value;
					}
					if (param.units != "")
					{
						s += " " + param.units;
					}
				}
				addParamText(s, coli * colWidth, linei * rowHeight, param.isHeader);

				linei++;
				if (linei >= nrows)
				{
					coli++;
					linei = 0;
				}
			}
		}

		void addParamText(string s, double x, double y, bool bold)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Text = s;
			if (bold)
			{
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Foreground = Brushes.DarkBlue;
			}
			textBlock.Margin = new Thickness(x, y, 0, 0);
			paramGrid.Children.Add(textBlock);
		}

        public override void updatePreview(OutParams outParams)
        {
            previewVis.update(outParams);
        }

        private void columnVis_ColumnSelected(object sender, MouseButtonEventArgs e)
        {
			double h = Screen.PrimaryScreen.WorkingArea.Height;
			double x, y;

			ColumnProps props = new ColumnProps(controlParams);

			Point pos = previewVis.PointToScreen(new Point(0, 0));
			x = pos.X + (columnVis.Width - props.Width) / 2;
			y = pos.Y;
			if (y + props.Height > h)
			{
				y = h - props.Height;
			}
			props.Left = x;
			props.Top = y;

            props.ShowDialog();
        }

        private void columnVis_GearSelected(object sender, MouseButtonEventArgs e)
        {
			Window props = null;
			double h = Screen.PrimaryScreen.WorkingArea.Height;
			double x, y;

            switch (controlParams.model)
            {
                case ModelType.CCD:
                    props = new AdvCCDProps(controlParams);
                    break;
                case ModelType.Probabilistic:
                    props = new AdvProbProps(controlParams);
                    break;
                case ModelType.Transport:
                    props = new AdvTransProps(controlParams);
                    break;
            }

			if (props != null)
			{
				Point pos = previewVis.PointToScreen(new Point(0, 0));
				x = pos.X + (columnVis.Width - props.Width);
				y = pos.Y;
				if (y + props.Height > h)
				{
					y = h - props.Height;
				}
				props.Left = x;
				props.Top = y;

				props.ShowDialog();
			}
		}

        private void columnVis_FlowSelected(object sender, MouseButtonEventArgs e)
        {
			double h = Screen.PrimaryScreen.WorkingArea.Height;
			double x, y;

            FlowProps props = new FlowProps(controlParams);

			Point pos = previewVis.PointToScreen(new Point(0, 0));
			x = pos.X;
			y = pos.Y;
			if (y + props.Height > h)
			{
				y = h - props.Height;
			}
			props.Left = x;
			props.Top = y;

            props.ShowDialog();
        }

        private void columnVis_InjectSelected(object sender, MouseButtonEventArgs e)
        {
			double x, y;

			InjectProps props = new InjectProps(controlParams);

			Point pos = this.PointToScreen(new Point(0, 0));
			x = pos.X;
			y = pos.Y - props.Height;
			if (y < 0)
			{
				y = 0;
			}
			props.Left = x;
			props.Top = y;

            props.ShowDialog();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double margin = 12;
            double y0 = Canvas.GetTop(columnVis);
            double w0 = mainCanvas.ActualWidth - margin * 2;
			double w = w0;
            double h2 = (mainCanvas.ActualHeight - y0 - margin * 4) / 2.7;

            if (w < 200)
            {
                w = 200;
            }
            if (h2 < 100)
            {
                h2 = 100;
            }

            columnVis.Width = w;
            columnVis.Height = h2;

            Canvas.SetTop(previewVis, y0 + margin + h2);
            previewVis.Width = w;
            previewVis.Height = h2;

			Canvas.SetTop(compGroup, y0 + margin + h2 * 2);
            previewDataGrid.MaxWidth = w;
            previewDataGrid.Height = h2 * 0.7;

			setPositionParamsLabal();
        }

		private void previewDataGrid_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			setPositionParamsLabal();
		}

		void setPositionParamsLabal()
		{
			double margin = 12;
			double x = Canvas.GetLeft(compGroup) + compGroup.ActualWidth + margin/2;
			double y = Canvas.GetTop(compGroup);

			Canvas.SetTop(paramGroup, y);
			Canvas.SetLeft(paramGroup, x);
		}

        private void previewDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // when data grid view automatically creates new comps: register their observer
            controlParams.updateCompsObervers();
        }

        private void previewDataGrid_UnloadingRow(object sender, DataGridRowEventArgs e)
        {
            controlParams.updateControlComp();
            columnVis.update();
        }

        private void previewDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;
        }

        private void modelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;
        }

        private void runModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;
        }

        private void kdefCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;

			if (controlParams.controlcomps.Count > 0)
			{
				if (System.Windows.MessageBox.Show("Adapt current K values?", "Modifying K value definition", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				{
					foreach (ControlComp comp in controlParams.controlcomps)
					{
						comp.K = 1 / comp.K;
					}
				}
			}
        }

    }
}
