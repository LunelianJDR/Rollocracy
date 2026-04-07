let heartbeatIntervalId = null;
let leaveHandler = null;
let alertAudio = null;

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

function getAlertAudio() {
    if (!alertAudio) {
        alertAudio = new Audio("/sounds/alerte.mp3");
        alertAudio.preload = "auto";
    }

    return alertAudio;
}

export function playAlertSound() {
    const audio = getAlertAudio();
    audio.currentTime = 0;
    audio.play().catch(() => { });
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
