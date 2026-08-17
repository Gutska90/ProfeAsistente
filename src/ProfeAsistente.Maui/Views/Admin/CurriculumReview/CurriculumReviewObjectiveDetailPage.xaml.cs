using ProfeAsistente.Maui.ViewModels.Admin.CurriculumReview;

namespace ProfeAsistente.Maui.Views.Admin.CurriculumReview;

public partial class CurriculumReviewObjectiveDetailPage : ContentPage
{
    public CurriculumReviewObjectiveDetailPage(CurriculumReviewObjectiveDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CurriculumReviewObjectiveDetailViewModel vm)
        {
            if (vm.IsDirty)
            {
                await DisplayAlert(
                    "Cambios sin guardar",
                    "Esta pantalla tiene cambios sin guardar.",
                    "Entendido");
            }

            if (vm.BatchId != Guid.Empty && !string.IsNullOrWhiteSpace(vm.ObjectiveTemporaryId) && !vm.IsDirty)
                await vm.CargarCommand.ExecuteAsync(null);
        }
    }

    protected override async void OnDisappearing()
    {
        if (BindingContext is CurriculumReviewObjectiveDetailViewModel { IsDirty: true })
        {
            await DisplayAlert(
                "Cambios sin guardar",
                "Hay cambios sin guardar en este OA. Use Guardar si desea conservarlos.",
                "Entendido");
        }

        base.OnDisappearing();
    }
}
