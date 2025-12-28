using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Linqlite.Sqlite
{

    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }


    }


}
