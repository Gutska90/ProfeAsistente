namespace ProfeAsistente.Maui.Views.Admin.Institutions;

public partial class InstitutionListPage : ContentPage
{
    public InstitutionListPage(ViewModels.Admin.Institutions.InstitutionListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
