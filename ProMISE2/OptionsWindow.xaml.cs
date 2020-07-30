using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for OptionsWindow.xaml
	/// </summary>
	public partial class OptionsWindow : Window
	{
		OptionParams optionParams;

		public OptionsWindow(OptionParams optionParams)
		{
			this.optionParams = optionParams;
			InitializeComponent();
			DataContext = optionParams;
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			optionParams.save();
			Close();
		}

		private void defaultsButton_Click(object sender, RoutedEventArgs e)
		{
			optionParams.reset();
		}

	}
}
