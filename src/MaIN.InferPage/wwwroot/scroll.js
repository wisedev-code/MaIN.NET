window.scrollManager = {
    userScrolledUp: false,
    isProgrammaticScroll: false,
    _savedScrollTop: null,

    saveScrollPosition: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;
        window.scrollManager._savedScrollTop = container.scrollTop;
        container.style.overflowY = 'hidden';
    },

    restoreScrollPosition: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;
        if (window.scrollManager._savedScrollTop !== null) {
            container.scrollTop = window.scrollManager._savedScrollTop;
            window.scrollManager._savedScrollTop = null;
        } else {
            container.scrollTop = container.scrollHeight;
        }
        container.style.overflowY = '';
    },

    isAtBottom: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return false;
        return container.scrollHeight - container.scrollTop <= container.clientHeight + 50;
    },

    scrollToBottomSmooth: (containerId) => {
        if (window.scrollManager.userScrolledUp) return;
        const container = document.getElementById(containerId);
        if (!container) return;
        window.scrollManager.isProgrammaticScroll = true;
        container.scrollTop = container.scrollHeight;
        window.scrollManager.isProgrammaticScroll = false;
    },

    attachScrollListener: (containerId) => {
        const container = document.getElementById(containerId);
        if (!container) return;

        container.addEventListener("wheel", (e) => {
            if (e.deltaY < 0) {
                window.scrollManager.userScrolledUp = true;
            }
        });

        container.addEventListener("touchmove", () => {
            window.scrollManager.userScrolledUp = true;
        });

        container.addEventListener("scroll", () => {
            if (window.scrollManager.isProgrammaticScroll) return;
            const atBottom = container.scrollHeight - container.scrollTop <= container.clientHeight + 50;
            if (atBottom) {
                window.scrollManager.userScrolledUp = false;
            }
        });
    }
};
