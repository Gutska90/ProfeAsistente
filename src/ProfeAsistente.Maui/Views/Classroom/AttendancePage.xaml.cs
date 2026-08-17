namespace ProfeAsistente.Maui.Views.Classroom;

public partial class AttendancePage : ContentPage
{
    public AttendancePage(ViewModels.Classroom.AttendanceViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
