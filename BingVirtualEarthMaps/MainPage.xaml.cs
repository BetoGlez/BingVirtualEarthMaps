using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls.Maps;
using Windows.Devices.Geolocation;
using Windows.UI;
using System.Net.Http;
using BingVirtualEarthMaps.Classes;
using Newtonsoft.Json;
using Windows.UI.Notifications;

namespace BingVirtualEarthMaps
{
    public sealed partial class MainPage : Page
    {
        private readonly string MAP_ACCS = "UbyVV4Ma5EM8zXJ44OZi%7EE0q2RVZqdjQ2CX1z9HHMZw%7EAlQ1dCMOGkoxR9h0Gctn4QncW1KHvfVz_lvwqEobK-U2fcAQBw9z5hi9gWV6i2NU";
        private readonly string BASE_API_URL = "http://dev.virtualearth.net/REST/V1/Routes/Driving";

        public MainPage()
        {
            this.InitializeComponent();

            mapViewRoutes.MapServiceToken = MAP_ACCS; 
        }

        private async void SetTravelRoute(object sender, RoutedEventArgs e)
        {
            string placeA = originLocationTextBox.Text;
            string placeB = endLocationTextBox.Text;

            if (!placeA.Trim().Equals("") && !placeB.Trim().Equals(""))
            {
                ResetRouteData();

                HttpClient httpClient = new HttpClient();
                HttpResponseMessage rawRouteResponse = await httpClient.GetAsync(ComposeApiUrl(placeA, placeB));
                if (rawRouteResponse.IsSuccessStatusCode)
                {
                    string stringResponse = await rawRouteResponse.Content.ReadAsStringAsync();
                    RouteData routeData = JsonConvert.DeserializeObject<RouteData>(stringResponse);
                
                    DisplayDirections(routeData);
                    DisplayRouteData(routeData);
                    DisplayMapRoute(routeData);
                } else
                {
                    ShowToastNotification("Error calculating route", "No route was found for the provided destinations");
                }
            } else
            {
                ShowToastNotification("Missing data", "Please provide a start and end destination in order to calculate the route");
            }
        }

        private String ComposeApiUrl(String placeA, String placeB)
        {
            return BASE_API_URL + "?wp.0=" + placeA + "&wp.1=" + placeB + "&routeAttributes=routePath&output=json&key=" + MAP_ACCS;
        }

        private void ResetRouteData()
        {
            mapViewRoutes.MapElements.Clear();
            for (int i = 0; i < mapViewRoutes.Layers.Count; i++) {
                mapViewRoutes.Layers[i].Visible = false;
            }
            routeDirectionsTextBlock.Text = "";
            routeDataTextBlock.Text = "";
        }

        private void DisplayMapRoute(RouteData routeData)
        {
            Resource resources = routeData.resourceSets[0].resources[0];

            RouteLeg routeLeg = resources.routeLegs[0];
            List<double> startLocationCoords = routeLeg.startLocation.point.coordinates;
            List<double> endLocationCoords = routeLeg.endLocation.point.coordinates;
            CenterMapToCoords(startLocationCoords[0], startLocationCoords[1]);
            DrawLocationPoint(startLocationCoords[0], startLocationCoords[1], routeLeg.startLocation.name);
            DrawLocationPoint(endLocationCoords[0], endLocationCoords[1], routeLeg.endLocation.name);

            List<List<double>> routePathCoords = resources.routePath.line.coordinates;
            for (int i = 0; i < routePathCoords.Count; i++)
            {
                if (i + 1 < routePathCoords.Count)
                {
                    DrawMapLine(routePathCoords[i], routePathCoords[i + 1]);
                }
            }
        }

        private void DisplayDirections(RouteData routeData)
        {
            RouteLeg routeLeg = routeData.resourceSets[0].resources[0].routeLegs[0];
            List<ItineraryItem> routePoints = routeLeg.itineraryItems;
            string routeInstructions = "";
            for (int i = 0; i < routePoints.Count; i++)
            {
                routeInstructions += " " + (i + 1) + ". " + routePoints[i].instruction.text + "\n";
            }
            routeDirectionsTextBlock.Text = routeInstructions;
        }

        private void DisplayRouteData(RouteData routeData)
        {
            Resource resource = routeData.resourceSets[0].resources[0];
            string routeOrigin = " • Start location: " + resource.routeLegs[0].startLocation.name;
            string routeEnd = " • End location: " + resource.routeLegs[0].endLocation.name;
            string routeDistance = " • Distance: " + resource.travelDistance + " kilometers";
            TimeSpan rawTime = TimeSpan.FromSeconds(resource.travelDuration);
            string formattedTime = rawTime.ToString(@"hh\:mm\:ss");
            string routeTime = " • Duration: " + formattedTime;
            string routeTraffic = " • Traffic: " + resource.trafficCongestion;

            string routeStatisticData = routeOrigin + "\n" + routeEnd + "\n" + routeDistance + "\n" + routeTime + "\n" + routeTraffic;

            routeDataTextBlock.Text = routeStatisticData;
        }

        private void CenterMapToCoords(double latitude, double longitude)
        {
            BasicGeoposition startPosition = new BasicGeoposition() { Latitude = latitude, Longitude = longitude };
            mapViewRoutes.Center = new Geopoint(startPosition);
            mapViewRoutes.ZoomLevel = 12;
        }

        private void DrawLocationPoint(double latitude, double longitude, String title)
        {
            BasicGeoposition startPosition = new BasicGeoposition() { Latitude = latitude, Longitude = longitude };
            Geopoint centerPosition = new Geopoint(startPosition);
            MapIcon myPOI = new MapIcon
            {
                Location = centerPosition,
                NormalizedAnchorPoint = new Windows.Foundation.Point(0.5, 1.0),
                Title = title,
                ZIndex = 0
            };
            mapViewRoutes.MapElements.Add(myPOI);
        }

        private void DrawMapLine(List<double> originCoords, List<double> endCoords)
        {
            // Define coords
            double originLatitude = originCoords[0];
            double originLongitude = originCoords[1];
            double endLatitude = endCoords[0];
            double endLongitude = endCoords[1];

            // Create Polyline
            var mapPolyline = new MapPolyline
            {
                Path = new Geopath(new List<BasicGeoposition> {
                    new BasicGeoposition() {Latitude=originLatitude, Longitude=originLongitude },
                    new BasicGeoposition() {Latitude=endLatitude, Longitude=endLongitude },
                }),
                StrokeColor = Colors.Red,
                StrokeThickness = 5
            };

            // Add Polyline to a layer on the map control.
            var MapLines = new List<MapElement>
            {
                mapPolyline
            };

            var LinesLayer = new MapElementsLayer
            {
                ZIndex = 1,
                MapElements = MapLines
            };

            mapViewRoutes.Layers.Add(LinesLayer);
        }

        private void ShowToastNotification(string title, string stringContent)
        {
            ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
            Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
            toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
            toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(stringContent));
            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            Windows.Data.Xml.Dom.XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);
            ToastNotifier.Show(toast);
        }
    }
}
