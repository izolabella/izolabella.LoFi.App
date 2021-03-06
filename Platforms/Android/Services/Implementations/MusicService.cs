using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using izolabella.Music.Platforms.Android;
using izolabella.Music.Structure.Music.Songs;
using izolabella.Music.Structure.Requests;
using static Android.OS.PowerManager;
using static Android.Net.Wifi.WifiEnterpriseConfig;
using static Android.Media.ChannelOut;
using Android.Media;
using Android.Content.PM;
using izolabella.LoFi.App.Platforms.Android.Notifications;
using izolabella.Music.Structure.Music.Artists;
using izolabella.LoFi.App.Wide.Services.Implementations;

namespace izolabella.LoFi.App.Platforms.Android.Services.Implementations
{
    [Service(Name = $"com.izolabella.LoFi.{nameof(MusicService)}", Exported = true, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class MusicService : Service
    {
        public override IBinder OnBind(Intent? I)
        {
            return new Binder();
        }

        public GenericMusicService? Service { get; private set; }

        public override StartCommandResult OnStartCommand(Intent? I, StartCommandFlags F, int StartId)
        {
            IzolabellaMusicChannel Channel = new("LoFi", "The running notifications.");
            NotificationManager? NotificationManager = (NotificationManager?)GetSystemService(NotificationService);
            if (NotificationManager != null && OperatingSystem.IsAndroidVersionAtLeast(29) && NotificationManager.GetNotificationChannel(Channel.Id) == null)
            {
                NotificationChannel Ch = Channel.Build();
                Ch.SetSound(null, null);
                NotificationManager?.CreateNotificationChannel(Ch);
            }

            Intent OnClickIntent = new(this, typeof(MainActivity));
            TaskStackBuilder? StackBuilder = TaskStackBuilder.Create(this);
            StackBuilder?.AddParentStack(Java.Lang.Class.FromType(typeof(MainActivity)));
            StackBuilder?.AddNextIntent(OnClickIntent);

            PendingIntent? OnClickPendingIntent = StackBuilder?.GetPendingIntent(0, PendingIntentFlags.Mutable);

            Notification Notification = IzolabellaNotification.Factory(this, Channel)
                .SetContentIntent(OnClickPendingIntent)
                .SetOngoing(true)
                .SetContentText("Playing . . .")
                .SetSmallIcon(Resource.Drawable.navigation_empty_icon)
                .SetVisibility(NotificationVisibility.Public)
                .SetCategory("LoFi")
                .Build();
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                this.StartForeground(110042340, Notification, ForegroundService.TypeMediaPlayback);
            }

            this.Service = MainPage.GetService().ConfigureAwait(true).GetAwaiter().GetResult();
            this.Service.BufferReloaded += this.MusicService_BufferReloaded;
            return StartCommandResult.Sticky;
        }

        private Task MusicService_BufferReloaded()
        {
            return Task.CompletedTask;
        }
    }
}
