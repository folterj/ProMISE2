using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Promise2
{
	public class Params : InParams, INotifyPropertyChanged
	{
		ArrayList observers;

		public float vc2 { get; set; }
		public float Vu { get; set; }
		public float Vl { get; set; }
		public float Px { get; set; }

		public RunModeType RunMode
		{
			get { return runMode; }
			set
			{
				runMode = value;
				NotifyPropertyChanged("RunMode");
				update();
			}
		}

		public float Vc
		{
			get { return vc; }
			set
			{
				if (value <= 0)
					throw new Exception("must be greater than zero");
				vc = value;
				NotifyPropertyChanged("Vc");
				update();
			}
		}

		public float Uf
		{
			get { return uf; }
			set { uf = value; NotifyPropertyChanged("Uf"); updateFromUf(); }
		}

		public float Lf
		{
			get { return lf; }
			set { lf = value; NotifyPropertyChanged("Lf"); updateFromLf(); }
		}

		public Params()
		{
			observers = new ArrayList();
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void NotifyPropertyChanged(string sProp)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(sProp));
			}
		}

		public bool updateFromUf()
		{
			float newlf = 1 - uf;
			if (newlf != lf)
			{
				lf = newlf;
				NotifyPropertyChanged("Lf");
				update();
				return true;
			}
			return false;
		}

		public bool updateFromLf()
		{
			float newuf = 1 - lf;
			if (newuf != uf)
			{
				uf = newuf;
				NotifyPropertyChanged("Uf");
				update();
				return true;
			}
			return false;
		}

		public void update()
		{
			Vu = uf * vc;
			NotifyPropertyChanged("Vu");
			Vl = lf * vc;
			NotifyPropertyChanged("Vl");
			Px = uf / lf;
			NotifyPropertyChanged("Px");

			vc2 = vc;
			if (vdeadinEnabled)
				vc2 += vdeadin;
			if (vdeadoutEnabled)
				vc2 += vdeadout;
			if (vdeadinsertEnabled)	// && insertpos != 0,1
				vc2 += vdeadinsert;

			updateObservers();
		}

		public void registerObserver(InparamsObserver observer)
		{
			observers.Add(observer);
		}

		public void unregisterObserver(InparamsObserver observer)
		{
			observers.Remove(observer);
		}

		void updateObservers()
		{
			// pass imparams with update: explicit update of visual control
			for (int i = 0; i < observers.Count; i++)
			{
				((InparamsObserver)observers[i]).update(this);
			}
		}

	}
}
