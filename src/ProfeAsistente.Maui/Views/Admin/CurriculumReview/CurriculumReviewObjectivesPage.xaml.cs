using ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

namespace ProfeAsistente.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewObjectivesPage : ContentPage
{
    public CurriculumReviewObjectivesPage(CurriculumReviewObjectivesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewObjectivesViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
