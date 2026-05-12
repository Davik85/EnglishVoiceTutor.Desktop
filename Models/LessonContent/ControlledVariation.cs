namespace EnglishVoiceTutor.Desktop.Models.LessonContent;

public sealed class ControlledVariation
{
    public List<string> CanChange { get; set; } = [];

    public List<string> CannotChange { get; set; } = [];

    public List<ContextVariant> ContextVariants { get; set; } = [];

    public List<string> CustomContextRules { get; set; } = [];

    public string InvalidContextRedirect { get; set; } = string.Empty;
}
