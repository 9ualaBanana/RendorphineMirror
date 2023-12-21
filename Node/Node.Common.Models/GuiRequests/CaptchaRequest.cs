namespace Node.Common.Models.GuiRequests;

public record CaptchaRequest(string Base64Image) : GuiRequest;
