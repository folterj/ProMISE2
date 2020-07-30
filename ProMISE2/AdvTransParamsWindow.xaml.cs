using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for AdvProps.xaml
	/// </summary>
	public partial class AdvTransProps : Window
	{
		ControlParams controlparams;

		public AdvTransProps(ControlParams controlparams)
		{
			this.controlparams = controlparams;
			InitializeComponent();
			DataContext = controlparams;
			columnJog.val = controlparams.column;
			kaJog.val = controlparams.ka;
		}

		private void columnJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlparams.column = (int)columnJog.Value;
		}

		private void kaJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlparams.ka = kaJog.Value;
		}
	}
}
