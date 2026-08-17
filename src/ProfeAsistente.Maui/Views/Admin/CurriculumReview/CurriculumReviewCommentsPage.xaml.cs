using ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

namespace ProfeAsistente.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewCommentsPage : ContentPage
{
    public CurriculumReviewCommentsPage(CurriculumReviewCommentsViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewCommentsViewModel vm)
            await vm.CargarCommand.ExecuteAsync(null);
    }
}
