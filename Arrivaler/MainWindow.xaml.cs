using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

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
        private GeolocationUtil geolocationUtil = null;
        private DispatcherQueue uiThreadDispatcherQueue;

        private async Task<bool> HasGeolocation()
        {
            geolocationUtil ??= new GeolocationUtil(
                await Geolocator.RequestAccessAsync() == GeolocationAccessStatus.Allowed
            );
            return geolocationUtil.hasAccess;
        }

        public MainWindow()
        {
            this.InitializeComponent();

            DestinationInfos = new ObservableCollection<DestinationInfo>
            {
                new DestinationInfo { Name = "Null Island", Pos = new(0.0, 0.0) },
                new DestinationInfo { Name = "Penguinlandia", Pos = new(-Math.PI/2, 0.0) },
            };

            var t = new System.Timers.Timer(1000.0)
            {
                AutoReset = true,
            };
            t.Elapsed += new System.Timers.ElapsedEventHandler(TimerElapsed);
            t.Start();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            uiThreadDispatcherQueue = DispatcherQueue.GetForCurrentThread();
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

        private void DestinationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Destination = e.AddedItems.Count > 0 ? e.AddedItems[0] as DestinationInfo : null;
            UpdateLocation();
        }

        private void DestinationSelect_Click(object sender, RoutedEventArgs e)
        {
            Destination = (e.OriginalSource as MenuFlyoutItem).Tag as DestinationInfo;
            DestinationSelectComboBox.SelectedValue = Destination;
            UpdateLocation();
        }
        private void DestinationDelete_Click(object sender, RoutedEventArgs e)
        {
            var it = (e.OriginalSource as MenuFlyoutItem).Tag as DestinationInfo;
            DestinationInfos.Remove(it);
            if (Destination == it)
            {
                Destination = null;
                DestinationSelectComboBox.SelectedValue = Destination;
                UpdateLocation();
            }
        }

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

            float latitude, longitude;
            if (name.Length < 3) { return; }
            try
            {
                latitude = float.Parse(latitudeStr, CultureInfo.InvariantCulture);
                longitude = float.Parse(longitudeStr, CultureInfo.InvariantCulture);
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
    }
}
