let heartbeatIntervalId = null;
let leaveHandler = null;
let visibilityHandler = null;

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
        if (document.visibilityState === "visible") {
            postJson("/api/presence/heartbeat", heartbeatPayload);
        }
    };

    leaveHandler = () => {
        const blob = new Blob(
            [JSON.stringify(disconnectPayload)],
            { type: "application/json" }
        );

        navigator.sendBeacon("/api/presence/disconnect", blob);
    };

    visibilityHandler = () => {
        if (document.visibilityState === "visible") {
            sendHeartbeat();
        }
    };

    sendHeartbeat();
    heartbeatIntervalId = window.setInterval(sendHeartbeat, 5000);

    window.addEventListener("pagehide", leaveHandler);
    window.addEventListener("beforeunload", leaveHandler);
    document.addEventListener("visibilitychange", visibilityHandler);
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

    if (visibilityHandler) {
        document.removeEventListener("visibilitychange", visibilityHandler);
        visibilityHandler = null;
    }
}