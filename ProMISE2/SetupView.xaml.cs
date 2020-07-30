using System;
using System.Windows.Controls;

namespace ProMISE2
{
	public partial class SetupView : UserControl
	{
		ProfileView view;

		ControlParams controlParams;

		public SetupView(ControlParams controlParams)
		{
			this.controlParams = controlParams;
			InitializeComponent();
            profileCombo.ItemsSource = Util.GetEnumDescriptions(typeof(ProfileType));
			volUnitsCombo.ItemsSource = Util.GetEnumDescriptions(typeof(VolUnitsType));
            massUnitsCombo.ItemsSource = Util.GetEnumDescriptions(typeof(MassUnitsType));
            timeUnitsCombo.ItemsSource = Util.GetEnumDescriptions(typeof(TimeUnitsType));
			this.DataContext = controlParams;

			// can make dynamic using ControlParamsObserver
			view = new SetupPreviewProfileView(controlParams);
			setupContent.Content = view;
		}

		public void updateParams(ControlParams controlParams)
		{
			this.controlParams = controlParams;
            this.DataContext = controlParams;
			view.updateParams(controlParams);
		}

		public void updatePreview(OutParams outParams)
		{
			view.updatePreview(outParams);
		}

		private void profileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			//set Handled to true to prevent event to buble up to tabcontrol
			e.Handled = true;
		}

        private void volUnitsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;
        }

        private void massUnitsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;
        }

        private void timeUnitsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //set Handled to true to prevent event to buble up to tabcontrol
            e.Handled = true;
        }

	}
}
