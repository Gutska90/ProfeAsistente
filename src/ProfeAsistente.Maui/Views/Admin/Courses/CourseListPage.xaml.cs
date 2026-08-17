namespace ProfeAsistente.Maui.Views.Admin.Courses;

public partial class CourseListPage : ContentPage
{
    public CourseListPage(ViewModels.Admin.Courses.CourseListViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
        Loaded += async (_, _) => await vm.LoadCommand.ExecuteAsync(null);
    }
}
