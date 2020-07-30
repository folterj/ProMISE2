using System;
using System.Collections.Generic;

namespace ProMISE2
{
	public class Axes
	{
		public float rangex = 0;
		public float rangexu = 0;
		public float rangexl = 0;
		public List<float> maxcon = new List<float>();
		public float scaleminu = 0;
		public float scalemaxu = 0;
		public float scaleminl = 0;
		public float scalemaxl = 0;
		public float scaleminulabel = 0;
		public float scalemaxulabel = 0;
		public float scaleminllabel = 0;
		public float scalemaxllabel = 0;
		public float colstart = 0;
		public float colend = 0;
		public float deadvolstart = 0;
		public float deadvolend = 0;
		public float deadvolinjectstart = 0;
		public float deadvolinjectend = 0;
		public bool showCol = false;
		public bool showDeadvolstart = false;
		public bool showDeadvolend = false;
		public bool showDeadvolinsert = false;
		public bool logScale = false;

		public Axes()
		{
		}

		public void update()
		{
			float tempmin;
			float tempmax;
			// make sure: maxlabel > minlabel
			if (scalemaxulabel < scaleminulabel)
			{
				tempmin = scaleminulabel;
				tempmax = scalemaxulabel;
				scaleminulabel = tempmax;
				scalemaxulabel = tempmin;

				tempmin = scaleminu;
				tempmax = scalemaxu;
				scaleminu = tempmax;
				scalemaxu = tempmin;
			}
			if (scalemaxllabel < scaleminllabel)
			{
				tempmin = scaleminllabel;
				tempmax = scalemaxllabel;
				scaleminllabel = tempmax;
				scalemaxllabel = tempmin;

				tempmin = scaleminl;
				tempmax = scalemaxl;
				scaleminl = tempmax;
				scalemaxl = tempmin;
			}
			rangexu = rangex;
			rangexl = rangex;
		}

		public void sync(float scaleu, float scalel)
		{
			scaleu /= (float)Math.Abs(scalemaxu - scaleminu);
			scalel /= (float)Math.Abs(scalemaxl - scaleminl);

			if (scaleu > scalel)
			{
				rangexu = rangex;
				rangexl = rangex * (scaleu / scalel);
			}
			else
			{
				rangexu = rangex * (scalel / scaleu);
				rangexl = rangex;
			}
		}

	}
}
