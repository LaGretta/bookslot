(function () {
    const cards = document.querySelectorAll(".ops-card-glow");
    cards.forEach((card) => {
        card.addEventListener("pointermove", (event) => {
            const rect = card.getBoundingClientRect();
            card.style.setProperty("--mx", `${event.clientX - rect.left}px`);
            card.style.setProperty("--my", `${event.clientY - rect.top}px`);
        });
    });

    let dragged = null;
    document.querySelectorAll("[data-ops-draggable='booking']").forEach((booking) => {
        booking.addEventListener("dragstart", (event) => {
            dragged = booking;
            event.dataTransfer.effectAllowed = "move";
            event.dataTransfer.setData("text/plain", booking.dataset.bookingId || "");
        });
    });

    document.querySelectorAll("[data-ops-drop-day]").forEach((day) => {
        day.addEventListener("dragover", (event) => {
            event.preventDefault();
            event.dataTransfer.dropEffect = "move";
        });

        day.addEventListener("drop", (event) => {
            event.preventDefault();
            if (!dragged) return;

            const body = day.querySelector(".ops-day-body");
            if (!body) return;

            const dayRect = body.getBoundingClientRect();
            const snapped = Math.max(0, Math.round((event.clientY - dayRect.top) / 36) * 36);
            dragged.style.top = `${snapped}px`;
            body.appendChild(dragged);
            showOpsToast("Візуально перенесено. Для реальної зміни часу відкрий запис і підтверди дію.");
            dragged = null;
        });
    });

    window.copyOpsInput = function (inputId, button) {
        const input = document.getElementById(inputId);
        if (!input || !navigator.clipboard) return;

        navigator.clipboard.writeText(input.value).then(() => {
            const original = button.innerHTML;
            button.innerHTML = "<i class=\"bi bi-check-lg\"></i>";
            setTimeout(() => {
                button.innerHTML = original;
            }, 1800);
        });
    };

    function showOpsToast(message) {
        let toast = document.querySelector(".ops-toast");
        if (!toast) {
            toast = document.createElement("div");
            toast.className = "ops-toast";
            document.body.appendChild(toast);
        }
        toast.textContent = message;
        toast.classList.add("is-visible");
        clearTimeout(showOpsToast.timer);
        showOpsToast.timer = setTimeout(() => toast.classList.remove("is-visible"), 2800);
    }
})();
