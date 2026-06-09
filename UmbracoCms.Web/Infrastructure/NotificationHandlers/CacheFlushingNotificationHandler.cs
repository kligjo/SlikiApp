using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace UmbracoCms.Web.Infrastructure.NotificationHandlers;

public class CacheFlushingNotificationHandler :
    INotificationHandler<ContentCacheRefresherNotification>,
    INotificationHandler<MediaCacheRefresherNotification>
{
    private readonly ICacheManager _cacheManager;

    public CacheFlushingNotificationHandler(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    public void Handle(ContentCacheRefresherNotification notification)
    {
        _cacheManager.Flush();
    }

    public void Handle(MediaCacheRefresherNotification notification)
    {
        _cacheManager.Flush();
    }
}
