using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		DispatcherTimer timer = new DispatcherTimer();
		bool splashMode = false;

		public AboutWindow(bool splashMode = false)
		{
			this.splashMode = splashMode;

			InitializeComponent();

			DataContext = this;
			updateDesc();
			updateCopy();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			int seconds = 5;
			if (splashMode)
			{
				// auto close timer
				timer.Tick += new EventHandler(timer_Tick);
				timer.Interval = new TimeSpan(0, 0, seconds);
				timer.Start();
			}
		}

		void timer_Tick(object sender, EventArgs e)
		{
			Close();
		}

		void updateDesc()
		{
			string s = AssemblyDescription;
			string url = Util.extractUrl(s);
			string text = Util.extractText(s);
			Hyperlink link = new Hyperlink(new Run(text))
			{
				NavigateUri = new Uri(url)
			};
			link.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(link_RequestNavigate);
			descText.Inlines.Add(link);
		}

		void updateCopy()
		{
			string s = AssemblyCopyright;
			string url = Util.extractUrl(s);
			string text = Util.extractText(s);
			Hyperlink link = new Hyperlink(new Run(text))
			{
				NavigateUri = new Uri(url)
			};
			link.RequestNavigate += new System.Windows.Navigation.RequestNavigateEventHandler(link_RequestNavigate);
			copyText.Inlines.Add(link);
		}

		void link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Util.openWebLink(e.Uri.AbsoluteUri);
		}

		public string WindowTitle
		{
			get
			{
				return "About " + AssemblyTitle;
			}
		}

		public string AssemblyTitle
		{
			get
			{
				return Util.getAssemblyTitle();
			}
		}

		public string AssemblyVersion
		{
			get
			{
				return "Version " + Util.getAssemblyVersion();
			}
		}

		public string AssemblyDescription
		{
			get
			{
				return Util.getAssemblyDescription();
			}
		}

		public string AssemblyCopyright
		{
			get
			{
				return Util.getAssemblyCopyright();
			}
		}

		public string AssemblyProduct
		{
			get
			{
				return Util.getAssemblyProduct();
			}
		}

		public string AssemblyCompany
		{
			get
			{
				return Util.getAssemblyCompany();
			}
		}

	}
}
