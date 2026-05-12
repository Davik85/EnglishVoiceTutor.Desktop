using EnglishVoiceTutor.Desktop.Models;

namespace EnglishVoiceTutor.Desktop.Localization;

public static class DiagnosticsLocalization
{
    private static readonly IReadOnlyDictionary<string, DiagnosticsLocalizedText> TextByLanguageId = new Dictionary<string, DiagnosticsLocalizedText>(StringComparer.OrdinalIgnoreCase)
    {
        [InterfaceLanguageOptions.EnglishId] = new("Diagnostics", "Technical information for troubleshooting.", "App version", "Backend URL", "Backend status", "AI status", "Settings file", "Lesson history file", "Interface language", "Native language", "Tutor avatar", "Refresh diagnostics", "Copy diagnostics", "Diagnostics copied.", "Could not copy diagnostics.", "connected", "unavailable", "checking...", "configured", "not configured", "unknown"),
        [InterfaceLanguageOptions.RussianId] = new("Диагностика", "Техническая информация для проверки и устранения проблем.", "Версия приложения", "URL бэкенда", "Статус бэкенда", "Статус AI", "Файл настроек", "Файл истории уроков", "Язык интерфейса", "Родной язык", "Аватар наставника", "Обновить диагностику", "Скопировать диагностику", "Диагностика скопирована.", "Не удалось скопировать диагностику.", "подключено", "недоступно", "проверка...", "настроен", "не настроен", "неизвестно"),
        [InterfaceLanguageOptions.SpanishId] = new("Diagnóstico", "Información técnica para solucionar problemas.", "Versión de la app", "URL del backend", "Estado del backend", "Estado de la IA", "Archivo de ajustes", "Archivo del historial de clases", "Idioma de la interfaz", "Idioma nativo", "Avatar del tutor", "Actualizar diagnóstico", "Copiar diagnóstico", "Diagnóstico copiado.", "No se pudo copiar el diagnóstico.", "conectado", "no disponible", "comprobando...", "configurada", "no configurada", "desconocido"),
        [InterfaceLanguageOptions.GermanId] = new("Diagnose", "Technische Informationen zur Fehlerbehebung.", "App-Version", "Backend-URL", "Backend-Status", "KI-Status", "Einstellungsdatei", "Datei des Lektionsverlaufs", "Sprache der Benutzeroberfläche", "Muttersprache", "Tutor-Avatar", "Diagnose aktualisieren", "Diagnose kopieren", "Diagnose kopiert.", "Diagnose konnte nicht kopiert werden.", "verbunden", "nicht verfügbar", "wird geprüft...", "konfiguriert", "nicht konfiguriert", "unbekannt"),
        [InterfaceLanguageOptions.FrenchId] = new("Diagnostic", "Informations techniques pour le dépannage.", "Version de l’application", "URL du backend", "État du backend", "État de l’IA", "Fichier des paramètres", "Fichier de l’historique des leçons", "Langue de l’interface", "Langue maternelle", "Avatar du tuteur", "Actualiser le diagnostic", "Copier le diagnostic", "Diagnostic copié.", "Impossible de copier le diagnostic.", "connecté", "indisponible", "vérification...", "configurée", "non configurée", "inconnu"),
        [InterfaceLanguageOptions.ItalianId] = new("Diagnostica", "Informazioni tecniche per la risoluzione dei problemi.", "Versione dell’app", "URL del backend", "Stato del backend", "Stato dell’IA", "File delle impostazioni", "File della cronologia delle lezioni", "Lingua dell’interfaccia", "Lingua madre", "Avatar del tutor", "Aggiorna diagnostica", "Copia diagnostica", "Diagnostica copiata.", "Impossibile copiare la diagnostica.", "connesso", "non disponibile", "controllo...", "configurata", "non configurata", "sconosciuto"),
        [InterfaceLanguageOptions.PortugueseId] = new("Diagnóstico", "Informações técnicas para solução de problemas.", "Versão do aplicativo", "URL do backend", "Status do backend", "Status da IA", "Arquivo de configurações", "Arquivo do histórico de aulas", "Idioma da interface", "Idioma nativo", "Avatar do tutor", "Atualizar diagnóstico", "Copiar diagnóstico", "Diagnóstico copiado.", "Não foi possível copiar o diagnóstico.", "conectado", "indisponível", "verificando...", "configurada", "não configurada", "desconhecido")
    };

    public static DiagnosticsLocalizedText GetText(string? languageId)
    {
        if (!string.IsNullOrWhiteSpace(languageId) && TextByLanguageId.TryGetValue(languageId.Trim(), out var text))
        {
            return text;
        }

        return TextByLanguageId[InterfaceLanguageOptions.EnglishId];
    }
}
