using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProMISE2
{
	public partial class ColumnVis : Canvas
	{
		ControlParams controlparams = new ControlParams();

		public event MouseButtonEventHandler ColumnSelected;
		public event MouseButtonEventHandler FlowSelected;
		public event MouseButtonEventHandler InjectSelected;
		public event MouseButtonEventHandler GearSelected;

		bool overCol = false;
		bool overFlow = false;
		bool overInject = false;
		bool overAdv = false;
		bool overBack = false;
		bool showSelectable = false;
		bool selected = false;
		bool mouseOverChanging = false;

		double w, h;
		float xyratio = 4;

		SolidColorBrush upBrush;
		SolidColorBrush lpBrush;
		LinearGradientBrush phaseBrush;
		LinearGradientBrush insertBrush;
		LinearGradientBrush mobBrush;
		float defThickness = 0.01f;
		float selThickness = 0.02f;
		float defvs, defvw, defvh, colvx, colvy, colvw, colvh;
		float vxdeadin, vxdeadout;

		SolidColorBrush selectableLine = new SolidColorBrush(Color.FromScRgb(0.5f, 0.25f, 0.25f, 0.25f));
		SolidColorBrush selectableFill = new SolidColorBrush(Color.FromScRgb(0.5f, 0.5f, 0.5f, 0.5f));
		SolidColorBrush hiliteLine = new SolidColorBrush(Color.FromScRgb(0.5f, 1, 0, 0));
		SolidColorBrush hiliteFill = new SolidColorBrush(Color.FromScRgb(0.5f, 1, 0.5f, 0.5f));

		public ColumnVis()
		{
			InitializeComponent();

			defvs = 0.2f;
			defvh = defvs;
			defvw = defvh / xyratio;
		}

		public void updateControlParams(ControlParams controlparams)
		{
			this.controlparams = controlparams;

			colvx = 2 * defvw;
			colvw = 1 - 4 * defvw;

			if (controlparams.profile == ProfileType.CCC)
			{
				colvy = defvh;
				colvh = 1 - defvh;
			}
			else if (controlparams.profile == ProfileType.CCD || controlparams.profile == ProfileType.ToroidalCCC)
			{
				colvy = defvh + defvs;
				colvh = 1 - defvh - defvs;
			}
			else
			{
				colvy = defvh + defvs / 2;
				colvh = 1 - defvh - defvs;
			}
		}

		public void update()
		{
			showSelectable = ((overBack || overCol || overFlow || overInject || overAdv) && !selected);
			redrawInit();
			switch (controlparams.profile)
			{
				case ProfileType.CCD: redrawCCD(); break;
				case ProfileType.CCC: redrawCCC(); break;
				case ProfileType.ToroidalCCC: redrawToroidalCCC(); break;
				case ProfileType.DroppletCCC: redrawDroppletCCC(); break;
				case ProfileType.CPC: redrawCPC(); break;
				case ProfileType.VortexCCD: redrawVortexCCC(); break;
			}
			redrawCommon();
		}

		private void redrawInit()
		{
			vxdeadin = controlparams.vdeadIn / controlparams.vc2;
			vxdeadout = controlparams.vdeadOut / controlparams.vc2;

			upBrush = Brushes.Yellow;
			lpBrush = Brushes.Lime;
			phaseBrush = createPhaseBrush(PhaseType.Both, controlparams.uf);
			insertBrush = createPhaseBrush(controlparams.injectPhase);
			switch (controlparams.runMode)
			{
				case RunModeType.UpperPhase:
					mobBrush = createPhaseBrush(PhaseType.Upper);
					break;
				case RunModeType.LowerPhase:
					mobBrush = createPhaseBrush(PhaseType.Lower);
					break;
				default:
					mobBrush = createPhaseBrush(PhaseType.Both);
					break;
			}

			w = ActualWidth;
			h = ActualHeight;
			// lock aspect ratio
			if (w / h > xyratio)
			{
				w = h * xyratio;
			}
			else
			{
				h = w / xyratio;
			}

			Children.Clear();
		}

		private void redrawCommon()
		{
			UIElement injectarrow;
			float injpos;
			float injheight;

			Rectangle backRect = new Rectangle();
			backRect.Width = w;
			backRect.Height = h;
			backRect.Fill = Brushes.Transparent;
			backRect.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(back_Over);
			Children.Add(backRect);

			// flow arrows
			float flowarrowh = colvy + (colvh - defvs) / 2;
			Children.Add(createHArrow(0, flowarrowh, 2 * defvw, defvs, Brushes.Blue, defThickness));

			UIElement flowHilite = createVHiliteRect(0, flowarrowh, 2 * defvw, defvs, showSelectable, overFlow);
			flowHilite.MouseDown += new MouseButtonEventHandler(flow_Click);
			flowHilite.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(flow_Over);
			ToolTipService.SetToolTip(flowHilite, "Flow");
			Children.Add(flowHilite);

			if (controlparams.runMode == RunModeType.DualMode || controlparams.runMode == RunModeType.Intermittent)
			{
				Children.Add(createHArrow(1, flowarrowh, -2 * defvw, defvs, Brushes.Blue, defThickness));
				flowHilite = createVHiliteRect(1 - 2 * defvw, flowarrowh, 2 * defvw, defvs, showSelectable, overFlow);
				flowHilite.MouseDown += new MouseButtonEventHandler(flow_Click);
				flowHilite.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(flow_Over);
				ToolTipService.SetToolTip(flowHilite, "Flow");
				Children.Add(flowHilite);
			}

			injpos = colvx + controlparams.injectPos * controlparams.Vc / controlparams.vc2 * colvw;
			if (controlparams.vdeadInjectEnabled && controlparams.injectPos > 0 && controlparams.injectPos < 1)
			{
				injpos += (controlparams.vdeadInject / 2) / controlparams.vc2 * colvw;
			}
			if (controlparams.vdeadInEnabled && controlparams.injectPos > 0)
			{
				injpos += controlparams.vdeadIn / controlparams.vc2 * colvw;
			}

			// inject tube
			float tubevh = defvh;
			float tubevw = tubevh / 2 / xyratio;
			UIElement injecttube = createTestTube(injpos - tubevw / 2, 0, tubevw, tubevh, Brushes.Blue, defThickness, insertBrush);
			Children.Add(injecttube);

			// inject sample
			UIElement sample = createComp(injpos - tubevw / 2, 0, tubevw, tubevh, defThickness, insertBrush);
			Children.Add(sample);

			UIElement injectHilite = createVHiliteRect(injpos - tubevw / 2, 0, tubevw, tubevh, showSelectable, overInject);
			injectHilite.MouseDown += new MouseButtonEventHandler(inject_Click);
			injectHilite.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(inject_Over);
			ToolTipService.SetToolTip(injectHilite, "Inject");
			Children.Add(injectHilite);

			// gear
			float gearvw = defvs / 4;
			float gearwh = xyratio * gearvw;
			UIElement gear = createGear(1 - gearvw, 1 - gearwh, gearvw, gearwh, Brushes.Blue);
			Children.Add(gear);

			UIElement gearHilite = createVHiliteRect(1 - gearvw, 1 - gearwh, gearvw, gearwh, showSelectable, overAdv);
			gearHilite.MouseDown += new MouseButtonEventHandler(gear_Click);
			gearHilite.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(gear_Over);
			ToolTipService.SetToolTip(gearHilite, "Model");
			Children.Add(gearHilite);

			if (controlparams.profile == ProfileType.CCC)
			{
				injheight = 0.3f;
			}
			else if (controlparams.profile == ProfileType.CCD || controlparams.profile == ProfileType.ToroidalCCC)
			{
				injheight = 0.2f;
			}
			else
			{
				injheight = 0.1f;
			}
			injectarrow = createVArrow(injpos - 0.025f / 2, tubevh, 0.025f, injheight, Brushes.Black, 0.005f);
			injectarrow.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(back_Over);
			Children.Add(injectarrow);

			UIElement colSelRect = createVHiliteRect(colvx, colvy, colvw, colvh, showSelectable, overCol);
			colSelRect.MouseDown += new MouseButtonEventHandler(col_Click);
			colSelRect.IsMouseDirectlyOverChanged += new DependencyPropertyChangedEventHandler(col_Over);
			ToolTipService.SetToolTip(colSelRect, "Column");
			Children.Add(colSelRect);
		}

		void drawPhaseArrows(float vstart, float vend, float vlen)
		{
			// upper phase arrow
			if (controlparams.runMode != RunModeType.LowerPhase)
			{
				Children.Add(createPhaseArrow(vstart, colvy, vlen, controlparams.uf * colvh, controlparams.fu / controlparams.uf, darkerBrush(upBrush), 0.02f));
			}
			// lower phase arrow
			if (controlparams.runMode != RunModeType.UpperPhase)
			{
				if (controlparams.runMode == RunModeType.DualMode || controlparams.runMode == RunModeType.Intermittent)
				{
					Children.Add(createPhaseArrow(vend, colvy + controlparams.uf * colvh, -vlen, (1 - controlparams.uf) * colvh, controlparams.fl / controlparams.lf, darkerBrush(lpBrush), 0.02f));
				}
				else
				{
					Children.Add(createPhaseArrow(vstart, colvy + controlparams.uf * colvh, vlen, (1 - controlparams.uf) * colvh, controlparams.fl / controlparams.lf, darkerBrush(lpBrush), 0.02f));
				}
			}
		}

		private void redrawCCC()
		{
			// dead volume
			if (controlparams.vdeadInEnabled)
			{
				Children.Add(createVRect(colvx, colvy + (colvh - defvs) / 2, colvw * vxdeadin, defvs, Brushes.Blue, defThickness, mobBrush));
			}
			if (controlparams.vdeadOutEnabled)
			{
				Children.Add(createVRect(colvx + colvw * (1 - vxdeadout), colvy + (colvh - defvs) / 2, colvw * vxdeadout, defvs, Brushes.Blue, defThickness, mobBrush));
			}
			// main column
			if (controlparams.injectPos == 0 || controlparams.injectPos == 1)
			{
				// draw a single column
				float colstart = colvx;
				float colend = colvx + colvw;
				if (controlparams.vdeadInEnabled)
				{
					colstart += colvw * vxdeadin;
				}
				if (controlparams.vdeadOutEnabled)
				{
					colend -= colvw * vxdeadout;
				}
				float collen = colvw * controlparams.vc / controlparams.vc2;
				// column + phases
				Children.Add(createVRect(colstart, colvy, collen, colvh, Brushes.Blue, defThickness, phaseBrush));

				drawPhaseArrows(colstart, colend, collen);
			}
			else
			{
				// draw 2 'columns' in series
				float collen1 = colvw * controlparams.injectPos * controlparams.vc / controlparams.vc2;
				float collen2 = colvw * (1 - controlparams.injectPos) * controlparams.vc / controlparams.vc2;
				float colstart1 = colvx;
				float colend2 = colvx + colvw;
				if (controlparams.vdeadInEnabled)
				{
					colstart1 += colvw * vxdeadin;
				}
				if (controlparams.vdeadOutEnabled)
				{
					colend2 -= colvw * vxdeadout;
				}
				float colend1 = colstart1 + collen1;
				float colstart2 = colend2 - collen2;
				if (controlparams.vdeadInjectEnabled)
				{
					Children.Add(createVRect(colend1, colvy + (colvh - defvs) / 2, colstart2 - colend1, defvs, Brushes.Blue, defThickness, mobBrush));
				}
				// column + phases
				Children.Add(createVRect(colstart1, colvy, collen1, colvh, Brushes.Blue, defThickness, phaseBrush));
				Children.Add(createVRect(colstart2, colvy, collen2, colvh, Brushes.Blue, defThickness, phaseBrush));

				drawPhaseArrows(colstart1, colend1, collen1);
				drawPhaseArrows(colstart2, colend2, collen2);
			}
		}

		private void redrawCPC()
		{
			float tubevh = colvh;
			float ductvh = tubevh + defvs;
			float tubevw = tubevh / xyratio / 2;
			float ductvx = 0;
			float tubevy = colvy - defvs / 2;
			float dx = colvw / 7.75f;

			float colstart = colvx;
			float colend = colvx + colvw;
			if (controlparams.vdeadInEnabled)
			{
				colstart += colvw * vxdeadin;
			}
			if (controlparams.vdeadOutEnabled)
			{
				colend -= colvw * vxdeadout;
			}
			float collen = colvw * controlparams.vc / controlparams.vc2;

			redrawInit();

			for (float x = 0; x < colvw; x += dx)
			{
				Children.Add(createVRoundRect(colvx + x, colvy + (colvh - tubevh) / 2, tubevw, tubevh, Brushes.Blue, defThickness, phaseBrush));
				if (x > 0)
				{
					Children.Add(createFullDuct(ductvx, tubevy, dx, tubevh, ductvh, Brushes.Blue, defThickness, mobBrush));
				}
				ductvx = colvx + x + tubevw / 2;
			}

			drawPhaseArrows(colstart, colend, collen);
		}

		private void redrawCCD()
		{
			float tubevh = colvh;
			float tubevw = tubevh / xyratio / 2;
			float colstart = colvx;
			float colend = colvx + colvw;
			if (controlparams.vdeadInEnabled)
			{
				colstart += colvw * vxdeadin;
			}
			if (controlparams.vdeadOutEnabled)
			{
				colend -= colvw * vxdeadout;
			}
			float collen = colvw * controlparams.vc / controlparams.vc2;

			redrawInit();

			for (float x = 0; x < colvw; x += colvw / 7.75f)
			{
				Children.Add(createTestTube(colvx + x, colvy + (colvh - tubevh) / 2, tubevw, tubevh, Brushes.Blue, defThickness, phaseBrush));
			}

			drawPhaseArrows(colstart, colend, collen);
		}

		private void redrawToroidalCCC()
		{
			float tubevh = colvh;
			float tubevw = tubevh / xyratio / 4;
			float colstart = colvx;
			float colend = colvx + colvw;
			float y1, y2, dx;
			if (controlparams.vdeadInEnabled)
			{
				colstart += colvw * vxdeadin;
			}
			if (controlparams.vdeadOutEnabled)
			{
				colend -= colvw * vxdeadout;
			}
			float collen = colvw * controlparams.vc / controlparams.vc2;

			redrawInit();

			y1 = colvy;
			y2 = y1 + tubevh;
			dx = colvw / 8.75f;

			for (float x = 0; x < colvw - dx; x += dx)
			{
				Children.Add(createCoil(colvx + x + dx, y1, colvx + x + dx / 2, y2, tubevw, true, Brushes.Blue, defThickness, phaseBrush));
			}
			for (float x = 0; x < colvw - dx; x += dx)
			{
				Children.Add(createCoil(colvx + x, y1, colvx + x + dx / 2, y2, tubevw, true, Brushes.Blue, defThickness, phaseBrush));
			}

			drawPhaseArrows(colstart, colend, collen);
		}

		private void redrawVortexCCC()
		{
			float tubevh = colvh;
			float ductdvh = defvs;
			float tubevw = tubevh / xyratio / 2;
			float ductvx = 0;
			float tubevy = colvy - defvs / 2;
			float dx = colvw / 7.75f;
			float vh;
			bool ductUp = false;

			float colstart = colvx;
			float colend = colvx + colvw;
			if (controlparams.vdeadInEnabled)
			{
				colstart += colvw * vxdeadin;
			}
			if (controlparams.vdeadOutEnabled)
			{
				colend -= colvw * vxdeadout;
			}
			float collen = colvw * controlparams.vc / controlparams.vc2;

			redrawInit();

			for (float x = 0; x < colvw; x += dx)
			{
				Children.Add(createVRect(colvx + x, colvy + (colvh - tubevh) / 2, tubevw, tubevh, Brushes.Blue, defThickness, phaseBrush));
				if (x > 0)
				{
					if (ductUp)
					{
						vh = tubevy - ductdvh;
					}
					else
					{
						vh = tubevh;
					}
					Children.Add(createShortDuct(ductvx, tubevy, dx, vh, ductdvh, ductUp, Brushes.Blue, defThickness, mobBrush));
					ductUp = !ductUp;
				}
				ductvx = colvx + x + tubevw / 2;
			}

			drawPhaseArrows(colstart, colend, collen);
		}

		private void redrawDroppletCCC()
		{
			float tubevh = colvh;
			float ductvh = tubevh + defvs;
			float tubevw = tubevh / xyratio / 6;
			float ductvx = 0;
			float tubevy = colvy - defvs / 2;
			float dx = colvw / 15.75f;

			float colstart = colvx;
			float colend = colvx + colvw;
			if (controlparams.vdeadInEnabled)
			{
				colstart += colvw * vxdeadin;
			}
			if (controlparams.vdeadOutEnabled)
			{
				colend -= colvw * vxdeadout;
			}
			float collen = colvw * controlparams.vc / controlparams.vc2;

			redrawInit();

			for (float x = 0; x < colvw; x += dx)
			{
				Children.Add(createVRect(colvx + x, colvy + (colvh - tubevh) / 2, tubevw, tubevh, Brushes.Blue, defThickness, phaseBrush));
				if (x > 0)
				{
					Children.Add(createFullDuct(ductvx, tubevy, dx, tubevh, ductvh, Brushes.Blue, defThickness, mobBrush));
				}
				ductvx = colvx + x + tubevw / 2;
			}

			drawPhaseArrows(colstart, colend, collen);
		}

		UIElement createVRect(float vx, float vy, float vw, float vh, Brush stroke, float vthick, Brush fill)
		{
			Rectangle rect = new Rectangle();
			float thick = vthick * (float)Math.Sqrt(w * h);

			rect.Width = convVSizeX(vw);
			rect.Height = convVSizeY(vh);
			SetLeft(rect, convVX(vx));
			SetTop(rect, convVY(vy));

			if (stroke != null)
			{
				rect.Stroke = stroke;
			}
			if (thick > 0)
			{
				rect.StrokeThickness = thick;
			}
			rect.Fill = fill;
			return rect;
		}

		UIElement createVRoundRect(float vx, float vy, float vw, float vh, Brush stroke, float vthick, Brush fill)
		{
			Rectangle rect = new Rectangle();
			float thick = vthick * (float)Math.Sqrt(w * h);
			float rad = (float)convVSizeX(vw / 2) / 2;

			rect.Width = convVSizeX(vw);
			rect.Height = convVSizeY(vh);
			SetLeft(rect, convVX(vx));
			SetTop(rect, convVY(vy));

			rect.RadiusX = rad;
			rect.RadiusY = rad;

			if (stroke != null)
			{
				rect.Stroke = stroke;
			}
			if (thick > 0)
			{
				rect.StrokeThickness = thick;
			}
			rect.Fill = fill;
			return rect;
		}

		UIElement createVHiliteRect(float vx, float vy, float vw, float vh, bool mark, bool hilite)
		{
			Rectangle rect = new Rectangle();
			float thick = selThickness * (float)Math.Sqrt(w * h);
			float rad = (float)(w / 40);

			rect.Width = convVSizeX(vw) + thick;
			rect.Height = convVSizeY(vh) + thick;
			SetLeft(rect, convVX(vx) - thick / 2);
			SetTop(rect, convVY(vy) - thick / 2);

			rect.RadiusX = rad;
			rect.RadiusY = rad;
			if (hilite)
			{
				rect.Stroke = hiliteLine;
				rect.Fill = hiliteFill;
			}
			else if (mark)
			{
				rect.Stroke = selectableLine;
				rect.Fill = selectableFill;
			}
			else
			{
				rect.Fill = Brushes.Transparent;
			}

			rect.StrokeThickness = thick;
			return rect;
		}

		UIElement createPhaseArrow(float vx, float vy, float vw, float vh, float size, Brush stroke, float vthick)
		{
			float vh2 = vh / 2;
			float vy2 = vy + (vh - vh2) / 2;
			float size2 = 1 / (1 + 1 / (size / Math.Max(controlparams.fu, controlparams.fl)));
			float vw2 = vw * size2;
			float vx2 = vx + (vw - vw2) / 2;

			return createHArrow(vx2, vy2, vw2, vh2, stroke, vthick);
		}

		UIElement createHArrow(float vx, float vy, float vw, float vh, Brush stroke, float vthick)
		{
			Path path = new Path();
			float vhy = vy + vh / 2;
			float thick = vthick * (float)Math.Sqrt(w * h);

			if (stroke != null)
			{
				path.Stroke = stroke;
			}
			path.StrokeThickness = thick;

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				ctx.BeginFigure(new Point(convVX(vx), convVY(vhy)), true, false);
				ctx.LineTo(new Point(convVX(vx + vw), convVY(vhy)), true, true);
				ctx.LineTo(new Point(convVX(vx + 0.75f * vw), convVY(vy)), true, true);
				ctx.BeginFigure(new Point(convVX(vx + vw), convVY(vhy)), true, false);
				ctx.LineTo(new Point(convVX(vx + 0.75f * vw), convVY(vy + vh)), true, true);
			}
			geometry.Freeze();
			path.Data = geometry;
			return path;
		}

		UIElement createVArrow(float vx, float vy, float vw, float vh, Brush stroke, float vthick)
		{
			Path path = new Path();
			float vhx = vx + vw / 2;
			float thick = vthick * (float)Math.Sqrt(w * h);

			path.Stroke = stroke;
			path.StrokeThickness = thick;

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				ctx.BeginFigure(new Point(convVX(vhx), convVY(vy)), true, false);
				ctx.LineTo(new Point(convVX(vhx), convVY(vy + vh)), true, true);
				ctx.LineTo(new Point(convVX(vx), convVY(vy + 0.75f * vh)), true, true);
				ctx.BeginFigure(new Point(convVX(vhx), convVY(vy + vh)), true, false);
				ctx.LineTo(new Point(convVX(vx + vw), convVY(vy + 0.75f * vh)), true, true);
			}
			geometry.Freeze();
			path.Data = geometry;
			return path;
		}

		UIElement createTestTube(float vx, float vy, float vw, float vh, Brush stroke, float vthick, Brush fill)
		{
			Path path = new Path();
			float thick = vthick * (float)Math.Sqrt(w * h);
			float rad = (float)convVSizeX(vw / 2);

			if (stroke != null)
			{
				path.Stroke = stroke;
			}
			path.Fill = fill;
			path.StrokeThickness = thick;

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				ctx.BeginFigure(new Point(convVX(vx), convVY(vy)), true, false);
				ctx.LineTo(new Point(convVX(vx), convVY(vy + vh) - rad), true, true);
				ctx.ArcTo(new Point(convVX(vx + vw), convVY(vy + vh) - rad), new Size(rad, rad), 0, true, SweepDirection.Counterclockwise, true, true);
				ctx.LineTo(new Point(convVX(vx + vw), convVY(vy)), true, true);
			}
			geometry.Freeze();
			path.Data = geometry;
			return path;
		}

		UIElement createCoil(float vx1, float vy1, float vx2, float vy2, float vw, bool dirDown, Brush stroke, float vthick, Brush fill)
		{
			Path path = new Path();
			float thick = vthick * (float)Math.Sqrt(w * h);
			float rad = (float)convVSizeX(vw / 2);
			float y1 = (float)convVY(vy1) + rad;
			float y2 = (float)convVY(vy2) - rad;

			if (stroke != null)
			{
				path.Stroke = stroke;
			}
			path.Fill = fill;
			path.StrokeThickness = thick;

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				ctx.BeginFigure(new Point(convVX(vx1), y1), true, false);
				ctx.LineTo(new Point(convVX(vx2), y2), true, true);
				ctx.ArcTo(new Point(convVX(vx2 + vw), y2), new Size(rad, rad), 0, true, SweepDirection.Counterclockwise, true, true);
				ctx.LineTo(new Point(convVX(vx1 + vw), y1), true, true);
				ctx.ArcTo(new Point(convVX(vx1), y1), new Size(rad, rad), 0, true, SweepDirection.Counterclockwise, true, true);
			}
			geometry.Freeze();
			path.Data = geometry;
			return path;
		}

		UIElement createComp(float vx, float vy, float vw, float vh, float vthick, Brush fill)
		{
			Ellipse ellipse = new Ellipse();
			float rad = vw * 0.75f;
			int n = controlparams.comps.Count;
			CMYKa cmyka = new CMYKa();
			float c = 0;
			float m = 0;
			float y = 0;
			float k = 0;
			float a = 0;

			ellipse.Width = convVSizeX(rad);
			ellipse.Height = convVSizeX(rad);
			SetLeft(ellipse, convVX(vx + (vw - rad) / 2));
			SetTop(ellipse, convVY(vy + vh / 2) - convVSizeX(rad / 2));

			if (n > 0)
			{
				for (int i = 0; i < n; i++)
				{
					cmyka = Util.ColorToCMYK(Util.colorRange(i, n));
					c += cmyka.c;
					m += cmyka.m;
					y += cmyka.y;
					k += cmyka.k;
					a += cmyka.a;
				}
				cmyka.c = c / n;
				cmyka.m = m / n;
				cmyka.y = y / n;
				cmyka.k = k / n + n / 10.0f;		// make darker
				cmyka.a = a / n;
				ellipse.Fill = new SolidColorBrush(Util.CMYKtoColor(cmyka));
			}
			return ellipse;
		}

		UIElement createFullDuct(float vx, float vy, float vw, float vh1, float vh2, Brush stroke, float vthick, Brush fill)
		{
			Path path = new Path();
			float thick = vthick * (float)Math.Sqrt(w * h);
			float dvh = vh2 - vh1;
			float radx = (vw / 2) / 2;
			float rady = (dvh / 2) / 2;
			float vx1, vy1, vx2, vy2;

			if (stroke != null)
			{
				path.Stroke = stroke;
			}
			path.Fill = fill;
			path.StrokeThickness = thick;

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				vx1 = vx;
				vy1 = (vy + vh1 + dvh / 2);
				vx2 = vx + vw / 2;
				vy2 = vy1;
				ctx.BeginFigure(new Point(convVX(vx1), convVY(vy1)), false, false);
				ctx.ArcTo(new Point(convVX(vx2), convVY(vy2)), new Size(convVSizeX(radx), convVSizeY(rady)), 0, true, SweepDirection.Counterclockwise, true, true);

				vx1 = vx2;
				vy1 = vy + dvh / 2;
				vx2 = vx + vw;
				vy2 = vy1;
				ctx.LineTo(new Point(convVX(vx1), convVY(vy1)), true, true);
				ctx.ArcTo(new Point(convVX(vx2), convVY(vy2)), new Size(convVSizeX(radx), convVSizeY(rady)), 0, true, SweepDirection.Clockwise, true, true);
			}
			geometry.Freeze();
			path.Data = geometry;
			return path;
		}

		UIElement createShortDuct(float vx, float vy, float vw, float vh, float dvh, bool up, Brush stroke, float vthick, Brush fill)
		{
			Path path = new Path();
			float thick = vthick * (float)Math.Sqrt(w * h);
			float radx = (vw / 2) / 2;
			float rady = (dvh / 2) / 2;
			float vx1, vy1, vx2, vy2;
			SweepDirection dir;

			if (stroke != null)
			{
				path.Stroke = stroke;
			}
			path.Fill = fill;
			path.StrokeThickness = thick;

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				vx1 = vx;
				vy1 = (vy + vh + dvh / 2);
				vx2 = vx + vw;
				vy2 = vy1;
				ctx.BeginFigure(new Point(convVX(vx1), convVY(vy1)), false, false);
				if (up)
				{
					dir = SweepDirection.Clockwise;
				}
				else
				{
					dir = SweepDirection.Counterclockwise;
				}
				ctx.ArcTo(new Point(convVX(vx2), convVY(vy2)), new Size(convVSizeX(radx), convVSizeY(rady)), 0, true, dir, true, true);
			}
			geometry.Freeze();
			path.Data = geometry;
			return path;
		}

		UIElement createGear(float vx, float vy, float vw, float vh, Brush fill)
		{
			Path path = new Path();
			float thick = 0.005f * (float)Math.Sqrt(w * h);
			float rad = (float)convVSizeX(vw / 2);
			float irad = 0.75f * rad;
			float srad = 0.5f * irad;
			double x1, y1, x2, y2;
			bool inward = true;
			int gears = 8;
			double gearstep = 2 * Math.PI / (gears * 2);
			double aoffset = gearstep / 2;

			path.Fill = fill;
			path.Stroke = fill;
			if (thick > 0)
			{
				path.StrokeThickness = thick;
			}

			StreamGeometry geometry = new StreamGeometry();
			using (StreamGeometryContext ctx = geometry.Open())
			{
				ctx.BeginFigure(new Point(convVX(vx + vw), convVY(vy + vh / 2)), true, true);

				for (double a = 0; a < 2 * Math.PI; a += gearstep)
				{
					x1 = convVX(vx) + rad * (1 + Math.Cos(a + aoffset));
					y1 = convVY(vy) + rad * (1 + Math.Sin(a + aoffset));
					x2 = convVX(vx) + (rad - irad) + irad * (1 + Math.Cos(a + aoffset));
					y2 = convVY(vy) + (rad - irad) + irad * (1 + Math.Sin(a + aoffset));
					if (inward)
					{
						ctx.LineTo(new Point(x1, y1), true, true);
						ctx.LineTo(new Point(x2, y2), true, true);
					}
					else
					{
						ctx.LineTo(new Point(x2, y2), true, true);
						ctx.LineTo(new Point(x1, y1), true, true);
					}
					inward = !inward;
				}
			}
			geometry.Freeze();

			EllipseGeometry circleGeometry = new EllipseGeometry(new Point(convVX(vx + vw / 2), convVY(vy + vh / 2)), srad, srad);

			GeometryGroup geometryGroup = new GeometryGroup();
			geometryGroup.Children.Add(geometry);
			geometryGroup.Children.Add(circleGeometry);

			path.Data = geometryGroup;
			return path;
		}

		void addText(string s, double vx, double vy, double vw, double vh, Brush color)
		{
			Viewbox viewBox = new Viewbox();
			TextBlock text = new TextBlock(new Run(s));
			double x = convVX(vx);
			double y = convVY(vy);

			text.Foreground = color;
			text.FontWeight = FontWeights.Bold;

			viewBox.Width = convVSizeX(vw);
			viewBox.Height = convVSizeY(vh);
			viewBox.Child = text;

			Canvas.SetLeft(viewBox, x);
			Canvas.SetTop(viewBox, y);
			Children.Add(viewBox);
		}

		LinearGradientBrush createPhaseBrush(PhaseType phase, float part = 0.5f)
		{
			LinearGradientBrush phaseBrush = new LinearGradientBrush();
			Color upColor = Colors.Yellow;
			Color lpColor = Colors.Lime;

			phaseBrush.StartPoint = new Point(0.5, 0);
			phaseBrush.EndPoint = new Point(0.5, 1);

			switch (phase)
			{
				case PhaseType.Upper:
					phaseBrush.GradientStops.Add(new GradientStop(upColor, 0));
					break;
				case PhaseType.Lower:
					phaseBrush.GradientStops.Add(new GradientStop(lpColor, 0));
					break;
				case PhaseType.Both:
					phaseBrush.GradientStops.Add(new GradientStop(upColor, 0));
					phaseBrush.GradientStops.Add(new GradientStop(upColor, part));
					phaseBrush.GradientStops.Add(new GradientStop(lpColor, part));
					phaseBrush.GradientStops.Add(new GradientStop(lpColor, 1));
					break;
			}
			return phaseBrush;
		}

		SolidColorBrush darkerBrush(SolidColorBrush brush0)
		{
			SolidColorBrush brush = brush0.Clone();
			float r = brush.Color.ScR / 2;
			float g = brush.Color.ScG / 2;
			float b = brush.Color.ScB / 2;
			brush.Color = Color.FromScRgb(0.75f, r, g, b);
			return brush;
		}

		private void back_Over(object sender, DependencyPropertyChangedEventArgs e)
		{
			overBack = (bool)e.NewValue;
			overCol = false;
			overFlow = false;
			overInject = false;
			overAdv = false;
			update();
		}

		private void col_Click(object sender, MouseButtonEventArgs e)
		{
			mouseOverChanging = true;
			if (ColumnSelected != null)
			{
				selected = true;
				update();
				ColumnSelected(this, e);
				selected = false;
			}
			overCol = false;
			update();
		}

		private void col_Over(object sender, DependencyPropertyChangedEventArgs e)
		{
			bool hilite = (bool)e.NewValue;
			if (mouseOverChanging)
			{
				mouseOverChanging = false;
			}
			else if (overCol != hilite)
			{
				overBack = false;
				overCol = hilite;
				overFlow = false;
				overInject = false;
				overAdv = false;
				if (hilite)
				{
					mouseOverChanging = true;
				}
				update();
			}
		}

		private void flow_Click(object sender, MouseButtonEventArgs e)
		{
			mouseOverChanging = true;
			if (FlowSelected != null)
			{
				selected = true;
				update();
				FlowSelected(this, e);
				selected = false;
			}
			overFlow = false;
			update();
		}

		private void flow_Over(object sender, DependencyPropertyChangedEventArgs e)
		{
			bool hilite = (bool)e.NewValue;
			if (mouseOverChanging)
			{
				mouseOverChanging = false;
			}
			else if (overFlow != hilite)
			{
				overBack = false;
				overCol = false;
				overFlow = hilite;
				overInject = false;
				overAdv = false;
				if (hilite)
				{
					mouseOverChanging = true;
				}
				update();
			}
		}

		private void inject_Click(object sender, MouseButtonEventArgs e)
		{
			mouseOverChanging = true;
			if (InjectSelected != null)
			{
				selected = true;
				update();
				InjectSelected(this, e);
				selected = false;
			}
			overInject = false;
			update();
		}

		private void inject_Over(object sender, DependencyPropertyChangedEventArgs e)
		{
			bool hilite = (bool)e.NewValue;
			if (mouseOverChanging)
			{
				mouseOverChanging = false;
			}
			else if (overInject != hilite)
			{
				overBack = false;
				overCol = false;
				overFlow = false;
				overInject = hilite;
				overAdv = false;
				if (hilite)
				{
					mouseOverChanging = true;
				}
				update();
			}
		}

		private void gear_Click(object sender, MouseButtonEventArgs e)
		{
			mouseOverChanging = true;
			if (GearSelected != null)
			{
				selected = true;
				update();
				GearSelected(this, e);
				selected = false;
			}
			overAdv = false;
			update();
		}

		private void gear_Over(object sender, DependencyPropertyChangedEventArgs e)
		{
			bool hilite = (bool)e.NewValue;
			if (mouseOverChanging)
			{
				mouseOverChanging = false;
			}
			else if (overAdv != hilite)
			{
				overBack = false;
				overCol = false;
				overFlow = false;
				overInject = false;
				overAdv = hilite;
				if (hilite)
				{
					mouseOverChanging = true;
				}
				update();
			}
		}

		private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			update();
		}

		double convVSizeX(double vx)
		{
			return vx * (1 - selThickness) * w;
		}

		double convVX(double vx)
		{
			return (selThickness / 2 + vx) * (1 - selThickness) * w;
		}

		double convVSizeY(double vy)
		{
			return vy * (1 - selThickness) * h;
		}

		double convVY(double vy)
		{
			return (selThickness / 2 + vy) * (1 - selThickness) * h;
		}

	}
}
