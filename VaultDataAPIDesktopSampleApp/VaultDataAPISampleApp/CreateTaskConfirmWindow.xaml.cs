using System.Windows;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for CreateTaskConfirmWindow.xaml
    /// </summary>
    public partial class CreateTaskConfirmWindow : Window
    {
        public string ConfigId { get; private set; }

        public string WorkflowType { get; private set; }

        public CreateTaskConfirmWindow(string configId, string workflowType)
        {
            InitializeComponent();
            ConfigId = configId ?? string.Empty;
            WorkflowType = workflowType ?? string.Empty;
            ConfigIdTextBox.Text = ConfigId;
            WorkflowTypeTextBox.Text = WorkflowType;
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
