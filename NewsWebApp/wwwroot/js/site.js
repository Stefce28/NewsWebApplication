(() => {
  const pulseClass = "shock-overload-pulse";

  if (document.body.classList.contains(pulseClass)) {
    window.setTimeout(() => {
      document.body.classList.remove(pulseClass);
    }, 900);
  }

  const overload = document.querySelector("[data-shock-overload]");

  if (!overload) {
    return;
  }

  const countdown = overload.querySelector("[data-shock-countdown]");
  const cooldownUntil = Number(overload.dataset.cooldownUntil);
  const resetUrl = overload.dataset.resetUrl;

  if (!countdown || !Number.isFinite(cooldownUntil)) {
    return;
  }

  const formatRemaining = milliseconds => {
    const totalSeconds = Math.max(0, Math.ceil(milliseconds / 1000));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${minutes}:${seconds.toString().padStart(2, "0")}`;
  };

  const updateCountdown = () => {
    const remaining = cooldownUntil - Date.now();
    countdown.textContent = formatRemaining(remaining);

    if (remaining <= 0) {
      window.clearInterval(timer);
      overload.classList.add("is-finished");
      resetShock();
      resetShockMeter();

      window.setTimeout(() => {
        overload.remove();
      }, 300);
    }
  };

  const resetShock = () => {
    if (!resetUrl) {
      return;
    }

    window.fetch(resetUrl, {
      method: "POST",
      headers: {
        "X-Requested-With": "XMLHttpRequest"
      }
    }).catch(() => {});
  };

  const resetShockMeter = () => {
    const meter = document.querySelector(".shock-meter");
    const fill = document.querySelector(".shock-meter-fill");
    const value = document.querySelector(".shock-meter-value");
    const track = document.querySelector(".shock-meter-track");

    if (meter) {
      meter.classList.remove(
        "shock-meter-medium",
        "shock-meter-high",
        "shock-meter-overload"
      );
      meter.classList.add("shock-meter-low");
    }

    if (fill) {
      fill.style.width = "0%";
    }

    if (value) {
      value.textContent = "0%";
    }

    if (track) {
      track.setAttribute("aria-valuenow", "0");
    }
  };

  const timer = window.setInterval(updateCountdown, 1000);
  updateCountdown();
})();
