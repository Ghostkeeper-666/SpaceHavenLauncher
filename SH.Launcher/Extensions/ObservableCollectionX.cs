using System.Collections.ObjectModel;

namespace SH.Launcher.Extensions;

internal static class ObservableCollectionX
{
    public static void MoveUp<T>(this ObservableCollection<T> list, T item, int min = 0)
    {
        if (item is null)
            return;
        int index = list.IndexOf(item);
        if (index >= 0 && index > min && index > 0)
            list.Move(index, index - 1);
    }

    public static void MoveDown<T>(this ObservableCollection<T> list, T item, int max = int.MaxValue)
    {
        if (item is null)
            return;
        int index = list.IndexOf(item);
        if (index >= 0 && index < max && index < list.Count - 1)
            list.Move(index, index + 1);
    }


}
