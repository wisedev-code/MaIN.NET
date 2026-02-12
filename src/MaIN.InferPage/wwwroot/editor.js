window.editorManager = {
    getInnerText: (element) => {
        return element.innerText;
    },
    clearContent: (element) => {
        element.innerText = "";
    },
    clickElement: (id) => {
        const el = document.getElementById(id);
        if (el) el.click();
    }
};
