using System;
using System.Windows;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for ReportWindow.xaml
	/// </summary>
	public partial class ReportWindow : Window
	{
		ChromPage chromPage;
		string report;

		public ReportWindow(double width, double height, TextParamList inParamList, string outParams)
		{
			InitializeComponent();

			chromPage = new ChromPage(width, height, new DateTime(), ChromPageContent.Params, 0, 0);
			chromPage.canvas = reportCanvas;
			reportCanvas.Width = width;
			reportCanvas.Height = height;
			chromPage.createReport(inParamList, outParams);

			report = inParamList.ToString();
			if (outParams != "")
			{
				if (report != "")
				{
					report += "\n";
				}
				report += "Output parameters\n";
				report += outParams.Replace("<b>", "").Replace("</b>", "");
			}
		}

		private void copyButton_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(report);
		}

	}
}
