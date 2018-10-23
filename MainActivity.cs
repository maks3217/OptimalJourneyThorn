using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Content.Res;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using System.Net;
using System.Threading.Tasks;
using static Android.Provider.SyncStateContract;
using Newtonsoft.Json;
using Android.Views;
using Android.Support.V4.Widget;
using System.Linq;
using Android.Locations;
using Android.Util;
using Android.Runtime;
using Plugin.Geolocator;
using Android;
using Android.Content.PM;
using Android.Support.V4.Content;
using Android.Support.V4.App;
using System.Threading;
using Android.Content;

namespace AndroidJakDojadevol1
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity, IOnMapReadyCallback
    {


        // Struck 

        public struct Stop
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public string ID { get; set; }
            public double Latitude { get; set; }
            public double Long { get; set; }
            public int Label { get; set; }
            public int PreviousConnection { get; set; }
            public int PreviousStop { get; set; }
            public bool FootPaths { get; set; }
        }

        public struct Connection
        {
            public int DepartureTime { get; set; }
            public int ArrivalTime { get; set; }
            public int StartingStop { get; set; }
            public int EndingStop { get; set; }
            public string LineNumber { get; set; }
            public int Trip { get; set; }
            public int PositionInTrip { get; set; }
        }
        const int RequestLocationId = 0;
        readonly string[] PermissionsLocation =
            {
                Manifest.Permission.AccessCoarseLocation,
                Manifest.Permission.AccessFineLocation
            };
        static readonly string TAG = "X:" + typeof(MainActivity).Name;
        private GoogleMap map;
        private Android.Support.V7.Widget.Toolbar toolbar;
        private DrawerLayout drawerLayout;
        private Button StopsButton;
        private Button FindButton;
        private TextView StartStopTextView;
        private TextView EndStopTextView;
        private CheckBox StartStopCheckBox;
        private CheckBox EndStopCheckBox;
        private ListView leftDrawer;
        private Spinner  HourSpinner;
        private Spinner MinutesSpinner;
        private ArrayAdapter HourSpinnerAdapter;
        private ArrayAdapter MinutesSpinnerAdapter;
        private ArrayAdapter JourneyAdapter;
        List<int> hours;
        List<int> minutes;
        static public List<Stop> Stops;
        static public List<Connection> Connections;
        static public List<int> SingleJourney = new List<int>();
        static public List<List<int>> Journey = new List<List<int>>();
        static public List<string> JourneyItems = new List<string>();
        static public Dictionary<string, int> StopDictionary;
        static public int[,] FootPaths;
        static public bool[] Trips = new bool[7284];
        double MylocationLat = 53.0308;
        double MylocationLong = 18.5929;
        int avarageTimeChanged;
        static public int walkingSpeed;
        int curentAvarageTimeChanged;
        int StartStopNumber;
        int EndStopNumber;
        bool StopSearchFlag = false;
        bool leftdrawertag = true;
        bool StopsButtonFlag = false;
        bool LocationAsyncFlag;
        private Marker LastStartMarker;
        private Marker LastEndMarker;
        private Marker MyLocation;
        Polyline polyline;
        WebClient webclient;

        static public ISharedPreferences pref = Application.Context.GetSharedPreferences("UserInfo", FileCreationMode.Private);

        static public int[,] GenFootPaths(StreamReader sr)
        {
            int i, j, value;
            string text;
            string[] bits;
            int[,] Fp = new int[Stops.Count, Stops.Count];
            using (sr)
            {
                while (!sr.EndOfStream)
                {
                    text = sr.ReadLine();
                    bits = text.Split(' ');
                    i = int.Parse(bits[0]);
                    j = int.Parse(bits[1]);
                    value = int.Parse(bits[2]);
                    Fp[i, j] = value;
                }
            }
            return Fp;
        }

        static public List<Stop> GenStopsList(StreamReader sr)
        {
            
            int count = 0;
            List<Stop> Stops = new List<Stop>();
            string line;
            string[] lineWords;
            Stop SingleStop = new Stop();
            using (sr)
            {
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    lineWords = line.Split('|');
                    SingleStop.ID = lineWords[0];
                    SingleStop.Name = lineWords[1];
                    SingleStop.Long = Double.Parse(lineWords[2], CultureInfo.InvariantCulture);
                    SingleStop.Latitude = Double.Parse(lineWords[3], CultureInfo.InvariantCulture);
                    SingleStop.Label = 0;
                    SingleStop.Number = count;
                    SingleStop.PreviousConnection = -1;
                    SingleStop.FootPaths = false;
                    SingleStop.PreviousStop = -1;
                    count++;
                    Stops.Add(SingleStop);
                }
            }
            return Stops;
        }

        static public Dictionary<string, int> GenDictionary(List<Stop> S)
        {
            Dictionary<string, int> D = new Dictionary<string, int>();
            foreach (Stop s in S)
            {
                D[s.ID] = s.Number;
            }
            return D;
        }

        static public List<Connection> GenConnectionsList(StreamReader sr)
        {
            List<Connection> Con = new List<Connection>();
            string line;
            string[] lineWords;
            Connection SingleCon = new Connection();
            using (sr)
            {
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    lineWords = line.Split(' ');

                    SingleCon.StartingStop = StopDictionary[lineWords[0]];
                    SingleCon.EndingStop = StopDictionary[lineWords[1]];
                    SingleCon.DepartureTime = DateToInt(lineWords[2]);
                    SingleCon.ArrivalTime = DateToInt(lineWords[3]);
                    SingleCon.LineNumber = lineWords[4];
                    SingleCon.Trip = Int32.Parse(lineWords[5]);
                    SingleCon.PositionInTrip = Int32.Parse(lineWords[6]);
                    Con.Add(SingleCon);
                }
            }
            return Con;
        }

        
        public void OnMapReady(GoogleMap googleMap)
        {

            map = googleMap;
            map.MarkerClick += map_MarkerClick;

            LatLng StartPosition = new LatLng(53.0308, 18.5929);
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(StartPosition, 14);
            map.MoveCamera(camera);
        }


        protected override void OnCreate(Bundle savedInstanceState)
        {
            
            AssetManager assets = this.Assets;
            StreamReader StopsReader = new StreamReader(assets.Open("przystanki.txt"));
            StreamReader ConnectionsReader = new StreamReader(assets.Open("polaczenia.txt"));
            StreamReader FootpathsReader = new StreamReader(assets.Open("sciezki.txt"));
            Stops = GenStopsList(StopsReader);
            StopDictionary = GenDictionary(Stops);
            Connections = GenConnectionsList(ConnectionsReader);
            FootPaths = GenFootPaths(FootpathsReader);
            Connections = Connections.OrderBy(s => s.DepartureTime).ToList();
           

            base.OnCreate(savedInstanceState);
            CultureInfo englishUSCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = englishUSCulture;

            // Mapa and Toolbar
            Xamarin.FormsMaps.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.Title = "Toruń";
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeButtonEnabled(true);
            MapFragment mapFragment = (MapFragment)FragmentManager.FindFragmentById(Resource.Id.map);
            mapFragment.GetMapAsync(this);
            //InitializeLocationManager();


            // Components
            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            leftDrawer = FindViewById<ListView>(Resource.Id.left_drawer);
            StopsButton = FindViewById<Button>(Resource.Id.StopsButton);
            FindButton = FindViewById<Button>(Resource.Id.FindButton);
            StartStopTextView = FindViewById<TextView>(Resource.Id.StartStopTextView);
            EndStopTextView = FindViewById<TextView>(Resource.Id.EndStopTextView);
            StartStopCheckBox = FindViewById<CheckBox>(Resource.Id.StartStopCheckBox);
            EndStopCheckBox = FindViewById<CheckBox>(Resource.Id.EndStopCheckBox);
            HourSpinner = FindViewById<Spinner>(Resource.Id.hspinner);
            MinutesSpinner = FindViewById<Spinner>(Resource.Id.mspinner);

            string walkingSpeedstr = pref.GetString("WalkingSpeed", string.Empty);
            string timeChangedstr = pref.GetString("MinimalTimeChanged", string.Empty);

            if (walkingSpeedstr == string.Empty)
            {
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutString("WalkingSpeed", "3"); // domyślna wartość
                editor.Apply();
                walkingSpeed = 3;
            }
            else walkingSpeed = Int32.Parse(walkingSpeedstr);
            if (timeChangedstr == string.Empty)
            {
                ISharedPreferencesEditor editor = pref.Edit();
                editor.PutString("MinimalTimeChanged", "1"); // domyślna wartość
                editor.Apply();
                curentAvarageTimeChanged = 1;
            }
            else curentAvarageTimeChanged = Int32.Parse(timeChangedstr);


            // events 

            StopsButton.Click += delegate
            {   
                DrawMarkers();
            };

            StartStopCheckBox.Click += delegate
            {
                if (StartStopCheckBox.Checked)
                    EndStopCheckBox.Checked = false;
                else EndStopCheckBox.Checked = true;
            };

            EndStopCheckBox.Click += delegate
            {
                if (StartStopCheckBox.Checked)
                    StartStopCheckBox.Checked = false;
                else StartStopCheckBox.Checked = true;
            };

            FindButton.Click += delegate
            {
                walkingSpeed = Int32.Parse(pref.GetString("WalkingSpeed", string.Empty));
                curentAvarageTimeChanged = Int32.Parse(pref.GetString("MinimalTimeChanged", string.Empty));
                ClearStopListLabel();
                ClearTrips();
                int DepTime = Int32.Parse(HourSpinner.SelectedItem.ToString()) * 60 + Int32.Parse(MinutesSpinner.SelectedItem.ToString());
                Stop s = Stops[StartStopNumber];
                s.Label = DepTime;
                Stops[StartStopNumber] = s;
                avarageTimeChanged = 0;
                RelaxesFootPaths(Stops[StartStopNumber]);
                EarliestArrivalTime(0);
                if (StopSearchFlag) EarliestArrivalTime(3600);
                Journey.Clear();
                JourneyItems.Clear();
                map.Clear();
                DrawMarkers();
                CreateJourney(Stops[EndStopNumber]);
                PrintJourney();
                JourneyAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, JourneyItems);
                leftDrawer.Adapter = JourneyAdapter;
            };

            hours = HourDate();
            minutes = MinutesDate();
            HourSpinnerAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, hours);
            MinutesSpinnerAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, minutes);
            HourSpinner.Adapter = HourSpinnerAdapter;
            MinutesSpinner.Adapter = MinutesSpinnerAdapter;
           
        }

        public void EarliestArrivalTime(int next_day)
        { 
            try
            {
                StopSearchFlag = true; ;
                Stop s;
                int ConnectionNumber = 0;
                foreach (Connection c in Connections)
                {
                    if (c.DepartureTime >= Stops[EndStopNumber].Label)
                    {
                        StopSearchFlag = false;
                        break;
                    }
                    if (Trips[c.Trip])
                    {
                        if (Stops[c.EndingStop].Label > c.ArrivalTime + next_day)
                        {
                            s = Stops[c.EndingStop];
                            s.Label = c.ArrivalTime + next_day;
                            s.PreviousConnection = ConnectionNumber;
                            s.PreviousStop = c.StartingStop;
                            s.FootPaths = false;
                            Stops[c.EndingStop] = s;
                            RelaxesFootPaths(Stops[c.EndingStop]);
                        }
                    }
                    else
                    {

                        if (Stops[c.StartingStop].Label + avarageTimeChanged <= c.DepartureTime + next_day)
                        {

                            if (Stops[c.EndingStop].Label > c.ArrivalTime + next_day)
                            {
                                s = Stops[c.EndingStop];
                                s.Label = c.ArrivalTime + next_day;
                                s.PreviousConnection = ConnectionNumber;
                                s.PreviousStop = c.StartingStop;
                                s.FootPaths = false;
                                Stops[c.EndingStop] = s;
                                RelaxesFootPaths(Stops[c.EndingStop]);
                                Trips[c.Trip] = true;
                            }
                        }
                    }
                    ConnectionNumber++;
                    avarageTimeChanged = curentAvarageTimeChanged;
                }
            }
            catch
            {
                
            }

        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.toolbar_menu, menu);
            return base.OnCreateOptionsMenu(menu);
        }
       
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                case Android.Resource.Id.Home:
                    if (leftdrawertag)
                        drawerLayout.OpenDrawer(leftDrawer);
                    else
                        drawerLayout.CloseDrawer(leftDrawer);
                    leftdrawertag = !leftdrawertag;
                    return true;
                case Resource.Id.action_info:
                    Android.App.AlertDialog.Builder alertDialogInfo = new Android.App.AlertDialog.Builder(this);
                    alertDialogInfo.SetTitle("Informacje");
                    alertDialogInfo.SetMessage(Resources.GetString(Resource.String.info));
                    alertDialogInfo.SetNeutralButton("Ok", delegate
                    {
                        alertDialogInfo.Dispose();
                    });
                    alertDialogInfo.Show();
                    return true;

                case Resource.Id.action_help:
                    Android.App.AlertDialog.Builder alertDialogHelp = new Android.App.AlertDialog.Builder(this);
                    alertDialogHelp.SetTitle("Pomoc");
                    alertDialogHelp.SetMessage(Resources.GetString(Resource.String.pomoc));
                    alertDialogHelp.SetNeutralButton("Ok", delegate
                    {
                        alertDialogHelp.Dispose();
                    });
                    alertDialogHelp.Show();
                    return true;

                case Resource.Id.action_settings:
                    Android.App.FragmentTransaction transaction = FragmentManager.BeginTransaction();
                    Settings settings = new Settings();
                    settings.Show(transaction, "Ustawienia");
                    return true;

                case Resource.Id.action_location:
                    LocationAsyncFlag = true;
                    GetLocationCompatAsync();
                    if(MyLocation == null)
                    {
                        MarkerOptions m = new MarkerOptions()
                            .SetPosition(new LatLng(MylocationLat, MylocationLong))
                            .SetTitle("Moja Pozycja")
                            .SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.MBlue));
                        MyLocation = map.AddMarker(m);
                    }
                    MyLocation.Position = new LatLng(MylocationLat, MylocationLong);
                    //CameraPosition.Builder builder = CameraPosition.InvokeBuilder();
                    //builder.Target(new LatLng(MylocationLat, MylocationLong));
                    //CameraPosition cameraPosition = builder.Build();
                    //CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);
                    //map.MoveCamera(cameraUpdate);

                    CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(new LatLng(MylocationLat, MylocationLong), 14);
                    map.MoveCamera(camera);

                    return true;
                
            }
            return base.OnOptionsItemSelected(item);
        }

        void map_MarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            e.Marker.ShowInfoWindow();
            if (e.Marker.Snippet != null)
            {
                if (StartStopCheckBox.Checked)
                {
                    if (LastStartMarker != null)
                        LastStartMarker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.if_Black_pin_132048));
                    e.Marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.Zielony));
                    LastStartMarker = e.Marker;
                    StartStopNumber = Int32.Parse(e.Marker.Snippet);
                    StartStopTextView.Text = e.Marker.Title;
                }
                if (EndStopCheckBox.Checked)
                {
                    if (LastEndMarker != null)
                        LastEndMarker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.if_Black_pin_132048 ));
                    e.Marker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.Czerwony));
                    LastEndMarker = e.Marker;
                    EndStopNumber = Int32.Parse(e.Marker.Snippet);
                    EndStopTextView.Text = e.Marker.Title;
                }
            }

        }

        void DrawMarkers()
        {
            bool flag;
            MarkerOptions StopMarker;
            foreach (Stop s in Stops)
            {
                flag = true;
                StopMarker = new MarkerOptions()
                .SetPosition(new LatLng(s.Latitude, s.Long))
                .SetTitle(s.Name)
                .SetSnippet(s.Number.ToString())
                .SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.if_Black_pin_132048));
                if (LastEndMarker != null && LastEndMarker.Position.Equals(StopMarker.Position))
                {
                    StopMarker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.Czerwony));
                    LastEndMarker = map.AddMarker(StopMarker);
                    flag = false;
                }
                if (LastStartMarker != null && LastStartMarker.Position.Equals(StopMarker.Position))
                {
                    StopMarker.SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.Zielony));
                    LastStartMarker = map.AddMarker(StopMarker);
                    flag = false;
                }
                if (flag)
                {
                    map.AddMarker(StopMarker);
                }
            } 
        }
        
        void DrawRoute(double startLan, double startLong, double endLan, double endLong, Color color, string walkingMode)
        {
            string strFullDirectionURL = "https://maps.googleapis.com/maps/api/directions/json?origin=" + startLan.ToString() + "," + startLong.ToString() +
                "&destination=" + endLan.ToString() + "," + endLong.ToString() + walkingMode + "&key=AIzaSyCLH38UIQs-2A43IhxYLnvXSe6ShZLe5lE";
            string strJSONDirectionResponse = FnHttpRequest(strFullDirectionURL);
            FnSetDirectionQuery(strJSONDirectionResponse, color);
            

        }

        void FnSetDirectionQuery(string strJSONDirectionResponse, Color color)
        {
            var objRoutes = JsonConvert.DeserializeObject<GoogleDirectionClass>(strJSONDirectionResponse);
            //objRoutes.routes.Count  --may be more then one 
            if (objRoutes.routes.Count > 0)
            {
                string encodedPoints = objRoutes.routes[0].overview_polyline.points;

                var lstDecodedPoints = FnDecodePolylinePoints(encodedPoints);
                //convert list of location point to array of latlng type
                var latLngPoints = new LatLng[lstDecodedPoints.Count];
                int index = 0;
                foreach (Location loc in lstDecodedPoints) 
                {
                    latLngPoints[index++] = new LatLng(loc.lat, loc.lng);
                }

                var polylineoption = new PolylineOptions();
                polylineoption.InvokeColor(color);
                polylineoption.Geodesic(true);
                polylineoption.Add(latLngPoints);
                RunOnUiThread(() =>
              map.AddPolyline(polylineoption));
                
            }
            else
            {
                Android.App.AlertDialog.Builder alertDialogInfo = new Android.App.AlertDialog.Builder(this);
                alertDialogInfo.SetTitle("Informacje");
                alertDialogInfo.SetMessage("Nieprawidłowy separator. Aby rysować trasy należy zmieć język na Angielski");
                alertDialogInfo.SetNeutralButton("Ok", delegate
                {
                    alertDialogInfo.Dispose();
                });
                alertDialogInfo.Show();
            }
        }

        string FnHttpRequest(string strUri)
        {
            webclient = new WebClient();
            string strResultData;
            try
            {
                strResultData = webclient.DownloadString(new Uri(strUri));
                
            }
            catch
            {
                strResultData = "Błąd";
            }
            finally
            {
                if (webclient != null)
                {
                    webclient.Dispose();
                    webclient = null;
                }
            }
            return strResultData;
        }

        List<Location> FnDecodePolylinePoints(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                return null;
            var poly = new List<Location>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            try
            {
                while (index < polylinechars.Length)
                {
                    // calculate next latitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length)
                        break;

                    currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                    //calculate next longitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length && next5bits >= 32)
                        break;

                    currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                    Location p = new Location();
                    p.lat = Convert.ToDouble(currentLat) / 100000.0;
                    p.lng = Convert.ToDouble(currentLng) / 100000.0;
                    poly.Add(p);
                }
            }
            catch
            {
                RunOnUiThread(() =>
                  Toast.MakeText(this, "Please Wait", ToastLength.Short).Show());
            }
            return poly;
        }

        static public int DateToInt(string date)
        {
            TimeSpan ts = TimeSpan.Parse(date);
            int minutes = (int)ts.TotalMinutes;
            return minutes;
            /*
            int time = 0;
            string[] dateAr = date.Split(':');
            time += Int32.Parse(dateAr[0]) * 60;
            time += Int32.Parse(dateAr[1]);
            return time;
            */
        }

        static public string IntToTime(int time)
        {
            time = time % 1440;
            string s = (time / 60) + ":";
            if (time % 60 > 9) s += time % 60;
            else s += "0" + time % 60;
            return s;
        }

        static public List<int> HourDate()
        {
            List<int> h = new List<int>();
            for (int i = 0; i < 24; i++)
                h.Add(i);
            return h;

        }

        static public List<int> MinutesDate()
        {
            List<int> h = new List<int>();
            for (int i = 0; i < 60; i++)
                h.Add(i);
            return h;
        }

        static public void ClearStopListLabel()
        {
            for (int i = 0; i < Stops.Count; i++)
            {
                Stop s = Stops[i];
                s.Label = 10000000;
                s.PreviousConnection = -1;
                s.PreviousStop = -1;
                s.FootPaths = false;
                Stops[i] = s;
            }

        }
        
        static public void ClearTrips()
        {
            for (int i = 0; i < 7284; i++)
                Trips[i] = false;
        }

        static public void RelaxesFootPaths(Stop stop)
        {
            for (int i = 0; i < Stops.Count; i++)
            {
                double walkTime = FootPaths[Stops[i].Number, stop.Number] * (3.0 / walkingSpeed); // takie rozwiązanie ze względu na to, iż w pierwotnym pliku zawierającym ścieżki
                int min = (int)Math.Ceiling(walkTime);                                           // znajdowały się czasy dzielące przystanki, przy stałej prędkości 3 km/h
                Stop tmp;                                                                        
                if (min + stop.Label < Stops[i].Label)
                {
                    tmp = Stops[i];
                    tmp.Label = min + stop.Label;
                    tmp.PreviousStop = stop.Number;
                    tmp.FootPaths = true;
                    Stops[i] = tmp;
                }
            }
        }

        static public void CreateJourney(Stop stop)
        {
            if (stop.PreviousConnection != -1 || stop.FootPaths)
            {
                if (stop.PreviousConnection != -1)
                {
                    CreateJourney(Stops[stop.PreviousStop]);
                    SingleJourney = new List<int>(new int[]
                    {1, stop.PreviousStop, stop.Number, Connections[stop.PreviousConnection].DepartureTime, Connections[stop.PreviousConnection].ArrivalTime, Connections[stop.PreviousConnection].Trip, stop.PreviousConnection});
                    Journey.Add(SingleJourney);
                }
                else
                {
                    CreateJourney(Stops[stop.PreviousStop]);
                    SingleJourney = new List<int>(new int[]
                    {0, stop.PreviousStop ,stop.Number, Stops[stop.PreviousStop].Label, Stops[stop.PreviousStop].Label + (int)Math.Ceiling(FootPaths[stop.PreviousStop,stop.Number] * (3.0/walkingSpeed)), (int)Math.Ceiling(FootPaths[stop.PreviousStop,stop.Number] * (3.0/walkingSpeed))});
                    Journey.Add(SingleJourney);
                }
            }
        }

        public void PrintJourney()
        {
            bool ConnectionFlag = false;
            string lineNumber = "";
            string item = "";
            bool colorFlag = true;
            int previousTrip = -1;
            int TripStartStop = -1;
            int TripEndStop = -1;
            int TripStartTime = -1;
            int TripEndTime = -1;
            int CountOfStops = 1;
            Color color;
            string walkinmode = "&mode=walking";
            foreach (List<int> list in Journey)
            {
                if (list[0] == 0)
                {
                    if (previousTrip != -1)
                    {
                        item = "Jedź " + CountOfStops+ " " + Odmien(CountOfStops) + " linią " + lineNumber + " \n " + " do przystanku " + Stops[TripEndStop].Name + "  "
                            + IntToTime(TripStartTime) + " - " + IntToTime(TripEndTime);
                        JourneyItems.Add(item);
                        if (colorFlag) color = Color.LightGreen;
                        else color = Color.Red;
                        DrawRoute(Stops[TripStartStop].Latitude, Stops[TripStartStop].Long , Stops[TripEndStop].Latitude, Stops[TripEndStop].Long , color, "");
                        previousTrip = -1;
                        ConnectionFlag = false;
                    }

                    item = "Przejdź spacerkiem do przystanku " + Stops[list[2]].Name + " \n " + list[5] + " min";
                    JourneyItems.Add(item);
                    color = Color.Gray;
                    DrawRoute(Stops[list[1]].Latitude, Stops[list[1]].Long, Stops[list[2]].Latitude, Stops[list[2]].Long , color, walkinmode);
                }
                else
                {
                    ConnectionFlag = true;
                    if (previousTrip == -1)
                    {
                        previousTrip = list[5];
                        TripStartStop = list[1];
                        TripEndStop = list[2];
                        TripStartTime = list[3];
                        TripEndTime = list[4];
                        lineNumber = Connections[list[6]].LineNumber;
                    }
                    else
                    {
                        if (previousTrip == list[5])
                        {
                            CountOfStops++;
                            TripEndStop = list[2];
                            TripEndTime = list[4];
                        }
                        else
                        {
                            item = "Jedź " + CountOfStops + " " + Odmien(CountOfStops) + " linią " + lineNumber + " \n " + " do przystanku " + Stops[TripEndStop].Name + "  "
                            + IntToTime(TripStartTime) + " - " + IntToTime(TripEndTime);
                            JourneyItems.Add(item);
                            if (colorFlag) color = Color.LightGreen;
                            else color = Color.Red;
                            DrawRoute(Stops[TripStartStop].Latitude, Stops[TripStartStop].Long, Stops[TripEndStop].Latitude, Stops[TripEndStop].Long, color, "");
                            colorFlag = !colorFlag;
                            CountOfStops = 1;
                            previousTrip = list[5];
                            TripStartStop = list[1];
                            TripEndStop = list[2];
                            TripStartTime = list[3];
                            TripEndTime = list[4];
                            lineNumber = Connections[list[6]].LineNumber;
                        }
                    }
                }
            }
            if (ConnectionFlag)
            {
                item = "Jedź " + CountOfStops + " " + Odmien(CountOfStops) +" linią " + lineNumber + "\n" + " do przystanku " + Stops[TripEndStop].Name + "  "
                               + IntToTime(TripStartTime) + " - " + IntToTime(TripEndTime);
                JourneyItems.Add(item);
                if (colorFlag) color = Color.LightGreen;
                else color = Color.Red;
                DrawRoute(Stops[TripStartStop].Latitude, Stops[TripStartStop].Long, Stops[TripEndStop].Latitude, Stops[TripEndStop].Long , color, "");
            }
        }

        int BinaryFind(int l, int p, long x)
        {
            int sr = 0;
            while (l <= p)
            {
                sr = (l + p) / 2;

                if (Connections[sr].DepartureTime == x)
                    return sr;

                if (Connections[sr].DepartureTime > x)
                    p = sr - 1;
                else
                    l = sr + 1;
            }

            return sr; //zwracamy -1, gdy nie znajdziemy elementu
        }

        async Task GetLocationCompatAsync()
        {
            const string permission = Manifest.Permission.AccessFineLocation;

            if (ContextCompat.CheckSelfPermission(this, permission) == (int)Permission.Granted)
            {
                await GetLocationAsync();
                return;
            }

            if (ActivityCompat.ShouldShowRequestPermissionRationale(this, permission))
            {
                

                return;
            }

            RequestPermissions(PermissionsLocation, RequestLocationId);
        }

        async Task GetLocationAsync()
        {

            try
            {
                var locator = CrossGeolocator.Current;
                locator.DesiredAccuracy = 100;
 
                var position = await locator.GetPositionAsync();
                MylocationLat = position.Latitude;
                MylocationLong = position.Longitude;
                LocationAsyncFlag = false;
            }
            catch (Exception ex)
            {

                StartStopTextView.Text = "Unable to get location: " + ex.ToString();
            }
        }

        string Odmien(int count)
        {
            if (count == 1)
                return "przystanek";
            else
            {
                if (count < 5)
                    return "przystanki";
                else
                    return "przystanków";
            }
        }
     

    }
}

