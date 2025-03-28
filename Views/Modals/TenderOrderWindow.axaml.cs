using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using EBISX_POS.API.Models;
using EBISX_POS.Models;
using EBISX_POS.State;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Diagnostics;
using EBISX_POS.Services;
using Microsoft.Extensions.DependencyInjection;
using EBISX_POS.API.Services.DTO.Order;
using System.Linq;
using EBISX_POS.ViewModels;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace EBISX_POS.Views
{
    public partial class TenderOrderWindow : Window
    {
        public TenderOrderWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void BackButton_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void EnterButton_Click(object? sender, RoutedEventArgs e)
        {
            var orderService = App.Current.Services.GetRequiredService<OrderService>();

            // Check if the tendered amount is sufficient
            if (TenderState.tenderOrder.TenderAmount >= TenderState.tenderOrder.AmountDue)
            {
                var finalOrder = new FinalizeOrderDTO()
                {
                    TotalAmount = TenderState.tenderOrder.TotalAmount,
                    CashTendered = TenderState.tenderOrder.TenderAmount,
                    OrderType = TenderState.tenderOrder.OrderType,
                    DiscountAmount = TenderState.tenderOrder.DiscountAmount,
                };

                await orderService.FinalizeOrder(finalOrder);

                // Kick off the asynchronous receipt generation task.
                await GenerateAndPrintReceiptAsync();

                OrderState.CurrentOrderItem = new OrderItemState();
                OrderState.CurrentOrder.Clear();
                OrderState.CurrentOrderItem.RefreshDisplaySubOrders();

                Close();
                return;
            }

            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
            {
                ContentHeader = "Insufficient Tender Amount",
                ContentMessage = "Input appropriate tender amount.",
                ButtonDefinitions = ButtonEnum.Ok,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                SizeToContent = SizeToContent.WidthAndHeight,
                Width = 400,
                ShowInCenter = true,
                Icon = MsBox.Avalonia.Enums.Icon.Warning
            }).ShowAsPopupAsync(this);
        }

        //private async void PwdScDiscount_Click(object? sender, RoutedEventArgs e)
        //{
        //    if (TenderState.tenderOrder.HasOrderDiscount)
        //    {
        //        await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        //        {
        //            ContentHeader = "Discounted Already",
        //            ContentMessage = "This order is already discounted.",
        //            ButtonDefinitions = ButtonEnum.Ok,
        //            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //            CanResize = false,
        //            SizeToContent = SizeToContent.WidthAndHeight,
        //            Width = 400,
        //            ShowInCenter = true,
        //            Icon = MsBox.Avalonia.Enums.Icon.Warning
        //        }).ShowAsPopupAsync(this);
        //        return;
        //    }

        //    var result = await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
        //    {
        //        ContentHeader = "PWD/SC Discount",
        //        ContentMessage = "Please ask the manager to swipe.",
        //        ButtonDefinitions = ButtonEnum.OkCancel,
        //        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        //        CanResize = false,
        //        SizeToContent = SizeToContent.WidthAndHeight,
        //        Width = 400,
        //        ShowInCenter = true,
        //        Icon = MsBox.Avalonia.Enums.Icon.Warning
        //    }).ShowAsPopupAsync(this);

        //    if (result == ButtonResult.Ok)
        //        TenderState.tenderOrder.HasPwdScDiscount = !TenderState.tenderOrder.HasPwdScDiscount;
        //}

        private async void PromoAndCouponDiscount_Click(object? sender, RoutedEventArgs e)
        {
            // Prevent applying multiple discounts to the same order.
            if (TenderState.tenderOrder.HasScDiscount || TenderState.tenderOrder.HasPwdDiscount || TenderState.tenderOrder.HasPromoDiscount)
            {
                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentHeader = "Discount Already Applied",
                    ContentMessage = "A discount has already been applied to this order. Please complete or cancel the current order before applying another discount.",
                    ButtonDefinitions = ButtonEnum.Ok,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    CanResize = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    Width = 400,
                    ShowInCenter = true,
                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                }).ShowAsPopupAsync(this);
                return;
            }

            if (sender is Button btn)
            {
                string discountType = GetDiscountType(btn);

                // Handle actions based on the discount type.
                switch (discountType)
                {
                    case "PROMO":
                        {
                            if (OrderState.CurrentOrder.Any(d => d.CouponCode != null))
                            {
                                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                                {
                                    ContentHeader = "Coupon Discount Detected",
                                    ContentMessage = "This order already has a coupon discount applied.",
                                    ButtonDefinitions = ButtonEnum.Ok,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                    CanResize = false,
                                    SizeToContent = SizeToContent.WidthAndHeight,
                                    Width = 400,
                                    ShowInCenter = true,
                                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                                }).ShowAsPopupAsync(this);
                                return;
                            }

                            var result = await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                            {
                                ContentHeader = "Apply Promo Discount",
                                ContentMessage = "Manager authorization is required to apply a promo discount. Please ask the manager to swipe their card.",
                                ButtonDefinitions = ButtonEnum.OkCancel,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                CanResize = false,
                                SizeToContent = SizeToContent.WidthAndHeight,
                                Width = 400,
                                ShowInCenter = true,
                                Icon = MsBox.Avalonia.Enums.Icon.Warning
                            }).ShowAsPopupAsync(this);

                            if (result == ButtonResult.Ok)
                            {
                                var promoWindow = new DiscountCodeWindow("PROMO");
                                await promoWindow.ShowDialog((Window)this.VisualRoot);
                            }
                            break;
                        }
                    case "COUPON":
                        {
                            if (OrderState.CurrentOrder.Count(d => d.CouponCode != null) >= 3)
                            {
                                await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                                {
                                    ContentHeader = "Maximum Coupon Discount Reached",
                                    ContentMessage = "This order already has the maximum allowed coupon discounts applied (3 coupons). You cannot apply any additional coupon discounts.",
                                    ButtonDefinitions = ButtonEnum.Ok,
                                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                    CanResize = false,
                                    SizeToContent = SizeToContent.WidthAndHeight,
                                    Width = 400,
                                    ShowInCenter = true,
                                    Icon = MsBox.Avalonia.Enums.Icon.Warning
                                }).ShowAsPopupAsync(this);
                                return;
                            }


                            var result = await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                            {
                                ContentHeader = "Apply Coupon Discount",
                                ContentMessage = "Manager authorization is required to apply a coupon discount. Please ask the manager to swipe their card.",
                                ButtonDefinitions = ButtonEnum.OkCancel,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                CanResize = false,
                                SizeToContent = SizeToContent.WidthAndHeight,
                                Width = 400,
                                ShowInCenter = true,
                                Icon = MsBox.Avalonia.Enums.Icon.Warning
                            }).ShowAsPopupAsync(this);

                            if (result == ButtonResult.Ok)
                            {
                                var promoWindow = new DiscountCodeWindow("COUPON");
                                await promoWindow.ShowDialog((Window)this.VisualRoot);
                            }
                            break;
                        }
                    default:
                        {
                            await MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                            {
                                ContentHeader = "Unknown Discount Type",
                                ContentMessage = "The selected discount type is not recognized. Please try again or contact support.",
                                ButtonDefinitions = ButtonEnum.Ok,
                                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                                CanResize = false,
                                SizeToContent = SizeToContent.WidthAndHeight,
                                Width = 400,
                                ShowInCenter = true,
                                Icon = MsBox.Avalonia.Enums.Icon.Warning
                            }).ShowAsPopupAsync(this);
                            break;
                        }
                }
            }
        }

        private string GetDiscountType(ContentControl control)
        {
            // Check if the Content is a TextBlock.
            if (control.Content is TextBlock textBlock)
            {
                return textBlock.Text;
            }
            // If the Content is a Panel (e.g., a StackPanel), attempt to get the first TextBlock.
            else if (control.Content is Panel panel)
            {
                var childTextBlock = panel.Children.OfType<TextBlock>().FirstOrDefault();
                return childTextBlock?.Text ?? string.Empty;
            }
            // Fallback: return the Content's string representation.
            return control.Content?.ToString() ?? string.Empty;
        }

        private async Task GenerateAndPrintReceiptAsync()
        {
            // Define target folder and file paths.
            string folderPath = @"C:\POS\Reciepts";
            string fileName = $"Receipt-{DateTimeOffset.UtcNow.ToString("MMMM-dd-yyyy-HH-mm-ss")}.txt";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // Ensure the target directory exists.
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }


                var receiptViewModel = new CustomerInvoiceReceiptViewModel();

                // Write receipt content to file.
                WriteReceiptContent(filePath, receiptViewModel);

                //// Notify the user if the file was created.
                //if (File.Exists(filePath))
                //{
                //    await MessageBoxManager
                //        .GetMessageBoxStandard("Notification", "Printing Receipt...", ButtonEnum.Ok)
                //        .ShowAsPopupAsync(this);
                //}

                // Open the receipt file automatically.
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (UnauthorizedAccessException)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", "Access Denied! Run the app as Administrator or choose another folder.", ButtonEnum.Ok)
                    .ShowAsPopupAsync(this);
            }
            catch (Exception ex)
            {
                await MessageBoxManager
                    .GetMessageBoxStandard("Error", ex.Message, ButtonEnum.Ok)
                    .ShowAsPopupAsync(this);
            }
        }
        private void WriteReceiptContent(string filePath, CustomerInvoiceReceiptViewModel viewModel)
        {
            int receiptWidth = 50; // Adjust as necessary for formatting
            var pesoCulture = new CultureInfo("en-PH");

            // Local helper to center text.
            string CenterText(string text) =>
                text.PadLeft((receiptWidth + text.Length) / 2).PadRight(receiptWidth);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                // Header
                writer.WriteLine(new string('=', receiptWidth));
                writer.WriteLine(CenterText("Customer Invoice Receipt"));
                writer.WriteLine(new string('=', receiptWidth));
                writer.WriteLine(CenterText(viewModel.BusinessName));
                writer.WriteLine(CenterText(viewModel.Address));
                writer.WriteLine(CenterText($"TIN: {viewModel.Tin}"));
                writer.WriteLine(CenterText($"MIN: {viewModel.Min}"));
                writer.WriteLine(new string('-', receiptWidth));
                writer.WriteLine();

                // Invoice details
                writer.WriteLine($"Invoice No: {viewModel.InvoiceNumber}".PadRight(receiptWidth - 10));
                writer.WriteLine(CenterText(viewModel.InvoiceType));
                writer.WriteLine($"Date: {viewModel.InvoiceDate:d}".PadRight(receiptWidth - 10));
                writer.WriteLine($"Cashier: {viewModel.Cashier}".PadRight(receiptWidth - 10));
                writer.WriteLine(new string('-', receiptWidth));

                // Items header// Items header
                writer.WriteLine($"{"Qty",-5} {"Description",-30} {"Amount",10}");
                writer.WriteLine(new string('-', receiptWidth));
                writer.WriteLine();

                // Invoice items
                foreach (var order in OrderState.CurrentOrder)
                {
                    foreach (var item in order.DisplaySubOrders)
                    {
                        // Simulate the grid column widths.
                        // Column 0: Fixed width (5 characters). Only show quantity if opacity is 1.
                        string quantityColumn = item.Opacity < 1.0
                            ? new string(' ', 5)
                            : $"{item.Quantity,-5}";

                        // Column 1: DisplayName in a left-aligned, fixed-width field (30 characters).
                        string displayNameColumn = $"{item.DisplayName,-30}";

                        // Column 2: Price string, right-aligned with 10 characters.
                        string priceColumn = item.IsUpgradeMeal ? $"{item.ItemPriceString,10}" : string.Empty;

                        // Write out the formatted line simulating the grid.
                        writer.WriteLine($"{quantityColumn}{displayNameColumn}{priceColumn}");
                    }

                    writer.WriteLine();
                }
                writer.WriteLine(new string('-', receiptWidth));

                // Totals
                writer.WriteLine(CenterText($"{"Total Amount:",-20}{TenderState.tenderOrder.TotalAmount.ToString("C", pesoCulture),20}"));
                writer.WriteLine(CenterText($"{"Due Amount:",-20}{TenderState.tenderOrder.AmountDue.ToString("C", pesoCulture),20}"));
                writer.WriteLine(CenterText($"{"Cash Tendered:",-20}{TenderState.tenderOrder.TenderAmount.ToString("C", pesoCulture),20}"));
                writer.WriteLine(CenterText($"{"Change:",-20}{TenderState.tenderOrder.ChangeAmount.ToString("C", pesoCulture),20}"));
                writer.WriteLine();

                writer.WriteLine(CenterText($"{"Vat Zero Sales:",-20}{0.ToString("C", pesoCulture),20}"));
                writer.WriteLine(CenterText($"{"Vat Exempt Sales:",-20}{(TenderState.tenderOrder.HasScDiscount || TenderState.tenderOrder.HasPwdDiscount ? TenderState.tenderOrder.DiscountAmount : 0m).ToString("C", pesoCulture),20}"));
                writer.WriteLine(CenterText($"{"Vatables Sales:",-20}{(!TenderState.tenderOrder.HasOrderDiscount ? TenderState.tenderOrder.TotalAmount / 1.12m : 0m).ToString("C", pesoCulture),20}"));
                writer.WriteLine(CenterText($"{"VAT Amount:",-20}{(!TenderState.tenderOrder.HasOrderDiscount ? TenderState.tenderOrder.TotalAmount - (TenderState.tenderOrder.TotalAmount / 1.12m) : 0m).ToString("C", pesoCulture),20}"));
                writer.WriteLine();

                foreach (var order in OrderState.CurrentOrder.Where(i => i.IsPwdDiscounted || i.IsSeniorDiscounted))
                {
                    // Signature section
                    writer.WriteLine(CenterText("Name:____________________________"));
                    writer.WriteLine(CenterText("Address:_________________________"));
                    writer.WriteLine(CenterText("TIN: _____________________________"));
                    writer.WriteLine(CenterText("Signature: _______________________"));
                    writer.WriteLine();
                }

                // Footer
                writer.WriteLine(CenterText("This Serve as Sales Invoice"));
                writer.WriteLine(CenterText("Arsene Software Solutions"));
                writer.WriteLine(CenterText("Labangon St. Cebu City, Cebu"));
                writer.WriteLine(CenterText($"VAT Reg TIN: {viewModel.Tin}"));
                writer.WriteLine(CenterText($"Date Issue: {viewModel.InvoiceDate:d}"));
                writer.WriteLine(CenterText($"Valid Until: {viewModel.InvoiceDate:d}"));
                writer.WriteLine();
                writer.WriteLine(new string('=', receiptWidth));
                writer.WriteLine(CenterText("Thank you for your purchase!"));
                writer.WriteLine(new string('=', receiptWidth));
                writer.WriteLine();
            }
        }


    }
};
