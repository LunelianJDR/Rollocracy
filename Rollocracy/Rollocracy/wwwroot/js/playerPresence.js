let heartbeatIntervalId = null;
let leaveHandler = null;
let rollocracyAlertAudio = null;

function postJson(url, payload) {
    return fetch(url, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(payload),
        keepalive: true
    }).catch(() => { });
}

export function playAlertSound() {
    try {
        if (!rollocracyAlertAudio) {
            rollocracyAlertAudio = new Audio("/sounds/alerte.mp3");
            rollocracyAlertAudio.preload = "auto";
        }

        rollocracyAlertAudio.currentTime = 0;
        const playPromise = rollocracyAlertAudio.play();

        if (playPromise && typeof playPromise.catch === "function") {
            playPromise.catch(() => {
                // Certains navigateurs bloquent l'audio sans interaction utilisateur.
            });
        }
    } catch {
    }
}

export function registerPlayerPresence(sessionId, playerSessionId, isGameMaster) {
    unregisterPlayerPresence();

    const heartbeatPayload = {
        sessionId: sessionId,
        playerSessionId: playerSessionId,
        isGameMaster: isGameMaster
    };

    const disconnectPayload = {
        sessionId: sessionId,
        playerSessionId: playerSessionId
    };

    const sendHeartbeat = () => {
        postJson("/api/presence/heartbeat", heartbeatPayload);
    };

    leaveHandler = () => {
        const blob = new Blob(
            [JSON.stringify(disconnectPayload)],
            { type: "application/json" }
        );

        navigator.sendBeacon("/api/presence/disconnect", blob);
    };

    sendHeartbeat();
    heartbeatIntervalId = window.setInterval(sendHeartbeat, 30000);

    window.addEventListener("pagehide", leaveHandler);
    window.addEventListener("beforeunload", leaveHandler);
}

export function unregisterPlayerPresence() {
    if (heartbeatIntervalId !== null) {
        window.clearInterval(heartbeatIntervalId);
        heartbeatIntervalId = null;
    }

    if (leaveHandler) {
        window.removeEventListener("pagehide", leaveHandler);
        window.removeEventListener("beforeunload", leaveHandler);
        leaveHandler = null;
    }
}

window.rollocracyRoom = window.rollocracyRoom || {};

window.rollocracyRoom.attachCharacterNameFilter = function (element) {
    if (!element) {
        return;
    }

    if (element.dataset.rollocracyNameFilterAttached === "1") {
        return;
    }

    const allowedCharRegex = /^[\p{L}\p{N},'"\/\- ]$/u;

    element.addEventListener("beforeinput", function (e) {
        if (e.inputType && e.inputType.startsWith("delete")) {
            return;
        }

        if (!e.data) {
            return;
        }

        if (!allowedCharRegex.test(e.data)) {
            e.preventDefault();
        }
    });

    element.addEventListener("paste", function (e) {
        const pastedText = (e.clipboardData || window.clipboardData)?.getData("text") ?? "";

        const filtered = Array.from(pastedText)
            .filter(char => allowedCharRegex.test(char))
            .join("")
            .slice(0, 22);

        e.preventDefault();

        const start = element.selectionStart ?? element.value.length;
        const end = element.selectionEnd ?? element.value.length;

        const currentValue = element.value ?? "";
        const nextValue =
            (currentValue.substring(0, start) + filtered + currentValue.substring(end)).slice(0, 22);

        element.value = nextValue;
        element.dispatchEvent(new Event("input", { bubbles: true }));

        const newCaretPosition = Math.min(start + filtered.length, nextValue.length);
        element.setSelectionRange(newCaretPosition, newCaretPosition);
    });

    element.dataset.rollocracyNameFilterAttached = "1";
};