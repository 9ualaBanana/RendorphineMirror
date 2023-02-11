namespace Telegram.Telegram.Updates.Images.Models;

public abstract record MediaFileProcessingCallbackData<T> : TelegramCallbackData<T> where T : struct, Enum
{
    public string FileCacheKey => Arguments.First();
    internal abstract IContentType ContentType { get; }


    public MediaFileProcessingCallbackData(TelegramCallbackData<T> original) : base(original)
    {
    }

    public MediaFileProcessingCallbackData(T value, string[] arguments) : base(value, arguments)
    {
    }
}