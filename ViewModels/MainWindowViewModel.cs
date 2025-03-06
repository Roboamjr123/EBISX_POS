﻿using System.Collections.ObjectModel;
using EBISX_POS.ViewModels; // Ensure this is added
using EBISX_POS.API.Models; // Ensure this is added
using EBISX_POS.State;
using System.Threading.Tasks;
using EBISX_POS.Services;
using System.Diagnostics;
using System.Linq;

namespace EBISX_POS.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly MenuService _menuService;

        public ObservableCollection<Category> ButtonList { get; } = new();

        public OrderSummaryViewModel OrderSummaryViewModel { get; } // Add this property
        public ItemListViewModel ItemListViewModel { get; } // Add this property

        public MainWindowViewModel(MenuService menuService)
        {
            _menuService = menuService;
            OrderSummaryViewModel = new OrderSummaryViewModel(); // Initialize it
            ItemListViewModel = new ItemListViewModel(menuService); // Initialize it

            _ = LoadCategories();
        }

        public string CashierName => CashierState.CashierName ?? "Developer";

        private async Task LoadCategories()
        {
            var categories = await _menuService.GetCategoriesAsync();
            ButtonList.Clear();
            categories.ForEach(category => ButtonList.Add(category));
         }

        public async Task LoadMenusAsync(int categoryId)
        {
            Debug.WriteLine($"Loading menus for category ID: {categoryId}");
            await ItemListViewModel.LoadMenusAsync(categoryId);
            Debug.WriteLine($"Finished loading menus for category ID: {categoryId}");
        }
    }
}
