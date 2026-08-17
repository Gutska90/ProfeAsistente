using ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

namespace ProfeAsistente.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewUnitsPage : ContentPage
{
    public CurriculumReviewUnitsPage(CurriculumReviewUnitsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewUnitsViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
