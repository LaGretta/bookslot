import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

const navItems = [
  { label: "Як працює", href: "#how-it-works" },
  { label: "AI-сценарій", href: "#ai-flow" },
  { label: "Докази", href: "#proof" },
  { label: "Тарифи", href: "#pricing" },
  { label: "Контакт", href: "#contact" },
];

const metrics = [
  ["18с", "чернетка запису"],
  ["24/7", "відповіді клієнтам"],
  ["0", "втрачених запитів"],
];

const proofCards = [
  {
    quote: "BookSlot перетворює хаотичні повідомлення в Telegram на чисті чернетки записів ще до того, як власник відкриє телефон.",
    person: "Студія краси",
    role: "Манікюр, брови, вії",
  },
  {
    quote: "Клієнти бачать реальні слоти, AI уточнює деталі, а дашборд залишається спокійним і чистим.",
    person: "Локальний сервіс",
    role: "Розклад власника",
  },
  {
    quote: "Сценарій відчувається преміально, але не стає складним. У цьому і є вся ідея.",
    person: "BookSlot Pro AI",
    role: "AI-шар запису",
  },
];

const features = [
  ["Посилання для запису", "Чиста публічна сторінка, де клієнти обирають послугу, дату і час."],
  ["AI-помічник", "Telegram-відповіді, які уточнюють послугу, дату, час, ім'я і контакт."],
  ["Контроль власника", "AI готує чернетку, а власник підтверджує запис у дашборді."],
  ["Розумна доступність", "Логіка слотів пов'язана з розкладом BookSlot і не вгадує навмання."],
];

const plans = [
  {
    name: "Безкоштовно",
    price: "0",
    note: "Для перших реальних клієнтів",
    items: ["30 записів на місяць", "Сторінка запису", "Базові послуги", "Без email-супроводу"],
    cta: "Почати безкоштовно",
  },
  {
    name: "Basic",
    price: "299",
    note: "200 записів і лист власнику про кожен новий запис",
    checkoutPlan: "Basic",
    items: ["200 записів на місяць", "Необмежені послуги", "Email власнику про новий запис", "Клієнтські листи та нагадування у Pro AI"],
    cta: "Оплатити 299 грн",
  },
  {
    name: "Pro AI",
    price: "599",
    note: "AI-помічник і повний email-супровід записів",
    featured: true,
    checkoutPlan: "Pro",
    items: ["Необмежені записи", "AI-помічник у Telegram", "Email власнику та клієнту", "Нагадування клієнту за 24 години"],
    cta: "Оплатити 599 грн",
  },
];

const walkthroughFrames = [
  {
    label: "01",
    title: "Посилання лежить у кабінеті",
    ownerScene: "owner-dashboard",
    clientScene: "client-profile",
    ownerTitle: "Власник відкриває дашборд BookSlot",
    ownerText: "У блоці “Посилання для клієнтів” вже є готова адреса сторінки запису.",
    clientTitle: "Клієнт поки бачить звичайний профіль",
    clientText: "Для нього все виглядає знайомо: сторінка бізнесу і кнопка запису в шапці.",
  },
  {
    label: "02",
    title: "Власник копіює лінк",
    ownerScene: "owner-copy",
    clientScene: "client-profile",
    ownerTitle: "Натискає “Копіювати”",
    ownerText: "Не треба нічого вигадувати вручну: BookSlot сам дає робоче посилання.",
    clientTitle: "Клієнт бачить той самий профіль",
    clientText: "Саме цей лінк потім стане входом у запис без переписки.",
  },
  {
    label: "03",
    title: "Лінк іде в шапку профілю",
    ownerScene: "owner-bio",
    clientScene: "client-tap",
    ownerTitle: "Вставляє посилання у соцмережу",
    ownerText: "У полі “Сайт / посилання” з’являється адреса BookSlot, і профіль готовий.",
    clientTitle: "Клієнт натискає посилання",
    clientText: "Він не пише в Direct, а просто переходить з профілю на сторінку запису.",
  },
  {
    label: "04",
    title: "Клієнт обирає послугу",
    ownerScene: "owner-live",
    clientScene: "client-booking",
    ownerTitle: "У власника профіль уже працює",
    ownerText: "Посилання лишається в біо, а всі записи автоматично збираються в кабінеті.",
    clientTitle: "BookSlot відкриває вільні слоти",
    clientText: "Клієнт бачить послуги, час і може вибрати зручний варіант за кілька секунд.",
  },
  {
    label: "05",
    title: "Запис готовий",
    ownerScene: "owner-notified",
    clientScene: "client-done",
    ownerTitle: "Власник отримує новий запис",
    ownerText: "Бронювання з’являється в дашборді, а сповіщення приходить на email.",
    clientTitle: "Клієнт бачить підтвердження",
    clientText: "Йому зрозуміло, коли приходити, а нагадування допоможе не забути запис.",
  },
];

