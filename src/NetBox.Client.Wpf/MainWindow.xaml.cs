using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using NetBox.Client.Networking;
using NetBox.Client.ViewModels;

namespace NetBox.Client.Wpf;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<string> _channels = new();
    private readonly ObservableCollection<string> _messages = new();
    private string? _username;
    private string? _currentChannel;

    private readonly MessageDispatcher _dispatcher;
    private readonly ConnectionManager _connection;
    private readonly ChatViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        ChannelsList.ItemsSource = _channels;
        ChatList.ItemsSource = _messages;

        _dispatcher = new MessageDispatcher();
        _connection = new ConnectionManager(_dispatcher);
        _viewModel = new ChatViewModel(_connection, _dispatcher);

        _dispatcher.MessageReceived += OnMessageReceivedAsync;
    }

    private async Task OnMessageReceivedAsync(NetBox.Shared.Protocols.Message msg)
    {
        // Handle chat messages from server
        if (msg.Command == NetBox.Shared.Protocols.Enums.Command.CHAT_MESSAGE)
        {
            if (NetBox.Shared.ServerEvents.ChatMessage.FromMessage(msg, out var chat))
            {
                // marshal to UI thread
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    _messages.Add($"[{chat.Sender}] {chat.Message}");
                }));
            }
        }
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameBox.Text?.Trim();
        if (string.IsNullOrEmpty(username))
        {
            System.Windows.MessageBox.Show("Please enter a username.", "NetBox", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _username = username;
        WelcomeText.Text = $"Connected as: {_username}";

        LoginPanel.Visibility = Visibility.Collapsed;
        MainPanel.Visibility = Visibility.Visible;
        
        try
        {
            var ok = await _viewModel.LoginAsync(_username);
            if (!ok)
            {
                System.Windows.MessageBox.Show("Login failed.", "NetBox", MessageBoxButton.OK, MessageBoxImage.Warning);
                LoginPanel.Visibility = Visibility.Visible;
                MainPanel.Visibility = Visibility.Collapsed;
                return;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Connection error: {ex.Message}", "NetBox", MessageBoxButton.OK, MessageBoxImage.Error);
            LoginPanel.Visibility = Visibility.Visible;
            MainPanel.Visibility = Visibility.Collapsed;
            return;
        }
    }

    private async void CreateChannelButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new CreateChannelDialog();
        if (dlg.ShowDialog() != true) return;

        var name = dlg.ChannelName?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        CreateChannelButton.IsEnabled = false;
        try
        {
            var resp = await _connection.CreateChannelAsync(name);
            if (resp.ResponseCode == NetBox.Shared.Protocols.Enums.CreateChannelResponseCode.CHANNEL_CREATED)
            {
                if (!_channels.Contains(name))
                    _channels.Add(name);
            }
            else if (resp.ResponseCode == NetBox.Shared.Protocols.Enums.CreateChannelResponseCode.CHANNEL_ALREADY_EXISTS)
            {
                System.Windows.MessageBox.Show($"Channel '{name}' already exists.", "Create Channel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show($"Create channel failed: {resp.ResponseCode}", "Create Channel", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error creating channel: {ex.Message}", "Create Channel", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            CreateChannelButton.IsEnabled = true;
        }
    }

    private void ChannelsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ChannelsList.SelectedItem as string;
        JoinChannelButton.IsEnabled = !string.IsNullOrEmpty(selected) && _currentChannel == null;
    }

    private async void JoinChannelButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = ChannelsList.SelectedItem as string;
        if (string.IsNullOrEmpty(selected)) return;

        _currentChannel = selected;
        LeaveChannelButton.IsEnabled = true;
        JoinChannelButton.IsEnabled = false;
        MessageBox.IsEnabled = true;
        SendButton.IsEnabled = true;

        _messages.Clear();
        _messages.Add($"[System] Joined channel: {_currentChannel}");

        try
        {
            var response = await _connection.JoinChannelAsync(_currentChannel);
            if (response.ResponseCode == NetBox.Shared.Protocols.Enums.JoinChannelResponseCode.FAILURE)
            {
                System.Windows.MessageBox.Show($"Failed to join channel: {response.ResponseCode}", "Join Channel", MessageBoxButton.OK, MessageBoxImage.Warning);
                _currentChannel = null;
                LeaveChannelButton.IsEnabled = false;
                JoinChannelButton.IsEnabled = ChannelsList.SelectedItem != null;
                MessageBox.IsEnabled = false;
                SendButton.IsEnabled = false;
            }
            else if (response.ResponseCode == NetBox.Shared.Protocols.Enums.JoinChannelResponseCode.CHANNEL_NOT_FOUND)
            {
                System.Windows.MessageBox.Show($"Channel '{_currentChannel}' not found.", "Join Channel", MessageBoxButton.OK, MessageBoxImage.Warning);
                _currentChannel = null;
                LeaveChannelButton.IsEnabled = false;
                JoinChannelButton.IsEnabled = ChannelsList.SelectedItem != null;
                MessageBox.IsEnabled = false;
                SendButton.IsEnabled = false;
            }
            else if (response.ResponseCode == NetBox.Shared.Protocols.Enums.JoinChannelResponseCode.ALREADY_IN_CHANNEL)
            {
                System.Windows.MessageBox.Show($"Already in channel '{_currentChannel}'.", "Join Channel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                System.Windows.MessageBox.Show($"Joined channel '{_currentChannel}'.", "Join Channel", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error joining channel: {ex.Message}", "Join Channel", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LeaveChannelButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentChannel == null) return;

        // TODO: send LeaveChannelRequest to server

        _messages.Add($"[System] Left channel: {_currentChannel}");
        _currentChannel = null;
        LeaveChannelButton.IsEnabled = false;
        JoinChannelButton.IsEnabled = ChannelsList.SelectedItem != null;
        MessageBox.IsEnabled = false;
        SendButton.IsEnabled = false;
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var text = MessageBox.Text;
        if (string.IsNullOrWhiteSpace(text)) return;

        _messages.Add($"[{_username}] {text}");
        MessageBox.Clear();

        // TODO: send SendMessageRequest to server
    }
}
