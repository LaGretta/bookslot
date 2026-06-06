document.documentElement.dataset.theme = "dark";

(() => {
  const storageKey = "bookslot-language";
  const cookieName = "bookslot_lang";
  const originalText = new WeakMap();
  const originalAttrs = new WeakMap();
  const originalTitle = document.title;
  let currentLanguage = localStorage.getItem(storageKey) || getCookie(cookieName) || "uk";
  let translating = false;
  let scheduled = false;

  const dictionary = {
    "Як працює": "How it works",
    "AI-сценарій": "AI flow",
    "Докази": "Proof",
    "Тарифи": "Pricing",
    "Контакт": "Contact",
    "AI-помічник": "AI assistant",
    "Можливості": "Features",
    "Профіль": "Profile",
    "Меню": "Menu",
    "Почати": "Start",
    "Почати →": "Start →",
    "Відкрити AI": "Open AI",
    "Відкрити AI-помічника": "Open AI assistant",
    "Подивитись сценарій": "View the flow",
    "Приватна бета вже відкрита": "Private beta is now open",
    "Менше переписок. Більше записів. Спокійний графік.": "Fewer messages. More bookings. A calmer schedule.",
    "BookSlot дає локальному бізнесу преміальний шар онлайн-запису: сторінку бронювання, Telegram AI-помічника, чисту логіку слотів і чернетки під контроль власника.": "BookSlot gives local businesses a premium booking layer: a public booking page, a Telegram AI assistant, clean slot logic, and drafts that stay under the owner's control.",
    "Telegram AI": "Telegram AI",
    "BookSlot bot": "BookSlot bot",
    "Клієнт натискає AI-посилання": "The client taps the AI link",
    "онлайн · питає деталі запису": "online · asking for booking details",
    "Привіт, хочу манікюр завтра після 16:00.": "Hi, I want a manicure tomorrow after 4 PM.",
    "Можу запропонувати 16:30 або 17:45. Який час бронюємо?": "I can offer 4:30 PM or 5:45 PM. Which time should I book?",
    "16:30. Олена, +380...": "4:30 PM. Olena, +380...",
    "Готово. Я підготував запис для власника в BookSlot.": "Done. I prepared the booking draft for the owner in BookSlot.",
    "Бот знайшов вільні слоти": "The bot found available slots",
    "Чернетка для власника": "Draft for the owner",
    "Олена · манікюр · 16:30": "Olena · manicure · 4:30 PM",
    "готово": "ready",
    "чернетка запису": "booking draft",
    "відповіді клієнтам": "client replies",
    "втрачених запитів": "lost requests",

    "// як це працює": "// how it works",
    "Один раз ставите посилання. Далі люди записуються самі.": "Add the link once. Then people book themselves.",
    "Нижче показано як у маленькому відео: де власник бере лінк у BookSlot, куди вставляє його в профілі, і що бачить клієнт, коли натискає на це посилання.": "Below is shown like a small video: where the owner gets the BookSlot link, where it goes in the profile, and what the client sees after tapping it.",
    "Посилання лежить у кабінеті": "The link is in the dashboard",
    "Власник відкриває дашборд BookSlot": "The owner opens the BookSlot dashboard",
    "У блоці “Посилання для клієнтів” вже є готова адреса сторінки запису.": "The “Client booking link” block already has a ready-to-use booking page URL.",
    "Клієнт поки бачить звичайний профіль": "The client still sees a regular profile",
    "Для нього все виглядає знайомо: сторінка бізнесу і кнопка запису в шапці.": "Everything feels familiar: a business profile and a booking link in the bio.",
    "Власник копіює лінк": "The owner copies the link",
    "Натискає “Копіювати”": "Taps “Copy”",
    "Не треба нічого вигадувати вручну: BookSlot сам дає робоче посилання.": "No need to invent anything manually: BookSlot gives you a working link.",
    "Клієнт бачить той самий профіль": "The client sees the same profile",
    "Саме цей лінк потім стане входом у запис без переписки.": "That link becomes the entry point to booking without messaging.",
    "Лінк іде в шапку профілю": "The link goes into the profile bio",
    "Вставляє посилання у соцмережу": "Adds the link to social media",
    "У полі “Сайт / посилання” з’являється адреса BookSlot, і профіль готовий.": "The BookSlot URL appears in the “Website / link” field, and the profile is ready.",
    "Клієнт натискає посилання": "The client taps the link",
    "Він не пише в Direct, а просто переходить з профілю на сторінку запису.": "They do not message in Direct; they go straight from the profile to booking.",
    "Клієнт обирає послугу": "The client chooses a service",
    "У власника профіль уже працює": "The owner's profile is already working",
    "Посилання лишається в біо, а всі записи автоматично збираються в кабінеті.": "The link stays in the bio, and all bookings are collected automatically in the dashboard.",
    "BookSlot відкриває вільні слоти": "BookSlot opens available slots",
    "Клієнт бачить послуги, час і може вибрати зручний варіант за кілька секунд.": "The client sees services and times, then chooses a convenient slot in seconds.",
    "Запис готовий": "Booking is ready",
    "Власник отримує новий запис": "The owner receives a new booking",
    "Бронювання з’являється в дашборді, а сповіщення приходить на email.": "The booking appears in the dashboard, and a notification arrives by email.",
    "Клієнт бачить підтвердження": "The client sees confirmation",
    "Йому зрозуміло, коли приходити, а нагадування допоможе не забути запис.": "They know when to arrive, and a reminder helps them not forget.",
    "Власник": "Owner",
    "Клієнт": "Client",
    "крок": "step",
    "Записи": "Bookings",
    "Посилання": "Link",
    "Послуги": "Services",
    "Кабінет BookSlot": "BookSlot dashboard",
    "Посилання для клієнтів": "Client booking link",
    "Дайте це посилання людям, щоб вони могли записатися самі.": "Give people this link so they can book themselves.",
    "Копіювати": "Copy",
    "Сторінка запису вже готова": "Booking page is ready",
    "Посилання скопійовано": "Link copied",
    "Редагування профілю": "Edit profile",
    "Зберегти": "Save",
    "Манікюр · брови · вії": "Manicure · brows · lashes",
    "Сайт / посилання": "Website / link",
    "Опис профілю": "Profile bio",
    "Запис онлайн без Direct": "Online booking without Direct",
    "Новий запис у BookSlot": "New booking in BookSlot",
    "Олена · Манікюр": "Olena · manicure",
    "Сьогодні о 16:30": "Today at 4:30 PM",
    "Email власнику вже відправлено.": "The owner email has been sent.",
    "Манікюр": "Manicure",
    "Вільні місця на цей тиждень. Запис онлайн нижче.": "Open slots this week. Online booking below.",
    "BookSlot · Студія краси": "BookSlot · Beauty studio",
    "Оберіть послугу": "Choose a service",
    "Брови": "Brows",
    "60 хв": "60 min",
    "40 хв": "40 min",
    "Запис підтверджено": "Booking confirmed",
    "Манікюр · 16:30": "Manicure · 4:30 PM",
    "Нагадування прийде за 24 години до запису.": "A reminder will arrive 24 hours before the booking.",

    "// можливості у результат": "// outcome features",
    "Спокійна операційна система для кожної розмови про запис.": "A calm operating system for every booking conversation.",
    "Посилання для запису": "Booking link",
    "Чиста публічна сторінка, де клієнти обирають послугу, дату і час.": "A clean public page where clients choose a service, date, and time.",
    "Telegram-відповіді, які уточнюють послугу, дату, час, ім'я і контакт.": "Telegram replies that clarify the service, date, time, name, and contact.",
    "Контроль власника": "Owner control",
    "AI готує чернетку, а власник підтверджує запис у дашборді.": "AI prepares the draft, and the owner confirms the booking in the dashboard.",
    "Розумна доступність": "Smart availability",
    "Логіка слотів пов'язана з розкладом BookSlot і не вгадує навмання.": "Slot logic is connected to the BookSlot schedule and does not guess randomly.",
    "BookSlot перетворює хаотичні повідомлення в Telegram на чисті чернетки записів ще до того, як власник відкриє телефон.": "BookSlot turns chaotic Telegram messages into clean booking drafts before the owner even opens their phone.",
    "Студія краси": "Beauty studio",
    "Клієнти бачать реальні слоти, AI уточнює деталі, а дашборд залишається спокійним і чистим.": "Clients see real slots, AI clarifies the details, and the dashboard stays calm and clean.",
    "Локальний сервіс": "Local service",
    "Розклад власника": "Owner schedule",
    "Сценарій відчувається преміально, але не стає складним. У цьому і є вся ідея.": "The flow feels premium without becoming complicated. That is the whole idea.",
    "AI-шар запису": "AI booking layer",
    "// прості тарифи": "// simple pricing",
    "Оберіть шар запису, якого заслуговує ваш розклад.": "Choose the booking layer your schedule deserves.",
    "Безкоштовно": "Free",
    "Для перших реальних клієнтів": "For your first real clients",
    "30 записів на місяць": "30 bookings per month",
    "Сторінка запису": "Booking page",
    "Базові послуги": "Basic services",
    "Без email-супроводу": "No email support",
    "Почати безкоштовно": "Start for free",
    "200 записів і лист власнику про кожен новий запис": "200 bookings and owner email for every new booking",
    "200 записів на місяць": "200 bookings per month",
    "Необмежені послуги": "Unlimited services",
    "Email власнику про новий запис": "Owner email for each new booking",
    "Клієнтські листи та нагадування у Pro AI": "Client emails and reminders in Pro AI",
    "Оплатити 299 грн": "Pay 299 UAH",
    "AI-помічник і повний email-супровід записів": "AI assistant and full booking email support",
    "Необмежені записи": "Unlimited bookings",
    "AI-помічник у Telegram": "Telegram AI assistant",
    "Email власнику та клієнту": "Email to owner and client",
    "Нагадування клієнту за 24 години": "Client reminder 24 hours before",
    "Оплатити 599 грн": "Pay 599 UAH",
    "Преміальний AI-апгрейд": "Premium AI upgrade",
    "грн/міс": "UAH/month",
    "грн": "UAH",
    "// швидкий контакт": "// quick contact",
    "Зберіть вашу систему запису в один фокус.": "Bring your booking system into one focus.",
    "Запустіть публічне посилання для запису, дайте AI зібрати потрібні деталі і залиште фінальний контроль у вашому дашборді.": "Launch a public booking link, let AI collect the right details, and keep final control in your dashboard.",
    "До дашборда": "To dashboard",
    "Створити акаунт": "Create account",
    "Контактний шар": "Contact layer",
    "Написати BookSlot": "Message BookSlot",
    "Пошта": "Email",
    "Написати в Telegram": "Message on Telegram",

    "Центр": "Hub",
    "Головна": "Home",
    "Центр керування": "Control center",
    "Командний шар профілю": "Profile command layer",
    "Один спокійний простір для записів, AI-чернеток, публічного посилання і щоденного ритму.": "One calm space for bookings, AI drafts, public links, and daily rhythm.",
    "Усі записи": "All bookings",
    "Жива черга": "Live queue",
    "Фільтруйте, підтверджуйте і впорядковуйте запити клієнтів без втрати преміального відчуття.": "Filter, confirm, and organize client requests without losing the premium feeling.",
    "Меню послуг": "Service menu",
    "Стек пропозицій": "Offer stack",
    "Тримайте сторінку запису чистою, зрозумілою і швидкою для вибору послуги.": "Keep the booking page clean, clear, and fast for choosing a service.",
    "Сітка часу": "Time grid",
    "Двигун доступності": "Availability engine",
    "Робочі години, блокування і вільні слоти залишаються пов'язаними з кожним клієнтським сценарієм.": "Working hours, blocks, and open slots stay connected to every client flow.",
    "Шар розмов": "Conversation layer",
    "Telegram-запити, запропоновані слоти і чернетки записів в одному зрозумілому просторі.": "Telegram requests, suggested slots, and booking drafts in one clear space.",
    "Профіль бізнесу": "Business profile",
    "Шар айдентики": "Identity layer",
    "Публічне посилання, логотип і деталі бізнесу формують перше враження клієнта.": "The public link, logo, and business details shape the client's first impression.",
    "Підписка": "Subscription",
    "Шар росту": "Growth layer",
    "Оберіть ліміт записів і AI-можливості, які відповідають темпу вашого бізнесу.": "Choose the booking limit and AI capabilities that match your business pace.",
    "Навігація профілю": "Profile navigation",
    "Дашборд": "Dashboard",
    "Розклад": "Schedule",
    "Налаштування": "Settings",
    "Акаунт": "Account",
    "Вийти з акаунта": "Sign out",
    "Почати з BookSlot": "Start with BookSlot",
    "Створи сторінку запису, підключи AI-помічника і керуй клієнтами з дашборду.": "Create a booking page, connect the AI assistant, and manage clients from the dashboard.",
    "Реєстрація": "Sign up",
    "Увійти": "Log in",
    "Спробувати": "Try it",
    "Вийти": "Sign out",
    "Продукт": "Product",
    "Контакти": "Contacts",
    "Онлайн-запис і AI-помічник для малого бізнесу. Менше повідомлень, більше записів.": "Online booking and an AI assistant for small businesses. Fewer messages, more bookings.",
    "Всі права захищені.": "All rights reserved.",

    "Моя сторінка запису": "My booking page",
    "AI-помічник уже готується для вашого бізнесу": "The AI assistant is already getting ready for your business",
    "Telegram-бот може відповідати клієнтам українською або російською, збирати дані для запису і створювати чернетку в дашборді.": "The Telegram bot can reply to clients, collect booking details, and create a draft in the dashboard.",
    "Відкрити AI": "Open AI",
    "Всього записів": "Total bookings",
    "Цього місяця": "This month",
    "Сьогодні": "Today",
    "Послуг": "Services",
    "Останні записи": "Latest bookings",
    "Всі записи": "All bookings",
    "Ще немає записів": "No bookings yet",
    "Поділіться посиланням або протестуйте AI-помічника у Telegram.": "Share your link or test the Telegram AI assistant.",
    "Клієнт": "Client",
    "Послуга": "Service",
    "Дата/час": "Date/time",
    "Дата / Час": "Date / Time",
    "Статус": "Status",
    "Нотатки": "Notes",
    "Дії": "Actions",
    "Очікує": "Pending",
    "Очікують": "Pending",
    "Підтверджено": "Confirmed",
    "Підтверджені": "Confirmed",
    "Завершено": "Completed",
    "Завершені": "Completed",
    "Скасовано": "Cancelled",
    "Скасовані": "Cancelled",
    "Всі статуси": "All statuses",
    "Записів не знайдено": "No bookings found",
    "Завершити": "Complete",
    "Скасувати": "Cancel",
    "Скасувати запис?": "Cancel this booking?",

    "Бізнес не знайдено": "Business not found",
    "Перевірте посилання або зверніться до власника": "Check the link or contact the owner",
    "На головну": "Go home",
    "Запис підтверджено!": "Booking confirmed!",
    "Перевірте свою пошту — ми надіслали підтвердження": "Check your email — we sent a confirmation",
    "Послуги ще не додані. Зверніться до закладу.": "No services have been added yet. Contact the business.",
    "1. Оберіть послугу": "1. Choose a service",
    "2. Оберіть дату": "2. Choose a date",
    "3. Оберіть час": "3. Choose a time",
    "4. Ваші дані": "4. Your details",
    "Ім'я та прізвище *": "Full name *",
    "Телефон *": "Phone *",
    "Email *": "Email *",
    "Коментар": "Comment",
    "Підтвердити запис": "Confirm booking",
    "Іван Петренко": "John Smith",

    "Відновлення пароля": "Password recovery",
    "Вхід": "Login",
    "// безпечний вхід": "// secure login",
    "Поверніться до записів, розкладу й AI-помічника без зайвого шуму.": "Return to bookings, schedule, and the AI assistant without extra noise.",
    "// відновлення доступу": "// restore access",
    "Введіть email, і ми надішлемо посилання для скидання пароля.": "Enter your email and we will send a password reset link.",
    "Перевірте пошту": "Check your email",
    "Якщо акаунт з таким email існує, ми надіслали посилання для скидання пароля.": "If an account with this email exists, we sent a password reset link.",
    "Надіслати посилання": "Send link",
    "Назад до входу": "Back to login",
    "Увійти в акаунт": "Log in to your account",
    "Створити акаунт": "Create account",
    "14 днів безкоштовно, без картки": "14 days free, no card required",
    "Пароль": "Password",
    "Підтвердити пароль": "Confirm password",
    "Зареєструватись": "Sign up",
    "Забули пароль?": "Forgot password?",
    "Немає акаунту?": "No account?",
    "Вже є акаунт?": "Already have an account?",
    "ваш пароль": "your password",
    "Ваш пароль": "your password",
    "мінімум 6 символів, одна цифра": "minimum 6 characters, one digit",
    "повторіть пароль": "repeat password",

    "AI-розмова": "AI conversation",
    "Повідомлення": "Messages",
    "Повідомлень ще немає.": "No messages yet.",
    "Система": "System",
    "Чернетка запису": "Booking draft",
    "Чернетку ще не зібрано.": "The draft has not been collected yet.",
    "Дата": "Date",
    "Час": "Time",
    "Контакт": "Contact",
    "Створити запис із чернетки": "Create booking from draft",
    "Чернетка ще неповна": "Draft is still incomplete",

    "Адмін-панель": "Admin panel",
    "Управління промо-акціями": "Promo campaign management",
    "Акаунтів": "Accounts",
    "Бізнесів": "Businesses",
    "Записів цього місяця": "Bookings this month",
    "Оплачених підписок": "Paid subscriptions",
    "Нових за 7 днів": "New in 7 days",
    "Моя підписка": "My subscription",
    "Застосувати": "Apply",
    "Забрати": "Remove",
    "Пошук по email...": "Search by email...",
    "Скинути": "Reset",
    "Профіль акаунта": "Account profile",
    "Закрити": "Close",
    "Email підтверджено": "Email confirmed",
    "Email не підтверджено": "Email not confirmed",
    "Контакти акаунта": "Account contacts",
    "Без імені": "No name",
    "Телефон:": "Phone:",
    "не вказано": "not provided",
    "Бізнес": "Business",
    "Категорія:": "Category:",
    "Телефон бізнесу:": "Business phone:",
    "Адреса:": "Address:",
    "Бізнес ще не створено.": "Business has not been created yet.",
    "Активність": "Activity",
    "Акція": "Promo",
    "використана": "used",
    "доступна": "available",
    "Останній запис:": "Latest booking:",
    "немає": "none",
    "Відкрити сторінку запису": "Open booking page",
    "Бізнес не створено": "Business not created",
    "Використано": "Used",
    "Доступна": "Available",
    "Немає підписки": "No subscription",
    "Нічого не знайдено": "Nothing found",
    "Показано до 50 останніх реєстрацій.": "Showing up to 50 latest registrations.",

    "// старт простору": "// start your space",
    "Запустіть сторінку запису, розклад і чистий дашборд для свого бізнесу.": "Launch a booking page, schedule, and clean dashboard for your business.",
    "Новий пароль": "New password",
    "// новий доступ": "// new access",
    "Створіть новий пароль і поверніться до свого робочого простору.": "Create a new password and return to your workspace.",
    "Пароль змінено": "Password changed",
    "Тепер можете увійти з новим паролем.": "You can now log in with your new password.",
    "Повтори пароль": "Repeat password",
    "Зберегти пароль": "Save password",
    "мінімум 6 символів": "minimum 6 characters",
    "ще раз": "one more time",

    "Додати послугу": "Add service",
    "Ще немає послуг": "No services yet",
    "Додайте перші послуги, щоб клієнти могли записатися": "Add your first services so clients can book.",
    "Назва": "Name",
    "Опис": "Description",
    "Тривалість": "Duration",
    "Ціна": "Price",
    "Активна": "Active",
    "Неактивна": "Inactive",
    "Видалити послугу?": "Delete this service?",
    "Назва *": "Name *",
    "Опис": "Description",
    "Тривалість (хв) *": "Duration (min) *",
    "Ціна (грн) *": "Price (UAH) *",
    "Тривалість (хв)": "Duration (min)",
    "Ціна (грн)": "Price (UAH)",
    "Додати": "Add",
    "Редагувати послугу": "Edit service",
    "Послугу додано!": "Service added!",
    "Послугу оновлено!": "Service updated!",
    "Послугу видалено!": "Service deleted!",
    "напр. Стрижка чоловіча": "e.g. Men's haircut",
    "Короткий опис послуги": "Short service description",

    "Розклад роботи": "Working schedule",
    "Робочі години": "Working hours",
    "Встановіть графік на тиждень. Клієнти записуються лише у вільний час.": "Set your weekly schedule. Clients can book only during available time.",
    "Гортайте таблицю вліво-вправо": "Scroll the table left and right",
    "День": "Day",
    "Робочий": "Working",
    "Початок": "Start",
    "Кінець": "End",
    "Зберегти розклад": "Save schedule",
    "Заблоковані часи": "Blocked times",
    "Додайте конкретні дату + час, щоб клієнти не могли на них записатись — наприклад, якщо вас вже записали деінде.": "Add a specific date and time so clients cannot book it — for example, if you are already booked elsewhere.",
    "Причина": "Reason",
    "Причина (необов'язково)": "Reason (optional)",
    "Заблокувати": "Block",
    "Розблокувати цей час?": "Unblock this time?",
    "Заблокованих годин немає — всі вільні": "No blocked times — everything is open",
    "Розклад збережено!": "Schedule saved!",
    "Невірна дата або час.": "Invalid date or time.",
    "Неділя": "Sunday",
    "Понеділок": "Monday",
    "Вівторок": "Tuesday",
    "Середа": "Wednesday",
    "Четвер": "Thursday",
    "П'ятниця": "Friday",
    "Субота": "Saturday",
    "напр. Особиста зустріч": "e.g. Personal meeting",

    "Налаштування бізнесу": "Business settings",
    "Помилка:": "Error:",
    "Фото / Лого бізнесу": "Business photo / logo",
    "Натисніть щоб змінити фото": "Click to change photo",
    "Обрати фото": "Choose photo",
    "JPG, PNG або WebP, до 2 МБ": "JPG, PNG, or WebP, up to 2 MB",
    "✓ Фото обрано": "✓ Photo selected",
    "Назва бізнесу *": "Business name *",
    "URL-адреса (slug) *": "URL address (slug) *",
    "Тільки малі літери, цифри та дефіси. Якщо slug зайнятий — додайте цифру, напр.": "Only lowercase letters, numbers, and hyphens. If the slug is taken, add a number, e.g.",
    "Категорія": "Category",
    "Барбершоп": "Barbershop",
    "Косметолог": "Cosmetologist",
    "Манікюр/Педикюр": "Manicure/Pedicure",
    "Масаж": "Massage",
    "Репетитор": "Tutor",
    "Фітнес/Тренер": "Fitness/Coach",
    "Лікар": "Doctor",
    "Інше": "Other",
    "Короткий опис вашого бізнесу для клієнтів": "Short description of your business for clients",
    "Цей slug вже зайнятий. Оберіть інший.": "This slug is already taken. Choose another one.",
    "Бізнес створено! Тепер додайте послуги та налаштуйте розклад.": "Business created! Now add services and set up your schedule.",
    "Налаштування збережено!": "Settings saved!",
    "напр. Барбершоп Іван": "e.g. Ivan Barbershop",

    "Поточний план": "Current plan",
    "Безкоштовний": "Free",
    "Діє до:": "Valid until:",
    "Прострочено — поновіть": "Expired — renew",
    "Безкоштовний план (30 записів/міс)": "Free plan (30 bookings/month)",
    "Записів на місяць:": "Bookings per month:",
    "Необмежено": "Unlimited",
    "🎁 Обмежена акція": "🎁 Limited promo",
    "7 днів Basic — безкоштовно.": "7 days of Basic — free.",
    "Просто зроби сторіс.": "Just post a story.",
    "Запіарь нас в Instagram або TikTok Stories і отримай Basic підписку на тиждень. Одна акція на акаунт.": "Promote us in Instagram or TikTok Stories and get a Basic subscription for one week. One promo per account.",
    "Крок 1": "Step 1",
    "Крок 2": "Step 2",
    "Крок 3": "Step 3",
    "Зроби сторіс із згадкою сайту BookSlot у своєму Instagram або TikTok": "Post a story mentioning the BookSlot website on your Instagram or TikTok.",
    "Скинь скріншот у Telegram": "Send a screenshot in Telegram",
    "Отримай 7 днів Basic протягом кількох годин": "Get 7 days of Basic within a few hours.",
    "Акцію використано": "Promo used",
    "Дякуємо за підтримку! Ти вже отримав свої 7 днів 🙌": "Thanks for the support! You have already received your 7 days 🙌",
    "Stripe ще не налаштований": "Stripe is not configured yet",
    "Відкрий appsettings.json і заповни:": "Open appsettings.json and fill in:",
    "Обрати план": "Choose a plan",
    "Масштабуйте по мірі росту бізнесу. Без прихованих платежів.": "Scale as your business grows. No hidden fees.",
    "БЕЗКОШТОВНО": "FREE",
    "Щоб почати і спробувати": "To start and try it",
    "До 30 записів/міс": "Up to 30 bookings/month",
    "Email-супровід записів": "Booking email support",
    "Аналітика": "Analytics",
    "⭐ Найпопулярніший": "⭐ Most popular",
    "200 записів + email власнику": "200 bookings + owner email",
    "До 200 записів/міс": "Up to 200 bookings/month",
    "Email клієнту та нагадування за 24 години — у Pro AI": "Client email and 24-hour reminder — in Pro AI",
    "Незабаром": "Soon",
    "Оплата пройшла! Підписку активовано.": "Payment successful! Subscription activated.",
    "Оплату скасовано.": "Payment cancelled.",
    "Невідомий тариф. Оберіть Basic або Pro AI.": "Unknown plan. Choose Basic or Pro AI.",
    "Stripe ще не налаштований": "Stripe is not configured yet",

    "Готово до запису": "Ready to book",
    "Чекаємо клієнта": "Waiting for client",
    "Активна": "Active",
    "Клієнт у Telegram": "Client in Telegram",
    "Привіт! Я допоможу обрати послугу та підготувати запис.": "Hi! I will help choose a service and prepare the booking.",
    "Дружній, чіткий і короткий": "Friendly, clear, and brief",
    "Преміальний і ввічливий": "Premium and polite",
    "Неформальний і теплий": "Casual and warm",
    "Що потрібно для роботи": "What is needed to work",
    "Додати послуги": "Add services",
    "Налаштувати розклад": "Set up schedule",
    "AI увімкнений": "AI enabled",
    "Увімкни AI →": "Enable AI →",
    "Telegram готовий": "Telegram ready",
    "Telegram не готовий": "Telegram not ready",
    "Посилання на вашого AI-бота": "Your AI bot link",
    "Дай це посилання клієнтам — і бот сам відповідатиме за вас": "Give this link to clients — the bot will reply for you.",
    "Постав у Instagram bio": "Put it in your Instagram bio",
    "Кидай у відповідь на питання": "Send it as a reply to questions",
    "Зроби QR-код для салону": "Create a QR code for your studio",
    "Посилання працюватиме, коли буде увімкнено AI, додано послуги і налаштовано розклад (див. чеклист угорі).": "The link will work when AI is enabled, services are added, and the schedule is configured (see the checklist above).",
    "Telegram-бот платформи ще не налаштований адміністратором.": "The platform Telegram bot has not been configured by the administrator yet.",
    "Як тільки його підключать — тут зʼявиться ваше персональне посилання.": "Once it is connected, your personal link will appear here.",
    "Спробувати самому": "Try it yourself",
    "Напиши як клієнт — побачиш, що відповість бот. Запис не створюється.": "Write as a client and see what the bot replies. No booking will be created.",
    "Тест": "Test",
    "Перевірити": "Check",
    "Відповідь бота": "Bot reply",
    "Ось що бачить ваш клієнт": "What your client sees",
    "Бот сам веде розмову — від «привіт» до готового запису": "The bot leads the conversation itself — from “hello” to a ready booking.",
    "Останні розмови": "Latest conversations",
    "Поки немає розмов. Поділись посиланням з клієнтами.": "No conversations yet. Share the link with clients.",
    "Увімкнути AI-помічника": "Enable AI assistant",
    "Привітання для клієнта": "Client greeting",
    "Перше повідомлення, яке бачить клієнт": "The first message the client sees",
    "Кілька слів про ваш бізнес": "A few words about your business",
    "Допомагає боту краще відповідати": "Helps the bot reply better",
    "Тон спілкування": "Tone of voice",
    "Скопійовано": "Copied",
    "Привіт! 💅": "Hi! 💅",
    "Вітаю! Що вас цікавить?": "Hello! What are you interested in?",
    "Педикюр": "Pedicure",
    "Чудово! На завтра є вільні години:": "Great! Tomorrow has these available times:",
    "Запишіть, будь ласка, імʼя та телефон 🙂": "Please send your name and phone number 🙂",
    "Олена, +380 67 123 45 67": "Olena, +380 67 123 45 67",
    "Запис підготовлено!": "Booking prepared!",
    "Привіт, є завтра на манікюр о 16:00?": "Hi, is there a manicure tomorrow at 4 PM?"
  };

  const attrNames = ["placeholder", "title", "aria-label", "value"];

  function getCookie(name) {
    const match = document.cookie.match(new RegExp(`(?:^|; )${name}=([^;]*)`));
    return match ? decodeURIComponent(match[1]) : "";
  }

  function setCookie(name, value) {
    document.cookie = `${name}=${encodeURIComponent(value)}; path=/; max-age=31536000; samesite=lax`;
  }

  function normalize(text) {
    return text.replace(/\s+/g, " ").trim();
  }

  function translateValue(value) {
    const trimmed = normalize(value);
    if (!trimmed) return value;
    return dictionary[trimmed] || value;
  }

  function translateTextNode(node) {
    const parent = node.parentElement;
    if (!parent || parent.closest("script, style, code, pre, [data-no-translate]")) return;

    if (!originalText.has(node)) originalText.set(node, node.nodeValue);
    const original = originalText.get(node);

    if (currentLanguage === "uk") {
      node.nodeValue = original;
      return;
    }

    const translated = translateValue(original);
    if (translated === original) return;

    const leading = original.match(/^\s*/)?.[0] || "";
    const trailing = original.match(/\s*$/)?.[0] || "";
    node.nodeValue = `${leading}${translated}${trailing}`;
  }

  function translateAttributes(element) {
    if (element.closest?.("script, style, code, pre, [data-no-translate]")) return;

    attrNames.forEach((name) => {
      if (!element.hasAttribute?.(name)) return;
      if (name === "value" && !["BUTTON", "INPUT"].includes(element.tagName)) return;
      if (element.tagName === "INPUT" && !["button", "submit", "reset"].includes((element.type || "").toLowerCase())) return;

      let stored = originalAttrs.get(element);
      if (!stored) {
        stored = {};
        originalAttrs.set(element, stored);
      }
      if (!(name in stored)) stored[name] = element.getAttribute(name);

      const original = stored[name];
      let translated = translateValue(original);
      if (currentLanguage === "en" && name === "placeholder" && /пароль/i.test(original)) {
        translated = "your password";
      }
      element.setAttribute(name, currentLanguage === "uk" ? original : translated);
    });
  }

  function walk(root) {
    if (!root) return;

    if (root.nodeType === Node.TEXT_NODE) {
      translateTextNode(root);
      return;
    }

    if (root.nodeType !== Node.ELEMENT_NODE && root.nodeType !== Node.DOCUMENT_NODE) return;

    if (root.nodeType === Node.ELEMENT_NODE) translateAttributes(root);

    const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT, {
      acceptNode(node) {
        if (node.nodeType === Node.ELEMENT_NODE && node.closest?.("script, style, code, pre, [data-no-translate]")) {
          return NodeFilter.FILTER_REJECT;
        }
        return NodeFilter.FILTER_ACCEPT;
      },
    });

    let node = walker.nextNode();
    while (node) {
      if (node.nodeType === Node.TEXT_NODE) translateTextNode(node);
      else translateAttributes(node);
      node = walker.nextNode();
    }
  }

  function updateSwitches() {
    document.querySelectorAll("[data-language-switch] button[data-lang]").forEach((button) => {
      button.classList.toggle("is-active", button.dataset.lang === currentLanguage);
      button.setAttribute("aria-pressed", String(button.dataset.lang === currentLanguage));
    });
  }

  function polishKnownFields() {
    document.querySelectorAll('input[type="password"][placeholder]').forEach((input) => {
      const original = input.getAttribute("data-i18n-placeholder-original") || input.getAttribute("placeholder");
      if (!input.hasAttribute("data-i18n-placeholder-original")) {
        input.setAttribute("data-i18n-placeholder-original", original);
      }
      input.setAttribute("placeholder", currentLanguage === "en" ? "your password" : input.getAttribute("data-i18n-placeholder-original"));
    });
  }

  function applyLanguage() {
    if (translating) return;
    translating = true;
    document.documentElement.lang = currentLanguage === "en" ? "en" : "uk";
    document.documentElement.dataset.lang = currentLanguage;
    walk(document.body);
    polishKnownFields();
    if (originalTitle) document.title = currentLanguage === "en" ? translateValue(originalTitle) : originalTitle;
    updateSwitches();
    translating = false;
  }

  function scheduleApply() {
    if (scheduled || translating) return;
    scheduled = true;
    window.requestAnimationFrame(() => {
      scheduled = false;
      applyLanguage();
    });
  }

  function setLanguage(language) {
    currentLanguage = language === "en" ? "en" : "uk";
    localStorage.setItem(storageKey, currentLanguage);
    setCookie(cookieName, currentLanguage);
    applyLanguage();
  }

  document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-language-switch] button[data-lang]");
    if (!button) return;
    setLanguage(button.dataset.lang);
  });

  window.BookSlotI18n = {
    setLanguage,
    getLanguage: () => currentLanguage,
    translate: scheduleApply,
  };

  document.addEventListener("DOMContentLoaded", () => {
    applyLanguage();
    const observer = new MutationObserver(scheduleApply);
    observer.observe(document.body, { childList: true, subtree: true, characterData: true });
  });
})();
