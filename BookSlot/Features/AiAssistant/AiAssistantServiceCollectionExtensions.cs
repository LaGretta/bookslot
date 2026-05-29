using BookSlot.Features.AiAssistant.Configuration;
using BookSlot.Features.AiAssistant.Services;
using BookSlot.Features.AiAssistant.Telegram;

namespace BookSlot.Features.AiAssistant;

public static class AiAssistantServiceCollectionExtensions
{
    public static IServiceCollection AddAiAssistantFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<AiAssistantOptions>(
            configuration.GetSection(AiAssistantOptions.SectionName));

        services.AddScoped<IAiConversationInterpreter, SafeAiConversationInterpreter>();
        services.AddScoped<IAiReceptionistService, AiReceptionistService>();
        services.AddScoped<IAiConversationStore, AiConversationStore>();
        services.AddScoped<IAiAvailabilityService, AiAvailabilityService>();
        services.AddScoped<IAiBookingOrchestrator, AiBookingOrchestrator>();
        services.AddScoped<ITelegramAssistantHandler, SafeTelegramAssistantHandler>();
        services.AddSingleton<ITelegramTokenProtector, TelegramTokenProtector>();
        services.AddHttpClient<ITelegramMessageSender, TelegramMessageSender>();
        services.AddHttpClient(nameof(TelegramPollingWorker));
        services.AddHostedService<TelegramPollingWorker>();

        return services;
    }
}
