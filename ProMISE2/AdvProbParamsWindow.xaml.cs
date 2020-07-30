using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for AdvProps.xaml
	/// </summary>
	public partial class AdvProbProps : Window
	{
		ControlParams controlparams;

		public AdvProbProps(ControlParams controlparams)
		{
			this.controlparams = controlparams;
			InitializeComponent();
			DataContext = controlparams;
			rotspeedJog.val = controlparams.mixSpeed;
			effJog.val = controlparams.efficiency;
		}

		private void rotspeedJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlparams.MixSpeed = rotspeedJog.Value;
		}

		private void effJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlparams.Efficiency = effJog.Value;
		}
	}
}
