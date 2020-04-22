using Microsoft.EntityFrameworkCore;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Geocaching
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Contains the ID string needed to use the Bing map.
        // Instructions here: https://docs.microsoft.com/en-us/bingmaps/getting-started/bing-maps-dev-center-help/getting-a-bing-maps-key
        private const string applicationId = "AoHiRfJOKEHPxXg0_ZAO8A6qYZGydiITE7MBtd8WpfoFBid2eF9TmzfSMmozld5e";

        private MapLayer layer;

        // Contains the location of the latest click on the map.
        // The Location object in turn contains information like longitude and latitude.
        private Location latestClickLocation;

        private Location gothenburg = new Location(57.719021, 11.991202);

        public class AppDbContext : DbContext
        {
            public DbSet<Person> Person { get; set; }
            public DbSet<Geocache> Geocache { get; set; }
            public DbSet<FoundGeocache> FoundGeocaches { get; set; }

            protected override void OnConfiguring(DbContextOptionsBuilder options)
            {
                options.UseSqlServer(@"Data Source=(local)\SQLEXPRESS;Initial Catalog=Geocaching;Integrated Security=True");
            }

            protected override void OnModelCreating(ModelBuilder model)
            {
                model.Entity<FoundGeocache>()
                    .HasKey(fg => new { fg.PersonID, fg.GeocacheID });
                model.Entity<FoundGeocache>()
                    .HasOne(fg => fg.Person)
                    .WithMany(p => p.FoundGeocaches)
                    .HasForeignKey(fg => fg.PersonID);
                model.Entity<FoundGeocache>()
                    .HasOne(fg => fg.Geocache)
                    .WithMany(g => g.FoundGeocaches)
                    .HasForeignKey(fg => fg.GeocacheID);

            }
        }

        public class Person
        {
            public int ID { get; set; }
            [Required]
            [MaxLength(50)]
            public string FirstName { get; set; }
            [Required]
            [MaxLength(50)]
            public string LastName { get; set; }
            [Required]
            public double Latitude { get; set; }
            [Required]
            public double Longitude { get; set; }
            [Required]
            [MaxLength(50)]
            public string Country { get; set; }
            [Required]
            [MaxLength(50)]
            public string City { get; set; }
            [Required]
            [MaxLength(50)]
            public string StreetName { get; set; }
            [Required]
            public byte StreetNumber { get; set; }
            public List<FoundGeocache> FoundGeocaches { get; set; }
        }

        public class Geocache
        {
            [DatabaseGenerated(DatabaseGeneratedOption.None)]
            public int ID { get; set; }
            public Person Person { get; set; }
            [Required]
            public double Latitude { get; set; }
            [Required]
            public double Longitude { get; set; }
            [Required]
            [MaxLength(255)]
            public string Contents { get; set; }
            [Required]
            [MaxLength(255)]
            public string Message { get; set; }
            public List<FoundGeocache> FoundGeocaches { get; set; }
        }

        public class FoundGeocache
        {
            public int PersonID { get; set; }
            public Person Person { get; set; }
            public int GeocacheID { get; set; }
            public Geocache Geocache { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Start();
        }
        static AppDbContext db = new AppDbContext();
        private Person activePerson = new Person();
        private List<Person> people = new List<Person>();
        private List<Geocache> geocaches = new List<Geocache>();
        private List<FoundGeocache> foundGeocaches = new List<FoundGeocache>();

        private void Start()
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            if (applicationId == null)
            {
                MessageBox.Show("Please set the applicationId variable before running this program.");
                Environment.Exit(0);
            }
            CreateMap();
            UpdateMap();
        }

        private void CreateMap()
        {
            map.CredentialsProvider = new ApplicationIdCredentialsProvider(applicationId);
            map.Center = gothenburg;
            map.ZoomLevel = 12;
            layer = new MapLayer();
            map.Children.Add(layer);

            MouseDown += (sender, e) =>
            {
                var point = e.GetPosition(this);
                latestClickLocation = map.ViewportPointToLocation(point);

                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    OnMapLeftClick();
                }
            };

            map.ContextMenu = new ContextMenu();

            var addPersonMenuItem = new MenuItem { Header = "Add Person" };
            map.ContextMenu.Items.Add(addPersonMenuItem);
            addPersonMenuItem.Click += OnAddPersonClick;

            var addGeocacheMenuItem = new MenuItem { Header = "Add Geocache" };
            map.ContextMenu.Items.Add(addGeocacheMenuItem);
            addGeocacheMenuItem.Click += OnAddGeocacheClick;
        }

        private void UpdateMap()
        {
            // It is recommended (but optional) to use this method for setting the color and opacity of each pin after every user interaction that might change something.
            // This method should then be called once after every significant action, such as clicking on a pin, clicking on the map, or clicking a context menu option.
            layer.Children.Clear();
            LoadDataFromDB();
            foreach (var person in people)
            {
                if (activePerson.FirstName != null)
                {
                    if (activePerson.ID == person.ID)
                    {
                        Location location = new Location();
                        location.Latitude = person.Latitude;
                        location.Longitude = person.Longitude;
                        string tooltip = GetPersonTooltip(person);
                        var pin = AddPin(location, tooltip, Colors.Blue);
                        pin.Opacity = 1;
                        pin.MouseDown += (s, a) =>
                        {
                            // Handle click on person pin here.                          
                            activePerson = person;
                            UpdateMap();
                            // Prevent click from being triggered on map.
                            a.Handled = true;
                        };
                    }
                    else
                    {
                        Location location = new Location();
                        location.Latitude = person.Latitude;
                        location.Longitude = person.Longitude;
                        string tooltip = GetPersonTooltip(person);
                        var pin = AddPin(location, tooltip, Colors.Blue);
                        pin.Opacity = 0.5;
                        pin.MouseDown += (s, a) =>
                        {                       
                            activePerson = person;
                            UpdateMap();
                            a.Handled = true;
                        };
                    }

                }
                else
                {
                    Location location = new Location();
                    location.Latitude = person.Latitude;
                    location.Longitude = person.Longitude;
                    string tooltip = GetPersonTooltip(person);
                    var pin = AddPin(location, tooltip, Colors.Blue);
                    pin.Opacity = 1;
                    pin.MouseDown += (s, a) =>
                    {                  
                        activePerson = person;
                        UpdateMap();
                        a.Handled = true;
                    };
                }
            }

            foreach (var geocache in geocaches)
            {
                Location location = new Location();
                location.Latitude = geocache.Latitude;
                location.Longitude = geocache.Longitude;                
                string tooltip = GetGeocacheTooltip(geocache);                
                if (activePerson.FirstName != null)
                {
                    if (geocache.Person != null)
                    {
                        if (activePerson.ID == geocache.Person.ID)
                        {
                            var pin = AddPin(location, tooltip, Colors.Black);                          
                            pin.MouseDown += (s, a) =>
                            {
                                a.Handled = true;
                            };
                        }
                        else
                        {
                            var pin = AddPin(location, tooltip, Colors.Red);
                            pin.MouseDown += (s, a) =>
                            {
                                AddFoundGeochacheToDB(geocache);
                                UpdateMap();
                                a.Handled = true;
                            };
                        }
                    }                  
                    else
                    {
                        var pin = AddPin(location, tooltip, Colors.Red);
                        pin.MouseDown += (s, a) =>
                        {
                            AddFoundGeochacheToDB(geocache);
                            UpdateMap();
                            a.Handled = true;
                        };
                    }                   
                }
                else
                {
                    var pin = AddPin(location, tooltip, Colors.Gray);
                }              
            }

            foreach (var fg in foundGeocaches)
            {                             
                if (fg.PersonID == activePerson.ID)
                {
                    Location location = new Location();
                    location.Latitude = fg.Geocache.Latitude;
                    location.Longitude = fg.Geocache.Longitude;
                    string tooltip = GetGeocacheTooltip(fg.Geocache);
                    var pin = AddPin(location, tooltip, Colors.Green);
                    pin.MouseDown += (s, a) =>
                    {
                        RemoveFoundGeocacheFromDb(fg);
                        UpdateMap();
                        a.Handled = true;
                    };
                }
            }
        }

        private void OnMapLeftClick()
        {
            // Handle map click here.
            layer.Children.Clear();
            activePerson = new Person();
            UpdateMap();
        }

        private void OnAddGeocacheClick(object sender, RoutedEventArgs args)
        {
            var dialog = new GeocacheDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            if (activePerson.FirstName != null)
            {

                string contents = dialog.GeocacheContents;
                string message = dialog.GeocacheMessage;
                // Add geocache to map and database here.
                int cacheID = 1;
                foreach (var cache in geocaches)
                {
                    cacheID = cacheID + 1;
                }
                var geocache = new Geocache();
                geocache.ID = cacheID;
                geocache.Latitude = latestClickLocation.Latitude;
                geocache.Longitude = latestClickLocation.Longitude;
                geocache.Contents = contents;
                geocache.Message = message;
                geocache.Person = db.Person.First(p => p.ID == activePerson.ID);


                db.Add(geocache);
                db.SaveChanges();
                OnMapLeftClick();


            }
            else
            {
                MessageBox.Show("Please select a person before adding a geocache.");
            }
            
        }

        private void OnAddPersonClick(object sender, RoutedEventArgs args)
        {
            var dialog = new PersonDialog();
            dialog.Owner = this;
            dialog.ShowDialog();
            if (dialog.DialogResult == false)
            {
                return;
            }

            string firstName = dialog.PersonFirstName;
            string lastName = dialog.PersonLastName;
            string city = dialog.AddressCity;
            string country = dialog.AddressCountry;
            string streetName = dialog.AddressStreetName;
            int streetNumber = dialog.AddressStreetNumber;

            // Add person to map and database here.
            var person = new Person();
            string streetNum = streetNumber.ToString();
            person.FirstName = firstName;
            person.LastName = lastName;
            person.Latitude = latestClickLocation.Latitude;
            person.Longitude = latestClickLocation.Longitude;
            person.Country = country;
            person.City = city;
            person.StreetName = streetName;
            person.StreetNumber = byte.Parse(streetNum);
            db.Add(person);
            db.SaveChanges();
            OnMapLeftClick();
        }

        private Pushpin AddPin(Location location, string tooltip, Color color)
        {
            var pin = new Pushpin();
            pin.Cursor = Cursors.Hand;
            pin.Background = new SolidColorBrush(color);
            ToolTipService.SetToolTip(pin, tooltip);
            ToolTipService.SetInitialShowDelay(pin, 0);
            layer.AddChild(pin, new Location(location.Latitude, location.Longitude));
            return pin;
        }

        private void OnLoadFromFileClick(object sender, RoutedEventArgs args)
        {
            activePerson = new Person();
            ClearDatabase();            
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Read the selected file here.          
            string[] lines = File.ReadAllLines(path).ToArray();

            var foundList = new Dictionary<Person, List<int>>();
            try
            {
                foreach (string line in lines)
                {
                    string[] values = line.Split('|').Select(v => v.Trim()).ToArray();
                    if (!line.StartsWith("Found:") && values.Length == 8)
                    {
                        var person = new Person();
                        person.FirstName = values[0];
                        person.LastName = values[1];
                        person.Country = values[2];
                        person.City = values[3];
                        person.StreetName = values[4];
                        person.StreetNumber = byte.Parse(values[5]);
                        person.Latitude = double.Parse(values[6]);
                        person.Longitude = double.Parse(values[7]);
                        db.Add(person);
                        db.SaveChanges();
                    }
                    else if (!line.StartsWith("Found:") && values.Length == 5)
                    {
                        var person = db.Person.OrderByDescending(p => p.ID).First();
                        var geocache = new Geocache();
                        geocache.Person = person;
                        geocache.ID = int.Parse(values[0]);
                        geocache.Latitude = double.Parse(values[1]);
                        geocache.Longitude = double.Parse(values[2]);
                        geocache.Contents = values[3];
                        geocache.Message = values[4];
                        db.Add(geocache);
                        db.SaveChanges();
                    }
                    else if (line.StartsWith("Found:"))
                    {
                        var person = db.Person.OrderByDescending(p => p.ID).First();
                        string[] found = line.Split(',', ':').Skip(1).Select(v => v.Trim()).ToArray();
                        var intList = new List<int>();
                        foreach (var foundNum in found)
                        {
                            int intAdd;
                            var num = int.TryParse(foundNum, out intAdd);
                            if (num)
                            {
                                intList.Add(intAdd);
                            }
                        }
                        foundList.Add(person, intList);
                    }
                }

                foreach (var pair in foundList)
                {
                    var personValue = pair.Value;
                    foreach (var value in personValue)
                    {
                        var geocache = db.Geocache.First(g => g.ID == value);
                        var foundGeocache = new FoundGeocache();
                        foundGeocache.Person = pair.Key;
                        foundGeocache.Geocache = geocache;
                        db.Add(foundGeocache);
                        db.SaveChanges();
                    }

                }
            }
            catch
            {
                MessageBox.Show("The textfile is not properly formatted");
            }
            

            UpdateMap();
        }

        private void OnSaveToFileClick(object sender, RoutedEventArgs args)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.DefaultExt = ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.FileName = "Geocaches";
            bool? result = dialog.ShowDialog();
            if (result != true)
            {
                return;
            }

            string path = dialog.FileName;
            // Write to the selected file here.
            List<string> dbContent = new List<string>();
          
            LoadDataFromDB();
            foreach (var person in people)
            {
                string found = "";
                string line = $"{person.FirstName} | {person.LastName} | {person.Country} | {person.City} | {person.StreetName} | {person.StreetNumber} | {person.Latitude} | {person.Longitude}";
                dbContent.Add(line);

                foreach (var geocache in geocaches.Where(g => g.Person.ID == person.ID))
                {
                    line = $"{geocache.ID} | {geocache.Latitude} | {geocache.Longitude} | {geocache.Contents} | {geocache.Message}";
                    dbContent.Add(line);
                }
                foreach (var fg in foundGeocaches.Where(fg => fg.PersonID == person.ID))
                {
                    found = found + ", " + (fg.GeocacheID.ToString());
                }               
                line = "Found:" + found.TrimStart(',');
                dbContent.Add(line);
                line = "";
                dbContent.Add(line);
            }
            
            string[] lines = dbContent.ToArray();
            File.WriteAllLines(path, lines);
        }

        private string GetPersonTooltip(Person person)
        {
            string tooltip = $"{person.FirstName} {person.LastName}\n{person.StreetName} {person.StreetNumber}\n{person.City}\n{person.Country}";
            return tooltip;
        }

        private string GetGeocacheTooltip(Geocache geocache)
        {
            string tooltip;
            if (geocache.Person == null)
            {
                tooltip = $"Latitude: {geocache.Latitude} \nLongitude: {geocache.Longitude}\n\n{geocache.Message}\n{geocache.Contents}";
            }
            else
            {
                tooltip = $"Latitude: {geocache.Latitude} \nLongitude: {geocache.Longitude}\n\n{geocache.Message}\n{geocache.Contents}\n{geocache.Person.FirstName} {geocache.Person.LastName}";
            }
            return tooltip;
        }

        private void LoadDataFromDB()
        {
            people = db.Person
                    .Include(p => p.FoundGeocaches)
                    .ToList();
            geocaches = db.Geocache
                .Include(g => g.Person)
                .ThenInclude(g => g.FoundGeocaches)
                .ToList();
            foundGeocaches = db.FoundGeocaches
                .ToList();
        }

        private void AddFoundGeochacheToDB(Geocache geocache)
        {
            var foundGeocache = new FoundGeocache();
            foundGeocache.GeocacheID = geocache.ID;
            foundGeocache.PersonID = activePerson.ID;
            db.FoundGeocaches.Add(foundGeocache);
            db.SaveChanges();
        }
        
        private void RemoveFoundGeocacheFromDb(FoundGeocache foundGeocache)
        {
            db.FoundGeocaches.Remove(foundGeocache);
            db.SaveChanges();
        }

        private static void ClearDatabase()
        {
            db.Person.RemoveRange(db.Person);
            db.Geocache.RemoveRange(db.Geocache);
            db.FoundGeocaches.RemoveRange(db.FoundGeocaches);
            db.SaveChanges();
        }
    }
}
