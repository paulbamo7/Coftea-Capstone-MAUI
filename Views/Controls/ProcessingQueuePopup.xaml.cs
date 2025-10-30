using Microsoft.Maui.Controls;

namespace Coftea_Capstone.Views.Controls
{
    public partial class ProcessingQueuePopup : ContentView
    {
        public ProcessingQueuePopup()
        {
            InitializeComponent();
            this.SetValue(VisualElement.ZIndexProperty, 1900);
            this.SetValue(Grid.RowSpanProperty, 2);
            this.SetValue(Grid.ColumnSpanProperty, 2);
        }
    }
}


