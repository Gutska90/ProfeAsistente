using ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

namespace ProfeAsistente.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewDiffPage : ContentPage
{
    public CurriculumReviewDiffPage(CurriculumReviewDiffViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewDiffViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
