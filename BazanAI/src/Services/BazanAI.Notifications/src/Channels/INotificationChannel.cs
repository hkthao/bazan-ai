
using System.Threading.Tasks;

namespace BazanAI.Notifications.Channels;

public interface INotificationChannel
{
    Task SendAsync(object message, string recipient);
}
