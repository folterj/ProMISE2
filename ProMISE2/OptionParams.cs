using System;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;

namespace ProMISE2
{
	[Serializable]
	public class OptionParams : INotifyPropertyChanged
	{
		public int timeStores { get; set; }
		public float exportDpi { get; set; }
		public float cflConstant { get; set; }

		[XmlIgnore]
		public string filePath;

		public OptionParams()
		{
			reset();
			setPath();
		}

        public OptionParams(OptionParams optionparams)
        {
            timeStores = optionparams.timeStores;
            exportDpi = optionparams.exportDpi;
            cflConstant = optionparams.cflConstant;
			setPath();
        }
        
        public void reset()
		{
			timeStores = 100;
			exportDpi = 300;
			cflConstant = 0.1f;

			NotifyPropertyChanged("timeStores");
			NotifyPropertyChanged("exportDpi");
			NotifyPropertyChanged("testMaxErrors");
			NotifyPropertyChanged("cflConstant");
			NotifyPropertyChanged("fullMassTransfer");
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string sProp)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(sProp));
			}
		}

		void setPath()
		{
			filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\ProMISE2";
			// automatically creates (complete) path if not exists
			Directory.CreateDirectory(filePath);
			filePath += "\\options.xml";
		}

		public void save()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(OptionParams));
			serializer.Serialize(new StreamWriter(filePath), this);
		}

		public static bool operator ==(OptionParams params1, OptionParams params2)
		{
			return (params1.timeStores == params2.timeStores &&
					params1.exportDpi == params2.exportDpi &&
					params1.cflConstant == params2.cflConstant);
		}

		public static bool operator !=(OptionParams params1, OptionParams params2)
		{
			return !(params1 == params2);
		}

		public override bool Equals(Object o)
		{
			return (this == (OptionParams)o);
		}

		public override int GetHashCode()
		{
			return 0;
		}
		
	}
}