const checkoutPath = (plan) => `/Dashboard/Subscription?handler=Checkout&plan=${encodeURIComponent(plan)}`;

const registerPath = (returnUrl = "/Dashboard") =>
  `/Identity/Account/Register?returnUrl=${encodeURIComponent(returnUrl)}`;

function planHref(plan, isLoggedIn) {
  if (!plan.checkoutPlan) {
    return isLoggedIn ? "/Dashboard/Subscription" : registerPath("/Dashboard/Subscription");
  }

  const target = checkoutPath(plan.checkoutPlan);
  return isLoggedIn ? target : registerPath(target);
}

function useReveal() {
  useEffect(() => {
    const nodes = document.querySelectorAll("[data-reveal]");

    if (!("IntersectionObserver" in window)) {
      nodes.forEach((node) => node.classList.add("is-visible"));
      return undefined;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
          }
        });
      },
      { rootMargin: "0px 0px -12% 0px", threshold: 0.14 },
    );

    nodes.forEach((node) => observer.observe(node));
    return () => observer.disconnect();
  }, []);
}

function useTilt() {
  useEffect(() => {
    const cards = document.querySelectorAll("[data-tilt]");

    const onMove = (event) => {
      const card = event.currentTarget;
      const rect = card.getBoundingClientRect();
      const x = event.clientX - rect.left - rect.width / 2;
      const y = event.clientY - rect.top - rect.height / 2;
      card.style.setProperty("--tilt-x", `${(-y / rect.height) * 10}deg`);
      card.style.setProperty("--tilt-y", `${(x / rect.width) * 12}deg`);
      card.style.setProperty("--glow-x", `${event.clientX - rect.left}px`);
      card.style.setProperty("--glow-y", `${event.clientY - rect.top}px`);
    };

    const onLeave = (event) => {
      const card = event.currentTarget;
      card.style.setProperty("--tilt-x", "0deg");
      card.style.setProperty("--tilt-y", "0deg");
    };

    cards.forEach((card) => {
      card.addEventListener("mousemove", onMove);
      card.addEventListener("mouseleave", onLeave);
    });

    return () => {
      cards.forEach((card) => {
        card.removeEventListener("mousemove", onMove);
        card.removeEventListener("mouseleave", onLeave);
      });
    };
  }, []);
}

function HeroMockup() {
  const [pulse, setPulse] = useState(0);

  useEffect(() => {
    const id = window.setInterval(() => setPulse((value) => (value + 1) % 3), 2100);
    return () => window.clearInterval(id);
  }, []);

  const chat = useMemo(
    () => [
      ["client", "Привіт, хочу манікюр завтра після 16:00."],
      ["ai", "Можу запропонувати 16:30 або 17:45. Який час бронюємо?"],
      ["client", "16:30. Олена, +380..."],
      ["ai", "Готово. Я підготував запис для власника в BookSlot."],
    ],
    [],
  );

  return (
    <div className="hero-device" data-tilt>
      <div className="device-top">
        <div>
          <span>Telegram AI</span>
          <strong>BookSlot bot</strong>
        </div>
        <div className="device-orb">TG</div>
      </div>

      <div className="booking-card hero-ai-entry">
        <div className="booking-card-title">
          <span>Клієнт натискає AI-посилання</span>
          <b>@bookslot_beauty_bot</b>
        </div>
        <div className="telegram-header">
          <div className="bot-avatar">AI</div>
          <div>
            <strong>BookSlot bot</strong>
            <span>онлайн · питає деталі запису</span>
          </div>
        </div>
      </div>

      <div className="chat-stack">
        {chat.map(([type, text], index) => (
          <div className={`chat-bubble ${type}`} style={{ "--delay": `${index * 160}ms` }} key={text}>
            {text}
          </div>
        ))}
      </div>

      <div className="bot-suggestion">
        <span>Бот знайшов вільні слоти</span>
        <div className="ai-slot-row">
          {["14:30", "16:30", "17:45"].map((slot, index) => (
            <button className={index === pulse ? "active" : ""} key={slot} type="button">
              {slot}
            </button>
          ))}
        </div>
      </div>

      <div className="draft-card">
        <div>
          <span>Чернетка для власника</span>
          <strong>Олена · манікюр · 16:30</strong>
        </div>
        <b>готово</b>
      </div>

    </div>
  );
}

