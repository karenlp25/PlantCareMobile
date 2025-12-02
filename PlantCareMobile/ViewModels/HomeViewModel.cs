using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PlantCareMobile.Models;
using PlantCareMobile.Services;

namespace PlantCareMobile.ViewModels
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly PlantDatabaseService _databaseService;
        private ObservableCollection<SavedPlant> _recentPlants;
        private bool _hasPlants;

        public ObservableCollection<SavedPlant> RecentPlants
        {
            get => _recentPlants;
            set { _recentPlants = value; OnPropertyChanged(); }
        }

        public bool HasPlants
        {
            get => _hasPlants;
            set { _hasPlants = value; OnPropertyChanged(); }
        }

        public HomeViewModel(PlantDatabaseService databaseService)
        {
            _databaseService = databaseService;
            _recentPlants = new ObservableCollection<SavedPlant>();

            // Escuchar si se guarda una nueva planta (desde la cámara) para actualizar la lista al momento
            PlantMessenger.Subscribe("PlantSaved", async (args) => await LoadRecentPlantsAsync());
            
            // Carga inicial
            Task.Run(async () => await LoadRecentPlantsAsync());
        }

        public async Task LoadRecentPlantsAsync()
        {
            try
            {
                var plants = await _databaseService.GetPlantsAsync();
                
                // Ordenamos por fecha descendente y tomamos solo las 3 primeras
                var recent = plants.OrderByDescending(p => p.DateAdded).Take(3).ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RecentPlants = new ObservableCollection<SavedPlant>(recent);
                    HasPlants = RecentPlants.Count > 0;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando home: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}