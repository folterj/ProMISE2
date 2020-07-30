using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProMISE2
{
    public class JogTextFormatter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool intMode;
            if (values != null)
            {
                if (values.Count() >= 2)
                {
                    if (values[1].GetType() == typeof(bool))
                    {
                        intMode = bool.Parse(values[1].ToString());
                        if (values[0].GetType() == typeof(float))
                        {
                            if (intMode)
                            {
                                return string.Format("{0:0}", values[0]);
                            }
                            else
                            {
                                return string.Format("{0:0.####}", values[0]);
                            }
                        }
                    }
                }
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            float x;

            if (value != null)
            {
                if (value.GetType() == typeof(String))
                {
                    float.TryParse(value.ToString(), out x);
                    return new object[] { x };
                }
            }
            return new object[] { };
        }
    }

    /// <summary>
    /// Interaction logic for JogControl.xaml
    /// </summary>
    public partial class JogControl : UserControl, INotifyPropertyChanged
    {
        public float val;
        String header;

        bool dragging;
        Point dragpos;
        float jogpos;
        DispatcherTimer dragTimer;

        public bool IntMode { get; set; }
        public bool LogScale { get; set; }
        public float ScalePower { get; set; }

        public float MinValue { get; set; }
        public float MaxValue { get; set; }
        public bool Positive { get; set; }
        public bool NotNegative { get; set; }

        public float Value
        {
            get { return val; }
            set
            {
                if (!validateVal(value))
                {
                    throw new Exception("Value out of range");
                }
                NotifyPropertyChanged("Value");
                if (ValueChanged != null)
                {
                    ValueChanged(this, new DependencyPropertyChangedEventArgs());
                }
            }
        }

        public String Header
        {
            get { return header; }
            set { header = value; jogGroup.Header = header.Replace("\\n", "\n"); }
        }

        bool validateVal(float value)
        {
            // validate & set (if possible)
            float newval = value;
            if (MinValue != MaxValue && newval < MinValue)
            {
                newval = MinValue;
            }
            if (MinValue != MaxValue && newval > MaxValue)
            {
                newval = MaxValue;
            }
            if (NotNegative && newval < 0)
            {
                newval = 0;
            }
            if (Positive && newval <= 0)
            {
                return false;
            }
            val = newval;
            return true;
        }

        public event DependencyPropertyChangedEventHandler ValueChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(string sProp)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(sProp));
            }
        }

        public JogControl()
        {
            InitializeComponent();

            IntMode = false;
            LogScale = false;
            ScalePower = 5;

            MinValue = 0;
            MaxValue = 0;
            Positive = false;
            NotNegative = false;
            dragging = false;
            jogpos = 0;

            dragTimer = new DispatcherTimer();
            dragTimer.Interval = TimeSpan.FromMilliseconds(200);
            dragTimer.Tick += new EventHandler(dragTimer_Elapsed);

            DataContext = this;
        }

        void redraw()
        {
            double w = jogCanvas.ActualWidth;
            double h = jogCanvas.ActualHeight;
            float outrad = (float)Math.Min(w, h);
            float griprad = outrad / 6;

            jogOutlineEllipse.Width = outrad;
            jogOutlineEllipse.Height = outrad;
            Canvas.SetLeft(jogOutlineEllipse, (w - outrad) / 2);

            jogScaleEllipse.Width = outrad;
            jogScaleEllipse.Height = outrad;
            Canvas.SetLeft(jogScaleEllipse, (w - outrad) / 2);
            jogScaleEllipse.StrokeThickness = griprad;

            dummyRect.Width = outrad;
            dummyRect.Height = outrad;
            Canvas.SetLeft(dummyRect, (w - outrad) / 2);

            jogGripEllipse.Width = griprad;
            jogGripEllipse.Height = griprad;

            // jog control ellipse
            float angle = jogpos * (float)Math.PI;
            // rotate 90 degrees:
            float y = (float)(outrad / 2 * (1 - 0.8 * Math.Cos(angle)));
            float x = (float)(outrad / 2 * (1 + 0.8 * Math.Sin(angle)));
            Canvas.SetLeft(jogGripEllipse, ((w - outrad) / 2) + (x - griprad / 2));
            Canvas.SetTop(jogGripEllipse, y - griprad / 2);
        }

        void calcJogPos()
        {
            // rotate 90 degrees:
            float outrad = (float)jogOutlineEllipse.Width;
            float angle = (float)Math.Atan2(dragpos.X - outrad / 2, outrad / 2 - dragpos.Y);
            if (angle > Math.PI / 2)
            {
                angle = (float)Math.PI / 2;
            }
            if (angle < -Math.PI / 2)
            {
                angle = -(float)Math.PI / 2;
            }
            jogpos = (float)Math.Round(angle / Math.PI * 8) / 8;
        }

        void dragTimer_Elapsed(object sender, EventArgs e)
        {
            modValue();
        }

        void modValue()
        {
            float mult, fac;
            float newval = val;
            mult = jogpos * 2;	// -1 ... 0 ... 1
            if (mult != 0)
            {
                if (LogScale)
                {
                    fac = (float)Math.Pow(10, Math.Abs(mult) / ScalePower);
                    if (mult > 0)
                    {
                        if (newval > 0)
                        {
                            newval *= fac;
                        }
                        else
                        {
                            newval = 0.0001f;
                        }
                    }
                    else if (mult < 0)
                    {
                        newval /= fac;
                    }
                }
                else
                {
                    fac = (float)Math.Pow(10, Math.Abs(mult * 4) - ScalePower);
                    if (mult > 0)
                    {
                        newval += fac;
                    }
                    else if (mult < 0)
                    {
                        newval -= fac;
                    }
                }
                Value = newval;
                NotifyPropertyChanged("Value");
            }
        }

        private void jog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            dragpos = e.GetPosition(jogCanvas);
            calcJogPos();
            modValue();
            redraw();
            dragTimer.Start();
        }

        private void jog_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                dragpos = e.GetPosition(jogCanvas);
                calcJogPos();
                redraw();
            }
        }

        private void jog_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (dragging)
            {
                dragTimer.Stop();
                dragging = false;
                jogpos = 0;
                redraw();
            }
        }

        private void jog_MouseLeave(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                dragTimer.Stop();
                dragging = false;
                jogpos = 0;
                redraw();
            }
        }

        private void jogCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            redraw();
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsEnabled)
            {
                jogCanvas.Opacity = 1;
                jogGripEllipse.Visibility = Visibility.Visible;
                jogScaleEllipse.Visibility = Visibility.Visible;
            }
            else
            {
                jogCanvas.Opacity = 0.3f;
                jogGripEllipse.Visibility = Visibility.Hidden;
                jogScaleEllipse.Visibility = Visibility.Hidden;
            }
        }

    }
}
