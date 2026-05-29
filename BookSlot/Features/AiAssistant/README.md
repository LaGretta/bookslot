# BookSlot AI Assistant foundation

This folder is an isolated preparation area for the future BookSlot AI Assistant / AI Receptionist.

Current safety rules:

- No production behavior is changed by these files.
- Database changes are isolated to new AI Assistant tables.
- No Telegram token, OpenAI key, or other secret should be committed.
- AI must not create bookings directly through `ApplicationDbContext`.
- Booking creation must go through `BookingService.CreateBookingAsync(...)`.
- Availability checks must reuse `BookingService.GetAvailableSlotsAsync(...)`.

## MVP idea

The AI Assistant should collect structured booking data from chat:

- service
- date
- time
- customer name
- customer contact

Only when all required fields are present should the system try to create a booking.

## Proposed phases

1. Safe contracts and service skeletons.
2. Telegram webhook proof of concept in a separate API endpoint.
3. AI parser that returns structured intent and draft data.
4. Availability check through the existing booking service.
5. Booking creation through the existing booking service.
6. Dashboard settings page for business owners.
7. Billing/premium gating.
8. WhatsApp/Instagram integrations later.

## Files

- `Contracts/AiIntentType.cs` describes supported chat intents.
- `Contracts/AiBookingDraft.cs` stores structured data collected from the conversation.
- `Contracts/AiAssistantReply.cs` stores the assistant result and missing fields.
- `Services/IAiConversationInterpreter.cs` defines the parser contract.
- `Services/IAiAvailabilityService.cs` defines an AI-safe availability lookup contract.
- `Services/IAiReceptionistService.cs` defines the higher-level receptionist flow contract.
- `Services/IAiBookingOrchestrator.cs` defines the booking orchestration contract.
- `Services/SafeAiConversationInterpreter.cs` is a deterministic placeholder with no external AI call.
- `Services/AiAvailabilityService.cs` wraps the existing `BookingService.GetAvailableSlotsAsync(...)`.
- `Services/AiReceptionistService.cs` reads active services, enriches draft data, checks availability, and returns a safe reply without creating a booking.
- `AiReceptionistRequest.RequireEnabledSettings` lets future production channels require the dashboard toggle before responding.
- `Services/AiBookingOrchestrator.cs` wraps the existing `BookingService`.
- `Configuration/AiAssistantOptions.cs` defines future config defaults.
- `Models/AiAssistantSettings.cs` stores non-secret dashboard settings for each business.
- `Models/TelegramBotConnection.cs` stores non-secret Telegram metadata only. Bot tokens are intentionally not stored here.
- `Models/AiConversation.cs`, `AiConversationMessage.cs`, and `AppointmentDraft.cs` store conversation memory and draft booking state for future multi-message Telegram flows.
- `Telegram/*` sketches the future Telegram MVP handler without adding a public webhook yet.
- `Pages/Dashboard/AiAssistant/Index.cshtml` is the dashboard preview/status page for business owners.

## Local debug endpoint

`AiAssistantSettings` is now persisted by the `AddAiAssistantSettings` migration.
It stores only non-secret owner settings: enabled flag, welcome message, business description, and tone of voice.
Telegram/OpenAI tokens are intentionally not stored here.

`AiConversations`, `AiConversationMessages`, and `AppointmentDrafts` are persisted by the `AddAiConversations` migration.
They store chat memory and booking draft state for future multi-message Telegram conversations.
They do not create real `Booking` rows.
Dashboard owners can review a conversation and manually create a booking from a ready draft.
That manual action uses `BookingService.CreateBookingAsync(...)` and still checks availability.

`/Api/AiAssistantDebug` is a development-only endpoint for checking the safe parser shape.
When `businessId` is provided, it uses the higher-level receptionist service and can read active services plus availability.

Example:

```text
/Api/AiAssistantDebug?message=hello&businessId=1&serviceId=2&date=2026-05-29&time=16:00&customerName=Anna&customerContact=+48123123123
```

In non-development environments this endpoint returns `404`.

`/Api/AiAvailabilityDebug` is a development-only endpoint for checking availability through the AI wrapper.
It reuses `BookingService.GetAvailableSlotsAsync(...)` and does not create bookings.

Example:

```text
/Api/AiAvailabilityDebug?businessId=1&serviceId=2&date=2026-05-29
```

`/Api/TelegramAiDebug` is a development-only endpoint for checking the Telegram handler shape.
It accepts a Telegram-like update JSON and returns the message that the system would plan to send.
It does not call Telegram, does not call OpenAI, and does not write to the database.
When `businessId` is provided as a query parameter, the debug endpoint can read active services and availability for that business.

`/Api/TelegramWebhook` is a disabled-by-default production-shaped skeleton.
It returns `404` unless `AiAssistant:TelegramWebhookEnabled` is true and requires `AiAssistant:TelegramWebhookBusinessId`.
When enabled, it requires an active `TelegramBotConnection`, requires dashboard AI settings to be enabled, and sends replies through Telegram `sendMessage`.
If `AiAssistant:TelegramWebhookSecretToken` is configured, requests must include Telegram's `X-Telegram-Bot-Api-Secret-Token` header with the same value.
`AiAssistant:TelegramBotToken` and `AiAssistant:TelegramWebhookSecretToken` must come from environment/config and must not be committed to the repository.
It still does not create bookings automatically.

For local demos without a public webhook URL, `TelegramPollingWorker` can poll Telegram `getUpdates`.
It is disabled by default and requires `AiAssistant:TelegramPollingEnabled`, `AiAssistant:TelegramPollingBusinessId`, and `AiAssistant:TelegramBotToken`.
Polling also requires the business to have enabled AI settings and active Telegram metadata.

Local environment example:

```text
AiAssistant__TelegramWebhookEnabled=true
AiAssistant__TelegramWebhookBusinessId=1
AiAssistant__TelegramBotToken=<telegram-bot-token>
AiAssistant__TelegramWebhookSecretToken=<random-secret-token>
AiAssistant__TelegramPollingEnabled=true
AiAssistant__TelegramPollingBusinessId=1
```

`/Api/AiAssistantSetupDebug` is a development-only helper for local setup.
`GET` lists local businesses and their AI/Telegram status.
`POST` can enable AI settings and Telegram metadata for a local business.
It does not store Telegram bot tokens.

The current interpreter is intentionally simple. It uses local keyword rules only, so it can show the future response shape without making AI calls or changing bookings.
It can recognize basic English, Polish, Ukrainian, and Russian booking/date keywords for the MVP demo.
Customer-facing replies are localized to Ukrainian or Russian when the incoming message uses those languages.
Telegram usernames are treated as contact data when available, but they are not treated as email addresses.

Example request body:

```json
{
  "update_id": 1001,
  "message": {
    "message_id": 10,
    "chat": {
      "id": 123456,
      "type": "private"
    },
    "from": {
      "id": 123456,
      "first_name": "Anna",
      "username": "anna_demo"
    },
    "text": "Hi, can I book a manicure tomorrow?"
  }
}
```

## Current warnings

The Telegram webhook is still an MVP skeleton:

- It supports one environment-mapped business at a time.
- It sends Telegram replies only when a bot token is configured.
- It stores conversation memory and draft data, but it does not create bookings automatically.
- The dashboard can manually create a booking from a ready draft for owner review.
