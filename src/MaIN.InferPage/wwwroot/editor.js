window.editorManager = {
    getInnerText: (element) => {
        return element.innerText;
    },
    clearContent: (element) => {
        element.innerText = "";
    }
};
