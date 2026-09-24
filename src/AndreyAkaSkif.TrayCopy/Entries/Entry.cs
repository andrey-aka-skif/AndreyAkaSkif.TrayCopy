using System.Text;

namespace AndreyAkaSkif.TrayCopy.Entries;

/// <summary>
/// Представляет запись: строку для копирования и имя, под которым она показывается
/// </summary>
/// <param name="Name">Имя записи</param>
/// <param name="Value">Строка, которая копируется в буфер обмена</param>
internal sealed record Entry(string Name, string Value)
{
    // Значение — секрет (токен), поэтому в ToString, а с ним в логи, сообщения об ошибках
    // и окно отладчика попадает только имя
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("Name = ").Append(Name);
        return true;
    }
}
