using CurrencyConverter.Core.ViewModels;
using Wpf.Ui.Controls;

namespace CurrencyConverter.Fluent.Views;

/// <summary>
/// Code-behind stays empty on purpose (MVVM): the only thing it does is receive the
/// ViewModel through constructor injection and use it as the DataContext for bindings.
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
