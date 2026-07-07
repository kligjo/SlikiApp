window.slikiGallery = {
    _fileRegistry: new Map(),

    downloadFile(url, filename) {
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    },

    registerFiles(inputId) {
        const input = document.getElementById(inputId);
        if (!input?.files) return [];
        const result = [];
        for (const file of input.files) {
            const id = crypto.randomUUID();
            this._fileRegistry.set(id, file);
            result.push({
                id,
                previewUrl: file.type.startsWith('image/') ? URL.createObjectURL(file) : null,
                name: file.name,
                size: file.size,
                type: file.type || 'application/octet-stream'
            });
        }
        return result;
    },

    uploadFile(fileId, dotnetRef) {
        const file = this._fileRegistry.get(fileId);
        if (!file) {
            dotnetRef.invokeMethodAsync('OnUploadDone', false, '', 'File reference lost — please re-select the file.');
            return;
        }
        const formData = new FormData();
        formData.append('file', file, file.name);
        const xhr = new XMLHttpRequest();
        xhr.open('POST', '/api/images/upload');
        xhr.upload.onprogress = e => {
            if (e.lengthComputable)
                dotnetRef.invokeMethodAsync('OnUploadProgress', Math.round(e.loaded / e.total * 100));
        };
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                dotnetRef.invokeMethodAsync('OnUploadDone', true, xhr.responseText, '');
            } else {
                let error = 'Upload failed.';
                try { error = JSON.parse(xhr.responseText).error || error; } catch {}
                dotnetRef.invokeMethodAsync('OnUploadDone', false, '', error);
            }
        };
        xhr.onerror = () => dotnetRef.invokeMethodAsync('OnUploadDone', false, '', 'Network error.');
        xhr.send(formData);
    },

    releaseFiles(fileIds) {
        for (const id of fileIds) this._fileRegistry.delete(id);
    },

    revokePreviewUrls(urls) {
        for (const url of urls) { if (url) URL.revokeObjectURL(url); }
    },

    initInfiniteScroll(dotnetRef) {
        this.disposeInfiniteScroll();
        const sentinel = document.getElementById('scroll-sentinel');
        if (!sentinel) return;
        this._scrollObserver = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting) {
                dotnetRef.invokeMethodAsync('OnSentinelVisible');
            }
        }, { rootMargin: '400px' });
        this._scrollObserver.observe(sentinel);
    },

    disposeInfiniteScroll() {
        if (this._scrollObserver) {
            this._scrollObserver.disconnect();
            this._scrollObserver = null;
        }
    }
};
