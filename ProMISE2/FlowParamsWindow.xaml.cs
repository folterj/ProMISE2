using System;
using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for FlowProps.xaml
	/// </summary>
	public partial class FlowProps : Window
	{
		ControlParams controlParams;

		public FlowProps(ControlParams controlParams)
		{
			this.controlParams = controlParams;

			InitializeComponent();

			intModeCombo.ItemsSource = Enum.GetValues(typeof(IntModeType));
			intStartPhaseCombo.ItemsSource = Enum.GetValues(typeof(PhaseType));
			eeModeCombo.ItemsSource = Util.GetEnumDescriptions(typeof(EEModeType));
			intUpCompCombo.Items.Clear();
			intLpCompCombo.Items.Clear();
			foreach(ControlComp comp in controlParams.controlcomps)
			{
				intUpCompCombo.Items.Add(comp.ToString());
				intLpCompCombo.Items.Add(comp.ToString());
			}

			DataContext = controlParams;

			fuJog.val = controlParams.fu;
			flJog.val = controlParams.fl;
			fuJog.IsEnabled = (controlParams.runMode != RunModeType.LowerPhase);
			flJog.IsEnabled = (controlParams.runMode != RunModeType.UpperPhase);
			ptransuJog.val = controlParams.Ptransu;
			ptranslJog.val = controlParams.Ptransl;
			updateTransJogsEnabled();
		}

		private void updateTransJogsEnabled()
		{
			ptransuJog.IsEnabled = (controlParams.ptransMode && controlParams.runMode != RunModeType.LowerPhase);
			ptranslJog.IsEnabled = (controlParams.ptransMode && controlParams.runMode != RunModeType.UpperPhase);
		}

		private void ptransCheck_Click(object sender, RoutedEventArgs e)
		{
			updateTransJogsEnabled();
		}

		private void fuJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.Fu = fuJog.Value;
		}

		private void flJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.Fl = flJog.Value;
		}

		private void ptransuJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.Ptransu = ptransuJog.Value;
		}

		private void ptranslJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.Ptransl = ptransuJog.Value;
		}

	}
}
