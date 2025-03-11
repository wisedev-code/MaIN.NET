window.scrollManager = {
    isUserScrolling: false,

    saveScrollPosition: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;
        sessionStorage.setItem("scrollTop", container.scrollTop);
    },

    restoreScrollPosition: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;
        container.scrollTop = 9999;
    },

    isAtBottom: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return false;
        return container.scrollHeight - container.scrollTop <= container.clientHeight + 50;
    },

    scrollToBottomSmooth: (bottomElement) => {
        if (!bottomElement) return;
        if (!window.scrollManager.isUserScrolling) {
            bottomElement.scrollIntoView({ behavior: 'smooth' });
        }
    },

    attachScrollListener: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;

        container.addEventListener("scroll", () => {
            window.scrollManager.isUserScrolling =
                container.scrollHeight - container.scrollTop > container.clientHeight + 50;
        });
    }
};

document.addEventListener("DOMContentLoaded", () => {
    window.scrollManager.attachScrollListener("bottom");
});