function OwnerMovie({ frame }) {
  return (
    <div className="movie-scenes owner-movie">
      <div
        className={`movie-scene dashboard-scene ${
          frame.ownerScene === "owner-dashboard" || frame.ownerScene === "owner-copy" ? "active" : ""
        } ${frame.ownerScene === "owner-copy" ? "is-copying" : ""}`}
      >
        <div className="dashboard-shell">
          <div className="dashboard-side">
            <span />
            <b>Записи</b>
            <b className="active">Посилання</b>
            <b>Послуги</b>
          </div>
          <div className="dashboard-main">
            <small>Кабінет BookSlot</small>
            <h3>Посилання для клієнтів</h3>
            <p>Дайте це посилання людям, щоб вони могли записатися самі.</p>
            <div className="copy-panel">
              <span>bookslot.app/beauty</span>
              <button type="button">Копіювати</button>
            </div>
            <div className="dashboard-hint">Сторінка запису вже готова</div>
          </div>
        </div>
        <span className="movie-pointer copy-pointer" />
        <div className={`copy-toast ${frame.ownerScene === "owner-copy" ? "visible" : ""}`}>Посилання скопійовано</div>
      </div>

      <div
        className={`movie-scene bio-scene ${frame.ownerScene === "owner-bio" || frame.ownerScene === "owner-live" ? "active" : ""} ${
          frame.ownerScene === "owner-bio" ? "is-editing" : ""
        } ${frame.ownerScene === "owner-live" ? "is-live" : ""}`}
      >
        <div className="social-editor">
          <div className="social-topline">
            <span>Редагування профілю</span>
            <b>Зберегти</b>
          </div>
          <div className="profile-row">
            <div className="insta-avatar">BS</div>
            <div>
              <strong>beauty.studio</strong>
              <small>Манікюр · брови · вії</small>
            </div>
          </div>
          <label className="profile-link-field">
            Сайт / посилання
            <span>bookslot.app/beauty</span>
          </label>
          <label className="profile-about-field">
            Опис профілю
            <span>Запис онлайн без Direct</span>
          </label>
        </div>
        <span className="movie-pointer bio-pointer" />
      </div>

      <div className={`movie-scene owner-live-scene ${frame.ownerScene === "owner-notified" ? "active" : ""}`}>
        <div className="owner-final-card">
          <small>Новий запис у BookSlot</small>
          <strong>Олена · Манікюр</strong>
          <span>Сьогодні о 16:30</span>
          <p>Email власнику вже відправлено.</p>
        </div>
        <div className="owner-mini-calendar">
          <b>16:30</b>
          <span>Манікюр</span>
        </div>
      </div>
    </div>
  );
}

function ClientMovie({ frame }) {
  return (
    <div className="movie-scenes client-movie">
      <div className={`movie-scene client-profile-scene ${frame.clientScene === "client-profile" || frame.clientScene === "client-tap" ? "active" : ""}`}>
        <div className="social-profile">
          <div className="profile-cover" />
          <div className="profile-head">
            <div className="insta-avatar">BS</div>
            <div>
              <strong>beauty.studio</strong>
              <small>Манікюр · брови · вії</small>
            </div>
          </div>
          <p>Вільні місця на цей тиждень. Запис онлайн нижче.</p>
          <button type="button">bookslot.app/beauty</button>
          <div className="social-grid">
            <span />
            <span />
            <span />
          </div>
        </div>
        <span className="movie-pointer tap-pointer" />
      </div>

      <div className={`movie-scene client-booking-scene ${frame.clientScene === "client-booking" ? "active" : ""}`}>
        <div className="public-booking">
          <small>BookSlot · Студія краси</small>
          <h3>Оберіть послугу</h3>
          <div className="service-option active">
            <span>Манікюр</span>
            <b>60 хв</b>
          </div>
          <div className="service-option">
            <span>Брови</span>
            <b>40 хв</b>
          </div>
          <div className="client-slots movie-slots">
            <span>14:30</span>
            <span className="selected">16:30</span>
            <span>17:45</span>
          </div>
        </div>
      </div>

      <div className={`movie-scene client-done-scene ${frame.clientScene === "client-done" ? "active" : ""}`}>
        <div className="client-confirmation">
          <small>Запис підтверджено</small>
          <strong>Манікюр · 16:30</strong>
          <span>beauty.studio</span>
          <p>Нагадування прийде за 24 години до запису.</p>
        </div>
      </div>
    </div>
  );
}

