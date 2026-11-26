using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared._DEN.CCVar;
using Content.Shared._DEN.Fax;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server._DEN.Fax;

public sealed partial class FaxLoggingSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _log = default!;

    private ISawmill _sawmill = default!;
    private static readonly HttpClient _httpClient = new();
    private string? _webhookUrl;

    private const int DiscordMaxDescription = 4000;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = _log.GetSawmill("faxlogging");

        SubscribeLocalEvent<FaxSentEvent>(OnFaxSent);

        _cfg.OnValueChanged(DCCVars.DiscordFaxWebhook, url => _webhookUrl = url, true);
    }

    private void OnFaxSent(FaxSentEvent msg)
    {
        if (string.IsNullOrWhiteSpace(msg.Content))
            return;

        _ = SendFaxToDiscord(msg);
    }

    private async Task SendFaxToDiscord(FaxSentEvent msg)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
            return;

        var content = MarkupToDiscord(msg.Content);

        if (content.Length > DiscordMaxDescription)
            content = content[..DiscordMaxDescription] + "... (truncated)";

        var stamps = msg.StampedBy.Count > 0 ? string.Join(", ", msg.StampedBy) : "None";

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = $"📠 Исходящий факс от {msg.DestinationAddress}",
                    description = content,
                    color = 3447003,
                    footer = new
                    {
                        text = $"Печати: {stamps}"
                    }
                }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_webhookUrl, httpContent);

            if (!response.IsSuccessStatusCode)
                _sawmill.Error($"Discord webhook returned error: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Error sending fax to discord webhook: {ex}");
        }
    }

    /// <summary>
    /// Переводит разметку бумаги в markdown Discord: оформление текста сохраняется, остальные теги отбрасываются.
    /// </summary>
    private static string MarkupToDiscord(string markup)
    {
        var message = FormattedMessage.FromMarkupPermissive(markup);
        var stack = new Stack<(string Name, StringBuilder Text)>();
        var current = new StringBuilder();
        var monoDepth = 0;

        foreach (var node in message.Nodes)
        {
            if (node.IsPlainText)
            {
                var text = node.Value.StringValue ?? string.Empty;
                // Внутри code span Discord не понимает экранирование, обратная кавычка разорвала бы span.
                current.Append(monoDepth > 0 ? text.Replace('`', '\'') : EscapeDiscord(text));
                continue;
            }

            switch (node.Name)
            {
                case "bold":
                case "italic":
                case "bolditalic":
                case "mono":
                    if (!node.Closing)
                    {
                        if (node.Name == "mono")
                            monoDepth++;

                        stack.Push((node.Name, current));
                        current = new StringBuilder();
                    }
                    else if (stack.TryPeek(out var top) && top.Name == node.Name)
                    {
                        if (node.Name == "mono")
                            monoDepth--;

                        stack.Pop();
                        AppendWrapped(top.Text, current.ToString(), GetMarker(top.Name));
                        current = top.Text;
                    }
                    break;
                case "head" when !node.Closing:
                    current.Append(node.Value.LongValue switch
                    {
                        1 => "# ",
                        2 => "## ",
                        3 => "### ",
                        _ => "-# "
                    });
                    break;
                case "bullet" when !node.Closing:
                    current.Append("- ");
                    break;
            }
        }

        // Незакрытые теги закрываем в конце текста.
        while (stack.Count > 0)
        {
            var (name, parent) = stack.Pop();
            AppendWrapped(parent, current.ToString(), GetMarker(name));
            current = parent;
        }

        return current.ToString().Trim();
    }

    private static string GetMarker(string name) => name switch
    {
        "bold" => "**",
        "italic" => "*",
        "bolditalic" => "***",
        _ => "`"
    };

    /// <summary>
    /// Discord не распознаёт маркеры, прилегающие к пробелам изнутри, поэтому пробелы выносятся наружу.
    /// </summary>
    private static void AppendWrapped(StringBuilder sb, string inner, string marker)
    {
        var trimmed = inner.Trim();
        if (trimmed.Length == 0)
        {
            sb.Append(inner);
            return;
        }

        var start = inner.IndexOf(trimmed, StringComparison.Ordinal);
        sb.Append(inner, 0, start)
            .Append(marker)
            .Append(trimmed)
            .Append(marker)
            .Append(inner, start + trimmed.Length, inner.Length - start - trimmed.Length);
    }

    /// <summary>
    /// Экранирует markdown Discord в тексте игрока, в том числе ссылки, цитаты, заголовки и списки.
    /// </summary>
    private static string EscapeDiscord(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is '\\' or '*' or '_' or '~' or '`' or '|' or '[' or ']' or '>' or '#' or '-')
                sb.Append('\\');

            sb.Append(c);
        }

        return sb.ToString();
    }
}
