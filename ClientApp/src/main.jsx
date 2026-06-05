import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

const navItems = [
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
    person: "BookSlot Ultra AI",
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
    items: ["30 записів на місяць", "Сторінка запису", "Базові послуги", "Без AI-помічника"],
    cta: "Почати безкоштовно",
  },
  {
    name: "Basic",
    price: "299",
    note: "Для активного локального бізнесу",
    items: ["200 записів на місяць", "Необмежені послуги", "Поштові сповіщення", "Чистий дашборд"],
    cta: "Обрати Basic",
  },
  {
    name: "Ultra AI",
    price: "599",
    note: "Преміальний AI-помічник",
    featured: true,
    items: ["AI-помічник у Telegram", "Чернетки записів", "Відповіді українською і російською", "Пріоритетні AI-оновлення"],
    cta: "Спробувати Ultra AI",
  },
];

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
      ["client", "Привіт, є сьогодні манікюр після 16:00?"],
      ["ai", "Так, є 16:30. Підкажіть ім'я та номер телефону."],
      ["client", "Олена, +380..."],
    ],
    [],
  );

  return (
    <div className="hero-device" data-tilt>
      <div className="device-top">
        <div>
          <span>ЖИВА ЧЕРГА</span>
          <strong>12 запитів</strong>
        </div>
        <div className="device-orb">AI</div>
      </div>

      <div className="booking-card">
        <div className="booking-card-title">
          <span>Посилання BookSlot</span>
          <b>Студія краси</b>
        </div>
        <div className="slot-grid">
          {["14:30", "16:30", "17:45"].map((slot, index) => (
            <button className={index === pulse ? "active" : ""} key={slot} type="button">
              {slot}
            </button>
          ))}
        </div>
      </div>

      <div className="chat-stack">
        {chat.map(([type, text], index) => (
          <div className={`chat-bubble ${type}`} style={{ "--delay": `${index * 160}ms` }} key={text}>
            {text}
          </div>
        ))}
      </div>

      <div className="draft-card">
        <div>
          <span>Чернетка готова</span>
          <strong>Манікюр · 16:30</strong>
        </div>
        <b>підтвердити</b>
      </div>

      <div className="device-metrics">
        {metrics.map(([value, label]) => (
          <div key={label}>
            <strong>{value}</strong>
            <span>{label}</span>
          </div>
        ))}
      </div>
    </div>
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
              <a href={primaryHref}>
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