function PhoneScreen({ role, frame }) {
  const isOwner = role === "owner";

  return (
    <div className={`journey-phone ${isOwner ? "owner-phone" : "client-phone"}`} data-tilt>
      <div className="phone-hardware">
        <span />
        <b>{isOwner ? "Власник" : "Клієнт"}</b>
        <i>{frame.label}</i>
      </div>

      <div className="phone-screen movie-screen">
        <div className="phone-status">
          <span>{isOwner ? "BookSlot dashboard" : "Instagram → BookSlot"}</span>
          <b>{isOwner ? "Copy link" : "Book now"}</b>
        </div>
        {isOwner ? <OwnerMovie frame={frame} /> : <ClientMovie frame={frame} />}
      </div>

      <div className="journey-caption">
        <span>{isOwner ? frame.ownerTitle : frame.clientTitle}</span>
        <p>{isOwner ? frame.ownerText : frame.clientText}</p>
      </div>
    </div>
  );
}

function HowItWorks() {
  const [activeFrame, setActiveFrame] = useState(0);
  const frame = walkthroughFrames[activeFrame];

  useEffect(() => {
    const id = window.setTimeout(() => setActiveFrame((step) => (step + 1) % walkthroughFrames.length), 3400);
    return () => window.clearTimeout(id);
  }, [activeFrame]);

  return (
    <section className="journey-section" id="how-it-works">
      <div className="section-kicker" data-reveal>
        // як це працює
      </div>
      <div className="journey-heading" data-reveal>
        <h2>Один раз ставите посилання. Далі люди записуються самі.</h2>
        <p>
          Нижче показано як у маленькому відео: де власник бере лінк у BookSlot, куди вставляє його в профілі, і що
          бачить клієнт, коли натискає на це посилання.
        </p>
      </div>

      <div className="journey-stage" data-reveal>
        <PhoneScreen role="owner" frame={frame} />
        <div className="journey-bridge movie-bridge" aria-hidden="true">
          <span />
          <b>
            <small>крок</small>
            <strong>{frame.label}</strong>
          </b>
          <span />
        </div>
        <PhoneScreen role="client" frame={frame} />
      </div>

      <div className="journey-steps movie-steps" data-reveal>
        {walkthroughFrames.map((step, index) => (
          <button
            className={activeFrame === index ? "active" : ""}
            type="button"
            onClick={() => setActiveFrame(index)}
            key={step.label}
          >
            <span>{step.label}</span>
            <b>{step.title}</b>
          </button>
        ))}
      </div>
    </section>
  );
}

