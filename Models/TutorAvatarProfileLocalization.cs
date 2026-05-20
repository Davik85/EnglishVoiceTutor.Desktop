namespace EnglishVoiceTutor.Desktop.Models;

public static class TutorAvatarProfileLocalization
{
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyDictionary<string, TutorAvatarLocalizedProfileText>>> ProfileTextByAvatarId = new(() =>
        new Dictionary<string, IReadOnlyDictionary<string, TutorAvatarLocalizedProfileText>>(StringComparer.OrdinalIgnoreCase)
        {
            [TutorAvatarOptions.DefaultAvatarId] = new Dictionary<string, TutorAvatarLocalizedProfileText>(StringComparer.OrdinalIgnoreCase)
            {
                [InterfaceLanguageOptions.EnglishId] = new(
                    ShortDescription: "22, London, fashion design student; likes padel and art.",
                    AgeText: "22",
                    Location: "London",
                    Role: "Fashion design student",
                    InterestsText: "Padel, art",
                    PersonalityText: "Friendly, warm, supportive, natural",
                    SpeakingStyleText: "Short, clear, encouraging, conversational"),
                [InterfaceLanguageOptions.RussianId] = new(
                    ShortDescription: "22 года, Лондон, студентка факультета дизайна одежды; любит падел и искусство.",
                    AgeText: "22",
                    Location: "Лондон",
                    Role: "Студентка факультета дизайна одежды",
                    InterestsText: "Падел, искусство",
                    PersonalityText: "Дружелюбная, тёплая, поддерживающая, естественная",
                    SpeakingStyleText: "Коротко, понятно, поддерживающе, в разговорном стиле"),
                [InterfaceLanguageOptions.SpanishId] = new(
                    ShortDescription: "22 años, Londres, estudiante de diseño de moda; le gustan el pádel y el arte.",
                    AgeText: "22",
                    Location: "Londres",
                    Role: "Estudiante de diseño de moda",
                    InterestsText: "Pádel, arte",
                    PersonalityText: "Amable, cálida, comprensiva, natural",
                    SpeakingStyleText: "Breve, claro, alentador, conversacional"),
                [InterfaceLanguageOptions.GermanId] = new(
                    ShortDescription: "22, London, Modedesign-Studentin; mag Padel und Kunst.",
                    AgeText: "22",
                    Location: "London",
                    Role: "Modedesign-Studentin",
                    InterestsText: "Padel, Kunst",
                    PersonalityText: "Freundlich, herzlich, unterstützend, natürlich",
                    SpeakingStyleText: "Kurz, klar, ermutigend, gesprächig"),
                [InterfaceLanguageOptions.FrenchId] = new(
                    ShortDescription: "22 ans, Londres, étudiante en design de mode ; elle aime le padel et l’art.",
                    AgeText: "22",
                    Location: "Londres",
                    Role: "Étudiante en design de mode",
                    InterestsText: "Padel, art",
                    PersonalityText: "Amicale, chaleureuse, encourageante, naturelle",
                    SpeakingStyleText: "Court, clair, encourageant, conversationnel"),
                [InterfaceLanguageOptions.ItalianId] = new(
                    ShortDescription: "22 anni, Londra, studentessa di design della moda; le piacciono il padel e l’arte.",
                    AgeText: "22",
                    Location: "Londra",
                    Role: "Studentessa di design della moda",
                    InterestsText: "Padel, arte",
                    PersonalityText: "Amichevole, calorosa, incoraggiante, naturale",
                    SpeakingStyleText: "Breve, chiaro, incoraggiante, conversazionale"),
                [InterfaceLanguageOptions.PortugueseId] = new(
                    ShortDescription: "22 anos, Londres, estudante de design de moda; gosta de padel e arte.",
                    AgeText: "22",
                    Location: "Londres",
                    Role: "Estudante de design de moda",
                    InterestsText: "Padel, arte",
                    PersonalityText: "Amigável, acolhedora, encorajadora, natural",
                    SpeakingStyleText: "Curto, claro, encorajador, conversacional")
            },
            [TutorAvatarOptions.NelliAvatarId] = new Dictionary<string, TutorAvatarLocalizedProfileText>(StringComparer.OrdinalIgnoreCase)
            {
                [InterfaceLanguageOptions.EnglishId] = new(
                    ShortDescription: "18, future graphic design student; likes drawing and computer games.",
                    AgeText: "18",
                    Location: "—",
                    Role: "Future graphic design student",
                    InterestsText: "Drawing, computer games",
                    PersonalityText: "Kind, cheerful, likes jokes",
                    SpeakingStyleText: "Friendly, playful, light, supportive")
            }
        });

    public static TutorAvatarLocalizedProfileText GetProfileText(string? avatarId, string? interfaceLanguageId)
    {
        var normalizedAvatarId = TutorAvatarOptions.GetById(avatarId).Id;
        var normalizedLanguageId = InterfaceLanguageOptions.GetById(interfaceLanguageId).Id;

        if (ProfileTextByAvatarId.Value.TryGetValue(normalizedAvatarId, out var profileTextByLanguageId)
            && profileTextByLanguageId.TryGetValue(normalizedLanguageId, out var localizedProfileText))
        {
            return localizedProfileText;
        }

        if (ProfileTextByAvatarId.Value.TryGetValue(normalizedAvatarId, out profileTextByLanguageId)
            && profileTextByLanguageId.TryGetValue(InterfaceLanguageOptions.EnglishId, out var englishProfileText))
        {
            return englishProfileText;
        }

        return ProfileTextByAvatarId.Value[TutorAvatarOptions.DefaultAvatarId][InterfaceLanguageOptions.EnglishId];
    }
}
