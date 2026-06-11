import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import { createPortal } from "react-dom";
import "./product.css";

const routes = [
  { href: "/Dashboard", label: "Центр", icon: "⌁" },
  { href: "/Dashboard/Bookings", label: "Записи", icon: "◷" },
  { href: "/Dashboard/Services", label: "Послуги", icon: "✂" },
  { href: "/Dashboard/Schedule", label: "Розклад", icon: "◎" },
  { href: "/Dashboard/AiAssistant", label: "AI", icon: "✦" },
  { href: "/Dashboard/Subscription", label: "Підписка", icon: "☆" },
  { href: "/Dashboard/Settings", label: "Налаштування", icon: "⚙" },
  { href: "/", label: "Головна", icon: "↗" },
];

const pageCopy = {
  "/Dashboard": {
    title: "Центр керування",
    eyebrow: "Командний шар профілю",
    text: "Один спокійний простір для записів, AI-чернеток, публічного посилання і щоденного ритму.",
  },
  "/Dashboard/Bookings": {
    title: "Усі записи",
    eyebrow: "Жива черга",
    text: "Фільтруйте, підтверджуйте і впорядковуйте запити клієнтів без втрати преміального відчуття.",
  },
  "/Dashboard/Services": {
    title: "Меню послуг",
    eyebrow: "Стек пропозицій",
    text: "Тримайте сторінку запису чистою, зрозумілою і швидкою для вибору послуги.",
  },
  "/Dashboard/Schedule": {
    title: "Сітка часу",
    eyebrow: "Двигун доступності",
    text: "Робочі години, блокування і вільні слоти залишаються пов'язаними з кожним клієнтським сценарієм.",
  },
  "/Dashboard/AiAssistant": {
    title: "AI-помічник",
    eyebrow: "Шар розмов",
    text: "Telegram-запити, запропоновані слоти і чернетки записів в одному зрозумілому просторі.",
  },
  "/Dashboard/Settings": {
    title: "Профіль бізнесу",
    eyebrow: "Шар айдентики",
    text: "Публічне посилання, логотип і деталі бізнесу формують перше враження клієнта.",
  },
  "/Dashboard/Subscription": {
    title: "Підписка",
    eyebrow: "Шар росту",
    text: "Оберіть ліміт записів і AI-можливості, які відповідають темпу вашого бізнесу.",
  },
};

function currentPage() {
  const path = window.location.pathname.replace(/\/$/, "") || "/";
  return pageCopy[path] || pageCopy[path.split("/").slice(0, 3).join("/")] || pageCopy["/Dashboard"];
}

function useContentHost() {
  const [host, setHost] = useState(null);

  useEffect(() => {
    const target =
      document.querySelector(".col-md-10.py-4") ||
      document.querySelector(".container.py-4") ||
      document.querySelector("main");

    if (!target) return undefined;

    const node = document.createElement("div");
    node.className = "bs-product-stage-host";
    target.prepend(node);
    target.classList.add("bs-product-enhanced-content");
    setHost(node);

    return () => {
      node.remove();
      target.classList.remove("bs-product-enhanced-content");
    };
  }, []);

  return host;
}

function useProductPolish() {
  useEffect(() => {
    document.body.classList.add("bs-product-react-active");

    const cards = [
      ...document.querySelectorAll(
        ".dashboard-ai-banner, .stat-card, .card, .lp-p-card, .ai-demo-phone, .booking-header, .service-card",
      ),
    ];

    cards.forEach((card, index) => {
      card.classList.add("bs-react-lift");
      card.style.setProperty("--stagger", `${Math.min(index, 12) * 42}ms`);
    });

    const rows = [...document.querySelectorAll("tbody tr")];
    rows.forEach((row, index) => {
      row.classList.add("bs-react-row");
      row.style.setProperty("--stagger", `${Math.min(index, 16) * 28}ms`);
    });

    const onMove = (event) => {
      document.documentElement.style.setProperty("--product-x", `${event.clientX}px`);
      document.documentElement.style.setProperty("--product-y", `${event.clientY}px`);
    };

    window.addEventListener("pointermove", onMove, { passive: true });

    return () => {
      document.body.classList.remove("bs-product-react-active");
      window.removeEventListener("pointermove", onMove);
    };
  }, []);
}

function AmbientLayer() {
  return (
    <div className="bs-product-ambient" aria-hidden="true">
      <div className="bs-product-aurora bs-product-aurora-a" />
      <div className="bs-product-aurora bs-product-aurora-b" />
      <div className="bs-product-scanline" />
      <div className="bs-product-cursor-glow" />
    </div>
  );
}

function ProductStage() {
  const page = useMemo(() => currentPage(), []);
  const path = window.location.pathname.replace(/\/$/, "") || "/";

  return (
    <section className="bs-product-stage">
      <div className="bs-product-stage-copy">
        <div className="bs-product-eyebrow">{page.eyebrow}</div>
        <h1>{page.title}</h1>
        <p>{page.text}</p>
      </div>

      <nav className="bs-product-routebar" aria-label="Навігація профілю">
        {routes.map((route) => (
          <a
            key={route.href}
            href={route.href}
            className={
              route.href === "/Dashboard"
                ? path === "/Dashboard"
                  ? "is-active"
                  : ""
                : route.href === "/"
                  ? path === "/"
                    ? "is-active"
                    : ""
                : path.startsWith(route.href)
                  ? "is-active"
                  : ""
            }
          >
            <span>{route.icon}</span>
            {route.label}
          </a>
        ))}
      </nav>
    </section>
  );
}

function ProductExperience() {
  useProductPolish();

  return (
    <>
      <AmbientLayer />
    </>
  );
}

const root = document.getElementById("bookslot-product-react-root");
if (root) {
  createRoot(root).render(<ProductExperience />);
}
