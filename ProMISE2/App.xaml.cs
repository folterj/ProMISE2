using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		App()
		{
			ProModel model = new ProModel();
			ProController controller = new ProController(model);
		}
	}
}
