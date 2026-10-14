using System.Windows;

namespace NetBox.Client.Wpf;

public partial class CreateChannelDialog : Window
{
    public string? ChannelName => ChannelNameBox.Text;

    public CreateChannelDialog()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ChannelName))
        {
            MessageBox.Show("Please enter a channel name.", "Create Channel", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DialogResult = true;
        Close();
    }
}
