using PlantCareMobile.Models;
using PlantCareMobile.Services;
using PlantCareMobile.ViewModels; // <--- Agrega este using

namespace PlantCareMobile.Views;

public partial class HomePage : ContentPage
{
    #region Properties
    private FileResult? selectedImage;
    private List<PlantResult>? identificationResults;
    private readonly PlantIdentificationService plantService;
    private readonly HomeViewModel _viewModel; // <--- Nueva variable para el ViewModel
    #endregion

    #region Constructor
    // Modificamos el constructor para recibir el ViewModel (Inyección)
    public HomePage(HomeViewModel viewModel) 
    {
        InitializeComponent();
        
        _viewModel = viewModel;
        BindingContext = _viewModel; // <--- Conectamos los datos

        plantService = new PlantIdentificationService();
    }
    #endregion

    #region Event Handlers
    private async void OnUploadAreaTapped(object sender, EventArgs e)
    {
        try
        {
            var action = await DisplayActionSheet(
                "Seleccionar imagen",
                "Cancelar",
                null,
                "📷 Tomar foto",
                "📁 Seleccionar de galería");

            switch (action)
            {
                case "📷 Tomar foto":
                    await OnTakePhotoClicked();
                    break;
                case "📁 Seleccionar de galería":
                    await OnPickPhotoClicked();
                    break;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error: {ex.Message}", "OK");
        }
    }

    private async void HelpUploadAreaTapped(object sender, EventArgs e)
    {
        await DisplayAlert("HELP", "VEN Y SANA MI DOLOOOOOOOOR, TU TIENES LA CURA DE ESTE AMOOO-OOOHR", "Pero que buena rola");
    }

    // 🔥 NUEVO: Manejador para "Ver todos"
    private async void OnVerTodosTapped(object sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync("//PlantsGalleryPage");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al navegar: {ex.Message}", "OK");
        }
    }
    #endregion

    #region Photo Methods
    private async Task OnTakePhotoClicked()
    {
        try
        {
            var photo = await MediaPicker.CapturePhotoAsync();
            if (photo != null)
            {
                selectedImage = photo;
                await IdentifyPlant();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al tomar foto: {ex.Message}", "OK");
        }
    }

    private async Task OnPickPhotoClicked()
    {
        try
        {
            var photo = await MediaPicker.PickPhotoAsync();
            if (photo != null)
            {
                selectedImage = photo;
                await IdentifyPlant();
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al seleccionar foto: {ex.Message}", "OK");
        }
    }
    #endregion

    #region Plant Identification
    private async Task IdentifyPlant()
    {
        if (selectedImage == null) return;

        try
        {
            // Mostrar loading indicator
            SetLoadingVisibility(true);

            // Llamar al servicio de identificación
            var result = await plantService.IdentifyPlantAsync(selectedImage);

            // Procesar resultados
            await ProcessIdentificationResults(result);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Error al identificar planta: {ex.Message}", "OK");
        }
        finally
        {
            // Ocultar loading indicator
            SetLoadingVisibility(false);
        }
    }

    private async Task ProcessIdentificationResults(PlantNetResponse? result)
    {
        if (result?.Results == null || result.Results.Count == 0)
        {
            await DisplayAlert("Sin resultados",
                "No se encontraron resultados. Intenta con otra imagen más clara.", "OK");
        }
        else
        {
            identificationResults = result.Results;
            await ShowResults();
        }
    }

    private async Task ShowResults()
    {
        if (identificationResults != null && identificationResults.Count > 0)
        {
            var mainResult = identificationResults.First();
            var message = BuildResultMessage(mainResult);

            var saveResult = await DisplayAlert("¡Planta Identificada!", message, "💾 Guardar", "Cerrar");

            if (saveResult)
            {
                await SavePlant(mainResult);
            }
        }
    }

    private string BuildResultMessage(PlantResult mainResult)
    {
        var commonNames = mainResult.Species?.CommonNames != null && mainResult.Species.CommonNames.Count > 0
            ? string.Join(", ", mainResult.Species.CommonNames)
            : "N/A";

        var message = $"🌱 {mainResult.Species?.ScientificNameWithoutAuthor ?? "Desconocido"}\n\n" +
                     $"Nombres comunes: {commonNames}\n\n" +
                     $"Puntuación: {(mainResult.Score * 100):F2}%";

        // Agregar otras coincidencias si las hay
        if (identificationResults != null && identificationResults.Count > 1)
        {
            message += "\n\n🌿 Otras posibles coincidencias:";
            for (int i = 1; i < Math.Min(4, identificationResults.Count); i++)
            {
                var otherResult = identificationResults[i];
                message += $"\n• {otherResult.Species?.ScientificNameWithoutAuthor} ({(otherResult.Score * 100):F2}%)";
            }
        }

        return message;
    }

   private async Task SavePlant(PlantResult plant)
    {
        if (selectedImage == null || plant?.Species == null) return;

        try
        {
            // 1. Pedir Ubicación (Como ya tenías)
            string location = await DisplayPromptAsync("Ubicación",
                "¿Dónde está ubicada esta planta?",
                "Siguiente", 
                "Cancelar",
                "Ej: Jardín, Sala, Balcón",
                maxLength: 100);

            if (location == null) return; // Si cancela, no guarda nada

            // 2. NUEVO: Pedir Apodo (Esto es lo que te faltaba)
            string nickname = await DisplayPromptAsync("Ponle un nombre",
                $"El nombre científico es {plant.Species.ScientificNameWithoutAuthor}.\n¿Quieres ponerle un apodo?",
                "Guardar",
                "Usar nombre científico",
                "Ej: Sr. Girasol",
                maxLength: 50);

            // Guardar imagen en almacenamiento local
            var imagePath = await SaveImageToLocalStorage(selectedImage);

            // Crear objeto SavedPlant con el Nickname
            var savedPlant = new SavedPlant
            {
                ScientificName = plant.Species.ScientificNameWithoutAuthor ?? "Desconocido",
                CommonNames = plant.Species.CommonNames != null && plant.Species.CommonNames.Count > 0
                    ? string.Join(", ", plant.Species.CommonNames)
                    : "N/A",
                Location = location,
                Nickname = nickname ?? "", // <--- Aquí guardamos el apodo
                ImagePath = imagePath,
                Score = plant.Score,
                DateAdded = DateTime.Now
            };

            // Guardar en base de datos
            // Nota: Aquí creamos una instancia nueva del servicio si no lo inyectaste, para asegurar que funcione rápido.
            var dbService = new PlantDatabaseService(); 
            await dbService.SavePlantAsync(savedPlant);

            // Notificar a toda la app que hay planta nueva
            PlantMessenger.Send("PlantSaved", savedPlant);

            // Mensaje de éxito usando el nombre bonito
            await DisplayAlert("✅ Éxito",
                $"¡Bienvenido/a {savedPlant.DisplayName} a tu jardín!",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo guardar la planta: {ex.Message}", "OK");
        }
    }

    private async Task<string> SaveImageToLocalStorage(FileResult photo)
    {
        var fileName = $"{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        using var sourceStream = await photo.OpenReadAsync();
        using var fileStream = File.OpenWrite(filePath);
        await sourceStream.CopyToAsync(fileStream);

        return filePath;
    }
    #endregion

    #region UI Helper Methods
    private void SetLoadingVisibility(bool isVisible)
    {
        var loadingIndicator = this.FindByName<StackLayout>("LoadingIndicator");
        if (loadingIndicator != null)
        {
            loadingIndicator.IsVisible = isVisible;
        }
    }
    #endregion
}