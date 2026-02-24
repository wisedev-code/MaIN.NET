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
    },
    attachPasteHandler: (element, dotNetHelper) => {
        // Handle paste only
        element.addEventListener('paste', async (e) => {
            let imageFile = null;

            if (e.clipboardData?.files?.length > 0) {
                for (const file of e.clipboardData.files) {
                    if (file.type.startsWith('image/')) {
                        imageFile = file;
                        break;
                    }
                }
            }

            if (!imageFile && e.clipboardData?.items) {
                for (const item of e.clipboardData.items) {
                    if (item.type.startsWith('image/')) {
                        imageFile = item.getAsFile();
                        break;
                    }
                }
            }

            if (!imageFile) return;

            e.preventDefault();
            await editorManager._processFile(imageFile, dotNetHelper);
        });
    },
    attachDropZone: (containerId, dotNetHelper) => {
        const container = document.getElementById(containerId);
        if (!container) return;

        let dragCounter = 0;

        container.addEventListener('dragenter', async (e) => {
            e.preventDefault();
            e.stopPropagation();
            dragCounter++;
            if (dragCounter === 1) {
                await dotNetHelper.invokeMethodAsync('OnDragEnter');
            }
        });

        container.addEventListener('dragleave', async (e) => {
            e.preventDefault();
            e.stopPropagation();
            dragCounter--;
            if (dragCounter === 0) {
                await dotNetHelper.invokeMethodAsync('OnDragLeave');
            }
        });

        container.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });

        container.addEventListener('drop', async (e) => {
            e.preventDefault();
            e.stopPropagation();
            dragCounter = 0;

            const files = e.dataTransfer?.files;
            if (!files || files.length === 0) {
                await dotNetHelper.invokeMethodAsync('OnDragLeave');
                return;
            }

            for (const file of files) {
                await editorManager._processFile(file, dotNetHelper);
            }
        });
    },
    _processFile: async (file, dotNetHelper) => {
        try {
            const arrayBuffer = await file.arrayBuffer();
            const uint8Array = new Uint8Array(arrayBuffer);

            // Convert to base64 - much smaller than int array
            let binary = '';
            for (let i = 0; i < uint8Array.length; i++) {
                binary += String.fromCharCode(uint8Array[i]);
            }
            const base64 = btoa(binary);

            let extension = '';
            const lastDot = file.name.lastIndexOf('.');
            if (lastDot > 0) {
                extension = file.name.substring(lastDot);
            } else if (file.type) {
                extension = '.' + file.type.split('/')[1].replace('jpeg', 'jpg');
            }

            const fileName = file.name || `file-${Date.now()}${extension}`;

            await dotNetHelper.invokeMethodAsync('OnFilePasted', fileName, extension, base64);
        } catch {
            // Silent fail
        }
    }
};
