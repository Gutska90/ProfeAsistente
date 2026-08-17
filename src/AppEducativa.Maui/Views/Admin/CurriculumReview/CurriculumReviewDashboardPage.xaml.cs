using AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

namespace AppEducativa.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewDashboardPage : ContentPage
{
    public CurriculumReviewDashboardPage(CurriculumReviewDashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewDashboardViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
