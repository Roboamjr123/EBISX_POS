using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EBISX_POS.Models;
using EBISX_POS.State;
using EBISX_POS.ViewModels;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia;
using System.Diagnostics;
using MsBox.Avalonia.Enums;
using Avalonia.Controls.ApplicationLifetimes;
using EBISX_POS.Services;
using EBISX_POS.API.Services.DTO.Order;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace EBISX_POS.Views
{
    public partial class OrderSummaryView : UserControl
    {
        private readonly OrderService _orderService;

        public OrderSummaryView(OrderService orderService)
        {
            InitializeComponent();
            _orderService = orderService; // Assign the injected service
            DataContext = new OrderSummaryViewModel();
        }

        public OrderSummaryView() : this(App.Current.Services.GetRequiredService<OrderService>())
        {
            // This constructor is required for Avalonia to instantiate the view in XAML.
        }

        private async void EditOrder_Button(object? sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton && clickedButton.DataContext is OrderItemState SelectedCurrentOrderItem)
            {
                var detailsWindow = new OrderItemEditWindow()
                {
                    DataContext = new OrderItemEditWindowViewModel(SelectedCurrentOrderItem)
                };

                Debug.WriteLine("EditOrder_Button: " + SelectedCurrentOrderItem.DisplaySubOrders.Count);


                await detailsWindow.ShowDialog((Window)this.VisualRoot);
            }
        }

        private async void VoidCurrentOrder_Button(object? sender, RoutedEventArgs e)
        {
            var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var owner = lifetime?.MainWindow;

            var box = MessageBoxManager.GetMessageBoxStandard(
                new MessageBoxStandardParams
                {
                    ContentHeader = "Void Order",
                    ContentMessage = "Please ask the manager to swipe.",
                    ButtonDefinitions = ButtonEnum.OkCancel,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 300,
                    ShowInCenter = true,
                    Icon = Icon.Warning
                });

            var result = await box.ShowAsPopupAsync(owner); 
            
            var subOrders = OrderState.CurrentOrderItem?.DisplaySubOrders;

            var voidOrder = new AddCurrentOrderVoidDTO
            {
                qty = OrderState.CurrentOrderItem.Quantity,
                menuId = subOrders?.FirstOrDefault(m => m.MenuId != null)?.MenuId ?? 0,
                drinkId = subOrders?.FirstOrDefault(m => m.DrinkId != null)?.DrinkId ?? 0,
                addOnId = subOrders?.FirstOrDefault(m => m.AddOnId != null)?.AddOnId ?? 0,
                drinkPrice = subOrders?.FirstOrDefault(m => m.DrinkId != null)?.ItemPrice ?? 0,
                addOnPrice = subOrders?.FirstOrDefault(m => m.AddOnId != null)?.ItemPrice ?? 0,
                managerEmail = "user1@example.com"
            };

            switch (result)
            {
                case ButtonResult.Ok:
                    OrderState.CurrentOrderItem = new OrderItemState();
                    OrderState.CurrentOrderItem.RefreshDisplaySubOrders();

                    var (success, message) = await _orderService.AddCurrentOrderVoid(voidOrder);


                    if (success)
                    {
                        Debug.WriteLine("VoidCurrentOrder_Button: Order voided successfully.");
                    }
                    else
                    {
                        Debug.WriteLine($"VoidCurrentOrder_Button: Error - {message}");
                    }
                    return;
                case ButtonResult.Cancel:
                    Debug.WriteLine("VoidCurrentOrder_Button: Order voiding canceled.");
                    return;
                default:
                    return;
            }

        }
    }
};
