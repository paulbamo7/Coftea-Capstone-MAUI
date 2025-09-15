using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Coftea_Capstone.ViewModel;

public partial class RetryDialogViewModel : ObservableObject
{
    private readonly Action _retryAction;

    public RetryDialogViewModel(Action retryAction)
    {
        _retryAction = retryAction;
    }

    [RelayCommand]
    private void Retry()
    {
        _retryAction?.Invoke();
    }
}