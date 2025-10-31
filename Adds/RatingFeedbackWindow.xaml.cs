using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace ScriptRunner
{
    public partial class RatingFeedbackWindow : Window
    {
        private const string ApiKey = "your_preshared_key_here"; // Replace with your actual key
        private readonly string _webhookUrl;
        private readonly string _scriptPath;
        private readonly int _rating;

        public FeedbackResult Feedback { get; private set; }

        public RatingFeedbackWindow(string webhookUrl, string scriptPath, int rating)
        {
            InitializeComponent();
            _webhookUrl = webhookUrl;
            _scriptPath = scriptPath;
            _rating = rating;
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var feedbackData = new
            {
                apiKey = ApiKey,
                scriptPath = _scriptPath,
                rating = _rating,
                email = EmailTextBox.Text,
                reason = ReasonComboBox.Text,
                details = DetailsTextBox.Text,
                timestamp = DateTime.UtcNow
            };

            string jsonPayload = JsonConvert.SerializeObject(feedbackData);

            try
            {
                using (var client = new HttpClient())
                {
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(_webhookUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Feedback submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        Feedback = new FeedbackResult { Submitted = true };
                        DialogResult = true;
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to submit feedback. Status: {response.StatusCode}\\nResponse: {errorContent}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Feedback = new FeedbackResult { Submitted = false };
                        DialogResult = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Feedback = new FeedbackResult { Submitted = false };
                DialogResult = false;
            }
        }
    }

    public class FeedbackResult
    {
        public bool Submitted { get; set; }
    }
}
