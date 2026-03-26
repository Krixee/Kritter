using System.Collections.Specialized;
using System.Windows.Controls;

namespace Kritter.Views;

public partial class OptimizasyonView : UserControl
{
    public OptimizasyonView()
    {
        InitializeComponent();

        LogList.Loaded += (_, _) =>
        {
            if (LogList.ItemsSource is INotifyCollectionChanged ncc)
            {
                ncc.CollectionChanged += (_, _) =>
                {
                    if (LogList.Items.Count > 0)
                        LogList.ScrollIntoView(LogList.Items[LogList.Items.Count - 1]);
                };
            }
        };
    }
}
