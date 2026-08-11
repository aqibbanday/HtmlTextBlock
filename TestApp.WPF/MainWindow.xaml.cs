using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AqiTechTips;

namespace TestApp.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            PopulateCodeBehindDemos();
        }

        private void PopulateCodeBehindDemos()
        {
            // <binding> reads DataContext at parse time, which already ran once via the
            // XAML-declared Html property before DataContext is available here - toggle
            // Html to force a re-parse now that DataContext is set.
            bindingDemo.DataContext = new { UserName = "Ada", UnreadCount = 3 };
            string bindingHtml = bindingDemo.Html;
            bindingDemo.Html = string.Empty;
            bindingDemo.Html = bindingHtml;

            string[] palette = { "red", "orange", "gold", "green", "blue", "purple" };
            rainbowDemo.Html = HtmlTextBuilder.StyleWords("The quick brown fox jumps over the lazy dog", (word, index) =>
                $"color:{palette[index % palette.Length]};font-weight:bold");

            string[] keywords = { "ERROR", "WARN", "OK" };
            keywordDemo.Html = HtmlTextBuilder.StyleWords("OK: cache warm. WARN: disk 80% full. ERROR: connection refused.", (word, index) =>
            {
                string trimmed = word.TrimEnd(':', '.');
                if (trimmed == "ERROR") return "color:white;background-color:red;font-weight:bold";
                if (trimmed == "WARN") return "color:black;background-color:gold;font-weight:bold";
                if (trimmed == "OK") return "color:white;background-color:green;font-weight:bold";
                return null;
            });
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is Hyperlink link)
            {
                // NavigateUri is null when the href couldn't be parsed as a valid Uri
                // (HtmlUpdater renders the Hyperlink anyway rather than dropping it).
                MessageBox.Show(link.NavigateUri != null ? link.NavigateUri.ToString() : "(invalid link)");
                e.Handled = true;
            }
        }


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            AddHandler(Hyperlink.ClickEvent, (RoutedEventHandler)Hyperlink_Click);

        }
    }
}
