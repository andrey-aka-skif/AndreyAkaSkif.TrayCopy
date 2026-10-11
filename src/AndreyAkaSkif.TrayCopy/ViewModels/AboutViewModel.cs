using AndreyAkaSkif.TrayCopy.Updates;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AndreyAkaSkif.TrayCopy.ViewModels;

/// <summary>
/// Представляет окно «О программе»: проверку обновлений и её результат
/// </summary>
/// <param name="updateChecker">Проверяет, вышла ли версия новее работающей</param>
internal sealed partial class AboutViewModel(UpdateChecker updateChecker) : ViewModelBase
{
    /// <summary>
    /// Состояние проверки обновлений; до первой проверки — <see cref="UpdateStatus.Checking"/>
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChecking))]
    [NotifyPropertyChangedFor(nameof(IsUpToDate))]
    [NotifyPropertyChangedFor(nameof(IsUpdateAvailable))]
    [NotifyPropertyChangedFor(nameof(IsFailed))]
    [NotifyPropertyChangedFor(nameof(CanCheckAgain))]
    public partial UpdateStatus Status { get; private set; }

    /// <summary>
    /// Найденный выпуск новее работающей версии; <see langword="null"/>, если его нет
    /// </summary>
    [ObservableProperty]
    public partial ReleaseInfo? Update { get; private set; }

    /// <summary>
    /// Признак идущей проверки
    /// </summary>
    public bool IsChecking => Status == UpdateStatus.Checking;

    /// <summary>
    /// Признак того, что установлена последняя версия
    /// </summary>
    public bool IsUpToDate => Status == UpdateStatus.UpToDate;

    /// <summary>
    /// Признак того, что вышла версия новее установленной
    /// </summary>
    public bool IsUpdateAvailable => Status == UpdateStatus.UpdateAvailable;

    /// <summary>
    /// Признак того, что проверка не удалась
    /// </summary>
    public bool IsFailed => Status == UpdateStatus.Failed;

    /// <summary>
    /// Признак того, что проверку предлагается повторить: новой версии не нашлось или
    /// проверка не удалась
    /// </summary>
    public bool CanCheckAgain => Status is UpdateStatus.UpToDate or UpdateStatus.Failed;

    // Отмена — это закрытие окна: результат показывать некому. Исключения из команды не
    // выходят: AsyncRelayCommand передал бы их в цикл интерфейса, и приложение упало бы
    [RelayCommand]
    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        Status = UpdateStatus.Checking;
        Update = null;

        try
        {
            Update = await updateChecker.FindUpdateAsync(cancellationToken);
            Status = Update is null ? UpdateStatus.UpToDate : UpdateStatus.UpdateAvailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            // Причины сбоя (нет сети, лимит запросов, ответ не того вида) для пользователя
            // равнозначны: проверку можно повторить
            Status = UpdateStatus.Failed;
        }
    }
}
