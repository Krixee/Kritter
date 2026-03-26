using System.Collections.Specialized;
using System.Windows.Controls;

namespace Kritter.Views;

public partial class YukleView : UserControl
{
    public YukleView()
    {
        InitializeComponent();

        // Auto-scroll log to bottom
        if (LogList.ItemsSource is INotifyCollectionChanged ncc)
        {
            ncc.CollectionChanged += (_, _) =>
            {
                if (LogList.Items.Count > 0)
                    LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
            };
        }

        LogList.Loaded += (_, _) =>
        {
            if (LogList.ItemsSource is INotifyCollectionChanged ncc2)
            {
                ncc2.CollectionChanged += (_, _) =>
                {
                    if (LogList.Items.Count > 0)
                        LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                };
            }
        };
    }
}
