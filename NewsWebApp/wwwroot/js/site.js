(() => {
  const pulseClass = "shock-overload-pulse";
  const shockLevelClasses = [
    "shock-level-low",
    "shock-level-medium",
    "shock-level-high",
    "shock-level-overload"
  ];
  const shockMeterClasses = [
    "shock-meter-low",
    "shock-meter-medium",
    "shock-meter-high",
    "shock-meter-overload"
  ];

  const readValue = (source, camelName, pascalName) =>
    source?.[camelName] ?? source?.[pascalName];

  if (document.body.classList.contains(pulseClass)) {
    window.setTimeout(() => {
      document.body.classList.remove(pulseClass);
    }, 900);
  }

  let overload = null;
  let countdown = null;
  let cooldownUntil = NaN;
  let resetUrl = "";
  let timer = null;

  const applyShockStatus = status => {
    const meter = document.querySelector("[data-shock-meter]");
    const fill = document.querySelector("[data-shock-meter-fill]");
    const value = document.querySelector("[data-shock-meter-value]");
    const track = document.querySelector("[data-shock-meter-track]");

    if (!meter) {
      return;
    }

    const rawPercentage = Number(
      readValue(status, "currentPercentage", "CurrentPercentage") ?? 0
    );
    const percentage = Math.min(100, Math.max(0, Math.round(rawPercentage)));
    const levelClass = String(
      readValue(status, "levelClass", "LevelClass") ?? "low"
    );
    const isInCooldown = Boolean(
      readValue(status, "isInCooldown", "IsInCooldown")
    );
    const overload = document.querySelector("[data-shock-overload]");

    meter.classList.remove(...shockMeterClasses);
    meter.classList.add(`shock-meter-${levelClass}`);
    document.body.classList.remove(...shockLevelClasses);
    document.body.classList.add(`shock-level-${levelClass}`);

    if (fill) {
      fill.style.width = `${percentage}%`;
    }

    if (value) {
      value.textContent = `${percentage}%`;
    }

    if (track) {
      track.setAttribute("aria-valuenow", String(percentage));
    }

    if (!isInCooldown && overload) {
      overload.remove();
    }
  };

  const refreshShockStatus = () => {
    const meter = document.querySelector("[data-shock-meter]");
    const statusUrl = meter?.dataset.shockStatusUrl;

    if (!statusUrl) {
      return;
    }

    const separator = statusUrl.includes("?") ? "&" : "?";

    window.fetch(`${statusUrl}${separator}_=${Date.now()}`, {
      cache: "no-store",
      headers: {
        "X-Requested-With": "XMLHttpRequest"
      }
    })
      .then(response => {
        if (!response.ok) {
          return null;
        }

        return response.json();
      })
      .then(status => {
        if (status) {
          applyShockStatus(status);
        }
      })
      .catch(() => {});
  };

  const formatRemaining = milliseconds => {
    const totalSeconds = Math.max(0, Math.ceil(milliseconds / 1000));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    return `${minutes}:${seconds.toString().padStart(2, "0")}`;
  };

  const updateCountdown = () => {
    if (!overload || !countdown || !Number.isFinite(cooldownUntil)) {
      return;
    }

    const remaining = cooldownUntil - Date.now();
    countdown.textContent = formatRemaining(remaining);

    if (remaining <= 0) {
      if (timer) {
        window.clearInterval(timer);
      }

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
    const meter = document.querySelector("[data-shock-meter]");
    const fill = document.querySelector("[data-shock-meter-fill]");
    const value = document.querySelector("[data-shock-meter-value]");
    const track = document.querySelector("[data-shock-meter-track]");

    if (meter) {
      meter.classList.remove(...shockMeterClasses);
      meter.classList.add("shock-meter-low");
    }

    document.body.classList.remove(...shockLevelClasses);
    document.body.classList.add("shock-level-low");

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

  overload = document.querySelector("[data-shock-overload]");

  if (overload) {
    countdown = overload.querySelector("[data-shock-countdown]");
    cooldownUntil = Number(overload.dataset.cooldownUntil);
    resetUrl = overload.dataset.resetUrl;

    if (countdown && Number.isFinite(cooldownUntil)) {
      timer = window.setInterval(updateCountdown, 1000);
      updateCountdown();
    }
  }

  window.addEventListener("pageshow", refreshShockStatus);
  refreshShockStatus();
})();
