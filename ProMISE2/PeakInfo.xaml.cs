using System.Windows;

namespace ProMISE2
{
	public partial class PeakInfo : Window
	{
		public PeakInfo()
		{
			InitializeComponent();
		}

		public void updateParams(OutComp outComp)
		{
			DataContext = new ControlComp(outComp);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

	}
}