function App() {
  const root = document.getElementById("bookslot-react-root");
  const isLoggedIn = root?.dataset.loggedIn === "true";
  const profileInitial = root?.dataset.profileInitial || "B";
  const primaryHref = isLoggedIn ? "/Dashboard/AiAssistant" : "/Identity/Account/Register";
  const secondaryHref = isLoggedIn ? "/Dashboard" : "#ai-flow";

  useReveal();
  useTilt();

  return (
    <main className="auric-shell">
      <div className="grain" aria-hidden="true" />
      <div className="aurora aurora-one" aria-hidden="true" />
      <div className="aurora aurora-two" aria-hidden="true" />

      <header className="floating-nav" data-reveal>
        <a className="brand" href="/">
          <span className="brand-mark" />
          <span>BookSlot</span>
        </a>
        <nav aria-label="Навігація головної">
          {navItems.map((item) => (
            <a href={item.href} key={item.href}>
              {item.label}
            </a>
          ))}
        </nav>
        <div className="landing-nav-actions">
          <button className="bs-language-switch" type="button" data-language-switch aria-label="Перемкнути мову">
            <span>UA</span>
            <span>EN</span>
            <i aria-hidden="true" />
          </button>
          <a className="nav-cta" href={primaryHref}>
            {isLoggedIn ? "Відкрити AI" : "Почати"} <span>↗</span>
          </a>
          {isLoggedIn && (
            <a className="landing-profile-button" href="/Dashboard">
              <span className="landing-profile-avatar">{profileInitial}</span>
              <span>Профіль</span>
              <span className="landing-profile-menu">☰</span>
            </a>
          )}
        </div>
      </header>

      <section className="hero-section">
        <div className="hero-copy" data-reveal>
          <div className="eyebrow">
            <span />
            Приватна бета вже відкрита
          </div>
          <h1>Менше переписок. Більше записів. Спокійний графік.</h1>
          <p>
            BookSlot дає локальному бізнесу преміальний шар онлайн-запису: сторінку бронювання,
            Telegram AI-помічника, чисту логіку слотів і чернетки під контроль власника.
          </p>
          <div className="hero-actions">
            <a className="primary-button" href={primaryHref}>
              {isLoggedIn ? "Відкрити AI-помічника" : "Почати"} <span>→</span>
            </a>
            <a className="secondary-button" href={secondaryHref}>
              Подивитись сценарій
            </a>
          </div>
        </div>
        <div className="hero-visual" data-reveal>
          <HeroMockup />
        </div>
      </section>

      <HowItWorks />

      <section className="outcome-section" id="ai-flow">
        <div className="section-kicker" data-reveal>
          // можливості у результат
        </div>
        <h2 data-reveal>Спокійна операційна система для кожної розмови про запис.</h2>
        <div className="feature-grid">
          {features.map(([title, text], index) => (
            <article className="glass-card" data-reveal data-tilt key={title}>
              <span>{String(index + 1).padStart(2, "0")}</span>
              <h3>{title}</h3>
              <p>{text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="proof-section" id="proof">
        <div className="proof-grid">
          {proofCards.map((card) => (
            <article className="quote-card" data-reveal key={card.person}>
              <div className="stars">☆☆☆☆☆</div>
              <p>“{card.quote}”</p>
              <div>
                <strong>{card.person}</strong>
                <span>{card.role}</span>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="pricing-section" id="pricing">
        <div className="section-kicker" data-reveal>
          // прості тарифи
        </div>
        <h2 data-reveal>Оберіть шар запису, якого заслуговує ваш розклад.</h2>
        <div className="pricing-grid">
          {plans.map((plan) => (
            <article className={`price-card ${plan.featured ? "featured" : ""}`} data-reveal data-tilt key={plan.name}>
              {plan.featured && <div className="plan-badge">Преміальний AI-апгрейд</div>}
              <h3>{plan.name}</h3>
              <div className="price">
                {plan.price}
                <small> грн/міс</small>
              </div>
              <p>{plan.note}</p>
              <ul>
                {plan.items.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
              <a href={planHref(plan, isLoggedIn)}>
                {plan.cta} <span>→</span>
              </a>
            </article>
          ))}
        </div>
      </section>

      <section className="contact-section" id="contact">
        <div className="contact-copy" data-reveal>
          <div className="section-kicker">// швидкий контакт</div>
          <h2>Зберіть вашу систему запису в один фокус.</h2>
          <p>
            Запустіть публічне посилання для запису, дайте AI зібрати потрібні деталі
            і залиште фінальний контроль у вашому дашборді.
          </p>
          <a className="primary-button" href={primaryHref}>
            {isLoggedIn ? "До дашборда" : "Створити акаунт"} <span>↗</span>
          </a>
        </div>
        <div className="contact-panel" data-reveal>
          <div className="contact-card-top">
            <span>Контактний шар</span>
            <strong>Написати BookSlot</strong>
          </div>
          <a href="mailto:bookslot0@gmail.com">
            <span>Пошта</span>
            <strong>bookslot0@gmail.com</strong>
          </a>
          <a href="https://t.me/koletvl" target="_blank" rel="noreferrer">
            <span>Telegram</span>
            <strong>@koletvl</strong>
          </a>
          <a href="https://t.me/koletvl" target="_blank" rel="noreferrer" className="contact-action">
            Написати в Telegram <span>↗</span>
          </a>
        </div>
      </section>
    </main>
  );
}

createRoot(document.getElementById("bookslot-react-root")).render(<App />);
