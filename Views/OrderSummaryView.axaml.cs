using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace EBISX_POS.Views
{
    public partial class OrderSummaryView : UserControl
    {
        public OrderSummaryView()
        {
            InitializeComponent();
            DataContext = new ViewModels.OrderSummaryViewModel();
        }
    }
};
