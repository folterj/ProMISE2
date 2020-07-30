using System.Collections.Generic;

namespace ProMISE2
{
	public class TextParamList : List<TextParam>
	{
		public TextParamList()
			: base()
		{
		}

		public void add(string name, string value = "", string units = "")
		{
			if (units != "")
			{
				units = "[" + units + "]";
			}
			Add(new TextParam(name, value, units));
		}

		public void addHeader(string name)
		{
			Add(new TextParam(name, true));
		}

		public int getNLines()
		{
			int nlines = 0;

			foreach (TextParam param in this)
			{
				if (param.isHeader && nlines > 0)
				{
					nlines++;
				}
				nlines++;
			}
			return nlines;
		}

		public override string ToString()
		{
			string s = "";

			foreach (TextParam param in this)
			{
				if (param.isHeader)
				{
					s += "\n" + param.name;
				}
				else
				{
					s += param.name + "\t" + param.value;
					if (param.units != "")
					{
						s += "\t" + param.units;
					}
				}
				s += "\n";
			}
			return s;
		}
	}

}
