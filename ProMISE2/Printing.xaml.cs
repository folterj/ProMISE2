using System;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for Printing.xaml
	/// </summary>
	public partial class Printing : Window
	{
		PrintContentInterface contentInterface;
		PrintDialog printDialog = new PrintDialog();
	
		double printWidth = 0;
		double printHeight = 0;
		double printPosX = 0;
		double printPosY = 0;
		double printDpiX = 0;
		double printDpiY = 0;

		string jobTitle = "ProMISE";

		public Printing()
		{
			InitializeComponent();

			preparePrintDialog();
		}

		void preparePrintDialog()
		{
			// Set page orientation (and size) to landscape
			printDialog.PrintTicket.PageOrientation = PageOrientation.Landscape;
			double width = (double)printDialog.PrintTicket.PageMediaSize.Width;
			double height = (double)printDialog.PrintTicket.PageMediaSize.Height;
			double mediaWidth = Math.Max(width, height);
			double mediaHeight = Math.Min(width, height);
			PageMediaSizeName mediaSizeName = (PageMediaSizeName)printDialog.PrintTicket.PageMediaSize.PageMediaSizeName;
			printDialog.PrintTicket.PageMediaSize = new PageMediaSize(mediaSizeName, mediaWidth, mediaHeight);

			// Get printable area
			PrintCapabilities printCapabilities = printDialog.PrintQueue.GetPrintCapabilities(printDialog.PrintTicket);
			PageImageableArea pageImageableArea = printCapabilities.PageImageableArea;
			printWidth = pageImageableArea.ExtentWidth;
			printHeight = pageImageableArea.ExtentHeight;
			printPosX = pageImageableArea.OriginWidth;
			printPosY = pageImageableArea.OriginHeight;

			printDpiX = (double)printDialog.PrintTicket.PageResolution.X;
			printDpiY = (double)printDialog.PrintTicket.PageResolution.Y;
		}

		public IDocumentPaginatorSource document
		{
			get { return documentViewer.Document; }
			set { documentViewer.Document = value; }
		}

		public double getWidth()
		{
			return printWidth;
		}

		public double getHeight()
		{
			return printHeight;
		}

		public void setPrintContentInterface(PrintContentInterface contentInterface)
		{
			this.contentInterface = contentInterface;
		}

		public void print(bool preview)
		{
			if (preview)
			{
				showPreview();
			}
			else
			{
				print();
			}
		}

		bool createDocument(double dpiX = 96, double dpiY = 96)
		{
			FixedDocument fixedDocument;
			PageContent pageContent;
			FixedPage fixedPage;

			double renderWidth = Util.convertToDpi(printWidth, dpiX);
			double renderHeight = Util.convertToDpi(printHeight, dpiY);
			UIElement[] pages = contentInterface.getPrintPages(renderWidth, renderHeight);

			double scaleX = 1 / Util.convertToDpi(1, dpiX);
			double scaleY = 1 / Util.convertToDpi(1, dpiY);

			if (pages.Length > 0)
			{
				fixedDocument = new FixedDocument();
				fixedDocument.PrintTicket = printDialog.PrintTicket;
				foreach (UIElement page in pages)
				{
					fixedPage = new FixedPage();
					fixedPage.PrintTicket = printDialog.PrintTicket;
					fixedPage.Width = printWidth;
					fixedPage.Height = printHeight;
					if (scaleX != 1 || scaleY != 1)
					{
						page.RenderTransform = new ScaleTransform(scaleX, scaleY);
						FixedPage.SetLeft(page, printPosX);
						FixedPage.SetTop(page, printPosY);
					}
					fixedPage.Children.Add(page);
					pageContent = new PageContent();
					pageContent.Child = fixedPage;
					fixedDocument.Pages.Add(pageContent);
				}

				document = fixedDocument;

				documentViewer.UpdateLayout();	// this forces layout recalc / redraw of chrompage (including chromview)
			}
			return (pages.Length > 0);
		}

		public void showPreview()
		{
			preparePrintDialog();
			createDocument();
			ShowDialog();
		}

		private void commandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			// needed so that preview executed works
		}

		private void commandBinding_PreviewExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
		{
			print();
		}

		public void print()
		{
			if ((bool)printDialog.ShowDialog())
			{
				preparePrintDialog();

				if (createDocument(printDpiX, printDpiY))
				{
					printDialog.PrintDocument(document.DocumentPaginator, jobTitle);
				}
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
			Hide();
		}

	}
}
