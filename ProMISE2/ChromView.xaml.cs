using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ProMISE2
{
	public delegate void SelectCompEventHandler(Object sender, OutComp comp);
	public delegate void SelectCompsEventHandler(Object sender, OutComp comp1, OutComp comp2);

	public interface TimeObserver
	{
		void timeChanged(int timei, string timeLabel);
	}

	public partial class ChromView : UserControl
	{
		public OutParams outParams;
		public OutSet outSet;
		public VisOutSet visOutSet;

		ToolTip chromToolTip = new ToolTip();

		public event SelectCompEventHandler CompSelected;
		public event SelectCompsEventHandler CompsSelected;

		public ArrayList timeObservers = new ArrayList();

		public bool TimeMode { get; set; }

		bool mouseEventsEnabled = false;
		Transparency transparency = Transparency.Outline;

		public bool redrawn = false;

		float[,] rBuffer;
		float[,] gBuffer;
		float[,] bBuffer;
		float[,] totBuffer;
		System.Drawing.Point[] circlePattern;
		Rect allRect = new Rect();
		Rect chromRect = new Rect();
		double xmarginScale, xmargin, ymargin1, ymargin2;
		double width, height;
		double avgsize;
		double vpenSize = 0.002;
		double vfontSize = 0.02;
		double vsmallTickSize = 0.01;
		double vlargeTickSize = 0.015;
		public float zoom = 1;
		public float scroll = 0;
		double penwidth, fontheight;
		FontFamily fontfamily = new FontFamily("GenericSansSerif");
		int overcomp = -1;
		int selcomp1 = -1;
		int selcomp2 = -1;

		int ntime = 0;
		int timei = 0;

		public ChromView()
		{
			init();
		}

		public ChromView(Transparency transparency, bool mouseEventsEnabled = false)
		{
			this.transparency = transparency;
			this.mouseEventsEnabled = mouseEventsEnabled;
			init();
		}

		void init()
		{
			InitializeComponent();
		}

		public void registerTimeObserver(TimeObserver observer)
		{
			timeObservers.Add(observer);
		}

		public void unregisterTimeObserver(TimeObserver observer)
		{
			timeObservers.Remove(observer);
		}

		public void update(OutParams outParams)
		{
			this.outParams = outParams;
			if (TimeMode)
			{
				ntime = outParams.timeVisOutSet.Count;
                // Reset scrollbar
                //scrollBar.Value = 0;
                timei = 0;
                if (timei >= ntime)
                {
                    timei = ntime - 1;
                }
				updateScroll();
                updateTimeText();
			}
			update();
		}

		public void update()
		{
			if (outParams != null)
			{
				if (TimeMode)
				{
					if (timei >= 0 && timei < outParams.timeOutSet.Count)
					{
						outSet = outParams.timeOutSet[timei];
					}
					else
					{
						outSet = null;
					}
					if (timei >= 0 && timei < outParams.timeVisOutSet.Count)
					{
						visOutSet = outParams.timeVisOutSet[timei];
					}
					else
					{
						visOutSet = null;
					}
				}
				else
				{
					outSet = outParams.outSet;
					visOutSet = outParams.visOutSet;
				}
			}
			else
			{
				outSet = null;
				visOutSet = null;
			}
			updateSizes();
			redraw();
		}

		public OutParams getOutParams()
		{
			return outParams;
		}

		public VisOutSet getVisOutSet()
		{
			return visOutSet;
		}

		private void mainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (updateSizes())
			{
				redraw();
			}
		}

		private void mainCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (mouseEventsEnabled && !TimeMode)
			{
				if (e.Delta > 0)
				{
					zoom *= 1.5f;
				}
				else if (e.Delta < 0)
				{
					zoom /= 1.5f;
					if (zoom < 1)
					{
						zoom = 1;
					}
				}
				updateScroll();
				redraw();
			}
		}

		private void mainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			// no component selected
			if (mouseEventsEnabled)
			{
				resetSelected();
				compSelected(null);
				compsSelected(null, null);
			}
		}

		public void compSelected(OutComp comp)
		{
			if (CompSelected != null)
			{
				CompSelected(this, comp);
			}
		}

		public void compsSelected(OutComp comp1, OutComp comp2)
		{
			if (CompsSelected != null)
			{
				CompsSelected(this, comp1, comp2);
			}
		}

		public int getChromNtime()
		{
			return ntime;
		}

		public void setChromTimei(int timei)
		{
			this.timei = timei;
			update();
		}

		public void reset()
		{
			bool needsRedraw = resetSelected();
			int timei0 = timei;

			if (ntime > 0)
			{
				timei = 0;
			}
			else
			{
				timei = -1;
			}
			if (timei != timei0)
			{
				needsRedraw = true;
			}
			if (needsRedraw)
			{
				scrollBar.Value = 0;
				update();
			}
		}

		void updateScroll()
		{
			if (TimeMode)
			{
				scrollBar.Minimum = 0;
				scrollBar.Maximum = ntime;
				scrollBar.ViewportSize = 1;
				scrollBar.SmallChange = 1;
				scrollBar.LargeChange = 10;
				scrollBar.Visibility = Visibility.Visible;
			}
			else
			{
				if (zoom > 1)
				{
					scrollBar.Minimum = 0;
					scrollBar.Maximum = zoom - 1;
					scrollBar.ViewportSize = 1;
					scrollBar.SmallChange = 0.1;
					scrollBar.LargeChange = 1;
					scrollBar.Visibility = Visibility.Visible;
				}
				else
				{
					scrollBar.Value = 0;
					scrollBar.Visibility = Visibility.Collapsed;
				}
			}
		}

        private void scrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TimeMode)
            {
                int timei0 = (int)e.NewValue;
                if (timei0 != timei)
                {
                    timei = timei0;
                    if (timei >= ntime)
                    {
                        timei = ntime - 1;
                    }
					update();
                    updateTimeText();
                }
            }
            else
            {
				scroll = (float)e.NewValue;
				update();
            }
        }

		void updateTimeText()
		{
			if (outSet != null && visOutSet != null)
			{
				string timeLabel = string.Format("Time={0:F1} [{1}]", outSet.time, visOutSet.timeUnits);

				foreach (TimeObserver observer in timeObservers)
				{
					observer.timeChanged(timei, timeLabel);
				}
			}
		}

		public void redraw()
		{
			if (width > 0 && height > 0)
			{
				clear();

				if (visOutSet != null)
				{
					drawRawChroms();
					drawScaleLines();
					drawChroms();
					drawPeaks();
					drawAxes();
				}
				redrawn = true;
			}
		}

		public void clear()
		{
			mainCanvas.Children.Clear();
			mainImage.Source = null;

			if (transparency != Transparency.Outline)
			{
				background.Stroke = null;
			}

			if (transparency == Transparency.Transparent)
			{
				background.Fill = null;
			}
			else if (transparency == Transparency.Partial)
			{
				background.Margin = new Thickness(chromRect.Left, chromRect.Top, width - chromRect.Right, height - chromRect.Bottom);
			}
		}

		public void drawChroms()
		{
			// reverse order to put sum chrom at back
			for (int i = visOutSet.visSeries.Length - 1; i >= 0; i--)
			{
				drawChrom(visOutSet.visSeries[i]);
			}
			// in case raw chroms contain type graph series
			foreach (VisSerie visSerie in visOutSet.visRawSeries)
			{
				drawChrom(visSerie);
			}
		}

		public void drawChrom(VisSerie visSerie)
		{
			VisPoint[] visPoints;
			PointCollection drawPoints = new PointCollection();
			Polyline polyline = new Polyline();
			Line line;
			double thickness = 2 * penwidth;
			int ncells;
			double vx, vy;
			int vn;
			double x, y, lastx, lasty;
			bool visible, lastvisible;
			bool reduce = false;
			int step = 1;
			bool multiColor = visSerie.multiColor;

			visPoints = visSerie.visPoints;
			ncells = visPoints.Length;

			if (visSerie.type == VisSerieType.Graph)
			{
				if (width > 0)
				{
					step = (int)(ncells / (width * zoom));
					reduce = (step > 1);
					if (step < 1)
					{
						step = 1;
					}
				}

				lastvisible = false;
				lastx = 0;
				lasty = 0;

				for (int i = 0; i < ncells; i += step)
				{
					if (!reduce)
					{
						vx = visPoints[i].vx;
						vy = visPoints[i].vy;
					}
					else
					{
						vx = 0;
						vy = 0;
						vn = 0;
						for (int j = 0; j < step; j++)
						{
							if (i + j < visPoints.Length)
							{
								vx += visPoints[i + j].vx;
								vy += visPoints[i + j].vy;
								vn++;
							}
						}
						vx /= vn;
						vy /= vn;
					}
					x = convVirtX(vx);
					y = convVirtY(vy);

					visible = checkOnScreenX(x);

					if (visible || lastvisible)
					{
						if (!multiColor)
						{
							drawPoints.Add(new Point(x, y));
						}
						else if (i > 0)
						{
							line = new Line();
							line.Stroke = new SolidColorBrush(visPoints[i].color);
							line.StrokeThickness = thickness;

							line.X1 = lastx;
							line.Y1 = lasty;
							line.X2 = x;
							line.Y2 = y;
							addMouseHandler(line, visSerie.compi);
							mainCanvas.Children.Add(line);
						}
					}
					lastx = x;
					lasty = y;
					lastvisible = visible;
				}

				if (!multiColor)
				{
					polyline = new Polyline();
					polyline.Stroke = new SolidColorBrush(visSerie.drawColor);
					polyline.StrokeThickness = thickness;
					polyline.Points = drawPoints;
					addMouseHandler(polyline, visSerie.compi);
					mainCanvas.Children.Add(polyline);
				}
			}
		}

		void addMouseHandler(FrameworkElement uiElement, int tagi)
		{
			if (mouseEventsEnabled)
			{
				uiElement.Tag = tagi;
				uiElement.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(comp_Over);
				uiElement.MouseMove += new MouseEventHandler(comp_Move);
				uiElement.MouseLeftButtonDown += new MouseButtonEventHandler(comp_Click);
			}
		}

		void drawRawChroms()
		{
			int pixelWidth, pixelHeight;
			WriteableBitmap bitmap;
			PixelFormat pixelFormat = PixelFormats.Bgra32;
			int bytesPerPixel = pixelFormat.BitsPerPixel / 8;
			byte[] byteData;
			Rect visRect;
			float tot, r, g, b, a;
			int ncells = 0;
			int pos;

			if (visOutSet.visRawSeries.Length > 0)
			{
				visRect = visOutSet.visRawSeries[0].visRect;
				pixelWidth = (int)(width * visRect.Width);
				pixelHeight = (int)(height * visRect.Height);
				bitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, pixelFormat, null);
				byteData = new byte[pixelHeight * pixelWidth * bytesPerPixel];

				rBuffer = new float[pixelWidth, pixelHeight];
				gBuffer = new float[pixelWidth, pixelHeight];
				bBuffer = new float[pixelWidth, pixelHeight];
				totBuffer = new float[pixelWidth, pixelHeight];

				foreach (VisSerie visSerie in visOutSet.visRawSeries)
				{
					circlePattern = Util.getCirclePoints(convVirtYsize(visSerie.drawSize / 2));
					drawChrom(visSerie, bitmap);
					ncells += visSerie.visPoints.Length;
				}
				ncells /= visOutSet.comps.Count;

				pos = 0;
				for (int y = 0; y < pixelHeight; y++)
				{
					for (int x = 0; x < pixelWidth; x++)
					{
						tot = totBuffer[x, y];
						if (tot != 0)
						{
							r = rBuffer[x, y] / tot;
							if (r > 1) r = 1;
							g = gBuffer[x, y] / tot;
							if (g > 1) g = 1;
							b = bBuffer[x, y] / tot;
							if (b > 1) b = 1;
							a = tot * 10 / ncells;
							if (a > 1) a = 1;
							byteData[pos] = (byte)(b * 0xFF);
							byteData[pos + 1] = (byte)(g * 0xFF);
							byteData[pos + 2] = (byte)(r * 0xFF);
							byteData[pos + 3] = (byte)(a * 0xFF);
						}
						pos += bytesPerPixel;
					}
				}

				bitmap.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), byteData, pixelWidth * bytesPerPixel, 0, 0);

				mainImage.Margin = new Thickness(0, ymargin1, 0, 0);
				mainImage.Source = bitmap;
				mainImage.InvalidateVisual();
			}
		}

		void drawChrom(VisSerie visSerie, WriteableBitmap bitmap)
		{
			VisPoint[] visPoints;
			Rect visRect;
			Color color;
			int ncells;
			float r, g, b;
			float weight = 1;
			double cx, cy;
			int pixelWidth, pixelHeight;
			int x, y;

			visPoints = visSerie.visPoints;
			ncells = visPoints.Length;
			color = visSerie.drawColor;
			weight = visSerie.drawWeight;
			r = color.ScR * weight;
			g = color.ScG * weight;
			b = color.ScB * weight;

			visRect = visSerie.visRect;
			pixelWidth = (int)(width * visRect.Width);
			pixelHeight = (int)(height * visRect.Height);

			if (visSerie.type == VisSerieType.Units)
			{
				foreach (VisPoint point in visPoints)
				{
					cx = convVirtX(point.vx);
					cy = convVirtY(point.vy);

					foreach (System.Drawing.Point circlePoint in circlePattern)
					{
						x = (int)(cx + circlePoint.X);
						y = (int)(cy + circlePoint.Y);
						if (x >= 0 && x < pixelWidth && y >= 0 && y < pixelHeight)
						{
							rBuffer[x, y] += r;
							gBuffer[x, y] += g;
							bBuffer[x, y] += b;
							totBuffer[x, y] += weight;
						}
					}
				}
			}
		}

		void drawPeaks()
		{
			VisComp visComp;
			double x, y1, y2;
			double angle;

			for (int i = 0; i < visOutSet.comps.Count; i++)
			{
				visComp = visOutSet.comps[i];
				x = convVirtX(visComp.point.vx);
				y1 = convVirtY(0);
				y2 = convVirtY(1);

				drawLine(x, y1, x, y2, visComp.lineColor, penwidth);

				TextBlock peakLabel = new TextBlock();
				peakLabel.Foreground = new SolidColorBrush(visComp.point.color);
				peakLabel.FontFamily = fontfamily;
				peakLabel.FontSize = fontheight;
				peakLabel.Text = visComp.label;

				if (visComp.point.vy > 0.5)
				{
					angle = -90;
					x -= peakLabel.FontSize / 2;
				}
				else
				{
					angle = 90;
					x += peakLabel.FontSize / 2;
				}

				peakLabel.TextEffects.Add(new TextEffect(new RotateTransform(angle), null, null, 0, int.MaxValue));
				Canvas.SetLeft(peakLabel, x);
				Canvas.SetTop(peakLabel, convVirtY(visComp.point.vy));

				mainCanvas.Children.Add(peakLabel);
			}
		}

		void drawAxes()
		{
			VisPoint visPoint;
			TextBlock titleText;
			TextBlock tickLabel;
			FormattedText ft;
			string label;
			double textWidth;
			double x1, y1, x2, y2;
			int naxes = visOutSet.visAxes.visAxes.Count;

			foreach (VisAxis axis in visOutSet.visAxes.visAxes)
			{
				drawLine(convVirtX(axis.point1.vx), convVirtY(axis.point1.vy), convVirtX(axis.point2.vx), convVirtY(axis.point2.vy), axis.drawColor, axis.drawSize * penwidth);

				if (axis.drawMinorTicks)
				{
					// Minor ticks
					foreach (VisPoint point in axis.scaletickpos)
					{
						y1 = convVirtY(point.vy);
						y2 = y1;
						if (axis.isHorizontal())
						{
							// X axis
							x1 = convVirtX(point.vx);
							x2 = x1;
							y2 += convVirtSize(vsmallTickSize) * axis.getScaleSideFactor();
						}
						else
						{
							// Y axis
							x1 = convVirtXnoScrollNoZoom(point.vx);
							x2 = x1;
							x2 -= convVirtSize(vsmallTickSize) * axis.getScaleSideFactor();
						}
						drawLine(x1, y1, x2, y2, point.color, penwidth);
					}
				}

				if (axis.drawMajorTicks || axis.drawLabels)
				{
					// Major ticks
					for (int i = 0; i < axis.scalelabelpos.Count; i++)
					{
						visPoint = axis.scalelabelpos[i];

						y1 = convVirtY(visPoint.vy);
						y2 = y1;
						if (axis.isHorizontal())
						{
							// X axis
							x1 = convVirtX(visPoint.vx);
							x2 = x1;
							y2 += convVirtSize(vlargeTickSize) * axis.getScaleSideFactor();
						}
						else
						{
							// Y axis
							x1 = convVirtXnoScrollNoZoom(visPoint.vx);
							x2 = x1;
							x2 -= convVirtSize(vlargeTickSize) * axis.getScaleSideFactor();
						}

						if (axis.drawMajorTicks)
						{
							drawLine(x1, y1, x2, y2, visPoint.color, penwidth);
						}

						if (axis.drawLabels)
						{
							label = axis.scalelabeltext[i];

							tickLabel = new TextBlock();
							tickLabel.Foreground = new SolidColorBrush(visPoint.color);
							tickLabel.FontFamily = fontfamily;
							tickLabel.FontSize = fontheight;
							tickLabel.Text = label;

							ft = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
													new Typeface(fontfamily, tickLabel.FontStyle, tickLabel.FontWeight, tickLabel.FontStretch),
													fontheight, tickLabel.Foreground);
							textWidth = ft.Width;	// accurate text width

							if (axis.isHorizontal())
							{
								// X axis
								if (axis.isReverse())
								{
									y2 -= fontheight;
								}
								x2 -= textWidth / 2;
							}
							else
							{
								// Y axis
								if (!axis.isReverse())
								{
									x2 -= (textWidth + fontheight * 0.2);
								}
								if (x2 < xmarginScale)
								{
									x2 = xmarginScale;
								}
								y2 -= fontheight / 2;
							}
							Canvas.SetLeft(tickLabel, x2);
							Canvas.SetTop(tickLabel, y2);
							mainCanvas.Children.Add(tickLabel);
						}
					}
				}

				label = axis.title;

				titleText = new TextBlock();
				titleText.Foreground = new SolidColorBrush(axis.drawColor);
				titleText.FontFamily = fontfamily;
				titleText.FontSize = fontheight;
				titleText.Text = label;

				ft = new FormattedText(label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
										new Typeface(fontfamily, titleText.FontStyle, titleText.FontWeight, titleText.FontStretch),
										fontheight, titleText.Foreground);
				textWidth = ft.Width;	// accurate text width

				if (axis.isHorizontal())
				{
					// X axis
					Canvas.SetLeft(titleText, convVirtXnoScrollNoZoom(0.5) - textWidth / 2);
					Canvas.SetTop(titleText, height - fontheight * 1.25);
				}
				else
				{
					// Y axis
					y1 = (axis.point1.vy + axis.point2.vy) / 2;
					titleText.TextEffects.Add(new TextEffect(new RotateTransform(-90), null, null, 0, int.MaxValue));
					Canvas.SetLeft(titleText, 0);
					Canvas.SetTop(titleText, convVirtY(y1) + textWidth / 2);
				}
				mainCanvas.Children.Add(titleText);
			}
		}

		void drawScaleLines()
		{
			double x1, y1, x2, y2;

			foreach (VisAxis axis in visOutSet.visAxes.visAxes)
			{
				if (axis.drawLines)
				{
					foreach (VisPoint visPoint in axis.scalelabelpos)
					{
						x1 = convVirtX(visPoint.vx);
						y1 = convVirtY(visPoint.vy);
						x2 = x1;
						y2 = y1;
						if (axis.isHorizontal())
						{
							y2 = convVirtY(0);
						}
						else
						{
							x2 = convVirtX(1);
						}
						drawLine(x1, y1, x2, y2, Colors.LightGray, penwidth / 2);
					}
				}
			}
		}

		private void comp_Over(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue)
			{
				Shape shape = (Shape)sender;
				if (shape != null)
				{
					overcomp = (int)shape.Tag;
					if (overcomp != selcomp1 && overcomp != selcomp2)
					{
						selectShape(overcomp);
					}
				}
			}
			else
			{
				if (overcomp != selcomp1 && overcomp != selcomp2)
				{
					unselectShape(overcomp);
				}
				overcomp = -1;
				chromToolTip.IsOpen = false;
			}
		}

		private void comp_Move(object sender, MouseEventArgs e)
		{
			UIElement uiElement = (UIElement)sender;
			VisSerie serie;
			VisPoint point;
			OutCell outCell = null;
			Point epos = e.GetPosition(mainCanvas);
			float vx = (float)convToVirtX(epos.X);
			float vy = (float)convToVirtY(epos.Y);
			float dist;
			float mindist = 0;
			int seri = -1;
			int pointi = -1;
			string compLabel = "";
			string toolTipText = "";

			if (mouseEventsEnabled)
			{
				if (overcomp >= 0 && overcomp < visOutSet.comps.Count)
				{
					compLabel = visOutSet.comps[overcomp].label;
				}
				else if (overcomp == visOutSet.comps.Count)
				{
					compLabel = "Sum";
				}

				// find closest point in visOutSet
				for (int s = 0; s < visOutSet.visSeries.Length; s++)
				{
					serie = visOutSet.visSeries[s];
					for (int i = 0; i < serie.visPoints.Length; i++)
					{
						point = serie.visPoints[i];
						dist = (float)Math.Sqrt(Math.Pow(point.vx - vx, 2) + Math.Pow(point.vy - vy, 2));
						if (dist < mindist || (s == 0 && i == 0))
						{
							mindist = dist;
							seri = s;
							pointi = i;
						}
					}
				}

				// find corresponding value in outSet (in units)
				if (seri >= 0 && pointi >= 0)
				{
					if (seri < outSet.unitsOutCells.Length)
					{
						if (pointi < outSet.unitsOutCells[seri].Length)
						{
							outCell = outSet.unitsOutCells[seri][pointi];
						}
					}
					if (outCell != null)
					{
						toolTipText = String.Format("[{0}]: Pos={1:F1} [{2}]", compLabel, outCell.pos, visOutSet.posUnits);
						if (visOutSet.useMultiplier)
						{
							toolTipText += String.Format(" Con={0:0.0}", Math.Abs(outCell.con) * visOutSet.conMultiplier);
						}
						else
						{
							toolTipText += String.Format(" Con={0:0.00E+0}", Math.Abs(outCell.con));
						}
						toolTipText += String.Format(" [{0}]", visOutSet.conUnits);
					}
				}
				chromToolTip.Content = toolTipText;
				ToolTipService.SetToolTip(uiElement, chromToolTip);
				chromToolTip.StaysOpen = true;
				chromToolTip.IsOpen = true;
			}
		}

		private void comp_Click(object sender, MouseButtonEventArgs e)
		{
			bool shift = (Keyboard.Modifiers == ModifierKeys.Shift);
			int selcomp01 = selcomp1;
			int selcomp02 = selcomp2;

			if (mouseEventsEnabled)
			{
				if (shift && selcomp1 >= 0)
				{
					// avoid same component selected twice
					if (overcomp != selcomp1)
					{
						unselectShape(selcomp2);
						selcomp2 = overcomp;
						selectShape(selcomp2);
					}
					// shift key pressed + 2 comps
					// close comp info window
					compSelected(null);
					if (selcomp1 >= 0 && selcomp1 < outSet.comps.Count &&
						selcomp2 >= 0 && selcomp2 < outSet.comps.Count)
					{
						if (selcomp1 < selcomp2)
						{
							compsSelected(outSet.comps[selcomp1], outSet.comps[selcomp2]);
						}
						else
						{
							compsSelected(outSet.comps[selcomp2], outSet.comps[selcomp1]);
						}
					}
				}
				else
				{
					// unselect old
					unselectShape(selcomp1);
					unselectShape(selcomp2);

					selcomp1 = overcomp;
					selcomp2 = -1;
					// select new
					selectShape(selcomp1);

					if (selcomp1 >= 0)
					{
						if (selcomp1 < outSet.comps.Count)
						{
							compSelected(outSet.comps[selcomp1]);
						}
					}
				}

			}
			e.Handled = true;   // avoid underlying canvas being called to
		}

		public bool resetSelected()
		{
			int selcomp01 = selcomp1;
			int selcomp02 = selcomp2;

			if (mouseEventsEnabled)
			{
				unselectShape(selcomp1);
				unselectShape(selcomp2);

				selcomp1 = -1;
				selcomp2 = -1;

				return (selcomp1 != selcomp01 || selcomp2 != selcomp02);
			}
			return false;
		}

		void selectShape(int comp, bool select = true)
		{
			Shape shape;
			object tag;
			double thickness;

			if (comp >= 0)
			{
				foreach (UIElement element in mainCanvas.Children)
				{
					if (typeof(Shape).IsAssignableFrom(element.GetType()))
					{
						shape = (Shape)element;
						tag = shape.Tag;
						if (tag != null)
						{
							if ((int)tag == comp)
							{
								if (select)
								{
									thickness = 3 * penwidth;
								}
								else
								{
									thickness = 2 * penwidth;
								}
								shape.StrokeThickness = thickness;
							}
						}
					}
				}
			}
		}

		void unselectShape(int comp)
		{
			selectShape(comp, false);
		}

		public bool updateSizes()
		{
			width = mainCanvas.ActualWidth;
			height = mainCanvas.ActualHeight;
			if (width > 0 && height > 0)
			{
				avgsize = Math.Sqrt(width * height);
				penwidth = (float)avgsize * vpenSize;
				fontheight = (float)avgsize * vfontSize;
				if (fontheight < 8)
				{
					fontheight = 8;
				}

				xmarginScale = fontheight;
				xmargin = xmarginScale + fontheight * 5;
				ymargin1 = fontheight * 0.5;
				ymargin2 = ymargin1 + fontheight * 3;
				allRect = new Rect(0, 0, width, height);
				chromRect = new Rect(xmargin, ymargin1, width - xmargin, height - ymargin1 - ymargin2);
				return true;
			}
			return false;
		}

		double convVirtX(double vx)
		{
			// x = (vx * zoom - scroll) * w
			return (vx * zoom - scroll) * chromRect.Width + chromRect.Left;
		}

		double convVirtXnoScroll(double vx)
		{
			return vx * zoom * chromRect.Width + chromRect.Left;
		}

		double convVirtXnoScrollNoZoom(double vx)
		{
			return vx * chromRect.Width + chromRect.Left;
		}

		double convVirtXsize(double vx)
		{
			return vx * chromRect.Width;
		}

		double convVirtY(double vy)
		{
			return vy * chromRect.Height + chromRect.Top;
		}

		double convVirtYsize(double vy)
		{
			return vy * chromRect.Height;
		}

		double convVirtSize(double v)
		{
			return v * avgsize;
		}

		double convToVirtX(double x)
		{
			// vx = (x / w + scroll) / zoom
			return ((x - chromRect.Left) / chromRect.Width + scroll) / zoom;
		}

		double convToVirtY(double y)
		{
			return (y - chromRect.Top) / chromRect.Height;
		}

		bool checkOnScreenX(double x)
		{
			return (x >= 0 && x < width);
		}

		void drawLine(double x1, double y1, double x2, double y2, Color color, double thickness)
		{
			Line line = new Line();
			line.Stroke = new SolidColorBrush(color);
			line.StrokeThickness = thickness;
			line.X1 = x1;
			line.Y1 = y1;
			line.X2 = x2;
			line.Y2 = y2;
			mainCanvas.Children.Add(line);
		}

	}
}
