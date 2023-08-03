using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Notifications;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Arrivaler
{
    public class DestinationInfo
    {
        public DestinationInfo() { }
        public string Name { get; set; }
        public PolarPos Pos { get; set; }

        override public string ToString() { return Name; }

    }

    class GeolocationUtil
    {
        public readonly bool hasAccess;
        public readonly Geolocator geolocator;
        public GeolocationUtil(bool hasAccess)
        {
            this.hasAccess = hasAccess;
            geolocator = hasAccess
                ? new Geolocator() { DesiredAccuracyInMeters = 50 }
                : null;

        }
    }


    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public ObservableCollection<DestinationInfo> DestinationInfos;
        public DestinationInfo Destination = null;

        private readonly DispatcherQueue uiThreadDispatcherQueue;

        public MainWindow()
        {
            this.InitializeComponent();

            DestinationInfos = new ObservableCollection<DestinationInfo>
            { // Default destinations TODO loading
                new DestinationInfo { Name = "Null Island", Pos = new(0.0, 0.0) },
                new DestinationInfo { Name = "Penguinlandia", Pos = new(-Math.PI/2, 0.0) },
            };

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            uiThreadDispatcherQueue = DispatcherQueue.GetForCurrentThread();

            StartGPSPolling();
        }

        // ~~~~ LOCATION TRACKING ~~~~ //

        void StartGPSPolling()
        {
            var t = new System.Timers.Timer(1000.0)
            {
                AutoReset = true,
            };
            t.Elapsed += new System.Timers.ElapsedEventHandler(TimerElapsed);
            t.Start();
        }

        private GeolocationUtil geolocationUtil = null;
        private async Task<bool> HasGeolocation()
        {
            geolocationUtil ??= new GeolocationUtil(
                await Geolocator.RequestAccessAsync() == GeolocationAccessStatus.Allowed
            );
            return geolocationUtil.hasAccess;
        }

        async void UpdateLocation()
        {
            string text;
            if (geolocationUtil == null)
            {
                uiThreadDispatcherQueue.TryEnqueue(() =>
                {
                    TextBlockDistanceInformation.Text = "Loading...";
                });
            }
            if (await HasGeolocation())
            {
                Geoposition posRaw = await geolocationUtil.geolocator.GetGeopositionAsync();
                var currentPos = PolarPos.FromLatLonDeg(
                    posRaw.Coordinate.Point.Position.Latitude,
                    posRaw.Coordinate.Point.Position.Longitude
                );
                if (Destination != null)
                {
                    double distanceFromDestination = MathHelper.ArcDistance(
                        Destination.Pos, currentPos
                    ) * MathHelper.EARTH_RADIUS_KM;

                    text = $"Distance from {Destination.Name}:\n{distanceFromDestination:.##} km";
                    UpdateIsArrived(distanceFromDestination);
                }
                else
                {
                    text = "No destination";
                }
            }
            else
            {
                text = "GPS permission denied";
            }
            uiThreadDispatcherQueue.TryEnqueue(() =>
            {
                TextBlockDistanceInformation.Text = text;
            });
        }
        void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdateLocation();
        }

        // ~~~~ DESTINATION SELECTION ~~~~ //

        private void DestinationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Destination = e.AddedItems.Count > 0 ? e.AddedItems[0] as DestinationInfo : null;
            HandleDestinationUpdate();
        }
        private void DestinationSelect_Click(object sender, RoutedEventArgs e)
        {
            Destination = (e.OriginalSource as MenuFlyoutItem).Tag as DestinationInfo;
            DestinationSelectComboBox.SelectedValue = Destination;
            HandleDestinationUpdate();
        }
        private void DestinationDelete_Click(object sender, RoutedEventArgs e)
        {
            var it = (e.OriginalSource as MenuFlyoutItem).Tag as DestinationInfo;
            DestinationInfos.Remove(it);
            if (Destination == it)
            {
                Destination = null;
                DestinationSelectComboBox.SelectedValue = Destination;
                HandleDestinationUpdate();
            }
        }
        void HandleDestinationUpdate()
        {
            justUpdatedDestination = true;
            HideArrivingToast();
            UpdateLocation();
        }


        // ~~~~ DESTINATION ADDING ~~~~ //

        private void AddDestinationX_TextChanged(object sender, TextChangedEventArgs e)
        {
            string name = AddDestinationName.Text;
            string latitudeStr = AddDestinationLatitude.Text;
            string longitudeStr = AddDestinationLongitude.Text;

            bool valid = true;
            float latitude, longitude;
            try
            {
                latitude = float.Parse(latitudeStr, CultureInfo.InvariantCulture);
                longitude = float.Parse(longitudeStr, CultureInfo.InvariantCulture);
            }
            catch (FormatException) { valid = false; }

            if (name.Length < 3) { valid = false; }

            AddDestinationButton.IsEnabled = valid;
        }

        private void AddDestinationButton_Click(object sender, RoutedEventArgs e)
        {
            string name = AddDestinationName.Text;
            string latitudeStr = AddDestinationLatitude.Text;
            string longitudeStr = AddDestinationLongitude.Text;

            double latitude, longitude;
            if (name.Length < 3) { return; }
            try
            {
                latitude = double.Parse(latitudeStr, CultureInfo.InvariantCulture);
                longitude = double.Parse(longitudeStr, CultureInfo.InvariantCulture);
            }
            catch (FormatException) { return; }

            DestinationInfos.Add(new()
            {
                Name = name,
                Pos = PolarPos.FromLatLonDeg(latitude, longitude)
            });
            AddDestinationName.Text = "";
            AddDestinationLatitude.Text = "";
            AddDestinationLongitude.Text = "";

        }

        // ~~~~ ARRIVAL DETECTION ~~~~ //

        private double arrivalDistanceThreshold = 2.0;
        private bool isArrived = false;
        private bool justUpdatedDestination = false;
        private void ArrivalDistanceThreshInput_LostFocus(object sender, RoutedEventArgs e)
        {

            try
            {
                arrivalDistanceThreshold = double.Parse(ArrivalDistanceThreshInput.Text, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                ArrivalDistanceThreshInput.Text = arrivalDistanceThreshold.ToString();
            }
        }

        private void UpdateIsArrived(double distance)
        {
            if (justUpdatedDestination)
            {
                // Allows the arrival toast to be shown immediately if already arrived.
                isArrived = false;
                justUpdatedDestination = false;
            }
            if (isArrived) { 
                // 500m buffer to prevent noise from causing rapid toggling.
                if (distance > arrivalDistanceThreshold + 0.5) {
                    isArrived = false;
                    HideArrivingToast();
                }
            } else
            {
                if (distance < arrivalDistanceThreshold)
                {
                    isArrived = true;
                    ShowArrivingToast();
                }
            }
        }

        // ~~~~ ARRIVAL ALARM ~~~~ //

        private readonly MediaPlayer alarmMediaPlayer = new()
        {
            Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/alert.wav")),
            IsLoopingEnabled = true,
        };
        private void SoundEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                alarmMediaPlayer.Position = TimeSpan.Zero;
                alarmMediaPlayer.Play();
            }
            else
                alarmMediaPlayer.Pause();
        }

        private readonly ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
        private ToastNotification toast = null;
        private void ShowArrivingToast()
        {
            SoundEnabled(true);
            var xmlPayload = new string($@"
            <toast>    
                <visual>    
                    <binding template=""ToastGeneric"">    
                        <text>Arriving at {Destination.Name}</text>
                    </binding>
                </visual>
                <actions>
                    <action content='Dismiss' arguments='action=dismiss'></action>
                </actions>
            </toast>");

            var doc = new XmlDocument();
            doc.LoadXml(xmlPayload);
            toast = new ToastNotification(doc)
            {
                ExpiresOnReboot = true,
            };
            toast.Dismissed += Toast_ActivatedDismissed;
            toast.Activated += Toast_ActivatedDismissed;
            notifier.Show(toast);
        }
        private void HideArrivingToast()
        {
            SoundEnabled(false);
            if (toast != null)
            {
                notifier.Hide(toast);
                toast = null;
            }
        }
        private void Toast_ActivatedDismissed(ToastNotification sender, object args)
        {
            SoundEnabled(false);
        }
    }
}
