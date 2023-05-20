namespace NodeToUI.Requests;

public record CaptchaRequest(string Base64Image) : GuiRequest;
