using PlantCareMobile.Views;

namespace PlantCareMobile
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
        }

        private async void logoutButton_Clicked(object sender, EventArgs e)
        {
            //LOGICA PARA CERRAR SESION

            //VOLVER a pagina de bienvenida
            await Shell.Current.GoToAsync("//WelcomePage");
        }
    }
}
