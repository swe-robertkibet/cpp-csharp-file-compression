using Avalonia.Controls;
using CompressionTool.Services;
using CompressionTool.ViewModels;

namespace CompressionTool.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Create file dialog service and pass to ViewModel
        var fileDialogService = new FileDialogService(this);
        DataContext = new MainWindowViewModel(fileDialogService);
    }
}