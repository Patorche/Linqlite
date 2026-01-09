using Linqlite.Attributes;
using Linqlite.Sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Linqlite.Models
{
    public class GpsLocalisation : SqliteEntity
    {
        private double? _latitude;
        private double? _longitude;
        private string _city = "";
        private string _country = "";

        public Photo? Parent { get; set; }


        [Column("latitude")]
        public double? Latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);

        }

        [Column("longitude")]
        public double? Longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }


        [Column("city")]
        public string City
        {
            get => _city;
            set => SetProperty(ref _city, value); 
        }

        [Column("country")]
        public string Country
        {
            get => _country;
            set => SetProperty(ref _country, value);
        }

    }
}
