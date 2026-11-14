using System.Windows;
using CurrencyConverter.Core.ViewModels;

namespace CurrencyConverter.Classic.Views;

/// <summary>
/// Code-behind stays empty on purpose (MVVM): the only thing it does is receive the
/// ViewModel through constructor injection and use it as the DataContext for bindings.
/// It is the same MainViewModel the Fluent window uses.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
