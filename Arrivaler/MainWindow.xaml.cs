using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Arrivaler
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
        }

        private async void btnGetLocation_Click(object sender, RoutedEventArgs e)
        {
            btnGetLocation.Content = "Thonking...";
            // Request permission to access location
            var accessStatus = await Geolocator.RequestAccessAsync();

            var destination = PolarPos.FromLatLonDeg(0.0, 0.0);

            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                Geolocator geolocator = new Geolocator();
                geolocator.DesiredAccuracyInMeters = 50;

                try
                {
                    Geoposition pos = await geolocator.GetGeopositionAsync();

                    double distanceFromDestination = MathHelper.ArcDistance(
                        destination,
                        PolarPos.FromLatLonDeg(
                            pos.Coordinate.Point.Position.Latitude,
                            pos.Coordinate.Point.Position.Longitude
                        )
                    ) * MathHelper.EARTH_RADIUS_KM;

                    // Display the latitude and longitude in the text block
                    // btnGetLocation.Content = $"Latitude: {pos.Coordinate.Point.Position.Latitude}, Longitude: {pos.Coordinate.Point.Position.Longitude}";
                    btnGetLocation.Content = $"Distance from destination: {distanceFromDestination.ToString(".##")} km";
                }
                catch (UnauthorizedAccessException)
                {
                    btnGetLocation.Content = "Location access denied.";
                }
                catch (Exception ex)
                {
                    btnGetLocation.Content = $"Error getting location: {ex.Message}";
                }
            }
            else
            {
                btnGetLocation.Content = "Location access denied.";
            }
        }
    }
}
