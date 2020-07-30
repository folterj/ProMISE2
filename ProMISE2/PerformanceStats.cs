using System;
using System.Diagnostics;

namespace ProMISE2
{
	public class PerformanceStats
	{
		Stopwatch sw;
		long modelTime;
		long outTime, visoutTime;
		long drawviewTime;
		long totalTime;
		bool modelTimeSet;
		bool outTimeSet, visoutTimeSet;
		bool drawviewTimeSet;
		bool totalTimeSet;

		public PerformanceStats()
		{
			sw = new Stopwatch();
			reset();
		}

		public void reset()
		{
			modelTime = 0;
			modelTimeSet = false;
			outTime = 0;
			outTimeSet = false;
			visoutTime = 0;
			visoutTimeSet = false;
			drawviewTime = 0;
			drawviewTimeSet = false;
			totalTime = 0;
			totalTimeSet = false;
		}

		public void update()
		{
			totalTime = modelTime + outTime + visoutTime + drawviewTime;
			totalTimeSet = true;
		}

		public void start()
		{
			sw.Reset();
			sw.Start();
		}

		public void stop()
		{
			sw.Stop();
		}

		public void storeModelTime()
		{
			stop();
			modelTime = sw.ElapsedMilliseconds;
			modelTimeSet = true;
			update();
		}

		public void storeOutTime()
		{
			stop();
			outTime = sw.ElapsedMilliseconds;
			outTimeSet = true;
			update();
		}

		public void storeVisoutTime()
		{
			stop();
			visoutTime = sw.ElapsedMilliseconds;
			visoutTimeSet = true;
			update();
		}

		public void storeDrawviewTime()
		{
			stop();
			drawviewTime = sw.ElapsedMilliseconds;
			drawviewTimeSet = true;
			update();
		}

		public String printf()
		{
			String s = "";

            if (modelTimeSet)
            {
                s += String.Format("Model: {0}\n", printTime(modelTime));
            }
            if (outTimeSet)
            {
                s += String.Format("Out: {0}\n", printTime(outTime));
            }
            if (visoutTimeSet)
            {
                s += String.Format("VisOut: {0}\n", printTime(visoutTime));
            }
            if (drawviewTimeSet)
            {
                s += String.Format("DrawView: {0}\n", printTime(drawviewTime));
            }
            if (totalTimeSet)
            {
                s += String.Format("Total: {0}\n", printTime(totalTime));
            }
            if (s == "")
            {
                s = "-";
            }
			return s;
		}

		public String printTime(long timems)
		{
            if (timems < 1000)
            {
                return String.Format("{0} ms", timems);
            }
            else
            {
                return String.Format("{0:F1} s", (float)timems / 1000);
            }
		}

	}
}
