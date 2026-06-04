import React, { useEffect, useMemo, useState } from "react";
import { createRoot } from "react-dom/client";
import { createPortal } from "react-dom";
import "./product.css";

const routes = [
  { href: "/Dashboard", label: "Hub", icon: "⌁" },
  { href: "/Dashboard/Bookings", label: "Records", icon: "◷" },
  { href: "/Dashboard/Services", label: "Services", icon: "✂" },
  { href: "/Dashboard/Schedule", label: "Schedule", icon: "◎" },
  { href: "/Dashboard/AiAssistant", label: "AI", icon: "✦" },
  { href: "/Dashboard/Subscription", label: "Plan", icon: "☆" },
  { href: "/Dashboard/Settings", label: "Settings", icon: "⚙" },
  { href: "/", label: "Home", icon: "↗" },
];

const pageCopy = {
  "/Dashboard": {
    title: "Control room",
    eyebrow: "Profile command layer",
    text: "One calm cockpit for bookings, AI drafts, public links and daily rhythm.",
  },
  "/Dashboard/Bookings": {
    title: "Booking records",
    eyebrow: "Live queue",
    text: "Filter, confirm and clean up client requests without losing the premium feel.",
  },
  "/Dashboard/Services": {
    title: "Service menu",
    eyebrow: "Offer stack",
    text: "Keep your public booking page sharp with services that feel easy to scan.",
  },
  "/Dashboard/Schedule": {
    title: "Time grid",
    eyebrow: "Availability engine",
    text: "Working hours, blocks and free slots stay connected to every client flow.",
  },
  "/Dashboard/AiAssistant": {
    title: "AI receptionist",
    eyebrow: "Conversation layer",
    text: "Telegram intake, suggested slots and draft bookings in one guided surface.",
  },
  "/Dashboard/Settings": {
    title: "Business profile",
    eyebrow: "Identity layer",
    text: "Your public link, logo and business details shape the client-facing experience.",
  },
  "/Dashboard/Subscription": {
    title: "Plan engine",
    eyebrow: "Growth layer",
    text: "Choose the booking and AI capacity that matches the pace of the business.",
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

    document.querySelectorAll(".sidebar").forEach((sidebar) => {
      sidebar.remove();
    });

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

function FlowRail() {
  return (
    <div className="bs-product-flow" aria-hidden="true">
      {["req", "match", "draft", "ok"].map((item, index) => (
        <span key={item} style={{ "--step": index }}>
          {item}
        </span>
      ))}
    </div>
  );
}

function ProductStage({ profileInitial }) {
  const page = useMemo(() => currentPage(), []);
  const path = window.location.pathname.replace(/\/$/, "") || "/";

  return (
    <section className="bs-product-stage">
      <div className="bs-product-stage-copy">
        <div className="bs-product-eyebrow">{page.eyebrow}</div>
        <h1>{page.title}</h1>
        <p>{page.text}</p>
      </div>

      <div className="bs-product-console">
        <div className="bs-product-console-top">
          <span className="bs-product-avatar">{profileInitial}</span>
          <div>
            <strong>BookSlot profile</strong>
            <span>react-enhanced workspace</span>
          </div>
        </div>
        <FlowRail />
      </div>

      <nav className="bs-product-routebar" aria-label="Product shortcuts">
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
  const root = document.getElementById("bookslot-product-react-root");
  const host = useContentHost();
  const profileInitial = root?.dataset.profileInitial || "B";

  useProductPolish();

  return (
    <>
      <AmbientLayer />
      {host ? createPortal(<ProductStage profileInitial={profileInitial} />, host) : null}
    </>
  );
}

const root = document.getElementById("bookslot-product-react-root");
if (root) {
  createRoot(root).render(<ProductExperience />);
}
