window.settingsManager = {
    save: function (key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    },
    load: function (key) {
        try {
            var raw = localStorage.getItem(key);
            if (!raw) return null;
            return JSON.parse(raw);
        } catch (e) {
            return null;
        }
    },
    remove: function (key) {
        localStorage.removeItem(key);
    },
    exists: function (key) {
        return localStorage.getItem(key) !== null;
    }
};