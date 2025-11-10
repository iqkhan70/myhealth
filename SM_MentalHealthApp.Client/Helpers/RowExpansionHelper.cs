using System.Collections.Generic;
using Radzen;

namespace SM_MentalHealthApp.Client.Helpers
{
    public static class RowExpansionHelper
    {
        /// <summary>
        /// Generic row expansion handler that fetches full item details and stores in dictionary
        /// </summary>
        public static async Task<TItem?> ExpandRowAsync<TItem>(
            Dictionary<int, TItem> expandedItems,
            TItem item,
            Func<int, Task<TItem?>> fetchFullItem,
            Func<TItem, int> getId,
            NotificationService? notificationService = null)
            where TItem : class
        {
            try
            {
                var id = getId(item);
                if (!expandedItems.ContainsKey(id))
                {
                    var fullItem = await fetchFullItem(id);
                    if (fullItem != null)
                    {
                        expandedItems[id] = fullItem;
                        return fullItem;
                    }
                }
                else
                {
                    return expandedItems[id];
                }
            }
            catch (Exception ex)
            {
                notificationService?.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Error",
                    Detail = $"Failed to load item details: {ex.Message}",
                    Duration = 4000
                });
            }

            return null;
        }

        /// <summary>
        /// Clear expanded items from dictionary
        /// </summary>
        public static void ClearExpanded<TItem>(Dictionary<int, TItem> expandedItems, int id)
        {
            if (expandedItems.ContainsKey(id))
            {
                expandedItems.Remove(id);
            }
        }
    }
}

