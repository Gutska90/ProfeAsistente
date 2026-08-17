using AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

namespace AppEducativa.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewChangesPage : ContentPage
{
    public CurriculumReviewChangesPage(CurriculumReviewChangesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewChangesViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
