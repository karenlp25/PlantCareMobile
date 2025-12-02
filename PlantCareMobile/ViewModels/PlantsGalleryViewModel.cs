using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PlantCareMobile.Models;
using PlantCareMobile.Services;

namespace PlantCareMobile.ViewModels;

public class PlantsGalleryViewModel : INotifyPropertyChanged
{
    // Ahora recibimos los servicios por inyección, no los creamos con 'new'
    private readonly PlantDatabaseService _databaseService;
    private readonly ApiService _apiService;

    private ObservableCollection<SavedPlant> _plants;
    private bool _isLoading;
    private bool _hasPlants;

    public ObservableCollection<SavedPlant> Plants
    {
        get => _plants;
        set
        {
            _plants = value;
            OnPropertyChanged();
            HasPlants = _plants?.Count > 0;
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public bool HasPlants
    {
        get => _hasPlants;
        set
        {
            _hasPlants = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoadPlantsCommand { get; }
    public ICommand EditNameCommand { get; }
    public ICommand AddSensorCommand { get; }
    public ICommand RenameSensorCommand { get; }
    public ICommand DeletePlantCommand { get; }
    
    // Comando para actualizar solo los datos del sensor manualmente
    public ICommand RefreshSensorsCommand { get; }

    // Constructor actualizado para recibir ApiService y DatabaseService
    public PlantsGalleryViewModel(PlantDatabaseService databaseService, ApiService apiService)
    {
        _databaseService = databaseService;
        _apiService = apiService;
        
        _plants = new ObservableCollection<SavedPlant>();
        
        LoadPlantsCommand = new Command(async () => await LoadPlantsAsync());
        AddSensorCommand = new Command<SavedPlant>(async (plant) => await AddSensorAsync(plant));
        DeletePlantCommand = new Command<SavedPlant>(async (plant) => await DeletePlantAsync(plant));
        RefreshSensorsCommand = new Command(async () => await UpdateSensorDataAsync());
        RenameSensorCommand = new Command<SavedPlant>(async (plant) => await RenameSensorAsync(plant));
        EditNameCommand = new Command<SavedPlant>(async (plant) => await EditNameAsync(plant));
        

        // Suscribirse al evento de planta guardada
        PlantMessenger.Subscribe("PlantSaved", async (args) => await LoadPlantsAsync());
        
        // Cargar datos iniciales
        Task.Run(async () => await LoadPlantsAsync()); 
    }

    public async Task LoadPlantsAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            // 1. Cargar plantas de la BD local
            var plants = await _databaseService.GetPlantsAsync();
            Plants = new ObservableCollection<SavedPlant>(plants);
            
            // 2. Intentar obtener datos en vivo de la API
            await UpdateSensorDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading plants: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task EditNameAsync(SavedPlant plant)
    {
        if (plant == null) return;

        // 1. Mostrar cuadro para escribir el nuevo nombre
        string newName = await Shell.Current.DisplayPromptAsync(
            "Renombrar Planta",
            $"Nombre actual: {plant.DisplayName}",
            initialValue: plant.Nickname,
            placeholder: "Ej: Sr. Girasol");

        // 2. Si el usuario escribió algo y aceptó
        if (newName != null)
        {
            // Actualizamos el Nickname
            plant.Nickname = newName;

            // Avisamos a la UI que el "DisplayName" cambió (para que se refresque el texto)
            // Nota: Al modificar una propiedad [ObservableProperty] o usar INotifyPropertyChanged correctamente, esto es automático.
            // Pero como DisplayName es calculada, forzamos la actualización guardando en BD.

            await _databaseService.SavePlantAsync(plant);

            // Recargamos la lista para ver el cambio reflejado
            await LoadPlantsAsync();
        }
    }

    private async Task UpdateSensorDataAsync()
    {
        if (Plants == null || Plants.Count == 0) return;

        foreach (var plant in Plants)
        {
            // Solo consultamos si la planta tiene un SensorId asignado
            if (!string.IsNullOrWhiteSpace(plant.SensorId))
            {
                var latestLog = await _apiService.GetLatestLogAsync(plant.SensorId);
                if (latestLog != null)
                {
                    // Actualizamos la UI (Gracias al INotifyPropertyChanged en el modelo, esto se verá automático)
                    plant.LiveTemp = $"{latestLog.Temp:F1}°C";
                    plant.LiveHumidity = $"{latestLog.MoistureAir:F0}%";
                }
            }
        }
    }

    private async Task DeletePlantAsync(SavedPlant plant)
    {
        if (plant == null) return;

        try
        {
            await _databaseService.DeletePlantAsync(plant);
            Plants.Remove(plant);
            
            if (!string.IsNullOrEmpty(plant.ImagePath) && File.Exists(plant.ImagePath))
            {
                File.Delete(plant.ImagePath);
            }
            HasPlants = Plants.Count > 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error deleting plant: {ex.Message}");
        }
    }
    private async Task AddSensorAsync(SavedPlant plant)
    {
        if (plant == null) return;
        IsLoading = true;
        try
        {
            var myDevices = await _apiService.GetUserDevicesAsync();

            if (myDevices == null || myDevices.Count == 0)
            {
                await Shell.Current.DisplayAlert("Sin sensores", "No se encontraron dispositivos.", "OK");
                return;
            }

            // --- TRUCO DE MAGIA: DICCIONARIO ---
            // Creamos un diccionario para recordar cuál nombre corresponde a qué código
            var deviceOptions = new Dictionary<string, string>();

            foreach (var udid in myDevices)
            {
                // Buscamos si tiene apodo guardado
                string nickname = Preferences.Get($"Nick_{udid}", null);
                
                // Si tiene apodo, mostramos: "Sensor Sala (ESP-e4...)"
                // Si no, mostramos solo: "ESP-e4..."
                string displayText = string.IsNullOrEmpty(nickname) ? udid : $"{nickname} ({udid})";
                
                deviceOptions.Add(displayText, udid);
            }

            // Mostramos las opciones "bonitas"
            string selectedDisplay = await Shell.Current.DisplayActionSheet(
                "Selecciona tu Sensor", "Cancelar", null, deviceOptions.Keys.ToArray());

            if (!string.IsNullOrEmpty(selectedDisplay) && selectedDisplay != "Cancelar")
            {
                // Recuperamos el código real usando el diccionario
                string realUdid = deviceOptions[selectedDisplay];
                
                plant.SensorId = realUdid;
                await _databaseService.SavePlantAsync(plant);
                await UpdateSensorDataAsync();
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Ocurrió un error: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RenameSensorAsync(SavedPlant plant)
    {
        if (plant == null || string.IsNullOrEmpty(plant.SensorId))
        {
            await Shell.Current.DisplayAlert("Error", "Primero debes vincular un sensor a esta planta.", "OK");
            return;
        }

        // 1. Preguntar el nuevo nombre
        string currentAlias = Preferences.Get($"Nick_{plant.SensorId}", "");
        string newName = await Shell.Current.DisplayPromptAsync(
            "Renombrar Sensor", 
            $"ID: {plant.SensorId}\nEscribe un nombre para identificarlo:",
            initialValue: currentAlias,
            placeholder: "Ej: Sensor Cocina");

        // 2. Guardar en la "libreta" de la app
        if (!string.IsNullOrWhiteSpace(newName))
        {
            // Guardamos con una clave única: "Nick_" + el código del sensor
            Preferences.Set($"Nick_{plant.SensorId}", newName);
            await Shell.Current.DisplayAlert("Listo", "Nombre actualizado. La próxima vez que busques dispositivos, aparecerá con este nombre.", "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
}