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

    registerFiles(inputId, generatePreviews = true) {
        const input = document.getElementById(inputId);
        if (!input?.files) return [];
        const result = [];
        for (const file of input.files) {
            const id = crypto.randomUUID();
            this._fileRegistry.set(id, file);
            result.push({
                id,
                previewUrl: generatePreviews && file.type.startsWith('image/') ? URL.createObjectURL(file) : null,
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

    // Parallel-upload variant — passes fileId back in callbacks so multiple
    // in-flight uploads can be tracked independently.
    uploadFileParallel(fileId, dotnetRef, endpoint) {
        const file = this._fileRegistry.get(fileId);
        if (!file) {
            dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', 'File reference lost — please re-select.');
            return;
        }
        const formData = new FormData();
        formData.append('file', file, file.name);
        const xhr = new XMLHttpRequest();
        xhr.open('POST', endpoint);
        xhr.onload = () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, true, xhr.responseText, '');
            } else {
                let error = 'Upload failed.';
                try { error = JSON.parse(xhr.responseText).error || error; } catch {}
                dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', error);
            }
        };
        xhr.onerror = () => dotnetRef.invokeMethodAsync('OnSlikarDone', fileId, false, '', 'Network error.');
        xhr.send(formData);
    },

    releaseFiles(fileIds) {
        for (const id of fileIds) this._fileRegistry.delete(id);
    },

    revokePreviewUrls(urls) {
        for (const url of urls) { if (url) URL.revokeObjectURL(url); }
    },

    generateVideoThumbnails() {
        if (!this._thumbObserver) {
            this._thumbObserver = new IntersectionObserver(entries => {
                for (const entry of entries) {
                    if (!entry.isIntersecting) continue;
                    const video = entry.target;
                    if (video.dataset.thumbDone) continue;
                    video.dataset.thumbDone = '1';
                    this._thumbObserver.unobserve(video);
                    this._captureVideoThumb(video);
                }
            }, { rootMargin: '400px' });
        }
        for (const video of document.querySelectorAll('.photo-grid video.photo-thumb')) {
            if (!video.dataset.thumbDone) {
                this._thumbObserver.observe(video);
            }
        }
    },

    _captureVideoThumb(video) {
        const src = video.src;
        if (!src) return;
        const tmp = document.createElement('video');
        tmp.muted = true;
        tmp.playsInline = true;
        tmp.preload = 'metadata';
        tmp.src = src;
        tmp.addEventListener('loadedmetadata', () => {
            tmp.currentTime = tmp.duration > 1 ? 1 : 0.01;
        }, { once: true });
        tmp.addEventListener('seeked', () => {
            try {
                const canvas = document.createElement('canvas');
                canvas.width = tmp.videoWidth || 320;
                canvas.height = tmp.videoHeight || 180;
                canvas.getContext('2d').drawImage(tmp, 0, 0, canvas.width, canvas.height);
                video.poster = canvas.toDataURL('image/jpeg', 0.75);
            } catch (_) {}
            tmp.src = '';
            tmp.load();
        }, { once: true });
        tmp.load();
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
