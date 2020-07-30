using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for AdvProps.xaml
	/// </summary>
	public partial class AdvCCDProps : Window
	{
		ControlParams controlparams;

		public AdvCCDProps(ControlParams controlparams)
		{
			this.controlparams = controlparams;
			InitializeComponent();
			DataContext = controlparams;
			columnJog.val = controlparams.column;
			effJog.val = controlparams.efficiency;
		}

		private void columnJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlparams.column = (int)columnJog.Value;
		}

		private void effJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlparams.Efficiency = effJog.Value;
		}
	}
}
