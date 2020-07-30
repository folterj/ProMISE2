using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for ColumnProps.xaml
	/// </summary>
	public partial class ColumnProps : Window
	{
		ControlParams controlParams;

		public ColumnProps(ControlParams controlParams)
		{
			this.controlParams = controlParams;
			InitializeComponent();
			DataContext = controlParams;
			ufJog.val = controlParams.uf;
			lfJog.val = controlParams.lf;
		}
	
		private void ufJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.uf = ufJog.Value;
			if (controlParams.updateFromUf())
			{
				lfJog.Value = controlParams.lf;
			}
		}

		private void lfJog_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			controlParams.lf = lfJog.Value;
			if (controlParams.updateFromLf())
			{
				ufJog.Value = controlParams.uf;
			}
		}

	}
}
