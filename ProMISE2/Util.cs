using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ProMISE2
{
	public struct CMYKa
	{
		public float c, m, y, k, a;
	};

	class Util
	{
		public static bool isApprox(float a, float b, float margin)
		{
			// < 10% deviation
			return (calcMaxError(a, b) < margin);
		}

		public static float calcMaxError(float a, float b)
		{
			return (Math.Abs(a - b) / Math.Max(Math.Abs(a), Math.Abs(b)));
		}

		public static float calcError(float val, float norm)
		{
			return (Math.Abs(val - norm) / Math.Abs(norm));
		}

		public static double calcScale(double sourceWidth, double sourceHeight, double destWidth, double destHeight)
		{
			double widthScale = destWidth / sourceWidth;
			double heightScale = destHeight / sourceHeight;
			double scale = Math.Min(widthScale, heightScale);
			return scale;
		}

        public static string toString(float x, int maxdec)
		{
			string s = x.ToString();
			if (getNDecimals(s) > maxdec)
			{
				s = string.Format("{0:F" + maxdec + "}", x);
				s = float.Parse(s).ToString();
			}
			return s;
		}

		public static string toStringExp(float x, int maxdec)
		{
			string s = string.Format("{0:E}", x);
			float f = getMantissa(s);
			int exp = getExponent(s);
			s = toString(f, maxdec);
			if (exp != 0)
				s += "E" + exp.ToString();
			return s;
		}

		public static int getNDecimals(string s)
		{
			int dec = 0;
			int i = s.IndexOf('.');
			int e = s.IndexOf('E');
			if (i >= 0)
			{
				if (e < 0)
					e = s.Length;
				dec = e - i - 1;
			}
			return dec;
		}

		public static float getMantissa(string s)
		{
			string[] p = s.Split('E');
			return float.Parse(p[0]);
		}

		public static int getExponent(string s)
		{
			string[] p = s.Split('E');
			if (p.Length > 1)
				return int.Parse(p[1]);
			return 0;
		}

		public static double convertToDpi(double x, double destDpi, double sourceDpi = 96)
		{
			return x / sourceDpi * destDpi;
		}

		public static string escape(string s)
		{
			return s.Replace("<", "[").Replace(">", "]");
		}

		// (y2-y0) / (y1-y0) = (x2-x0) / (x1-x0)

		public static float calcCor(float x0, float x1, float y0, float y1, float x)
		{
			if (x0 != x1 && y0 != y1)
			{
				return y0 + (x - x0) / (x1 - x0) * (y1 - y0);
			}
			return y0;
		}

        public static float calcCorX(float x0, float x1, float y0, float y1, float y)
		{
			if (x0 != x1 && y0 != y1)
			{
				return x0 + (y - y0) / (y1 - y0) * (x1 - x0);
			}
			return x0;
		}

		public static float[] createFilter(float filtersigma)
		{
			float[] filterWeight = new float[(int)(2 * 4 * filtersigma + 1)];
			int i = 0;

			if (filterWeight.Length == 1)
			{
				filterWeight[0] = 1;
			}
			else
			{
				for (int x = -4 * (int)filtersigma; x <= 4 * filtersigma; x++)
				{
					filterWeight[i++] = (float)(1 / (Math.Sqrt(2 * Math.PI) * filtersigma) * Math.Exp(-Math.Pow(x, 2) / (2 * Math.Pow(filtersigma, 2))));
				}
			}
			return filterWeight;
		}

		public static Color colorRange(int coli, int totcol, float f = 1, float a = 1)
		{
			float r = 0;
			float g = 0;
			float b = 0;
			int range;
			float rangef;
			float col2;
			Color basecol = Colors.Black;

			if (totcol <= 2)
			{
				if (totcol <= 2 && coli == 0)
				{
					basecol = Colors.Red;
				}
				else if (totcol == 2 && coli == 1)
				{
					basecol = Colors.Blue;
				}
				r = basecol.ScR;
				g = basecol.ScG;
				b = basecol.ScB;
			}
			else
			{
				col2 = (float)coli / totcol * 6;
				range = (int)col2;
				rangef = col2 - range;
				switch (range)
				{
					case 0:
						r = 1;
						g = rangef;
						b = 0;
						break;
					case 1:
						r = 1 - rangef;
						g = 1;
						b = 0;
						break;
					case 2:
						r = 0;
						g = 1;
						b = rangef;
						break;
					case 3:
						r = 0;
						g = 1 - rangef;
						b = 1;
						break;
					case 4:
						r = rangef;
						g = 0;
						b = 1;
						break;
					case 5:
						r = 1;
						g = 0;
						b = 1 - rangef;
						break;
				}
			}
			if (f < 1)
			{
				// darken
				r *= f;
				g *= f;
				b *= f;
			}
			else if (f > 1)
			{
				// lighten
				r += (1 / f);
				g += (1 / f);
				b += (1 / f);
			}
			return Color.FromScRgb(a, r, g, b);
		}

		public static Color CMYKtoColor(CMYKa cmyka)
		{
			float r = (1 - cmyka.c) * (1 - cmyka.k);
			float g = (1 - cmyka.m) * (1 - cmyka.k);
			float b = (1 - cmyka.y) * (1 - cmyka.k);
			float a = cmyka.a;
			return Color.FromScRgb(a, r, g, b);
		}

		public static CMYKa ColorToCMYK(Color color)
		{
			CMYKa cmyka;
			float c = 1 - color.ScR;
			float m = 1 - color.ScG;
			float y = 1 - color.ScB;
			float a = color.ScA;

			float min = (float)Math.Min(c, Math.Min(m, y));
			if (min == 1)
			{
				cmyka.c = 0;
				cmyka.m = 0;
				cmyka.y = 0;
				cmyka.k = 1;
			}
			else
			{
				cmyka.c = (c - min) / (1 - min);
				cmyka.m = (m - min) / (1 - min);
				cmyka.y = (y - min) / (1 - min);
				cmyka.k = min;
			}
			cmyka.a = a;
			return cmyka;
		}

		public static System.Drawing.Point[] getCirclePoints(double radius)
		{
			List<System.Drawing.Point> points = new List<System.Drawing.Point>();

			for (int y = (int)Math.Round(-radius); y < radius; y++)
			{
				for (int x = (int)Math.Round(-radius); x < radius; x++)
				{
					if (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) <= radius)
					{
						points.Add(new System.Drawing.Point(x, y));
					}
				}
			}
			return points.ToArray();
		}

		public static BitmapSource visualToBitmap(Visual visual, double width, double height)
		{
			RenderTargetBitmap bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Default);
			bitmap.Render(visual);
			return bitmap;
		}

		public static void saveBitmapImage(BitmapSource bitmap, string fileName)
		{
			TiffBitmapEncoder encoder = new TiffBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(bitmap));
			using (FileStream file = File.Create(fileName))
			{
				encoder.Save(file);
			}
		}

		public static string createNumberedFilename(string filePath, int i)
		{
			string[] parts = filePath.Split('.');
			string basePath;
			string ext;

			if (parts.Length > 1)
			{
				basePath = parts[0];
				ext = "." + parts[1];
			}
			else
			{
				basePath = filePath;
				ext = "";
			}
			return string.Format("{0}{1:0000}{2}", basePath, i, ext);
		}

		public static Array GetEnumDescriptions(Type enumType)
		{
			Array names = Enum.GetValues(enumType);
			Array descriptions = Array.CreateInstance(typeof(String), names.Length);
			int i = 0;
			foreach (Enum val in Enum.GetValues(enumType))
			{
				descriptions.SetValue(GetEnumDescription(val), i++);
			}
			return descriptions;
		}

		public static String GetEnumDescription(Enum value)
		{
			FieldInfo fi = value.GetType().GetField(value.ToString());

			DescriptionAttribute[] attributes =
				(DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

			if (attributes != null && attributes.Length > 0)
			{
				return attributes[0].Description;
			}
			else
			{
				return value.ToString();
			}
		}

		public static Assembly getAssembly()
		{
			return Assembly.GetExecutingAssembly();
		}

		public static string getAssemblyName()
		{
			return getAssembly().GetName().Name;
		}

		public static string getAssemblyTitle()
		{
			object[] attributes = getAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attributes.Length > 0)
			{
				AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
				if (titleAttribute.Title != "")
				{
					return titleAttribute.Title;
				}
			}
			return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
		}

		public static string getAssemblyDescription()
		{
			object[] attributes = getAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyDescriptionAttribute)attributes[0]).Description;
		}

		public static string getAssemblyProduct()
		{
			object[] attributes = getAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyProductAttribute)attributes[0]).Product;
		}

		public static string getAssemblyCopyright()
		{
			object[] attributes = getAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
		}

		public static string getAssemblyCompany()
		{
			object[] attributes = getAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
			if (attributes.Length == 0)
			{
				return "";
			}
			return ((AssemblyCompanyAttribute)attributes[0]).Company;
		}

		public static string getAssemblyVersion()
		{
			return getAssembly().GetName().Version.ToString();
		}

		public static int getAssemblyMajorVersion()
		{
			return getAssembly().GetName().Version.Major;
		}

		public static int getAssemblyMinorVersion()
		{
			return getAssembly().GetName().Version.Minor;
		}

		public static string getRegistryKeyString()
		{
			return Constants.registryRoot + getAssemblyName();
		}

		public static string getRegistry(string label)
		{
			RegistryKey reg = Registry.CurrentUser.OpenSubKey(getRegistryKeyString());
			if (reg != null)
			{
				if (reg.ValueCount > 0)
				{
					if (reg.GetValue(label) != null)
					{
						return reg.GetValue(label).ToString();
					}
				}
			}
			return "";
		}

		public static void setRegistry(string label, string value)
		{
			string regKeyString = getRegistryKeyString();
			RegistryKey reg = Registry.CurrentUser.OpenSubKey(regKeyString, true);

			if (reg == null)
			{
				reg = Registry.CurrentUser.CreateSubKey(regKeyString);
			}
			reg = Registry.CurrentUser.OpenSubKey(regKeyString, true);

			if (reg != null)
			{
				reg.SetValue(label, value);
			}
		}

		public static string extractText(string s)
		{
			string text = "";
			string part;
			int i;

			foreach (string part0 in s.Split(' '))
            {
				part = part0;
				if (part.StartsWith("http"))
                {
					i = part.LastIndexOf("/");
					part = part.Substring(i + 1);
                }
				if (text != "")
                {
					text += " ";
                }
				text += part;
            }
			return text;
		}

		public static string extractUrl(string s)
		{
			foreach (string part in s.Split(' '))
			{
				if (part.StartsWith("http"))
				{
					return part;
				}
			}
			return "";
		}

		public static string getUrl(string url)
		{
			string content = "";

			WebRequest request = WebRequest.Create(url);
			request.UseDefaultCredentials = true;
			request.Timeout = 30000; // takes max approx 2.5 seconds
			((HttpWebRequest)request).Accept = "*/*";
			((HttpWebRequest)request).UserAgent = "compatible"; // essential!
			WebResponse response = request.GetResponse();
			Stream responseStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(responseStream);
			content = reader.ReadToEnd();
			reader.Close();
			responseStream.Close();
			response.Close();

			return content;
		}

		public static string postUrl(string url, string datastring)
		{
			string content = "";
			Encoding defenc = ASCIIEncoding.ASCII;
			byte[] data = defenc.GetBytes(datastring);

			try
			{
				WebRequest request = WebRequest.Create(url);
				request.UseDefaultCredentials = true;
				request.Timeout = 30000; // takes max approx 2.5 seconds
				((HttpWebRequest)request).Accept = "*/*";
				((HttpWebRequest)request).UserAgent = "compatible"; // essential!
				request.Method = "POST";
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = data.Length;

				Stream dataStream = request.GetRequestStream();
				dataStream.Write(data, 0, data.Length);
				dataStream.Close();

				WebResponse response = request.GetResponse();
				Stream responseStream = response.GetResponseStream();
				StreamReader reader = new StreamReader(responseStream);
				content = reader.ReadToEnd();
				reader.Close();
				responseStream.Close();
				response.Close();
			}
			catch (Exception e)
			{
				content = e.Message;
			}
			return content;
		}

		public static bool openWebLink(string link)
		{
			try
			{
				Process.Start(link);
				return true;
			}
			catch (Exception e)
			{
				Clipboard.SetText(link);
				MessageBox.Show("Error: " + e.Message + "\nPlease use your preferred browser to navigate to this link manually\n(This link has been copied to the clipboard - Select Paste in your browser address bar)", "Application error");
			}
			return false;
		}

		public static bool openMailLink(string link, string email, string body)
		{
			try
			{
				Process.Start(link);
				return true;
			}
			catch (Exception e)
			{
				if (body != "")
				{
					Clipboard.SetText(body);
					MessageBox.Show("Error: " + e.Message + "\nPlease use your preferred application to send an e-mail manually to: " + email + "\n(The message content has been copied to the clipboard - Select Paste in your email message body)", "Application error");
				}
				else
				{
					Clipboard.SetText(email);
					MessageBox.Show("Error: " + e.Message + "\nPlease use your preferred application to send an e-mail manually\n(The email address has been copied to the clipboard)", "Application error");
				}
			}
			return false;
		}

		public static int countStringSplit(string s, string sep)
		{
			int n = 0;
			string[] parts = s.Trim().Split(new string[] { sep }, StringSplitOptions.RemoveEmptyEntries);
			foreach (string part in parts)
			{
				if (part.Trim() != "")
				{
					n++;
				}
			}

			return n;
		}

		public static void gcCollect()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

	}
}
