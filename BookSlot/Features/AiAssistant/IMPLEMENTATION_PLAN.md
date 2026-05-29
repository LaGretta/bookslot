# BookSlot AI Receptionist implementation plan

## Current BookSlot assets we reuse

- Business accounts: `Business` links the logged-in owner to services, schedule, subscription, and bookings.
- Services: existing active services are the source of truth for what clients can book.
- Working schedule: existing work schedules decide whether a business works on a date.
- Availability: AI checks available slots through `BookingService.GetAvailableSlotsAsync(...)`.
- Booking creation: manual AI draft confirmation uses `BookingService.CreateBookingAsync(...)`.
- Dashboard auth: AI settings live under the existing authenticated dashboard.

## Safe MVP architecture

The AI feature is isolated under `Features/AiAssistant`.

- `Configuration`: environment-driven feature settings and secrets references.
- `Contracts`: structured intent, draft, and reply DTOs.
- `Models`: AI settings, Telegram metadata, conversation memory, and appointment drafts.
- `Services`: deterministic parser, receptionist flow, availability wrapper, and booking orchestrator.
- `Telegram`: Telegram update DTOs, handler, and sendMessage wrapper.
- `Pages/Api`: development debug endpoints plus disabled-by-default Telegram webhook.
- `Pages/Dashboard/AiAssistant`: owner-only settings, tester, recent conversations, and draft review.

The public landing page, public booking flow, existing routes, auth, and production deployment are not changed.

## Phase plan

1. Safe foundation
   - Add isolated feature folder, contracts, services, settings, and dev endpoints.
   - Add AI-only database tables.
   - Keep automatic booking disabled.

2. Telegram proof of concept
   - Enable webhook only through environment variables.
   - Require a mapped business and active Telegram metadata.
   - Use Telegram secret header for webhook protection.

3. Conversation parser
   - Replace the deterministic parser with an AI-backed parser that returns strict JSON.
   - Keep missing-field checks and ask clarifying questions when data is incomplete.

4. Appointment draft to booking
   - Keep draft creation separate from real booking creation.
   - Only create bookings through `BookingService`.
   - Start with owner-reviewed manual confirmation, then consider auto-confirmation later.

5. Dashboard settings
   - Let owners configure tone, welcome message, and Telegram metadata.
   - Add clearer onboarding and status checks.
   - Hide secret values; use environment variables or encrypted storage.

6. Premium plan gate
   - Gate AI Receptionist behind `Pro` or a future AI plan.
   - Keep Free/Basic users on the existing booking flow.

7. Later integrations
   - WhatsApp Business API.
   - Instagram DM.
   - Facebook Messenger.
   - Google Calendar.
   - Email/SMS reminders.

## Production safety checklist

- Do not commit real Telegram/OpenAI tokens.
- Do not enable `AiAssistant__TelegramWebhookEnabled` in production before review.
- Do not map a production business ID before testing on a demo business.
- Do not let AI write directly to booking tables.
- Do not bypass existing availability checks.
- Do not change the public landing page without product/design review.
- Do not deploy this branch without owner approval.

## Next recommended coding step

Connect a real test Telegram bot to a demo business only:

1. Create a Telegram bot with BotFather.
2. Put token and secret into local/user secrets or environment variables.
3. Use a local tunnel for webhook testing.
4. Send a message to the bot and verify that a conversation plus draft is stored.
5. Confirm a ready draft manually from the dashboard.

