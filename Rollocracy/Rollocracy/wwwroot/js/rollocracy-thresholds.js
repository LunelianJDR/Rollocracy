window.rollocracyThresholds = window.rollocracyThresholds || {
    getElementRect: function (element) {
        if (!element) {
            return { left: 0, width: 0 };
        }

        const rect = element.getBoundingClientRect();
        return {
            left: rect.left,
            width: rect.width
        };
    }
};