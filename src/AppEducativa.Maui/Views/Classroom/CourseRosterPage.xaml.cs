namespace AppEducativa.Maui.Views.Classroom;

public partial class CourseRosterPage : ContentPage
{
    public CourseRosterPage(ViewModels.Classroom.CourseRosterViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
