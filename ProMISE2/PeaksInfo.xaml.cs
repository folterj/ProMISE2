using System;
using System.Windows;

namespace ProMISE2
{
	public partial class PeaksInfo : Window, ControlCompObserver
	{
		OutComp comp1, comp2;

		public PeaksInfo()
		{
			InitializeComponent();
		}

		public void updateParams(OutComp comp1, OutComp comp2)
		{
			this.comp1 = comp1;
			this.comp2 = comp2;

			updateControlComp();
		}

		public void updateControlComp()
		{
			float sel = comp1.k / comp2.k;
			float rs = Equations.calcRes(comp1, comp2);

			labelText.Text = String.Format("{0}, {1}", comp1.label, comp2.label);
			kText.Text = String.Format("{0}, {1}", comp1.k, comp2.k);
			selText.Text = String.Format("{0:0.000}", sel);
			if (rs != 0)
			{
				rsText.Text = String.Format("{0:0.000}", rs);
			}
			else
			{
				rsText.Text = "-";
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Hide();
			e.Cancel = true;
		}

	}
}
