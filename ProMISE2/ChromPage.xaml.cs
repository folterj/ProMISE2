using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProMISE2
{
	/// <summary>
	/// Interaction logic for ChromPage.xaml
	/// </summary>
	public partial class ChromPage : UserControl
	{
		ChromPageContent pageContent;

		public ChromView chromView;
		public Canvas canvas;

		double fontSize = 10;

		public ChromPage(double width, double height, DateTime timestamp, ChromPageContent pageContent, int page, int npages)
		{
			this.pageContent = pageContent;

			Width = width;
			Height = height;

			InitializeComponent();

			intoText.Text = string.Format("{0:yyyy/MM/dd HH:mm:ss} - Page {1}/{2}", timestamp, page, npages);

			if (pageContent == ChromPageContent.Chrom)
			{
				chromView = new ChromView(Transparency.Transparent);
				addContent(chromView);
			}
			else if (pageContent == ChromPageContent.Params)
			{
				canvas = new Canvas();
				addContent(canvas);
			}
		}

		void addContent(UIElement uiElement)
		{
			Grid.SetRow(uiElement, 2);
			Grid.SetColumnSpan(uiElement, 2);
			mainGrid.Children.Add(uiElement);
		}

		public void createReport(TextParamList inparams, string outparams)
		{
			double avgsize = Math.Sqrt(Width * Height);
			bool bold = false;
			bool boldEnded = false;
			int nlines = 0;
			int ncols = 3;
			int nrows = 0;
			double x, y;

			fontSize = 0.015 * avgsize;
			double rowSize = 1.6 * fontSize;
			double colSize = 8 * fontSize;

			if (inparams != null)
			{
				nlines = inparams.getNLines();
				nrows = (int)Math.Ceiling((float)nlines / ncols);
				int coli = 0;
				int linei = 0;
				double colWidth = Width / ncols;

				foreach (TextParam param in inparams)
				{
					if (param.isHeader && linei > 0)
					{
						linei++;
						if (linei >= nrows)
						{
							coli++;
							linei = 0;
						}
					}

					x = coli * colWidth;
					y = linei * rowSize;
					addText(param.name, x, y, param.isHeader);

					x += colSize;
					addText(param.value, x, y);

					x += colSize;
					addText(param.units, x, y);

					linei++;
					if (linei >= nrows)
					{
						coli++;
						linei = 0;
					}
				}

				for (x = 1; x < ncols; x++)
				{
					addLine(x * colWidth, 0, x * colWidth, nrows * rowSize);
				}
				addLine(0, 0, Width, 0);
				addLine(0, nrows * rowSize, Width, nrows * rowSize);
			}

			if (outparams != "")
			{
				int row = nrows;
				int col = 0;
				string[] lines;
				string[] parts;
				string part;

				addText("Output parameters", 0, row * rowSize, true);
				row++;

				lines = outparams.Split('\n');

				if (lines.Length > 0)
				{
					// set columns size by dividing total width over number of columns
					ncols = lines[0].Split('\t').Length;
					colSize = Width / ncols;
				}

				foreach (string line in lines)
				{
					parts = line.Split('\t');
					foreach (string part0 in parts)
					{
						part = part0;
						if (part.StartsWith("<b>"))
						{
							bold = true;
						}
						else if (part.StartsWith("</b>"))
						{
							bold = false;
						}
						else if (part.EndsWith("</b>"))
						{
							boldEnded = true;
						}
						part = part.Replace("<b>", "");
						part = part.Replace("</b>", "");

						addText(part, col * colSize, row * rowSize, bold);

						if (boldEnded)
						{
							boldEnded = false;
							bold = false;
						}

						col++;
					}
					row++;
					col = 0;
				}
			}
		}

		void addText(string content, double x, double y, bool bold = false)
		{
			TextBlock textBlock = new TextBlock();

			if (bold)
			{
				textBlock.FontWeight = FontWeights.Bold;
				textBlock.Foreground = Brushes.DarkBlue;
			}

			textBlock.Text = content;
			textBlock.FontFamily = new FontFamily("GenericSansSerif");
			textBlock.FontSize = fontSize;

			Canvas.SetLeft(textBlock, x);
			Canvas.SetTop(textBlock, y);

			canvas.Children.Add(textBlock);
		}

		void addLine(double x1, double y1, double x2, double y2)
		{
			Line line = new Line();

			line.Stroke = Brushes.LightGray;
			line.X1 = x1;
			line.Y1 = y1;
			line.X2 = x2;
			line.Y2 = y2;

			canvas.Children.Add(line);
		}

	}
}
