using PlantCareMobile.ViewModels;

namespace PlantCareMobile.Views;

public partial class PlantsGalleryPage : ContentPage
{
    private readonly PlantsGalleryViewModel _viewModel;

    // --- CAMBIO IMPORTANTE AQUÍ ---
    // Agregamos 'PlantsGalleryViewModel viewModel' dentro de los paréntesis
    public PlantsGalleryPage(PlantsGalleryViewModel viewModel)
    {
        InitializeComponent();
        
        // 1. Guardamos el ViewModel en la variable privada para usarlo luego
        _viewModel = viewModel;
        
        // 2. Asignamos el BindingContext aquí (y no en el XAML)
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Esto refrescará la lista cada vez que entres a la pantalla
        if (_viewModel != null)
        {
            await _viewModel.LoadPlantsAsync();
        }
    }
}