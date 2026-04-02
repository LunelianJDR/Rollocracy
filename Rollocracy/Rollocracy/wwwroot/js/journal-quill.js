window.rollocracyJournal = window.rollocracyJournal || {};

window.rollocracyJournal.findQuill = function (host) {
    if (!host) {
        return null;
    }

    if (host.__quill) {
        return host.__quill;
    }

    const allElements = [host, ...host.querySelectorAll("*")];
    for (const element of allElements) {
        if (element && element.__quill) {
            return element.__quill;
        }
    }

    return null;
};

window.rollocracyJournal.getHtml = function (host) {
    const quill = window.rollocracyJournal.findQuill(host);

    if (quill && quill.root) {
        return quill.root.innerHTML || "";
    }

    const editor = host ? host.querySelector(".ql-editor") : null;
    return editor ? (editor.innerHTML || "") : "";
};

window.rollocracyJournal.setHtml = function (host, html) {
    const quill = window.rollocracyJournal.findQuill(host);

    if (quill) {
        quill.setContents([]);
        quill.clipboard.dangerouslyPasteHTML(html || "");
        return true;
    }

    const editor = host ? host.querySelector(".ql-editor") : null;
    if (editor) {
        editor.innerHTML = html || "";
        return true;
    }

    return false;
};