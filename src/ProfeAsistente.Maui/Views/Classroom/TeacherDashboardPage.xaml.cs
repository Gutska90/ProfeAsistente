namespace ProfeAsistente.Maui.Views.Classroom;

public partial class TeacherDashboardPage : ContentPage
{
    public TeacherDashboardPage(ViewModels.Classroom.TeacherDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
