using System.Windows.Controls;

namespace ProMISE2
{
    public class ProfileView : UserControl
    {
        public ProfileView()
            : base()
        {
        }

        public virtual void updateParams(ControlParams controlparams)	// will be overwritten
        {
        }

        public virtual void updatePreview(OutParams outparams)  // will be overwritten
        {
        }

    }
}
