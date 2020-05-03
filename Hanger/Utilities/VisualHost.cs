using System.Windows;
using System.Windows.Media;

namespace Hanger.Utilities
{
    class VisualHost : UIElement
    {
        public Visual visual { get; set; }

        protected override int VisualChildrenCount
        {
            get { return visual != null ? 1 : 0; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visual;
        }

    }
}
