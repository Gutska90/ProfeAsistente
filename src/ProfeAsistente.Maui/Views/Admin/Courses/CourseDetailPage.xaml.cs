namespace ProfeAsistente.Maui.Views.Admin.Courses;

public partial class CourseDetailPage : ContentPage
{
    public CourseDetailPage(ViewModels.Admin.Courses.CourseDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
