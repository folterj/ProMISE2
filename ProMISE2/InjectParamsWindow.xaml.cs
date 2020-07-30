using System;
using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for InjectProps.xaml
	/// </summary>
	public partial class InjectProps : Window
	{
		ControlParams controlParams;

		public InjectProps(ControlParams controlParams)
		{
			this.controlParams = controlParams;

			InitializeComponent();

			injectModeCombo.ItemsSource = Enum.GetValues(typeof(InjectModeType));
			phaseCombo.ItemsSource = Enum.GetValues(typeof(PhaseType));
			feedUnitsCombo.ItemsSource = Enum.GetValues(typeof(QuantityType));
			DataContext = controlParams;
			posJog.val = controlParams.injectPos;
			posJog.IsEnabled = controlParams.advancedMode;
		}

		private void posJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.InjectPos = posJog.Value;
			controlParams.update();
		}

	}
}
