namespace AndreyAkaSkif.TrayCopy.Settings.Persistence;

/// <summary>
/// Определяет, как настройки приложения читаются и сохраняются
/// </summary>
internal interface ISettingsStore
{
    /// <summary>
    /// Читает настройки. Если их ещё нет, возвращает настройки по умолчанию. Если сохранённые
    /// настройки нечитаемы, откладывает их в резервную копию и возвращает настройки
    /// по умолчанию вместе с расположением копии
    /// </summary>
    SettingsLoadResult Load();

    /// <summary>
    /// Сохраняет настройки, защищая значения записей согласно
    /// <see cref="AppSettings.Protection"/>
    /// </summary>
    void Save(AppSettings settings);
}
