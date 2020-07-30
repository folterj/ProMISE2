
namespace ProMISE2
{
	public class TextParam
	{
		public string name = "";
		public string value = "";
		public string units = "";
		public bool isHeader = false;

		public TextParam()
		{
		}

		public TextParam(string name, string value, string units)
		{
			this.name = name;
			this.value = value;
			this.units = units;
		}

		public TextParam(string name, bool isHeader)
		{
			this.name = name;
			this.isHeader = isHeader;
		}

	}
}
