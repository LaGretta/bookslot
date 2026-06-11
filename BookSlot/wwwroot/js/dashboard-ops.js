(function () {
    const cards = document.querySelectorAll(".ops-card-glow");
    cards.forEach((card) => {
        card.addEventListener("pointermove", (event) => {
            const rect = card.getBoundingClientRect();
            card.style.setProperty("--mx", `${event.clientX - rect.left}px`);
            card.style.setProperty("--my", `${event.clientY - rect.top}px`);
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

})();
