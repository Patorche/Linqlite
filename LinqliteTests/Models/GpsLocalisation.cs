using Linqlite.Attributes;
using Linqlite.Sqlite;

namespace Linqlite.Models
{
    public class GpsLocalisation : SqliteObservableEntity
    {
        private double? _latitude;
        private double? _longitude;
        private string _city;
        private string _country;

        [ColumnAttribute(ColumnName = "latitude")]
        public double? Latitude
        {
            get => _latitude;
            set
            {
                if (SetProperty(ref _latitude, value)) { }
                    //UpdateMappedProperty(Parent,value, nameof(Latitude));
            }
        }

        [ColumnAttribute(ColumnName = "longitude")]
        public double? Longitude
        {
            get => _longitude;
            set
            {
                if (SetProperty(ref _longitude, value)) { }
                    //UpdateMappedProperty(Parent, value, nameof(Longitude));
            }
        }

        
        [ColumnAttribute(ColumnName = "city")]
        public string City
        {
            get => _city;
            set
            {
                if (SetProperty(ref _city, value)) { }
                  //  UpdateMappedProperty(Parent, value, nameof(City));
            }
        }

        [ColumnAttribute(ColumnName = "country")]
        public string Country
        {
            get => _country;
            set
            {
                if (SetProperty(ref _country, value)) { }
                   // UpdateMappedProperty(Parent, value, nameof(Country));
            }
        }

    }
}
