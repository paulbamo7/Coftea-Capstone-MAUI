using System.Threading.Tasks;

namespace Coftea_Capstone.Views.Pages;

public partial class OrderCompleteDialogPage : ContentPage
{
    private TaskCompletionSource<bool>? _tcs;

    public OrderCompleteDialogPage()
    {
        InitializeComponent();
        BindingContext = new DialogVM(this);
    }

    public Task<bool> ShowAsync()
    {
        _tcs = new TaskCompletionSource<bool>();
        return _tcs.Task;
    }

    private void Resolve(bool result)
    {
        _tcs?.TrySetResult(result);
    }

    private class DialogVM
    {
        private readonly OrderCompleteDialogPage _page;
        public DialogVM(OrderCompleteDialogPage page)
        {
            _page = page;
        }

        public Command ConfirmCommand => new Command(async () =>
        {
            _page.Resolve(true);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        });

        public Command CloseCommand => new Command(async () =>
        {
            _page.Resolve(false);
            await Application.Current.MainPage.Navigation.PopModalAsync();
        });
    }
}


