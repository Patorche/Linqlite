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
        private string _city;
        private string _country;

        public Photo Parent { get; set; }


        [Column(ColumnName = "latitude")]
        public double? Latitude
        {
            get => _latitude;
            set
            {
                if (SetProperty(ref _latitude, value) && Parent != null) ;
                // UpdateMappedProperty(Parent,value, nameof(Latitude));
            }
        }

        [Column(ColumnName = "longitude")]
        public double? Longitude
        {
            get => _longitude;
            set
            {
                if (SetProperty(ref _longitude, value) && Parent != null) ;
                //  UpdateMappedProperty(Parent, value, nameof(Longitude));
            }
        }


        [Column(ColumnName = "city")]
        public string City
        {
            get => _city;
            set
            {
                if (SetProperty(ref _city, value) && Parent != null) ;
                //UpdateMappedProperty(Parent, value, nameof(City));
            }
        }

        [Column(ColumnName = "country")]
        public string Country
        {
            get => _country;
            set
            {
                if (SetProperty(ref _country, value) && Parent != null) ;
                //UpdateMappedProperty(Parent, value, nameof(Country));
            }
        }

    }
}
