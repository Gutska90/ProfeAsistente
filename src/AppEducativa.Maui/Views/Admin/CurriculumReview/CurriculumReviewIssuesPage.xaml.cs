using AppEducativa.Maui.ViewModels.Admin.CurriculumReview;

namespace AppEducativa.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewIssuesPage : ContentPage
{
    public CurriculumReviewIssuesPage(CurriculumReviewIssuesViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewIssuesViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
