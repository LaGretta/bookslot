import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import "./styles.css";

const navItems = ["AI flow", "Proof", "Pricing", "Contact"];

const metrics = [
  ["18s", "draft creation"],
  ["24/7", "client replies"],
  ["0 missed", "slot requests"],
];

const proofCards = [
  {
    quote: "BookSlot turns messy Telegram messages into clean booking drafts before the owner opens the phone.",
    person: "Beauty studio",
    role: "Manicure, brows, lashes",
  },
  {
    quote: "Clients see real slots, AI asks the missing details, and the dashboard stays calm.",
    person: "Local service",
    role: "Owner-operated schedule",
  },
  {
    quote: "The flow feels premium without becoming complicated. That is exactly the point.",
    person: "BookSlot Ultra AI",
    role: "Reception layer",
  },
];

const features = [
  ["Booking link", "A clean public page where clients choose service, date and time."],
  ["AI Receptionist", "Telegram replies that ask for service, date, time, name and contact."],
  ["Owner control", "AI prepares the draft, the owner confirms the booking inside dashboard."],
  ["Smart availability", "Slot logic stays connected to BookSlot schedule instead of guessing."],
];

const plans = [
  {
    name: "Free",
    price: "0",
    note: "For the first real clients",
    items: ["30 bookings / month", "Booking page", "Basic services", "No AI receptionist"],
    cta: "Start free",
  },
  {
    name: "Basic",
    price: "299",
    note: "For active local teams",
    items: ["200 bookings / month", "Unlimited services", "Email notifications", "Clean dashboard"],
    cta: "Choose Basic",
  },
  {
    name: "Ultra AI",
    price: "599",
    note: "The premium receptionist layer",
    featured: true,
    items: ["AI Receptionist in Telegram", "Booking drafts", "Ukrainian and Russian replies", "Priority AI upgrades"],
    cta: "Try Ultra AI",
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
          <span>LIVE QUEUE</span>
          <strong>12 requests</strong>
        </div>
        <div className="device-orb">AI</div>
      </div>

      <div className="booking-card">
        <div className="booking-card-title">
          <span>BookSlot link</span>
          <b>Beauty Studio</b>
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
          <span>Draft ready</span>
          <strong>Манікюр · 16:30</strong>
        </div>
        <b>confirm</b>
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
        <nav aria-label="Landing navigation">
          {navItems.map((item) => (
            <a href={`#${item.toLowerCase().replace(" ", "-")}`} key={item}>
              {item}
            </a>
          ))}
        </nav>
        <div className="landing-nav-actions">
          <a className="nav-cta" href={primaryHref}>
            {isLoggedIn ? "Open AI" : "Start free"} <span>↗</span>
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
            Private beta now open
          </div>
          <h1>Booking that thinks before the client waits.</h1>
          <p>
            BookSlot gives local businesses a premium scheduling layer: a booking page, Telegram AI receptionist,
            clean slot logic, and owner-approved drafts.
          </p>
          <div className="hero-actions">
            <a className="primary-button" href={primaryHref}>
              {isLoggedIn ? "Open AI Receptionist" : "Start free"} <span>→</span>
            </a>
            <a className="secondary-button" href={secondaryHref}>
              Watch the flow
            </a>
          </div>
        </div>
        <div className="hero-visual" data-reveal>
          <HeroMockup />
        </div>
      </section>

      <section className="outcome-section" id="ai-flow">
        <div className="section-kicker" data-reveal>
          // capabilities into outcomes
        </div>
        <h2 data-reveal>A quiet operating system for every booking conversation.</h2>
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
          // low-friction plans
        </div>
        <h2 data-reveal>Choose the reception layer your schedule deserves.</h2>
        <div className="pricing-grid">
          {plans.map((plan) => (
            <article className={`price-card ${plan.featured ? "featured" : ""}`} data-reveal data-tilt key={plan.name}>
              {plan.featured && <div className="plan-badge">Premium AI upgrade</div>}
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
          <div className="section-kicker">// low-friction inquiry</div>
          <h2>Bring your booking stack into focus.</h2>
          <p>
            Launch a public booking link, let AI collect the missing details, and keep final control inside your
            dashboard.
          </p>
          <a className="primary-button" href={primaryHref}>
            {isLoggedIn ? "Go to dashboard" : "Create account"} <span>↗</span>
          </a>
        </div>
        <div className="contact-panel" data-reveal>
          <div className="contact-card-top">
            <span>Contact layer</span>
            <strong>Talk to BookSlot</strong>
          </div>
          <a href="mailto:bookslot0@gmail.com">
            <span>Email</span>
            <strong>bookslot0@gmail.com</strong>
          </a>
          <a href="https://t.me/koletvl" target="_blank" rel="noreferrer">
            <span>Telegram</span>
            <strong>@koletvl</strong>
          </a>
          <a href="https://t.me/koletvl" target="_blank" rel="noreferrer" className="contact-action">
            Write in Telegram <span>↗</span>
          </a>
        </div>
      </section>

    </main>
  );
}

createRoot(document.getElementById("bookslot-react-root")).render(<App />);
