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

            // Ensure overlay is always dismissed after drop
            try { await dotNetHelper.invokeMethodAsync('OnDragLeave'); } catch {}
        });
    },
    _processFile: async (file, dotNetHelper) => {
        try {
            const base64 = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => resolve(reader.result.split(',')[1]);
                reader.onerror = () => reject(reader.error);
                reader.readAsDataURL(file);
            });

            let extension = '';
            const lastDot = file.name.lastIndexOf('.');
            if (lastDot > 0) {
                extension = file.name.substring(lastDot);
            } else if (file.type) {
                extension = '.' + file.type.split('/')[1].replace('jpeg', 'jpg');
            }

            const fileName = file.name || `file-${Date.now()}${extension}`;

            await dotNetHelper.invokeMethodAsync('OnFileReceived', fileName, extension, base64);
        } catch (err) {
            try { await dotNetHelper.invokeMethodAsync('OnDragLeave'); } catch {}
        }
    },
    copyImageToClipboard: async (base64) => {
        const res = await fetch(`data:image/png;base64,${base64}`);
        const blob = await res.blob();
        await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
    }
};
